using QybotrunPkg;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace OctoFixFlow
{
    /// <summary>
    /// RunningInfoShow.xaml 的交互逻辑
    /// </summary>
    public partial class RunningInfoShow : UserControl
    {
        private readonly MainWidget _mainWidget;
        //grpc
        public ObservableCollection<string> RunLogs { get; } = new ObservableCollection<string>();

        // 添加私有变量记录上一次日志的步骤和状态，用于避免重复日志
        private int _lastLoggedStep = -1;
        private int _lastLoggedState = -1;
        private DispatcherTimer _infoPollingTimer; // 定时获取ScriptInfo的计时器
        private runtime_info _currentRuntimeInfo; // 存储当前实时运行信息
        public RunningInfoShow(MainWidget mainWidget)
        {
            InitializeComponent();
            _mainWidget = mainWidget;
            DataContext = this;
            InitProtect();
            InitInfoPollingTimer();
        }

        private void InitProtect()
        {
            guideProtocolNameTextBox.Text = AppGlobalConfig.Instance.GuideProtocolName;
            guideProtocolAuthorTextBox.Text = AppGlobalConfig.Instance.GuideProtocolAuthor;
            guideProtocolDescriptionTextBox.Text = AppGlobalConfig.Instance.GuideProtocolDescription;
            guideProtocolStartTimeTextBox.Text = AppGlobalConfig.Instance.GuideProtocolStartTime;

        }

        private void InitInfoPollingTimer()
        {
            // 先清理旧计时器，避免重复启动
            if (_infoPollingTimer != null)
            {
                _infoPollingTimer.Stop();
                _infoPollingTimer.Tick -= InfoPollingTimer_Tick;
                _infoPollingTimer = null;
            }

            // 创建UI线程安全的DispatcherTimer
            _infoPollingTimer = new DispatcherTimer();
            _infoPollingTimer.Interval = TimeSpan.FromSeconds(1); // 1秒轮询一次（可根据需求调整，如500ms）
            _infoPollingTimer.Tick += InfoPollingTimer_Tick; // 绑定定时触发事件
            _infoPollingTimer.Start(); // 启动计时器
        }
        private async void InfoPollingTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // 调用ScriptGetInfoAsync获取实时信息（假设该方法在_mainWidget中，若在当前类可直接调用）
                var runtimeInfo = await _mainWidget.ScriptGetInfoAsync();

                if (runtimeInfo != null)
                {
                    _currentRuntimeInfo = runtimeInfo; // 保存最新信息
                    UpdateRuntimeInfoUI(runtimeInfo); // 更新UI
                }
            }
            catch (Exception ex)
            {
                // 捕获异常，不中断轮询，仅输出日志（可选显示提示）
                Debug.WriteLine($"定时获取运行信息失败：{ex.Message}");
                // _mainWidget.ShowNotification($"获取运行信息失败：{ex.Message}", NotificationControl.NotificationType.Warn);
            }
        }
        private void UpdateRuntimeInfoUI(runtime_info info)
        {
            Debug.WriteLine("=============");

            Debug.WriteLine(info.ScriptName);
            Debug.WriteLine(info.StepName);
            Debug.WriteLine(info.SysState);

            Debug.WriteLine(info.TotalStep);
            Debug.WriteLine(info.CurrentStep);
            Debug.WriteLine("====++++++==");

            if (info.TotalStep > 0)
            {
                double progress = (double)info.CurrentStep / info.TotalStep * 100;
                RunProgressBar.Value = progress;
                //ProgressPercentText.Text = $"{(int)progress}%";
            }
            int nowState = -1;
            switch (info.SysState)
            {
                case QybotrunPkg.runtime_info.Types.state.Idle://idle
                    nowState = 0;
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153));
                    StatusText.Text = _mainWidget._res.ScriptUINotRun;
                    RunTimeText.Text = "00:00:00";
                    RunProgressBar.Value = 0;
                    _mainWidget.runFlag = false;
                    _mainWidget.pauseFlag = false;
                    break;
                case QybotrunPkg.runtime_info.Types.state.Err://err
                    nowState = 1;
                    //StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(231, 76, 60)); // 红色
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                    StatusText.Text = _mainWidget._res.ScriptUILogError;
                    break;
                case QybotrunPkg.runtime_info.Types.state.Busy://run
                    nowState = 2;
                    //StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(39, 174, 96)); // 绿色
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
                    StatusText.Text = _mainWidget._res.ScriptUILogRun;
                    break;
                case QybotrunPkg.runtime_info.Types.state.Pause://pause
                    nowState = 3;
                    // StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(243, 156, 18)); // 橙色
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(243, 156, 18));
                    StatusText.Text = _mainWidget._res.ScriptUILogPause;
                    break;
            }
            if (info.CurrentStep > 0 && info.CurrentStep != _lastLoggedStep)
            {
                GenerateStepLog(nowState, info.CurrentStep, info.TotalStep, info.StepName, info.Details);
                _lastLoggedStep = info.CurrentStep;
            }
            if (nowState == 1)
            {
                GenerateStepLog(nowState, info.CurrentStep, info.TotalStep, info.StepName, info.Details);
                _infoPollingTimer.Stop();
                _infoPollingTimer.Tick -= InfoPollingTimer_Tick;
                _infoPollingTimer = null;
            }
            //     bool shouldLog = (info.CurrentStep != _lastLoggedStep) ||
            //(nowState == 1 && _lastLoggedState != 1) ||
            //_lastLoggedStep == -1;
            //     Debug.WriteLine(shouldLog);
            //     Debug.WriteLine(info.CurrentStep);

            //     if (shouldLog && info.CurrentStep > 0)
            //     {
            //         // 添加日志条目
            //         GenerateStepLog(nowState, info.CurrentStep, info.TotalStep, info.StepName, info.Details);
            //         // 更新记录的步骤和状态
            //         _lastLoggedStep = info.CurrentStep;
            //         _lastLoggedState = nowState;
            //     }
        }


        // 监控数据处理方法（更新UI需通过Dispatcher）
        //private void OnMonitorDataReceived(object sender, ScriptMonitorEventArgs e)
        //{
        //    // 确保在UI线程处理（如果需要更新UI）
        //    Dispatcher.Invoke(() =>
        //    {
        //        // 更新运行时间 (将秒转换为时分秒格式)
        //        double seconds = e.RunTime / 1000.0;
        //        RunTimeText.Text = TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");

        //        //CurrentStepText.Text = $"{e.CurrentStep}/{e.MaxStep}";

        //        if (e.MaxStep > 0)
        //        {
        //            double progress = (double)e.CurrentStep / e.MaxStep * 100;
        //            RunProgressBar.Value = progress;
        //            //ProgressPercentText.Text = $"{(int)progress}%";
        //        }
        //        // 更新状态和指示器颜色
        //        StatusText.Text = e.State;
        //        switch (e.State)
        //        {
        //            case "run":
        //                //StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(39, 174, 96)); // 绿色
        //                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
        //                break;
        //            case "pause":
        //                // StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(243, 156, 18)); // 橙色
        //                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(243, 156, 18));
        //                break;
        //            case "idle":
        //                // StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(52, 152, 219)); // 蓝色
        //                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(52, 152, 219));
        //                _mainWidget.runFlag = false;
        //                _mainWidget.pauseFlag = false;

        //                break;
        //            case "err":
        //                //StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(231, 76, 60)); // 红色
        //                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
        //                break;
        //        }
        //        bool shouldLog = (e.CurrentStep != _lastLoggedStep) ||
        //               (e.State == "err" && _lastLoggedState != "err") ||
        //               _lastLoggedStep == -1;
        //        if (shouldLog && e.CurrentStep > 0)
        //        {
        //            // 添加日志条目
        //            GenerateStepLog(e.State, e.CurrentStep, e.MaxStep, e.ErrorCode, e.ErrorInfo);
        //            // 更新记录的步骤和状态
        //            _lastLoggedStep = e.CurrentStep;
        //            _lastLoggedState = e.State;
        //        }

        //    });
        //}
        private void GenerateStepLog(int state, int currentStep, int maxStep, string stepName, string stepDetails)
        {
            //string stepName = _mainWidget.FlowSteps[currentStep - 1].Type; ;

            string stateText = state == 2 ? _mainWidget._res.ScriptUILogRun :
                                 state == 3 ? _mainWidget._res.ScriptUILogPause :
                                 state == 0 ? _mainWidget._res.ScriptUILogIdle :
                                 state == 1 ? _mainWidget._res.ScriptUILogError : _mainWidget._res.ScriptUILogUnknown;

            // 5. 构建状态描述（步骤信息）
            string statusDesc = $"[{stateText}] / {stepName} ({currentStep}/{maxStep})";
            if (state == 1)
            {
                AddLogEntry(stepDetails);
            }
            AddLogEntry(statusDesc);
        }
        private void AddLogEntry(string message)
        {
            // 添加时间戳
            string logEntry = $"{DateTime.Now:HH:mm:ss} | {message}";

            // 添加到集合开头（最新日志在顶部）
            RunLogs.Insert(RunLogs.Count, logEntry);
            ScrollToBottom();
        }
        private void ScrollToBottom()
        {
            if (RunLogListBox.Items.Count > 0)
            {
                // 获取最后一项
                var lastItem = RunLogListBox.Items[RunLogListBox.Items.Count - 1];

                // 滚动到该项
                RunLogListBox.ScrollIntoView(lastItem);

                // 确保完全可见（处理虚拟化）
                RunLogListBox.UpdateLayout();
            }
        }

        private void ExitRunningClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is Window parentWindow)
            {
                parentWindow.Close();
            }
        }
        //流程暂停
        private async void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (!_mainWidget.pauseFlag)
            {
                if (!_mainWidget.runFlag)
                {
                    _mainWidget.ShowNotification(_mainWidget._res.ScriptNotStart, NotificationControl.NotificationType.Warn);
                    return;
                }
                _mainWidget.ShowNotification(_mainWidget._res.ScriptPause, NotificationControl.NotificationType.Info);

                await _mainWidget.ScriptPauseAsync();
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.ScriptContinue, NotificationControl.NotificationType.Info);

                await _mainWidget.ScriptContinueAsync();
            }
        }
        //流程停止
        private async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualPCRStop, NotificationControl.NotificationType.Info);

            await _mainWidget.ScriptStopAsync();
        }
    }
}
