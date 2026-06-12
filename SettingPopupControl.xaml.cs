using ClosedXML.Excel;
using Microsoft.Win32;
using Newtonsoft.Json;

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private List<(int Row, int Col)> _manualFluoSelectedWells = new List<(int Row, int Col)>();//荧光检测的孔位

        public SettingPopupControl(MainWidget mainWidget)
        {
            InitializeComponent();
            _mainWidget = mainWidget;
        }

        #region 变量的新增方法
        private bool _isUpdatingRowVariableCombo = false; // 行下拉框专属锁
        private bool _isUpdatingColVariableCombo = false; // 列下拉框专属锁
        /// <summary>
        /// 创建支持变量的输入行：文本框+已定义变量下拉框
        /// </summary>
        /// <param name="step">当前步骤</param>
        /// <param name="propertyName">绑定的属性名</param>
        /// <param name="labelText">行标签</param>
        /// <param name="isInt">是否为整数类型</param>
        /// <returns>封装好的行控件</returns>
        private StackPanel CreateVariableInputRow(FlowStep step, string propertyName, string labelText, bool isInt, string variateNamePropName, string variateValuePropName)
        {
            var variableValues = CalculateVariableValues(step.Index);
            var varNamePropInfo = step.GetType().GetProperty(variateNamePropName);
            var varValuePropInfo = step.GetType().GetProperty(variateValuePropName);

            var inputTextBox = new TextBox
            {
                Style = (Style)FindResource("InputTextBoxStyle"),
                Width = 100,
                VerticalAlignment = VerticalAlignment.Center
            };
            inputTextBox.SetBinding(TextBox.TextProperty, new Binding
            {
                Source = step,
                Path = new PropertyPath(propertyName),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
            });

            // 变量下拉框
            var variableCombo = new ComboBox
            {
                Style = (Style)FindResource("InputComboBoxStyle"),
                Width = 80,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ItemsSource = variableValues.Keys.ToList()
            };
            variableCombo.SetBinding(Selector.SelectedItemProperty, new Binding
            {
                Source = step,
                Path = new PropertyPath(variateNamePropName),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            inputTextBox.TextChanged += (s, e) =>
            {
                if (inputTextBox.IsFocused)
                {
                    // 清空当前专属变量
                    varNamePropInfo?.SetValue(step, null);
                    varValuePropInfo?.SetValue(step, null);
                    variableCombo.SelectedItem = null;
                }
            };
            variableCombo.SelectionChanged += (s, e) =>
            {
                if (variableCombo.SelectedItem is string selectedVar)
                {
                    if (variableValues.TryGetValue(selectedVar, out float calculatedVal))
                    {
                        bool isValueInteger = Math.Abs(calculatedVal - Math.Round(calculatedVal)) <= 0.0001f;//true:int  false:float
                        if (isInt)
                        {
                            if (isValueInteger)
                            {
                                varNamePropInfo?.SetValue(step, selectedVar);
                                varValuePropInfo?.SetValue(step, calculatedVal.ToString());
                                int intVal = (int)Math.Round(calculatedVal);
                                inputTextBox.Text = intVal.ToString();
                                var bindingExpr = inputTextBox.GetBindingExpression(TextBox.TextProperty);
                                bindingExpr?.UpdateSource();
                            }
                            else
                            {
                                variableCombo.SelectedItem = null;
                                varNamePropInfo?.SetValue(step, null);
                                varValuePropInfo?.SetValue(step, null);
                                inputTextBox.Text = calculatedVal.ToString();
                            }
                        }
                        else
                        {
                            varNamePropInfo?.SetValue(step, selectedVar);
                            varValuePropInfo?.SetValue(step, calculatedVal.ToString());
                            inputTextBox.Text = calculatedVal.ToString();
                            var bindingExpr = inputTextBox.GetBindingExpression(TextBox.TextProperty);
                            bindingExpr?.UpdateSource();
                        }
                    }
                }
            };

            // 4. 封装成横向布局
            var inputPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            inputPanel.Children.Add(inputTextBox);
            inputPanel.Children.Add(variableCombo);
            // 5. 封装成详情行
            return CreateDetailRow(labelText, inputPanel);
        }
        /// <summary>
        /// 模拟执行流程，计算在指定步骤之前，所有变量的最终值
        /// </summary>
        /// <param name="beforeStepIndex">当前步骤的Index</param>
        /// <returns>字典: Key是变量名, Value是最终计算值</returns>
        private Dictionary<string, float> CalculateVariableValues(int beforeStepIndex)
        {
            var resultDict = new Dictionary<string, float>();

            // 1. 找出当前步骤之前的所有变量步骤，并按顺序排列
            var priorSteps = _mainWidget.FlowSteps
                .Where(s => s.Type == "Variate" && s.Index < beforeStepIndex)
                .OrderBy(s => s.Index) // 必须按时间顺序执行
                .ToList();

            foreach (var step in priorSteps)
            {
                string varName = step.VariateScriptName;
                float num = step.VariateNum;

                // 2. 根据操作类型更新字典中的值
                // 注意：这里需要匹配你之前代码中的 _res.SettingManualVariateXxx
                // 如果你的 VariateStep 里存的是枚举，请自行修改 switch 判断

                // 初始化：如果变量名不存在，先给个默认值0 (虽然正常流程是先赋值)
                if (varName != null && !resultDict.ContainsKey(varName)) resultDict[varName] = 0;


                // 判断操作类型
                if (step.VariateStep == _mainWidget._res.SettingManualVariateEqual) // 赋值 =
                {
                    resultDict[varName] = num;
                }
                else if (step.VariateStep == _mainWidget._res.SettingManualVariateAdd) // 加 +=
                {
                    resultDict[varName] += num;
                }
                else if (step.VariateStep == _mainWidget._res.SettingManualVariateMinus) // 减 -=
                {
                    resultDict[varName] -= num;
                }
                else if (step.VariateStep == _mainWidget._res.SettingManualVariateMultiply) // 乘 *=
                {
                    resultDict[varName] *= num;
                }
                else if (step.VariateStep == _mainWidget._res.SettingManualVariateDivide) // 除 /=
                {
                    if (num != 0) // 简单的防除零保护
                        resultDict[varName] /= num;
                }
            }

            return resultDict;
        }
        #endregion
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
            switch (moduleType)//0：单通道移液器；1：八通道移液器；2：96通道移液器；3：抓手；4：PCR；5：加热振荡；6：磁吸；7：温控;8:垃圾桶;9:96荧光检测模块
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
                    //StopShakerRealTimeMonitor();
                    // 开启加热振荡温度&转速实时监控（传入模块ID nowModuleId）
                    //_ = StartShakerRealTimeMonitor(5, nowModuleId);
                    mainSettingTable.SelectedIndex = 5;
                    break;
                case 6:
                    mainSettingTable.SelectedIndex = 4;
                    break;
                case 7:
                    //StopShakerRealTimeMonitor();
                    // 开启加热振荡温度&转速实时监控（传入模块ID nowModuleId）
                    //_ = StartShakerRealTimeMonitor(7, nowModuleId);
                    mainSettingTable.SelectedIndex = 6;
                    break;
                case 8://96荧光检测模块
                    mainSettingTable.SelectedIndex = 8;
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
                "Annotation" => res.WindowActionAnno,
                "Variate" => res.WindowActionVariate,
                "Fluo" => res.WindowActionFluo,
                "If" => res.WindowActionIf,
                "elseIf" => res.WindowActionElseIf,
                "else" => res.WindowActionElse,

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
                var waitTimePanel = CreateVariableInputRow(step, nameof(step.WaitTime), res.StepDetailWaitTime, true, nameof(step.WaitVariateName), nameof(step.WaitVariateValue));
                StepDetailPanel.Children.Add(waitTimePanel);
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
                var shakeTimePanel = CreateVariableInputRow(step, nameof(step.WaitTime), res.StepDetailWaitTime, true, nameof(step.ShakerVariateTimeName), nameof(step.ShakerVariateTimeValue));
                StepDetailPanel.Children.Add(shakeTimePanel);
                // 振荡转速输入
                var shakeSpeedPanel = CreateVariableInputRow(step, nameof(step.ShakeRPM), res.StepDetailShakeSpeed, true, nameof(step.ShakerVariateSpeedName), nameof(step.ShakerVariateSpeedValue));
                StepDetailPanel.Children.Add(shakeSpeedPanel);
                // 振荡温度输入
                var shakeTempPanel = CreateVariableInputRow(step, nameof(step.ShakeTemp), res.StepDetailShakeTemp, false, nameof(step.ShakerVariateTempName), nameof(step.ShakerVariateTempValue));
                StepDetailPanel.Children.Add(shakeTempPanel);
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
                // 磁吸高度输入
                var magnetHeightPanel = CreateVariableInputRow(step, nameof(step.MagnetNums), res.StepDetailMagnetDistance, false, nameof(step.MagnetVariateName), nameof(step.MagnetVariateValue));
                StepDetailPanel.Children.Add(magnetHeightPanel);
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
                var tempControlTempPanel = CreateVariableInputRow(step, nameof(step.TempCtrlTemp), res.StepDetailShakeTemp, false, nameof(step.TempControlVariateTempName), nameof(step.TempControlVariateTempValue));
                StepDetailPanel.Children.Add(tempControlTempPanel);
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
                    Style = (Style)FindResource("ActionButtonStyle"),
                    Content = res.StepDetailSelect,
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
                // 循环起始数字
                var LoopStartNumTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                LoopStartNumTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("LoopStartNum"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.WindowLoopStart, LoopStartNumTextBox));
                // 循环结束数字
                var loopEndPanel = CreateVariableInputRow(step, nameof(step.LoopEndNum), res.WindowLoopEnd, true, nameof(step.LoopEndVariateName), nameof(step.LoopEndVariateValue));
                StepDetailPanel.Children.Add(loopEndPanel);
                //var LoopEndNumTextBox = new TextBox
                //{
                //    Style = (Style)FindResource("InputTextBoxStyle"),
                //    Width = 140,
                //    VerticalAlignment = VerticalAlignment.Center
                //};
                //LoopEndNumTextBox.SetBinding(TextBox.TextProperty, new Binding
                //{
                //    Source = step,
                //    Path = new PropertyPath("LoopEndNum"),
                //    Mode = BindingMode.TwoWay,
                //    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                //});
                //StepDetailPanel.Children.Add(CreateDetailRow(res.WindowLoopEnd, LoopEndNumTextBox));
                // 循环自增量
                var LoopAddNumTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                LoopAddNumTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("LoopAddNum"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.WindowLoopStep, LoopAddNumTextBox));
                return;
            }
            else if (step.Type == "Annotation")
            {
                // 注释内容 

                var annoValueTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    Height = 90,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                annoValueTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("AnnoValue"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.WindowActionAnno, annoValueTextBox));
                return;
            }
            else if (step.Type == "Variate")//变量
            {
                var VariateScriptNameTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                VariateScriptNameTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("VariateScriptName"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                VariateScriptNameTextBox.PreviewTextInput += (s, e) =>
                {
                    var textBox = s as TextBox;
                    string newText = textBox.Text + e.Text;

                    // 第一个不能为数字
                    if (newText.Length == 1 && char.IsDigit(e.Text[0]))
                    {
                        e.Handled = true;
                    }
                };
                StepDetailPanel.Children.Add(CreateDetailRow(res.SettingManualVariateScriptName, VariateScriptNameTextBox));
                // 动作
                var posVariateFromCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { res.SettingManualVariateEqual, res.SettingManualVariateAdd, res.SettingManualVariateMinus, res.SettingManualVariateMultiply, res.SettingManualVariateDivide },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var posFromVariateBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("VariateStep"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                posVariateFromCombo.SetBinding(ComboBox.SelectedItemProperty, posFromVariateBinding);

                StepDetailPanel.Children.Add(CreateDetailRow(res.SettingManualVariateScriptStep, posVariateFromCombo));
                // 变量值
                var variateValueTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                variateValueTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("VariateNum"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Delay = 300
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.SettingManualVariateScriptValue, variateValueTextBox));
                return;
            }
            else if (step.Type == "Fluo")
            {
                var posFromCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { res.SettingManualPCRStart, res.SettingManualPCROpen, res.SettingManualPCRClose, res.SettingManualPCRWaitRun, res.SettingManualFluoConcHomo },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var posFromBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("FluoStep"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                posFromCombo.SetBinding(ComboBox.SelectedItemProperty, posFromBinding);

                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailFluoprocedure, posFromCombo));
                var selectWellsBtn = new Button
                {
                    Style = (Style)FindResource("ActionButtonStyle"),
                    Content = res.StepDetailSelect,
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 5)
                };
                // 2. 显示已选孔位的文本框（只读）
                var selectedWellsTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 180,
                    Height = 60,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 5, 0, 5)
                };

                // 绑定到FlowStep的SelectedWells属性（稍后添加）
                selectedWellsTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("SelectedWells"),
                    Mode = BindingMode.OneWay
                });

                // 3. 按钮点击事件：弹出孔位选择窗口
                selectWellsBtn.Click += (s, e) =>
                {
                    var wellWindow = new WellSelectionWindow();

                    // 如果已有选中孔位，传递给窗口进行回显
                    if (!string.IsNullOrEmpty(step.SelectedWells))
                    {
                        wellWindow.SelectedWells = step.SelectedWells
                            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(wellStr =>
                            {
                                var parts = wellStr.Split(',');
                                return (Row: int.Parse(parts[0]), Col: int.Parse(parts[1]));
                            })
                            .ToList();
                    }

                    // 显示模态窗口
                    if (wellWindow.ShowDialog() == true)
                    {
                        // 将选中的孔位转换为系统统一格式："行,列;行,列"
                        step.SelectedWells = string.Join(";",
                            wellWindow.SelectedWells.Select(w => $"{w.Row},{w.Col}"));

                        // 同时同步到WellPosition属性（保持和其他步骤一致）
                        step.WellPosition = FormatSelectedColumns(wellWindow.SelectedWells, 8, 12);
                    }
                };

                // 添加到详情面板
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailSelectWells, selectWellsBtn));
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailSelectdWells, selectedWellsTextBox));
                // 创建原始体积输入框并绑定
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

                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailOriginalVolume, volumeTextBox));
                // 创建最终体积输入框并绑定
                var volumeFinalTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 150
                };

                var volumeFinalBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("PushOutvolume"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                };

                volumeFinalTextBox.SetBinding(TextBox.TextProperty, volumeFinalBinding);

                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailFinalVolume, volumeFinalTextBox));
                var positionFluoCombo0 = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12" },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                // 绑定到step.Position（双向）
                var positionFluoBinding0 = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("Normaposition0"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                positionFluoCombo0.SetBinding(ComboBox.SelectedItemProperty, positionFluoBinding0);
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailOperationPosition, positionFluoCombo0));
                var positionFluoCombo1 = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12" },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                // 绑定到step.Position（双向）
                var positionFluoBinding1 = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("Normaposition1"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                positionFluoCombo1.SetBinding(ComboBox.SelectedItemProperty, positionFluoBinding1);
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailFluoTipOnPosition, positionFluoCombo1));

                var positionFluoCombo2 = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12" },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                // 绑定到step.Position（双向）
                var positionFluoBinding2 = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("Normaposition2"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                positionFluoCombo2.SetBinding(ComboBox.SelectedItemProperty, positionFluoBinding2);
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailFluoDiluentPosition, positionFluoCombo2));

                var positionFluoCombo3 = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12" },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                // 绑定到step.Position（双向）
                var positionFluoBinding3 = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("Normaposition3"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                positionFluoCombo3.SetBinding(ComboBox.SelectedItemProperty, positionFluoBinding3);
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailFluoProductPosition, positionFluoCombo3));

                return;
            }
            else if (step.Type == "If")
            {
                var IfScriptNameTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                IfScriptNameTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("IfScriptName"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                IfScriptNameTextBox.PreviewTextInput += (s, e) =>
                {
                    var textBox = s as TextBox;
                    string newText = textBox.Text + e.Text;

                    // 第一个不能为数字
                    if (newText.Length == 1 && char.IsDigit(e.Text[0]))
                    {
                        e.Handled = true;
                    }
                };
                StepDetailPanel.Children.Add(CreateDetailRow(res.SettingManualIfScriptName, IfScriptNameTextBox));
                // 动作
                var posIfFromCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { res.SettingManuaIfGreaterThan, res.SettingManuaIfLessThan, res.SettingManuaIfEquals, res.SettingManuaIfNotEquals, res.SettingManuaIfGreaterThanorEqual, res.SettingManuaIfLessThanorEqual },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var posFromIfBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("IfStep"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                posIfFromCombo.SetBinding(ComboBox.SelectedItemProperty, posFromIfBinding);
                StepDetailPanel.Children.Add(CreateDetailRow(res.SettingManualIfScriptStep, posIfFromCombo));

                // 判断值
                var ifValueTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                ifValueTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("IfNum"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Delay = 300
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.SettingManualIfScriptValue, ifValueTextBox));
                return;
            }
            else if (step.Type == "elseIf")
            {
                // 动作
                var posIfFromCombo = new ComboBox
                {
                    Style = (Style)FindResource("InputComboBoxStyle"),
                    ItemsSource = new List<string> { res.SettingManuaIfGreaterThan, res.SettingManuaIfLessThan, res.SettingManuaIfEquals, res.SettingManuaIfNotEquals, res.SettingManuaIfGreaterThanorEqual, res.SettingManuaIfLessThanorEqual },
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var posFromIfBinding = new Binding
                {
                    Source = step,
                    Path = new PropertyPath("IfStep"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                posIfFromCombo.SetBinding(ComboBox.SelectedItemProperty, posFromIfBinding);
                StepDetailPanel.Children.Add(CreateDetailRow(res.SettingManualIfScriptStep, posIfFromCombo));

                // 判断值
                var ifValueTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    VerticalAlignment = VerticalAlignment.Center
                };
                ifValueTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("IfNum"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Delay = 300
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.SettingManualIfScriptValue, ifValueTextBox));
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
                    wellSelectionCanvas.ClearSelection();
                    wellSelectionCanvas.IsInteractive = false;

                    // 绑定画布的耗材数据（从板位映射中获取）
                    if (_mainWidget._plateConsumableMap.TryGetValue(_mainWidget._currentSelectedPlateId, out var consumable))
                    {
                        // 显示当前耗材名称
                        step.ConsName = string.Format(res.StepDetailCurrentCons, consumable.Name);
                        step.ConsRows = consumable.Settings.numRows;
                        step.ConsCols = consumable.Settings.numColumns;
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

            // 变量下拉框
            var variableValues = CalculateVariableValues(step.Index);
            var variableRowCombo = new ComboBox
            {
                Style = (Style)FindResource("InputComboBoxStyle"),
                Width = 80,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ItemsSource = variableValues.Keys.ToList()
            };
            variableRowCombo.SetBinding(Selector.SelectedItemProperty, new Binding
            {
                Source = step,
                Path = new PropertyPath("WellRowVariateName"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            variableRowCombo.SelectionChanged += (s, e) =>
            {
                var combo = s as ComboBox;
                if (!combo.IsKeyboardFocusWithin)
                    return;

                if (_isUpdatingRowVariableCombo) return;
                if (e.AddedItems.Count == 0) return;
                //if (e.AddedItems[0] is not string selectedVar) return;
                _isUpdatingRowVariableCombo = true;
                try
                {
                    if (variableRowCombo.SelectedItem is string selectedVar)
                    {
                        //var variableValues = CalculateVariableValues(step.Index);
                        if (variableValues.TryGetValue(selectedVar, out float calculatedVal))
                        {
                            bool isValueInteger = Math.Abs(calculatedVal - Math.Round(calculatedVal)) <= 0.0001f;//true:int  false:float
                            Debug.WriteLine(selectedVar);
                            Debug.WriteLine(calculatedVal);

                            if (isValueInteger)
                            {
                                step.WellRowVariateName = selectedVar;
                                step.WellRowVariateValue = calculatedVal.ToString();
                                //进行设置画布 selectwells
                                if (step.WellRowVariateName != "")
                                {
                                    int rowVal = int.Parse((step.WellRowVariateValue));

                                    Debug.WriteLine("厕所" + rowVal);
                                    if (step.SelectedCells == null)
                                        return;
                                    if (wellSelectionCanvas.CurrentSelectionMode == CanvasSelectionMode.SingleCell)
                                    {
                                        if (rowVal < 1 || rowVal > step.ConsRows)
                                        {
                                            variableRowCombo.SelectedItem = null;
                                            step.WellRowVariateName = "";
                                            step.WellRowVariateValue = "";
                                            return;
                                        }
                                        string rowText = rowVal.ToString();

                                        var existingCells = step.SelectedCells?
                                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                            .ToList() ?? new List<string>();
                                        existingCells[0] = rowText;
                                        step.SelectedCells = string.Join(",", existingCells); //行：4 列：6
                                        string columnText = $"{ResourceHelper.Instance.StepDetailRowPrefix}{existingCells[0]} {ResourceHelper.Instance.StepDetailColumnPrefix}{existingCells[1]}";
                                        step.WellPosition = columnText;
                                    }
                                    else if (wellSelectionCanvas.CurrentSelectionMode == CanvasSelectionMode.EntireColumn)
                                    {
                                        if (rowVal < ((step.ConsRows - 1) * -1) || rowVal > step.ConsRows || rowVal == 0)
                                        {
                                            variableRowCombo.SelectedItem = null;
                                            step.WellRowVariateName = "";
                                            step.WellRowVariateValue = "";
                                            return;
                                        }

                                        var existingCells = step.SelectedCells?
                                            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                            .ToList() ?? new List<string>();
                                        if (rowVal > 0)
                                        {
                                            existingCells = existingCells.GetRange(rowVal - 1, existingCells.Count - (rowVal - 1));
                                        }
                                        else
                                        {
                                            int countToKeep = step.ConsRows - Math.Abs(rowVal);
                                            existingCells = existingCells.GetRange(0, countToKeep);
                                        }

                                        step.SelectedCells = string.Join(";", existingCells);
                                        var cellList = new List<(int Row, int Col)>();
                                        foreach (var cellStr in existingCells)
                                        {
                                            var parts = cellStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                                            if (parts.Length == 2 && int.TryParse(parts[0], out int row) && int.TryParse(parts[1], out int col))
                                            {
                                                cellList.Add((row, col));
                                            }
                                        }
                                        step.WellPosition = FormatSelectedColumns(cellList, step.ConsRows, step.ConsCols);
                                    }
                                    else if (wellSelectionCanvas.CurrentSelectionMode == CanvasSelectionMode.EntirePlate)
                                    {

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
                            }
                            else
                            {
                                variableRowCombo.SelectedItem = null;
                                step.WellRowVariateName = "";
                                step.WellRowVariateValue = "";
                                //inputTextBox.Text = calculatedVal.ToString();
                            }
                        }
                    }
                }
                finally
                {
                    _isUpdatingRowVariableCombo = false;
                }

            };
            var variableColText = new TextBlock
            {
                Text = res.StepDetailColumnPrefix,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14
            };
            var variableColCombo = new ComboBox
            {
                Style = (Style)FindResource("InputComboBoxStyle"),
                Width = 80,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ItemsSource = variableValues.Keys.ToList()
            };
            variableColCombo.SetBinding(Selector.SelectedItemProperty, new Binding
            {
                Source = step,
                Path = new PropertyPath("WellColVariateName"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            variableColCombo.SelectionChanged += (s, e) =>
            {
                var combo = s as ComboBox;
                if (!combo.IsKeyboardFocusWithin)
                    return;
                if (_isUpdatingColVariableCombo) return;
                if (e.AddedItems.Count == 0) return;
                _isUpdatingColVariableCombo = true;
                try
                {
                    if (variableColCombo.SelectedItem is string selectedVar)
                    {
                        if (variableValues.TryGetValue(selectedVar, out float calculatedVal))
                        {
                            bool isValueInteger = Math.Abs(calculatedVal - Math.Round(calculatedVal)) <= 0.0001f;//true:int  false:float

                            if (isValueInteger)
                            {
                                step.WellColVariateName = selectedVar;
                                step.WellColVariateValue = calculatedVal.ToString();
                                //进行设置画布 selectwells
                                if (step.WellColVariateName != "")
                                {
                                    int colVal = int.Parse((step.WellColVariateValue));
                                    if (colVal < 1 || colVal > step.ConsCols)
                                    {
                                        variableColCombo.SelectedItem = null;
                                        step.WellColVariateName = "";
                                        step.WellColVariateValue = "";
                                        return;
                                    }
                                    Debug.WriteLine("尺寸" + colVal);
                                    if (wellSelectionCanvas.CurrentSelectionMode == CanvasSelectionMode.SingleCell)
                                    {
                                        string colText = colVal.ToString();

                                        var existingCells = step.SelectedCells?
                                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                            .ToList() ?? new List<string>();
                                        existingCells[1] = colText;
                                        step.SelectedCells = string.Join(",", existingCells); //行：4 列：6
                                        string columnText = $"{ResourceHelper.Instance.StepDetailRowPrefix}{existingCells[0]} {ResourceHelper.Instance.StepDetailColumnPrefix}{existingCells[1]}";
                                        step.WellPosition = columnText;
                                    }
                                    else if (wellSelectionCanvas.CurrentSelectionMode == CanvasSelectionMode.EntireColumn)
                                    {
                                        var existingCells = step.SelectedCells?
                                            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                            .ToList() ?? new List<string>();
                                        for (int i = 0; i < existingCells.Count; i++)
                                        {
                                            var rowColParts = existingCells[i].Split(',');
                                            if (rowColParts.Length >= 1) // 确保行号存在
                                            {
                                                string rowNum = rowColParts[0];
                                                existingCells[i] = $"{rowNum},{colVal}"; // 替换列号
                                            }
                                        }
                                        step.SelectedCells = string.Join(";", existingCells);
                                        var cellList = new List<(int Row, int Col)>();
                                        foreach (var cellStr in existingCells)
                                        {
                                            var parts = cellStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                                            if (parts.Length == 2 && int.TryParse(parts[0], out int row) && int.TryParse(parts[1], out int col))
                                            {
                                                cellList.Add((row, col));
                                            }
                                        }
                                        step.WellPosition = FormatSelectedColumns(cellList, step.ConsRows, step.ConsCols);
                                    }
                                    else if (wellSelectionCanvas.CurrentSelectionMode == CanvasSelectionMode.EntirePlate)
                                    {

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
                            }
                            else
                            {
                                variableColCombo.SelectedItem = null;
                                step.WellColVariateName = "";
                                step.WellColVariateValue = "";
                                //inputTextBox.Text = calculatedVal.ToString();
                            }
                        }
                    }
                }
                finally
                {
                    _isUpdatingColVariableCombo = false;
                }

            };
            var inputPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            inputPanel.Children.Add(variableRowCombo);
            inputPanel.Children.Add(variableColText);
            inputPanel.Children.Add(variableColCombo);
            StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailRowPrefix, inputPanel));

            /*
            //罗贤全
            //            var wellRowPanel = CreateVariableInputRow(
            //    step,
            //    nameof(step.WellRowVariateValue),
            //    res.StepDetailRowPrefix.TrimEnd('：', ':'), // 取资源里的「行」，自动去掉冒号，对齐其他参数标签
            //    false, // 非必填，和温度参数一致
            //    nameof(step.WellRowVariateName),
            //    nameof(step.WellRowVariateValue)
            //);
            //            StepDetailPanel.Children.Add(wellRowPanel);

            //            // 2. 列参数行（支持变量绑定）
            //            var wellColPanel = CreateVariableInputRow(
            //                step,
            //                nameof(step.WellColVariateValue),
            //                res.StepDetailColumnPrefix.TrimEnd('：', ':'), // 取资源里的「列」
            //                false,
            //                nameof(step.WellColVariateName),
            //                nameof(step.WellColVariateValue)
            //            );
            //            StepDetailPanel.Children.Add(wellColPanel);

            //            // 【可选保留】原有组合后的孔位只读预览，方便用户查看最终效果，不影响核心逻辑
            //            var wellPositionPreviewTextBox = new TextBox
            //            {
            //                Style = (Style)FindResource("InputTextBoxStyle"),
            //                Width = 140,
            //                VerticalAlignment = VerticalAlignment.Center,
            //                IsReadOnly = true
            //            };
            //            wellPositionPreviewTextBox.SetBinding(TextBox.TextProperty, new Binding
            //            {
            //                Source = step,
            //                Path = new PropertyPath("WellPosition"),
            //                Mode = BindingMode.OneWay
            //            });
            //            StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailWellPosition, wellPositionPreviewTextBox));
            //罗贤全
            */

            StepDetailPanel.Children.Add(new TextBlock
            {
                Text = res.StepDetailWellSelectionArea, // “孔位选择区：”/“Well Position Selection Area:”
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 5, 0, 2)
            });
            // 绑定画布的选中列变更事件
            wellSelectionCanvas.SelectedColumnsChanged += (plateId, columnText) =>
            {
                if (_isUpdatingRowVariableCombo || _isUpdatingColVariableCombo) return;

                step.WellPosition = columnText;
                var selectedCells = _mainWidget._selectedCellsFromText(columnText);
                step.SelectedCells = string.Join(";", selectedCells.Select(c => $"{c.Row},{c.Col}"));

                variableRowCombo.SelectedItem = null;
                step.WellRowVariateName = "";
                step.WellRowVariateValue = "";
                variableColCombo.SelectedItem = null;
                step.WellColVariateName = "";
                step.WellColVariateValue = "";
            };
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
                    // 创建等待输入框并绑定
                    var waitTextBox = new TextBox
                    {
                        Style = (Style)FindResource("InputTextBoxStyle"),
                        Width = 150
                    };

                    var waitBinding = new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("PipeWaitTime"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                    };

                    waitTextBox.SetBinding(TextBox.TextProperty, waitBinding);

                    StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailWaitTime, waitTextBox));
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
                    // 创建等待输入框并绑定
                    var waitTextBox = new TextBox
                    {
                        Style = (Style)FindResource("InputTextBoxStyle"),
                        Width = 150
                    };

                    var waitBinding = new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("PipeWaitTime"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                    };

                    waitTextBox.SetBinding(TextBox.TextProperty, waitBinding);

                    StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailWaitTime, waitTextBox));
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

                    // 创建吸入体积输入框并绑定
                    var volumeInhaTextBox = new TextBox
                    {
                        Style = (Style)FindResource("InputTextBoxStyle"),
                        Width = 150
                    };

                    var volumeInhaBinding = new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("InhaVolume"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                    };

                    volumeInhaTextBox.SetBinding(TextBox.TextProperty, volumeInhaBinding);

                    StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailInhaVolume, volumeInhaTextBox));
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
        private string FormatSelectedColumns(List<(int Row, int Col)> selectedCells, int consRows, int consCols)
        {
            if (selectedCells.Count == 0)
                return "";

            var cells = selectedCells.ToList();

            if (!cells.Any())
                return "";

            var rows = cells.Select(c => c.Row).Distinct().OrderBy(r => r).ToList();
            var cols = cells.Select(c => c.Col).Distinct().OrderBy(c => c).ToList();

            string rowText = FormatRange(rows, consRows);
            string colText = FormatRange(cols, consCols);

            return $"{ResourceHelper.Instance.StepDetailRowPrefix}{rowText} {ResourceHelper.Instance.StepDetailColumnPrefix}{colText}";
        }
        private string FormatRange(List<int> numbers, int consNums)
        {
            if (numbers.Count == 0)
                return "";
            if (numbers.Count == 1)
                return numbers[0].ToString();

            int lastNumber = numbers[numbers.Count - 1];

            var ranges = new List<string>();
            int start = numbers[0];
            int end = numbers[0];

            for (int i = 1; i < numbers.Count; i++)
            {
                if (numbers[i] == end + 1)
                {
                    end = numbers[i];
                }
                else
                {
                    ranges.Add(start == end ? $"{start}" : $"{start}~{end}");
                    start = end = numbers[i];
                }
            }
            ranges.Add(start == end ? $"{start}" : $"{start}~{end}");

            return string.Join("；", ranges);
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
                    // 1挡（txtCalib1）
                    txtCalib1.Text = calibrationParams.k_1.ToString("F2");
                    // 2挡（txtCalib2）
                    txtCalib2.Text = calibrationParams.k_2.ToString("F2");
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
            Formatting = Newtonsoft.Json.Formatting.None,  // 紧凑格式，解决换行导致的字符串截断
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
                    k_1000 = float.Parse(txtCalib1000.Text),        // 1000挡
                    k_1 = float.Parse(txtCalib1.Text),              // 1挡
                    k_2 = float.Parse(txtCalib2.Text)        // 1挡


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
            string heightInput = MagneticHeightSetValue.Text.Trim();

            if (!float.TryParse(heightInput, out float heightValue))
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingMagneticHeightInvalid, NotificationControl.NotificationType.Warn);
                return;
            }
            if (heightValue < 0 || heightValue > 25)
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingMagneticHeightInvalid, NotificationControl.NotificationType.Warn);
                return;
            }

            _mainWidget.ShowNotification(_mainWidget._res.SettingManualMagneticUp, NotificationControl.NotificationType.Info);
            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from magnetic import Magnetic");
            pythonCode.AppendLine($"Magnetic.on({nowModuleId}, {heightValue})");
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
        private async void btnGetShakerCool_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualGetTemperature, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode2 = new StringBuilder();
            pythonCode2.AppendLine("from shaker import Shaker");
            pythonCode2.AppendLine($"debug(Shaker.get_temp({nowModuleId}))");

            // 2. 执行脚本并获取返回结果
            var rawResponse2 = await _mainWidget.ScriptDebugAsync(pythonCode2.ToString());
            var response2 = _mainWidget.ParseScriptDebugResponse(rawResponse2);

            if (response2 != null && response2.Result == "succeed" && !string.IsNullOrEmpty(response2.Data))
            {
                string dataStr = response2.Data?.ToString() ?? "";
                realGetShakeTemp.Text = dataStr.Trim('\'', '"');

                _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);

            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
            }
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
        private async void btnGetCool_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualGetTemperature, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from cool import Cool");
            pythonCode.AppendLine($"debug(Cool.get_temp({nowModuleId}))");

            // 2. 执行脚本并获取返回结果
            var rawResponse = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
            var response = _mainWidget.ParseScriptDebugResponse(rawResponse);

            if (response != null && response.Result == "succeed" && !string.IsNullOrEmpty(response.Data))
            {

                string dataStr = response.Data?.ToString() ?? "";
                realGetTemp.Text = dataStr.Trim('\'', '"');

                _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
            }
        }
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
        #region 荧光检测模块
        //脚本选择
        private void btnSeleteFluo_Click(object sender, RoutedEventArgs e)
        {
            // 实例化孔位选择窗口
            var wellWindow = new WellSelectionWindow();

            // 回显之前已选中的孔位（如果有）
            if (_manualFluoSelectedWells.Count > 0)
            {
                wellWindow.SelectedWells = new List<(int Row, int Col)>(_manualFluoSelectedWells);
            }

            // 显示模态窗口
            if (wellWindow.ShowDialog() == true)
            {
                // 保存原始选中数据（供后续启动检测使用）
                _manualFluoSelectedWells = wellWindow.SelectedWells;

                // 转换为易读的格式显示在文本框中（如：A1, A2, B3, C5）
                string displayText = ConvertSelectedWellsToDisplayText(_manualFluoSelectedWells);
                FluoFileAddress.Text = displayText;
            }
        }
        //开始检测
        private async void btnStartFluo_Click(object sender, RoutedEventArgs e)
        {
            if (_manualFluoSelectedWells.Count == 0)
            {
                _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
                return;
            }
            string nowSelectedWells = string.Join(";",
                                 _manualFluoSelectedWells.Select(w => $"{w.Row},{w.Col}"));
            byte[] checkedArray = ConvertToFluoCheckedArray(_manualFluoSelectedWells);
            string pythonCheckedList = _mainWidget.ConvertSelectedWellsToPythonChecked(nowSelectedWells);
            try
            {
                StringBuilder pythonCode = new StringBuilder();

                pythonCode.AppendLine("from flour import Flour");
                pythonCode.AppendLine($"Flour.start(id=1,checked={pythonCheckedList}, is_open=True)");
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
            catch (Exception ex)
            {
                _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationFailure, NotificationControl.NotificationType.Error);
            }
        }
        //开盖
        private async void btnOpenFluo_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualPCROpen, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from flour import Flour");
            pythonCode.AppendLine($"debug(Flour.door_open({nowModuleId}))");
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
        //关盖
        private async void btnCloseFluo_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualPCRClose, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from flour import Flour");
            pythonCode.AppendLine($"Flour.door_close({nowModuleId})");
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
        //获得值
        private async void btnGetFluo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnGetFluo.IsEnabled = false;
                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from flour import Flour");
                pythonCode.AppendLine($"debug(Flour.get_state({nowModuleId}))");
                var rawFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
                var pyFlag = _mainWidget.ParseScriptDebugResponse(rawFlag);
                if (pyFlag == null || pyFlag.Result != "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
                    return;
                }
                string nowState = pyFlag.Data.ToString();
                Match workMatch = Regex.Match(nowState, @"['""]work_state['""]\s*:\s*(-?\d+)");
                int workState = -1;
                if (workMatch.Success)
                {
                    workState = int.Parse(workMatch.Groups[1].Value);
                }
                if (workState != 0)
                {
                    _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
                    return;
                }
                StringBuilder pythonCode2 = new StringBuilder();
                pythonCode2.AppendLine("from flour import Flour");
                pythonCode2.AppendLine($"debug(Flour.get_all_data({nowModuleId}))");
                var rawFlag2 = await _mainWidget.ScriptDebugAsync(pythonCode2.ToString());
                var pyFlag2 = _mainWidget.ParseScriptDebugResponse(rawFlag2);
                if (pyFlag2 == null || pyFlag2.Result != "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);

                    return;
                }
                string rawData = pyFlag2.Data.ToString();
                List<int> Fluoresult = Enumerable.Repeat(0, 96).ToList();
                MatchCollection matches = Regex.Matches(rawData, @"['""]value['""]\s*:\s*(-?\d+)");

                int count = Math.Min(matches.Count, 96);
                for (int i = 0; i < count; i++)
                {
                    if (int.TryParse(matches[i].Groups[1].Value, out int value))
                    {
                        Fluoresult[i] = value;
                    }
                }
                Export96WellToExcel(Fluoresult);
                _mainWidget.ShowNotification(_mainWidget._res.DeviceOperationSucc, NotificationControl.NotificationType.Info);
            }
            finally
            {
                btnGetFluo.IsEnabled = true;
            }
        }
        public static void Export96WellToExcel(List<int> fluoData)
        {

            string fileName = $"Fluo_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Data");

                // 第一行：列号 1-12（B1到M1）
                for (int col = 1; col <= 12; col++)
                {
                    worksheet.Cell(1, col + 1).Value = col.ToString();
                }

                // 第一列：行号 A-H（A2到A9）
                char[] rowLabels = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H' };
                for (int row = 0; row < 8; row++)
                {
                    worksheet.Cell(row + 2, 1).Value = rowLabels[row].ToString();
                }



                // ================== 5. 写入96孔数据 ==================
                // 数据映射关系和之前完全一致：
                // 索引 0-11 → A1-A12 → 第2行第2-13列
                // 索引 12-23 → B1-B12 → 第3行第2-13列
                // ...
                // 索引 84-95 → H1-H12 → 第9行第2-13列
                for (int i = 0; i < 96; i++)
                {
                    int excelRow = (i / 12) + 2; // ClosedXML行号从1开始
                    int excelCol = (i % 12) + 2; // ClosedXML列号从1开始
                    worksheet.Cell(excelRow, excelCol).Value = fluoData[i];
                }

                worksheet.Row(1).Height = 22;   // 表头行高
                for (int row = 2; row <= 9; row++)
                    worksheet.Row(row).Height = 20;

                // 设置字体大小
                worksheet.Range("A1:M9").Style.Font.FontSize = 12;
                // 表头加粗
                worksheet.Row(1).Style.Font.Bold = true;

                // 自动调整列宽（只需这一句，不要重复设置固定宽度）
                worksheet.Columns().AdjustToContents();

                workbook.SaveAs(savePath);

                Console.WriteLine($"导出成功！文件保存至：{savePath}");
                //_mainWidget.ShowNotification(savePath, NotificationControl.NotificationType.Info);

                // MessageBox.Show($"导出成功！\n文件路径：{savePath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }
        /// <summary>
        /// 将选中的孔位列表转换为用户易读的显示格式（A1, B2, C3...）
        /// </summary>
        private string ConvertSelectedWellsToDisplayText(List<(int Row, int Col)> selectedWells)
        {
            if (selectedWells == null || selectedWells.Count == 0)
            {
                return "未选择任何孔位";
            }

            string[] rowLabels = { "A", "B", "C", "D", "E", "F", "G", "H" };
            List<string> displayNames = new List<string>();

            foreach (var well in selectedWells)
            {
                // 行号1对应A，行号8对应H
                string rowLabel = rowLabels[well.Row - 1];
                displayNames.Add($"{rowLabel}{well.Col}");
            }

            // 超过10个孔位时显示数量+前几个示例，避免文本框过长
            //if (displayNames.Count > 10)
            //{
            //    return $"已选择 {displayNames.Count} 个孔位：{string.Join(", ", displayNames.Take(10))}...";
            //}

            return string.Join(", ", displayNames);
        }
        private byte[] ConvertToFluoCheckedArray(List<(int Row, int Col)> selectedWells)
        {
            byte[] checkedArray = new byte[12];

            foreach (var well in selectedWells)
            {
                int colIndex = well.Col - 1;
                int bitPosition = well.Row - 1;
                checkedArray[colIndex] |= (byte)(1 << bitPosition);
            }

            return checkedArray;
        }
        #endregion


    }
}
