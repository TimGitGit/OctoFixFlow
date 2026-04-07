using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        private int nowModuleId;
        // 加热振荡实时监控：取消令牌源（用于停止线程）
        private CancellationTokenSource _shakerRealTimeCts;
        // 标记是否正在实时获取加热振荡数据（避免重复开启线程）
        private bool _isShakerRealTimeMonitoring;
        public SettingPopupControl(MainWidget mainWidget)
        {
            InitializeComponent();
            _mainWidget = mainWidget;
        }
        // 辅助方法：创建详情行（Label + 输入控件）
        private StackPanel CreateDetailRow(string labelText, UIElement inputControl)
        {
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5),
                Children =
        {
            new TextBlock { Text = labelText ,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14},
            inputControl
        }
            };
        }
        // 创建液体参数显示行（只读）
        private Grid CreateParamRow(string label, string propertyName, FlowStep step)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto,
                MinWidth = 175
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });

            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 13,
                Margin = new Thickness(0, 2, 5, 2),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelText, 0);
            grid.Children.Add(labelText);

            var paramValueText = new TextBox
            {
                Style = (Style)FindResource("InputTextBoxStyle"),
                FontSize = 13,
                Margin = new Thickness(5, 2, 0, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            paramValueText.SetBinding(TextBox.TextProperty, new Binding
            {
                Source = step,
                //Path = new PropertyPath($"SelectedLiquid.{propertyName}"),
                Path = new PropertyPath(propertyName),
                Mode = BindingMode.TwoWay, // 双向绑定：界面修改 → 同步到对象，对象变更 → 同步到界面
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus, // 失去焦点时更新数据，避免输入过程中报错
                StringFormat = "{0:F2}" // 保留两位小数，与导出格式一致
            });
            //paramValueText.SetBinding(TextBlock.TextProperty, new Binding
            //{
            //    Source = step,
            //    Path = new PropertyPath($"SelectedLiquid.{propertyName}"),
            //    StringFormat = "{0:F2}"
            //});
            Grid.SetColumn(paramValueText, 1);
            grid.Children.Add(paramValueText);

            return grid;
        }

        /// <summary>
        /// 显示弹窗：传入标题+设置内容UI
        /// </summary>
        public void Show(int moduleType, string moduleName, int nowid)
        {
            nowModuleName = moduleName;
            settingTitle.Text = nowModuleName;
            nowModuleId = nowid;
            switch (moduleType)//0：单通道移液器；1：八通道移液器；2：96通道移液器；3：抓手；4：PCR；5：加热振荡；6：磁吸；7：温控;8:垃圾桶
            {
                case -1:
                    mainSettingTable.SelectedIndex = 3;
                    break;
                case 0:
                    mainSettingTable.SelectedIndex = 0;
                    break;
                case 3:
                    mainSettingTable.SelectedIndex = 1;
                    break;
                case 4://PCR
                    mainSettingTable.SelectedIndex = 7;
                    break;
                case 5://加热振荡
                    StopShakerRealTimeMonitor();
                    // 开启加热振荡温度&转速实时监控（传入模块ID nowModuleId）
                    _ = StartShakerRealTimeMonitor(5, nowModuleId);
                    mainSettingTable.SelectedIndex = 5;
                    break;
                case 6:
                    mainSettingTable.SelectedIndex = 4;
                    break;
                case 7:
                    StopShakerRealTimeMonitor();
                    // 开启加热振荡温度&转速实时监控（传入模块ID nowModuleId）
                    _ = StartShakerRealTimeMonitor(7, nowModuleId);
                    mainSettingTable.SelectedIndex = 6;
                    break;
            }
            this.Visibility = Visibility.Visible;

            // 播放显示动画
            var showAnim = (Storyboard)this.Resources["ShowPopupAnim"];
            showAnim.Begin();
        }
        // 显示步骤详情（后续可扩展为不同步骤类型的布局）
        // 显示步骤详情（修改后）
        public void setStepDetail(FlowStep step)
        {
            // 清空现有详情
            StepDetailPanel.Children.Clear();
            var res = ResourceHelper.Instance;
            string stepTypeText = step.Type switch
            {
                "Wait" => res.FlowStepWaitContent,
                "Aspirate" => res.WindowActionAspirate,
                "Dispense" => res.WindowActionDispense,
                "TipOn" => res.WindowActionTipOn,
                "TipOff" => res.WindowActionTipOff,
                "Shake" => res.WindowActionShake,
                "Magnetic" => res.WindowActionMagnetic,
                "Temp Ctrl" => res.WindowActionTemperature,
                "PCR" => res.WindowActionPCR,
                "Transfer" => res.WindowActionTransfer,
                "Mix" => res.WindowActionMix,
                "Loop" => res.WindowActionLoop,
                _ => step.Type // 未知类型默认显示原始值
            };
            // 添加通用详情标题
            StepDetailPanel.Children.Add(new TextBlock
            {
                Text = $"{stepTypeText} {res.StepDetailDetails}",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });
            if (step.Type == "Wait")
            {
                // 等待时间输入（秒）
                var waitTimeTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                waitTimeTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("WaitTime"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailWaitTime, waitTimeTextBox));
                return;
            }
            else if (step.Type == "Shake")//加热振荡
            {
                // 振荡位置
                var actualPlatePositions = AppGlobalConfig.Instance.PlateModuleMap
                    .Values
                    .Where(module =>
                        !string.IsNullOrEmpty(module.PlatePosition) &&
                        int.TryParse(module.PlatePosition, out _) &&
                        module.Type == 5)
                    .Select(module => int.Parse(module.PlatePosition))
                    .Distinct()
                    .OrderBy(num => num)
                    .Select(num => $"P{num}")
                    .ToList();
                var posiShakeCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = actualPlatePositions,
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var posiShakeBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("Position"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                posiShakeCombo.SetBinding(ComboBox.SelectedItemProperty, posiShakeBinding);
                // 位置下拉框选择变更时校验耗材类型
                posiShakeCombo.SelectionChanged += (s, e) =>
                {
                    if (posiShakeCombo.SelectedItem is string newPosition)
                    {
                        string selectedPlatePosition = newPosition.Replace("P", "");
                        var matchedModule = AppGlobalConfig.Instance.PlateModuleMap
        .Values
        .FirstOrDefault(module =>
            module.Type == 5 &&
            module.PlatePosition == selectedPlatePosition);
                        step.ModuleName = matchedModule.Name;
                    }
                };
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailOperationPosition, posiShakeCombo));
                // 振荡时间输入（秒）
                var shakeTimeTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                shakeTimeTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("WaitTime"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailWaitTime, shakeTimeTextBox));
                // 振荡转速输入
                var shakeRPMTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                shakeRPMTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("ShakeRPM"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailShakeSpeed, shakeRPMTextBox));
                // 振荡温度输入
                var shakeTempTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                shakeTempTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("ShakeTemp"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailShakeTemp, shakeTempTextBox));
                return;
            }
            else if (step.Type == "Magnetic")//磁吸
            {
                // 磁吸位置
                var actualPlatePositions = AppGlobalConfig.Instance.PlateModuleMap
                    .Values
                    .Where(module =>
                        !string.IsNullOrEmpty(module.PlatePosition) &&
                        int.TryParse(module.PlatePosition, out _) &&
                        module.Type == 6)
                    .Select(module => int.Parse(module.PlatePosition))
                    .Distinct()
                    .OrderBy(num => num)
                    .Select(num => $"P{num}")
                    .ToList();
                var posiMagneticCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = actualPlatePositions,
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var posiMagneticBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("Position"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                posiMagneticCombo.SetBinding(ComboBox.SelectedItemProperty, posiMagneticBinding);
                posiMagneticCombo.SelectionChanged += (s, e) =>
                {
                    if (posiMagneticCombo.SelectedItem is string newPosition)
                    {
                        string selectedPlatePosition = newPosition.Replace("P", "");
                        var matchedModule = AppGlobalConfig.Instance.PlateModuleMap
                        .Values
                        .FirstOrDefault(module =>
                        module.Type == 6 &&
                        module.PlatePosition == selectedPlatePosition);
                        step.ModuleName = matchedModule.Name;
                    }
                };
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailOperationPosition, posiMagneticCombo));
                // 上升
                var magneticUpCheckBox = new CheckBox
                {
                    Content = res.StepDetailMagnetUp,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14,
                    Margin = new Thickness(5, 0, 20, 0)
                };
                magneticUpCheckBox.SetBinding(CheckBox.IsCheckedProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("IsMagnetUp"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

                // 下降
                var magneticDownCheckBox = new CheckBox
                {
                    Content = res.StepDetailMagnetDown,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                magneticDownCheckBox.SetBinding(CheckBox.IsCheckedProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("IsMagnetDown"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                magneticUpCheckBox.Checked += (s, e) =>
                {
                    step.IsMagnetUp = true;
                    step.IsMagnetDown = false;

                };
                magneticDownCheckBox.Checked += (s, e) =>
                {
                    step.IsMagnetDown = true;

                    step.IsMagnetUp = false;
                };
                var magnetDirectionPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                magnetDirectionPanel.Children.Add(magneticUpCheckBox);
                magnetDirectionPanel.Children.Add(magneticDownCheckBox);
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailMagnetLiftDrop, magnetDirectionPanel));
                return;
            }
            else if (step.Type == "Temp Ctrl")//温控
            {
                // 温控位置
                var actualPlatePositions = AppGlobalConfig.Instance.PlateModuleMap
                    .Values
                    .Where(module =>
                        !string.IsNullOrEmpty(module.PlatePosition) &&
                        int.TryParse(module.PlatePosition, out _) &&
                        module.Type == 7)
                    .Select(module => int.Parse(module.PlatePosition))
                    .Distinct()
                    .OrderBy(num => num)
                    .Select(num => $"P{num}")
                    .ToList();
                var posiTempCtrlCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = actualPlatePositions,
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var posiTempCtrlBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("Position"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                posiTempCtrlCombo.SetBinding(ComboBox.SelectedItemProperty, posiTempCtrlBinding);
                posiTempCtrlCombo.SelectionChanged += (s, e) =>
                {
                    if (posiTempCtrlCombo.SelectedItem is string newPosition)
                    {
                        string selectedPlatePosition = newPosition.Replace("P", "");
                        var matchedModule = AppGlobalConfig.Instance.PlateModuleMap
                        .Values
                        .FirstOrDefault(module =>
                        module.Type == 7 &&
                        module.PlatePosition == selectedPlatePosition);
                        step.ModuleName = matchedModule.Name;
                    }
                };
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailOperationPosition, posiTempCtrlCombo));
                // 温控温度输入
                var tempCtrlTempTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                tempCtrlTempTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("TempCtrlTemp"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailShakeTemp, tempCtrlTempTextBox));
                // 打开
                var tempOpenCheckBox = new CheckBox
                {
                    Content = res.SettingManualStartTemperature,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14,
                    Margin = new Thickness(5, 0, 20, 0)
                };
                tempOpenCheckBox.SetBinding(CheckBox.IsCheckedProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("IsTempCtrlOpen"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

                // 关闭
                var tempCloseCheckBox = new CheckBox
                {
                    Content = res.SettingManualStopTemperature,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                tempCloseCheckBox.SetBinding(CheckBox.IsCheckedProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("IsTempCtrlClose"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                tempOpenCheckBox.Checked += (s, e) =>
                {
                    step.IsTempCtrlOpen = true;
                    step.IsTempCtrlClose = false;

                };
                tempCloseCheckBox.Checked += (s, e) =>
                {

                    step.IsTempCtrlOpen = false;
                    step.IsTempCtrlClose = true;

                };
                var magnetDirectionPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                magnetDirectionPanel.Children.Add(tempOpenCheckBox);
                magnetDirectionPanel.Children.Add(tempCloseCheckBox);
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailTempCtrlAction, magnetDirectionPanel));
                return;
            }
            else if (step.Type == "PCR")//PCR
            {
                // 动作
                var posFromCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { res.SettingManualPCRStart, res.SettingManualPCRStop, res.SettingManualPCROpen, res.SettingManualPCRClose, res.SettingManualPCRWaitRun },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var posFromBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("PcrStep"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                posFromCombo.SetBinding(ComboBox.SelectedItemProperty, posFromBinding);

                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailPCRprocedure, posFromCombo));
                var PCRScriptTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                PCRScriptTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("PcrScriptAdress"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                PCRScriptTextBox.IsReadOnly = true;
                StepDetailPanel.Children.Add(CreateDetailRow(res.SettingManualPCRScriptRun, PCRScriptTextBox));
                var selectDatFileBtn = new Button
                {
                    Style = (Style)FindResource("ActionButtonStyle"), // 复用原有样式，也可替换为按钮专属样式
                    Content = "Select", // 按钮显示文本
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                selectDatFileBtn.Click += async (sender, e) =>
                {
                    var openFileDialog = new Microsoft.Win32.OpenFileDialog();

                    // 筛选条件：仅显示.dat文件
                    openFileDialog.Filter = "Dat|*.dat";
                    // 设置默认文件类型为.dat
                    openFileDialog.DefaultExt = ".dat";

                    // 显示文件选择框，判断用户是否确认选择
                    if (openFileDialog.ShowDialog() == true)
                    {
                        string fileContent = await File.ReadAllTextAsync(openFileDialog.FileName); // 显式指定编码，避免乱码

                        step.PcrScriptAdress = fileContent;
                    }
                };
                StepDetailPanel.Children.Add(CreateDetailRow("Select File", selectDatFileBtn)); // 第一个参数可替换为res中的对应文本（如res.SettingManualSelectDatFile）
                return;
            }
            else if (step.Type == "Transfer")//抓手
            {
                // 起始板位
                var posFromCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12", "P13", "P14", "P15" },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var posFromBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("FromPos"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                posFromCombo.SetBinding(ComboBox.SelectedItemProperty, posFromBinding);

                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailTransferFrom, posFromCombo));
                // 终止板位
                var posToCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12", "P13", "P14", "P15" },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var posToBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("ToPos"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                posToCombo.SetBinding(ComboBox.SelectedItemProperty, posToBinding);

                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailTransferTo, posToCombo));
                // 抓板下压距离
                var TransferPositionTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                TransferPositionTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("TransferPosition"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailTransferPosition, TransferPositionTextBox));
                return;
            }
            else if (step.Type == "Loop")//循环
            {
                // 起始板位
                var posFromCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12", "P13", "P14", "P15" },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var posFromBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("FromPos"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                posFromCombo.SetBinding(ComboBox.SelectedItemProperty, posFromBinding);

                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailTransferFrom, posFromCombo));
                // 终止板位
                var posToCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12", "P13", "P14", "P15" },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var posToBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("ToPos"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                posToCombo.SetBinding(ComboBox.SelectedItemProperty, posToBinding);

                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailTransferTo, posToCombo));
                // 抓板下压距离
                var TransferPositionTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                TransferPositionTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("TransferPosition"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailTransferPosition, TransferPositionTextBox));
                return;
            }
            // -------------------------- 通用控件（吸液/注液/取头/退头） --------------------------
            // 创建位置下拉框并绑定
            var positionCombo = new ComboBox
            {
                Style = (Style)FindResource("InputComboBoxStyle"),
                ItemsSource = new List<string> { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12" },
                Width = 140,
                VerticalAlignment = VerticalAlignment.Center
            };
            // 绑定到step.Position（双向）
            var positionBinding = new Binding
            {
                Source = step,
                Path = new PropertyPath("Position"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            positionCombo.SetBinding(ComboBox.SelectedItemProperty, positionBinding);
            // 定义专用孔位选择画布
            ConsumableCanvas wellSelectionCanvas = new ConsumableCanvas
            {
                Height = 220,
                Width = 310,
                Margin = new Thickness(0, 5, 0, 5),
                Background = Brushes.AliceBlue,
                IsInteractive = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                PlateId = step.Position.Replace("P", "")
            };
            // 绑定画布的选中列变更事件
            wellSelectionCanvas.SelectedColumnsChanged += (plateId, columnText) =>
            {
                step.WellPosition = columnText;
                var selectedCells = _mainWidget._selectedCellsFromText(columnText);
                step.SelectedCells = string.Join(";", selectedCells.Select(c => $"{c.Row},{c.Col}"));

            };
            // 耗材名称显示控件
            TextBlock consumableNameText = new TextBlock
            {
                FontSize = 14,
                Margin = new Thickness(5, 5, 0, 5),
                Foreground = Brushes.DarkSlateGray,
                Text = step.ConsName
            };
            // 位置下拉框选择变更时校验耗材类型
            positionCombo.SelectionChanged += (s, e) =>
            {
                if (positionCombo.SelectedItem is string newPosition)
                {
                    // 更新当前选中的板位ID
                    _mainWidget._currentSelectedPlateId = newPosition.Replace("P", "");
                    wellSelectionCanvas.PlateId = _mainWidget._currentSelectedPlateId;
                    wellSelectionCanvas.ClearSelection();                    // 清空之前的选择
                    wellSelectionCanvas.IsInteractive = false;

                    // 绑定画布的耗材数据（从板位映射中获取）
                    if (_mainWidget._plateConsumableMap.TryGetValue(_mainWidget._currentSelectedPlateId, out var consumable))
                    {
                        // 显示当前耗材名称
                        step.ConsName = string.Format(res.StepDetailCurrentCons, consumable.Name);
                        consumableNameText.Text = step.ConsName;
                        wellSelectionCanvas.ConsData = consumable.Settings;  // 关联当前板位的耗材数据
                        int consType = consumable.Settings.type;
                        if ((step.Type == "Aspirate" || step.Type == "Dispense" || step.Type == "Mix"))
                        {
                            // 吸液/注液允许：0（微孔板）、1（储液槽）
                            if (consType == 0 || consType == 1)
                            {
                                wellSelectionCanvas.IsInteractive = true;
                            }
                            else
                            {
                                _mainWidget.ShowNotification(res.StepDetailAspDispConsTip, NotificationControl.NotificationType.Warn); // 替换通知
                            }
                        }
                        else if ((step.Type == "TipOn" || step.Type == "TipOff"))
                        {
                            // 取头/退头允许：2（TIP盒）
                            if (consType == 2 || consType == 3)
                            {
                                wellSelectionCanvas.IsInteractive = true;
                            }
                            else
                            {
                                _mainWidget.ShowNotification(res.StepDetailTipOnOffConsTip, NotificationControl.NotificationType.Warn); // 替换通知
                            }
                        }
                    }
                    else
                    {
                        wellSelectionCanvas.ConsData = null;  // 无耗材时清空
                        step.WellPosition = "";
                        step.SelectedColumns = "";
                        step.ConsName = "";
                        consumableNameText.Text = step.ConsName;
                    }
                }
            };
            StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailOperationPosition, positionCombo));
            //移液器选择
            var availablePipettes = AppGlobalConfig.Instance.PlateModuleMap
    .Values
    .Where(module =>
        (module.Name == "pipette_1" || module.Name == "pipette_2")
    )
    .Select(module => module.Name)
    .ToList();
            var pipetteCombo = new ComboBox
            {
                Style = (Style)FindResource("InputComboBoxStyle"),
                ItemsSource = availablePipettes, // 仅显示已配置的移液器
                Width = 140,
                VerticalAlignment = VerticalAlignment.Center,
                SelectedItem = string.IsNullOrEmpty(step.SelectedPipetteName)
           ? availablePipettes.FirstOrDefault() // 默认选中第一个
           : step.SelectedPipetteName
            };
            pipetteCombo.SetBinding(ComboBox.SelectedItemProperty, new Binding
            {
                Source = step,
                Path = new PropertyPath("SelectedPipetteName"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            pipetteCombo.SelectionChanged += (s, e) =>
            {

                if (pipetteCombo.SelectedItem is string pipetteName && wellSelectionCanvas != null)
                {
                    var selectedPipette = AppGlobalConfig.Instance.PlateModuleMap
                        .Values
                        .FirstOrDefault(module => module.Name == pipetteName);

                    if (selectedPipette != null)
                    {
                        wellSelectionCanvas.CurrentSelectionMode = selectedPipette.Type switch
                        {
                            0 => CanvasSelectionMode.SingleCell,    // 单通道：选单个单元格
                            1 => CanvasSelectionMode.EntireColumn,  // 八通道：选整列
                            2 => CanvasSelectionMode.EntirePlate,   // 96通道：选整板（新增）
                            _ => CanvasSelectionMode.SingleCell     // 默认值：兜底防异常
                        };
                        wellSelectionCanvas.ClearSelection();
                    }
                }
            };
            StepDetailPanel.Children.Add(CreateDetailRow(
res.StepDetailSelectedPipette,
pipetteCombo));
            if (wellSelectionCanvas != null && pipetteCombo.SelectedItem is string initPipetteName)
            {
                // 根据名称查找对应的ModuleDatas对象
                var initPipette = AppGlobalConfig.Instance.PlateModuleMap
                    .Values
                    .FirstOrDefault(module => module.Name == initPipetteName);

                if (initPipette != null)
                {
                    wellSelectionCanvas.CurrentSelectionMode = initPipette.Type switch
                    {
                        0 => CanvasSelectionMode.SingleCell,    // 单通道：选单个单元格
                        1 => CanvasSelectionMode.EntireColumn,  // 八通道：选整列
                        2 => CanvasSelectionMode.EntirePlate,   // 96通道：选整板（新增）
                        _ => CanvasSelectionMode.SingleCell     // 默认值：兜底防异常
                    };
                }
                // 兜底：若未找到，默认设为单通道模式
                else
                {
                    wellSelectionCanvas.CurrentSelectionMode = CanvasSelectionMode.SingleCell;
                }
            }
            // 孔位选择（所有步骤通用）
            var wellPositionTextBox = new TextBox
            {
                Style = (Style)FindResource("InputTextBoxStyle"),
                Width = 140,
                VerticalAlignment = VerticalAlignment.Center
            };
            wellPositionTextBox.SetBinding(TextBox.TextProperty, new Binding
            {
                Source = step,
                Path = new PropertyPath("WellPosition"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            wellPositionTextBox.IsReadOnly = true;
            StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailWellPosition, wellPositionTextBox));

            StepDetailPanel.Children.Add(new TextBlock
            {
                Text = res.StepDetailWellSelectionArea, // “孔位选择区：”/“Well Position Selection Area:”
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 5, 0, 2)
            });
            StepDetailPanel.Children.Add(consumableNameText);

            StepDetailPanel.Children.Add(wellSelectionCanvas);

            // 移液器选择、体积输入（吸液/注液特有）
            if (step.Type == "Aspirate" || step.Type == "Dispense" || step.Type == "Mix")
            {
                if (step.Type == "Aspirate")
                {
                    // 创建体积输入框并绑定
                    var volumeTextBox = new TextBox
                    {
                        Style = (Style)FindResource("InputTextBoxStyle"),
                        Width = 150
                    };

                    var volumeBinding = new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("Volume"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                    };

                    volumeTextBox.SetBinding(TextBox.TextProperty, volumeBinding);

                    StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailVolume, volumeTextBox));
                }
                else if (step.Type == "Dispense")
                {
                    // 创建体积输入框并绑定
                    var volumeTextBox = new TextBox
                    {
                        Style = (Style)FindResource("InputTextBoxStyle"),
                        Width = 150
                    };

                    var volumeBinding = new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("Volume"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                    };

                    volumeTextBox.SetBinding(TextBox.TextProperty, volumeBinding);

                    StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailVolume, volumeTextBox));

                    // 创建push体积输入框并绑定
                    var volumePushTextBox = new TextBox
                    {
                        Style = (Style)FindResource("InputTextBoxStyle"),
                        Width = 150
                    };

                    var volumePushBinding = new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("PushOutvolume"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                    };

                    volumePushTextBox.SetBinding(TextBox.TextProperty, volumePushBinding);

                    StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailPushVolume, volumePushTextBox));
                }
                else if (step.Type == "Mix")
                {
                    // --------------- 1. 混合容量 + 混合次数 同一行 ---------------
                    var volumeMixAllVolumeTextBox = new TextBox
                    {
                        Style = (Style)FindResource("InputTextBoxStyle"),
                        Width = 150
                    };

                    volumeMixAllVolumeTextBox.SetBinding(TextBox.ToolTipProperty, new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("SelectedPipetteMaxVolume"),
                        Mode = BindingMode.OneWay
                    });

                    var volumeMixAllVolumeBinding = new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("MixVolume"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                    };

                    volumeMixAllVolumeTextBox.SetBinding(TextBox.TextProperty, volumeMixAllVolumeBinding);

                    StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailMixVolume, volumeMixAllVolumeTextBox));
                    var volumeMixAllCountTextBox = new TextBox
                    {
                        Style = (Style)FindResource("InputTextBoxStyle"),
                        Width = 150
                    };

                    volumeMixAllCountTextBox.SetBinding(TextBox.ToolTipProperty, new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("SelectedPipetteMaxVolume"),
                        Mode = BindingMode.OneWay
                    });

                    var volumeMixAllCountBinding = new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("MixCount"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };

                    volumeMixAllCountTextBox.SetBinding(TextBox.TextProperty, volumeMixAllCountBinding);

                    StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailMixCount, volumeMixAllCountTextBox));

                    // 创建push体积输入框并绑定
                    var volumePushTextBox = new TextBox
                    {
                        Style = (Style)FindResource("InputTextBoxStyle"),
                        Width = 150
                    };

                    var volumePushBinding = new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("PushOutvolume"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                    };

                    volumePushTextBox.SetBinding(TextBox.TextProperty, volumePushBinding);

                    StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailPushVolume, volumePushTextBox));
                }

                // #################### 新增：液体参数选择与显示 ####################
                var liquidHeaderRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center
                };

                // 液体参数标题
                liquidHeaderRow.Children.Add(new TextBlock
                {
                    Text = res.StepDetailLiquidParams,
                    Width = 140,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center
                });

                // 液体选择下拉框（无标签，直接放在标题右侧）
                var liquidCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = _mainWidget.Liquids,
                    DisplayMemberPath = "name",
                    Width = 100,
                    Margin = new Thickness(5, 0, 0, 0), // 与标题保持间距
                    VerticalAlignment = VerticalAlignment.Center
                };
                liquidCombo.SetBinding(ComboBox.SelectedItemProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("SelectedLiquid"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                liquidCombo.SelectionChanged += (s, e) =>
                {
                    if (liquidCombo.SelectedItem is LiquidSettings selectedLiquid)
                    {
                        // 将选中液体的参数同步到FlowStep对象
                        step.LiquidAisAirB = selectedLiquid.aisAirB;
                        step.LiquidAisAirA = selectedLiquid.aisAirA;
                        step.LiquidAisSpeed = selectedLiquid.aisSpeed;
                        step.LiquidAisDelay = selectedLiquid.aisDelay;
                        step.LiquidAisDistance = selectedLiquid.aisDistance;
                        step.LiquidDisAirB = selectedLiquid.disAirB;
                        step.LiquidDisAirA = selectedLiquid.disAirA;
                        step.LiquidDisSpeed = selectedLiquid.disSpeed;
                        step.LiquidDisDelay = selectedLiquid.disDelay;
                        step.LiquidDisDistance = selectedLiquid.disDistance;
                    }
                };
                liquidHeaderRow.Children.Add(liquidCombo);

                StepDetailPanel.Children.Add(liquidHeaderRow);

                // 液体参数总容器（与下拉框左对齐，保持同一列）
                StackPanel liquidParamsContainer = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch // 拉伸填满父容器宽度
                };

                // 吸液参数组
                liquidParamsContainer.Children.Add(new TextBlock
                {
                    Text = res.StepDetailAspirationParams,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12
                });

                // 吸液参数面板（2+2+1布局）
                StackPanel aspirateParams = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch // 确保参数面板能扩展宽度
                };

                // 第一行：Air Aspiration Before Aspiration
                var aspirateRow1 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                aspirateRow1.RowDefinitions.Add(new RowDefinition());
                var paramControl1 = CreateParamRow(res.StepDetailAspAirB, nameof(FlowStep.LiquidAisAirB), step);
                Grid.SetRow(paramControl1, 0);
                aspirateRow1.Children.Add(paramControl1);
                aspirateParams.Children.Add(aspirateRow1);

                // 第二行：Air Aspiration After Aspiration
                var aspirateRow2 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                aspirateRow2.RowDefinitions.Add(new RowDefinition());
                var paramControl2 = CreateParamRow(res.StepDetailAspAirA, nameof(FlowStep.LiquidAisAirA), step);
                Grid.SetRow(paramControl2, 0);
                aspirateRow2.Children.Add(paramControl2);
                aspirateParams.Children.Add(aspirateRow2);
                // 第三行：Aspiration Speed
                var aspirateRow3 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                aspirateRow3.RowDefinitions.Add(new RowDefinition());
                var paramControl3 = CreateParamRow(res.StepDetailAspSpeed, nameof(FlowStep.LiquidAisSpeed), step);
                Grid.SetRow(paramControl3, 0);
                aspirateRow3.Children.Add(paramControl3);
                aspirateParams.Children.Add(aspirateRow3);
                // 第四行：Aspiration Delay
                var aspirateRow4 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                aspirateRow4.RowDefinitions.Add(new RowDefinition());
                var paramControl4 = CreateParamRow(res.StepDetailAspDelay, nameof(FlowStep.LiquidAisDelay), step);
                Grid.SetRow(paramControl4, 0);
                aspirateRow4.Children.Add(paramControl4);
                aspirateParams.Children.Add(aspirateRow4);
                // 第五行：Aspiration Distance
                var aspirateRow5 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                aspirateRow5.RowDefinitions.Add(new RowDefinition());
                var paramControl5 = CreateParamRow(res.StepDetailAspDist, nameof(FlowStep.LiquidAisDistance), step);
                Grid.SetRow(paramControl5, 0);
                aspirateRow5.Children.Add(paramControl5);
                aspirateParams.Children.Add(aspirateRow5);

                liquidParamsContainer.Children.Add(aspirateParams);

                // 注液参数组
                liquidParamsContainer.Children.Add(new TextBlock
                {
                    Text = res.StepDetailDispensingParams,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 5, 0, 3),
                    FontSize = 12
                });

                // 注液参数面板（2+2+1布局）
                StackPanel dispenseParams = new StackPanel();

                // 第一行：2个参数
                // 第一行：注液前吸空气
                var dispenseRow1 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                dispenseRow1.RowDefinitions.Add(new RowDefinition());
                var paramDisControl1 = CreateParamRow(res.StepDetailDispAirB, nameof(FlowStep.LiquidDisAirB), step);
                Grid.SetRow(paramDisControl1, 0);
                dispenseRow1.Children.Add(paramDisControl1);
                dispenseParams.Children.Add(dispenseRow1);
                // 第二行：注液后吸空气
                var dispenseRow2 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                dispenseRow2.RowDefinitions.Add(new RowDefinition());
                var paramDisControl2 = CreateParamRow(res.StepDetailDispAirA, nameof(FlowStep.LiquidDisAirA), step);
                Grid.SetRow(paramDisControl2, 0);
                dispenseRow2.Children.Add(paramDisControl2);
                dispenseParams.Children.Add(dispenseRow2);
                // 第二行：2个参数
                var dispenseRow3 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                dispenseRow3.RowDefinitions.Add(new RowDefinition());
                var paramDisControl3 = CreateParamRow(res.StepDetailDispSpeed, nameof(FlowStep.LiquidDisSpeed), step);
                Grid.SetRow(paramDisControl3, 0);
                dispenseRow3.Children.Add(paramDisControl3);
                dispenseParams.Children.Add(dispenseRow3);
                var dispenseRow4 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                dispenseRow4.RowDefinitions.Add(new RowDefinition());
                var paramDisControl4 = CreateParamRow(res.StepDetailDispDelay, nameof(FlowStep.LiquidDisDelay), step);
                Grid.SetRow(paramDisControl4, 0);
                dispenseRow4.Children.Add(paramDisControl4);
                dispenseParams.Children.Add(dispenseRow4);
                // 第三行：1个参数
                var dispenseRow5 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                dispenseRow5.RowDefinitions.Add(new RowDefinition());
                var paramDisControl5 = CreateParamRow(res.StepDetailDispDist, nameof(FlowStep.LiquidDisDistance), step);
                Grid.SetRow(paramDisControl5, 0);
                dispenseRow5.Children.Add(paramDisControl5);
                dispenseParams.Children.Add(dispenseRow5);

                liquidParamsContainer.Children.Add(dispenseParams);

                StepDetailPanel.Children.Add(liquidParamsContainer);
            }

            // 初始化画布数据（首次加载时）
            _mainWidget._currentSelectedPlateId = step.Position.Replace("P", "");
            wellSelectionCanvas.PlateId = _mainWidget._currentSelectedPlateId;
            if (_mainWidget._plateConsumableMap.TryGetValue(_mainWidget._currentSelectedPlateId, out var initConsumable))
            {
                wellSelectionCanvas.ConsData = initConsumable.Settings;
            }
            if (!string.IsNullOrEmpty(step.SelectedCells))
            {
                var cells = step.SelectedCells.Split(';')
                    .Select(s => s.Split(','))
                    .Where(parts => parts.Length == 2 &&
                                    int.TryParse(parts[0], out int row) &&
                                    int.TryParse(parts[1], out int col))
                    .Select(parts => (Row: int.Parse(parts[0]), Col: int.Parse(parts[1])))
                    .ToList();

                wellSelectionCanvas.SetSelectedCells(cells);
            }
        }

        /// <summary>
        /// 隐藏弹窗
        /// </summary>
        private void Hide()
        {
            StopShakerRealTimeMonitor();
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
            if (float.TryParse(txtAspirateVol.Text, out float vol) &&
                float.TryParse(txtAspirateSpeed.Text, out float speed))
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualAspirate, NotificationControl.NotificationType.Info);

                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from pipe import Pipe");
                pythonCode.AppendLine($"Pipe.aspirate({nowModuleId},{vol},{speed})");
                var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
                var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
                if (pyFlag != null)
                {
                    if (pyFlag.Result == "succeed")
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                    }
                    else
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                    }
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
                }

            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
            }
        }
        // 移液器控制：注液
        private async void Dispense_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtDispenseVol.Text, out float vol) &&
                float.TryParse(txtDispenseSpeed.Text, out float speed))
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualDispense, NotificationControl.NotificationType.Info);

                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from pipe import Pipe");
                pythonCode.AppendLine($"Pipe.dispense({nowModuleId},{vol},{speed})");
                var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
                var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
                if (pyFlag != null)
                {
                    if (pyFlag.Result == "succeed")
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                    }
                    else
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                    }
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
            }
        }
        // 移液器控制：退头
        private async void EjectTip_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualEjectTip, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from pipe import Pipe");
            pythonCode.AppendLine($"Pipe.eject({nowModuleId})");
            var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
            if (pyFlag != null)
            {
                if (pyFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }

        // 移液器控制：复位
        private async void ResetPipette_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualReset, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from pipe import Pipe");
            pythonCode.AppendLine($"Pipe.reset({nowModuleId})");
            var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
            if (pyFlag != null)
            {
                if (pyFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }

        // 移液器控制：定标

        private async void GetCalibration_Click(object sender, RoutedEventArgs e)
        {

            _mainWidget.ShowNotification(_mainWidget._res.SettingManualGetCalibration, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from pipe import Pipe");
            pythonCode.AppendLine($"debug(Pipe.get_cali({nowModuleId}))");
            var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
            if (pyFlag != null)
            {
                if (pyFlag.Result == "succeed")
                {
                    string pythonDictStr = pyFlag.Data.ToString();
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                    string standardJsonStr = pythonDictStr
                    .Replace("'", "\"")          // 单引号替换为双引号（JSON要求双引号）
                    .Replace("nan", "0.0");
                    standardJsonStr = standardJsonStr.Trim('"');

                    PipeCalibrationParams calibrationParams = JsonConvert.DeserializeObject<PipeCalibrationParams>(standardJsonStr);
                    // 回程差（txtCalib0）
                    txtCalib0.Text = calibrationParams.backdiff.ToString("F2");
                    // 10挡（txtCalib10）
                    txtCalib10.Text = calibrationParams.k_10.ToString("F2");
                    // 20挡（txtCalib20）
                    txtCalib20.Text = calibrationParams.k_20.ToString("F2");
                    // 50挡（txtCalib50）
                    txtCalib50.Text = calibrationParams.k_50.ToString("F2");
                    // 100挡（txtCalib100）
                    txtCalib100.Text = calibrationParams.k_100.ToString("F2");
                    // 200挡（txtCalib200）
                    txtCalib200.Text = calibrationParams.k_200.ToString("F2");
                    // 300挡（txtCalib300）
                    txtCalib300.Text = calibrationParams.k_300.ToString("F2");
                    // 400挡（txtCalib400）
                    txtCalib400.Text = calibrationParams.k_400.ToString("F2");
                    // 500挡（txtCalib500）
                    txtCalib500.Text = calibrationParams.k_500.ToString("F2");
                    // 600挡（txtCalib600）
                    txtCalib600.Text = calibrationParams.k_600.ToString("F2");
                    // 700挡（txtCalib700）
                    txtCalib700.Text = calibrationParams.k_700.ToString("F2");
                    // 800挡（txtCalib800）
                    txtCalib800.Text = calibrationParams.k_800.ToString("F2");
                    // 900挡（txtCalib900）
                    txtCalib900.Text = calibrationParams.k_900.ToString("F2");
                    // 1000挡（txtCalib1000）
                    txtCalib1000.Text = calibrationParams.k_1000.ToString("F2");
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }
        //设置定标
        private async void SetCalibration_Click(object sender, RoutedEventArgs e)
        {
            if (TryParseCalibrationParams(out var calibrationParams))
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualSetCalibration, NotificationControl.NotificationType.Info);


                string calibrationJson = JsonConvert.SerializeObject(
    calibrationParams,
    new JsonSerializerSettings
    {
        Formatting = Formatting.None,  // 紧凑格式，解决换行导致的字符串截断
        NullValueHandling = NullValueHandling.Ignore,
        FloatParseHandling = FloatParseHandling.Double
    });


                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from pipe import Pipe");
                pythonCode.AppendLine($"debug(Pipe.set_cali({nowModuleId}, \"\"\"{calibrationJson}\"\"\"))");
                var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
                var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
                if (pyFlag != null)
                {
                    if (pyFlag.Result == "succeed")
                    {

                        _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);

                    }
                    else
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                    }
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
                }


                // 调用设置定标方法
                //var result = await _mainWidget.SetPipeCalibrationAsync(calibrationParams);
                //if (result == 0)
                //{
                //    Dispatcher.Invoke(() =>
                //    {
                //        _mainWidget.ShowNotification("定标参数已保存", NotificationControl.NotificationType.Info);
                //    });
                //}
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
                    backdiff = float.Parse(txtCalib0.Text),       // 回程差
                    k_10 = float.Parse(txtCalib10.Text),           // 10挡
                    k_20 = float.Parse(txtCalib20.Text),           // 20挡
                    k_50 = float.Parse(txtCalib50.Text),           // 50挡
                    k_100 = float.Parse(txtCalib100.Text),         // 100挡
                    k_200 = float.Parse(txtCalib200.Text),         // 200挡
                    k_300 = float.Parse(txtCalib300.Text),         // 300挡
                    k_400 = float.Parse(txtCalib400.Text),         // 400挡
                    k_500 = float.Parse(txtCalib500.Text),         // 500挡
                    k_600 = float.Parse(txtCalib600.Text),         // 600挡
                    k_700 = float.Parse(txtCalib700.Text),         // 700挡
                    k_800 = float.Parse(txtCalib800.Text),         // 800挡
                    k_900 = float.Parse(txtCalib900.Text),         // 900挡
                    k_1000 = float.Parse(txtCalib1000.Text)        // 1000挡
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
        private async void OpenGripper_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualGripperOpen, NotificationControl.NotificationType.Info);
            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from gripper import Gripper");
            pythonCode.AppendLine($"Gripper.release({nowModuleId})");
            var rawMagneticUpFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var magneticUpFlag = _mainWidget.ParseScriptDebugResponse(rawMagneticUpFlag);
            if (magneticUpFlag != null)
            {
                if (magneticUpFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveFail, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }
        //关闭抓手
        private async void CloseGripper_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualGripperClose, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from gripper import Gripper");
            pythonCode.AppendLine($"Gripper.grasp({nowModuleId})");
            var rawMagneticUpFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var magneticUpFlag = _mainWidget.ParseScriptDebugResponse(rawMagneticUpFlag);
            if (magneticUpFlag != null)
            {
                if (magneticUpFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }
        #endregion
        #region 磁吸模块
        private async void UpMagnetic_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualMagneticUp, NotificationControl.NotificationType.Info);
            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from magnetic import Magnetic");
            pythonCode.AppendLine($"Magnetic.on({nowModuleId})");
            var rawMagneticUpFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var magneticUpFlag = _mainWidget.ParseScriptDebugResponse(rawMagneticUpFlag);
            if (magneticUpFlag != null)
            {
                if (magneticUpFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }
        private async void DownMagnetic_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualMagneticDown, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from magnetic import Magnetic");
            pythonCode.AppendLine($"Magnetic.off({nowModuleId})");
            var rawMagneticUpFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var magneticUpFlag = _mainWidget.ParseScriptDebugResponse(rawMagneticUpFlag);
            if (magneticUpFlag != null)
            {
                if (magneticUpFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }

        #endregion
        #region 加热振荡模块

        /// <summary>
        /// 开启加热振荡温度&转速实时监控
        /// </summary>
        /// <param name="moduleId">加热振荡模块ID</param>
        private async Task StartShakerRealTimeMonitor(int moduleType, int moduleId)
        {
            // 标记为正在监控
            _isShakerRealTimeMonitoring = true;
            // 初始化取消令牌源
            _shakerRealTimeCts = new CancellationTokenSource();
            var token = _shakerRealTimeCts.Token;

            try
            {
                // 循环获取数据，直到收到取消信号
                while (!token.IsCancellationRequested)
                {
                    switch (moduleType)
                    {
                        case 5:
                            {
                                StringBuilder pythonCode2 = new StringBuilder();
                                pythonCode2.AppendLine("from shaker import Shaker");
                                pythonCode2.AppendLine($"debug(Shaker.get_temp({moduleId}))");

                                // 2. 执行脚本并获取返回结果
                                var rawResponse2 = await _mainWidget.ScriptDebugAsync(pythonCode2.ToString());
                                var response2 = _mainWidget.ParseScriptDebugResponse(rawResponse2);

                                if (response2 != null && response2.Result == "succeed" && !string.IsNullOrEmpty(response2.Data))
                                {
                                    string dataStr = response2.Data?.ToString() ?? "";
                                    realGetShakeTemp.Text = dataStr.Trim('\'', '"');

                                }
                                break;
                            }
                        case 7:
                            {
                                StringBuilder pythonCode = new StringBuilder();
                                pythonCode.AppendLine("from cool import Cool");
                                pythonCode.AppendLine($"debug(Cool.get_temp({moduleId}))");

                                // 2. 执行脚本并获取返回结果
                                var rawResponse = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
                                var response = _mainWidget.ParseScriptDebugResponse(rawResponse);

                                if (response != null && response.Result == "succeed" && !string.IsNullOrEmpty(response.Data))
                                {

                                    string dataStr = response.Data?.ToString() ?? "";
                                    realGetTemp.Text = dataStr.Trim('\'', '"');
                                }

                                break;
                            }
                    }


                    // 4. 间隔1秒获取一次（可根据需求调整，如500ms）
                    await Task.Delay(1000, token);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
            }
            finally
            {
                // 重置监控状态
                _isShakerRealTimeMonitoring = false;
                _shakerRealTimeCts?.Dispose(); // 释放资源
                _shakerRealTimeCts = null;
            }
        }

        /// <summary>
        /// 停止加热振荡温度&转速实时监控
        /// </summary>
        private void StopShakerRealTimeMonitor()
        {
            // 如果正在监控且取消令牌源不为null
            if (_isShakerRealTimeMonitoring && _shakerRealTimeCts != null && !_shakerRealTimeCts.Token.IsCancellationRequested)
            {
                _shakerRealTimeCts.Cancel(); // 发送取消信号
                _shakerRealTimeCts.Dispose(); // 释放资源
                _shakerRealTimeCts = null;
            }
            _isShakerRealTimeMonitoring = false; // 重置状态
        }

        private async void btnStartTemperature_Click(object sender, RoutedEventArgs e)
        {
            string tempInputText = ShakerTempSetValue.Text.Trim();
            if (float.TryParse(tempInputText, out float targetTemperature))
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualStartTemperature, NotificationControl.NotificationType.Info);
                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from shaker import Shaker");
                pythonCode.AppendLine($"Shaker.start_temp({nowModuleId},{targetTemperature})");
                var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
                var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
                if (pyFlag != null)
                {
                    if (pyFlag.Result == "succeed")
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                    }
                    else
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                    }
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
            }
        }
        private async void btnStopTemperature_Click(object sender, RoutedEventArgs e)
        {

            _mainWidget.ShowNotification(_mainWidget._res.SettingManualStopTemperature, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from shaker import Shaker");
            pythonCode.AppendLine($"Shaker.stop_temp({nowModuleId})");
            var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
            if (pyFlag != null)
            {
                if (pyFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }

        }
        //开始振荡
        private async void btnStartShake_Click(object sender, RoutedEventArgs e)
        {
            string speedInputText = ShakerSpeedSetValue.Text.Trim();
            string timeInputText = ShakerTimeSetValue.Text.Trim();
            if (float.TryParse(speedInputText, out float targetSpeed) && float.TryParse(timeInputText, out float targetTime))
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualStartShaking, NotificationControl.NotificationType.Info);
                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from shaker import Shaker");
                pythonCode.AppendLine($"Shaker.start_shaker({nowModuleId},{speedInputText},{timeInputText})");
                var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
                var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
                if (pyFlag != null)
                {
                    if (pyFlag.Result == "succeed")
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                    }
                    else
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                    }
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
            }
        }
        private async void btnStopShake_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualStopShaking, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from shaker import Shaker");
            pythonCode.AppendLine($"Shaker.stop_shaker({nowModuleId})");
            var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
            if (pyFlag != null)
            {
                if (pyFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }

        #endregion
        #region 温控模块

        private async void btnStartCool_Click(object sender, RoutedEventArgs e)
        {
            string tempInputText = CoolTempSetValue.Text.Trim();
            if (float.TryParse(tempInputText, out float targetTemperature))
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualStartTemperature, NotificationControl.NotificationType.Info);
                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from cool import Cool");
                pythonCode.AppendLine($"Cool.start({nowModuleId},{targetTemperature})");
                var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
                var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
                if (pyFlag != null)
                {
                    if (pyFlag.Result == "succeed")
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                    }
                    else
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                    }
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
            }
        }

        private async void btnStopCool_Click(object sender, RoutedEventArgs e)
        {

            _mainWidget.ShowNotification(_mainWidget._res.SettingManualStopTemperature, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from cool import Cool");
            pythonCode.AppendLine($"Cool.stop({nowModuleId})");
            var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
            if (pyFlag != null)
            {
                if (pyFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }

        }
        #endregion
        #region PCR模块

        private async void btnStartPCR_Click(object sender, RoutedEventArgs e)
        {
            if (PCRFileAddress.Text == "")
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);

                return;
            }
            string filePath = PCRFileAddress.Text;
            string fileContent = string.Empty;
            fileContent = await File.ReadAllTextAsync(filePath);

            _mainWidget.ShowNotification(_mainWidget._res.SettingManualPCRStart, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from pcr import Pcr");
            pythonCode.AppendLine($"Pcr.run({nowModuleId},\"{fileContent}\")");
            var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
            if (pyFlag != null)
            {
                if (pyFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }

        private async void btnStopPCR_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualPCRStop, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from pcr import Pcr");
            pythonCode.AppendLine($"Pcr.stop({nowModuleId})");
            var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
            if (pyFlag != null)
            {
                if (pyFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }
        private async void btnOpenPCR_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualPCROpen, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from pcr import Pcr");
            pythonCode.AppendLine($"Pcr.opendoor({nowModuleId})");
            var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
            if (pyFlag != null)
            {
                if (pyFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }

        private async void btnClosePCR_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualPCRClose, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from pcr import Pcr");
            pythonCode.AppendLine($"Pcr.closedoor({nowModuleId})");
            var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
            if (pyFlag != null)
            {
                if (pyFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }
        private void btnSeletePCR_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "Dat File (*.dat)|*.dat";

            openFileDialog.Title = "Select PCR.dat";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                PCRFileAddress.Text = selectedFilePath;

            }
            else
            {
                PCRFileAddress.Text = "";
            }
        }
        #endregion


    }
}
