using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Newtonsoft.Json.Linq;
using QybotrunPkg;
using Serilog;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static QybotrunPkg.qybotrun;
using static ScriptEngine.ScriptEngine;
namespace OctoFixFlow
{
    /// <summary>
    /// MainWidget.xaml 的交互逻辑
    /// </summary>
    public partial class MainWidget : Window
    {
        public readonly ResourceHelper _res;
        private SettingPopupControl TopSettingPopup;

        private DatabaseService databaseService;
        private const int MAX_NOTIFICATIONS = 3;//14.6  12.9

        // 耗材项数据集合（绑定到ItemsControl）
        public ObservableCollection<ConsumableItem> Consumables { get; set; }
        // 记录板位与耗材的关联（板位编号 -> 耗材）
        public Dictionary<string, ConsumableItem> _plateConsumableMap = new Dictionary<string, ConsumableItem>();
        // 记录当前鼠标所在的板位
        private Border _currentHoveredPlate = null;
        // 流程步骤集合
        public ObservableCollection<FlowStep> FlowSteps { get; set; }
        private int _stepIndex = 1; // 步骤序号计数器
        private int _stepClickIndex = 1; // 步骤序号计数器
        public string _currentSelectedPlateId;    // 记录当前选中的板位ID

        private int _currentLevel = 0;
        private Stack<int> _levelStack = new Stack<int>();

        //液体类
        public ObservableCollection<LiquidSettings> Liquids { get; set; }

        //grpc
        public class ScriptDebugParsedResult
        {
            /// <summary>
            /// 核心数据（对应JSON的data字段）
            /// </summary>
            public string Data { get; set; }

            /// <summary>
            /// 详情描述（对应JSON的details字段）
            /// </summary>
            public string Details { get; set; }

            /// <summary>
            /// 执行结果（对应JSON的result字段，如succeed/fail）
            /// </summary>
            public string Result { get; set; }
        }
        private GrpcChannel _channel;
        private ScriptEngineClient _ScriptClient;//数据通信
        private qybotrunClient _qybotrunClient;//qypython通信


        private float UVFlag = 0;//0:close;1:open
        private float LightFlag = 0;//0:close;1:open
        public bool runFlag = false;
        public bool pauseFlag = false;



        public MainWidget()
        {
            InitializeComponent();
            _res = ResourceHelper.Instance;
            databaseService = new DatabaseService();
            DataContext = this;
            InitializeLanguage();
            //GRPC
            InitializeGrpcClient();
            //GRPC
            Consumables = new ObservableCollection<ConsumableItem>();
            _ = LoadConsumablesData();
            Liquids = new ObservableCollection<LiquidSettings>();
            _ = LoadLiquidsData();
            FlowSteps = new ObservableCollection<FlowStep>();

            FlowSteps.Add(new FlowStep
            {
                Index = 1,
                Type = "start",
                IsSelected = false,
                IsSystemStep = true,
                Level = 0
            });
            FlowSteps.Add(new FlowStep
            {
                Index = 2,
                Type = "end",
                IsSelected = false,
                IsSystemStep = true,
                Level = 0
            });
            FlowList.ItemsSource = FlowSteps;
            _levelStack.Clear();
            _levelStack.Push(0);
            _stepIndex = 3;
            RebuildStepIndexes();

            ActionSelectComboBox.SelectedIndex = 0;
            InitTopSettingPopup();
        }
        private void InitializeLanguage()
        {
            if (_res.IsEnglish)
                LangSwitch.IsChecked = false;
            else
                LangSwitch.IsChecked = true;
        }
        //GRPC
        private void InitializeGrpcClient()
        {
            try
            {
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
                _channel = GrpcChannel.ForAddress(address, channelOptions);

                _ScriptClient = new ScriptEngineClient(_channel);
                _qybotrunClient = new qybotrunClient(_channel);

                ShowNotification(_res.GrpcLoadSucc, NotificationControl.NotificationType.Info);
            }
            catch (System.Exception ex)
            {
                ShowNotification($"{_res.GrpcLoadFail}: {ex.Message}", NotificationControl.NotificationType.Error);

                Application.Current.Shutdown();
            }
        }
        private void InitTopSettingPopup()
        {
            TopSettingPopup = new SettingPopupControl(this);

            Grid.SetColumn(TopSettingPopup, 0);
            Grid.SetColumnSpan(TopSettingPopup, 6);
            Panel.SetZIndex(TopSettingPopup, 100);
            TopSettingPopup.Visibility = Visibility.Collapsed;
            MainContentGrid.Children.Add(TopSettingPopup);
        }
        private string LoadIP(string systemFolder)
        {
            try
            {
                if (!Directory.Exists(systemFolder))
                {
                    Directory.CreateDirectory(systemFolder);
                }

                var ipFilePath = System.IO.Path.Combine(systemFolder, "IP.ini");
                //const string defaultIp = "http://192.168.100.10:8001"
                //;http://192.168.100.10:8003  数据库  qybot   root qy123456
                const string defaultIp = "http://192.168.100.10:8001";

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
                return "http://192.168.100.10:8001";
            }
        }
        //GRPC
        // 加载耗材数据
        private async Task LoadConsumablesData()
        {
            var allConsSettings = await databaseService.GetAllConsumablesAsync();
            foreach (var setting in allConsSettings)
            {
                Consumables.Add(new ConsumableItem
                {
                    Name = setting.name,
                    Settings = setting
                });
            }
        }
        private async Task LoadLiquidsData()
        {
            var allLiquidSettings = await databaseService.GetAllLiquidsAsync();
            foreach (var liquid in allLiquidSettings)
            {
                Liquids.Add(liquid);
            }
        }
        public async void RefreshConsumablesAndLiquids()
        {
            Consumables.Clear();
            Liquids.Clear();

            await LoadConsumablesData();
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
                var plateGrid = plateBorder.Child as Grid;
                if (plateGrid == null) return;
                var plateId = plateBorder.Tag.ToString();

                var oldConsumableCanvas = plateGrid.Children.Cast<FrameworkElement>()
                    .FirstOrDefault(child => child.Tag?.ToString() == "TopConsumable");
                if (oldConsumableCanvas != null)
                    plateGrid.Children.Remove(oldConsumableCanvas);

                var bottomTextBlock = plateGrid.Children.Cast<FrameworkElement>()
                    .OfType<TextBlock>()
                    .FirstOrDefault(t => t.Tag?.ToString() == "BottomLayer");
                if (bottomTextBlock != null)
                    bottomTextBlock.Visibility = Visibility.Collapsed;

                var canvas = new ConsumableCanvas
                {
                    Tag = "TopConsumable",
                    ConsData = consumable.Settings,
                    Height = 300,
                    Width = 300,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.Transparent,
                    PlateId = plateId
                };
                canvas.SelectedColumnsChanged += OnPlateColumnsSelected;
                plateGrid.Children.Add(canvas);
                string consumableName = consumable.Name;


                string moduleName = string.Empty;
                var nameLayer = plateGrid.Children.Cast<FrameworkElement>()
                     .FirstOrDefault(child => child.Tag?.ToString() == "NameLayer");

                //// 2. 若NameLayer存在，且里面包含模块名称的TextBlock → 判定有模块
                if (nameLayer is StackPanel nameStack)
                {
                    var moduleNameText = nameStack.Children.Cast<TextBlock>().FirstOrDefault();
                    if (moduleNameText != null && !string.IsNullOrEmpty(moduleNameText.Text))
                    {
                        moduleName = moduleNameText.Text;
                    }
                    plateGrid.Children.Remove(nameLayer);
                }

                var nameStack2 = new StackPanel
                {
                    Tag = "NameLayer",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                var primaryBrush = (SolidColorBrush)FindResource("PrimaryColor");

                nameStack2.Children.Add(new TextBlock
                {
                    Text = moduleName,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = primaryBrush
                });
                nameStack2.Children.Add(new TextBlock
                {
                    Text = consumableName,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = primaryBrush
                });
                plateGrid.Children.Add(nameStack2);

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
                var cells = _selectedCellsFromText(columnText);
                //selectedStep.SelectedColumns = string.Join(",", columns);
                selectedStep.SelectedCells = string.Join(";", cells.Select(c => $"{c.Row},{c.Col}"));
            }
        }
        private List<int> _selectedColumnsFromText(string text)
        {
            var result = new List<int>();
            if (string.IsNullOrEmpty(text)) return result;

            string columnPrefix = ResourceHelper.Instance.StepDetailColumnPrefix;
            var content = text.Replace(columnPrefix, "");
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
        public List<(int Row, int Col)> _selectedCellsFromText(string text)
        {
            var result = new List<(int, int)>();
            if (string.IsNullOrEmpty(text))
                return result;

            // 拆分“行”和“列”部分（假设格式为“行A 列B”）
            string rowPrefix = ResourceHelper.Instance.StepDetailRowPrefix;
            string colPrefix = ResourceHelper.Instance.StepDetailColumnPrefix;

            // 提取行范围文本（如“1~3”）
            var rowPart = text.Split(new[] { rowPrefix }, StringSplitOptions.None).Skip(1).FirstOrDefault()?.Split(new[] { colPrefix }, StringSplitOptions.None).FirstOrDefault()?.Trim();
            // 提取列范围文本（如“2~5”）
            var colPart = text.Split(new[] { colPrefix }, StringSplitOptions.None).Skip(1).FirstOrDefault()?.Trim();

            // 解析行范围为行号列表
            var rows = ParseRangeToNumbers(rowPart);
            // 解析列范围为列号列表
            var cols = ParseRangeToNumbers(colPart);

            // 生成所有单元格组合（行×列）
            foreach (var row in rows)
            {
                foreach (var col in cols)
                {
                    result.Add((row, col));
                }
            }

            return result;
        }
        // 解析“X”或“X~Y”为数字列表
        private List<int> ParseRangeToNumbers(string rangeText)
        {
            var result = new List<int>();
            if (string.IsNullOrEmpty(rangeText))
                return result;

            // 拆分部分并移除空条目（避免空字符串干扰）
            var parts = rangeText.Split(new[] { '；' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.Contains("~"))
                {
                    // 拆分范围（如"2~4"→["2","4"]）
                    var rangeParts = part.Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries);
                    // 确保拆分后有2个有效部分且能解析为数字
                    if (rangeParts.Length == 2 &&
                        int.TryParse(rangeParts[0], out int start) &&
                        int.TryParse(rangeParts[1], out int end))
                    {
                        int min = Math.Min(start, end);
                        int max = Math.Max(start, end);
                        // 添加范围内的所有数字
                        for (int i = min; i <= max; i++)
                        {
                            result.Add(i);
                        }
                    }
                }
                else
                {
                    // 单个数字解析
                    if (int.TryParse(part, out int num))
                    {
                        result.Add(num);
                    }
                }
            }

            // 去重并排序
            return result.Distinct().OrderBy(n => n).ToList();
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
        public Border FindPlateBorderByPlateId(string plateId)
        {
            if (string.IsNullOrEmpty(plateId))
                return null;

            if (this.FindName("PlateContainer") is not Panel plateContainer)
                return null;

            foreach (var child in plateContainer.Children)
            {
                if (child is Border border
                    && border.Tag?.ToString()?.Trim() == plateId.Trim()
                    && border.Style?.TargetType == typeof(Border) // 确保是板位 Border（避免匹配其他 Border）
                    )
                {
                    return border;
                }
            }

            return null;
        }
        //更新模块
        public void UpdateDeviceModule()
        {
            //判断垃圾桶
            if (AppGlobalConfig.Instance.IsTrashEnabled)
            {
                //Grid.SetRow(PlateBorder12, 0);
                //Grid.SetColumn(PlateBorder12, 2);
                //Grid.SetRowSpan(PlateBorder12, 2);
                //ConsSettings trash_bin = new ConsSettings();
                //trash_bin.name = "trash_can";
                //trash_bin.labW = (float)120.3;//138.00
                //trash_bin.labL = (float)81.2;//140.00
                //trash_bin.labH = (float)10.2;//110.00
                //trash_bin.offsetX = (float)49.3;//0
                //trash_bin.offsetY = (float)31.4;//0
                //trash_bin.distanceRow = 9;//0
                //trash_bin.distanceColumn = 9;//0
                //trash_bin.distanceColumnX = (float)60.15;
                //trash_bin.distanceRowY = (float)40.6;
                //trash_bin.type = 3;
                //trash_bin.labVolume = 2400;
                //trash_bin.consMaxAvaiVol = 1800;
                //trash_bin.consDep = 90;
                //trash_bin.topShape = 1;
                //trash_bin.topUpperX = (float)81.2;
                //trash_bin.topUpperY = (float)120.3;
                //trash_bin.numRows = 1;
                //trash_bin.numColumns = 1;

                Grid.SetRow(PlateBorder12, 0);
                Grid.SetColumn(PlateBorder12, 2);
                Grid.SetRowSpan(PlateBorder12, 2);
                ConsSettings trash_bin = new ConsSettings();
                trash_bin.name = "trash_can";
                trash_bin.labW = (float)138.00;//
                trash_bin.labL = (float)140.00;//140.00
                trash_bin.labH = (float)0;//110.00
                trash_bin.offsetX = (float)0;//0
                trash_bin.offsetY = (float)36.4;//0
                trash_bin.distanceRow = 9;//0
                trash_bin.distanceColumn = 9;//0
                trash_bin.distanceColumnX = (float)70;
                trash_bin.distanceRowY = (float)69;
                trash_bin.type = 3;
                trash_bin.labVolume = 2400;
                trash_bin.consMaxAvaiVol = 1800;
                trash_bin.consDep = 90;
                trash_bin.topShape = 1;
                trash_bin.topUpperX = (float)140.00;
                trash_bin.topUpperY = (float)138.00;
                trash_bin.numRows = 1;
                trash_bin.numColumns = 1;

                var canvas = new ConsumableCanvas
                {
                    Tag = "TopConsumable",
                    ConsData = trash_bin,
                    Height = 300,
                    Width = 300,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    // Background = Brushes.White,
                    Background = Brushes.Transparent,
                    PlateId = "12"
                };
                canvas.SelectedColumnsChanged += OnPlateColumnsSelected;
                ConsumableItem trash_bin2 = new ConsumableItem();
                trash_bin2.Name = "trash_can";
                trash_bin2.Settings = trash_bin;
                _plateConsumableMap["12"] = trash_bin2;
            }
            //判断抓手
            if (AppGlobalConfig.Instance.IsGripperEnabled)
                ActionTransferButton.Visibility = Visibility.Visible;
            else
                ActionTransferButton.Visibility = Visibility.Collapsed;
            //判断PCR
            if (AppGlobalConfig.Instance.IsPCREnabled)
            {
                ActionPCRButton.Visibility = Visibility.Visible;
                Grid.SetRow(PlateBorder10, 0);
                Grid.SetColumn(PlateBorder10, 0);
                Grid.SetRowSpan(PlateBorder10, 2);
            }
            else
            {
                ActionPCRButton.Visibility = Visibility.Collapsed;
                Grid.SetRow(PlateBorder10, 1);
                Grid.SetColumn(PlateBorder10, 0);
                Grid.SetRowSpan(PlateBorder10, 1);
            }
            //判断加热振荡
            if (AppGlobalConfig.Instance.HasHeatingShaking())
                ActionShakeButton.Visibility = Visibility.Visible;
            else
                ActionShakeButton.Visibility = Visibility.Collapsed;
            //判断磁吸
            if (AppGlobalConfig.Instance.HasMagnetic())
                ActionMagneticButton.Visibility = Visibility.Visible;
            else
                ActionMagneticButton.Visibility = Visibility.Collapsed;
            //判断温控
            if (AppGlobalConfig.Instance.HasTemperatureControl())
                ActionTemperatureButton.Visibility = Visibility.Visible;
            else
                ActionTemperatureButton.Visibility = Visibility.Collapsed;
        }
        //更新板位
        public void UpdatePlateDisplay(Border plateBorder, ModuleDatas plateModule)
        {
            if (plateBorder == null) return;

            if (plateBorder.Child is not Grid plateGrid) return;
            var plateId = plateBorder.Tag.ToString();

            var oldBottomLayer = plateGrid.Children.Cast<FrameworkElement>()
                   .FirstOrDefault(child => child.Tag?.ToString() == "BottomLayer");
            if (oldBottomLayer != null)
                plateGrid.Children.Remove(oldBottomLayer);

            var oldNameLayer = plateGrid.Children.Cast<FrameworkElement>()
                   .FirstOrDefault(child => child.Tag?.ToString() == "NameLayer");
            if (oldNameLayer != null)
                plateGrid.Children.Remove(oldNameLayer);

            var imageUri = new Uri(plateModule.ModuleImage, UriKind.RelativeOrAbsolute);
            var moduleImage = new Image
            {
                Tag = "BottomLayer",
                Source = new BitmapImage(imageUri),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(2)
            };
            plateGrid.Children.Add(moduleImage);

            var nameStack = new StackPanel
            {
                Tag = "NameLayer",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,

            };

            nameStack.Children.Add(new TextBlock
            {
                Text = plateModule.Name,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White
            });
            plateGrid.Children.Add(nameStack);

        }

        // 清除板位内容的方法
        private void ClearPlateContent(string plateId)
        {
            if (this.FindName($"PlateGrid{plateId}") is not Grid plateGrid) return;

            // 修复：转换为 FrameworkElement（可访问 Tag）
            var consumableCanvas = plateGrid.Children.Cast<FrameworkElement>()
                .FirstOrDefault(child => child.Tag?.ToString() == "TopConsumable");
            if (consumableCanvas != null)
                plateGrid.Children.Remove(consumableCanvas);

            // 修复：转换为 FrameworkElement，判断是否为 TextBlock
            var bottomLayer = plateGrid.Children.Cast<FrameworkElement>()
                .FirstOrDefault(child => child.Tag?.ToString() == "BottomLayer");
            if (bottomLayer is TextBlock bottomTextBlock)
            {
                bottomTextBlock.Visibility = Visibility.Visible;
            }

            var nameLayer = plateGrid.Children.Cast<FrameworkElement>()
             .FirstOrDefault(child => child.Tag?.ToString() == "NameLayer");
            string moduleName = "";
            if (nameLayer is StackPanel nameStack)
            {
                var moduleNameText = nameStack.Children.Cast<TextBlock>().FirstOrDefault();
                if (moduleNameText != null && !string.IsNullOrEmpty(moduleNameText.Text))
                {

                    moduleName = moduleNameText.Text;
                }
                plateGrid.Children.Remove(nameLayer);
            }
            if (moduleName != "")
            {
                var nameStack2 = new StackPanel
                {
                    Tag = "NameLayer",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,

                };

                nameStack2.Children.Add(new TextBlock
                {
                    Text = moduleName,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Brushes.White
                });
                plateGrid.Children.Add(nameStack2

                    );
            }


            if (_plateConsumableMap.ContainsKey(plateId))
                _plateConsumableMap.Remove(plateId);
        }
        //切换动作区
        private void ActionSelectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ActionSelectComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;
            var selectedCategory = selectedItem.Tag;
            switch (selectedCategory)
            {
                case "All":
                    SetAllButtonsVisibility(Visibility.Visible);
                    break;
                case "Basic"://基础操作：吸液、分液、装枪头、卸枪头、转移、等待
                    ShowBaseButtons();
                    break;
                case "Module"://模块功能：振荡、磁力、温控、PCR
                    ShowModuleButtons();
                    break;
                case "Other"://其他
                    SetAllButtonsVisibility(Visibility.Collapsed);
                    LoopButton.Visibility = Visibility.Visible;
                    break;
            }
        }
        //显示基础按钮，隐藏模块按钮
        private void ShowBaseButtons()
        {
            // 基础按钮显示
            AspirateButton.Visibility = Visibility.Visible;
            DispenseButton.Visibility = Visibility.Visible;
            PickTipButton.Visibility = Visibility.Visible;
            EjectTipButton.Visibility = Visibility.Visible;
            if (AppGlobalConfig.Instance.IsGripperEnabled)
                ActionTransferButton.Visibility = Visibility.Visible;
            else
                ActionTransferButton.Visibility = Visibility.Collapsed;
            WaitButton.Visibility = Visibility.Visible;
            ActionMixButton.Visibility = Visibility.Visible;

            // 模块按钮隐藏
            ActionShakeButton.Visibility = Visibility.Collapsed;
            ActionMagneticButton.Visibility = Visibility.Collapsed;
            ActionTemperatureButton.Visibility = Visibility.Collapsed;
            ActionPCRButton.Visibility = Visibility.Collapsed;
            //其他按钮隐藏
            LoopButton.Visibility = Visibility.Collapsed;

        }
        // 显示模块按钮，隐藏基础按钮
        private void ShowModuleButtons()
        {
            // 模块按钮显示
            if (AppGlobalConfig.Instance.HasHeatingShaking())
                ActionShakeButton.Visibility = Visibility.Visible;
            else
                ActionShakeButton.Visibility = Visibility.Collapsed;
            if (AppGlobalConfig.Instance.HasMagnetic())
                ActionMagneticButton.Visibility = Visibility.Visible;
            else
                ActionMagneticButton.Visibility = Visibility.Collapsed;
            if (AppGlobalConfig.Instance.HasTemperatureControl())
                ActionTemperatureButton.Visibility = Visibility.Visible;
            else
                ActionTemperatureButton.Visibility = Visibility.Collapsed;
            if (AppGlobalConfig.Instance.IsPCREnabled)
                ActionPCRButton.Visibility = Visibility.Visible;
            else
                ActionPCRButton.Visibility = Visibility.Collapsed;

            // 基础按钮隐藏
            AspirateButton.Visibility = Visibility.Collapsed;
            DispenseButton.Visibility = Visibility.Collapsed;
            PickTipButton.Visibility = Visibility.Collapsed;
            EjectTipButton.Visibility = Visibility.Collapsed;
            ActionTransferButton.Visibility = Visibility.Collapsed;
            WaitButton.Visibility = Visibility.Collapsed;
            ActionMixButton.Visibility = Visibility.Collapsed;
            //其他按钮隐藏
            LoopButton.Visibility = Visibility.Collapsed;
        }

