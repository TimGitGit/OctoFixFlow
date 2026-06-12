using Newtonsoft.Json.Linq;
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
        private bool _inputStateHandled = false;
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
        private async void UpdateRuntimeInfoUI(runtime_info info)
        {
            //Debug.WriteLine("=============");

            //Debug.WriteLine(info.ScriptName);
            //Debug.WriteLine(info.StepName);
            //Debug.WriteLine("SysState" + info.SysState);

            //Debug.WriteLine(info.TotalStep);
            //Debug.WriteLine(info.CurrentStep);
            //Debug.WriteLine("====++++++==");

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
                    _inputStateHandled = false;
                    StatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(153, 153, 153));
                    StatusText.Text = _mainWidget._res.ScriptUINotRun;
                    RunTimeText.Text = "00:00:00";
                    RunProgressBar.Value = 0;
                    _mainWidget.runFlag = false;
                    _mainWidget.pauseFlag = false;
                    break;
                case QybotrunPkg.runtime_info.Types.state.Err://err
                    nowState = 1;
                    _inputStateHandled = false;
                    //StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(231, 76, 60)); // 红色
                    StatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
                    StatusText.Text = _mainWidget._res.ScriptUILogError;
                    break;
                case QybotrunPkg.runtime_info.Types.state.Busy://run
                    nowState = 2;
                    _inputStateHandled = false;
                    //StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(39, 174, 96)); // 绿色
                    StatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96));
                    StatusText.Text = _mainWidget._res.ScriptUILogRun;
                    break;
                case QybotrunPkg.runtime_info.Types.state.Pause://pause
                    nowState = 3;
                    _inputStateHandled = false;
                    StatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18));
                    StatusText.Text = _mainWidget._res.ScriptUILogPause;
                    break;
                case QybotrunPkg.runtime_info.Types.state.Input://bridgee
                    nowState = 4;
                    StatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 18, 243));//蓝色
                    StatusText.Text = _mainWidget._res.ScriptUILogPause;
                    if (!_inputStateHandled)
                    {
                        _inputStateHandled = true;
                        var input = await _mainWidget.ScriptGetInputAsync();

                        var nowFluoData = input.Data;

                        JObject fluoData = JObject.Parse(nowFluoData);
                        float? fluoMinFloat = (float?)fluoData["fluoMin"];
                        JArray fluoW = (JArray)fluoData["fluoW"];
                        JArray dilutionPlan = (JArray)fluoData["plan"];

                        if (fluoMinFloat.HasValue)
                        {
                            float actualValue = fluoMinFloat.Value;
                            ConsFluoText.Text = actualValue.ToString();
                        }
                        if (fluoW != null)
                        {
                            //ClearAllWells(true);
                            // 1. 填充左边荧光值孔板（LeftPlateGrid）
                            foreach (JToken wellItem in fluoW)
                            {
                                int wellIndex = (int)wellItem[0];
                                double fluoValue = (double)wellItem[1];

                                if (FindName(GetWellControlName(wellIndex, "Left")) is Label lbl)
                                {
                                    lbl.Content = fluoValue.ToString("F0"); // 显示整数，要小数就改成F2
                                }
                            }
                        }
                        if (dilutionPlan != null)
                        {
                            //ClearAllWells(false);

                            // 2. 填充右边稀释方案孔板（RightPlateGrid）
                            foreach (JToken planItem in dilutionPlan)
                            {
                                int wellIndex = (int)planItem["well"];
                                double sampleVol = (double)planItem["sample_vol_uL"];
                                double diluentVol = (double)planItem["diluent_vol_uL"];

                                if (FindName(GetWellControlName(wellIndex, "Right")) is Label lbl)
                                {
                                    // 竖着显示：上面样本量，下面稀释液量
                                    lbl.Content = $"{sampleVol:F2}\n{diluentVol:F2}";
                                    lbl.FontSize = 15; // 调小字体，确保两行都能显示
                                }
                            }
                        }




                        mainRunControl.Visibility = Visibility.Collapsed;
                        mainRunShow.Visibility = Visibility.Visible;

                    }



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
        #region 数据显示页面
        private string GetWellControlName(int wellIndex, string prefix)
        {
            int row = wellIndex / 12;
            int col = wellIndex % 12 + 1;
            char rowChar = (char)('A' + row);
            return $"{prefix}_{rowChar}{col}";
        }

        // 清空所有孔位的旧数据（避免残留）
        private void ClearAllWells(bool isLeft)
        {
            if (isLeft)
            {
                // 清空左边荧光值孔板
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 1; col <= 12; col++)
                    {
                        if (FindName($"Left_{(char)('A' + row)}{col}") is Label lbl)
                        {
                            lbl.Content = string.Empty;
                            lbl.FontSize = 15;
                        }
                    }
                }
            }
            else
            {            // 清空右边稀释方案孔板
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 1; col <= 12; col++)
                    {
                        if (FindName($"Right_{(char)('A' + row)}{col}") is Label lbl)
                        {
                            lbl.Content = string.Empty;
                            lbl.FontSize = 15;
                        }
                    }
                }

            }



        }
        //切换显示界面
        private void runningToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainRunShow.Visibility == Visibility.Visible)
            {
                mainRunControl.Visibility = Visibility.Visible;
                mainRunShow.Visibility = Visibility.Collapsed;
            }
            else
            {
                mainRunControl.Visibility = Visibility.Collapsed;
                mainRunShow.Visibility = Visibility.Visible;
            }

        }
        //显示荧光检测数据页面
        private void RunFluoShow_Click(object sender, RoutedEventArgs e)
        {

        }
        //显示磁吸数据页面
        private void RunMagaShow_Click(object sender, RoutedEventArgs e)
        {

        }
        //显示振荡数据页面
        private void RunShakerShow_Click(object sender, RoutedEventArgs e)
        {

        }
        //确认
        private void RunShowConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.ScriptContinue, NotificationControl.NotificationType.Info);
            if (ConsFluoText.Text == null)
            {
                Dictionary<string, string> user_out = new();
                user_out.Add("dddd", "212");
                _mainWidget.ScriptSetInputAsync(user_out);
            }
            else
            {
                Dictionary<string, string> user_out = new();
                user_out.Add("fluoMin", ConsFluoText.Text);
                _mainWidget.ScriptSetInputAsync(user_out);

            }

            mainRunControl.Visibility = Visibility.Visible;
            mainRunShow.Visibility = Visibility.Collapsed;
        }
        #endregion


    }
}
