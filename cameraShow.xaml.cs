using Microsoft.Web.WebView2.Core;
using System.Windows;

namespace OctoFixFlow
{
    public partial class CameraShowWindow : Window
    {
        private readonly string _streamUrl = "http://192.168.100.10:8080/?action=stream";

        public CameraShowWindow()
        {
            InitializeComponent();
            Loaded += CameraShowWindow_Loaded;
        }

        private async void CameraShowWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var envOptions = new CoreWebView2EnvironmentOptions();
                envOptions.AdditionalBrowserArguments = "--allow-running-insecure-content";

                var webViewEnv = await CoreWebView2Environment.CreateAsync(null, null, envOptions);
                await webView.EnsureCoreWebView2Async(webViewEnv);

                string rotatedHtml = $@"
                <!DOCTYPE html>
                <html style='margin:0; padding:0; width:100%; height:100%;'>
                    <body style='margin:0; padding:0; width:100%; height:100%;'>
                        <!-- CSS transform: rotate(180deg) 实现180度旋转 -->
                        <img src='{_streamUrl}' 
                             style='width:100%; height:100%; object-fit:contain; 
                                    transform: rotate(180deg); 
                                    transform-origin: center center;'>
                    </body>
                </html>";

                webView.NavigateToString(rotatedHtml);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"失败原因：{ex.Message}\n1. 确认安装WebView2运行时\n2. 确认摄像头IP可访问", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}