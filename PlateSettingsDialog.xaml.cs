using HelixToolkit.Wpf;
using MySqlConnector;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml;

namespace OctoFixFlow
{
    /// <summary>
    /// PlateSettingsDialog.xaml 的交互逻辑
    /// </summary>
    public partial class PlateSettingsDialog : UserControl
    {
        private readonly MainWidget _mainWidget;
        private DatabaseService databaseService;
        private SettingPopupControl TopSettingPopup;
        // 字段+属性，用于绑定UI
        private ConsSettings _consNew;
        public ConsSettings consNew
        {
            get => _consNew;
            set
            {
                _consNew = value;
                OnPropertyChanged(); // 实例变化时通知UI刷新
            }
        }
        // 液体相关字段与属性
        private LiquidSettings _liquidNew;
        public LiquidSettings liquidNew
        {
            get => _liquidNew;
            set
            {
                _liquidNew = value;
                OnPropertyChanged();
            }
        }
        private string oldLiquidName;

        // 实现INotifyPropertyChanged接口
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private string oldConsName;

        private int _heatingOscillCount = 0; // 加热振荡模块计数器
        private List<ModuleDatas> _heatingOscillModules = new List<ModuleDatas>(); // 加热振荡模块列表
        private int _magneticCount = 0; // 磁吸模块计数器
        private List<ModuleDatas> _magneticModules = new List<ModuleDatas>(); // 磁吸模块列表
        private int _tempCount = 0; // 温控模块计数器
        private List<ModuleDatas> _tempModules = new List<ModuleDatas>(); // 温控模块列表
        private float _currentJumpSize = 0.1f;

        //移液器3D
        private ContainerUIElement3D _pipetteContainer; // 用来容纳移液器的容器，方便刷新

        public PlateSettingsDialog(MainWidget mainWidget)
        {
            InitializeComponent();
            _mainWidget = mainWidget;
            databaseService = new DatabaseService();
            consNew = new ConsSettings();
            liquidNew = new LiquidSettings();
            liquidNew.PropertyChanged += LiquidNew_PropertyChanged;
            this.DataContext = this;
            consNew.PropertyChanged += ConsNew_PropertyChanged;
            TopSettingPopup = new SettingPopupControl(_mainWidget);
            Grid.SetRow(TopSettingPopup, 0);
            Grid.SetRowSpan(TopSettingPopup, 4);
            Panel.SetZIndex(TopSettingPopup, 100);
            TopSettingPopup.Visibility = Visibility.Collapsed;

            if (this.Content is Grid rootGrid)
            {
                rootGrid.Children.Add(TopSettingPopup);
            }
            this.Loaded += async (s, e) =>
            {
                await loadSqlData();
                InitPipette3D();

            };
            AddHeatingOscillModule(1, "P9");
            AddMagnetModule(1, "P6");
            AddTempControlModule(1, "P3");
        }
        private async Task loadSqlData()
        {
            List<string> consList = await databaseService.GetAllConsumableNamesAsync();
            foreach (string cons in consList)
            {
                ListBoxItem newItem = new ListBoxItem
                {
                    Content = cons,
                    Padding = new Thickness(10, 8, 10, 8)
                };

                consumableList.Items.Add(newItem);
            }
            List<string> liquidList = await databaseService.GetAllLiquidNamesAsync();
            foreach (string liquid in liquidList)
            {
                ListBoxItem newItem = new ListBoxItem
                {
                    Content = liquid,
                    Padding = new Thickness(10, 8, 10, 8)
                };
                liquidumableList.Items.Add(newItem); // 假设液体ListBox名称为liquidumableList
            }
        }
        //更新耗材
        private async void ConsNew_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(consNew.name))
                return;

