using System.Configuration;
using System.Data;
using System.Windows;
using Serilog;

namespace OctoFixFlow
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex? _mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "OctoFixFlow";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                // 应用已在运行，通知现有实例并退出
                MessageBox.Show("The application is already running", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }
            // 配置全局Logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush(); //在应用退出时关闭日志
            _mutex?.ReleaseMutex();
            base.OnExit(e);
        }
    }

}