        // 批量设置所有按钮可见性
        private void SetAllButtonsVisibility(Visibility visibility)
        {
            AspirateButton.Visibility = visibility;
            DispenseButton.Visibility = visibility;
            PickTipButton.Visibility = visibility;
            EjectTipButton.Visibility = visibility;
            ActionTransferButton.Visibility = visibility;
            ActionShakeButton.Visibility = visibility;
            ActionMagneticButton.Visibility = visibility;
            ActionTemperatureButton.Visibility = visibility;
            ActionPCRButton.Visibility = visibility;
            WaitButton.Visibility = visibility;
            ActionMixButton.Visibility = visibility;
            LoopButton.Visibility = visibility;

            if (visibility == Visibility.Visible)
            {
                UpdateDeviceModule();
            }
        }
        // 点击动作功能区按钮时添加流程步骤
        //private void AddFlowStep(string type)
        //{
        //    int endStepIndex = FlowSteps.Count - 1;

        //    var step = new FlowStep
        //    {
        //        Index = _stepIndex++,
        //        Type = type,
        //        Volume = 50,
        //        TransferPosition = 0,
        //        Position = "P1",
        //        SelectedPipetteName = "pipette_1",
        //        IsSelected = false,
        //        IsSystemStep = false,
        //        Level = 0
        //    };
        //    FlowSteps.Insert(_stepClickIndex, step);
        //    _stepClickIndex++;
        //    // 重新编号所有步骤（确保序号连续）
        //    RebuildStepIndexes();
        //    if (type.Equals("loop", StringComparison.OrdinalIgnoreCase))
        //    {
        //        var stepLoop = new FlowStep
        //        {
        //            Index = _stepIndex++,
        //            Type = "endLoop",
        //            Volume = 50,
        //            TransferPosition = 0,
        //            Position = "P1",
        //            SelectedPipetteName = "pipette_1",
        //            IsSelected = false,
        //            IsSystemStep = false,
        //            Level = 1
        //        };
        //        FlowSteps.Insert(_stepClickIndex, stepLoop);
        //        _stepClickIndex++;
        //        // 重新编号所有步骤（确保序号连续）
        //        RebuildStepIndexes();
        //    }
        //}

        private void AddFlowStep(string type)
        {
            int endStepIndex = FlowSteps.Count - 1;

            var step = new FlowStep
            {
                Index = _stepIndex++,
                Type = type,
                Volume = 50,
                TransferPosition = 0,
                Position = "P1",
                SelectedPipetteName = "pipette_1",
                IsSelected = false,
                IsSystemStep = false,
                Level = _currentLevel // 用当前层级
            };
            FlowSteps.Insert(_stepClickIndex, step);
            _stepClickIndex++;

            if (type.Equals(_res.WindowActionLoop, StringComparison.OrdinalIgnoreCase))
            {
                // 插入 Loop 后，层级+1
                _currentLevel++;
                var stepLoop = new FlowStep
                {
                    Index = _stepIndex++,
                    Type = _res.WindowActionEndLoop,
                    Volume = 50,
                    TransferPosition = 0,
                    Position = "P1",
                    SelectedPipetteName = "pipette_1",
                    IsSelected = false,
                    IsSystemStep = false,
                    Level = _currentLevel - 1 // End Loop 和 Loop 同层级
                };
                FlowSteps.Insert(_stepClickIndex, stepLoop);
                //_stepClickIndex++;
                // 插入 End Loop 后，层级-1
                //_currentLevel--;
            }

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
                _stepClickIndex = step.Index;
                _currentLevel = step.Level;
                foreach (var s in FlowSteps)
                    s.IsSelected = false;

                // 选中当前步骤
                step.IsSelected = true;
                TopSettingPopup.setStepDetail(step);
                TopSettingPopup.Show(-1, ResourceHelper.Instance.WindowStepdetails, -1);
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


        private void AspirateButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlowStep("Aspirate");
        }

        private void DispenseButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlowStep("Dispense");
        }