            // 调用数据库更新方法（使用之前实现的 UpdateConsumableAsync）
            if (consNew.name != oldConsName)
            {
                bool isUpdated = await databaseService.UpdateConsumableNameAsync(oldConsName, consNew.name);
                if (!isUpdated)
                {
                    _mainWidget.ShowNotification("名称重复", NotificationControl.NotificationType.Warn);
                }
                else
                {
                    oldConsName = consNew.name;
                    if (consumableList.SelectedItem is ListBoxItem selectedItem)
                        selectedItem.Content = consNew.name;
                }
            }
            else
            {
                bool isUpdated = await databaseService.UpdateConsumableAsync(consNew);
                if (!isUpdated)
                {
                    _mainWidget.ShowNotification($"更新失败：{consNew.name} 的 {e.PropertyName} 属性", NotificationControl.NotificationType.Warn);
                }
                if (consNew.topShape == 0)
                {
                    consTopRdiusBlock.Visibility = Visibility.Visible;
                    consTopRdiusBox.Visibility = Visibility.Visible;
                    consTopWidthBlock.Visibility = Visibility.Collapsed;
                    consTopWidthBox.Visibility = Visibility.Collapsed;
                    consTopLongBlock.Visibility = Visibility.Collapsed;
                    consTopLongBox.Visibility = Visibility.Collapsed;
                }
                else if (consNew.topShape == 1)
                {
                    consTopRdiusBlock.Visibility = Visibility.Collapsed;
                    consTopRdiusBox.Visibility = Visibility.Collapsed;
                    consTopWidthBlock.Visibility = Visibility.Visible;
                    consTopWidthBox.Visibility = Visibility.Visible;
                    consTopLongBlock.Visibility = Visibility.Visible;
                    consTopLongBox.Visibility = Visibility.Visible;
                }
            }

        }
        //更新液体
        private async void LiquidNew_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(liquidNew.name))
                return;

            // 处理名称修改
            if (liquidNew.name != oldLiquidName)
            {
                bool isUpdated = await databaseService.UpdateLiquidNameAsync(oldLiquidName, liquidNew.name);
                if (!isUpdated)
                {
                    _mainWidget.ShowNotification("液体名称更新失败（可能重复）", NotificationControl.NotificationType.Warn);
                }
                else
                {
                    oldLiquidName = liquidNew.name;
                    if (liquidumableList.SelectedItem is ListBoxItem selectedItem)
                        selectedItem.Content = liquidNew.name;
                }
            }
            else
            {
                // 更新其他属性
                bool isUpdated = await databaseService.UpdateLiquidAsync(liquidNew);
                if (!isUpdated)
                {
                    _mainWidget.ShowNotification($"液体更新失败：{liquidNew.name} 的 {e.PropertyName} 属性", NotificationControl.NotificationType.Warn);
                }
            }
            RefreshPipetteModel();
        }
        //退出界面
        private void ExitSetClick(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is Window parentWindow)
            {
                _mainWidget.RefreshConsumablesAndLiquids();

                parentWindow.Close();
            }
        }
        //耗材点击
        private async void ConsumableList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 获取当前选中的项
            if (consumableList.SelectedItem is ListBoxItem selectedItem)
            {
                string itemName = selectedItem.Content.ToString();
                oldConsName = itemName;
                ConsSettings consSQL = await databaseService.GetConsumableByNameAsync(itemName);
                updateCons(consSQL);
            }
        }
        //液体点击
        private async void LiquidumableList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (liquidumableList.SelectedItem is ListBoxItem selectedItem)
            {
                string itemName = selectedItem.Content.ToString();
                oldLiquidName = itemName;
                LiquidSettings liquidSQL = await databaseService.GetLiquidByNameAsync(itemName);
                updateLiquid(liquidSQL);
            }
        }
        //更新耗材
        private void updateCons(ConsSettings consSQL)
        {
            // 1. 取消旧实例的事件订阅
            if (consNew != null)
            {
                consNew.PropertyChanged -= ConsNew_PropertyChanged;
            }

            // 2. 创建新实例，复制所有属性（必须完整，不能省略）
            var newCons = new ConsSettings
            {
                name = consSQL.name,
                id = consSQL.id,
                type = consSQL.type,
                description = consSQL.description,
                NW = consSQL.NW,
                NE = consSQL.NE,
                SW = consSQL.SW,
                SE = consSQL.SE,
                numRows = consSQL.numRows,
                numColumns = consSQL.numColumns,
                labL = consSQL.labL,
                labW = consSQL.labW,
                labH = consSQL.labH,
                distanceRowY = consSQL.distanceRowY, // 关键属性：孔位置计算
                distanceColumnX = consSQL.distanceColumnX, // 关键属性：孔位置计算
                distanceRow = consSQL.distanceRow, // 关键属性：行间距
                distanceColumn = consSQL.distanceColumn, // 关键属性：列间距
                offsetX = consSQL.offsetX,
                offsetY = consSQL.offsetY,
                RobotX = consSQL.RobotX,
                RobotY = consSQL.RobotY,
                RobotZ = consSQL.RobotZ,
                labVolume = consSQL.labVolume,
                consMaxAvaiVol = consSQL.consMaxAvaiVol,
                consDep = consSQL.consDep,
                topShape = consSQL.topShape, // 关键属性：孔形状
                topRadius = consSQL.topRadius, // 关键属性：孔大小
                topUpperX = consSQL.topUpperX,
                topUpperY = consSQL.topUpperY,
                TIPMAXCapacity = consSQL.TIPMAXCapacity,
                TIPMAXAvailable = consSQL.TIPMAXAvailable,
                TIPTotalLength = consSQL.TIPTotalLength,
                TIPHeadHeight = consSQL.TIPHeadHeight,
                TIPConeLength = consSQL.TIPConeLength,
                TIPMAXRadius = consSQL.TIPMAXRadius,
                TIPMINRadius = consSQL.TIPMINRadius,
                TIPDepthOFComp = consSQL.TIPDepthOFComp
            };

            // 3. 订阅新实例事件
            newCons.PropertyChanged += ConsNew_PropertyChanged;

            // 4. 触发UI更新
            consNew = newCons;

            // 5. 强制刷新所有绑定
            this.DataContext = null;
            this.DataContext = this;

            // 6. 更新顶部形状显示
            UpdateTopShapeVisibility(consNew.topShape);
        }
        //更新液体
        private void updateLiquid(LiquidSettings liquidSQL)
        {
            // 取消旧实例事件订阅
            if (liquidNew != null)
                liquidNew.PropertyChanged -= LiquidNew_PropertyChanged;

            // 创建新实例并复制属性
            var newLiquid = new LiquidSettings
            {
                name = liquidSQL.name,
                description = liquidSQL.description,
                aisAirB = liquidSQL.aisAirB,
                aisAirA = liquidSQL.aisAirA,
                aisSpeed = liquidSQL.aisSpeed,
                aisDelay = liquidSQL.aisDelay,
                aisDistance = liquidSQL.aisDistance,
                disAirB = liquidSQL.disAirB,
                disAirA = liquidSQL.disAirA,
                disSpeed = liquidSQL.disSpeed,
                disDelay = liquidSQL.disDelay,
                disDistance = liquidSQL.disDistance
            };

            // 订阅新实例事件
            newLiquid.PropertyChanged += LiquidNew_PropertyChanged;
            liquidNew = newLiquid;
            RefreshPipetteModel();
            // 刷新UI绑定
            this.DataContext = null;
            this.DataContext = this;
        }
        // 提取单独的方法更新顶部形状可见性，避免重复代码
        private void UpdateTopShapeVisibility(int topShape)
        {
            if (topShape == 0) // 圆柱体
            {
                consTopRdiusBlock.Visibility = Visibility.Visible;
                consTopRdiusBox.Visibility = Visibility.Visible;
                consTopWidthBlock.Visibility = Visibility.Collapsed;
                consTopWidthBox.Visibility = Visibility.Collapsed;
                consTopLongBlock.Visibility = Visibility.Collapsed;
                consTopLongBox.Visibility = Visibility.Collapsed;
            }
            else if (topShape == 1) // 立方体
            {
                consTopRdiusBlock.Visibility = Visibility.Collapsed;
                consTopRdiusBox.Visibility = Visibility.Collapsed;
                consTopWidthBlock.Visibility = Visibility.Visible;
                consTopWidthBox.Visibility = Visibility.Visible;
                consTopLongBlock.Visibility = Visibility.Visible;
                consTopLongBox.Visibility = Visibility.Visible;
            }
        }

        //新增耗材
        private async void addConsClick(object sender, RoutedEventArgs e)
        {
            int maxNumber = 0;

            // 遍历现有项，提取数字部分
            foreach (ListBoxItem item in consumableList.Items)
            {
                string content = item.Content.ToString();
                Match match = Regex.Match(content, @"耗材(\d+)");

                if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                {
                    if (number > maxNumber)
                    {
                        maxNumber = number;
                    }
                }
            }

            // 生成新的耗材名称（最大数字+1）
            string newConsumableName = $"耗材{maxNumber + 1}";

            bool addFlag = await databaseService.AddConsumableAsync(newConsumableName);
            if (addFlag)
            {
                // 创建新的ListBoxItem并添加到列表
                ListBoxItem newItem = new ListBoxItem
                {
                    Content = newConsumableName,
                    Padding = new Thickness(10, 8, 10, 8)
                };

                consumableList.Items.Add(newItem);

                // 自动选中新添加的项
                consumableList.SelectedItem = newItem;
                consNew.name = newConsumableName;
                consNew.id = 0;
                consNew.type = 0;
                consNew.description = "";
                consNew.NW = 0;
                consNew.SW = 0;
                consNew.NE = 0;
                consNew.SE = 0;
                consNew.numRows = 0;
                consNew.numColumns = 0;
                consNew.labL = 0;
                consNew.labW = 0;
                consNew.labH = 0;
                consNew.distanceRowY = 0;
                consNew.distanceColumnX = 0;
                consNew.distanceRow = 0;
                consNew.distanceColumn = 0;
                consNew.offsetX = 0;
                consNew.offsetY = 0;
                consNew.RobotX = 0;
                consNew.RobotY = 0;
                consNew.RobotZ = 0;

                consNew.labVolume = 0;
                consNew.consMaxAvaiVol = 0;
                consNew.consDep = 0;
                consNew.topShape = 0;
                consNew.topRadius = 0;
                consNew.topUpperX = 0;
                consNew.topUpperY = 0;

                consNew.TIPMAXCapacity = 0;
                consNew.TIPMAXAvailable = 0;
                consNew.TIPTotalLength = 0;
                consNew.TIPHeadHeight = 0;
                consNew.TIPConeLength = 0;
                consNew.TIPMAXRadius = 0;
                consNew.TIPMINRadius = 0;
                consNew.TIPDepthOFComp = 0;
                _mainWidget.ShowNotification("添加耗材成功", NotificationControl.NotificationType.Info);
            }
            else
            {
                _mainWidget.ShowNotification("添加耗材失败", NotificationControl.NotificationType.Warn);

            }

        }
        //导入耗材
        private void inConsClick(object sender, RoutedEventArgs e)
        {

        }
        //导出耗材
        private void outConsClick(object sender, RoutedEventArgs e)
        {

        }
        //删除耗材
        // PlateSettingsDialog.cs 中实现
        private async void removeConsClick(object sender, RoutedEventArgs e)
        {
            // 检查是否有选中项
            if (consumableList.SelectedItem is not ListBoxItem selectedItem)
            {
                _mainWidget.ShowNotification("请先选择要删除的耗材", NotificationControl.NotificationType.Warn);
                return;
            }

            string consName = selectedItem.Content.ToString();

            //// 弹出确认对话框（可选，防止误操作）
            //var result = MessageBox.Show(
            //    $"确定要删除耗材「{consName}」吗？\n删除后数据将无法恢复。",
            //    "确认删除",
            //    MessageBoxButton.YesNo,
            //    MessageBoxImage.Warning);

            //if (result != MessageBoxResult.Yes)
            //    return;

            // 1. 删除数据库中的记录
            bool isDeleted = await databaseService.DeleteConsumableAsync(consName);
            if (!isDeleted)
            {
                _mainWidget.ShowNotification("删除失败，耗材不存在或已被占用", NotificationControl.NotificationType.Error);
                return;
            }

            // 2. 从ListBox中移除该项
            consumableList.Items.Remove(selectedItem);

            // 3. 清空当前显示的耗材数据（避免显示已删除的数据）
            _mainWidget.ShowNotification($"耗材「{consName}」已成功删除", NotificationControl.NotificationType.Info);
        }
        //新增液体
        private async void addLiquidClick(object sender, RoutedEventArgs e)
        {
            int maxNumber = 0;
            // 遍历液体列表获取最大序号
            foreach (ListBoxItem item in liquidumableList.Items)
            {
                string content = item.Content.ToString();
                Match match = Regex.Match(content, @"液体(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number) && number > maxNumber)
                {
                    maxNumber = number;
                }
            }

            string newLiquidName = $"液体{maxNumber + 1}";
            bool addFlag = await databaseService.AddLiquidAsync(newLiquidName);
            if (addFlag)
            {
                ListBoxItem newItem = new ListBoxItem
                {
                    Content = newLiquidName,
                    Padding = new Thickness(10, 8, 10, 8)
                };
                liquidumableList.Items.Add(newItem);
                liquidumableList.SelectedItem = newItem;

                // 初始化新液体属性（修正为LiquidSettings）
                liquidNew = new LiquidSettings
                {
                    name = newLiquidName,
                    description = "",
                    aisAirB = 0,
                    aisAirA = 0,
                    aisSpeed = 0,
                    aisDelay = 0,
                    aisDistance = 0,
                    disAirB = 0,
                    disAirA = 0,
                    disSpeed = 0,
                    disDelay = 0,
                    disDistance = 0
                };
                liquidNew.PropertyChanged += LiquidNew_PropertyChanged;
                oldLiquidName = newLiquidName;

                _mainWidget.ShowNotification("添加液体成功", NotificationControl.NotificationType.Info);
            }
            else
            {
                _mainWidget.ShowNotification("添加液体失败", NotificationControl.NotificationType.Warn);
            }
        }
        //导入液体
        private void inLiquidClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "液体参数文件 (*.liq)|*.liq|所有文件 (*.*)|*.*",
                Title = "导入液体参数"
            };

            bool? result = openFileDialog.ShowDialog(Window.GetWindow(this));
            if (result == true)
            {
                try
                {
                    var liquid = ImportLiquidFromFile(openFileDialog.FileName);
                    if (liquid != null)
                    {
                        SaveImportedLiquid(liquid);
                    }
                }
                catch (Exception ex)
                {
                    _mainWidget.ShowNotification($"导入失败: {ex.Message}", NotificationControl.NotificationType.Error);
                }
            }
        }
        //导出液体
        private void outLiquidClick(object sender, RoutedEventArgs e)
        {
            if (liquidumableList.SelectedItem is not ListBoxItem selectedItem)
            {
                _mainWidget.ShowNotification("请先选择要导出的液体", NotificationControl.NotificationType.Warn);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "液体参数文件 (*.liq)|*.liq|所有文件 (*.*)|*.*",
                FileName = $"{liquidNew.name}.liq",
                Title = "导出液体参数"
            };

            bool? result = saveFileDialog.ShowDialog(Window.GetWindow(this));
            if (result == true)
            {
                try
                {
                    ExportLiquidToFile(liquidNew, saveFileDialog.FileName);
                    _mainWidget.ShowNotification($"液体「{liquidNew.name}」已成功导出", NotificationControl.NotificationType.Info);
                }
                catch (Exception ex)
                {
                    _mainWidget.ShowNotification($"导出失败: {ex.Message}", NotificationControl.NotificationType.Error);
                }
            }
        }
        private LiquidSettings ImportLiquidFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("找不到指定的液体参数文件", filePath);
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);

            var liquidAttr = xmlDoc.SelectSingleNode("//LiquidClass/LiquidAttribute");
            if (liquidAttr == null)
            {
                throw new XmlException("无效的液体参数文件格式，未找到LiquidAttribute节点");
            }

            // 解析XML并映射到LiquidSettings对象
            var liquid = new LiquidSettings
            {
                name = GetXmlNodeValue(liquidAttr, "Name", "导入的液体"),
                description = GetXmlNodeValue(liquidAttr, "Description", ""),

                // 吸液参数 (A_开头对应ais属性)
                aisSpeed = ParseXmlNodeValue<float>(liquidAttr, "A_Speed", 100f),
                aisDelay = ParseXmlNodeValue<float>(liquidAttr, "A_Delay", 0.5f),
                aisAirB = ParseXmlNodeValue<float>(liquidAttr, "A_Preair", 0f),
                aisAirA = ParseXmlNodeValue<float>(liquidAttr, "A_Postair", 0f),
                aisDistance = ParseXmlNodeValue<float>(liquidAttr, "A_DisfwBottom", 1.5f),

                // 注液参数 (D_开头对应dis属性)
                disSpeed = ParseXmlNodeValue<float>(liquidAttr, "D_Speed", 100f),
                disDelay = ParseXmlNodeValue<float>(liquidAttr, "D_Delay", 0.5f),
                disAirB = ParseXmlNodeValue<float>(liquidAttr, "D_Preair", 0f),
                disAirA = ParseXmlNodeValue<float>(liquidAttr, "D_Postair", 0f),
                disDistance = ParseXmlNodeValue<float>(liquidAttr, "D_DisfwBottom", 1.5f)
            };

            return liquid;
        }

        // 将液体参数导出到文件
        private void ExportLiquidToFile(LiquidSettings liquid, string filePath)
        {
            var xmlDoc = new XmlDocument();

            // 创建XML声明
            var xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
            xmlDoc.AppendChild(xmlDeclaration);

            // 创建根节点
            var root = xmlDoc.CreateElement("LiquidClass");
            root.SetAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
            root.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            xmlDoc.AppendChild(root);

            // 创建液体属性节点
            var liquidAttr = xmlDoc.CreateElement("LiquidAttribute");
            root.AppendChild(liquidAttr);

            // 添加基本信息
            AddXmlNode(xmlDoc, liquidAttr, "ID", "100148"); // 可以根据实际情况修改
            AddXmlNode(xmlDoc, liquidAttr, "Name", liquid.name);
            AddXmlNode(xmlDoc, liquidAttr, "Author", "");
            AddXmlNode(xmlDoc, liquidAttr, "LiquidType", "Custom");
            AddXmlNode(xmlDoc, liquidAttr, "Description", liquid.description);

            // 添加吸液参数 (A_开头)
            AddXmlNode(xmlDoc, liquidAttr, "A_Speed", liquid.aisSpeed.ToString("F2"));
            AddXmlNode(xmlDoc, liquidAttr, "A_Acceleration", "1000");
            AddXmlNode(xmlDoc, liquidAttr, "A_Deceleration", "1000");
            AddXmlNode(xmlDoc, liquidAttr, "A_Delay", liquid.aisDelay.ToString("F2"));
            AddXmlNode(xmlDoc, liquidAttr, "A_Preair", liquid.aisAirB.ToString("F2"));
            AddXmlNode(xmlDoc, liquidAttr, "A_PreairDelay", "0");
            AddXmlNode(xmlDoc, liquidAttr, "A_Postair", liquid.aisAirA.ToString("F2"));
            AddXmlNode(xmlDoc, liquidAttr, "A_PostairDelay", "0");
            AddXmlNode(xmlDoc, liquidAttr, "A_DisfwBottom", liquid.aisDistance.ToString("F2"));

            // 添加注液参数 (D_开头)
            AddXmlNode(xmlDoc, liquidAttr, "D_Speed", liquid.disSpeed.ToString("F2"));
            AddXmlNode(xmlDoc, liquidAttr, "D_Acceleration", "1000");
            AddXmlNode(xmlDoc, liquidAttr, "D_Deceleration", "1000");
            AddXmlNode(xmlDoc, liquidAttr, "D_Delay", liquid.disDelay.ToString("F2"));
            AddXmlNode(xmlDoc, liquidAttr, "D_Preair", liquid.disAirB.ToString("F2"));
            AddXmlNode(xmlDoc, liquidAttr, "D_PreairDelay", "0");
            AddXmlNode(xmlDoc, liquidAttr, "D_Postair", liquid.disAirA.ToString("F2"));
            AddXmlNode(xmlDoc, liquidAttr, "D_PostairDelay", "0");
            AddXmlNode(xmlDoc, liquidAttr, "D_DisfwBottom", liquid.disDistance.ToString("F2"));

            // 添加其他必要的固定参数
            AddXmlNode(xmlDoc, liquidAttr, "A_MSIntoWell", "100");
            AddXmlNode(xmlDoc, liquidAttr, "A_MSOutofWell", "50");
            AddXmlNode(xmlDoc, liquidAttr, "D_MSIntoWell", "100");
            AddXmlNode(xmlDoc, liquidAttr, "D_MSOutofWell", "50");
            AddXmlNode(xmlDoc, liquidAttr, "UID", "2");

            // 保存文件
            xmlDoc.Save(filePath);
        }
        private async void SaveImportedLiquid(LiquidSettings liquid)
        {
            // 处理名称重复
            string originalName = liquid.name;
            int counter = 1;
            while (liquidumableList.Items.Cast<ListBoxItem>()
                .Any(item => item.Content.ToString() == liquid.name))
            {
                liquid.name = $"{originalName}_{counter}";
                counter++;
            }

            // 添加到数据库
            bool addSuccess = await databaseService.AddLiquidAsync(liquid.name);
            if (!addSuccess)
            {
                _mainWidget.ShowNotification("添加液体到数据库失败", NotificationControl.NotificationType.Error);
                return;
            }

            // 更新数据库中的详细参数
            bool updateSuccess = await databaseService.UpdateLiquidAsync(liquid);
            if (!updateSuccess)
            {
                _mainWidget.ShowNotification("更新液体参数失败", NotificationControl.NotificationType.Warn);
            }

            // 更新UI
            var newItem = new ListBoxItem
            {
                Content = liquid.name,
                Padding = new Thickness(10, 8, 10, 8)
            };
            liquidumableList.Items.Add(newItem);
            liquidumableList.SelectedItem = newItem;

            // 加载新液体数据
            updateLiquid(liquid);
            oldLiquidName = liquid.name;

            _mainWidget.ShowNotification($"液体「{liquid.name}」导入成功", NotificationControl.NotificationType.Info);
        }

        // 辅助方法：添加XML节点
        private void AddXmlNode(XmlDocument doc, XmlNode parent, string nodeName, string value)
        {
            var node = doc.CreateElement(nodeName);
            node.InnerText = value;
            parent.AppendChild(node);
        }

        // 辅助方法：获取XML节点值
        private string GetXmlNodeValue(XmlNode parent, string nodeName, string defaultValue)
        {
            var node = parent.SelectSingleNode(nodeName);
            return node?.InnerText ?? defaultValue;
        }

        // 辅助方法：解析XML节点值为指定类型
        private T ParseXmlNodeValue<T>(XmlNode parent, string nodeName, T defaultValue)
        {
            string value = GetXmlNodeValue(parent, nodeName, null);
            if (value == null)
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        //删除液体
        private async void removeLiquidClick(object sender, RoutedEventArgs e)
        {
            if (liquidumableList.SelectedItem is not ListBoxItem selectedItem)
            {
                _mainWidget.ShowNotification("请先选择要删除的液体", NotificationControl.NotificationType.Warn);
                return;
            }

            string liquidName = selectedItem.Content.ToString();
            //var result = MessageBox.Show(
            //    $"确定要删除液体「{liquidName}」吗？\n删除后数据将无法恢复。",
            //    "确认删除",
            //    MessageBoxButton.YesNo,
            //    MessageBoxImage.Warning);

            //if (result != MessageBoxResult.Yes)
            //    return;

            // 从数据库删除
            bool isDeleted = await databaseService.DeleteLiquidAsync(liquidName);
            if (!isDeleted)
            {
                _mainWidget.ShowNotification("删除失败，液体不存在或已被占用", NotificationControl.NotificationType.Error);
                return;
            }

            // 从列表移除
            liquidumableList.Items.Remove(selectedItem);
            _mainWidget.ShowNotification($"液体「{liquidName}」已成功删除", NotificationControl.NotificationType.Info);
        }
        //点击耗材管理
        private void transToCons(object sender, RoutedEventArgs e)
        {
            mainSetTable.SelectedIndex = 0;

        }
        //点击液体管理
        private void transToLiquid(object sender, RoutedEventArgs e)
        {
            mainSetTable.SelectedIndex = 1;

        }
        //手动控制
        private void transToControl(object sender, RoutedEventArgs e)
        {
            mainSetTable.SelectedIndex = 2;

        }
        //手动控制
        //12板位
        // 板位按钮点击事件：切换选中板位并加载坐标
        private void PlateButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton btn && btn.Tag is string plateId)
            {
                // 重置所有板位按钮状态
                foreach (var child in FindVisualChildren<ToggleButton>(this))
                {
                    if (child.Name.StartsWith("btnPlate"))
                        child.IsChecked = false;
                }
                btn.IsChecked = true;
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualGetPlate, NotificationControl.NotificationType.Info);
                LoadPlateCoordinates("p" + plateId);

                if (int.Parse(plateId) < 4 || int.Parse(plateId) > 12)
                    btnMoveToPlate.IsEnabled = false;
                else
                    btnMoveToPlate.IsEnabled = true;
            }
        }
        // 辅助方法：查找视觉子元素
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                        yield return t;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }
        }
        // 加载板位坐标
        private async Task LoadPlateCoordinates(string plateId)
        {
            string sql = "SELECT `offset` FROM plate WHERE name = @name";
            MySqlParameter[] param = new MySqlParameter[]
            {
    new MySqlParameter("@name", plateId)
            };
            DataTable dt = await databaseService.QueryMySqlDataAsync(sql, param);
            if (dt.Rows.Count > 0)
            {
                string offsetValue = dt.Rows[0]["offset"].ToString();
                if (!string.IsNullOrWhiteSpace(offsetValue))
                {
                    string[] coordinateArray = offsetValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (coordinateArray.Length == 3)
                    {
                        if (double.TryParse(coordinateArray[0].Trim(), out double xValue))
                        {
                            txtPlateX.Text = xValue.ToString("F2");
                        }
                        else
                        {
                            txtPlateX.Text = string.Empty;
                        }

                        if (double.TryParse(coordinateArray[1].Trim(), out double yValue))
                        {
                            txtPlateY.Text = yValue.ToString("F2");
                        }
                        else
                        {
                            txtPlateY.Text = string.Empty;
                        }

                        if (double.TryParse(coordinateArray[2].Trim(), out double zValue))
                        {
                            txtPlateZ.Text = zValue.ToString("F2");
                        }
                        else
                        {
                            txtPlateZ.Text = string.Empty;
                        }

                        _mainWidget.ShowNotification(_mainWidget._res.SettingManualGetPlateSucc, NotificationControl.NotificationType.Info);
                    }
                    else
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.SettingManualGetPlateFail, NotificationControl.NotificationType.Warn);

                        txtPlateX.Text = txtPlateY.Text = txtPlateZ.Text = string.Empty;
                    }
                    _mainWidget.ShowNotification(_mainWidget._res.SettingManualGetPlateSucc, NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.SettingManualGetPlateFail, NotificationControl.NotificationType.Warn);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualGetPlateFail, NotificationControl.NotificationType.Warn);

                txtPlateX.Text = txtPlateY.Text = txtPlateZ.Text = string.Empty;
            }
        }
        // 移动到指定板位坐标
        private async void MoveToPlate_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtPlateX.Text, out float numX) &&
                float.TryParse(txtPlateY.Text, out float numY)
               )
            {
                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from arm import Arm");
                pythonCode.AppendLine($"Arm.to(1, {{'x': {numX},'y':{numY}}} )");
                var rawMoveToFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
                var moveToFlag = _mainWidget.ParseScriptDebugResponse(rawMoveToFlag);
                if (moveToFlag != null)
                {
                    if (moveToFlag.Result == "succeed")
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveSucc, NotificationControl.NotificationType.Info);
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
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
            }
        }
        //移动Z轴
        private async void DownToPlate_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtPlateZ.Text, out float numZ))
            {
                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from arm import Arm");
                pythonCode.AppendLine($"Arm.to(1, {{'z': {numZ}}} )");
                var rawDownMoveFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());
                var downMoveFlag = _mainWidget.ParseScriptDebugResponse(rawDownMoveFlag);
                if (downMoveFlag != null)
                {
                    if (downMoveFlag.Result == "succeed")
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveSucc, NotificationControl.NotificationType.Info);
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
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
            }

        }
        // 保存当前板位坐标
        private async void SavePlatePos_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前选中的板位
            var selectedPlate = FindVisualChildren<ToggleButton>(this)
                .FirstOrDefault(btn => btn.Name.StartsWith("btnPlate") && btn.IsChecked == true)?.Tag?.ToString();

            if (selectedPlate == null)
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualSetPlateNull, NotificationControl.NotificationType.Warn);
                return;
            }
            string nowPlate = "p" + selectedPlate;

            if (float.TryParse(txtPlateX.Text, out float x) &&
                float.TryParse(txtPlateY.Text, out float y) &&
                float.TryParse(txtPlateZ.Text, out float z))
            {

                string offset = $"{x},{y},{z}";

                string updateSql = "UPDATE plate SET `offset` = @offset WHERE name = @name";

                MySqlParameter[] updateParam = new MySqlParameter[]
                {
                new MySqlParameter("@name", nowPlate),
                new MySqlParameter("@offset", offset)
                };

                int affectedRows = await databaseService.ExecuteMySqlNonQueryAsync(updateSql, updateParam);

                if (affectedRows > 0)
                {
                    _mainWidget.ShowNotification(_mainWidget._res.SettingManualSetPlateSucc, NotificationControl.NotificationType.Info);
                }
                else//Y120X162
                {
                    _mainWidget.ShowNotification(_mainWidget._res.SettingManualSetPlateFail, NotificationControl.NotificationType.Warn);

                }

            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
            }
        }
        //复位X
        private async void btnResetX_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualStartResetX, NotificationControl.NotificationType.Info);
            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from arm import Arm");
            pythonCode.AppendLine("Arm.reset(1,{'x'})");

            var rawXResetFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//复位X
            var xResetFlag = _mainWidget.ParseScriptDebugResponse(rawXResetFlag);
            if (xResetFlag != null)
            {
                if (xResetFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.SettingManualResetSucc, NotificationControl.NotificationType.Info);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }
        //复位Y
        private async void btnResetY_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualStartResetY, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from arm import Arm");
            pythonCode.AppendLine("Arm.reset(1,{'y'})");

            var rawYResetFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//复位y
            var yResetFlag = _mainWidget.ParseScriptDebugResponse(rawYResetFlag);
            if (yResetFlag != null)
            {
                if (yResetFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.SettingManualResetSucc, NotificationControl.NotificationType.Info);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }
        //复位Z
        private async void btnResetZ_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification(_mainWidget._res.SettingManualStartResetZ, NotificationControl.NotificationType.Info);

            StringBuilder pythonCode = new StringBuilder();
            pythonCode.AppendLine("from arm import Arm");
            if (jumpZButton.IsChecked == true)
                pythonCode.AppendLine("Arm.reset(1,{'z'})");
            else if (jumpZ2Button.IsChecked == true)
                pythonCode.AppendLine("Arm.reset(2,{'z'})");
            else if (jumpZ3Button.IsChecked == true)
                pythonCode.AppendLine("Arm.reset(3,{'z'})");

            var rawZResetFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//复位Z
            var zResetFlag = _mainWidget.ParseScriptDebugResponse(rawZResetFlag);
            if (zResetFlag != null)
            {
                if (zResetFlag.Result == "succeed")
                {
                    _mainWidget.ShowNotification(_mainWidget._res.SettingManualResetSucc, NotificationControl.NotificationType.Info);
                }
            }
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.WindowGrpcComFail, NotificationControl.NotificationType.Error);
            }
        }
        // 微调按钮点击事件
        private void JumpSizeRadio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton radioBtn || !radioBtn.IsChecked.GetValueOrDefault())
                return;

            if (string.IsNullOrEmpty(radioBtn.Tag?.ToString()))
                return;

            var tagText = radioBtn.Tag.ToString().Trim();
            var numberPart = new string(tagText.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());

            if (float.TryParse(numberPart, out float jumpSize))
            {
                _currentJumpSize = jumpSize;

            }
        }
        //XY方向
        private void JumpDirectionXY_Click(object sender, RoutedEventArgs e)
        {
            JumpLeftButton.IsEnabled = true;
            JumpRightButton.IsEnabled = true;
        }
        //Z方向
        private void JumpDirectionZ_Click(object sender, RoutedEventArgs e)
        {
            JumpLeftButton.IsEnabled = false;
            JumpRightButton.IsEnabled = false;
        }
        //Z2方向
        private void JumpDirectionZ2_Click(object sender, RoutedEventArgs e)
        {
            JumpLeftButton.IsEnabled = false;
            JumpRightButton.IsEnabled = false;
        }
        //Z3方向

        private void JumpDirectionZ3_Click(object sender, RoutedEventArgs e)
        {
            JumpLeftButton.IsEnabled = false;
            JumpRightButton.IsEnabled = false;
        }
        //左(X减少)
        private async void JumpLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtPlateX.Text, out float currentX))
            {
                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from arm import Arm");
                pythonCode.AppendLine($"Arm.by(1, {{'x': -{_currentJumpSize}}} )");
                var rawLeftFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//z轴上升
                var leftMoveFlag = _mainWidget.ParseScriptDebugResponse(rawLeftFlag);
                if (leftMoveFlag != null)
                {
                    if (leftMoveFlag.Result == "succeed")
                    {
                        float newX = currentX - _currentJumpSize;
                        txtPlateX.Text = newX.ToString("F2");
                        _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveSucc, NotificationControl.NotificationType.Info);
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
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
            }
        }
        //上
        private async void JumpUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (jumpXYButton.IsChecked == true)//Y
            {
                if (float.TryParse(txtPlateY.Text, out float currentY))
                {
                    StringBuilder pythonCode = new StringBuilder();
                    pythonCode.AppendLine("from arm import Arm");
                    pythonCode.AppendLine($"Arm.by(1, {{'y': -{_currentJumpSize}}} )");
                    var rawUpMoveFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//z轴上升
                    var upMoveFlag = _mainWidget.ParseScriptDebugResponse(rawUpMoveFlag);
                    if (upMoveFlag != null)
                    {
                        if (upMoveFlag.Result == "succeed")
                        {
                            float newY = currentY - _currentJumpSize;
                            txtPlateY.Text = newY.ToString("F2");
                            _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveSucc, NotificationControl.NotificationType.Info);
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
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
                }
            }
            else if (jumpZButton.IsChecked == true) //Z
            {
                if (float.TryParse(txtPlateZ.Text, out float currentZ))
                {
                    StringBuilder pythonCode = new StringBuilder();
                    pythonCode.AppendLine("from arm import Arm");
                    pythonCode.AppendLine($"Arm.by(1, {{'z': -{_currentJumpSize}}} )");
                    var rawUpMoveFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//z轴上升
                    var upMoveFlag = _mainWidget.ParseScriptDebugResponse(rawUpMoveFlag);
                    if (upMoveFlag != null)
                    {
                        if (upMoveFlag.Result == "succeed")
                        {
                            float newZ = currentZ - _currentJumpSize;
                            txtPlateZ.Text = newZ.ToString("F2");
                            _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveSucc, NotificationControl.NotificationType.Info);
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
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
                }
            }
            else if (jumpZ2Button.IsChecked == true) //Z2
            {

                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from arm import Arm");
                pythonCode.AppendLine($"Arm.by(2, {{'z': -{_currentJumpSize}}} )");
                var rawUpMoveFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//z轴上升
                var upMoveFlag = _mainWidget.ParseScriptDebugResponse(rawUpMoveFlag);
                if (upMoveFlag != null)
                {
                    if (upMoveFlag.Result == "succeed")
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveSucc, NotificationControl.NotificationType.Info);
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
            else if (jumpZ3Button.IsChecked == true) //Z3
            {

                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from arm import Arm");
                pythonCode.AppendLine($"Arm.by(3, {{'z': -{_currentJumpSize}}} )");
                var rawUpMoveFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//z轴上升
                var upMoveFlag = _mainWidget.ParseScriptDebugResponse(rawUpMoveFlag);
                if (upMoveFlag != null)
                {
                    if (upMoveFlag.Result == "succeed")
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveSucc, NotificationControl.NotificationType.Info);
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
        }
        //下
        private async void JumpDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (jumpXYButton.IsChecked == true)//Y
            {
                if (float.TryParse(txtPlateY.Text, out float currentY))
                {
                    StringBuilder pythonCode = new StringBuilder();
                    pythonCode.AppendLine("from arm import Arm");
                    pythonCode.AppendLine($"Arm.by(1, {{'y': {_currentJumpSize}}} )");
                    var rawDownMoveFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//z轴下降
                    var downMoveFlag = _mainWidget.ParseScriptDebugResponse(rawDownMoveFlag);
                    if (downMoveFlag != null)
                    {
                        if (downMoveFlag.Result == "succeed")
                        {
                            float newY = currentY + _currentJumpSize;
                            txtPlateY.Text = newY.ToString("F2");
                            _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveSucc, NotificationControl.NotificationType.Info);
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
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
                }
            }
            else if (jumpZButton.IsChecked == true)//Z
            {
                if (float.TryParse(txtPlateZ.Text, out float currentZ))
                {
                    StringBuilder pythonCode = new StringBuilder();
                    pythonCode.AppendLine("from arm import Arm");
                    pythonCode.AppendLine($"Arm.by(1, {{'z': {_currentJumpSize}}} )");
                    var rawDownMoveFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//z轴下降
                    var downMoveFlag = _mainWidget.ParseScriptDebugResponse(rawDownMoveFlag);
                    if (downMoveFlag != null)
                    {
                        if (downMoveFlag.Result == "succeed")
                        {
                            float newZ = currentZ + _currentJumpSize;
                            txtPlateZ.Text = newZ.ToString("F2");
                            _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveSucc, NotificationControl.NotificationType.Info);
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
                else
                {
                    _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
                }
            }
            else if (jumpZ2Button.IsChecked == true) //Z2
            {

                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from arm import Arm");
                pythonCode.AppendLine($"Arm.by(2, {{'z': {_currentJumpSize}}} )");
                var rawUpMoveFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//z轴上升
                var upMoveFlag = _mainWidget.ParseScriptDebugResponse(rawUpMoveFlag);
                if (upMoveFlag != null)
                {
                    if (upMoveFlag.Result == "succeed")
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveSucc, NotificationControl.NotificationType.Info);
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
            else if (jumpZ3Button.IsChecked == true) //Z3
            {

                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from arm import Arm");
                pythonCode.AppendLine($"Arm.by(3, {{'z': {_currentJumpSize}}} )");
                var rawUpMoveFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//z轴上升
                var upMoveFlag = _mainWidget.ParseScriptDebugResponse(rawUpMoveFlag);
                if (upMoveFlag != null)
                {
                    if (upMoveFlag.Result == "succeed")
                    {
                        _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveSucc, NotificationControl.NotificationType.Info);
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
        }
        //右
        private async void JumpRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtPlateX.Text, out float currentX))
            {
                StringBuilder pythonCode = new StringBuilder();
                pythonCode.AppendLine("from arm import Arm");
                pythonCode.AppendLine($"Arm.by(1, {{'x': {_currentJumpSize}}} )");
                var rawRIghtMoveFlag = await _mainWidget.ScriptDebugAsync(pythonCode.ToString());//z轴下降
                var rightMoveFlag = _mainWidget.ParseScriptDebugResponse(rawRIghtMoveFlag);
                if (rightMoveFlag != null)
                {
                    if (rightMoveFlag.Result == "succeed")
                    {
                        float newX = currentX + _currentJumpSize;
                        txtPlateX.Text = newX.ToString("F2");
                        _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveSucc, NotificationControl.NotificationType.Info);
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
            else
            {
                _mainWidget.ShowNotification(_mainWidget._res.SettingManualMoveCoordinate, NotificationControl.NotificationType.Warn);
            }
        }

        #region 手动控制-模块添加

        private void PipetteSettingOneBtn_Click(object sender, RoutedEventArgs e)
        {
            TopSettingPopup.Show(0, "pipette_1", 1);
        }
        private void PipetteSettingTwoBtn_Click(object sender, RoutedEventArgs e)
        {
            TopSettingPopup.Show(0, "pipette_2", 2);
        }
        private void GripperSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            TopSettingPopup.Show(3, "shift_1", 1);
        }

        private void PCRSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            TopSettingPopup.Show(4, "PCR_1", 1);
        }

        private void AddHeatingOscillModule(int nowId, string nowPlate)//"P1" 
        {
            _heatingOscillCount++;
            var newModule = new ModuleDatas { Name = $"shaker_{nowId}", Type = 5, PlatePosition = nowPlate };
            _heatingOscillModules.Add(newModule);

            var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };

            var nameText = new TextBlock
            {
                Text = $"shaker_{_heatingOscillCount}",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#666666")),
                Width = 66
            };
            rowPanel.Children.Add(nameText);

            var plateTextBox = new TextBox
            {
                Margin = new Thickness(5, 0, 0, 0),
                Text = nowPlate,
                IsReadOnly = true,
                Width = 60,
                Style = (Style)FindResource("InputTextBoxStyle")
            };

            rowPanel.Children.Add(plateTextBox);

            var editBtn = new Button
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            var primaryColorBrush = Application.Current.Resources["PrimaryColor"] as SolidColorBrush;

            editBtn.Content = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M19.14,12.94c0.04-0.3,0.06-0.61,0.06-0.94c0-0.32-0.02-0.64-0.07-0.94l2.03-1.58c0.18-0.14,0.23-0.41,0.12-0.61 l-1.92-3.32c-0.12-0.22-0.37-0.29-0.59-0.22l-2.39,0.96c-0.5-0.38-1.03-0.7-1.62-0.94L14.4,2.81c-0.04-0.24-0.24-0.41-0.48-0.41 h-3.84c-0.24,0-0.43,0.17-0.47,0.41L9.25,5.35C8.66,5.59,8.12,5.92,7.63,6.29L5.24,5.33c-0.22-0.08-0.47,0-0.59,0.22L2.74,8.87 C2.62,9.08,2.66,9.34,2.86,9.48l2.03,1.58C4.84,11.36,4.8,11.69,4.8,12s0.02,0.64,0.07,0.94l-2.03,1.58 c-0.18,0.14-0.23,0.41-0.12,0.61l1.92,3.32c0.12,0.22,0.37,0.29,0.59,0.22l2.39-0.96c0.5,0.38,1.03,0.7,1.62,0.94l0.36,2.54 c0.05,0.24,0.24,0.41,0.48,0.41h3.84c0.24,0,0.44-0.17,0.47-0.41l0.36-2.54c0.59-0.24,1.13-0.56,1.62-0.94l2.39,0.96 c0.22,0.08,0.47,0,0.59-0.22l1.92-3.32c0.12-0.22,0.07-0.47-0.12-0.61L19.14,12.94z M12,15.6c-1.98,0-3.6-1.62-3.6-3.6 s1.62-3.6,3.6-3.6s3.6,1.62,3.6,3.6S13.98,15.6,12,15.6z"),
                Fill = primaryColorBrush,
                Stretch = Stretch.Uniform
            };
            editBtn.Click += (s, e) =>
            {
                TopSettingPopup.Show(5, $"shaker_{_heatingOscillCount}", _heatingOscillCount);
            };
            rowPanel.Children.Add(editBtn);

            heatingOscillContainer.Children.Add(rowPanel);
        }
        //磁吸模块添加
        private void AddMagnetModule(int nowId, string nowPlate)
        {
            _magneticCount++;
            var newModule = new ModuleDatas { Name = $"magnetic_{nowId}", Type = 6, PlatePosition = nowPlate };

            _magneticModules.Add(newModule);

            var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };

            var nameText = new TextBlock
            {
                Text = $"magnetic_{_magneticCount}",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#666666")),
                Width = 84
            };
            rowPanel.Children.Add(nameText);

            var plateTextBox = new TextBox
            {
                Margin = new Thickness(5, 0, 0, 0),
                Text = nowPlate,
                IsReadOnly = true,
                Width = 60,
                Style = (Style)FindResource("InputTextBoxStyle")
            };

            rowPanel.Children.Add(plateTextBox);

            var editBtn = new Button
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            var primaryColorBrush = Application.Current.Resources["PrimaryColor"] as SolidColorBrush;

            editBtn.Content = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M19.14,12.94c0.04-0.3,0.06-0.61,0.06-0.94c0-0.32-0.02-0.64-0.07-0.94l2.03-1.58c0.18-0.14,0.23-0.41,0.12-0.61 l-1.92-3.32c-0.12-0.22-0.37-0.29-0.59-0.22l-2.39,0.96c-0.5-0.38-1.03-0.7-1.62-0.94L14.4,2.81c-0.04-0.24-0.24-0.41-0.48-0.41 h-3.84c-0.24,0-0.43,0.17-0.47,0.41L9.25,5.35C8.66,5.59,8.12,5.92,7.63,6.29L5.24,5.33c-0.22-0.08-0.47,0-0.59,0.22L2.74,8.87 C2.62,9.08,2.66,9.34,2.86,9.48l2.03,1.58C4.84,11.36,4.8,11.69,4.8,12s0.02,0.64,0.07,0.94l-2.03,1.58 c-0.18,0.14-0.23,0.41-0.12,0.61l1.92,3.32c0.12,0.22,0.37,0.29,0.59,0.22l2.39-0.96c0.5,0.38,1.03,0.7,1.62,0.94l0.36,2.54 c0.05,0.24,0.24,0.41,0.48,0.41h3.84c0.24,0,0.44-0.17,0.47-0.41l0.36-2.54c0.59-0.24,1.13-0.56,1.62-0.94l2.39,0.96 c0.22,0.08,0.47,0,0.59-0.22l1.92-3.32c0.12-0.22,0.07-0.47-0.12-0.61L19.14,12.94z M12,15.6c-1.98,0-3.6-1.62-3.6-3.6 s1.62-3.6,3.6-3.6s3.6,1.62,3.6,3.6S13.98,15.6,12,15.6z"),
                Fill = primaryColorBrush,
                Stretch = Stretch.Uniform
            };
            editBtn.Click += (s, e) =>
            {
                TopSettingPopup.Show(6, $"magnetic_{_magneticCount}", _magneticCount);
            };
            rowPanel.Children.Add(editBtn);

            magnetContainer.Children.Add(rowPanel);
        }
        //温控模块添加
        private void AddTempControlModule(int nowId, string nowPlate)
        {
            _tempCount++;
            var newModule = new ModuleDatas { Name = $"tempctrl_{_tempCount}", Type = 7, PlatePosition = "P1" };
            _tempModules.Add(newModule);

            var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };

            var nameText = new TextBlock
            {
                Text = $"tempctrl_{_tempCount}",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#666666")),
                Width = 84
            };
            rowPanel.Children.Add(nameText);

            var plateTextBox = new TextBox
            {
                Margin = new Thickness(5, 0, 0, 0),
                Text = nowPlate,
                IsReadOnly = true,
                Width = 60,
                Style = (Style)FindResource("InputTextBoxStyle")
            };
            rowPanel.Children.Add(plateTextBox);

            var editBtn = new Button
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            var primaryColorBrush = Application.Current.Resources["PrimaryColor"] as SolidColorBrush;

            editBtn.Content = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M19.14,12.94c0.04-0.3,0.06-0.61,0.06-0.94c0-0.32-0.02-0.64-0.07-0.94l2.03-1.58c0.18-0.14,0.23-0.41,0.12-0.61 l-1.92-3.32c-0.12-0.22-0.37-0.29-0.59-0.22l-2.39,0.96c-0.5-0.38-1.03-0.7-1.62-0.94L14.4,2.81c-0.04-0.24-0.24-0.41-0.48-0.41 h-3.84c-0.24,0-0.43,0.17-0.47,0.41L9.25,5.35C8.66,5.59,8.12,5.92,7.63,6.29L5.24,5.33c-0.22-0.08-0.47,0-0.59,0.22L2.74,8.87 C2.62,9.08,2.66,9.34,2.86,9.48l2.03,1.58C4.84,11.36,4.8,11.69,4.8,12s0.02,0.64,0.07,0.94l-2.03,1.58 c-0.18,0.14-0.23,0.41-0.12,0.61l1.92,3.32c0.12,0.22,0.37,0.29,0.59,0.22l2.39-0.96c0.5,0.38,1.03,0.7,1.62,0.94l0.36,2.54 c0.05,0.24,0.24,0.41,0.48,0.41h3.84c0.24,0,0.44-0.17,0.47-0.41l0.36-2.54c0.59-0.24,1.13-0.56,1.62-0.94l2.39,0.96 c0.22,0.08,0.47,0,0.59-0.22l1.92-3.32c0.12-0.22,0.07-0.47-0.12-0.61L19.14,12.94z M12,15.6c-1.98,0-3.6-1.62-3.6-3.6 s1.62-3.6,3.6-3.6s3.6,1.62,3.6,3.6S13.98,15.6,12,15.6z"),
                Fill = primaryColorBrush,
                Stretch = Stretch.Uniform
            };
            editBtn.Click += (s, e) =>
            {
                TopSettingPopup.Show(7, $"tempctrl_{_tempCount}", _tempCount);

            };
            rowPanel.Children.Add(editBtn);

            tempControlContainer.Children.Add(rowPanel);
        }


        #endregion

        #region  移液器3D
        private void InitPipette3D()
        {
            _pipetteContainer = new ContainerUIElement3D();
            Pipette3DView.Children.Add(_pipetteContainer);

            RefreshPipetteModel();
        }
        // 核心方法：刷新移液器模型（参数变化时调用）
        private void RefreshPipetteModel()
        {
            if (_pipetteContainer == null) return;
            _pipetteContainer.Children.Clear();

            const float tipMaxCapacity = 1000f;
            float preAir = liquidNew?.aisAirB ?? 0f;    // 前吸空气
            float postAir = liquidNew?.aisAirA ?? 0f;  // 后吸空气
            float liquidVol = tipMaxCapacity - preAir - postAir;
            if (liquidVol < 0) liquidVol = 0;
            if (preAir < 0) preAir = 0;
            if (postAir < 0) postAir = 0;

            var config = new PlateConfig
            {
                GTopHeight = 5.87,       // 挂台高度 (对应你 liquidNew 里的某个参数)
                TopRadius = 3.4,          // 挂台半径
                PassageTopRadius = 3.09,  // 通道顶半径
                PassageHeight = 62.36,    // 通道高度
                TailConeRadius = 2.6,     // 尾锥底半径
                TailConeHeight = 27.98,   // 尾锥高度
                TailEndRadiu = 0.5,       // 吸尖半径
                FilterinHeight = 77.20,   // 滤芯所在高度
                FilterHeight = 3.48        // 滤芯高度
            };

            // 2. 生成移液器 (0,0,0 为中心，放在容器里)
            CreateLiquidColumn(config, preAir, postAir, liquidVol, tipMaxCapacity, 0, 0, 0, _pipetteContainer);


            CreatePipetteInTab(config, 0, 0, 0, _pipetteContainer);


            // 3. 稍微放大一点，方便观察
            _pipetteContainer.Transform = new ScaleTransform3D(1.5, 1.5, 1.5, 0, 0, 0);
        }

        // 复用你 MainWindow 里的 CreateDynamicPipette 逻辑 (简化版)
        private void CreatePipetteInTab(PlateConfig config, double offsetX, double offsetY, double offsetZ, ContainerUIElement3D container)
        {
            if (config == null) return;

            // 材质：半透明塑料
            Color plasticColor = Color.FromRgb(240, 240, 240);
            Material tipMaterial = CreateTransparentPlastic(plasticColor, 180);
            Material filterMaterial = MaterialHelper.CreateMaterial(Colors.Red); // 滤芯红色

            double topRadius = config.TopRadius;
            double topHeight = config.GTopHeight;
            double passageRadius = config.PassageTopRadius;
            double passageHeight = config.PassageHeight;
            double tailConeRadius = config.TailConeRadius;
            double tailConeHeight = config.TailConeHeight;
            double tailEndRadiu = config.TailEndRadiu;
            double filterinHeight = config.FilterinHeight;
            double filterHeight = config.FilterHeight;

            double currentZ = topHeight + passageHeight + tailConeHeight;

            // 1. 顶部挂台
            Point3D t1 = new Point3D(offsetX, offsetY, currentZ + offsetZ);
            Point3D t2 = new Point3D(offsetX, offsetY, currentZ - topHeight + offsetZ);
            container.Children.Add(AddCone(t1, t2, topRadius, topRadius, tipMaterial));
            currentZ -= topHeight;

            // 2. 中间通道
            Point3D t3 = new Point3D(offsetX, offsetY, currentZ + offsetZ);
            Point3D t4 = new Point3D(offsetX, offsetY, currentZ - passageHeight + offsetZ);
            container.Children.Add(AddCone(t3, t4, passageRadius, tailConeRadius, tipMaterial));
            currentZ -= passageHeight;

            // 3. 尾部锥尖
            Point3D t5 = new Point3D(offsetX, offsetY, currentZ + offsetZ);
            Point3D t6 = new Point3D(offsetX, offsetY, currentZ - tailConeHeight + offsetZ);
            container.Children.Add(AddCone(t5, t6, tailConeRadius, tailEndRadiu, tipMaterial));

            // 4. 中间滤芯 (红色)
            Point3D filterPos = new Point3D(offsetX, offsetY, filterinHeight + offsetZ);
            double filterAvgR = CalculateFilterAverageRadius(
                passageHeight, tailConeRadius, passageRadius,
                filterinHeight - tailConeHeight, filterHeight) - 0.5;

            container.Children.Add(AddCone(
                filterPos,
                new Point3D(filterPos.X, filterPos.Y, filterPos.Z + filterHeight),
                filterAvgR, filterAvgR, filterMaterial));
        }


        private Material CreateTransparentPlastic(Color color, byte opacity = 120)
        {
            Color transparentColor = Color.FromArgb(opacity, color.R, color.G, color.B);
            MaterialGroup group = new MaterialGroup();
            group.Children.Add(new DiffuseMaterial(new SolidColorBrush(transparentColor)));
            group.Children.Add(new SpecularMaterial(Brushes.White, 80));
            return group;
        }

        private TruncatedConeVisual3D AddCone(Point3D p1, Point3D p2, double baseRadius, double topRadius, Material mat, int thetaDiv = 40)
        {
            Vector3D direction = p2 - p1;
            double height = direction.Length;
            direction.Normalize();

            return new TruncatedConeVisual3D
            {
                Origin = p1,
                Normal = direction,
                Height = height,
                BaseRadius = baseRadius,
                TopRadius = topRadius,
                Material = mat,
                BackMaterial = mat,
                ThetaDiv = thetaDiv
            };
        }

        static double CalculateFilterAverageRadius(double pipeTotalHeight, double pipeMinRadius, double pipeMaxRadius, double filterBottomHeight, double filterHeight)
        {
            double slope = (pipeMaxRadius - pipeMinRadius) / pipeTotalHeight;
            double rBottom = pipeMinRadius + slope * filterBottomHeight;
            double rTop = pipeMinRadius + slope * (filterBottomHeight + filterHeight);
            return (rBottom + rTop) / 2;
        }
        #endregion
        #region 画水柱
        //private void CreateLiquidColumn(PlateConfig config, float preAirVol, float postAirVol, float liquidVol, float tipMaxCapacity,
        //    double offsetX, double offsetY, double offsetZ, ContainerUIElement3D container)
        //{
        //    // 1. 材质定义：液体蓝色，气封灰色
        //    Color liquidColor = Color.FromRgb(74, 144, 226);
        //    Material liquidMaterial = CreateTransparentPlastic(liquidColor, 200);
        //    Color airColor = Color.FromRgb(160, 160, 160);
        //    Material airMaterial = CreateTransparentPlastic(airColor, 120);

        //    // 2. 枪头Z轴坐标系（从顶部往下画）
        //    double passageTopZ = config.PassageHeight + config.TailConeHeight; // 液体上限（滤芯下方）
        //    double tipTipZ = 0; // 枪头尖端（最底部）
        //    double maxFillHeight = passageTopZ - tipTipZ; // 枪头可填充的总物理高度

        //    // 3. 计算比例：1μL对应多少毫米高度
        //    double heightPerUl = maxFillHeight / tipMaxCapacity;

        //    // 4. 计算各部分的物理高度
        //    double preAirHeight = preAirVol * heightPerUl;
        //    double liquidHeight = liquidVol * heightPerUl;
        //    double postAirHeight = postAirVol * heightPerUl;

        //    // 5. 开始绘制（使用 currentZ 从顶部往下移动）
        //    double currentZ = passageTopZ + offsetZ;

        //    // --- 绘制前导气封 (最顶部，灰色) ---
        //    if (preAirVol > 0.01)
        //    {
        //        double topZ = currentZ;
        //        double bottomZ = currentZ - preAirHeight;

        //        double rTop = GetRadiusAtZ(config, topZ - offsetZ);
        //        double rBottom = GetRadiusAtZ(config, bottomZ - offsetZ);

        //        container.Children.Add(AddCone(
        //            new Point3D(offsetX, offsetY, bottomZ),
        //            new Point3D(offsetX, offsetY, topZ),
        //            rBottom, rTop, airMaterial));

        //        currentZ = bottomZ; // 画完后，下移到气封底部
        //    }

        //    // --- 绘制液体柱 (中间，蓝色) ---
        //    if (liquidVol > 0.01)
        //    {
        //        double topZ = currentZ;
        //        double bottomZ = currentZ - liquidHeight;

        //        double rTop = GetRadiusAtZ(config, topZ - offsetZ);
        //        double rBottom = GetRadiusAtZ(config, bottomZ - offsetZ);

        //        container.Children.Add(AddCone(
        //            new Point3D(offsetX, offsetY, bottomZ),
        //            new Point3D(offsetX, offsetY, topZ),
        //            rBottom, rTop, liquidMaterial));

        //        currentZ = bottomZ; // 画完后，下移到液体底部
        //    }

        //    // --- 绘制后导气封 (最底部，灰色，靠近枪尖) ---
        //    if (postAirVol > 0.01)
        //    {
        //        double topZ = currentZ;
        //        // 确保不会画到枪尖外面去
        //        double bottomZ = Math.Max(currentZ - postAirHeight, tipTipZ + offsetZ);

        //        double rTop = GetRadiusAtZ(config, topZ - offsetZ);
        //        double rBottom = GetRadiusAtZ(config, bottomZ - offsetZ);

        //        container.Children.Add(AddCone(
        //            new Point3D(offsetX, offsetY, bottomZ),
        //            new Point3D(offsetX, offsetY, topZ),
        //            rBottom, rTop, airMaterial));
        //    }
        //}
        private void CreateLiquidColumn(PlateConfig config, float preAirVol, float postAirVol, float liquidVol, float tipMaxCapacity,
    double offsetX, double offsetY, double offsetZ, ContainerUIElement3D container)
        {
            // 没有液体就什么都不画
            if (liquidVol <= 0.01f) return;

            // 1. 实心液体材质（最后一个参数是透明度，255=完全实心不透明）
            Color liquidColor = Color.FromRgb(74, 144, 226);
            Material solidLiquidMaterial = CreateTransparentPlastic(liquidColor, 230);

            // 2. 枪头坐标系
            double passageTopZ = config.PassageHeight + config.TailConeHeight; // 液体最高不能超过滤芯
            double tipTipZ = 0; // 枪头最尖端
            double heightPerUl = (passageTopZ - tipTipZ) / tipMaxCapacity; // 1μL对应多少毫米

            // 3. 【核心】只计算液体的上下边界，空气完全不画
            double liquidTop = passageTopZ + offsetZ - preAirVol * heightPerUl;  // 液体顶部 = 滤芯下方 - 前气封高度
            double liquidBottom = liquidTop - liquidVol * heightPerUl;          // 液体底部 = 液体顶部 - 液体高度
            liquidBottom = Math.Max(liquidBottom, tipTipZ + offsetZ);           // 边界保护：不能低于枪尖

            // 4. 只画一个实心液体柱！没有任何重叠，彻底解决闪烁
            double rTop = GetRadiusAtZ(config, liquidTop - offsetZ);
            double rBottom = GetRadiusAtZ(config, liquidBottom - offsetZ);

            container.Children.Add(AddCone(
                new Point3D(offsetX, offsetY, liquidBottom),
                new Point3D(offsetX, offsetY, liquidTop),
                rBottom, rTop, solidLiquidMaterial));
        }
        /// <summary>
        /// 计算截锥体积（单位：μL，1cm³=1000μL）
        /// </summary>
        private double CalculateTruncatedConeVolume(double r1, double r2, double height)
        {
            // 体积公式：V = (1/3)πh(R² + Rr + r²)，单位转换为μL
            double volumeCm3 = (1.0 / 3.0) * Math.PI * height / 10 * (Math.Pow(r1 / 10, 2) + (r1 / 10) * (r2 / 10) + Math.Pow(r2 / 10, 2));
            return volumeCm3 * 1000; // 转换为μL
        }

        /// <summary>
        /// 根据Z坐标获取枪头对应位置的半径（适配锥度）
        /// </summary>
        private double GetRadiusAtZ(PlateConfig config, double z)
        {
            if (z <= 0) return config.TailEndRadiu; // 尖端以下
            if (z <= config.TailConeHeight) // 尾锥部分
            {
                double ratio = z / config.TailConeHeight;
                return config.TailEndRadiu + ratio * (config.TailConeRadius - config.TailEndRadiu);
            }
            if (z <= config.TailConeHeight + config.PassageHeight) // 通道部分
            {
                double ratio = (z - config.TailConeHeight) / config.PassageHeight;
                return config.TailConeRadius + ratio * (config.PassageTopRadius - config.TailConeRadius);
            }
            return config.PassageTopRadius; // 通道以上
        }
        #endregion

    }
}
