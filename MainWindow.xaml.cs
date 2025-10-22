using HelixToolkit.Wpf;
using Serilog;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string stlResourcePath = "/OctoFixFlow;component/images/OctoFlow3D.STL";
                var resourceUri = new Uri(stlResourcePath, UriKind.Relative);

                var resourceInfo = Application.GetResourceStream(resourceUri);
                if (resourceInfo?.Stream == null)
                {
                    ShowNotification(_res.MainWindowDetailSTL, NotificationControl.NotificationType.Error);
                    return;
                }

                // 使用StLReader替代ModelImporter（支持直接读取流）
                var reader = new StLReader();
                using (resourceInfo.Stream)
                {
                    Model3DGroup modelGroup = reader.Read(resourceInfo.Stream);

                    // 应用自定义材质（替换默认蓝色）
                    ApplyCustomMaterial(modelGroup);

                    modelVisual.Content = modelGroup;
                }
                modelViewport.ZoomExtents();
            }
            catch (Exception ex)
            {
                ShowNotification($"{_res.MainWindowDetailLoadFail}: {ex.Message}", NotificationControl.NotificationType.Error);
            }
        }

        // 递归应用材质的辅助方法
        private void ApplyCustomMaterial(Model3DGroup group)
        {
            foreach (var model in group.Children)
            {
                if (model is Model3DGroup subGroup)
                {
                    ApplyCustomMaterial(subGroup);
                }
                else if (model is GeometryModel3D geometryModel)
                {
                    // 创建银色金属质感材质
                    var materialGroup = new MaterialGroup();
                    materialGroup.Children.Add(new DiffuseMaterial(Brushes.Silver));
                    materialGroup.Children.Add(new SpecularMaterial(Brushes.White, 100));

                    geometryModel.Material = materialGroup;
                    geometryModel.BackMaterial = materialGroup; // 双面渲染
                }
            }
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
            MainWidget mWidget = new MainWidget();
            Application.Current.MainWindow = mWidget;
            this.Close();

            mWidget.Show();
            //mWidget.InitializeCameraAsync();
            ShowNotification($"{_res.MainWindowDetailLoginIN}: {login_Name.Text}", NotificationControl.NotificationType.Info);
        }
        //退出按钮
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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