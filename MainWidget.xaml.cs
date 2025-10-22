using Grpc.Net.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static DataEngine.DataEngine;
using CommonModel;
using static CommonModel.CommonModel;
using static PipetteModule.PipetteModule;
using static ScriptEngine.ScriptEngine;
using static ShiftModule.ShiftrModule;
using MotorModule;
using static MotorModule.MotorModule;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataEngine;
using PipetteModule;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml;
using ScriptEngine;
using Grpc.Core;
using System.Diagnostics;
using System.Globalization;
using System.ComponentModel;
using OctoFixFlow.Resource;
namespace OctoFixFlow
{
    /// <summary>
    /// MainWidget.xaml 的交互逻辑
    /// </summary>
    public partial class MainWidget : Window
    {
        public readonly ResourceHelper _res;

        public ObservableCollection<string> RunLogs { get; } = new ObservableCollection<string>();
        private DatabaseService databaseService;
        private const int MAX_NOTIFICATIONS = 3;//14.6  12.9

        // 耗材项数据集合（绑定到ItemsControl）
        public ObservableCollection<ConsumableItem> Consumables { get; set; }
        // 记录板位与耗材的关联（板位编号 -> 耗材）
        private Dictionary<string, ConsumableItem> _plateConsumableMap = new Dictionary<string, ConsumableItem>();
        // 记录当前鼠标所在的板位
        private Border _currentHoveredPlate = null;
        // 流程步骤集合
        public ObservableCollection<FlowStep> FlowSteps { get; set; }
        private int _stepIndex = 1; // 步骤序号计数器
        private string _currentSelectedPlateId;    // 记录当前选中的板位ID
        //液体类
        public ObservableCollection<LiquidSettings> Liquids { get; set; }

        //grpc
        private GrpcChannel _channel;
        private CommonModelClient _commonClient;//通用模块
        private DataEngineClient _dataEngineClient;//数据库
        private MotorModuleClient _MotorClient;//电机
        private PipetteModuleClient _pipetteClient;//移液器
        private ScriptEngineClient _ScriptClient;//数据通信
        private ShiftrModuleClient _ShiftClient;//抓手

        private float UVFlag = 0;//0:close;1:open
        private float LightFlag = 0;//0:close;1:open
        public float DoorFlag = 0;//1：当前未关上
        public bool runFlag = false;
        public bool pauseFlag = false;
        //grpc
        /// <summary>
        /// 脚本监控事件（对应C++的monitorDataReceived信号）
        /// </summary>
        public event EventHandler<ScriptMonitorEventArgs> MonitorDataReceived;
        private CancellationTokenSource _monitorCts;
        // 添加私有变量记录上一次日志的步骤和状态，用于避免重复日志
        private int _lastLoggedStep = -1;
        private string _lastLoggedState = "";
        public MainWidget()
        {
            InitializeComponent();
            _res = ResourceHelper.Instance;
            databaseService = new DatabaseService();

            DataContext = this;
            InitializeLanguage();
            //GRPC
            InitializeGrpcClient();
            _ = LoadDeviceStatus();

            //GRPC
            //AddLogEntry("任务启动");
            Consumables = new ObservableCollection<ConsumableItem>();
            _ = LoadConsumablesData(); // 加载耗材数据
            Liquids = new ObservableCollection<LiquidSettings>();
            _ = LoadLiquidsData();
            FlowSteps = new ObservableCollection<FlowStep>();// 初始化流程步骤集合
                                                             // 添加开始步骤
            FlowSteps.Add(new FlowStep
            {
                Index = 1,
                //Name = "开始",
                //Name = "Start",
                Type = "start",
                IsSelected = false,
                IsSystemStep = true // 标记为系统步骤
            });
            // 添加结束步骤
            FlowSteps.Add(new FlowStep
            {
                Index = 2,
                //Name = "结束",
                //Name = "End",
                Type = "end",
                IsSelected = false,
                IsSystemStep = true // 标记为系统步骤
            });
            FlowList.ItemsSource = FlowSteps;
            _stepIndex = 3;
            //LangSwitch.Visibility = Visibility.Collapsed;
            //settingButton.Visibility = Visibility.Collapsed;

        }
        private void InitializeLanguage()
        {
            ResourceHelper.Instance.SwitchToEnglish();


            LangSwitch.IsChecked = false;
        }
        //GRPC
        private void InitializeGrpcClient()
        {
            try
            {
                // 配置通道参数
                var channelOptions = new GrpcChannelOptions
                {
                    HttpHandler = new System.Net.Http.SocketsHttpHandler
                    {
                        PooledConnectionIdleTimeout = System.Threading.Timeout.InfiniteTimeSpan,
                        KeepAlivePingDelay = System.TimeSpan.FromSeconds(60),
                        EnableMultipleHttp2Connections = true
                    }
                };
                var appPath = AppDomain.CurrentDomain.BaseDirectory;
                var systemFolder = System.IO.Path.Combine(appPath, "system");
                string address = LoadIP(systemFolder);
                //string address = "http://192.168.1.247:8082";
                //  string address = "http://127.0.0.1:8082";
                _channel = GrpcChannel.ForAddress(address, channelOptions);
                // 创建客户端实例
                _commonClient = new CommonModelClient(_channel);
                _pipetteClient = new PipetteModuleClient(_channel);
                _dataEngineClient = new DataEngineClient(_channel);
                _MotorClient = new MotorModuleClient(_channel);
                _ShiftClient = new ShiftrModuleClient(_channel);
                _ScriptClient = new ScriptEngineClient(_channel);

                ShowNotification(_res.GrpcLoadSucc, NotificationControl.NotificationType.Info);
                // 测试连接
                //TestConnectionAsync().Wait();
            }
            catch (System.Exception ex)
            {
                ShowNotification($"{_res.GrpcLoadFail}: {ex.Message}", NotificationControl.NotificationType.Error);

                Application.Current.Shutdown();
            }
        }
        private string LoadIP(string systemFolder)
        {
            try
            {
                if (!Directory.Exists(systemFolder))
                {
                    Directory.CreateDirectory(systemFolder);
                }

                // 2. 定义IP配置文件路径
                var ipFilePath = System.IO.Path.Combine(systemFolder, "IP.ini");
                const string defaultIp = "http://192.168.100.10:8001"; // 默认IP地址

                if (!File.Exists(ipFilePath))
                {
                    File.WriteAllText(ipFilePath, defaultIp);
                    return defaultIp;
                }

                string ipContent = File.ReadAllText(ipFilePath).Trim();
                if (string.IsNullOrEmpty(ipContent))
                {
                    File.WriteAllText(ipFilePath, defaultIp);
                    return defaultIp;
                }

                if (Uri.IsWellFormedUriString(ipContent, UriKind.Absolute))
                {
                    return ipContent;
                }
                else
                {
                    File.WriteAllText(ipFilePath, defaultIp);
                    return defaultIp;
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"{_res.GrpcIPFail}: {ex.Message}", NotificationControl.NotificationType.Error);
                return "http://192.168.1.247:8082";
            }
        }
        private async Task LoadDeviceStatus()
        {
            try
            {
                var switchValues = await GetSwitchValuesAsync();

                LightFlag = switchValues.GetValueOrDefault("fill_light", -1f);
                UVFlag = switchValues.GetValueOrDefault("uv_lamp", -1f);

                Dispatcher.Invoke(() =>
                {
                    UpdateLightButtonStyle((int)LightFlag);
                    UpdateUVButtonStyle((int)UVFlag);
                });
                Dispatcher.Invoke(() =>
                {
                    ShowNotification(_res.GrpcDeviceLoadSucc, NotificationControl.NotificationType.Info);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowNotification($"{_res.GrpcDeviceLoadFail}: {ex.Message}", NotificationControl.NotificationType.Error);
                });
            }
        }
        //GRPC
        // 加载耗材数据
        private async Task LoadConsumablesData()
        {
            // 从数据库获取所有耗材设置（假设已有该方法）
            var allConsSettings = await databaseService.GetAllConsumablesAsync();
            foreach (var setting in allConsSettings)
            {
                Consumables.Add(new ConsumableItem
                {
                    Name = setting.name,
                    Settings = setting // 关联平面图数据
                });
            }
        }
        private async Task LoadLiquidsData()
        {
            // 从数据库获取所有液体设置（假设数据库服务有该方法）
            var allLiquidSettings = await databaseService.GetAllLiquidsAsync();
            foreach (var liquid in allLiquidSettings)
            {
                Liquids.Add(liquid); // 直接添加液体数据（LiquidSettings已实现INotifyPropertyChanged）
            }
        }
        public async void RefreshConsumablesAndLiquids()
        {
            // 清空现有耗材数据并重新加载
            Consumables.Clear();
            Liquids.Clear();

            await LoadConsumablesData(); // 复用已有的耗材加载方法
            await LoadLiquidsData();

            ShowNotification(_res.SettingDataSave, NotificationControl.NotificationType.Info);
        }