        private void PickTipButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlowStep("TipOn");
        }

        private void EjectTipButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlowStep("TipOff");
        }

        private void WaitButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlowStep("Wait");
        }
        private void ShakeButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlowStep("Shake");
        }

        private void MagneticButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlowStep("Magnetic");
        }

        private void TemperatureButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlowStep("Temp Ctrl");
        }

        private void PCRButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlowStep("PCR");
        }
        private void TransferButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlowStep("Transfer");
        }
        private void MixButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlowStep("Mix");
        }
        private void LoopButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlowStep("Loop");
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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

            e.Handled = true;
        }

        // 重新编号步骤（删除后保持序号连续）
        private void RebuildStepIndexes()
        {
            //for (int i = 0; i < FlowSteps.Count; i++)
            //{
            //    FlowSteps[i].Index = i + 1;
            //}
            //_stepIndex = FlowSteps.Count + 1;

            // 局部栈：仅用于本次重建序号，每次重建都重新初始化
            Stack<int> levelCounters = new Stack<int>();
            // 初始压入0（外层层级的计数器），确保栈永远不为空
            levelCounters.Push(0);

            foreach (var step in FlowSteps)
            {
                // 先获取当前层级的计数器（用Peek()，只看不取，避免栈空）
                int currentCounter = levelCounters.Peek();

                if (step.Type.Equals("loop", StringComparison.OrdinalIgnoreCase))
                {
                    // 遇到Loop：当前层级计数器+1，然后压入新的内层计数器（初始0）
                    levelCounters.Pop(); // 取出当前层级计数器
                    levelCounters.Push(currentCounter + 1); // +1后放回
                    levelCounters.Push(0); // 新增内层计数器（Loop内部的步骤从0开始）
                }
                else if (step.Type.Equals("endLoop", StringComparison.OrdinalIgnoreCase))
                {
                    // 遇到EndLoop：弹出内层计数器，回到上一层
                    if (levelCounters.Count > 1) // 保留最外层的0，避免栈空
                    {
                        levelCounters.Pop();
                    }
                    // EndLoop本身用上层的序号，不需要+1
                    currentCounter = levelCounters.Peek();
                }
                else
                {
                    // 普通步骤：当前层级计数器+1
                    levelCounters.Pop(); // 取出当前计数器
                    levelCounters.Push(currentCounter + 1); // +1后放回
                    currentCounter = currentCounter + 1; // 更新当前计数器值
                }

                // 生成嵌套显示序号（过滤掉0，比如[0,4,1] → "4-1"）
                var counterList = levelCounters.Reverse().ToList();
                step.DisplayIndex = string.Join("-", counterList.Where(n => n > 0));

                // 全局Index仍保持连续（内部逻辑用）
                step.Index = FlowSteps.IndexOf(step) + 1;
            }

            // 更新全局计数器，避免新增步骤时索引重复
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

        /// <summary>
        /// 创建脚本python
        /// </summary>
        /// <returns>生成的脚本Python字符串</returns>
        private string CreateScriptPython()
        {
            StringBuilder pythonCode = new StringBuilder();
            // 1. 写入 Python 导入语句
            pythonCode.AppendLine("from typing import Tuple");
            pythonCode.AppendLine("from qyrobot import Robot");
            pythonCode.AppendLine("from plate import Plate");
            pythonCode.AppendLine("from arm import Arm");
            pythonCode.AppendLine("from pipe import Pipe");
            pythonCode.AppendLine("from gripper import Gripper");
            pythonCode.AppendLine("from shaker import Shaker");
            pythonCode.AppendLine("from magnetic import Magnetic");
            pythonCode.AppendLine("from cool import Cool");
            pythonCode.AppendLine("from pcr import Pcr");
            pythonCode.AppendLine("from consumables import consumables");
            pythonCode.AppendLine("from baseaction import (tipon, tipoff, aspirate, dispense, mixing , movePlate, wait)");
            pythonCode.AppendLine();
            string protocolName = AppGlobalConfig.Instance.GuideProtocolName;
            string author = AppGlobalConfig.Instance.GuideProtocolAuthor;
            string description = AppGlobalConfig.Instance.GuideProtocolDescription;
            AppGlobalConfig.Instance.GuideProtocolStartTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH_mm_ss");
            string stepsNum = FlowSteps.Count().ToString();

            // 2. 写入 metadata 字典
            pythonCode.AppendLine("metadata = {");
            pythonCode.AppendLine($"    \"protocolName\": \"{protocolName}\",");
            pythonCode.AppendLine($"    \"author\": \"{author}\",");
            pythonCode.AppendLine($"    \"description\": \"{description}\",");
            pythonCode.AppendLine($"    \"created\": \"{AppGlobalConfig.Instance.GuideProtocolStartTime}\",");
            pythonCode.AppendLine($"    \"steps\": \"{stepsNum}\",");
            pythonCode.AppendLine("}");
            List<string> moduleList = new List<string>(new string[15]);
            for (int plateIndex = 0; plateIndex < 15; plateIndex++)
            {
                string plateId = (plateIndex + 1).ToString();
                var plateModule = AppGlobalConfig.Instance.PlateModuleMap.Values
                    .FirstOrDefault(module =>
                        !string.IsNullOrEmpty(module.PlatePosition) &&
                        module.PlatePosition == plateId);
                if (plateId == "13" || plateId == "14")
                {
                    if (plateModule != null)
                    {
                        string pipetteStr = $"{plateModule.Name}|{plateModule.Type}|{plateModule.PipetteVolume}";
                        moduleList[plateIndex] = pipetteStr;
                    }
                    else
                    {
                        moduleList[plateIndex] = ""; // 无模块时仍存空字符串
                    }

                }
                else
                {
                    moduleList[plateIndex] = plateModule?.Name ?? "";
                }
            }
            List<string> consumableList = new List<string>(new string[12]);
            for (int plateIndex = 0; plateIndex < 12; plateIndex++)
            {
                string plateId = (plateIndex + 1).ToString();
                string plateName = $"P{plateId}";

                _plateConsumableMap.TryGetValue(plateId, out var plateConsumable);
                consumableList[plateIndex] = plateConsumable?.Name ?? "";
            }
            string moduleListStr = string.Join(", ", moduleList.Select(m => $"\"{m}\""));
            pythonCode.AppendLine($"plate_modules = [{moduleListStr}]");
            string consumableListStr = string.Join(", ", consumableList.Select(c => $"\"{c}\""));
            pythonCode.AppendLine($"plate_consumables = [{consumableListStr}]");
            pythonCode.AppendLine();

            // 3. 写入耗材
            List<string> writtenConsNames = new List<string>();
            foreach (var plateConsumable in _plateConsumableMap)
            {
                string plateId = plateConsumable.Key; // 板位编号（如P1、P10）
                var consumable = plateConsumable.Value; // 耗材信息
                var settings = consumable.Settings; // 耗材详细参数
                if (writtenConsNames.Contains(settings.name))
                {
                    continue;
                }
                writtenConsNames.Add(settings.name);
                string rawVarName = string.IsNullOrWhiteSpace(settings.name) ? plateId : settings.name;
                string validVarName = System.Text.RegularExpressions.Regex.Replace(rawVarName, @"[^a-zA-Z0-9_]", "_");
                if (!char.IsLetter(validVarName[0]))
                {
                    validVarName = $"cons_{validVarName}";
                }
                // 1. 初始化consumables实例
                pythonCode.AppendLine($"{validVarName} = consumables()");

                // 2. 基础尺寸（对应示例的length/width/height）
                pythonCode.AppendLine($"{validVarName}.length = {settings.labL:F2}");
                pythonCode.AppendLine($"{validVarName}.width = {settings.labW:F2}");
                pythonCode.AppendLine($"{validVarName}.height = {settings.labH:F2}");

                // 3. 孔深度（well_depth → consDep）
                pythonCode.AppendLine($"{validVarName}.well_depth = {settings.consDep:F2}");
                // 3. 下压深度（tip_take_depth → TIPDepthOFComp）
                pythonCode.AppendLine($"{validVarName}.tip_take_depth = {settings.TIPDepthOFComp:F2}");
                // 3. TIP高度度（tip_height → TIPConeLength）枪身总长度
                pythonCode.AppendLine($"{validVarName}.tip_height = {settings.TIPConeLength:F2}");
                // 4. offset偏移（元组：(offsetX, offsetY)）#耗材中心点距离A1孔的距离
                float offsetX = settings.offsetX;
                float offsetY = settings.offsetY;
                pythonCode.AppendLine($"{validVarName}.offset = ({offsetX:F2}, {offsetY:F2})"); // 保留1位小数，匹配示例格式

                // 5. pitch间距（元组：(distanceRow, distanceColumn) → 行/列间距）#行间距、列间距
                float pitchRow = settings.distanceColumn;
                float pitchCol = settings.distanceRow;
                pythonCode.AppendLine($"{validVarName}.pitch = ({pitchRow:F2}, {pitchCol:F2})");


                // 6. cath_offset（元组：(RobotX, RobotY, RobotZ)）
                float cathX = settings.RobotX;
                float cathY = settings.RobotY;
                float cathZ = settings.RobotZ;
                pythonCode.AppendLine($"{validVarName}.cath_offset = ({cathX:F2}, {cathY:F2}, {cathZ:F2})");
                //绘图用，剩余参数
                pythonCode.AppendLine($"{validVarName}.name = \"{settings.name ?? string.Empty}\"");
                pythonCode.AppendLine($"{validVarName}.id = {settings.id}");
                pythonCode.AppendLine($"{validVarName}.type = {settings.type}");
                pythonCode.AppendLine($"{validVarName}.description = \"{settings.description ?? string.Empty}\"");
                pythonCode.AppendLine($"{validVarName}.NW = {settings.NW}");
                pythonCode.AppendLine($"{validVarName}.SW = {settings.SW}");
                pythonCode.AppendLine($"{validVarName}.NE = {settings.NE}");
                pythonCode.AppendLine($"{validVarName}.SE = {settings.SE}");
                pythonCode.AppendLine($"{validVarName}.numRows = {settings.numRows}"); // 行数
                pythonCode.AppendLine($"{validVarName}.numColumns = {settings.numColumns}"); // 列数
                pythonCode.AppendLine($"{validVarName}.distanceRowY = {settings.distanceRowY:F2}"); // A1孔距离X
                pythonCode.AppendLine($"{validVarName}.distanceColumnX = {settings.distanceColumnX:F2}"); // A1孔距离Y
                pythonCode.AppendLine($"{validVarName}.labVolume = {settings.labVolume:F2}"); // 最大容量
                pythonCode.AppendLine($"{validVarName}.consMaxAvaiVol = {settings.consMaxAvaiVol:F2}"); // 可用最大容量
                pythonCode.AppendLine($"{validVarName}.topShape = {settings.topShape}"); // 孔顶部形状（int类型）
                pythonCode.AppendLine($"{validVarName}.topRadius = {settings.topRadius:F2}"); // 孔顶部半径
                pythonCode.AppendLine($"{validVarName}.topUpperX = {settings.topUpperX:F2}"); // 孔顶部上沿X尺寸
                pythonCode.AppendLine($"{validVarName}.topUpperY = {settings.topUpperY:F2}"); // 孔顶部上沿Y尺寸
                pythonCode.AppendLine($"{validVarName}.TIPMAXCapacity = {settings.TIPMAXCapacity:F2}"); // 枪头最大容量
                pythonCode.AppendLine($"{validVarName}.TIPMAXAvailable = {settings.TIPMAXAvailable:F2}"); // 枪头最大可用容量
                pythonCode.AppendLine($"{validVarName}.TIPTotalLength = {settings.TIPTotalLength:F2}"); // 枪头总长度
                pythonCode.AppendLine($"{validVarName}.TIPHeadHeight = {settings.TIPHeadHeight:F2}"); // 枪头头部高度
                pythonCode.AppendLine($"{validVarName}.TIPConeLength = {settings.TIPConeLength:F2}"); // 枪头锥体长
                pythonCode.AppendLine($"{validVarName}.TIPMAXRadius = {settings.TIPMAXRadius:F2}"); // 枪头最大半径
                pythonCode.AppendLine($"{validVarName}.TIPMINRadius = {settings.TIPMINRadius:F2}"); // 枪头最小半径
                //pythonCode.AppendLine($"{validVarName}.TIPDepthOFComp = {settings.TIPDepthOFComp:F2}"); // 枪头压缩深度
                pythonCode.AppendLine();
            }
            Dictionary<string, string> moduleIdDict = new Dictionary<string, string>();
            foreach (var kvp in AppGlobalConfig.Instance.PlateModuleMap)
            {
                int type = kvp.Value.Type;
                string moduleName = kvp.Value.Name;
                switch (type)
                {
                    case 0://单通道
                        string[] PIPETTEparts = moduleName.Split('_', StringSplitOptions.RemoveEmptyEntries);
                        //moduleIdDict.Add(moduleName, "PIPETTE_ID_" + kvp.Key);
                        moduleIdDict.Add(moduleName, PIPETTEparts.Last());
                        pythonCode.AppendLine("PIPETTE_ID_" + kvp.Key + "=" + PIPETTEparts.Last());
                        break;
                    case 1://8通道
                        string[] PIPETTEparts2 = moduleName.Split('_', StringSplitOptions.RemoveEmptyEntries);
                        //moduleIdDict.Add(moduleName, "PIPETTE_ID_" + kvp.Key);
                        moduleIdDict.Add(moduleName, PIPETTEparts2.Last());
                        pythonCode.AppendLine("PIPETTE_ID_" + kvp.Key + "=" + PIPETTEparts2.Last());
                        break;
                    case 2://96通道
                        string[] PIPETTEparts96 = moduleName.Split('_', StringSplitOptions.RemoveEmptyEntries);
                        //moduleIdDict.Add(moduleName, "PIPETTE_ID_" + kvp.Key);
                        moduleIdDict.Add(moduleName, PIPETTEparts96.Last());
                        pythonCode.AppendLine("PIPETTE_ID_" + kvp.Key + "=" + PIPETTEparts96.Last());
                        break;
                    case 3://抓手
                        string[] GRIPPERparts = moduleName.Split('_', StringSplitOptions.RemoveEmptyEntries);
                        //moduleIdDict.Add(moduleName, "GRIPPER_ID_" + kvp.Key);
                        moduleIdDict.Add(moduleName, GRIPPERparts.Last());
                        pythonCode.AppendLine("GRIPPER_ID_" + kvp.Key + "=" + GRIPPERparts.Last());
                        break;
                    case 4://PCR
                        moduleIdDict.Add(moduleName, "1");
                        pythonCode.AppendLine("PCR_ID_10 = 1");
                        break;
                    case 5://SHAKER
                        string[] SHAKERparts = moduleName.Split('_', StringSplitOptions.RemoveEmptyEntries);
                        //moduleIdDict.Add(moduleName, "SHAKER_ID_" + kvp.Key);
                        moduleIdDict.Add(moduleName, SHAKERparts.Last());
                        pythonCode.AppendLine("SHAKER_ID_" + kvp.Key + "=" + SHAKERparts.Last());
                        break;
                    case 6://MAGNETIC
                        string[] MAGNETICparts = moduleName.Split('_', StringSplitOptions.RemoveEmptyEntries);
                        //moduleIdDict.Add(moduleName, "MAGNETIC_ID_" + kvp.Key);
                        moduleIdDict.Add(moduleName, MAGNETICparts.Last());
                        pythonCode.AppendLine("MAGNETIC_ID_" + kvp.Key + "=" + MAGNETICparts.Last());
                        break;
                    case 7://TEMPCTRL
                        string[] TEMPCTRLparts = moduleName.Split('_', StringSplitOptions.RemoveEmptyEntries);
                        //moduleIdDict.Add(moduleName, "TEMPCTRL_ID_" + kvp.Key);
                        moduleIdDict.Add(moduleName, TEMPCTRLparts.Last());
                        pythonCode.AppendLine("TEMPCTRL_ID_" + kvp.Key + "=" + TEMPCTRLparts.Last());
                        break;
                }
            }
            pythonCode.AppendLine();
            pythonCode.AppendLine("def run():");
            pythonCode.AppendLine($"    Robot.reset()");
            foreach (var flowStep in FlowSteps)
            {
                string stepType = MapStepType(flowStep.Type);
                var pipperName = flowStep.SelectedPipetteName;
                var moduleName = flowStep.ModuleName;


                switch (stepType)
                {
                    case "aspirate":
                        string aspirateCons = GetConsumableVarNameByPlate(flowStep.Position);
                        if (moduleIdDict.TryGetValue(pipperName, out string aisPipetteId))
                        {
                            var (aisRowNum, aisColNum) = ParsePipettePosition(flowStep.SelectedCells);
                            pythonCode.AppendLine($"    # 吸液（{flowStep.Position} {flowStep.WellPosition}，{flowStep.Volume}μL）");
                            pythonCode.AppendLine($"    aspirate(pipe_id={aisPipetteId}, plate=\"{MapPlatePosition(flowStep.Position)}\",  cons={aspirateCons},row={aisRowNum}, col={aisColNum}, depth={aspirateCons}.bottom({flowStep.LiquidAisDistance:F2}), vol={flowStep.Volume:F2}, speed={flowStep.LiquidAisSpeed:F2})");
                        }
                        break;

                    case "dispense":
                        string dispenseCons = GetConsumableVarNameByPlate(flowStep.Position);
                        var (disRowNum, disColNum) = ParsePipettePosition(flowStep.SelectedCells);
                        if (moduleIdDict.TryGetValue(pipperName, out string disPipetteId))
                        {
                            pythonCode.AppendLine($"    # 注液（{flowStep.Position} {flowStep.WellPosition}，{flowStep.Volume}μL）");
                            pythonCode.AppendLine($"    dispense(pipe_id={disPipetteId}, plate=\"{MapPlatePosition(flowStep.Position)}\",  cons={dispenseCons},row={disRowNum},  col={disColNum}, depth={dispenseCons}.bottom({flowStep.LiquidDisDistance:F2}), vol={flowStep.Volume:F2}, speed={flowStep.LiquidDisSpeed:F2})");
                        }
                        break;
                    case "mix":
                        string mixingCons = GetConsumableVarNameByPlate(flowStep.Position);
                        var (misRowNum, misColNum) = ParsePipettePosition(flowStep.SelectedCells);
                        if (moduleIdDict.TryGetValue(pipperName, out string mixPipetteId))
                        {
                            pythonCode.AppendLine($"    # 混合（{flowStep.MixVolume}μL，{flowStep.MixCount}轮）");
                            pythonCode.AppendLine($"    mixing(pipe_id={mixPipetteId}, plate=\"{MapPlatePosition(flowStep.Position)}\", cons={mixingCons},row={misRowNum}, col={misColNum}, vol={flowStep.MixVolume:F2}, rounds={flowStep.MixCount}, speed={flowStep.LiquidAisSpeed:F2}, depth={mixingCons}.bottom({flowStep.LiquidAisDistance:F2}))");
                        }
                        break;
                    case "tipon": // 取头
                        string tiponCons = GetConsumableVarNameByPlate(flowStep.Position); // 根据板位获取耗材变量名
                        var (tipOnRowNum, tipOnColNum) = ParsePipettePosition(flowStep.SelectedCells);
                        if (moduleIdDict.TryGetValue(pipperName, out string tipOnPipetteId))
                        {
                            pythonCode.AppendLine($"    # 取头（{flowStep.Position} {flowStep.WellPosition}）");
                            pythonCode.AppendLine($"    tipon(pipe_id={tipOnPipetteId}, plate=\"{MapPlatePosition(flowStep.Position)}\",  cons={tiponCons},row={tipOnRowNum},  col={tipOnColNum})");
                        }

                        break;
                    case "tipoff": // 退头
                        string tipoffCons = GetConsumableVarNameByPlate(flowStep.Position);
                        var (tipOffRowNum, tipOffColNum) = ParsePipettePosition(flowStep.SelectedCells);
                        if (moduleIdDict.TryGetValue(pipperName, out string tipOffPipetteId))
                        {
                            pythonCode.AppendLine($"    # 退头（{flowStep.Position} {flowStep.WellPosition}）");
                            pythonCode.AppendLine($"    tipoff(pipe_id={tipOffPipetteId}, plate=\"{MapPlatePosition(flowStep.Position)}\",   cons={tipoffCons},row={tipOffRowNum},  col={tipOffColNum})");
                        }

                        break;

                    case "wait":
                        int waitTimeS = flowStep.WaitTime > 0 ? flowStep.WaitTime : 0;
                        pythonCode.AppendLine($"    # 等待（{flowStep.WaitContent}）");
                        pythonCode.AppendLine($"    wait(s={waitTimeS})");
                        break;
                    case "shake":
                        if (moduleIdDict.TryGetValue(moduleName, out string shakeModuleId))
                        {
                            pythonCode.AppendLine($"    # 混匀振荡（{flowStep.ShakeRPM}rpm，{flowStep.WaitTime}秒）");
                            pythonCode.AppendLine($"    Shaker.start_temp(id={shakeModuleId}, temp={flowStep.ShakeTemp:F1})");
                            pythonCode.AppendLine($"    Shaker.start_shaker(id={shakeModuleId}, rpm={flowStep.ShakeRPM}, time={flowStep.WaitTime})");
                        }

                        break;
                    case "magnetic":
                        if (moduleIdDict.TryGetValue(moduleName, out string magneticModuleId))
                        {
                            string magneticAction = flowStep.IsMagnetUp ? "on" : "off";
                            pythonCode.AppendLine($"    # 磁吸{(flowStep.IsMagnetUp ? "上升" : "下降")}");
                            pythonCode.AppendLine($"    Magnetic.{magneticAction}(id={magneticModuleId})");
                        }

                        break;
                    case "temp ctrl":
                        if (moduleIdDict.TryGetValue(moduleName, out string tempModuleId))
                        {
                            if (flowStep.IsTempCtrlOpen)
                            {
                                pythonCode.AppendLine($"    # 温控（{flowStep.ModuleName}，{flowStep.TempCtrlTemp}℃）");
                                pythonCode.AppendLine($"    Cool.start(id={tempModuleId}, temp={flowStep.TempCtrlTemp})");
                            }
                            else
                            {
                                pythonCode.AppendLine($"    # 温控（{flowStep.ModuleName}，{flowStep.TempCtrlTemp}℃）");
                                pythonCode.AppendLine($"    Cool.stop(id={tempModuleId})");
                            }

                        }
                        break;
                    case "transfer":
                        if (moduleIdDict.TryGetValue("gripper_1", out string shiftModuleId))
                        {
                            string shiftCons = GetConsumableVarNameByPlate(flowStep.FromPos);
                            pythonCode.AppendLine($"    # 移板（{flowStep.FromPos} → {flowStep.ToPos}）");
                            pythonCode.AppendLine($"    movePlate(id={shiftModuleId},cons={shiftCons}, from_plate=\"{MapPlatePosition(flowStep.FromPos)}\", to_plate=\"{MapPlatePosition(flowStep.ToPos)}\", pushing={flowStep.TransferPosition})");
                        }

                        break;
                    case "pcr":
                        if (flowStep.PcrStep == _res.SettingManualPCRStart)
                        {
                            pythonCode.AppendLine($"    # PCR（{flowStep.PcrStep}）");
                            pythonCode.AppendLine($"    Pcr.run(id={1},data=\"{flowStep.PcrScriptAdress}\")");
                        }
                        else if (flowStep.PcrStep == _res.SettingManualPCRStop)
                        {
                            pythonCode.AppendLine($"    # PCR（{flowStep.PcrStep}）");
                            pythonCode.AppendLine($"    Pcr.stop(id={1})");
                        }
                        else if (flowStep.PcrStep == _res.SettingManualPCROpen)
                        {
                            pythonCode.AppendLine($"    # PCR（{flowStep.PcrStep}）");
                            pythonCode.AppendLine($"    Pcr.opendoor(id={1})");
                        }
                        else if (flowStep.PcrStep == _res.SettingManualPCRClose)
                        {
                            pythonCode.AppendLine($"    # PCR（{flowStep.PcrStep}）");
                            pythonCode.AppendLine($"    Pcr.closedoor(id={1})");

                        }

                        break;
                }
                pythonCode.AppendLine();
            }



            string fullPythonCode = pythonCode.ToString();
            return fullPythonCode;
        }
        // ========== 辅助函数（需根据你的业务逻辑实现） ==========
        /// <summary>
        /// 根据板位获取耗材变量名（如P1 → tipbox_1000）
        /// </summary>
        private string GetConsumableVarNameByPlate(string plateId)
        {
            string plateIdNum = plateId.Replace("P", "");

            // 需根据_plateConsumableMap映射，示例逻辑：
            if (_plateConsumableMap.TryGetValue(plateIdNum, out var consumable))
            {
                string rawName = string.IsNullOrWhiteSpace(consumable.Settings.name) ? plateId : consumable.Settings.name;
                string validName = System.Text.RegularExpressions.Regex.Replace(rawName, @"[^a-zA-Z0-9_]", "_");
                if (!char.IsLetter(validName[0])) validName = $"cons_{validName}";
                return validName;
            }
            return "";
        }
        public void ExportGridToPngFile(Grid grid, string saveFolderPath, double dpi = 91.5, string fileName = "grid_export.png")
        {
            // 1. 确保 Grid 已经完成布局和渲染
            grid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            grid.Arrange(new Rect(grid.DesiredSize));
            grid.UpdateLayout();

            // 2. 创建 RenderTargetBitmap 来渲染 Grid
            var renderBitmap = new RenderTargetBitmap(
                (int)grid.ActualWidth,
                (int)grid.ActualHeight,
                95.5, 91.5, // DPI 设置
                PixelFormats.Pbgra32);


            // 3. 渲染 Grid
            renderBitmap.Render(grid);

            // 7. 拼接完整的图片保存路径
            string fullImagePath = Path.Combine(saveFolderPath, fileName);
            // 处理文件名后缀（确保是.png）
            if (Path.GetExtension(fullImagePath).ToLower() != ".png")
            {
                fullImagePath = Path.ChangeExtension(fullImagePath, ".png");
            }

            // 8. 创建BitmapEncoder并保存为PNG文件
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            // 9. 写入文件（使用using确保文件流释放，避免文件被占用）
            using (var fileStream = new FileStream(fullImagePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                encoder.Save(fileStream);
            }
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
                "振荡" => "shaker",
                "磁吸" => "magnetic",
                "温控" => "tempctrl",
                "转载" => "shift",
                "混合" => "mixing",
                "循环" => "loop",
                "结束循环" => "endloop",
                _ => flowStepType.ToLower()
            };
        }

        //映射板位
        private string MapPlatePosition(string position)
        {
            // 假设position格式为"P3"、"P9"、"P1"等
            //string plateId = position.Replace("P", "");
            //if (plateId == "3") return "magnetic_1"; // 假设P3对应magnetic_1
            //if (plateId == "9") return "shaker_1";   // 假设P9对应shaker_1
            //return "p" + plateId;
            string plateId = position.Replace("P", "");
            return "p" + plateId;
        }
        /// <summary>
        /// 解析移液器孔位字符串（支持单孔位/8通道两种格式）
        /// 格式1："2,5" → row=2，col=5
        /// 格式2："1,4;2,4;3,4;4,4;5,4;6,4;7,4;8,4" → row=1，col=4（8通道取首行+公共列）
        /// </summary>
        /// <param name="positionStr">待解析字符串</param>
        /// <returns>解析后的(row, col)，解析失败返回(0,0)</returns>
        /// <exception cref="ArgumentException">格式非法时抛出</exception>
        /// 
        /* private Tuple<int, int> ParsePipettePosition(string positionStr)
         {

             if (positionStr.Contains(";"))
             {
                 // 8通道格式解析
                 string[] channelParts = positionStr.Split(';', StringSplitOptions.RemoveEmptyEntries);

                 if (channelParts.Length != 8)
                 {
                     throw new ArgumentException($"8通道格式必须包含8组孔位，当前仅{channelParts.Length}组", nameof(positionStr));
                 }

                 int? commonCol = null;
                 int firstRow = 0;

                 // 遍历解析每一组，校验列是否一致
                 for (int i = 0; i < channelParts.Length; i++)
                 {
                     string[] rowCol = channelParts[i].Split(',', StringSplitOptions.RemoveEmptyEntries);

                     // 单组格式校验（必须是"行,列"）
                     if (rowCol.Length != 2 || !int.TryParse(rowCol[0], out int row) || !int.TryParse(rowCol[1], out int col))
                     {
                         throw new ArgumentException($"8通道第{i + 1}组格式错误：{channelParts[i]}，正确格式应为\"行,列\"", nameof(positionStr));
                     }

                     // 记录第一行，校验所有列是否一致
                     if (i == 0)
                     {
                         firstRow = row;
                         commonCol = col;
                     }
                     else
                     {
                         if (col != commonCol)
                         {
                             throw new ArgumentException($"8通道列号不一致，第1组列={commonCol}，第{i + 1}组列={col}", nameof(positionStr));
                         }
                     }
                 }

                 // 返回8通道结果：首行 + 公共列
                 return Tuple.Create(firstRow, commonCol.Value);
             }
             else
             {
                 // 单孔位格式解析
                 string[] rowCol = positionStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                 if (rowCol.Length != 2 || !int.TryParse(rowCol[0], out int row) || !int.TryParse(rowCol[1], out int col))
                 {
                     throw new ArgumentException($"单孔位格式错误：{positionStr}，正确格式应为\"行,列\"", nameof(positionStr));
                 }

                 // 返回单孔位结果
                 return Tuple.Create(row, col);
             }


         }
         */

        /// <summary>
        /// 解析移液器孔位字符串（支持单孔位/8通道/96通道三种格式）
        /// 格式1（单通道）："2,5" → row=2，col=5
        /// 格式2（8通道）："1,4;2,4;3,4;4,4;5,4;6,4;7,4;8,4" → row=1，col=4（8通道取首行+公共列）
        /// 格式3（96通道）：包含96组「行,列」（用;分隔），覆盖整板所有孔位 → 返回(1,1)（约定1,1代表整板）
        /// </summary>
        /// <param name="positionStr">待解析字符串</param>
        /// <returns>解析后的(row, col)：
        /// 单通道/8通道返回对应行列表；96通道返回(0,0)；解析失败返回(0,0)
        /// </returns>
        /// <exception cref="ArgumentException">格式非法时抛出</exception>
        private Tuple<int, int> ParsePipettePosition(string positionStr)
        {
            // 空值/空字符串校验
            if (string.IsNullOrWhiteSpace(positionStr))
            {
                throw new ArgumentException("孔位字符串不能为空", nameof(positionStr));
            }

            string[] allParts = positionStr.Split(';', StringSplitOptions.RemoveEmptyEntries);

            // 分支1：96通道格式解析（96组孔位）
            if (allParts.Length == 96)
            {
                // 存储所有解析后的(row, col)
                var cellList = new List<(int Row, int Col)>();

                // 遍历解析每一组孔位
                for (int i = 0; i < allParts.Length; i++)
                {
                    string[] rowCol = allParts[i].Split(',', StringSplitOptions.RemoveEmptyEntries);

                    // 单组格式校验（必须是"行,列"）
                    if (rowCol.Length != 2 || !int.TryParse(rowCol[0], out int row) || !int.TryParse(rowCol[1], out int col))
                    {
                        throw new ArgumentException($"96通道第{i + 1}组格式错误：{allParts[i]}，正确格式应为\"行,列\"", nameof(positionStr));
                    }

                    // 校验行列有效性（适配标准96孔板：行1~8，列1~12）
                    if (row < 1 || row > 8 || col < 1 || col > 12)
                    {
                        throw new ArgumentException($"96通道第{i + 1}组行列值非法：行={row}，列={col}，标准96孔板行范围1~8，列范围1~12", nameof(positionStr));
                    }

                    cellList.Add((row, col));
                }

                // 校验是否覆盖整板（可选：确保行1~8、列1~12都存在）
                var distinctRows = cellList.Select(c => c.Row).Distinct().OrderBy(r => r).ToList();
                var distinctCols = cellList.Select(c => c.Col).Distinct().OrderBy(c => c).ToList();
                bool isFullPlate = distinctRows.SequenceEqual(Enumerable.Range(1, 8)) && distinctCols.SequenceEqual(Enumerable.Range(1, 12));
                if (!isFullPlate)
                {
                    throw new ArgumentException("96通道格式包含96组孔位，但未覆盖标准96孔板所有行（1~8）和列（1~12）", nameof(positionStr));
                }

                // 96通道返回约定值(0,0)代表整板
                return Tuple.Create(1, 1);
            }
            // 分支2：8通道格式解析（8组孔位）
            else if (allParts.Length == 8)
            {
                int? commonCol = null;
                int firstRow = 0;

                // 遍历解析每一组，校验列是否一致
                for (int i = 0; i < allParts.Length; i++)
                {
                    string[] rowCol = allParts[i].Split(',', StringSplitOptions.RemoveEmptyEntries);

                    // 单组格式校验（必须是"行,列"）
                    if (rowCol.Length != 2 || !int.TryParse(rowCol[0], out int row) || !int.TryParse(rowCol[1], out int col))
                    {
                        throw new ArgumentException($"8通道第{i + 1}组格式错误：{allParts[i]}，正确格式应为\"行,列\"", nameof(positionStr));
                    }

                    // 校验行列有效性（可选：适配96孔板行范围）
                    if (row < 1 || row > 8 || col < 1 || col > 12)
                    {
                        throw new ArgumentException($"8通道第{i + 1}组行列值非法：行={row}，列={col}，标准96孔板行范围1~8，列范围1~12", nameof(positionStr));
                    }

                    // 记录第一行，校验所有列是否一致
                    if (i == 0)
                    {
                        firstRow = row;
                        commonCol = col;
                    }
                    else
                    {
                        if (col != commonCol)
                        {
                            throw new ArgumentException($"8通道列号不一致，第1组列={commonCol}，第{i + 1}组列={col}", nameof(positionStr));
                        }
                    }
                }

                // 返回8通道结果：首行 + 公共列
                return Tuple.Create(firstRow, commonCol.Value);
            }
            // 分支3：单通道格式解析（仅1组孔位）
            else if (allParts.Length == 1)
            {
                string[] rowCol = allParts[0].Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (rowCol.Length != 2 || !int.TryParse(rowCol[0], out int row) || !int.TryParse(rowCol[1], out int col))
                {
                    throw new ArgumentException($"单孔位格式错误：{positionStr}，正确格式应为\"行,列\"", nameof(positionStr));
                }

                // 校验行列有效性（可选）
                if (row < 1 || row > 8 || col < 1 || col > 12)
                {
                    throw new ArgumentException($"单孔位行列值非法：行={row}，列={col}，标准96孔板行范围1~8，列范围1~12", nameof(positionStr));
                }

                // 返回单孔位结果
                return Tuple.Create(row, col);
            }
            // 格式不匹配（既不是1/8/96组）
            else
            {
                throw new ArgumentException($"孔位字符串格式错误：{positionStr}，仅支持单孔位（1组）、8通道（8组）、96通道（96组）格式", nameof(positionStr));
            }
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
            ShowNotification(_res.GrpcInitStart, NotificationControl.NotificationType.Info);

            if (!runFlag && !pauseFlag)
            {

                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from qyrobot import Robot");
                pythonCode.AppendLine("Robot.reset()");

                var rawInitFlag = await ScriptDebugAsync(pythonCode.ToString());//open
                var initFlag = ParseScriptDebugResponse(rawInitFlag);
                if (initFlag != null)
                {
                    if (initFlag.Result == "succeed")
                    {
                        ShowNotification(_res.GrpcInitSucc, NotificationControl.NotificationType.Info);
                    }
                }
                else
                {
                    ShowNotification(_res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
                }

            }
            else
            {
                ShowNotification(_res.GrpcStartRunning, NotificationControl.NotificationType.Warn);
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
                    string fileExtension = System.IO.Path.GetExtension(openFileDialog.FileName).ToLowerInvariant();
                    string scriptJson = File.ReadAllText(openFileDialog.FileName);
                    if (string.IsNullOrEmpty(scriptJson))
                    {
                        ShowNotification(_res.OpenFileDialog_Empty, NotificationControl.NotificationType.Warn);
                        return;
                    }
                    switch (fileExtension)
                    {
                        case ".py":
                            LoadScriptFromPython(scriptJson);
                            break;
                        case ".xlsx":
                        case ".xls":
                        case ".csv":
                            Debug.WriteLine(fileExtension);
                            break;
                        default:
                            ShowNotification(_res.OpenFileDialog_ErrFormal, NotificationControl.NotificationType.Warn);
                            return;
                    }
                }
                catch (Exception ex)
                {
                    ShowNotification($"{_res.OpenFileDialog_Error}: {ex.Message}", NotificationControl.NotificationType.Error);
                }
            }
        }
        private async void LoadScriptFromPython(string scriptContent)
        {
            try
            {
                FlowSteps.Clear();
                _plateConsumableMap.Clear();
                _stepIndex = 3;
                int isLeftSigna = -1;//0:单通道；1：八通道；2：96通道
                int isRightSigna = -1;//0:单通道；1：八通道；2：96通道

                #region metadata
                Match metadataMatch = Regex.Match(
     scriptContent,
     @"metadata = \{([\s\S]*?)\}",
     RegexOptions.Multiline | RegexOptions.IgnoreCase
 );
                if (metadataMatch.Success)
                {
                    string metadataContent = metadataMatch.Groups[1].Value;

                    Match protoNameMatch = Regex.Match(metadataContent, @"\""protocolName\""\s*:\s*\""(.*?)\""");
                    if (protoNameMatch.Success)
                    {
                        AppGlobalConfig.Instance.GuideProtocolName = protoNameMatch.Groups[1].Value;
                    }

                    // 解析author
                    Match authorMatch = Regex.Match(metadataContent, @"\""author\""\s*:\s*\""(.*?)\""");
                    if (authorMatch.Success)
                    {
                        AppGlobalConfig.Instance.GuideProtocolAuthor = authorMatch.Groups[1].Value;
                    }

                    // 解析description
                    Match descMatch = Regex.Match(metadataContent, @"\""description\""\s*:\s*\""(.*?)\""");
                    if (descMatch.Success)
                    {
                        AppGlobalConfig.Instance.GuideProtocolDescription = descMatch.Groups[1].Value;
                    }
                }
                #endregion
                #region plate_modules 模块列表
                Match moduleListMatch = Regex.Match(scriptContent, @"plate_modules = \[(.*?)\]", RegexOptions.Singleline);
                List<string> plateModules = new List<string>();
                if (moduleListMatch.Success)
                {
                    string moduleItems = moduleListMatch.Groups[1].Value;
                    plateModules = moduleItems.Split(',')
                        .Select(item => item.Trim().Trim('"'))
                        .ToList();
                }
                for (int i = 0; i < plateModules.Count; i++)
                {
                    string moduleItem = plateModules[i];
                    int plateIndex = i + 1;
                    string platePosition = plateIndex.ToString();
                    if (moduleItem.Contains("PCR"))
                    {
                        AppGlobalConfig.Instance.IsPCREnabled = true;
                        var pcrModule = new ModuleDatas
                        {
                            Name = "PCR",
                            Type = 4,
                            PlatePosition = "10",
                            PipetteVolume = 0,
                            ModuleImage = "/OctoFixFlow;component/images/PCR.png"
                        };
                        AppGlobalConfig.Instance.AddOrUpdateModule("10", pcrModule);
                    }
                    else if (moduleItem.Contains("tempctrl"))//温控
                    {
                        var moduleData = new ModuleDatas
                        {
                            Name = moduleItem,
                            Type = 7,
                            PlatePosition = platePosition,
                            PipetteVolume = 0,
                            ModuleImage = "/OctoFixFlow;component/images/Temp.png"
                        };

                        AppGlobalConfig.Instance.AddOrUpdateModule(platePosition, moduleData);
                    }
                    else if (moduleItem.Contains("shaker"))//加热振荡
                    {
                        var moduleData = new ModuleDatas
                        {
                            Name = moduleItem,
                            Type = 5,
                            PlatePosition = platePosition,
                            PipetteVolume = 0,
                            ModuleImage = "/OctoFixFlow;component/images/MixedHeating.png"
                        };

                        AppGlobalConfig.Instance.AddOrUpdateModule(platePosition, moduleData);
                    }
                    else if (moduleItem.Contains("magnetic"))//磁吸
                    {
                        var moduleData = new ModuleDatas
                        {
                            Name = moduleItem,
                            Type = 6,
                            PlatePosition = platePosition,
                            PipetteVolume = 0,
                            ModuleImage = "/OctoFixFlow;component/images/Magnet.png"
                        };

                        AppGlobalConfig.Instance.AddOrUpdateModule(platePosition, moduleData);
                    }
                    else if (moduleItem.Contains("gripper"))//抓手
                    {
                        AppGlobalConfig.Instance.IsGripperEnabled = true;
                        var gripperModule = new ModuleDatas
                        {
                            Name = "gripper_1",
                            Type = 3,
                            PlatePosition = "15",
                            PipetteVolume = 0,
                        };
                        AppGlobalConfig.Instance.AddOrUpdateModule("15", gripperModule);
                    }
                    else if (moduleItem.Contains("pipette"))//移液器
                    {
                        string[] pipetteParams = moduleItem.Split('|');
                        string pipetteName = pipetteParams[0]; // pipette_1/pipette_2
                        int pipetteType = int.Parse(pipetteParams[1]); // 0：单通道移液器；1：八通道移液器；2：96通道移液器
                        if (pipetteName == "pipette_1")
                            isLeftSigna = pipetteType;
                        else if (pipetteName == "pipette_2")
                            isRightSigna = pipetteType;
                        int pipetteVolume = int.Parse(pipetteParams[2]);//200/1000
                        var pipetteModule = new ModuleDatas
                        {
                            Name = pipetteName,
                            Type = pipetteType,
                            PlatePosition = platePosition,
                            PipetteVolume = pipetteVolume,
                            ModuleImage = ""
                        };
                        AppGlobalConfig.Instance.AddOrUpdateModule(platePosition, pipetteModule);
                    }
                }
                UpdateDeviceModule();
                var plateModuleMap = AppGlobalConfig.Instance.PlateModuleMap;

                foreach (var (plateId, moduleDatas) in plateModuleMap)
                {
                    Border targetBorder = FindPlateBorderByPlateId(plateId);

                    UpdatePlateDisplay(targetBorder, moduleDatas);
                }
                #endregion
                #region //耗材列表
                // 步骤1：先提取Python中的plate_consumables列表（板位索引→耗材名称）
                List<string> plateConsumables = new List<string>();
                Match plateConsumablesMatch = Regex.Match(
                    scriptContent,
                    @"plate_consumables\s*=\s*\[(.*?)\]",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase
                );
                if (plateConsumablesMatch.Success)
                {
                    string consumableItemsStr = plateConsumablesMatch.Groups[1].Value;
                    // 分割并清理每个耗材名称（去除引号、空格，兼容写入时的字符串格式）
                    plateConsumables = consumableItemsStr.Split(',')
                        .Select(item => item.Trim().Trim('"', '\'')) // 兼容双引号/单引号，与写入格式一致
                        .ToList();
                }

                // 步骤2：提取Python中所有耗材对象的参数，建立「耗材名称→参数字典」的映射（完全对齐:F2格式化的写入格式）
                Dictionary<string, Dictionary<string, object>> consumableParamMap = new Dictionary<string, Dictionary<string, object>>();

                // 2.1 匹配所有耗材初始化语句（如：cons_1_0mLDeep_hole_plate = consumables()）
                MatchCollection consumableVarMatches = Regex.Matches(
                    scriptContent,
                    @"(\w+)\s*=\s*consumables\(\)",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase
                );

                foreach (Match varMatch in consumableVarMatches)
                {
                    string consumableVarName = varMatch.Groups[1].Value; // Python中的耗材变量名（如cons_1_0mLDeep_hole_plate）
                    Dictionary<string, object> paramDict = new Dictionary<string, object>(); // 存储当前耗材的所有参数

                    // 2.2 优化正则匹配规则：精准捕获保留2位小数的属性赋值（解决换行/空格导致的匹配遗漏）
                    // 匹配格式：变量名.属性名 = 值（支持换行、空格、2位小数浮点值）
                    string paramPattern = $@"{Regex.Escape(consumableVarName)}\.(\w+)\s*=\s*(.*?)(?=\r?\n|{Regex.Escape("\n")}|$)";
                    MatchCollection paramMatches = Regex.Matches(
                        scriptContent,
                        paramPattern,
                        RegexOptions.Multiline | RegexOptions.Singleline
                    );

                    foreach (Match paramMatch in paramMatches)
                    {
                        string propName = paramMatch.Groups[1].Value; // 属性名（length/width/well_depth等，与写入一致）
                        string propValueStr = paramMatch.Groups[2].Value.Trim(); // 属性值字符串（支持140.00、127.76等2位小数格式）

                        // 2.3 解析属性值（兼容int/float(2位小数)/string/元组类型，完全对齐写入格式）
                        object propValue = null;
                        if (propValueStr.StartsWith("(") && propValueStr.EndsWith(")"))
                        {
                            // 解析元组类型（如offset=(14.20, 12.50)、cath_offset=(63.00, 42.00, 14.00)，兼容2位小数）
                            string tupleContent = propValueStr.Trim('(', ')');
                            List<float> tupleValues = tupleContent.Split(',')
                                .Select(item =>
                                {
                                    // 精准解析元组内的2位小数浮点值，失败给0.0f
                                    float.TryParse(item.Trim(), out float val);
                                    return val;
                                })
                                .ToList();
                            propValue = tupleValues;
                        }
                        else if (int.TryParse(propValueStr, out int intVal))
                        {
                            // 解析整数类型（id/type/numRows等，与写入一致）
                            propValue = intVal;
                        }
                        else if (float.TryParse(propValueStr, out float floatVal))
                        {
                            // 解析浮点类型（支持140.00、127.76等2位小数格式，float.TryParse可直接识别）
                            propValue = floatVal;
                        }
                        else if ((propValueStr.StartsWith("\"") && propValueStr.EndsWith("\"")) || (propValueStr.StartsWith("'") && propValueStr.EndsWith("'")))
                        {
                            // 解析字符串类型（name/description等，去除引号，与写入格式一致）
                            propValue = propValueStr.Trim('"', '\'');
                        }
                        else
                        {
                            // 默认按字符串存储，兼容未知属性
                            propValue = propValueStr;
                        }

                        // 存入当前耗材的参数字典（去重，避免重复赋值覆盖）
                        if (!paramDict.ContainsKey(propName))
                        {
                            paramDict.Add(propName, propValue);
                        }
                    }

                    // 2.4 以耗材的name属性为key，建立全局参数映射（匹配plate_consumables中的名称，与写入一致）
                    if (paramDict.ContainsKey("name") && paramDict["name"] is string consumableName && !string.IsNullOrEmpty(consumableName))
                    {
                        if (consumableParamMap.ContainsKey(consumableName))
                        {
                            consumableParamMap[consumableName] = paramDict; // 覆盖重复名称的耗材参数
                        }
                        else
                        {
                            consumableParamMap.Add(consumableName, paramDict);
                        }
                    }
                }

                // 步骤3：遍历plateConsumables，匹配参数并构造ConsumableItem，存入_plateConsumableMap+更新界面
                for (int plateIndex = 0; plateIndex < 11; plateIndex++) // 对应12个板位
                {
                    string plateId = (plateIndex + 1).ToString(); // 板位ID：1→P1，12→P12
                    if (plateIndex >= plateConsumables.Count) break; // 若plateConsumables不足12个，终止循环

                    string targetConsumableName = plateConsumables[plateIndex]; // 获取当前板位对应的耗材名称
                    if (string.IsNullOrEmpty(targetConsumableName)) continue; // 空名称跳过

                    // 匹配当前耗材名称对应的参数（精准匹配写入时的耗材名称）
                    if (!consumableParamMap.TryGetValue(targetConsumableName, out Dictionary<string, object> targetParamDict))
                    {
                        System.Diagnostics.Debug.WriteLine($"未找到耗材「{targetConsumableName}」对应的参数，板位{plateId}跳过");
                        continue;
                    }

                    // 3.1 构造ConsumableSettings（完全对齐写入时的属性映射，兼容2位小数浮点值）
                    ConsSettings consumableSettings = new ConsSettings();
                    // 基础属性赋值（一一对应写入逻辑）
                    consumableSettings.name = targetParamDict.ContainsKey("name") ? targetParamDict["name"] as string : targetConsumableName;
                    consumableSettings.id = targetParamDict.ContainsKey("id") ? (targetParamDict["id"] is int idVal ? idVal : 0) : 0;
                    consumableSettings.type = targetParamDict.ContainsKey("type") ? (targetParamDict["type"] is int typeVal ? typeVal : 0) : 0;
                    consumableSettings.description = targetParamDict.ContainsKey("description") ? targetParamDict["description"] as string : string.Empty;

                    // 尺寸属性（length→labL、width→labW、height→labH、well_depth→consDep，兼容2位小数浮点值）
                    consumableSettings.labL = targetParamDict.ContainsKey("length") ? (targetParamDict["length"] is float labLVal ? labLVal : 0.0f) : 0.0f;
                    consumableSettings.labW = targetParamDict.ContainsKey("width") ? (targetParamDict["width"] is float labWVal ? labWVal : 0.0f) : 0.0f;
                    consumableSettings.labH = targetParamDict.ContainsKey("height") ? (targetParamDict["height"] is float labHVal ? labHVal : 0.0f) : 0.0f;
                    Debug.WriteLine("21");
                    consumableSettings.consDep = targetParamDict.ContainsKey("well_depth") ? (targetParamDict["well_depth"] is float consDepVal ? consDepVal : 0.0f) : 0.0f;

                    // 偏移和间距属性（元组解析，兼容2位小数浮点值，优化冗余代码）
                    List<float> offsetList = targetParamDict.ContainsKey("offset") && targetParamDict["offset"] is List<float> ? (List<float>)targetParamDict["offset"] : new List<float>();
                    consumableSettings.offsetX = offsetList.Count >= 2 ? offsetList[0] : 0.0f;
                    consumableSettings.offsetY = offsetList.Count >= 2 ? offsetList[1] : 0.0f;

                    List<float> pitchList = targetParamDict.ContainsKey("pitch") && targetParamDict["pitch"] is List<float> ? (List<float>)targetParamDict["pitch"] : new List<float>();
                    consumableSettings.distanceRow = pitchList.Count >= 2 ? pitchList[1] : 0.0f;
                    consumableSettings.distanceColumn = pitchList.Count >= 2 ? pitchList[0] : 0.0f;

                    List<float> cathList = targetParamDict.ContainsKey("cath_offset") && targetParamDict["cath_offset"] is List<float> ? (List<float>)targetParamDict["cath_offset"] : new List<float>();
                    consumableSettings.RobotX = cathList.Count >= 3 ? cathList[0] : 0.0f;
                    consumableSettings.RobotY = cathList.Count >= 3 ? cathList[1] : 0.0f;
                    consumableSettings.RobotZ = cathList.Count >= 3 ? cathList[2] : 0.0f;

                    // 方位和行列属性（int类型，与写入一致）
                    consumableSettings.NW = targetParamDict.ContainsKey("NW") ? (targetParamDict["NW"] is int nwVal ? nwVal : 0) : 0;
                    consumableSettings.SW = targetParamDict.ContainsKey("SW") ? (targetParamDict["SW"] is int swVal ? swVal : 0) : 0;
                    consumableSettings.NE = targetParamDict.ContainsKey("NE") ? (targetParamDict["NE"] is int neVal ? neVal : 0) : 0;
                    consumableSettings.SE = targetParamDict.ContainsKey("SE") ? (targetParamDict["SE"] is int seVal ? seVal : 0) : 0;
                    consumableSettings.numRows = targetParamDict.ContainsKey("numRows") ? (targetParamDict["numRows"] is int numRowsVal ? numRowsVal : 0) : 0;
                    consumableSettings.numColumns = targetParamDict.ContainsKey("numColumns") ? (targetParamDict["numColumns"] is int numColumnsVal ? numColumnsVal : 0) : 0;

                    // 剩余绘图/功能参数（一一对应写入逻辑，兼容2位小数浮点值）
                    consumableSettings.distanceRowY = targetParamDict.ContainsKey("distanceRowY") ? (targetParamDict["distanceRowY"] is float distanceRowYVal ? distanceRowYVal : 0.0f) : 0.0f;
                    consumableSettings.distanceColumnX = targetParamDict.ContainsKey("distanceColumnX") ? (targetParamDict["distanceColumnX"] is float distanceColumnXVal ? distanceColumnXVal : 0.0f) : 0.0f;
                    consumableSettings.labVolume = targetParamDict.ContainsKey("labVolume") ? (targetParamDict["labVolume"] is float labVolumeVal ? labVolumeVal : 0.0f) : 0.0f;
                    consumableSettings.consMaxAvaiVol = targetParamDict.ContainsKey("consMaxAvaiVol") ? (targetParamDict["consMaxAvaiVol"] is float consMaxAvaiVolVal ? consMaxAvaiVolVal : 0.0f) : 0.0f;
                    consumableSettings.topShape = targetParamDict.ContainsKey("topShape") ? (targetParamDict["topShape"] is int topShapeVal ? topShapeVal : 0) : 0;
                    consumableSettings.topRadius = targetParamDict.ContainsKey("topRadius") ? (targetParamDict["topRadius"] is float topRadiusVal ? topRadiusVal : 0.0f) : 0.0f;
                    consumableSettings.topUpperX = targetParamDict.ContainsKey("topUpperX") ? (targetParamDict["topUpperX"] is float topUpperXVal ? topUpperXVal : 0.0f) : 0.0f;
                    consumableSettings.topUpperY = targetParamDict.ContainsKey("topUpperY") ? (targetParamDict["topUpperY"] is float topUpperYVal ? topUpperYVal : 0.0f) : 0.0f;
                    consumableSettings.TIPMAXCapacity = targetParamDict.ContainsKey("TIPMAXCapacity") ? (targetParamDict["TIPMAXCapacity"] is float tipMaxCapVal ? tipMaxCapVal : 0.0f) : 0.0f;
                    consumableSettings.TIPMAXAvailable = targetParamDict.ContainsKey("TIPMAXAvailable") ? (targetParamDict["TIPMAXAvailable"] is float tipMaxAvaVal ? tipMaxAvaVal : 0.0f) : 0.0f;
                    consumableSettings.TIPTotalLength = targetParamDict.ContainsKey("TIPTotalLength") ? (targetParamDict["TIPTotalLength"] is float tipTotalLenVal ? tipTotalLenVal : 0.0f) : 0.0f;
                    consumableSettings.TIPHeadHeight = targetParamDict.ContainsKey("TIPHeadHeight") ? (targetParamDict["TIPHeadHeight"] is float tipHeadHVal ? tipHeadHVal : 0.0f) : 0.0f;
                    consumableSettings.TIPConeLength = targetParamDict.ContainsKey("tip_height") ? (targetParamDict["tip_height"] is float tipConeLenVal ? tipConeLenVal : 0.0f) : 0.0f;
                    consumableSettings.TIPMAXRadius = targetParamDict.ContainsKey("TIPMAXRadius") ? (targetParamDict["TIPMAXRadius"] is float tipMaxRadVal ? tipMaxRadVal : 0.0f) : 0.0f;
                    consumableSettings.TIPMINRadius = targetParamDict.ContainsKey("TIPMINRadius") ? (targetParamDict["TIPMINRadius"] is float tipMinRadVal ? tipMinRadVal : 0.0f) : 0.0f;
                    consumableSettings.TIPDepthOFComp = targetParamDict.ContainsKey("tip_take_depth") ? (targetParamDict["tip_take_depth"] is float tipDepthCompVal ? tipDepthCompVal : 0.0f) : 0.0f;

                    // 3.2 构造ConsumableItem（与写入逻辑一致）
                    ConsumableItem consumableItem = new ConsumableItem();
                    consumableItem.Name = targetConsumableName;
                    consumableItem.Settings = consumableSettings;

                    // 3.3 存入_plateConsumableMap并更新界面（UI操作需在Dispatcher中执行，逻辑不变）
                    Dispatcher.Invoke(() =>
                    {
                        // 清空当前板位原有耗材
                        ClearPlateContent(plateId);

                        // 查找板位Grid和Border
                        Grid plateGrid = this.FindName($"PlateGrid{plateId}") as Grid;
                        if (plateGrid == null) return;
                        Border plateBorder = FindParentBorder(plateGrid);
                        if (plateBorder == null) return;

                        // 隐藏底部TextBlock（与拖拽/恢复逻辑一致）
                        var bottomTextBlock = plateGrid.Children.Cast<FrameworkElement>()
                            .OfType<TextBlock>()
                            .FirstOrDefault(t => t.Tag?.ToString() == "BottomLayer");
                        if (bottomTextBlock != null)
                            bottomTextBlock.Visibility = Visibility.Collapsed;

                        // 创建并添加ConsumableCanvas（显示耗材，兼容2位小数参数）
                        var canvas = new ConsumableCanvas
                        {
                            Tag = "TopConsumable",
                            ConsData = consumableSettings,
                            Height = 300,
                            Width = 300,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Brushes.Transparent,
                            PlateId = plateId
                        };
                        canvas.SelectedColumnsChanged += OnPlateColumnsSelected;
                        plateGrid.Children.Add(canvas);

                        // 设置板位ToolTip（与拖拽逻辑一致）
                        //string boardTipPrefix = ResourceHelper.Instance.WindowBoardToopTip;
                        //plateBorder.ToolTip = $"{boardTipPrefix}{plateId}：{targetConsumableName}";

                        string moduleName = string.Empty;
                        var nameLayer = plateGrid.Children.Cast<FrameworkElement>()
                             .FirstOrDefault(child => child.Tag?.ToString() == "NameLayer");

                        //// 2. 若NameLayer存在，且里面包含模块名称的TextBlock → 判定有模块
                        if (nameLayer is StackPanel nameStack)
                        {
                            var moduleNameText = nameStack.Children.Cast<TextBlock>().FirstOrDefault();
                            if (moduleNameText != null && !string.IsNullOrEmpty(moduleNameText.Text))
                            {
                                moduleName = moduleNameText.Text;
                            }
                            plateGrid.Children.Remove(nameLayer);
                        }

                        var nameStack2 = new StackPanel
                        {
                            Tag = "NameLayer",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                        };
                        var primaryBrush = (SolidColorBrush)FindResource("PrimaryColor");

                        nameStack2.Children.Add(new TextBlock
                        {
                            Text = moduleName,
                            FontSize = 20,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = primaryBrush
                        });
                        nameStack2.Children.Add(new TextBlock
                        {
                            Text = targetConsumableName,
                            FontSize = 20,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = primaryBrush
                        });
                        plateGrid.Children.Add(nameStack2);


                        // 存入/更新_plateConsumableMap（兼容板位重复赋值）
                        if (_plateConsumableMap.ContainsKey(plateId))
                        {
                            _plateConsumableMap[plateId] = consumableItem;
                        }
                        else
                        {
                            _plateConsumableMap.Add(plateId, consumableItem);
                        }
                    });
                }
                #endregion
                #region 流程
                // 添加开始步骤
                FlowSteps.Add(new FlowStep
                {
                    Index = 1,
                    //Name = "开始",
                    Type = "start",
                    IsSelected = false,
                    IsSystemStep = true
                });
                string[] allPythonLines = scriptContent.Split(
    new[] { Environment.NewLine, "\n", "\r" },
    StringSplitOptions.RemoveEmptyEntries);
                bool isEnterRunFunction = false;
                List<string> runFunctionLines = new List<string>();
                foreach (string singleLine in allPythonLines)
                {
                    string trimLine = singleLine.Trim();
                    string originalLine = singleLine;

                    if (trimLine.StartsWith("def run():") && !isEnterRunFunction)
                    {
                        isEnterRunFunction = true;
                        continue;
                    }

                    if (isEnterRunFunction && !originalLine.StartsWith("    ") && !string.IsNullOrWhiteSpace(trimLine))
                    {
                        isEnterRunFunction = false;
                        break;
                    }

                    if (isEnterRunFunction && originalLine.StartsWith("    ") && !string.IsNullOrWhiteSpace(trimLine))
                    {

                        string lineWithoutIndent = originalLine.TrimStart(' ', '\t');
                        runFunctionLines.Add(lineWithoutIndent); // 存入无缩进的行，便于解析
                                                                 // 若需要保留原始缩进，直接存入 originalLine 即可
                                                                 // runFunctionLines.Add(originalLine);
                    }
                }
                string shakerTemp = "";
                foreach (string targetLine in runFunctionLines)
                {
                    if (targetLine.Contains("(") && targetLine.Contains(")"))
                    {
                        string pattern = @"^([\w\.]+)\((.*)\)";
                        Match funcMatch = Regex.Match(targetLine, pattern);
                        if (funcMatch.Success)
                        {
                            var flowStep = new FlowStep
                            {
                                Index = _stepIndex++,
                                IsSelected = false,
                                IsSystemStep = false
                            };
                            string funcName = funcMatch.Groups[1].Value;
                            string paramContent = funcMatch.Groups[2].Value;
                            Debug.WriteLine($"    → 函数名：{funcName}，括号内参数：{paramContent}");

                            switch (funcName)
                            {
                                case "tipon":
                                    string tiponID = ExtractSingleParamValue(paramContent, "pipe_id");
                                    string tiponPlate = ExtractSingleParamValue(paramContent, "plate");
                                    string tiponRow = ExtractSingleParamValue(paramContent, "row");
                                    string tiponCol = ExtractSingleParamValue(paramContent, "col");
                                    flowStep.Position = MapPlatePositionBack(tiponPlate);
                                    flowStep.Type = "TipOn";
                                    if (tiponID == "1")
                                    {
                                        flowStep.SelectedPipetteName = "pipette_1";
                                        flowStep.SelectedCells = ReverseParsePipettePosition(int.Parse(tiponRow), int.Parse(tiponCol), isLeftSigna);
                                        flowStep.WellPosition = ReverseParsePipetteWellPosition(int.Parse(tiponRow), int.Parse(tiponCol), isLeftSigna);
                                    }
                                    else if (tiponID == "2")
                                    {
                                        flowStep.SelectedPipetteName = "pipette_2";
                                        flowStep.SelectedCells = ReverseParsePipettePosition(int.Parse(tiponRow), int.Parse(tiponCol), isRightSigna);
                                        flowStep.WellPosition = ReverseParsePipetteWellPosition(int.Parse(tiponRow), int.Parse(tiponCol), isRightSigna);
                                    }
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "aspirate":
                                    string aspID = ExtractSingleParamValue(paramContent, "pipe_id");
                                    string aspPlate = ExtractSingleParamValue(paramContent, "plate");
                                    string aspRow = ExtractSingleParamValue(paramContent, "row");
                                    string aspCol = ExtractSingleParamValue(paramContent, "col");
                                    string aspVol = ExtractSingleParamValue(paramContent, "vol");
                                    string aspSpeed = ExtractSingleParamValue(paramContent, "speed");
                                    string aspDepth = ExtractSingleParamValue(paramContent, "depth");
                                    float aspDepthValue = ExtractFloatValueFromBracket(aspDepth);

                                    flowStep.Position = MapPlatePositionBack(aspPlate);
                                    flowStep.Type = "Aspirate";
                                    flowStep.Volume = float.Parse(aspVol);
                                    flowStep.LiquidAisSpeed = float.Parse(aspSpeed);
                                    flowStep.LiquidAisDistance = aspDepthValue;
                                    if (aspID == "1")
                                    {
                                        flowStep.SelectedPipetteName = "pipette_1";
                                        flowStep.SelectedCells = ReverseParsePipettePosition(int.Parse(aspRow), int.Parse(aspCol), isLeftSigna);
                                        flowStep.WellPosition = ReverseParsePipetteWellPosition(int.Parse(aspRow), int.Parse(aspCol), isLeftSigna);
                                    }
                                    else if (aspID == "2")
                                    {
                                        flowStep.SelectedPipetteName = "pipette_2";
                                        flowStep.SelectedCells = ReverseParsePipettePosition(int.Parse(aspRow), int.Parse(aspCol), isRightSigna);
                                        flowStep.WellPosition = ReverseParsePipetteWellPosition(int.Parse(aspRow), int.Parse(aspCol), isRightSigna);
                                    }
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "dispense":
                                    string disID = ExtractSingleParamValue(paramContent, "pipe_id");
                                    string disPlate = ExtractSingleParamValue(paramContent, "plate");
                                    string disRow = ExtractSingleParamValue(paramContent, "row");
                                    string disCol = ExtractSingleParamValue(paramContent, "col");
                                    string disVol = ExtractSingleParamValue(paramContent, "vol");
                                    string disSpeed = ExtractSingleParamValue(paramContent, "speed");
                                    string disDepth = ExtractSingleParamValue(paramContent, "depth");
                                    float disDepthValue = ExtractFloatValueFromBracket(disDepth);

                                    flowStep.Position = MapPlatePositionBack(disPlate);
                                    flowStep.Type = "Dispense";
                                    flowStep.Volume = float.Parse(disVol);
                                    flowStep.LiquidDisSpeed = float.Parse(disSpeed);
                                    flowStep.LiquidDisDistance = disDepthValue;
                                    if (disID == "1")
                                    {
                                        flowStep.SelectedPipetteName = "pipette_1";
                                        flowStep.SelectedCells = ReverseParsePipettePosition(int.Parse(disRow), int.Parse(disCol), isLeftSigna);
                                        flowStep.WellPosition = ReverseParsePipetteWellPosition(int.Parse(disRow), int.Parse(disCol), isLeftSigna);
                                    }
                                    else if (disID == "2")
                                    {
                                        flowStep.SelectedPipetteName = "pipette_2";
                                        flowStep.SelectedCells = ReverseParsePipettePosition(int.Parse(disRow), int.Parse(disCol), isRightSigna);
                                        flowStep.WellPosition = ReverseParsePipetteWellPosition(int.Parse(disRow), int.Parse(disCol), isRightSigna);
                                    }
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "tipoff":
                                    string tipoffID = ExtractSingleParamValue(paramContent, "pipe_id");
                                    string tipoffPlate = ExtractSingleParamValue(paramContent, "plate");
                                    string tipoffRow = ExtractSingleParamValue(paramContent, "row");
                                    string tipoffCol = ExtractSingleParamValue(paramContent, "col");
                                    flowStep.Position = MapPlatePositionBack(tipoffPlate);
                                    flowStep.Type = "TipOff";
                                    if (tipoffID == "1")
                                    {
                                        flowStep.SelectedPipetteName = "pipette_1";
                                        flowStep.SelectedCells = ReverseParsePipettePosition(int.Parse(tipoffRow), int.Parse(tipoffCol), isLeftSigna);
                                        flowStep.WellPosition = ReverseParsePipetteWellPosition(int.Parse(tipoffRow), int.Parse(tipoffCol), isLeftSigna);
                                    }
                                    else if (tipoffID == "2")
                                    {
                                        flowStep.SelectedPipetteName = "pipette_2";
                                        flowStep.SelectedCells = ReverseParsePipettePosition(int.Parse(tipoffRow), int.Parse(tipoffCol), isRightSigna);
                                        flowStep.WellPosition = ReverseParsePipetteWellPosition(int.Parse(tipoffRow), int.Parse(tipoffCol), isRightSigna);
                                    }
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "wait":
                                    string waitTime = ExtractSingleParamValue(paramContent, "s");
                                    //string waitValue = ExtractSingleParamValue(paramContent, "value");
                                    flowStep.Type = "Wait";
                                    flowStep.WaitTime = int.Parse(waitTime);
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "mixing":
                                    string mixID = ExtractSingleParamValue(paramContent, "pipe_id");
                                    string mixPlate = ExtractSingleParamValue(paramContent, "plate");
                                    string mixRow = ExtractSingleParamValue(paramContent, "row");
                                    string mixCol = ExtractSingleParamValue(paramContent, "col");
                                    string mixVol = ExtractSingleParamValue(paramContent, "vol");
                                    string mixRound = ExtractSingleParamValue(paramContent, "rounds");
                                    string mixSpeed = ExtractSingleParamValue(paramContent, "speed");
                                    string mixDepth = ExtractSingleParamValue(paramContent, "depth");
                                    float mixDepthValue = ExtractFloatValueFromBracket(mixDepth);

                                    flowStep.Position = MapPlatePositionBack(mixPlate);
                                    flowStep.Type = "Mix";
                                    flowStep.MixVolume = float.Parse(mixVol);
                                    flowStep.MixCount = int.Parse(mixRound);
                                    flowStep.LiquidAisSpeed = float.Parse(mixSpeed);
                                    flowStep.LiquidDisSpeed = float.Parse(mixSpeed);
                                    flowStep.LiquidAisDistance = mixDepthValue;
                                    flowStep.LiquidDisDistance = mixDepthValue;

                                    if (mixID == "1")
                                    {
                                        flowStep.SelectedPipetteName = "pipette_1";
                                        flowStep.SelectedCells = ReverseParsePipettePosition(int.Parse(mixRow), int.Parse(mixCol), isLeftSigna);
                                        flowStep.WellPosition = ReverseParsePipetteWellPosition(int.Parse(mixRow), int.Parse(mixCol), isLeftSigna);
                                    }
                                    else if (mixID == "2")
                                    {
                                        flowStep.SelectedPipetteName = "pipette_2";
                                        flowStep.SelectedCells = ReverseParsePipettePosition(int.Parse(mixRow), int.Parse(mixCol), isRightSigna);
                                        flowStep.WellPosition = ReverseParsePipetteWellPosition(int.Parse(mixRow), int.Parse(mixCol), isRightSigna);
                                    }
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "movePlate":
                                    string transferID = ExtractSingleParamValue(paramContent, "id");
                                    string transferFromPlate = ExtractSingleParamValue(paramContent, "from_plate");
                                    string transferToPlate = ExtractSingleParamValue(paramContent, "to_plate");
                                    string transferPosition = ExtractSingleParamValue(paramContent, "pushing");
                                    flowStep.ModuleName = "gripper_" + transferID;
                                    flowStep.Type = "Transfer";
                                    flowStep.FromPos = transferFromPlate.ToUpper();
                                    flowStep.ToPos = transferToPlate.ToUpper();
                                    if (transferPosition == "")
                                        flowStep.TransferPosition = 0;
                                    else
                                        flowStep.TransferPosition = float.Parse(transferPosition);

                                    FlowSteps.Add(flowStep);
                                    break;
                                case "Shaker.start_temp":
                                    shakerTemp = ExtractSingleParamValue(paramContent, "temp");
                                    break;
                                case "Shaker.start_shaker":
                                    string shakerID = ExtractSingleParamValue(paramContent, "id");
                                    string shakerRPM = ExtractSingleParamValue(paramContent, "rpm");
                                    string shakerTime = ExtractSingleParamValue(paramContent, "time");

                                    flowStep.ModuleName = "shaker_" + shakerID;
                                    int targetShakerIndex = plateModules.IndexOf(flowStep.ModuleName);


                                    string finalResult = $"P{targetShakerIndex + 1}";
                                    flowStep.Type = "Shake";
                                    flowStep.Position = finalResult;
                                    flowStep.ShakeTemp = float.Parse(shakerTemp);
                                    flowStep.ShakeRPM = int.Parse(shakerRPM);
                                    flowStep.WaitTime = int.Parse(shakerTime);
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "Magnetic.on":
                                    string magneticENID = ExtractSingleParamValue(paramContent, "id");
                                    flowStep.ModuleName = "magnetic_" + magneticENID;
                                    int targetMagneticonIndex = plateModules.IndexOf(flowStep.ModuleName);

                                    string finalmagneticENResult = $"P{targetMagneticonIndex + 1}";
                                    flowStep.Type = "Magnetic";
                                    flowStep.Position = finalmagneticENResult;
                                    flowStep.IsMagnetUp = true;
                                    flowStep.IsMagnetDown = false;
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "Magnetic.off"://magnetic
                                    string magneticDisID = ExtractSingleParamValue(paramContent, "id");
                                    flowStep.ModuleName = "magnetic_" + magneticDisID;
                                    int targetMagneticoffIndex = plateModules.IndexOf(flowStep.ModuleName);

                                    string finalmagneticDisResult = $"P{targetMagneticoffIndex + 1}";
                                    flowStep.Type = "Magnetic";
                                    flowStep.Position = finalmagneticDisResult;
                                    flowStep.IsMagnetUp = false;
                                    flowStep.IsMagnetDown = true;
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "Cool.start":
                                    string coolID = ExtractSingleParamValue(paramContent, "id");
                                    flowStep.ModuleName = "tempctrl_" + coolID;
                                    int targetIndex = plateModules.IndexOf(flowStep.ModuleName);

                                    string finalcoolResult = $"P{targetIndex + 1}";
                                    string coolTemp = ExtractSingleParamValue(paramContent, "temp");

                                    flowStep.Type = "Temp Ctrl";
                                    flowStep.TempCtrlTemp = float.Parse(coolTemp);
                                    flowStep.Position = finalcoolResult;
                                    flowStep.IsTempCtrlOpen = true;
                                    flowStep.IsTempCtrlClose = false;
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "Cool.stop":
                                    string coolStopID = ExtractSingleParamValue(paramContent, "id");
                                    flowStep.ModuleName = "tempctrl_" + coolStopID;
                                    int targetCoolStopIndex = plateModules.IndexOf(flowStep.ModuleName);
                                    string finalcoolStopResult = $"P{targetCoolStopIndex + 1}";

                                    flowStep.Type = "Temp Ctrl";
                                    flowStep.Position = finalcoolStopResult;
                                    flowStep.IsTempCtrlOpen = false;
                                    flowStep.IsTempCtrlClose = true;
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "Pcr.opendoor":
                                    flowStep.PcrStep = _res.SettingManualPCROpen;
                                    flowStep.Type = "PCR";
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "Pcr.closedoor":
                                    flowStep.PcrStep = _res.SettingManualPCRClose;
                                    flowStep.Type = "PCR";
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "Pcr.stop":
                                    flowStep.PcrStep = _res.SettingManualPCRStop;
                                    flowStep.Type = "PCR";
                                    FlowSteps.Add(flowStep);
                                    break;
                                case "Pcr.run":
                                    string PcrrunValue = ExtractSingleParamValue(paramContent, "data");
                                    flowStep.PcrStep = _res.SettingManualPCRStart;
                                    flowStep.PcrScriptAdress = PcrrunValue;
                                    flowStep.Type = "PCR";
                                    FlowSteps.Add(flowStep);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                }
                // 添加结束步骤
                FlowSteps.Add(new FlowStep
                {
                    Index = _stepIndex++,
                    Type = "end",
                    IsSelected = false,
                    IsSystemStep = true
                });

                // 重新编号步骤
                RebuildStepIndexes();
                #endregion
                ShowNotification(_res.ScriptLoadSucc, NotificationControl.NotificationType.Info);
            }
            catch (Exception ex)
            {
                ShowNotification($"{_res.ScriptLoadFail}: {ex.Message}", NotificationControl.NotificationType.Error);
            }
        }
        /// <summary>
        /// 反向还原移液器孔位字符串（ParsePipettePosition的逆操作）
        /// 支持两种格式还原：
        /// 1.  单孔位：根据row、col → "行,列"（如row=2,col=5 → "2,5"）
        /// 2.  8通道：根据首行firstRow、公共列commonCol → 8组孔位拼接字符串（如firstRow=1,commonCol=4 → "1,4;2,4;3,4;4,4;5,4;6,4;7,4;8,4"）
        /// 3. 96通道：识别约定值(1,1) → 生成覆盖整板的96组孔位字符串（行1~8、列1~12）
        /// </summary>
        /// <param name="row">单孔位行号 / 8通道首行行号/ 96通道约定值1</param>
        /// <param name="col">单孔位列号 / 8通道公共列号 / 96通道约定值1</param>
        /// <param name="channelType">通道类型：0=单通道，1=8通道，2=96通道</param>
        /// <returns>还原后的SelectedCells字符串</returns>
        /// <exception cref="ArgumentOutOfRangeException">行/列号小于1时抛出（非法孔位）</exception>
        /*
        private string ReverseParsePipettePosition(int row, int col, int is8Channel)
        {
            // 基础参数校验：行号、列号必须大于0（孔位无0行0列）
            if (row < 1 || col < 1)
            {
                throw new ArgumentOutOfRangeException($"行号（{row}）和列号（{col}）必须大于0", nameof(row));
            }

            // 场景1：单通道（0）→ 格式："行,列"
            if (is8Channel == 0)
            {
                return $"{row},{col}";
            }
            // 场景2：8通道（1）→ 格式："1,4;2,4;...;8,4"
            else if (is8Channel == 1)
            {
                List<string> channelPositionList = new List<string>();
                for (int i = 0; i < 8; i++)
                {
                    int currentRow = row + i; // 首行+偏移量0~7，生成8行
                    channelPositionList.Add($"{currentRow},{col}");
                }
                return string.Join(";", channelPositionList);
            }
            // 场景3：96通道（2）→ 按需自定义格式（以下为默认实现，可根据你的业务调整）
            else if (is8Channel == 2)
            {
                List<string> channelPositionList = new List<string>();
                // 96通道默认：生成12行×8列（可根据你的实际格式修改循环逻辑）
                // 若你需要和8通道类似的“公共列+连续行”格式，可保留以下逻辑；若有其他格式，直接调整即可
                for (int i = 0; i < 96; i++)
                {
                    int currentRow = row + i; // 首行+偏移量0~95，生成96行（或按你的96通道格式调整）
                    channelPositionList.Add($"{currentRow},{col}");
                }
                return string.Join(";", channelPositionList);
            }
            // 场景4：非法通道类型（兜底分支，避免无返回值，同时抛出明确异常）
            else
            {
                throw new ArgumentException($"非法的通道类型：{is8Channel}，仅支持0（单通道）、1（8通道）、2（96通道）", nameof(is8Channel));
            }
        }
        */

        private string ReverseParsePipettePosition(int row, int col, int channelType)
        {
            // 分支1：96通道（约定值1,1代表整板）
            if (channelType == 2)
            {
                // 校验96通道必须传入约定值(1,1)
                if (row != 1 || col != 1)
                {
                    throw new ArgumentOutOfRangeException($"96通道必须传入约定值(1,1)，当前传入({row},{col})", nameof(row));
                }

                // 生成整板96组孔位（行1~8，列1~12）
                List<string> platePositionList = new List<string>();
                for (int r = 1; r <= 8; r++) // 96孔板固定8行
                {
                    for (int c = 1; c <= 12; c++) // 96孔板固定12列
                    {
                        platePositionList.Add($"{r},{c}");
                    }
                }
                return string.Join(";", platePositionList);
            }

            // 单通道/8通道：基础参数校验（行号、列号必须大于0）
            if (row < 1 || col < 1)
            {
                throw new ArgumentOutOfRangeException($"行号（{row}）和列号（{col}）必须大于0", nameof(row));
            }

            // 分支2：单通道（0）→ 格式："行,列"
            if (channelType == 0)
            {
                return $"{row},{col}";
            }
            // 分支3：8通道（1）→ 格式："1,4;2,4;...;8,4"
            else if (channelType == 1)
            {
                List<string> channelPositionList = new List<string>();
                for (int i = 0; i < 8; i++)
                {
                    int currentRow = row + i; // 首行+偏移量0~7，生成8行
                                              // 校验8通道行号不超过96孔板最大行（8）
                    if (currentRow > 8)
                    {
                        throw new ArgumentOutOfRangeException($"8通道首行({row})+偏移后行号({currentRow})超过96孔板最大行（8）", nameof(row));
                    }
                    channelPositionList.Add($"{currentRow},{col}");
                }
                return string.Join(";", channelPositionList);
            }
            // 分支4：非法通道类型
            else
            {
                throw new ArgumentException($"非法的通道类型：{channelType}，仅支持0（单通道）、1（8通道）、2（96通道）", nameof(channelType));
            }
        }


        /// <summary>
        /// 反向还原移液器孔位可读字符串（适配界面显示）
        /// 支持三种格式还原：
        /// 1. 单孔位：根据row、col → "行X 列Y"（如row=2,col=5 → "行2 列5"）
        /// 2. 8通道：根据首行firstRow、公共列commonCol → "行1~8 列Y"（如firstRow=1,commonCol=4 → "行1~8 列4"）
        /// 3. 96通道：识别约定值(1,1) → "行1~8 列1~12"（整板）
        /// </summary>
        /// <param name="row">单孔位行号 / 8通道首行行号 / 96通道约定值1</param>
        /// <param name="col">单孔位列号 / 8通道公共列号 / 96通道约定值1</param>
        /// <param name="channelType">通道类型：0=单通道，1=8通道，2=96通道</param>
        /// <returns>还原后的可读孔位字符串</returns>
        /// <exception cref="ArgumentOutOfRangeException">单通道/8通道行/列号小于1时抛出；96通道非(1,1)时抛出</exception>
        private string ReverseParsePipetteWellPosition(int row, int col, int channelType)
        {
            // 分支1：96通道（约定值1,1代表整板）
            if (channelType == 2)
            {
                // 校验96通道必须传入约定值(1,1)
                if (row != 1 || col != 1)
                {
                    throw new ArgumentOutOfRangeException($"96通道必须传入约定值(1,1)，当前传入({row},{col})", nameof(row));
                }
                // 96通道返回整板可读格式
                return $"{ResourceHelper.Instance.StepDetailRowPrefix}1~8 {ResourceHelper.Instance.StepDetailColumnPrefix}1~12";
            }

            // 单通道/8通道：基础参数校验（行号、列号必须大于0）
            if (row < 1 || col < 1)
            {
                throw new ArgumentOutOfRangeException($"行号（{row}）和列号（{col}）必须大于0", nameof(row));
            }

            // 分支2：单通道（0）→ 格式："行X 列Y"
            if (channelType == 0)
            {
                return $"{ResourceHelper.Instance.StepDetailRowPrefix}{row} {ResourceHelper.Instance.StepDetailColumnPrefix}{col}";
            }
            // 分支3：8通道（1）→ 格式："行1~8 列Y"
            else if (channelType == 1)
            {
                // 校验8通道首行合法性（确保偏移后不超过8行）
                if (row + 7 > 8)
                {
                    throw new ArgumentOutOfRangeException($"8通道首行({row})偏移后超过96孔板最大行（8）", nameof(row));
                }
                return $"{ResourceHelper.Instance.StepDetailRowPrefix}1~8 {ResourceHelper.Instance.StepDetailColumnPrefix}{col}";
            }
            // 分支4：非法通道类型
            else
            {
                throw new ArgumentException($"非法的通道类型：{channelType}，仅支持0（单通道）、1（8通道）、2（96通道）", nameof(channelType));
            }
        }
        /*
        private string ReverseParsePipetteWellPosition(int row, int col, int is8Channel)
        {
            // 基础参数校验：行号、列号必须大于0（孔位无0行0列）
            if (row < 1 || col < 1)
            {
                throw new ArgumentOutOfRangeException($"行号（{row}）和列号（{col}）必须大于0", nameof(row));
            }

            // 场景1：单通道（0）→ 格式："行,列"
            if (is8Channel == 0)
            {
                return $"{ResourceHelper.Instance.StepDetailRowPrefix}{row} {ResourceHelper.Instance.StepDetailColumnPrefix}{col}";
            }
            // 场景2：8通道（1）→ 格式："1,4;2,4;...;8,4"
            else if (is8Channel == 1)
            {
                return $"{ResourceHelper.Instance.StepDetailRowPrefix}{"1~8"} {ResourceHelper.Instance.StepDetailColumnPrefix}{col}";
            }
            // 场景3：96通道（2）→ 按需自定义格式（以下为默认实现，可根据你的业务调整）
            else if (is8Channel == 2)
            {
                List<string> channelPositionList = new List<string>();
                // 96通道默认：生成12行×8列（可根据你的实际格式修改循环逻辑）
                // 若你需要和8通道类似的“公共列+连续行”格式，可保留以下逻辑；若有其他格式，直接调整即可
                for (int i = 0; i < 96; i++)
                {
                    int currentRow = row + i; // 首行+偏移量0~95，生成96行（或按你的96通道格式调整）
                    channelPositionList.Add($"{currentRow},{col}");
                }
                return string.Join(";", channelPositionList);
            }
            // 场景4：非法通道类型（兜底分支，避免无返回值，同时抛出明确异常）
            else
            {
                throw new ArgumentException($"非法的通道类型：{is8Channel}，仅支持0（单通道）、1（8通道）、2（96通道）", nameof(is8Channel));
            }
        }
        */
        private string ExtractSingleParamValue(string paramContent, string targetParamName)
        {
            if (string.IsNullOrWhiteSpace(paramContent) || string.IsNullOrWhiteSpace(targetParamName))
            {
                return string.Empty;
            }

            // 正则匹配规则：
            // 1.  \b：单词边界，避免匹配到platex这类包含targetParamName的无效参数
            // 2.  \s*=\s*：匹配参数名和值之间的任意空格（兼容id = xxx 或 id=xxx格式）
            // 3.  ([^,)]+)：匹配值内容（直到逗号或右括号结束，兼容带引号/不带引号的值）
            // 4.  RegexOptions.IgnoreCase：忽略参数名大小写（兼容Plate/plate格式）
            string pattern = $@"\b{Regex.Escape(targetParamName)}\b\s*=\s*([^,)]+)";
            Match paramMatch = Regex.Match(paramContent, pattern, RegexOptions.IgnoreCase);

            if (paramMatch.Success)
            {
                string rawValue = paramMatch.Groups[1].Value.Trim();
                // 去除值两端的双引号/单引号（兼容plate="p7" 或 plate='p7'格式）
                rawValue = rawValue.Trim('"', '\'');
                // 去除值两端多余的空格（兼容 col=  5 这类格式）
                return rawValue.Trim();
            }

            // 未匹配到参数时返回空字符串
            return string.Empty;
        }
        /// <summary>
        /// 从带括号的字符串中提取数值（如"bottom(2.00"、"bottom(3.50)" → 2.00、3.50）
        /// </summary>
        /// <param name="bracketStr">带括号的字符串（如bottom(2.00、cons.bottom(5.60)）</param>
        /// <param name="defaultValue">解析失败时的默认值</param>
        /// <returns>提取并转换后的float数值</returns>
        private float ExtractFloatValueFromBracket(string bracketStr, float defaultValue = 0.0f)
        {
            if (string.IsNullOrWhiteSpace(bracketStr))
            {
                return defaultValue;
            }

            // 正则匹配规则：捕获括号内的所有数字（包括整数、小数，支持正负号）
            // \(：匹配左括号；([\d\.]+)：捕获数字和小数点；\)：匹配右括号（非必填，兼容缺少右括号的情况）
            string pattern = @"\(([\d\.]+)\)?";
            Match numMatch = Regex.Match(bracketStr, pattern);

            if (numMatch.Success)
            {
                string numStr = numMatch.Groups[1].Value.Trim();
                // 安全解析为float，失败返回默认值
                if (float.TryParse(numStr, out float result))
                {
                    // 可选：保留两位小数，与导出时的:F2格式一致
                    return (float)Math.Round(result, 2);
                }
            }

            // 匹配失败或解析失败，返回默认值
            return defaultValue;
        }
        private Border FindParentBorder(DependencyObject childControl)
        {
            if (childControl == null) return null;

            // 遍历视觉树，查找父级Border
            DependencyObject parent = VisualTreeHelper.GetParent(childControl);
            while (parent != null)
            {
                if (parent is Border border)
                {
                    return border; // 找到父级Border，返回
                }
                // 继续向上查找父级
                parent = VisualTreeHelper.GetParent(parent);
            }

            return null; // 未找到父级Border
        }
        /// <summary>
        /// 将JSON中的板位格式（如p1、magnetic_1）转换回UI格式（如P1）
        /// </summary>
        private string MapPlatePositionBack(string plate)
        {
            if (string.IsNullOrEmpty(plate)) return "P1";

            if (plate.StartsWith("p"))
                return $"P{plate.Substring(1)}"; // p1 → P1
            //if (plate == "magnetic_1")
            //    return "P3"; // 对应磁分离板位
            //if (plate == "shaker_1")
            //    return "P9"; // 对应震荡板位

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
                // 1. 创建脚本Python
                ShowNotification(_res.ScriptStartCreating, NotificationControl.NotificationType.Info);
                //string scriptJson = CreateScriptJson();
                string scriptPy = CreateScriptPython();
                // 2. 保存脚本到文件
                string scriptRootPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts");
                Directory.CreateDirectory(scriptRootPath);
                string projectName = AppGlobalConfig.Instance.GuideProtocolName;
                string validProjectName = string.IsNullOrWhiteSpace(projectName)
                       ? "untitled"
                       : System.Text.RegularExpressions.Regex.Replace(projectName, @"[\\/:*?""<>|]", "_");
                string fileName = $"{validProjectName}_{AppGlobalConfig.Instance.GuideProtocolStartTime}.py";
                string subFolderName = System.IO.Path.GetFileNameWithoutExtension(fileName);
                string subFolderPath = System.IO.Path.Combine(scriptRootPath, subFolderName);
                Directory.CreateDirectory(subFolderPath);
                string fullPath = System.IO.Path.Combine(subFolderPath, fileName);
                ExportGridToPngFile(PlateContainer, subFolderPath);
                File.WriteAllText(fullPath, scriptPy);

                int stepsNum = FlowSteps.Count();

                // 假设通过gRPC发送脚本
                var response = await ScriptRunAsync(scriptPy, stepsNum);
                if (response == null)
                {

                    ShowNotification(_res.WindowGrpcComFail, NotificationControl.NotificationType.Warn);
                    return;
                }
                if (response.Result == QybotrunPkg.execute_result.Types.errcode.Succeed)
                {
                    runFlag = true;
                    ShowNotification(_res.ScriptStartSucc, NotificationControl.NotificationType.Info);
                    var settingsDialog = new RunningInfoShow(this);
                    settingsDialog.RunLogs.Clear();

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
                else
                {
                    ShowNotification(response.Errinfo, NotificationControl.NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"{_res.ScriptStartCreateFail}: {ex.Message}", NotificationControl.NotificationType.Error);
            }
        }


        //补光灯
        private async void LightControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (LightFlag == 0)
            {
                ShowNotification(_res.DeviceLightOpen, NotificationControl.NotificationType.Info);

                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from qyrobot import Robot");
                pythonCode.AppendLine("Robot.filllight(1)");

                var rawLightFlag = await ScriptDebugAsync(pythonCode.ToString());//open
                var lightFlag = ParseScriptDebugResponse(rawLightFlag);
                if (lightFlag != null)
                {
                    if (lightFlag.Result == "succeed")
                    {
                        UpdateLightButtonStyle(1);
                        LightFlag = 1;
                    }
                    else
                    {
                        UpdateLightButtonStyle(0);
                        LightFlag = 0;
                    }
                }
                else
                {
                    ShowNotification(_res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
                }

            }
            else if (LightFlag == 1)
            {
                ShowNotification(_res.DeviceLightClose, NotificationControl.NotificationType.Info);

                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from qyrobot import Robot");
                pythonCode.AppendLine("Robot.filllight(0)");

                var rawLightFlag = await ScriptDebugAsync(pythonCode.ToString());//close
                var lightFlag = ParseScriptDebugResponse(rawLightFlag);
                if (lightFlag != null)
                {
                    if (lightFlag.Result == "succeed")
                    {
                        UpdateLightButtonStyle(0);
                        LightFlag = 0;
                    }
                    else
                    {
                        UpdateLightButtonStyle(1);
                        LightFlag = 1;
                    }
                }
                else
                {
                    ShowNotification(_res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
                }

            }
        }
        //UV灯
        private async void UVLightControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (UVFlag == 0)
            {
                ShowNotification(_res.DeviceUVOpen, NotificationControl.NotificationType.Info);
                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from qyrobot import Robot");
                pythonCode.AppendLine("Robot.uv(1)");

                var rawUvFlag = await ScriptDebugAsync(pythonCode.ToString());//open
                var uvFlag = ParseScriptDebugResponse(rawUvFlag);
                if (uvFlag != null)
                {
                    if (uvFlag.Result == "succeed")
                    {
                        UpdateUVButtonStyle(1);
                        UVFlag = 1;
                    }
                    else
                    {
                        UpdateUVButtonStyle(0);
                        UVFlag = 0;
                    }
                }
                else
                {
                    ShowNotification(_res.DeviceUVOpen, NotificationControl.NotificationType.Info);
                }



            }
            else if (UVFlag == 1)
            {
                ShowNotification(_res.DeviceUVClose, NotificationControl.NotificationType.Info);
                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from qyrobot import Robot");
                pythonCode.AppendLine("Robot.uv(0)");

                var rawUvFlag = await ScriptDebugAsync(pythonCode.ToString());//close
                var uvFlag = ParseScriptDebugResponse(rawUvFlag);
                if (uvFlag != null)
                {
                    if (uvFlag.Result == "succeed")
                    {
                        UpdateUVButtonStyle(0);
                        UVFlag = 0;

                    }
                    else
                    {
                        UpdateUVButtonStyle(1);
                        UVFlag = 1;
                    }
                }
                else
                {
                    ShowNotification(_res.WindowGrpcComFail, NotificationControl.NotificationType.Error);

                }

            }
        }

        //摄像头
        private void CameraControlButton_Click(object sender, RoutedEventArgs e)
        {
            var cameraWindow = new CameraShowWindow
            {
                Owner = this
            };
            cameraWindow.Show();
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
                IsSystemStep = true, // 标记为系统步骤
                Level = 0
            });
            // 添加结束步骤
            FlowSteps.Add(new FlowStep
            {
                Index = 2,
                //Name = "结束",
                Type = "end",
                IsSelected = false,
                IsSystemStep = true, // 标记为系统步骤
                Level = 0
            });
            FlowList.ItemsSource = FlowSteps;
            _stepIndex = 3;
            _stepClickIndex = 1;
        }
        //快速生成
        private void QuickGenerateButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new RunningInfoShow(this);

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
        //grpc
        //开始流程
        public async Task<execute_result> ScriptRunAsync(string scriptData, int scriptStep)
        {
            var request = new script_data
            {
                Data = scriptData,
                StartStep = scriptStep
            };

            try
            {
                var response = await _qybotrunClient.script_runAsync(
                    request);

                return response;
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
        //流程暂停
        public async Task ScriptPauseAsync()
        {
            try
            {
                var response = await _qybotrunClient.script_pauseAsync(
                    new Empty(),
                    deadline: DateTime.UtcNow.AddSeconds(10));

                Dispatcher.Invoke(() =>
                {
                    if (response.Result == 0)
                    {
                        runFlag = false;
                        pauseFlag = true;
                        ShowNotification(_res.DeviceOperationSucc,
                            NotificationControl.NotificationType.Info);
                    }
                    else
                    {
                        string errorMessage = $"{_res.DeviceOperationFailure}  ({response.Result}): ";

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
        //流程继续
        public async Task ScriptContinueAsync()
        {
            try
            {
                var response = await _qybotrunClient.script_resumeAsync(
                    new Empty(),
                    deadline: DateTime.UtcNow.AddSeconds(10));

                Dispatcher.Invoke(() =>
                {
                    if (response.Result == 0)
                    {
                        runFlag = true;
                        pauseFlag = false;
                        ShowNotification(_res.DeviceOperationSucc,
                            NotificationControl.NotificationType.Info);
                    }
                    else
                    {
                        string errorMessage = $"{_res.DeviceOperationFailure} ({response.Result}): ";

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
        //流程停止
        public async Task ScriptStopAsync()
        {
            try
            {
                var response = await _qybotrunClient.script_abortAsync(
                    new Empty(),
                    deadline: DateTime.UtcNow.AddSeconds(10));

                Dispatcher.Invoke(() =>
                {
                    if (response.Result == 0)
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
                        string errorMessage = $"{_res.DeviceOperationFailure} ({response.Result}): ";

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
        //流程调试
        public async Task<debug_data> ScriptDebugAsync(string debugData)
        {
            var request = new debug_data
            {
                Data = debugData
            };

            try
            {
                var response = await _qybotrunClient.script_debugAsync(
                    request);

                return response;
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
        //流程获取
        public async Task<runtime_info> ScriptGetInfoAsync()
        {
            try
            {
                var response = await _qybotrunClient.get_runtimeAsync(new Empty());

                return response;
            }
            catch (Grpc.Core.RpcException ex)
            {
                return default(runtime_info);
            }
            catch (Exception ex)
            {
                return default(runtime_info);
            }
        }
        /// <summary>
        /// 解析ScriptDebugAsync返回的原始数据
        /// </summary>
        /// <param name="rawDebugData">gRPC返回的原始debug_data对象</param>
        /// <returns>解析后的强类型数据，解析失败返回null</returns>
        public ScriptDebugParsedResult ParseScriptDebugResponse(debug_data rawDebugData)
        {
            // 1. 校验原始数据是否为空
            if (rawDebugData == null || string.IsNullOrWhiteSpace(rawDebugData.Data))
            {
                Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(
                        "json null",
                        NotificationControl.NotificationType.Warn);
                });
                return null;
            }

            try
            {
                // 步骤1：先将JSON字符串解析为JsonDocument（手动处理字段，避免类型不匹配）
                using var jsonDoc = JsonDocument.Parse(rawDebugData.Data); // using自动释放资源
                var rootElement = jsonDoc.RootElement;

                // 步骤2：初始化解析结果对象
                var parsedResult = new ScriptDebugParsedResult();

                // 步骤3：手动提取details字段（字符串类型，直接获取）
                if (rootElement.TryGetProperty("details", out var detailsElem))
                {
                    parsedResult.Details = detailsElem.GetString() ?? string.Empty; // 避免null，给默认空字符串
                }

                // 步骤4：手动提取result字段（字符串类型，直接获取）
                if (rootElement.TryGetProperty("result", out var resultElem))
                {
                    parsedResult.Result = resultElem.GetString() ?? string.Empty;
                }

                // 步骤5：手动提取data字段（关键！无论data是什么类型，都转为原始JSON字符串）
                if (rootElement.TryGetProperty("data", out var dataElem))
                {
                    parsedResult.Data = dataElem.GetRawText(); // 核心方法：将data的原始JSON（{} / "xxx" / []）转为字符串
                }
                if (parsedResult.Result == "err")
                {
                    ShowNotification(
    $"Error：{parsedResult.Details}",
    NotificationControl.NotificationType.Error);
                }
                // 步骤6：返回解析后的对象
                return parsedResult;
            }
            catch (JsonException ex)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    ShowNotification(
                        $"JSON Err：{ex.Message}\nJSON：{rawDebugData.Data}",
                        NotificationControl.NotificationType.Error);
                });
                return null;
            }
        }
        //中文
        private void LangSwitch_Checked(object sender, RoutedEventArgs e)
        {
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
