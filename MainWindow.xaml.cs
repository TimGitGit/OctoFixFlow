using Assimp;
using Serilog;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace OctoFixFlow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int MAX_NOTIFICATIONS = 3;
        private readonly ResourceHelper _res;
        public MainWindow()
        {
            InitializeComponent();
            _res = ResourceHelper.Instance;
            Loaded += MainWindow_Loaded;
        }

        #region 异步后台加载3D模型
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.IsRememberChecked)
            {
                chkRememberUser.IsChecked = true;
                login_Name.Text = Properties.Settings.Default.RememberUserName;
            }
            if (Properties.Settings.Default.IsRememberDevice)
            {
                chkAutoLoadDevice.IsChecked = true;
            }
            try
            {
                string modelResourcePath = "/OctoFixFlow;component/images/QYRB-12C.fbx";
                var resourceUri = new Uri(modelResourcePath, UriKind.Relative);
                var resourceInfo = Application.GetResourceStream(resourceUri);

                if (resourceInfo?.Stream == null)
                {
                    ShowNotification(_res.MainWindowDetailSTL, NotificationControl.NotificationType.Error);
                    return;
                }

                Model3DGroup modelGroup = null;
                await Task.Run(() =>
                {
                    using (resourceInfo.Stream)
                    using (var importer = new AssimpContext())
                    {
                        PostProcessSteps postProcess = PostProcessSteps.GenerateNormals
                                                     | PostProcessSteps.Triangulate
                                                     | PostProcessSteps.FlipUVs;

                        var scene = importer.ImportFileFromStream(resourceInfo.Stream, postProcess, "fbx");
                        modelGroup = ConvertAssimpSceneToModel3DGroup(scene);


                        Transform3DGroup transformGroup = new Transform3DGroup();

                        transformGroup.Children.Add(new RotateTransform3D(
                           new AxisAngleRotation3D(new System.Windows.Media.Media3D.Vector3D(1, 0, 0), 90)));
                        modelGroup.Transform = transformGroup;


                        if (!modelGroup.IsFrozen) modelGroup.Freeze();
                    }
                });

                if (modelGroup != null)
                {
                    modelVisual.Content = modelGroup;
                    var camera = modelViewport.Camera as PerspectiveCamera;
                    if (camera != null)
                    {
                        camera.Position = new Point3D(0, 10, 0);
                        camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(-5, -0.1, 0);
                        camera.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);
                    }
                    modelViewport.ZoomExtents();
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowNotification($"{_res.MainWindowDetailLoadFail}: {ex.Message}", NotificationControl.NotificationType.Error);
                    Log.Error($"模型加载异常：{ex.Message}\r\n{ex.StackTrace}");
                });
            }
        }
        /// <summary>
        /// 抽离模型转换逻辑，纯CPU计算，在后台线程执行，线程安全
        /// </summary>
        private Model3DGroup ConvertAssimpSceneToModel3DGroup(Scene scene)
        {
            var modelGroup = new Model3DGroup();
            foreach (var mesh in scene.Meshes)
            {
                var geometry = new MeshGeometry3D();

                foreach (var vertex in mesh.Vertices)
                {
                    geometry.Positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));
                }

                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    var face = mesh.Faces[i];
                    if (face.IndexCount == 3)
                    {
                        geometry.TriangleIndices.Add((int)face.Indices[0]);
                        geometry.TriangleIndices.Add((int)face.Indices[1]);
                        geometry.TriangleIndices.Add((int)face.Indices[2]);
                    }
                }

                if (mesh.HasNormals)
                {
                    foreach (var normal in mesh.Normals)
                    {
                        geometry.Normals.Add(new System.Windows.Media.Media3D.Vector3D(normal.X, normal.Y, normal.Z));
                    }
                }

                System.Windows.Media.Media3D.Material material = new DiffuseMaterial(Brushes.Gray);
                if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < scene.Materials.Count)
                {
                    Assimp.Material assimpMat = scene.Materials[mesh.MaterialIndex];
                    if (assimpMat.HasColorDiffuse)
                    {
                        var assimpColor = assimpMat.ColorDiffuse;
                        var wpfColor = Color.FromArgb(
                            (byte)(assimpColor.A * 255),
                            (byte)(assimpColor.R * 255),
                            (byte)(assimpColor.G * 255),
                            (byte)(assimpColor.B * 255)
                        );
                        material = new DiffuseMaterial(new SolidColorBrush(wpfColor));
                    }
                }

                var geoModel = new GeometryModel3D(geometry, material);
                geoModel.BackMaterial = material;
                modelGroup.Children.Add(geoModel);
            }
            return modelGroup;
        }
        #endregion

        /// <summary>
        /// 适配AssimpNet 4.1.0的模型加载方法
        /// </summary>
        private Model3DGroup Load3DModelFromStream(Stream stream, string fileExtension)
        {
            var importer = new AssimpContext();

            // 4.1.0版本核心：用PostProcessSteps替代ImportSettings（官方唯一支持的参数）
            PostProcessSteps postProcess = PostProcessSteps.GenerateNormals   // 自动生成法线（必加，否则模型无光照）
                                         | PostProcessSteps.Triangulate    // 强制三角面（WPF 3D仅支持三角面）
                                         | PostProcessSteps.FlipUVs;       // 适配WPF纹理方向

            // 4.1.0版本ImportFromStream正确参数：Stream + 格式后缀 + PostProcessSteps
            // 第二个参数：文件格式（fbx/step/stl，小写），第三个参数：后处理步骤
            var scene = importer.ImportFileFromStream(stream, postProcess, fileExtension.Trim('.').ToLower());

            var modelGroup = new Model3DGroup();
            foreach (var mesh in scene.Meshes)
            {
                var geometry = new MeshGeometry3D();

                // 填充顶点（Assimp.Vector3D → WPF.Point3D）
                foreach (var vertex in mesh.Vertices)
                {
                    geometry.Positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));
                }

                // 填充三角面索引
                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    var face = mesh.Faces[i];
                    if (face.IndexCount == 3)
                    {
                        geometry.TriangleIndices.Add((int)face.Indices[0]);
                        geometry.TriangleIndices.Add((int)face.Indices[1]);
                        geometry.TriangleIndices.Add((int)face.Indices[2]);
                    }
                }

                // 填充法线（解决模型发黑/无颜色问题）
                if (mesh.HasNormals)
                {
                    foreach (var normal in mesh.Normals)
                    {
                        // 明确指定WPF的Vector3D，避免与Assimp冲突
                        geometry.Normals.Add(new System.Windows.Media.Media3D.Vector3D(normal.X, normal.Y, normal.Z));
                    }
                }

                // 读取SolidWorks原始颜色（核心：区分Assimp和WPF的Material）
                System.Windows.Media.Media3D.Material material = new DiffuseMaterial(Brushes.Gray); // 兜底颜色
                if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < scene.Materials.Count)
                {
                    // 明确指定Assimp的Material
                    Assimp.Material assimpMat = scene.Materials[mesh.MaterialIndex];
                    if (assimpMat.HasColorDiffuse)
                    {
                        var assimpColor = assimpMat.ColorDiffuse;
                        // 转换Assimp颜色（0~1）→ WPF颜色（0~255）
                        var wpfColor = Color.FromArgb(
                            (byte)(assimpColor.A * 255),
                            (byte)(assimpColor.R * 255),
                            (byte)(assimpColor.G * 255),
                            (byte)(assimpColor.B * 255)
                        );
                        material = new DiffuseMaterial(new SolidColorBrush(wpfColor));
                    }
                }

                // 创建WPF 3D模型并添加到组
                var geoModel = new GeometryModel3D(geometry, material);
                geoModel.BackMaterial = material; // 双面渲染，避免背面透明
                modelGroup.Children.Add(geoModel);
            }

            return modelGroup;
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (login_Name.Text == "")
            {
                ShowNotification(_res.MainWindowDetailUserEmpty, NotificationControl.NotificationType.Warn);
                return;
            }
            if (login_pass.Password == "")
            {
                ShowNotification(_res.MainWindowDetailPassEmpty, NotificationControl.NotificationType.Warn);
                return;
            }


            Properties.Settings.Default.IsRememberDevice = chkAutoLoadDevice.IsChecked ?? false;
            if (Properties.Settings.Default.IsRememberDevice)
            {
                bool isServerReachable = DatabaseService.Instance.IsMySqlServerReachable();
                if (!isServerReachable)
                {
                    ShowNotification(_res.MainWindowNotConn, NotificationControl.NotificationType.Error);
                    return;
                }

            }

            Properties.Settings.Default.IsRememberChecked = chkRememberUser.IsChecked ?? false;
            if (Properties.Settings.Default.IsRememberChecked)
            {
                Properties.Settings.Default.RememberUserName = login_Name.Text;
            }
            else
            {
                Properties.Settings.Default.RememberUserName = "";
            }
            Properties.Settings.Default.Save();

            MainWidget mWidget = new MainWidget();
            Application.Current.MainWindow = mWidget;
            this.Close();
            mWidget.Show();
            //mWidget.InitializeCameraAsync();
            ShowNotification($"{_res.MainWindowDetailLoginIN}: {login_Name.Text}", NotificationControl.NotificationType.Info);
            bool isAutoLoad = chkAutoLoadDevice.IsChecked ?? false;
            ShowGuideWindow(mWidget, isAutoLoad);
        }
        //退出按钮
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            DatabaseService.Instance.Close();

            Application.Current.Shutdown();
        }
        // 显示引导窗口的方法
        private void ShowGuideWindow(MainWidget mainWidget, bool isAutoLoad)
        {
            mainWidget.IsEnabled = false;

            GuideWindow guideWindow = new GuideWindow(isAutoLoad);
            guideWindow.Owner = mainWidget;
            guideWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            guideWindow.GuideCompleted += () =>
            {
                mainWidget.UpdateDeviceModule();
                mainWidget.IsEnabled = true;
                var plateModuleMap = AppGlobalConfig.Instance.PlateModuleMap;

                foreach (var (plateId, moduleDatas) in plateModuleMap)
                {
                    Border targetBorder = mainWidget.FindPlateBorderByPlateId(plateId);

                    mainWidget.Dispatcher.Invoke(() =>
                    {
                        mainWidget.UpdatePlateDisplay(targetBorder, moduleDatas);
                    });
                }
            };

            guideWindow.Show();
        }
        public void ShowNotification(string message, NotificationControl.NotificationType type, int duration = 3000)
        {
            Dispatcher.Invoke(() =>
            {
                if (NotificationHost.Children.Count >= MAX_NOTIFICATIONS)
                {
                    var oldestNotification = NotificationHost.Children[0] as NotificationControl;
                    oldestNotification?.Close();
                }

                var notification = new NotificationControl(message, type, duration);
                NotificationHost.Children.Add(notification);
                if (type == NotificationControl.NotificationType.Info)
                {
                    Log.Information(message);
                }
                else if (type == NotificationControl.NotificationType.Warn)
                {
                    Log.Warning(message);
                }
                else if (type == NotificationControl.NotificationType.Error)
                {
                    Log.Error(message);
                }
                UpdateNotificationPositions();
            });
        }

        private void UpdateNotificationPositions()
        {
            double topPosition = 0;
            foreach (var child in NotificationHost.Children)
            {
                if (child is NotificationControl notification)
                {
                    var transform = notification.RenderTransform as TranslateTransform;
                    if (transform != null)
                    {
                        transform.Y = topPosition;
                        topPosition += notification.ActualHeight;
                    }
                }
            }
        }


    }
}