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

        #region 核心优化：异步后台加载3D模型，彻底解决卡顿
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
                    modelViewport.ZoomExtents();
                    //if (modelViewport.Camera is PerspectiveCamera camera)
                    //{
                    //    camera.Position = new System.Windows.Media.Media3D.Point3D(-2000, 0, 0);
                    //    camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(-1, 0, 0);
                    //    camera.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);
                    //    camera.FieldOfView = 15;
                    //}
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

        //    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        //    {
        //        // 核心修改：延迟加载3D模型，让窗口先完成初始化（包括任务栏图标渲染）
        //        // DispatcherPriority.Background 表示窗口初始化完成后再执行
        //        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        //        {
        //            string modelResourcePath = "/OctoFixFlow;component/images/QYRB-12C.fbx";
        //            var resourceUri = new Uri(modelResourcePath, UriKind.Relative);
        //            var resourceInfo = Application.GetResourceStream(resourceUri);

        //            Model3DGroup modelGroup = Load3DModelFromStream(resourceInfo.Stream, Path.GetExtension(modelResourcePath));
        //            Transform3DGroup transformGroup = new Transform3DGroup();
        //            transformGroup.Children.Add(new RotateTransform3D(
        //new AxisAngleRotation3D(new System.Windows.Media.Media3D.Vector3D(1, 0, 0), 90)));


        //            // 步骤4：把组合旋转应用到模型
        //            modelGroup.Transform = transformGroup;
        //            modelVisual.Content = modelGroup;
        //            modelViewport.ZoomExtents();

        //            // 最后关闭流，释放资源
        //            resourceInfo.Stream.Close();
        //        }));
        //    }


        //    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        //    {
        //        try
        //        {
        //            // 模型路径：支持FBX/STEP/STL，确保文件生成操作是Resource
        //            string modelResourcePath = "/OctoFixFlow;component/images/QYRB-12C.fbx";
        //            var resourceUri = new Uri(modelResourcePath, UriKind.Relative);

        //            var resourceInfo = Application.GetResourceStream(resourceUri);
        //            if (resourceInfo?.Stream == null)
        //            {
        //                ShowNotification(_res.MainWindowDetailSTL, NotificationControl.NotificationType.Error);
        //                return;
        //            }

        //            // 加载模型（适配4.1.0 API）
        //            Model3DGroup modelGroup = Load3DModelFromStream(resourceInfo.Stream, Path.GetExtension(modelResourcePath));
        //            RotateTransform3D rotateTransform = new RotateTransform3D(
        //new AxisAngleRotation3D(new System.Windows.Media.Media3D.Vector3D(1, 0, 0), 90)); // 轴：X轴(1,0,0)，角度：90度
        //            modelGroup.Transform = rotateTransform; // 给模型组应用旋转
        //            modelVisual.Content = modelGroup;
        //            modelViewport.ZoomExtents();
        //        }
        //        catch (Exception ex)
        //        {
        //            ShowNotification($"{_res.MainWindowDetailLoadFail}: {ex.Message}", NotificationControl.NotificationType.Error);
        //        }
        //    }

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
        //private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        string stlResourcePath = "/OctoFixFlow;component/images/QYRB-12C.STL";
        //        var resourceUri = new Uri(stlResourcePath, UriKind.Relative);

        //        var resourceInfo = Application.GetResourceStream(resourceUri);
        //        if (resourceInfo?.Stream == null)
        //        {
        //            ShowNotification(_res.MainWindowDetailSTL, NotificationControl.NotificationType.Error);
        //            return;
        //        }

        //        var reader = new StLReader();
        //        using (resourceInfo.Stream)
        //        {
        //            Model3DGroup modelGroup = reader.Read(resourceInfo.Stream);

        //            // 应用自定义材质（替换默认蓝色）
        //            ApplyCustomMaterial(modelGroup);

        //            modelVisual.Content = modelGroup;
        //        }
        //        modelViewport.ZoomExtents();
        //    }
        //    catch (Exception ex)
        //    {
        //        ShowNotification($"{_res.MainWindowDetailLoadFail}: {ex.Message}", NotificationControl.NotificationType.Error);
        //    }
        //}

        // 递归应用材质的辅助方法
        //private void ApplyCustomMaterial(Model3DGroup group)
        //{
        //    foreach (var model in group.Children)
        //    {
        //        if (model is Model3DGroup subGroup)
        //        {
        //            ApplyCustomMaterial(subGroup);
        //        }
        //        else if (model is GeometryModel3D geometryModel)
        //        {
        //            // 创建银色金属质感材质
        //            var materialGroup = new MaterialGroup();
        //            materialGroup.Children.Add(new DiffuseMaterial(Brushes.Silver));
        //            materialGroup.Children.Add(new SpecularMaterial(Brushes.White, 100));

        //            geometryModel.Material = materialGroup;
        //            geometryModel.BackMaterial = materialGroup; // 双面渲染
        //        }
        //    }
        //}
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

            // 显示引导窗口（非模态，但由于MainWidget被禁用，用户必须先完成引导）
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