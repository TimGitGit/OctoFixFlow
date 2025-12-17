using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OctoFixFlow
{
    /// <summary>
    /// SettingPopupControl.xaml 的交互逻辑
    /// </summary>
    public partial class SettingPopupControl : UserControl
    {
        private readonly MainWidget _mainWidget;
        private string nowModuleName;
        public SettingPopupControl(MainWidget mainWidget)
        {
            InitializeComponent();
            _mainWidget = mainWidget;
        }
        /// <summary>
        /// 显示弹窗：传入标题+设置内容UI
        /// </summary>
        public void Show(int moduleType, string moduleName)
        {
            nowModuleName = moduleName;
            settingTitle.Text = nowModuleName;

            switch (moduleType)//0：单通道移液器；1：八通道移液器；2：96通道移液器；3：抓手；4：PCR；5：加热振荡；6：磁吸；7：温控;8:垃圾桶
            {
                case 0:
                    mainSettingTable.SelectedIndex = 0;
                    break;
                case 3:
                    mainSettingTable.SelectedIndex = 1;
                    break;
            }
            this.Visibility = Visibility.Visible;

            // 播放显示动画
            var showAnim = (Storyboard)this.Resources["ShowPopupAnim"];
            showAnim.Begin();
        }

        /// <summary>
        /// 隐藏弹窗
        /// </summary>
        private void Hide()
        {
            var hideAnim = (Storyboard)this.Resources["HidePopupAnim"];
            // 动画结束后隐藏控件
            hideAnim.Completed += (s, e) => this.Visibility = Visibility.Collapsed;
            hideAnim.Begin();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void SettingPopupControl_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 追溯点击的元素，判断是否在PopupBorder内部
            DependencyObject clickedElement = e.OriginalSource as DependencyObject;
            bool isClickInsidePopup = false;

            // 向上遍历视觉树，检查是否包含PopupBorder
            while (clickedElement != null)
            {
                if (clickedElement == PopupBorder)
                {
                    isClickInsidePopup = true;
                    break;
                }
                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            // 点击在弹窗外部 → 关闭弹窗
            if (!isClickInsidePopup)
            {
                Hide();
            }
        }
        #region 移液器模块
        // 移液器控制：吸液
        private async void Aspirate_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtAspirateVol.Text, out int vol) &&
                int.TryParse(txtAspirateSpeed.Text, out int speed))
            {
                _mainWidget.ShowNotification($"执行吸液：{vol}μl，速度：{speed}μl/s", NotificationControl.NotificationType.Info);
                // _pipetteClient.Aspirate(vol, speed); // 示例GRPC调用
                var coordinates = await _mainWidget.AspiratePipeAsync(nowModuleName, vol, speed);
                if (coordinates == 0)
                    _mainWidget.ShowNotification("吸液成功", NotificationControl.NotificationType.Info);

            }
            else
            {
                _mainWidget.ShowNotification("吸液参数格式错误", NotificationControl.NotificationType.Error);
            }
        }
        // 移液器控制：注液
        private async void Dispense_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtDispenseVol.Text, out int vol) &&
                int.TryParse(txtDispenseSpeed.Text, out int speed))
            {
                _mainWidget.ShowNotification($"执行注液：{vol}μl，速度：{speed}μl/s", NotificationControl.NotificationType.Info);
                var coordinates = await _mainWidget.DispensePipeAsync(nowModuleName, vol, speed);
                if (coordinates == 0)
                    _mainWidget.ShowNotification("注液成功", NotificationControl.NotificationType.Info);
            }
            else
            {
                _mainWidget.ShowNotification("注液参数格式错误", NotificationControl.NotificationType.Error);
            }
        }
        // 移液器控制：退头
        private async void EjectTip_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification("执行退头操作", NotificationControl.NotificationType.Info);
            var coordinates = await _mainWidget.BreakPipeAsync(nowModuleName);
            if (coordinates == 0)
                _mainWidget.ShowNotification("退头成功", NotificationControl.NotificationType.Info);
        }

        // 移液器控制：复位
        private async void ResetPipette_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification("移液器复位中", NotificationControl.NotificationType.Info);
            var coordinates = await _mainWidget.ResetPipeAsync(nowModuleName);
            if (coordinates == 0)
                _mainWidget.ShowNotification("复位成功", NotificationControl.NotificationType.Info);
        }

        // 移液器控制：定标

        private async void GetCalibration_Click(object sender, RoutedEventArgs e)
        {
            var calibrationParams = await _mainWidget.GetPipeCalibrationAsync(nowModuleName);

            if (calibrationParams != null)
            {
                // 将获取到的定标参数更新到 UI 控件
                Dispatcher.Invoke(() =>
                {
                    // 回程差（txtCalib0）
                    txtCalib0.Text = calibrationParams.BackDiff.ToString("F2");
                    // 10挡（txtCalib10）
                    txtCalib10.Text = calibrationParams.K10.ToString("F2");
                    // 20挡（txtCalib20）
                    txtCalib20.Text = calibrationParams.K20.ToString("F2");
                    // 50挡（txtCalib50）
                    txtCalib50.Text = calibrationParams.K50.ToString("F2");
                    // 100挡（txtCalib100）
                    txtCalib100.Text = calibrationParams.K100.ToString("F2");
                    // 200挡（txtCalib200）
                    txtCalib200.Text = calibrationParams.K200.ToString("F2");
                    // 300挡（txtCalib300）
                    txtCalib300.Text = calibrationParams.K300.ToString("F2");
                    // 400挡（txtCalib400）
                    txtCalib400.Text = calibrationParams.K400.ToString("F2");
                    // 500挡（txtCalib500）
                    txtCalib500.Text = calibrationParams.K500.ToString("F2");
                    // 600挡（txtCalib600）
                    txtCalib600.Text = calibrationParams.K600.ToString("F2");
                    // 700挡（txtCalib700）
                    txtCalib700.Text = calibrationParams.K700.ToString("F2");
                    // 800挡（txtCalib800）
                    txtCalib800.Text = calibrationParams.K800.ToString("F2");
                    // 900挡（txtCalib900）
                    txtCalib900.Text = calibrationParams.K900.ToString("F2");
                    // 1000挡（txtCalib1000）
                    txtCalib1000.Text = calibrationParams.K1000.ToString("F2");

                    _mainWidget.ShowNotification("定标参数获取成功", NotificationControl.NotificationType.Info);
                });
            }
        }
        //设置定标
        private async void SetCalibration_Click(object sender, RoutedEventArgs e)
        {
            if (TryParseCalibrationParams(out var calibrationParams))
            {
                // 调用设置定标方法
                var result = await _mainWidget.SetPipeCalibrationAsync(calibrationParams);
                if (result == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _mainWidget.ShowNotification("定标参数已保存", NotificationControl.NotificationType.Info);
                    });
                }
            }
        }
        private bool TryParseCalibrationParams(out PipeCalibrationParams @params)
        {
            @params = null;
            try
            {
                // 解析每个 TextBox 的值（保留两位小数）
                @params = new PipeCalibrationParams
                {
                    PipeName = nowModuleName,
                    BackDiff = float.Parse(txtCalib0.Text),       // 回程差
                    K10 = float.Parse(txtCalib10.Text),           // 10挡
                    K20 = float.Parse(txtCalib20.Text),           // 20挡
                    K50 = float.Parse(txtCalib50.Text),           // 50挡
                    K100 = float.Parse(txtCalib100.Text),         // 100挡
                    K200 = float.Parse(txtCalib200.Text),         // 200挡
                    K300 = float.Parse(txtCalib300.Text),         // 300挡
                    K400 = float.Parse(txtCalib400.Text),         // 400挡
                    K500 = float.Parse(txtCalib500.Text),         // 500挡
                    K600 = float.Parse(txtCalib600.Text),         // 600挡
                    K700 = float.Parse(txtCalib700.Text),         // 700挡
                    K800 = float.Parse(txtCalib800.Text),         // 800挡
                    K900 = float.Parse(txtCalib900.Text),         // 900挡
                    K1000 = float.Parse(txtCalib1000.Text)        // 1000挡
                };
                return true;
            }
            catch (FormatException)
            {
                _mainWidget.ShowNotification("定标参数格式错误，请输入有效的数字", NotificationControl.NotificationType.Error);
                return false;
            }
            catch (Exception ex)
            {
                _mainWidget.ShowNotification($"解析定标参数失败: {ex.Message}", NotificationControl.NotificationType.Error);
                return false;
            }
        }


        #endregion
        #region 抓手模块
        //打开抓手
        private void OpenGripper_Click(object sender, RoutedEventArgs e)
        {

        }
        //关闭抓手
        private void CloseGripper_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion
    }
}