        // 启动拖拽（鼠标按下耗材项时）
        private void ConsumableItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is ConsumableItem consumable)
            {
                // 启动拖拽，传递耗材数据（包含平面图设置）
                DragDrop.DoDragDrop(border, consumable, DragDropEffects.Copy);
            }
        }
        // 拖拽进入板位时验证数据类型
        private void PlateSlot_DragEnter(object sender, DragEventArgs e)
        {
            // 只允许拖拽ConsumableItem类型的数据
            if (e.Data.GetDataPresent(typeof(ConsumableItem)))
            {
                e.Effects = DragDropEffects.Copy; // 允许复制
            }
            else
            {
                e.Effects = DragDropEffects.None; // 不允许拖拽
            }
        }

        // 拖拽完成后，在板位显示耗材平面图
        private void PlateSlot_Drop(object sender, DragEventArgs e)
        {
            if (sender is Border plateBorder &&
                e.Data.GetData(typeof(ConsumableItem)) is ConsumableItem consumable)
            {
                // 获取板位的Grid容器（用于显示平面图）
                var plateGrid = plateBorder.Child as Grid;
                if (plateGrid == null) return;

                // 清空板位原有内容（保留板位编号）
                plateGrid.Children.Clear();
                plateGrid.Children.Add(new TextBlock
                {
                    Text = $"P{plateBorder.Tag}", // 显示板位编号（如P1、P2）
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 5, 0, 0)
                });
                var plateId = plateBorder.Tag.ToString();

                // 添加耗材平面图到板位
                var canvas = new ConsumableCanvas
                {
                    ConsData = consumable.Settings, // 绑定耗材的平面图数据
                    Height = 250, // 板位内平面图高度
                    Width = 250,  // 板位内平面图宽度
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.Transparent,
                    PlateId = plateId
                };
                canvas.SelectedColumnsChanged += OnPlateColumnsSelected;

                plateGrid.Children.Add(canvas);

                // 记录板位与耗材的关联（用于后续操作）
                _plateConsumableMap[plateId] = consumable;
            }
        }
        // 新增：处理板位列选择事件，更新孔位输入框
        private void OnPlateColumnsSelected(string plateId, string columnText)
        {
            // 如果是当前选中的步骤关联的板位，才更新孔位输入框
            if (_currentSelectedPlateId == plateId &&
                   FlowSteps.FirstOrDefault(s => s.IsSelected) is FlowStep selectedStep)
            {
                // 更新孔位显示文本
                selectedStep.WellPosition = columnText;

                // 提取选中的列并保存（如"2,3,4"）
                var columns = _selectedColumnsFromText(columnText);
                selectedStep.SelectedColumns = string.Join(",", columns);
            }
        }
        private List<int> _selectedColumnsFromText(string text)
        {
            var result = new List<int>();
            if (string.IsNullOrEmpty(text)) return result;

            // 移除"列："前缀
            var content = text.Replace("列：", "");
            // 按分隔符拆分
            var parts = content.Split('；');
            foreach (var part in parts)
            {
                if (part.Contains("~"))
                {
                    // 处理范围（如"2~4"）
                    var range = part.Split('~').Select(int.Parse).ToList();
                    for (int i = range[0]; i <= range[1]; i++)
                        result.Add(i);
                }
                else
                {
                    // 处理单个列（如"6"）
                    result.Add(int.Parse(part));
                }
            }
            return result;
        }
        // 鼠标进入板位时记录
        private void PlateSlot_MouseEnter(object sender, MouseEventArgs e)
        {
            _currentHoveredPlate = sender as Border;
            _currentHoveredPlate.Focus(); // 获取焦点，确保能接收键盘事件
        }

        // 鼠标离开板位时清除记录
        private void PlateSlot_MouseLeave(object sender, MouseEventArgs e)
        {
            _currentHoveredPlate = null;
        }

        // 处理键盘按键事件
        private void PlateSlot_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 只处理Delete键
            if ((e.Key == Key.Delete || e.Key == Key.Back) && _currentHoveredPlate != null)
            {
                // 获取板位ID（如"P1"中的"1"）
                string plateId = _currentHoveredPlate.Tag.ToString();

                // 清除板位内容
                ClearPlateContent(plateId);

                // 从映射中移除关联
                if (_plateConsumableMap.ContainsKey(plateId))
                {
                    _plateConsumableMap.Remove(plateId);
                }

                e.Handled = true; // 标记事件已处理，避免冒泡
            }
        }

        // 清除板位内容的方法
        private void ClearPlateContent(string plateId)
        {
            // 根据板位ID获取对应的Grid
            if (this.FindName($"PlateGrid{plateId}") is Grid plateGrid)
            {
                // 清空Grid内容，只保留板位编号文本
                plateGrid.Children.Clear();
                plateGrid.Children.Add(new TextBlock
                {
                    Text = $"P{plateId}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0)
                });
            }
        }
        // 点击动作功能区按钮时添加流程步骤
        private void AddFlowStep(string type)
        {
            int endStepIndex = FlowSteps.Count - 1;

            var step = new FlowStep
            {
                Index = _stepIndex++,
                //Name = $"{type} Steps {_stepIndex - 1}",
                Type = type,
                Volume = 50, // 默认体积
                Position = "P1", // 默认位置
                IsSelected = false,
                IsSystemStep = false
            };
            FlowSteps.Insert(endStepIndex, step);

            // 重新编号所有步骤（确保序号连续）
            RebuildStepIndexes();
        }

        // 点击流程步骤显示详情
        private void FlowStep_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is FlowStep step)
            {
                if (step.IsSystemStep)
                {
                    return;
                }
                foreach (var s in FlowSteps)
                    s.IsSelected = false;

                // 选中当前步骤
                step.IsSelected = true;
                ShowStepDetail(step);
            }
            foreach (var plateId in _plateConsumableMap.Keys)
            {
                if (this.FindName($"PlateGrid{plateId}") is Grid plateGrid)
                {
                    foreach (var child in plateGrid.Children)
                    {
                        if (child is ConsumableCanvas canvas)
                        {
                            canvas.ClearSelection();
                        }
                    }
                }
            }
        }

        // 显示步骤详情（后续可扩展为不同步骤类型的布局）
        // 显示步骤详情（修改后）
        private void ShowStepDetail(FlowStep step)
        {
            //RunInfoView.Visibility = Visibility.Collapsed;
            //StepDetailView.Visibility = Visibility.Visible;
            // 清空现有详情
            StepDetailPanel.Children.Clear();
            var res = ResourceHelper.Instance;
            string stepTypeText = step.Type switch
            {
                "Wait" => res.FlowStepWaitContent, // 等待步骤（复用之前的等待文本）
                "Aspirate" => res.WindowActionAspirate,
                "Dispense" => res.WindowActionDispense,
                "TipOn" => res.WindowActionTipOn,
                "TipOff" => res.WindowActionTipOff,
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
                    Margin = new Thickness(5, 0, 0, 0),
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
                // 等待内容描述
                var waitContentTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 140,
                    Height = 80,
                    Margin = new Thickness(5, 0, 0, 0),
                    AcceptsReturn = true, // 允许换行
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalAlignment = VerticalAlignment.Center
                };
                waitContentTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("WaitContent"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailWaitDesc, waitContentTextBox));
                // 添加提示文本
                StepDetailPanel.Children.Add(new TextBlock
                {
                    //Text = "提示: 等待时间将自动转换为毫秒执行",
                    Text = res.StepDetailWaitNote,
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 5, 0, 0)
                });
                return;
            }
            // -------------------------- 通用控件（吸液/注液/取头/退头） --------------------------
            // 创建位置下拉框并绑定
            var positionCombo = new ComboBox
            {
                Style = (Style)FindResource("InputComboBoxStyle"),
                ItemsSource = new List<string> { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12" },
                Width = 140,
                Margin = new Thickness(5, 0, 0, 0),
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
                var columns = _selectedColumnsFromText(columnText);
                step.SelectedColumns = string.Join(",", columns);
            };
            // 耗材名称显示控件
            TextBlock consumableNameText = new TextBlock
            {
                FontSize = 14,
                Margin = new Thickness(5, 5, 0, 5)
            };
            // 位置下拉框选择变更时校验耗材类型
            positionCombo.SelectionChanged += (s, e) =>
            {
                if (positionCombo.SelectedItem is string newPosition)
                {
                    // 更新当前选中的板位ID
                    _currentSelectedPlateId = newPosition.Replace("P", "");
                    wellSelectionCanvas.PlateId = _currentSelectedPlateId;
                    wellSelectionCanvas.ClearSelection();                    // 清空之前的选择
                    wellSelectionCanvas.IsInteractive = false;

                    // 绑定画布的耗材数据（从板位映射中获取）
                    if (_plateConsumableMap.TryGetValue(_currentSelectedPlateId, out var consumable))
                    {
                        // 显示当前耗材名称
                        consumableNameText.Text = string.Format(res.StepDetailCurrentCons, consumable.Name);

                        consumableNameText.Foreground = Brushes.DarkSlateGray;
                        wellSelectionCanvas.ConsData = consumable.Settings;  // 关联当前板位的耗材数据
                        int consType = consumable.Settings.type;
                        if ((step.Type == "Aspirate" || step.Type == "Dispense"))
                        {
                            // 吸液/注液允许：0（微孔板）、1（储液槽）
                            if (consType == 0 || consType == 1)
                            {
                                wellSelectionCanvas.IsInteractive = true;
                            }
                            else
                            {
                                ShowNotification(res.StepDetailAspDispConsTip, NotificationControl.NotificationType.Warn); // 替换通知
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
                                ShowNotification(res.StepDetailTipOnOffConsTip, NotificationControl.NotificationType.Warn); // 替换通知
                            }
                        }
                    }
                    else
                    {
                        wellSelectionCanvas.ConsData = null;  // 无耗材时清空
                        step.WellPosition = "";
                        step.SelectedColumns = "";
                        consumableNameText.Text = "";
                    }
                }
            };
            StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailOperationPosition, positionCombo));
            // 孔位选择（所有步骤通用）
            var wellPositionTextBox = new TextBox
            {
                Style = (Style)FindResource("InputTextBoxStyle"),
                Width = 140, // 调整宽度（根据实际需求）
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center // 与其他输入控件保持一致
            };
            wellPositionTextBox.SetBinding(TextBox.TextProperty, new Binding
            {
                Source = step,
                Path = new PropertyPath("WellPosition"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailWellPosition, wellPositionTextBox));

            StepDetailPanel.Children.Add(new TextBlock
            {
                Text = res.StepDetailWellSelectionArea, // “孔位选择区：”/“Well Position Selection Area:”
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 5, 0, 2) // 与上方控件间距5，与下方耗材名称间距2
            });

            // 2. 耗材名称（单独一行，在标题下方）
            consumableNameText = new TextBlock
            {
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5), // 与下方画布间距5
                HorizontalAlignment = HorizontalAlignment.Left, // 与标题左对齐
                Foreground = Brushes.DarkSlateGray
            };
            StepDetailPanel.Children.Add(consumableNameText);

            StepDetailPanel.Children.Add(wellSelectionCanvas);

            // 体积输入（吸液/注液特有）
            if (step.Type == "Aspirate" || step.Type == "Dispense")
            {
                // 创建体积输入框并绑定
                var volumeTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 200,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                // 绑定到step.Volume（双向）
                volumeTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("Volume"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged // 输入时立即同步
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailVolume, volumeTextBox));
                // #################### 新增：混合项控件 ####################
                var mixHeaderRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 10, 0, 5), // 与上方控件保持间距
                    VerticalAlignment = VerticalAlignment.Center
                };

                // 混合设置标题
                mixHeaderRow.Children.Add(new TextBlock
                {
                    Text = res.StepDetailMixedSettings,
                    FontWeight = FontWeights.SemiBold,
                    Width = 140, // 与其他标签宽度一致
                    FontSize = 14
                });

                // 混合Checkbox（直接放在标题右侧）
                var mixCheckBox = new CheckBox
                {
                    Content = res.StepDetailEnableMix,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 0, 0) // 与标题保持小间距
                };
                mixCheckBox.SetBinding(CheckBox.IsCheckedProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("IsMixEnabled"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

                mixHeaderRow.Children.Add(mixCheckBox);
                StepDetailPanel.Children.Add(mixHeaderRow);
                // 2. 混合次数
                var mixCountTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 200,
                    Margin = new Thickness(5, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                mixCountTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("MixCount"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                // 绑定启用状态到Checkbox
                mixCountTextBox.SetBinding(TextBox.IsEnabledProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("IsMixEnabled")
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailMixCount, mixCountTextBox));

                // 3. 混合体积
                var mixVolumeTextBox = new TextBox
                {
                    Style = (Style)FindResource("InputTextBoxStyle"),
                    Width = 200,
                    Margin = new Thickness(5, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                mixVolumeTextBox.SetBinding(TextBox.TextProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("MixVolume"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                // 绑定启用状态到Checkbox
                mixVolumeTextBox.SetBinding(TextBox.IsEnabledProperty, new Binding
                {
                    Source = step,
                    Path = new PropertyPath("IsMixEnabled")
                });
                StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailMixVolume, mixVolumeTextBox));

                if (step.Type == "Dispense")
                {
                    // 创建第一次体积输入框并绑定
                    var FirstvolumeTextBox = new TextBox
                    {
                        Style = (Style)FindResource("InputTextBoxStyle"),
                        Width = 200,
                        Margin = new Thickness(5, 0, 0, 0)
                    };
                    // 绑定到step.FirstVol（双向）
                    FirstvolumeTextBox.SetBinding(TextBox.TextProperty, new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("FirstVol"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged // 输入时立即同步
                    });
                    StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailInitialVol, FirstvolumeTextBox));
                    // 创建第一次延时输入框并绑定
                    var FirstdelayTextBox = new TextBox
                    {
                        Style = (Style)FindResource("InputTextBoxStyle"),
                        Width = 200,
                        Margin = new Thickness(5, 0, 0, 0)
                    };
                    // 绑定到step.FirstDelay（双向）
                    FirstdelayTextBox.SetBinding(TextBox.TextProperty, new Binding
                    {
                        Source = step,
                        Path = new PropertyPath("FirstDelay"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged // 输入时立即同步
                    });
                    StepDetailPanel.Children.Add(CreateDetailRow(res.StepDetailInitialDelay, FirstdelayTextBox));
                }
                // #################### 新增：液体参数选择与显示 ####################
                var liquidHeaderRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 10, 0, 5),
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
                    ItemsSource = Liquids,
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
                liquidHeaderRow.Children.Add(liquidCombo);

                StepDetailPanel.Children.Add(liquidHeaderRow);

                // 液体参数总容器（与下拉框左对齐，保持同一列）
                StackPanel liquidParamsContainer = new StackPanel
                {
                    Margin = new Thickness(0, 5, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch // 拉伸填满父容器宽度
                };

                // 吸液参数组
                liquidParamsContainer.Children.Add(new TextBlock
                {
                    Text = res.StepDetailAspirationParams,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 5, 0, 3),
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
                var paramControl1 = CreateParamRow(res.StepDetailAspAirB, nameof(LiquidSettings.aisAirB), step);
                Grid.SetRow(paramControl1, 0);
                aspirateRow1.Children.Add(paramControl1);
                aspirateParams.Children.Add(aspirateRow1);

                // 第二行：Air Aspiration After Aspiration
                var aspirateRow2 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                aspirateRow2.RowDefinitions.Add(new RowDefinition());
                var paramControl2 = CreateParamRow(res.StepDetailAspAirA, nameof(LiquidSettings.aisAirA), step);
                Grid.SetRow(paramControl2, 0);
                aspirateRow2.Children.Add(paramControl2);
                aspirateParams.Children.Add(aspirateRow2);
                // 第三行：Aspiration Speed
                var aspirateRow3 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                aspirateRow3.RowDefinitions.Add(new RowDefinition());
                var paramControl3 = CreateParamRow(res.StepDetailAspSpeed, nameof(LiquidSettings.aisSpeed), step);
                Grid.SetRow(paramControl3, 0);
                aspirateRow3.Children.Add(paramControl3);
                aspirateParams.Children.Add(aspirateRow3);
                // 第四行：Aspiration Delay
                var aspirateRow4 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                aspirateRow4.RowDefinitions.Add(new RowDefinition());
                var paramControl4 = CreateParamRow(res.StepDetailAspDelay, nameof(LiquidSettings.aisDelay), step);
                Grid.SetRow(paramControl4, 0);
                aspirateRow4.Children.Add(paramControl4);
                aspirateParams.Children.Add(aspirateRow4);
                // 第五行：Aspiration Distance
                var aspirateRow5 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                aspirateRow5.RowDefinitions.Add(new RowDefinition());
                var paramControl5 = CreateParamRow(res.StepDetailAspDist, nameof(LiquidSettings.aisDistance), step);
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
                var paramDisControl1 = CreateParamRow(res.StepDetailDispAirB, nameof(LiquidSettings.disAirB), step);
                Grid.SetRow(paramDisControl1, 0);
                dispenseRow1.Children.Add(paramDisControl1);
                dispenseParams.Children.Add(dispenseRow1);
                // 第二行：注液后吸空气
                var dispenseRow2 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                dispenseRow2.RowDefinitions.Add(new RowDefinition());
                var paramDisControl2 = CreateParamRow(res.StepDetailDispAirA, nameof(LiquidSettings.disAirA), step);
                Grid.SetRow(paramDisControl2, 0);
                dispenseRow2.Children.Add(paramDisControl2);
                dispenseParams.Children.Add(dispenseRow2);
                // 第二行：2个参数
                var dispenseRow3 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                dispenseRow3.RowDefinitions.Add(new RowDefinition());
                var paramDisControl3 = CreateParamRow(res.StepDetailDispSpeed, nameof(LiquidSettings.disSpeed), step);
                Grid.SetRow(paramDisControl3, 0);
                dispenseRow3.Children.Add(paramDisControl3);
                dispenseParams.Children.Add(dispenseRow3);
                var dispenseRow4 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                dispenseRow4.RowDefinitions.Add(new RowDefinition());
                var paramDisControl4 = CreateParamRow(res.StepDetailDispDelay, nameof(LiquidSettings.disDelay), step);
                Grid.SetRow(paramDisControl4, 0);
                dispenseRow4.Children.Add(paramDisControl4);
                dispenseParams.Children.Add(dispenseRow4);
                // 第三行：1个参数
                var dispenseRow5 = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                dispenseRow5.RowDefinitions.Add(new RowDefinition());
                var paramDisControl5 = CreateParamRow(res.StepDetailDispDist, nameof(LiquidSettings.disDistance), step);
                Grid.SetRow(paramDisControl5, 0);
                dispenseRow5.Children.Add(paramDisControl5);
                dispenseParams.Children.Add(dispenseRow5);

                liquidParamsContainer.Children.Add(dispenseParams);

                StepDetailPanel.Children.Add(liquidParamsContainer);
            }

            // 初始化画布数据（首次加载时）
            _currentSelectedPlateId = step.Position.Replace("P", "");
            wellSelectionCanvas.PlateId = _currentSelectedPlateId;
            if (_plateConsumableMap.TryGetValue(_currentSelectedPlateId, out var initConsumable))
            {
                wellSelectionCanvas.ConsData = initConsumable.Settings;
            }
            // 恢复步骤保存的选中列状态
            if (!string.IsNullOrEmpty(step.SelectedColumns))
            {
                var columns = step.SelectedColumns.Split(',')
                    .Select(int.Parse)
                    .ToList();
                wellSelectionCanvas.SetSelectedColumns(columns);
            }

        }
        private void ClearAllPlateSelections()
        {
            foreach (var plateId in _plateConsumableMap.Keys)
            {
                if (this.FindName($"PlateGrid{plateId}") is Grid plateGrid)
                {
                    var canvas = plateGrid.Children.OfType<ConsumableCanvas>().FirstOrDefault();
                    canvas?.ClearSelection();
                }
            }
        }
        // 创建液体参数显示行（只读）
        private Grid CreateParamRow(string label, string propertyName, FlowStep step)
        {
            // 用Grid替代StackPanel，布局更灵活（避免水平StackPanel的宽度限制）
            var grid = new Grid();
            // 定义两列：标签列（自适应内容宽度）、参数值列（占剩余空间）
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto,       // 标签列宽度跟随文本内容自动调整
                MinWidth = 175                 // 保留最小宽度（避免过窄，可根据需要调整）
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)  // 参数值列占剩余全部空间
            });

            // 标签文本（允许换行，避免超长截断）
            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 13,
                Margin = new Thickness(0, 2, 5, 2),  // 右侧留间距，与参数值分开
                TextWrapping = TextWrapping.Wrap,    // 核心：文本超长时自动换行
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelText, 0);  // 放在第一列
            grid.Children.Add(labelText);

            // 参数值文本（占剩余空间，避免固定宽度限制）
            var paramValueText = new TextBlock
            {
                FontSize = 13,
                Margin = new Thickness(5, 2, 0, 2),
                VerticalAlignment = VerticalAlignment.Center
                // 去掉固定Width=175，改为随列宽自适应
            };
            // 保持原有的数据绑定逻辑
            paramValueText.SetBinding(TextBlock.TextProperty, new Binding
            {
                Source = step,
                Path = new PropertyPath($"SelectedLiquid.{propertyName}"),
                StringFormat = "{0:F2}"
            });
            Grid.SetColumn(paramValueText, 1);  // 放在第二列
            grid.Children.Add(paramValueText);

            return grid;  // 返回Grid容器
        }
        //private StackPanel CreateParamRow(string label, string propertyName, FlowStep step, double width)
        //{
        //    var paramValueText = new TextBlock
        //    {
        //        FontSize = 13,
        //        Margin = new Thickness(5, 2, 0, 2),
        //        Width = 175 // 固定值宽度
        //    };
        //    paramValueText.SetBinding(TextBlock.TextProperty, new Binding
        //    {
        //        Source = step,
        //        Path = new PropertyPath($"SelectedLiquid.{propertyName}"),
        //        StringFormat = "{0:F2}"
        //    });

        //    return new StackPanel
        //    {
        //        Orientation = Orientation.Horizontal,
        //        Width = width, // 每个参数项总宽度（标签+值）
        //        Children =
        //{
        //    new TextBlock { Text = label, Width = 175, FontSize = 13 }, // 固定标签宽度
        //    paramValueText
        //}
        //    };
        //}
        private void UpdatePlateInteractivity(string targetPlateId)
        {
            // 遍历所有可能的板位（1-12）
            for (int i = 1; i <= 12; i++)
            {
                string plateId = i.ToString();
                // 找到板位对应的Grid
                if (this.FindName($"PlateGrid{plateId}") is Grid plateGrid)
                {
                    // 找到Grid中的ConsumableCanvas
                    var canvas = plateGrid.Children.OfType<ConsumableCanvas>().FirstOrDefault();
                    if (canvas != null)
                    {
                        // 只有目标板位允许交互
                        canvas.IsInteractive = plateId == targetPlateId;
                    }
                }
            }
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
            new TextBlock { Text = labelText ,FontSize = 14},
            inputControl
        }
            };
        }


        private void AspirateButton_Click(object sender, RoutedEventArgs e)
        {
            //AddFlowStep("吸液");
            AddFlowStep("Aspirate");

        }

        private void DispenseButton_Click(object sender, RoutedEventArgs e)
        {
            //AddFlowStep("注液");
            AddFlowStep("Dispense");
        }

        private void PickTipButton_Click(object sender, RoutedEventArgs e)
        {
            //AddFlowStep("取头");
            AddFlowStep("TipOn");

        }

        private void EjectTipButton_Click(object sender, RoutedEventArgs e)
        {
            //AddFlowStep("退头");
            AddFlowStep("TipOff");
        }

        private void WaitButton_Click(object sender, RoutedEventArgs e)
        {
            //AddFlowStep("等待");
            AddFlowStep("Wait");
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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
        //设置界面
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建弹窗实例
            var settingsDialog = new PlateSettingsDialog(this);

            // 使用Window作为容器显示弹窗（确保弹窗可模态显示）
            var dialogWindow = new Window
            {
                Width = 1300,
                Height = 750,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = false,
                Content = settingsDialog,
            };

            // 显示模态弹窗
            dialogWindow.ShowDialog();
        }
        public void ShowNotification(string message, NotificationControl.NotificationType type, int duration = 3000)
        {
            Dispatcher.Invoke(() =>
            {
                // 限制最大通知数量
                if (NotificationHost.Children.Count >= MAX_NOTIFICATIONS)
                {
                    // 移除最旧的通知
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



                // 更新通知位置
                UpdateNotificationPositions();
            });
        }

        private void UpdateNotificationPositions()
        {
            // 计算新通知应该出现的位置（在现有通知下方）
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

        // 监听流程列表的键盘事件（删除选中步骤）
        private void FlowList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Back && e.Key != Key.Delete)
                return;


            var selectedStep = FlowSteps.FirstOrDefault(step => step.IsSelected);
            if (selectedStep == null)
                return;
            if (selectedStep.IsSystemStep)
            {
                ShowNotification(_res.GrpcStartEndRemove, NotificationControl.NotificationType.Warn);
                e.Handled = true;
                return;
            }
            FlowSteps.Remove(selectedStep);

            RebuildStepIndexes();

            StepDetailPanel.Children.Clear();

            e.Handled = true;
        }

        // 重新编号步骤（删除后保持序号连续）
        private void RebuildStepIndexes()
        {
            for (int i = 0; i < FlowSteps.Count; i++)
            {
                FlowSteps[i].Index = i + 1;
                //if (!FlowSteps[i].IsSystemStep)
                //{
                //    FlowSteps[i].Name = $"{FlowSteps[i].Type} Steps {i + 1}";
                //}
            }
            _stepIndex = FlowSteps.Count + 1;
        }
        //创建板位脚本
        private JArray BuildCreaList()
        {
            var creaList = new JArray();

            // 遍历板位-耗材映射关系(对应C++的nowCreaList)
            foreach (var plateConsumable in _plateConsumableMap)
            {
                string plateId = plateConsumable.Key; // 板位编号
                var consumable = plateConsumable.Value; // 耗材信息
                var settings = consumable.Settings; // 耗材详细设置

                // 创建单个耗材JSON对象
                var creaItem = new JObject();
                creaItem["plate"] = plateId; // 板位ID

                // 构建耗材参数(对应C++的crea_param)
                var creaParam = new JObject
        {
            // 基本信息
            {"name", settings.name ?? ""},
            {"id", settings.id},
            {"type", settings.type},
            {"description", settings.description ?? ""},

            // 坐标信息
            {"NW", settings.NW},
            {"SW", settings.SW},
            {"NE", settings.NE},
            {"SE", settings.SE},

            // 孔阵信息
            {"numRows", settings.numRows},
            {"numColumns", settings.numColumns},

            // 尺寸信息
            {"labL", settings.labL},
            {"labW", settings.labW},
            {"labH", settings.labH},

            // 孔距与偏移
            {"distanceRowY", settings.distanceRowY},
            {"distanceColumnX", settings.distanceColumnX},
            {"distanceRow", settings.distanceRow},
            {"distanceColumn", settings.distanceColumn},
            {"offsetX", settings.offsetX},
            {"offsetY", settings.offsetY},

            // 抓手位置
            {"RobotX", settings.RobotX},
            {"RobotY", settings.RobotY},
            {"RobotZ", settings.RobotZ},

            // 容量信息
            {"labVolume", settings.labVolume},
            {"consMaxAvaiVol", settings.consMaxAvaiVol},
            {"consDep", settings.consDep},

            // 孔形状信息
            {"topShape", settings.topShape},
            {"topRadius", settings.topRadius},
            {"topUpperX", settings.topUpperX},
            {"topUpperY", settings.topUpperY},

            // 枪头信息
            {"TIPMAXCapacity", settings.TIPMAXCapacity},
            {"TIPMAXAvailable", settings.TIPMAXAvailable},
            {"TIPTotalLength", settings.TIPTotalLength},
            {"TIPHeadHeight", settings.TIPHeadHeight},
            {"TIPConeLength", settings.TIPConeLength},
            {"TIPMAXRadius", settings.TIPMAXRadius},
            {"TIPMINRadius", settings.TIPMINRadius},
            {"TIPDepthOFComp", settings.TIPDepthOFComp}
        };

                creaItem["crea_param"] = creaParam;
                creaList.Add(creaItem);
            }

            return creaList;
        }
        //创建脚本
        /// <summary>
        /// 创建脚本JSON（对应C++的creaScript方法）
        /// </summary>
        /// <returns>生成的脚本JSON字符串</returns>
        private string CreateScriptJson()
        {
            // 清空之前的步骤名称记录（对应C++的m_stepNames.clear()）
            var stepNames = new Dictionary<int, string>();
            int nowStepName = 0;
            float emptyVolume = 0; // 跟踪吸液/注液体积（对应C++的emptyVolume）

            // 创建脚本根对象
            var script = new JObject();
            script["creator"] = "Admin";
            script["description"] = "样本前处理流程步骤";
            script["script_name"] = "样本前处理流程";
            script["script_steps"] = FlowSteps.Count;

            // 创建步骤列表（对应C++的stepList）
            var stepList = new JArray();
            foreach (var flowStep in FlowSteps)
            {
                var step = new JObject();
                string stepType = MapStepType(flowStep.Type); // 映射步骤类型（吸液→aspirate等）
                step["type"] = stepType;
                stepNames[nowStepName] = stepType;
                nowStepName++;

                // 根据步骤类型处理不同参数
                switch (stepType)
                {
                    case "start":
                        step["start_step"] = 1;
                        break;

                    case "end":
                        step["is_reset"] = "false";
                        break;

                    case "aspirate":
                        // 处理板位
                        step["plate"] = MapPlatePosition(flowStep.Position);

                        // 构建吸液参数
                        var aspirateParams = new JObject();
                        aspirateParams["row"] = 1; // 固定行=1（8通道）
                        aspirateParams["col"] = ExtractColumnFromWellPosition(flowStep.WellPosition); // 从孔位提取列
                        aspirateParams["pipette"] = "pipette_1";
                        aspirateParams["volume"] = flowStep.Volume;
                        aspirateParams["liquid_dete"] = "off";
                        aspirateParams["liquid_follow"] = "true";
                        aspirateParams["mix_volume"] = flowStep.MixVolume;
                        aspirateParams["mix_count"] = flowStep.MixCount;
                        step["aspirate_param"] = aspirateParams;

                        // 累加吸液体积（对应C++的emptyVolume +=）
                        emptyVolume += flowStep.Volume;

                        // 添加耗材信息（labcons_info）
                        step["labcons_info"] = CreateLabconsInfo(flowStep.Position);

                        // 添加液体信息（liquid_info）
                        step["liquid_info"] = CreateLiquidInfo(flowStep, 1);
                        break;

                    case "dispense":
                        // 处理板位
                        step["plate"] = MapPlatePosition(flowStep.Position);

                        // 构建注液参数（对应C++的dispense_param）
                        var dispenseParams = new JObject();
                        dispenseParams["row"] = 1;
                        dispenseParams["col"] = ExtractColumnFromWellPosition(flowStep.WellPosition);
                        dispenseParams["pipette"] = "pipette_1";
                        dispenseParams["volume"] = flowStep.Volume;

                        // 处理体积（支持排空功能，对应C++的empty_the_gun逻辑）
                        //if (flowStep.IsEmptyGun && emptyVolume > 0)
                        //{
                        //    dispenseParams["volume"] = emptyVolume;
                        //    emptyVolume = 0;
                        //}
                        //else
                        //{
                        //    dispenseParams["volume"] = flowStep.Volume;
                        //    emptyVolume -= flowStep.Volume;
                        //}
                        dispenseParams["liquid_dete"] = "off";
                        dispenseParams["first_vol"] = flowStep.FirstVol;//第一次润洗的体积
                        dispenseParams["first_delay"] = flowStep.FirstDelay;//第一次润洗后的延时

                        //dispenseParams["is_liquid_follow"] = "false";//边排液边上升
                        //dispenseParams["follow_psition"] = 10;//边排液边上升
                        //dispenseParams["follow_speed"] = 1;//边排液边上升
                        dispenseParams["mix_volume"] = flowStep.MixVolume;
                        dispenseParams["mix_count"] = flowStep.MixCount;
                        dispenseParams["empty_the_gun"] = 0;
                        //dispenseParams["empty_the_gun"] = flowStep.IsEmptyGun ? 1 : 0;
                        step["dispense_param"] = dispenseParams;

                        // 添加耗材信息和液体信息
                        step["labcons_info"] = CreateLabconsInfo(flowStep.Position);
                        step["liquid_info"] = CreateLiquidInfo(flowStep, 2);
                        break;

                    case "tipon": // 取头
                    case "tipoff": // 退头
                                   // 处理板位
                        step["plate"] = MapPlatePosition(flowStep.Position);

                        // 构建取头/退头参数
                        var tipParams = new JObject();
                        tipParams["row"] = 1;
                        tipParams["col"] = ExtractColumnFromWellPosition(flowStep.WellPosition);
                        tipParams["pipette"] = "pipette_1";
                        step[$"{stepType}_param"] = tipParams;

                        // 添加耗材信息（对应C++的labcons_info）
                        step["labcons_info"] = CreateTipLabconsInfo(flowStep.Position);
                        break;

                    case "wait":
                        // 构建等待参数（对应C++的wait_param）
                        var waitParams = new JObject();

                        // 转换秒为毫秒（处理0值情况，默认24小时）
                        int waitTimeMs = flowStep.WaitTime * 1000;
                        if (waitTimeMs <= 0)
                        {
                            waitTimeMs = 86400 * 1000; // 24小时默认值
                        }
                        waitParams["time_ms"] = waitTimeMs;

                        // 等待内容描述
                        waitParams["contents"] = flowStep.WaitContent;

                        step["wait_param"] = waitParams;
                        break;
                }

                stepList.Add(step);
            }

            // 将步骤列表添加到脚本
            script["step_list"] = stepList;
            script["crea_list"] = BuildCreaList();

            // 转换为格式化的JSON字符串
            return JsonConvert.SerializeObject(script, Newtonsoft.Json.Formatting.Indented);
        }
        private string MapStepType(string flowStepType)
        {
            return flowStepType switch
            {
                "吸液" => "aspirate",
                "注液" => "dispense",
                "取头" => "tipon",
                "退头" => "tipoff",
                "等待" => "wait",
                "开始" => "start",
                "结束" => "end",
                "震荡" => "shaker",
                "磁分离" => "magnetic",
                "转移" => "shift",
                _ => flowStepType.ToLower()
            };
        }
        //映射板位
        private string MapPlatePosition(string position)
        {
            // 假设position格式为"P3"、"P9"、"P1"等
            string plateId = position.Replace("P", "");
            if (plateId == "3") return "magnetic_1"; // 假设P3对应magnetic_1
            if (plateId == "9") return "shaker_1";   // 假设P9对应shaker_1
            return "p" + plateId;
        }
        // 辅助方法：从孔位文本提取列号（对应C++的正则解析）
        private int ExtractColumnFromWellPosition(string wellPosition)
        {
            if (string.IsNullOrEmpty(wellPosition)) return 1;

            // 示例："列：2~4" → 提取2；"列：6" → 提取6
            var regex = new System.Text.RegularExpressions.Regex(@"列：\s*(\d+)");
            var match = regex.Match(wellPosition);
            return match.Success ? int.Parse(match.Groups[1].Value) : 1;
        }
        // 辅助方法：创建耗材信息（labcons_info）
        private JObject CreateLabconsInfo(string position)
        {
            string plateId = position.Replace("P", "");
            // 从板位映射获取耗材信息（对应C++的nowCreaList）
            if (_plateConsumableMap.TryGetValue(plateId, out var consumable))
            {
                var consData = consumable.Settings;
                return new JObject
        {
            {"name", consData.name},
            {"type", "container"},
            {"pipette_x", 0},
            {"pipette_y", 0},
            {"pipette_z", 0},
            {"shift_x", 0},
            {"shift_y", 0},
            {"shift_z", 0},
            {"margin_1", consData.offsetX}, // 左边距
            {"margin_2", consData.offsetY}, // 上边距
            {"height", consData.labH},
            {"depth", consData.consDep},
            {"row", consData.numRows},
            {"col", consData.numColumns},
            {"span_row", consData.distanceRow},
            {"span_col", consData.distanceColumn}
        };
            }

            // 默认耗材信息
            return new JObject
    {
        {"name", "未知耗材"},
        {"type", "container"},
        {"margin_1", 0},
        {"margin_2", 0},
        {"row", 8},
        {"col", 12}
    };
        }
        // 辅助方法：创建液体信息（liquid_info）
        private JObject CreateLiquidInfo(FlowStep step, int type)
        {
            var liquid = step.SelectedLiquid ?? new LiquidSettings(); // 使用选中的液体参数
            if (type == 1)//吸液
            {
                return new JObject
    {
        {"name", liquid.name ?? "默认液体"},
        {"density", 1000},
        {"aspirate_speed", liquid.aisSpeed},
        {"aspirate_air_before", liquid.aisAirB},
        {"aspirate_air_after", liquid.aisAirA},
        {"dispense_speed", liquid.disSpeed},
        {"dispense_air_before", liquid.disAirB},
        {"dispense_air_after", liquid.disAirA},
        {"aspirate_suction_delay", liquid.aisDelay},
        {"aspirate_dispense_delay", liquid.disDelay},
        {"aspirate_distance_to_port_bottom", liquid.aisDistance}
    };
            }
            else if (type == 2)
            {
                return new JObject
    {
        {"name", liquid.name ?? "默认液体"},
        {"density", 1000},
        {"aspirate_speed", liquid.aisSpeed},
        {"aspirate_air_before", liquid.aisAirB},
        {"aspirate_air_after", liquid.aisAirA},
        {"dispense_speed", liquid.disSpeed},
        {"dispense_air_before", liquid.disAirB},
        {"dispense_air_after", liquid.disAirA},
        {"aspirate_suction_delay", liquid.aisDelay},
        {"aspirate_dispense_delay", liquid.disDelay},
        {"aspirate_distance_to_port_bottom", liquid.disDistance}
    };
            }
            else
            {
                // 处理无效 type：返回默认对象或抛出异常
                throw new ArgumentException($"无效的操作类型：{type}", nameof(type));
                // 或返回默认值：return new JObject();
            }

        }
        // 辅助方法：创建吸头耗材信息（针对tipon/tipoff）
        private JObject CreateTipLabconsInfo(string position)
        {
            string plateId = position.Replace("P", "");
            // 废液槽特殊处理（对应C++的"p12"逻辑）
            if (plateId == "12")
            {
                return new JObject
        {
            {"name", "废液槽"},
            {"type", "tip"},
            {"pipette_x", 0},
            {"pipette_y", 0},
            {"pipette_z", 0},
            {"shift_x", 0},
            {"shift_y", 0},
            {"shift_z", 0},
            {"margin_1", 35},
            {"margin_2", 0},
            {"margin_3", 35},
            {"margin_4", 35},
            {"height", 75},
            {"tip_height", 75},
            {"depth", 10},
            {"row", 1},
            {"col", 1},
            {"span_row", 0},
            {"span_col", 0}
        };
            }

            // TIP盒耗材信息
            if (_plateConsumableMap.TryGetValue(plateId, out var consumable))
            {
                var consData = consumable.Settings;
                return new JObject
        {
            {"name", consData.name},
            {"type", "tip"},
            {"pipette_x", 0},
            {"pipette_y", 0},
            {"pipette_z", 0},
            {"shift_x", 0},
            {"shift_y", 0},
            {"shift_z", 0},
            {"margin_1", consData.offsetX},
            {"margin_2", consData.offsetY},
            {"height", consData.labH},
            {"tip_height", consData.TIPTotalLength - consData.TIPDepthOFComp},
            {"depth", consData.TIPDepthOFComp},
            {"row", consData.numRows},
            {"col", consData.numColumns},
            {"span_row", consData.distanceRow},
            {"span_col", consData.distanceColumn}
        };
            }

            // 默认吸头信息
            return new JObject
    {
        {"name", "tip96"},
        {"type", "tip"},
        {"margin_1", 14.38},
        {"margin_2", 11.24},
        {"height", 60.8},
        {"tip_height", 50},
        {"depth", 10},
        {"row", 8},
        {"col", 12},
        {"span_row", 9},
        {"span_col", 9}
    };
        }
        //创建脚本
        //初始化
        private async void InitButton_Click(object sender, RoutedEventArgs e)
        {
            var switchValues = await GetSwitchValuesAsync();
             ShowNotification(_res.GrpcInitStart, NotificationControl.NotificationType.Info);

            DoorFlag = switchValues.GetValueOrDefault("door_lock", -1f);
            if (DoorFlag == 0)
            {
                if (!runFlag && !pauseFlag)
                {
                    ShowNotification(_res.GrpcIniting, NotificationControl.NotificationType.Info);
                    var motorActionsX = new List<MotorActionParams>();
                    motorActionsX.Add(new MotorActionParams
                    {
                        MotorId = 0,
                        ActionType = 4,
                        Target = 0,
                        Speed = 300.0f,
                        Acc = 500.0f,
                        Dcc = 500.0f
                    });
                    var motorActionsY = new List<MotorActionParams>();
                    motorActionsY.Add(new MotorActionParams
                    {
                        MotorId = 1,
                        ActionType = 4,
                        Target = 0,
                        Speed = 300.0f,
                        Acc = 500.0f,
                        Dcc = 500.0f
                    });
                    var motorActionsZ = new List<MotorActionParams>();
                    motorActionsZ.Add(new MotorActionParams
                    {
                        MotorId = 2,
                        ActionType = 4,
                        Target = 0,
                        Speed = 25.0f,
                        Acc = 100.0f,
                        Dcc = 100.0f
                    });
                    // 并行执行前两个动作
                    var resetX = await MotorActionAsync(motorActionsZ);
                    var resetY = await MotorActionAsync(motorActionsX);
                    var resetZ = await MotorActionAsync(motorActionsY);
                    var breakTIP = await BreakPipeAsync();
                    var resetTIP = await ResetPipeAsync();

                    if (resetX == 0 && resetY == 0 && resetZ == 0 && resetTIP == 0 && breakTIP == 0)
                    {
                        RunInfoView.Visibility = Visibility.Collapsed;
                        StepDetailView.Visibility = Visibility.Visible;
                        ShowNotification(_res.GrpcInitSucc, NotificationControl.NotificationType.Info);
                    }
                }
                else
                {
                     ShowNotification(_res.GrpcStartRunning, NotificationControl.NotificationType.Warn);
                }

            }
            else if (DoorFlag == 1)
            {
                await Task.Delay(100);
                ShowNotification(_res.GrpcFailDoor, NotificationControl.NotificationType.Warn);
            }

        }
        //加载脚本
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建文件对话框
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = _res.OpenFileDialog_Filter,
                Title = _res.OpenFileDialog_Title,
                InitialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts")
            };

            // 显示对话框并检查用户是否选择了文件
            bool? result = openFileDialog.ShowDialog(this);
            if (result == true)
            {
                try
                {
                    // 读取文件内容
                    string scriptJson = File.ReadAllText(openFileDialog.FileName);
                    if (string.IsNullOrEmpty(scriptJson))
                    {
                        ShowNotification(_res.OpenFileDialog_Empty, NotificationControl.NotificationType.Warn);
                        return;
                    }

                    // 加载脚本数据
                    LoadScriptFromJson(scriptJson);

                    // 切换到步骤详情视图
                    StepDetailView.Visibility = Visibility.Visible;
                    RunInfoView.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    ShowNotification($"{_res.OpenFileDialog_Error}: {ex.Message}", NotificationControl.NotificationType.Error);
                }
            }
        }
        /// <summary>
        /// 从JSON字符串加载流程数据
        /// </summary>
        /// <param name="scriptJson">导出的脚本JSON字符串</param>
        private void LoadScriptFromJson(string scriptJson)
        {
            try
            {
                // 解析JSON根对象
                var script = JObject.Parse(scriptJson);

                // 1. 清空现有数据
                FlowSteps.Clear();
                _plateConsumableMap.Clear();
                StepDetailPanel.Children.Clear();
                _stepIndex = 3; // 重置步骤计数器

                // 2. 恢复基础信息（可选：显示创建者和描述）
                string creator = script["creator"]?.ToString() ?? "未知用户";
                string description = script["description"]?.ToString() ?? "无描述";

                // 3. 恢复耗材配置（crea_list）
                var creaList = script["crea_list"] as JArray;
                if (creaList != null)
                {
                    foreach (var creaItem in creaList)
                    {
                        string plateId = creaItem["plate"]?.ToString();
                        if (string.IsNullOrEmpty(plateId)) continue;

                        var creaParam = creaItem["crea_param"] as JObject;
                        if (creaParam == null) continue;

                        // 查找匹配的耗材（从现有Consumables中匹配名称）
                        string consName = creaParam["name"]?.ToString();
                        var matchedConsumable = Consumables.FirstOrDefault(c => c.Name == consName);

                        if (matchedConsumable != null)
                        {
                            // 记录板位与耗材的关联
                            _plateConsumableMap[plateId] = matchedConsumable;

                            // 恢复板位显示
                            Dispatcher.Invoke(() =>
                            {
                                // 清空板位原有内容
                                ClearPlateContent(plateId);

                                // 重新添加耗材到板位
                                if (this.FindName($"PlateGrid{plateId}") is Grid plateGrid)
                                {
                                    plateGrid.Children.Clear();
                                    var plateText = new TextBlock
                                    {
                                        Text = $"P{plateId}", // 显示板位编号（如P1、P2）
                                        FontSize = 16,
                                        FontWeight = FontWeights.Bold,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        VerticalAlignment = VerticalAlignment.Top, // 顶部对齐
                                        Margin = new Thickness(0, 5, 0, 0), // 顶部留出间距
                                    };
                                    // 设置ZIndex确保在最上层
                                    plateGrid.Children.Add(plateText);
                                    var canvas = new ConsumableCanvas
                                    {
                                        ConsData = matchedConsumable.Settings,
                                        Height = 250,
                                        Width = 250,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Background = Brushes.Transparent,
                                        PlateId = plateId
                                    };
                                    canvas.SelectedColumnsChanged += OnPlateColumnsSelected;
                                    plateGrid.Children.Add(canvas);
                                }
                            });
                        }
                    }
                }

                // 4. 恢复流程步骤（step_list）
                var stepList = script["step_list"] as JArray;
                if (stepList != null)
                {
                    // 添加开始步骤
                    FlowSteps.Add(new FlowStep
                    {
                        Index = 1,
                        //Name = "开始",
                        Type = "start",
                        IsSelected = false,
                        IsSystemStep = true
                    });

                    // 解析步骤列表
                    foreach (var stepItem in stepList)
                    {
                        string stepType = stepItem["type"]?.ToString();
                        if (string.IsNullOrEmpty(stepType) || stepType == "start" || stepType == "end")
                            continue; // 跳过系统步骤（开始/结束）

                        // 转换为中文步骤类型
                        string chineseType = stepType switch
                        {
                            "aspirate" => "吸液",
                            "dispense" => "注液",
                            "tipon" => "取头",
                            "tipoff" => "退头",
                            "wait" => "等待",
                            "shaker" => "震荡",
                            "magnetic" => "磁分离",
                            "shift" => "转移",
                            _ => "未知"
                        };

                        // 创建步骤对象
                        var flowStep = new FlowStep
                        {
                            Index = _stepIndex++,
                            //Name = $"{chineseType}步骤{_stepIndex - 1}",
                            Type = chineseType,
                            Position = "P1", // 默认值，后续会覆盖
                            Volume = 50,
                            IsSelected = false,
                            IsSystemStep = false
                        };

                        // 根据步骤类型解析参数
                        switch (stepType)
                        {
                            case "aspirate":
                                var aspirateParam = stepItem["aspirate_param"] as JObject;
                                if (aspirateParam != null)
                                {
                                    flowStep.Volume = aspirateParam["volume"]?.Value<int>() ?? 50;
                                    flowStep.Position = MapPlatePositionBack(stepItem["plate"]?.ToString());
                                    flowStep.MixCount = aspirateParam["mix_count"]?.Value<int>() ?? 0;
                                    flowStep.MixVolume = aspirateParam["mix_volume"]?.Value<float>() ?? 0;
                                    flowStep.IsMixEnabled = flowStep.MixCount > 0;
                                    // 新增：恢复孔位信息
                                    int col = aspirateParam["col"]?.Value<int>() ?? 1; // 从JSON提取列号
                                    flowStep.SelectedColumns = col.ToString(); // 保存选中列
                                    flowStep.WellPosition = $"列：{col}"; // 孔位文本（如"列：2"）
                                }
                                break;

                            case "dispense":
                                var dispenseParam = stepItem["dispense_param"] as JObject;
                                if (dispenseParam != null)
                                {
                                    flowStep.Volume = dispenseParam["volume"]?.Value<int>() ?? 50;
                                    flowStep.Position = MapPlatePositionBack(stepItem["plate"]?.ToString());
                                    flowStep.MixCount = dispenseParam["mix_count"]?.Value<int>() ?? 0;
                                    flowStep.MixVolume = dispenseParam["mix_volume"]?.Value<float>() ?? 0;
                                    flowStep.IsMixEnabled = flowStep.MixCount > 0;
                                    flowStep.FirstVol = dispenseParam["first_vol"]?.Value<int>() ?? 0;
                                    flowStep.FirstDelay = dispenseParam["first_delay"]?.Value<int>() ?? 0;

                                    // 新增：恢复孔位信息
                                    int col = dispenseParam["col"]?.Value<int>() ?? 1;
                                    flowStep.SelectedColumns = col.ToString();
                                    flowStep.WellPosition = $"列：{col}";
                                }
                                break;

                            case "tipon":
                            case "tipoff":
                                flowStep.Position = MapPlatePositionBack(stepItem["plate"]?.ToString());
                                // 新增：恢复吸头/退头步骤的孔位信息
                                var tipParam = stepItem[$"{stepType}_param"] as JObject;
                                if (tipParam != null)
                                {
                                    int col = tipParam["col"]?.Value<int>() ?? 1;
                                    flowStep.SelectedColumns = col.ToString();
                                    flowStep.WellPosition = $"列：{col}";
                                }
                                break;

                            case "wait":
                                var waitParam = stepItem["wait_param"] as JObject;
                                if (waitParam != null)
                                {
                                    // 转换毫秒为秒
                                    flowStep.WaitTime = (int)(waitParam["time_ms"]?.Value<int>() ?? 0 / 1000);
                                    flowStep.WaitContent = waitParam["contents"]?.ToString() ?? "Waiting for the operation";
                                }
                                break;
                        }

                        FlowSteps.Add(flowStep);
                    }

                    // 添加结束步骤
                    FlowSteps.Add(new FlowStep
                    {
                        Index = _stepIndex++,
                        //Name = "结束",
                        Type = "end",
                        IsSelected = false,
                        IsSystemStep = true
                    });

                    // 重新编号步骤
                    RebuildStepIndexes();
                }

                ShowNotification(_res.ScriptLoadSucc, NotificationControl.NotificationType.Info);
            }
            catch (Exception ex)
            {
                ShowNotification($"{_res.ScriptLoadFail}: {ex.Message}", NotificationControl.NotificationType.Error);
            }
        }

        /// <summary>
        /// 将JSON中的板位格式（如p1、magnetic_1）转换回UI格式（如P1）
        /// </summary>
        private string MapPlatePositionBack(string plate)
        {
            if (string.IsNullOrEmpty(plate)) return "P1";

            if (plate.StartsWith("p"))
                return $"P{plate.Substring(1)}"; // p1 → P1
            if (plate == "magnetic_1")
                return "P3"; // 对应磁分离板位
            if (plate == "shaker_1")
                return "P9"; // 对应震荡板位

            return "P1"; // 默认值
        }
        //流程开始
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证是否有步骤
            if (FlowSteps.Count == 0)
            {
                ShowNotification(_res.ScriptStartEmpty, NotificationControl.NotificationType.Warn);
                return;
            }
            var invalidSteps = new List<int>();
            foreach (var step in FlowSteps)
            {
                // 只校验吸液和注液步骤
                if ((step.Type == "吸液" || step.Type == "注液") &&
                    !step.IsSystemStep) // 排除系统步骤（开始/结束）
                {
                    // 检查液体参数是否未选择
                    if (step.SelectedLiquid == null || string.IsNullOrEmpty(step.SelectedLiquid.name))
                    {
                        invalidSteps.Add(step.Index); // 记录未选择液体参数的步骤序号
                    }
                }
            }

            // 如果存在未选择液体参数的步骤，提示并终止流程
            if (invalidSteps.Count > 0)
            {
                string stepNumbers = string.Join("、", invalidSteps);
                ShowNotification(_res.ScriptStartLiquidEmpty,
                    NotificationControl.NotificationType.Error);
                return; // 不继续执行后续流程
            }
            try
            {
                // 1. 创建脚本JSON
                ShowNotification(_res.ScriptStartCreating, NotificationControl.NotificationType.Info);
                string scriptJson = CreateScriptJson();

                // 2. 保存脚本到文件
                string scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts");
                Directory.CreateDirectory(scriptPath);
                string fileName = $"{DateTime.Now:yyyyMMddHHmmss}_script.json";
                string fullPath = System.IO.Path.Combine(scriptPath, fileName);
                File.WriteAllText(fullPath, scriptJson);

                // 3. 发送脚本到服务端执行（对应C++的emit startScript）
                // 假设通过gRPC发送脚本
                var response = await ScriptStartAsync(scriptJson);
                if (response == 0)
                {
                    StartMonitoring();
                    RunLogs.Clear();
                    runFlag = true;
                    ShowNotification(_res.ScriptStartSucc, NotificationControl.NotificationType.Info);
                    // 切换视图
                    StepDetailView.Visibility = Visibility.Collapsed;
                    RunInfoView.Visibility = Visibility.Visible;
                }
                else if (response == 1)
                {
                    ShowNotification(_res.ScriptStartCheckFail, NotificationControl.NotificationType.Error);
                }
                else if (response == 2)
                {
                    ShowNotification(_res.ScriptStartFail, NotificationControl.NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"{_res.ScriptStartCreateFail}: {ex.Message}", NotificationControl.NotificationType.Error);
            }
        }
        //流程暂停
        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainPauseName.Tag.ToString() == "pause")
            {
                if (!runFlag)
                {
                    ShowNotification(_res.ScriptNotStart, NotificationControl.NotificationType.Warn);
                    return;
                }
                ShowNotification(_res.ScriptPause, NotificationControl.NotificationType.Info);
                mainPauseName.Content = "继续";
                mainPauseName.Tag = "resume";
                await ScriptPauseAsync();
            }
            else if (mainPauseName.Tag.ToString() == "resume")
            {
                ShowNotification(_res.ScriptContinue, NotificationControl.NotificationType.Info);
                var switchValues = await GetSwitchValuesAsync();

                DoorFlag = switchValues.GetValueOrDefault("door_lock", -1f);
                if (DoorFlag == 0)
                {

                    ShowNotification(_res.GrpcFailDoor, NotificationControl.NotificationType.Warn);
                }
                else
                {
                    mainPauseName.Content = "暂停";
                    mainPauseName.Tag = "pause";
                    await ScriptContinueAsync();
                }
            }

        }
        //流程停止
        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {

            //RunInfoView.Visibility = Visibility.Collapsed;
            //StepDetailView.Visibility = Visibility.Visible;
            //StopMonitoring();   
            await ScriptStopAsync();

        }
        /// <summary>
        /// 启动脚本监控（对应C++的Script_monitor方法）
        /// </summary>
        /// <returns>异步任务</returns>
        public async Task ScriptMonitorAsync(CancellationToken cancellationToken)
        {
            try
            {
                // 使用传入的取消令牌
                var callOptions = new CallOptions(cancellationToken: cancellationToken);

                // 调用gRPC流方法
                using (var call = _ScriptClient.Script_monitor(new ScriptEngine.Void(), callOptions))
                {
                    // 循环读取流数据，同时检查取消请求
                    while (await call.ResponseStream.MoveNext(cancellationToken) &&
                          !cancellationToken.IsCancellationRequested)
                    {
                        var info = call.ResponseStream.Current;

                        // 触发事件传递监控数据
                        MonitorDataReceived?.Invoke(this, new ScriptMonitorEventArgs
                        {
                            ErrorCode = info.Errcode,
                            ErrorInfo = info.Errinfo,
                            State = info.State,
                            CurrentStep = info.CurrentSetp + 1,
                            MaxStep = info.MaxSetp,
                            MaxTime = info.MaxTime,
                            RunTime = info.RunTime
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 预期的取消操作，不视为错误
                Debug.WriteLine("ScriptMonitor was canceled");
            }
            catch (RpcException ex)
            {
                Debug.WriteLine($"ScriptMonitor gRPC error: {ex.Status.Detail} (Code: {ex.StatusCode})");
                MonitorDataReceived?.Invoke(this, new ScriptMonitorEventArgs
                {
                    ErrorCode = -1,
                    ErrorInfo = $"gRPC监控异常: {ex.Status.Detail}"
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ScriptMonitor error: {ex.Message}");
                MonitorDataReceived?.Invoke(this, new ScriptMonitorEventArgs
                {
                    ErrorCode = -2,
                    ErrorInfo = $"监控异常: {ex.Message}"
                });
            }
        }
        // 启动监控并订阅事件
        private void StartMonitoring()
        {
            // 先停止可能正在运行的监控
            StopMonitoring();

            // 创建新的取消令牌源
            _monitorCts = new CancellationTokenSource();

            // 订阅监控数据事件（更新UI或日志）
            MonitorDataReceived += OnMonitorDataReceived;

            // 启动监控方法（传入取消令牌）
            _ = ScriptMonitorAsync(_monitorCts.Token).ConfigureAwait(false);
        }
        // 停止监控
        private void StopMonitoring()
        {
            // 取消订阅事件，避免内存泄漏
            MonitorDataReceived -= OnMonitorDataReceived;

            // 如果存在活跃的取消令牌，请求取消
            if (_monitorCts != null && !_monitorCts.IsCancellationRequested)
            {
                _monitorCts.Cancel();
                _monitorCts.Dispose(); // 释放资源
                _monitorCts = null;
            }

            // 可选：清空监控状态显示
            ResetMonitorUI();
        }

        // 重置监控UI显示
        private void ResetMonitorUI()
        {
            Dispatcher.Invoke(() =>
            {
                RunTimeText.Text = "00:00:00";
                CurrentStepText.Text = "0/0";
                StatusText.Text = _res.ScriptUINotRun;
                StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(153, 153, 153)); // 灰色
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153));
                RunProgressBar.Value = 0;
                ProgressPercentText.Text = "0%";
            });
        }
        // 监控数据处理方法（更新UI需通过Dispatcher）
        private void OnMonitorDataReceived(object sender, ScriptMonitorEventArgs e)
        {
            // 确保在UI线程处理（如果需要更新UI）
            Dispatcher.Invoke(() =>
            {
                // 更新运行时间 (将秒转换为时分秒格式)
                double seconds = e.RunTime / 1000.0;
                RunTimeText.Text = TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");

                CurrentStepText.Text = $"{e.CurrentStep}/{e.MaxStep}";

                if (e.MaxStep > 0)
                {
                    double progress = (double)e.CurrentStep / e.MaxStep * 100;
                    RunProgressBar.Value = progress;
                    ProgressPercentText.Text = $"{(int)progress}%";
                }
                // 更新状态和指示器颜色
                StatusText.Text = e.State;
                switch (e.State)
                {
                    case "run":
                        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(39, 174, 96)); // 绿色
                        StatusText.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
                        break;
                    case "pause":
                        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(243, 156, 18)); // 橙色
                        StatusText.Foreground = new SolidColorBrush(Color.FromRgb(243, 156, 18));
                        break;
                    case "idle":
                        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(52, 152, 219)); // 蓝色
                        StatusText.Foreground = new SolidColorBrush(Color.FromRgb(52, 152, 219));
                        runFlag = false;
                        pauseFlag = false;

                        break;
                    case "err":
                        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(231, 76, 60)); // 红色
                        StatusText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                        break;
                }
                bool shouldLog = (e.CurrentStep != _lastLoggedStep) ||
                       (e.State == "err" && _lastLoggedState != "err") ||
                       _lastLoggedStep == -1;
                if (shouldLog && e.CurrentStep > 0)
                {
                    // 添加日志条目
                    GenerateStepLog(e.State, e.CurrentStep, e.MaxStep, e.ErrorCode, e.ErrorInfo);
                    // 更新记录的步骤和状态
                    _lastLoggedStep = e.CurrentStep;
                    _lastLoggedState = e.State;
                }

            });
        }
        private void GenerateStepLog(string state, int currentStep, int maxStep, int errorCode, string errorInfo = "")
        {
            string stepName = FlowSteps[currentStep - 1].Type; ;

            string stateText = state == "run" ? _res.ScriptUILogRun :
                                 state == "pause" ? _res.ScriptUILogPause :
                                 state == "idle" ? _res.ScriptUILogIdle :
                                 state == "err" ? _res.ScriptUILogError : _res.ScriptUILogUnknown;

            // 5. 构建状态描述（步骤信息）
            string statusDesc = $"[{stateText}] / {stepName} ({currentStep}/{maxStep})";
            if (state == "err")
            {
                statusDesc += $" | {errorCode} / {errorInfo}";
            }
            AddLogEntry(statusDesc);
        }
        //补光灯
        private async void LightControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (LightFlag == 0)
            {
                ShowNotification(_res.DeviceLightOpen, NotificationControl.NotificationType.Info);

                var lightFllag = await SetSwitchAsync("fill_light", 0);//open
                UpdateLightButtonStyle(1);
                LightFlag = 1;
            }
            else if (LightFlag == 1)
            {
                ShowNotification(_res.DeviceLightClose, NotificationControl.NotificationType.Info);

                var lightFllag = await SetSwitchAsync("fill_light", 1);//close//fill_light
                UpdateLightButtonStyle(0);
                LightFlag = 0;
            }
        }
        //UV灯
        private async void UVLightControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (UVFlag == 0)
            {
                ShowNotification(_res.DeviceUVOpen, NotificationControl.NotificationType.Info);

                var switchValues = await GetSwitchValuesAsync();

                DoorFlag = switchValues.GetValueOrDefault("fill_light", -1f);
                if (DoorFlag == 1)
                {
                    ShowNotification(_res.GrpcFailDoor, NotificationControl.NotificationType.Warn);
                }
                else
                {
                    var lightFllag = await SetSwitchAsync("uv_lamp", 0);//open
                    UpdateUVButtonStyle(1);
                    UVFlag = 1;
                }

            }
            else if (UVFlag == 1)
            {
                ShowNotification(_res.DeviceUVClose, NotificationControl.NotificationType.Info);

                var lightFllag = await SetSwitchAsync("uv_lamp", 1);//close//fill_light
                UpdateUVButtonStyle(0);
                UVFlag = 0;
            }
        }
        // 更新补光灯按钮样式
        private void UpdateLightButtonStyle(int LightFlag)
        {
            if (LightFlag == 1)
            {
                // 灯打开状态样式
                LightControlButton.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 绿色
                LightControlButton.Foreground = Brushes.White;
                LightControlButton.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
            else
            {
                // 灯关闭状态样式
                LightControlButton.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 红色
                LightControlButton.Foreground = Brushes.White;
                LightControlButton.BorderBrush = new SolidColorBrush(Color.FromRgb(198, 40, 40));
            }
        }

        // 更新UV灯按钮样式
        private void UpdateUVButtonStyle(int UVFlag)
        {
            if (UVFlag == 1)
            {
                // UV灯打开状态样式
                UVLightControlButton.Background = new SolidColorBrush(Color.FromRgb(156, 39, 176)); // 紫色
                UVLightControlButton.Foreground = Brushes.White;
                UVLightControlButton.BorderBrush = new SolidColorBrush(Color.FromRgb(123, 31, 162));
            }
            else
            {
                // UV灯关闭状态样式
                UVLightControlButton.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 红色
                UVLightControlButton.Foreground = Brushes.White;
                UVLightControlButton.BorderBrush = new SolidColorBrush(Color.FromRgb(198, 40, 40));
            }
        }
        //一键删除
        private void AllClearButton_Click(object sender, RoutedEventArgs e)
        {
            FlowSteps.Clear();
            FlowSteps.Add(new FlowStep
            {
                Index = 1,
                //Name = "开始",
                Type = "start",
                IsSelected = false,
                IsSystemStep = true // 标记为系统步骤
            });
            // 添加结束步骤
            FlowSteps.Add(new FlowStep
            {
                Index = 2,
                //Name = "结束",
                Type = "end",
                IsSelected = false,
                IsSystemStep = true // 标记为系统步骤
            });
            FlowList.ItemsSource = FlowSteps;
            _stepIndex = 3;
        }
        //快速生成
        private void QuickGenerateButton_Click(object sender, RoutedEventArgs e)
        {
            var quickWindow = new QuickFlowWindow(
                this,
        FlowSteps,
        _plateConsumableMap,
        Liquids


    );

            if (quickWindow.ShowDialog() == true)
            {
                // 刷新列表显示
                FlowList.Items.Refresh();

                // 显示成功提示
                ShowNotification(_res.ScriptSuccCrea, NotificationControl.NotificationType.Info);
            }
        }
        //grpc
        // 获取数据库板位数据
        public async Task<Plate_info> GetSQLDataAsync(string plate)
        {
            if (string.IsNullOrEmpty(plate))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(_res.SQLPosNameNotEmpty, NotificationControl.NotificationType.Error);
                });
                return null;
            }

            var request = new Plate_info
            {
                Name = plate
            };

            try
            {
                // 调用gRPC服务获取数据
                var response = await _dataEngineClient.Data_Plate_getAsync(
                    request,
                    deadline: DateTime.UtcNow.AddSeconds(10));

                return response;
            }
            catch (Grpc.Core.RpcException ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(
                        $"gRPC Warn: {ex.Status.Detail}\n{ex.StatusCode}",
                        NotificationControl.NotificationType.Error);
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                });
            }

            return null;
        }
        // 保存数据库板位数据
        public async Task<Plate_info> SetSQLDataAsync(string plate, float x, float y, float z)
        {
            if (string.IsNullOrEmpty(plate))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(_res.SQLPosNameNotEmpty, NotificationControl.NotificationType.Error);
                });
                return null;
            }

            var request = new Plate_info
            {
                Name = plate,
                X = x,
                Y = y,
                Z = z
            };

            try
            {
                // 调用gRPC服务保存数据
                var response = await _dataEngineClient.Data_Plate_setAsync(
                    request,
                    deadline: DateTime.UtcNow.AddSeconds(10));
                return response;
            }
            catch (Grpc.Core.RpcException ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(
                        $"gRPC Warn: {ex.Status.Detail}\n{ex.StatusCode}",
                        NotificationControl.NotificationType.Error);
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                });
            }

            return null;
        }
        //设置UV、补光灯、门
        public async Task<int> SetSwitchAsync(string switchName, float switchPower)
        {
            var request = new switch_info
            {
                Id = switchName,
                Sw = switchPower
            };

            try
            {
                var response = await _commonClient.Set_switch_infoAsync(
                    request,
                    deadline: DateTime.UtcNow.AddSeconds(5));

                // 正确处理UI更新和返回值
                var result = response.Errcode == 0 ? 0 : -1;
                var message = response.Errcode == 0
                    ? "设置开关成功"
                    : $"操作失败 ({response.Errcode})";

                await Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(
                         message,
                         response.Errcode == 0
                             ? NotificationControl.NotificationType.Info
                             : NotificationControl.NotificationType.Error);
                });

                return result;
            }
            catch (Grpc.Core.RpcException ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(
                        $"gRPC Warn: {ex.Status.Detail}\n{ex.StatusCode}",
                        NotificationControl.NotificationType.Error);
                });

                return -1;
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                });

                return -1;
            }
        }
        //获得UV、补光灯、门状态
        public async Task<Dictionary<string, float>> GetSwitchValuesAsync()
        {
            try
            {
                var response = await _commonClient.Get_switch_infoAsync(
                    new CommonModel.Void(),
                    deadline: DateTime.UtcNow.AddSeconds(5));

                if (response == null || response.Info.Count == 0)
                {
                    string message = response == null
                        ? "服务器未响应"
                        : "没有可用开关";

                    //ShowNotification(message, NotificationControl.NotificationType.Warn);
                    return new Dictionary<string, float>();
                }

                // 将响应转换为字典
                return response.Info.ToDictionary(
                    info => info.Id,
                    info => info.Sw
                );
            }
            catch (Grpc.Core.RpcException ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Warn: {ex.Status.Detail}\n{ex.StatusCode}",
                        NotificationControl.NotificationType.Error);
                });

                return new Dictionary<string, float>();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                });

                return new Dictionary<string, float>();
            }
        }
        //电机运动
        public async Task<int> MotorActionAsync(IEnumerable<MotorActionParams> motorActions)
        {
            var moveBases = motorActions.Select(action => new Motor_action_param_base
            {
                Id = action.MotorId,
                Type = action.ActionType,
                Target = action.Target,
                Speed = action.Speed,
                Acc = action.Acc,
                Dcc = action.Dcc
            }).ToList();

            var request = new Motor_action_param
            {
                Move = { moveBases }
            };

            try
            {
                var response = await _MotorClient.Motor_actionAsync(
                    request,
                    deadline: DateTime.UtcNow.AddSeconds(15));

                // 关键：用 Func<int> 让 Dispatcher.Invoke 返回处理结果，再由外层方法返回
                int resultCode = Dispatcher.Invoke(() =>
                {
                    if (response.Errcode == 0)
                    {
                        // 这里的 return 是 Func<int> 的返回值，会被外层的 resultCode 接收
                        return response.Errcode;
                    }
                    else
                    {
                        string errorMessage = $"{_res.DeviceOperationFailure} ({response.Errcode}): ";
                        //switch (response.Errcode)
                        //{
                        //    case 1: errorMessage += "电机不支持此操作"; break;
                        //    case 2: errorMessage += "无效参数"; break;
                        //    case 3: errorMessage += "操作已取消"; break;
                        //    case 4: errorMessage += "电机超时"; break;
                        //    case 5: errorMessage += "电机繁忙"; break;
                        //    case 6: errorMessage += "事件触发识别"; break;
                        //    case 7: errorMessage += "驱动器错误"; break;
                        //    default: errorMessage += "未知错误"; break;
                        //}
                        //if (!string.IsNullOrEmpty(response.Errinfo))
                        //{
                        //    errorMessage += $"\n细节: {response.Errinfo}";
                        //}
                        ShowNotification(errorMessage, NotificationControl.NotificationType.Error);
                        return response.Errcode; // 同样返回错误码
                    }
                });

                return resultCode;
            }
            catch (Grpc.Core.RpcException ex)
            {
                // 异常处理同理：捕获 Dispatcher.Invoke 的返回值
                int errorCode = Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Warn: {ex.Status.Detail}\n{ex.StatusCode}",
                        NotificationControl.NotificationType.Error);
                    return -1; // 返回异常码
                });
                return errorCode;
            }
            catch (Exception ex)
            {
                int errorCode = Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                    return -1;
                });
                return errorCode;
            }
        }
        //移液器吸液
        public async Task<int> AspiratePipeAsync(float volume, float speed)
        {
            var request = new Pipe_action_param
            {
                PipeName = "pipette_1",

                Volume = volume
            };

            var liquidParam = new Pipe_liquid_param
            {
                Speed = speed,              // 吸液速度
                Density = 1000.0f,             // 密度
                AirBefore = 0.0f,           // 前置空气量
                AirAfter = 0.0f             // 后置空气量
            };
            request.Liq = liquidParam;

            try
            {
                var response = await _pipetteClient.Pipe_aspirateAsync(
                    request,
                    deadline: DateTime.UtcNow.AddSeconds(15));

                int resultCode = Dispatcher.Invoke(() =>
                {
                    if (response.Errcode == 0)
                    {
                        return response.Errcode;
                    }
                    else
                    {
                        string errorMessage = $"{_res.DeviceOperationFailure} ({response.Errcode}): ";
                        ShowNotification(errorMessage, NotificationControl.NotificationType.Error);
                        return response.Errcode;
                    }
                });

                return resultCode;
            }
            catch (Grpc.Core.RpcException ex)
            {
                int errorCode = Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Warn: {ex.Status.Detail}\n: {ex.StatusCode}",
                        NotificationControl.NotificationType.Error);
                    return -1;
                });
                return errorCode;
            }
            catch (Exception ex)
            {
                int errorCode = Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                    return -1;
                });
                return errorCode;
            }
        }
        //移液器注液
        public async Task<int> DispensePipeAsync(float volume, float speed)
        {
            var request = new Pipe_action_param
            {
                PipeName = "pipette_1",

                Volume = volume
            };

            var liquidParam = new Pipe_liquid_param
            {
                Speed = speed,              // 注液速度
                Density = 1000.0f,             // 密度
                AirBefore = 0.0f,           // 前置空气量
                AirAfter = 0.0f             // 后置空气量
            };
            request.Liq = liquidParam;

            try
            {
                var response = await _pipetteClient.Pipe_disenseAsync(
                    request,
                    deadline: DateTime.UtcNow.AddSeconds(15));

                int resultCode = Dispatcher.Invoke(() =>
                {
                    if (response.Errcode == 0)
                    {
                        return response.Errcode;
                    }
                    else
                    {
                        string errorMessage = $"{_res.DeviceOperationFailure} ({response.Errcode}): ";
                        ShowNotification(errorMessage, NotificationControl.NotificationType.Error);
                        return response.Errcode;
                    }
                });

                return resultCode;
            }
            catch (Grpc.Core.RpcException ex)
            {
                int errorCode = Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Warn: {ex.Status.Detail}\n: {ex.StatusCode}",
                        NotificationControl.NotificationType.Error);
                    return -1;
                });
                return errorCode;
            }
            catch (Exception ex)
            {
                int errorCode = Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                    return -1;
                });
                return errorCode;
            }
        }
        //移液器退头
        public async Task<int> BreakPipeAsync()
        {
            var request = new Pipe_action_param
            {
                PipeName = "pipette_1",
            };

            try
            {
                var response = await _pipetteClient.Pipe_breakTipAsync(
                    request,
                    deadline: DateTime.UtcNow.AddSeconds(15));

                int resultCode = Dispatcher.Invoke(() =>
                {
                    if (response.Errcode == 0)
                    {
                        return response.Errcode;
                    }
                    else
                    {
                        string errorMessage = $"{_res.DeviceOperationFailure} ({response.Errcode}): ";
                        ShowNotification(errorMessage, NotificationControl.NotificationType.Error);
                        return response.Errcode;
                    }
                });

                return resultCode;
            }
            catch (Grpc.Core.RpcException ex)
            {
                int errorCode = Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Warn: {ex.Status.Detail}\n: {ex.StatusCode}",
                        NotificationControl.NotificationType.Error);
                    return -1;
                });
                return errorCode;
            }
            catch (Exception ex)
            {
                int errorCode = Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                    return -1;
                });
                return errorCode;
            }
        }
        //移液器复位
        public async Task<int> ResetPipeAsync()
        {
            var request = new Pipe_action_param
            {
                PipeName = "pipette_1",
            };

            try
            {
                var response = await _pipetteClient.Pipe_resetAsync(
                    request,
                    deadline: DateTime.UtcNow.AddSeconds(15));

                int resultCode = Dispatcher.Invoke(() =>
                {
                    if (response.Errcode == 0)
                    {
                        return response.Errcode;
                    }
                    else
                    {
                        string errorMessage = $"{_res.DeviceOperationFailure}  ({response.Errcode}): ";
                        ShowNotification(errorMessage, NotificationControl.NotificationType.Error);
                        return response.Errcode;
                    }
                });

                return resultCode;
            }
            catch (Grpc.Core.RpcException ex)
            {
                int errorCode = Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Warn:  {ex.Status.Detail} \n: {ex.StatusCode}",
                        NotificationControl.NotificationType.Error);
                    return -1;
                });
                return errorCode;
            }
            catch (Exception ex)
            {
                int errorCode = Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                    return -1;
                });
                return errorCode;
            }
        }
        //移液器获得定标
        public async Task<PipeCalibrationParams> GetPipeCalibrationAsync(string pipeName)
        {
            var request = new Pipe_cali_param
            {
                PipeName = pipeName
            };

            try
            {
                var response = await _pipetteClient.Pipe_get_caliAsync(
                    request,
                    deadline: DateTime.UtcNow.AddSeconds(15));

                return new PipeCalibrationParams
                {
                    PipeName = pipeName,
                    BackDiff = response.Backdiff,
                    K10 = response.K10,
                    K20 = response.K20,
                    K50 = response.K50,
                    K100 = response.K100,
                    K200 = response.K200,
                    K300 = response.K300,
                    K400 = response.K400,
                    K500 = response.K500,
                    K600 = response.K600,
                    K700 = response.K700,
                    K800 = response.K800,
                    K900 = response.K900,
                    K1000 = response.K1000
                };
            }
            catch (Grpc.Core.RpcException ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(
                        $"gRPC Warn: {ex.Status.Detail}\n: {ex.StatusCode}",
                        NotificationControl.NotificationType.Error);
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                });
            }

            return null;
        }
        //移液器设置定标
        public async Task<int> SetPipeCalibrationAsync(PipeCalibrationParams @params)
        {
            var request = new Pipe_cali_param
            {
                PipeName = @params.PipeName,
                Backdiff = @params.BackDiff,
                K10 = @params.K10,
                K20 = @params.K20,
                K50 = @params.K50,
                K100 = @params.K100,
                K200 = @params.K200,
                K300 = @params.K300,
                K400 = @params.K400,
                K500 = @params.K500,
                K600 = @params.K600,
                K700 = @params.K700,
                K800 = @params.K800,
                K900 = @params.K900,
                K1000 = @params.K1000
            };

            try
            {
                var response = await _pipetteClient.Pipe_set_caliAsync(
                    request,
                    deadline: DateTime.UtcNow.AddSeconds(15));

                int resultCode = Dispatcher.Invoke(() =>
                {
                    if (response != null)
                    {
                        return 0;
                    }
                    else
                    {
                        string errorMessage = _res.DeviceOperationFailure;
                        ShowNotification(errorMessage, NotificationControl.NotificationType.Error);
                        return -1;
                    }
                });

                return resultCode;
            }
            catch (Grpc.Core.RpcException ex)
            {
                int errorCode = Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Warn: {ex.Status.Detail}\n: {ex.StatusCode}",
                        NotificationControl.NotificationType.Error);
                    return -1;
                });
                return errorCode;
            }
            catch (Exception ex)
            {
                int errorCode = Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                    return -1;
                });
                return errorCode;
            }
        }
        //开始流程
        public async Task<int> ScriptStartAsync(string scriptValue)
        {
            var request = new Script_data
            {
                ScriptJson = scriptValue
            };

            var response = await _ScriptClient.Script_checkAsync(
                request,
                deadline: DateTime.UtcNow.AddSeconds(15));
            if (response.Errcode == 0)
            {
                var responseRun = await _ScriptClient.Script_runAsync(
request,
deadline: DateTime.UtcNow.AddSeconds(15));
                if (responseRun.Errcode == 0)
                {
                    return 0;
                }
                else
                {
                    return 2;
                }
            }
            else
            {
                return 1;
            }
        }

        //暂停流程
        public async Task ScriptPauseAsync()
        {
            try
            {
                var response = await _ScriptClient.Script_pauseAsync(
                    new ScriptEngine.Void(),
                    deadline: DateTime.UtcNow.AddSeconds(10));

                Dispatcher.Invoke(() =>
                {
                    if (response.Errcode == 0)
                    {
                        runFlag = false;
                        pauseFlag = true;
                        ShowNotification(_res.DeviceOperationSucc,
                            NotificationControl.NotificationType.Info);
                    }
                    else
                    {
                        string errorMessage = $"{_res.DeviceOperationFailure}  ({response.Errcode}): ";

                        if (!string.IsNullOrEmpty(response.Errinfo))
                        {
                            errorMessage += $"\n: {response.Errinfo}";
                        }

                        ShowNotification(errorMessage,
                            NotificationControl.NotificationType.Error);
                    }
                });
            }
            catch (Grpc.Core.RpcException ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                         $"gRPC Warn: {ex.Status.Detail}\n: {ex.StatusCode}",
                         NotificationControl.NotificationType.Error);

                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                });
            }
        }
        //继续流程
        public async Task ScriptContinueAsync()
        {
            try
            {
                var response = await _ScriptClient.Script_continueAsync(
                    new ScriptEngine.Void(),
                    deadline: DateTime.UtcNow.AddSeconds(10));

                Dispatcher.Invoke(() =>
                {
                    if (response.Errcode == 0)
                    {
                        runFlag = true;
                        pauseFlag = false;
                        ShowNotification(_res.DeviceOperationSucc,
                            NotificationControl.NotificationType.Info);
                    }
                    else
                    {
                        string errorMessage = $"{_res.DeviceOperationFailure} ({response.Errcode}): ";

                        if (!string.IsNullOrEmpty(response.Errinfo))
                        {
                            errorMessage += $"\n: {response.Errinfo}";
                        }

                        ShowNotification(errorMessage,
                            NotificationControl.NotificationType.Error);
                    }
                });
            }
            catch (Grpc.Core.RpcException ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Warn: {ex.Status.Detail}\n: {ex.StatusCode}",
                        NotificationControl.NotificationType.Error);

                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                });
            }
        }
        //停止流程

        public async Task ScriptStopAsync()
        {
            try
            {
                var response = await _ScriptClient.Script_stopAsync(
                    new ScriptEngine.Void(),
                    deadline: DateTime.UtcNow.AddSeconds(10));

                Dispatcher.Invoke(() =>
                {
                    if (response.Errcode == 0)
                    {
                        runFlag = false;
                        //_lastStep = -1;
                        //_lastScriptErrod = "";
                        pauseFlag = false;
                        ShowNotification(_res.DeviceOperationSucc,
                            NotificationControl.NotificationType.Info);
                    }
                    else
                    {
                        string errorMessage = $"{_res.DeviceOperationFailure} ({response.Errcode}): ";

                        if (!string.IsNullOrEmpty(response.Errinfo))
                        {
                            errorMessage += $"\n: {response.Errinfo}";
                        }

                        ShowNotification(errorMessage,
                            NotificationControl.NotificationType.Error);
                    }
                });
            }
            catch (Grpc.Core.RpcException ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Warn: {ex.Status.Detail}\n: {ex.StatusCode}",
                        NotificationControl.NotificationType.Error);

                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowNotification(
                        $"gRPC Err: {ex.Message}",
                        NotificationControl.NotificationType.Error);
                });
            }
        }
        //中文
        private void LangSwitch_Checked(object sender, RoutedEventArgs e)
        {
            // 切换为中文
            ResourceHelper.Instance.SwitchToChinese();
            ShowNotification("已切换为中文", NotificationControl.NotificationType.Info);
        }
        //英文
        private void LangSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            ResourceHelper.Instance.SwitchToEnglish();
            ShowNotification("It has been switched to English", NotificationControl.NotificationType.Info);
        }
    }
}
