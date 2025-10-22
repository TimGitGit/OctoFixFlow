using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
        // 液体相关字段与属性（新增）
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

            this.Loaded += async (s, e) =>
            {
                await loadSqlData();
            };
        }
        private async Task loadSqlData()
        {
            List<string> consList = await databaseService.GetAllConsumableNamesAsync();
            foreach (string cons in consList) {
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
                bool isUpdated = await databaseService.UpdateConsumableNameAsync(oldConsName,consNew.name);
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
                ConsSettings consSQL= await databaseService.GetConsumableByNameAsync(itemName);
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
                _mainWidget.ShowNotification("板位信息获取", NotificationControl.NotificationType.Info);
                if (plateId == "3")
                {
                LoadPlateCoordinates("magnetic_1");

                }else if (plateId == "9")
                {
                    LoadPlateCoordinates("shaker_1");
                }
                else
                {
                    LoadPlateCoordinates("p" + plateId);
                }
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
        private async void LoadPlateCoordinates(string plateId)
        {
            var coordinates =  await _mainWidget.GetSQLDataAsync(plateId);
            if (coordinates.Errcode == 0)
            {
                txtPlateX.Text = coordinates.X.ToString("F2");
                txtPlateY.Text = coordinates.Y.ToString("F2");
                txtPlateZ.Text = coordinates.Z.ToString("F2");
                _mainWidget.ShowNotification("板位信息获取成功", NotificationControl.NotificationType.Info);
            }
            else
            {
                _mainWidget.ShowNotification("板位信息获取失败", NotificationControl.NotificationType.Warn);
            }
        }
        // 移动到指定板位坐标
        private async void MoveToPlate_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtPlateX.Text, out float x) &&
                float.TryParse(txtPlateY.Text, out float y)
               )
            {
                // 发送移动指令到硬件（实际需调用GRPC接口）
                _mainWidget.ShowNotification($"正在移动到坐标：X={x:F2}, Y={y:F2}", NotificationControl.NotificationType.Info);
                // 合并 X 和 Y 电机的动作到同一个列表中
                var allMotorActions = new List<MotorActionParams>
    {
        // X 电机参数
        new MotorActionParams
        {
            MotorId = 0,
            ActionType = 0,
            Target = x,
            Speed = 300.0f,
            Acc = 500.0f,
            Dcc = 500.0f
        },
        // Y 电机参数（与 X 电机一起发送）
        new MotorActionParams
        {
            MotorId = 1,
            ActionType = 0,
            Target = y,
            Speed = 300.0f,
            Acc = 500.0f,
            Dcc = 500.0f
        }
    };

                // 单次调用 gRPC 接口，同时发送 X 和 Y 电机的动作
               var result =  await _mainWidget.MotorActionAsync(allMotorActions);
                if(result == 0)
                    _mainWidget.ShowNotification("设备移动成功", NotificationControl.NotificationType.Info);
            }
            else
            {
                _mainWidget.ShowNotification("坐标格式错误，请输入有效的数字", NotificationControl.NotificationType.Error);
            }
        }
        //移动Z轴
        private async void DownToPlate_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtPlateZ.Text, out float z))
            {
                _mainWidget.ShowNotification("设备移动Z轴", NotificationControl.NotificationType.Info);
                var motorActions = new List<MotorActionParams>();
                motorActions.Add(new MotorActionParams
                {
                    MotorId = 2,
                    ActionType = 0,
                    Target = z,
                    Speed = 30.0f,
                    Acc = 100.0f,
                    Dcc = 100.0f
                });
               var result =  await _mainWidget.MotorActionAsync(motorActions);
                if (result == 0)
                    _mainWidget.ShowNotification("设备移动成功", NotificationControl.NotificationType.Info);
            }
            else
            {
                _mainWidget.ShowNotification("坐标格式错误，请输入有效的数字", NotificationControl.NotificationType.Error);
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
                _mainWidget.ShowNotification("请先选择板位", NotificationControl.NotificationType.Warn);
                return;
            }
            string nowPlate = selectedPlate;
            if (selectedPlate == "3")
            {
                nowPlate = "magnetic_1";

            }
            else if (selectedPlate == "9")
            {
                nowPlate = "shaker_1";
            }
            else
            {
                nowPlate = "p" + selectedPlate;
            }

            if (float.TryParse(txtPlateX.Text, out float x) &&
                float.TryParse(txtPlateY.Text, out float y) &&
                float.TryParse(txtPlateZ.Text, out float z))
            {
                // 保存坐标到存储（数据库或配置文件）
                //_plateCoordinateMap[selectedPlate] = (x, y, z);
               var coordinates =  await _mainWidget.SetSQLDataAsync(nowPlate,x,y,z);
                if (coordinates.Errcode == 0)
                {

                    _mainWidget.ShowNotification($"板位 {selectedPlate} 坐标已保存", NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification("板位信息保存失败", NotificationControl.NotificationType.Warn);
                }
            }
            else
            {
                _mainWidget.ShowNotification("坐标格式错误，保存失败", NotificationControl.NotificationType.Error);
            }
        }
        //复位X
        private async void btnResetX_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification("设备X轴复位", NotificationControl.NotificationType.Info);
            var motorActions = new List<MotorActionParams>();
            motorActions.Add(new MotorActionParams
            {
                MotorId = 0,
                ActionType = 4,
                Target = 0,
                Speed = 300.0f,
                Acc = 500.0f,
                Dcc = 500.0f
            });
           var result =  await _mainWidget.MotorActionAsync(motorActions);
            if (result == 0)
                _mainWidget.ShowNotification("设备复位成功", NotificationControl.NotificationType.Info);
        }
        //复位Y
        private async void btnResetY_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification("设备Y轴复位", NotificationControl.NotificationType.Info);
            var motorActions = new List<MotorActionParams>();
            motorActions.Add(new MotorActionParams
            {
                MotorId = 1,
                ActionType = 4,
                Target = 0,
                Speed = 300.0f,
                Acc = 500.0f,
                Dcc = 500.0f
            });
            var result = await _mainWidget.MotorActionAsync(motorActions);
            if (result == 0)
                _mainWidget.ShowNotification("设备复位成功", NotificationControl.NotificationType.Info);
        }
        //复位Z
        private async void btnResetZ_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification("设备Z轴复位", NotificationControl.NotificationType.Info);
            var motorActions = new List<MotorActionParams>();
            motorActions.Add(new MotorActionParams
            {
                MotorId = 2,
                ActionType = 4,
                Target = 0,
                Speed = 300.0f,
                Acc = 500.0f,
                Dcc = 500.0f
            });
            var result = await _mainWidget.MotorActionAsync(motorActions);
            if (result == 0)
                _mainWidget.ShowNotification("设备复位成功", NotificationControl.NotificationType.Info);
        }
        // 微调按钮点击事件
        //X减少
        private async void FineXClose_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtFineAdjust.Text, out float adjustValue) &&
               float.TryParse(txtPlateX.Text, out float currentX))
            {
                var motorActions = new List<MotorActionParams>();
                motorActions.Add(new MotorActionParams
                {
                    MotorId = 0,
                    ActionType = 1,
                    Target = adjustValue * -1,
                    Speed = 300.0f,
                    Acc = 300.0f,
                    Dcc = 300.0f
                });
                var coordinates = await _mainWidget.MotorActionAsync(motorActions);
                if (coordinates == 0)
                {
                    float newX = currentX - adjustValue;
                    txtPlateX.Text = newX.ToString("F2"); 
                   _mainWidget.ShowNotification("设备移动成功", NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification("设备移动失败", NotificationControl.NotificationType.Warn);
                }
            }
            else
            {
                _mainWidget.ShowNotification("坐标格式错误，保存失败", NotificationControl.NotificationType.Error);
            }
        }

        private async void FineXAdd_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtFineAdjust.Text, out float adjustValue) &&
               float.TryParse(txtPlateX.Text, out float currentX))
            {
                var motorActions = new List<MotorActionParams>();
                motorActions.Add(new MotorActionParams
                {
                    MotorId = 0,
                    ActionType = 1,
                    Target = adjustValue,
                    Speed = 300.0f,
                    Acc = 300.0f,
                    Dcc = 300.0f
                });
                var coordinates = await _mainWidget.MotorActionAsync(motorActions);
                if (coordinates == 0)
                {
                    float newX = currentX + adjustValue;
                    txtPlateX.Text = newX.ToString("F2");
                    _mainWidget.ShowNotification("设备移动成功", NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification("设备移动失败", NotificationControl.NotificationType.Warn);
                }
            }
            else
            {
                _mainWidget.ShowNotification("坐标格式错误，保存失败", NotificationControl.NotificationType.Error);
            }
        }

        private async void FineYAdd_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtFineAdjust.Text, out float adjustValue) &&
               float.TryParse(txtPlateY.Text, out float currentY))
            {
                var motorActions = new List<MotorActionParams>();
                motorActions.Add(new MotorActionParams
                {
                    MotorId = 1,
                    ActionType = 1,
                    Target = adjustValue * -1,
                    Speed = 300.0f,
                    Acc = 300.0f,
                    Dcc = 300.0f
                });
                var coordinates = await _mainWidget.MotorActionAsync(motorActions);
                if (coordinates == 0)
                {
                    float newY = currentY - adjustValue;
                    txtPlateY.Text = newY.ToString("F2");
                    _mainWidget.ShowNotification("设备移动成功", NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification("设备移动失败", NotificationControl.NotificationType.Warn);
                }
            }
            else
            {
                _mainWidget.ShowNotification("坐标格式错误，保存失败", NotificationControl.NotificationType.Error);
            }
        }

        private async void FineYClose_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtFineAdjust.Text, out float adjustValue) &&
               float.TryParse(txtPlateY.Text, out float currentY))
            {
                var motorActions = new List<MotorActionParams>();
                motorActions.Add(new MotorActionParams
                {
                    MotorId = 1,
                    ActionType = 1,
                    Target = adjustValue,
                    Speed = 300.0f,
                    Acc = 300.0f,
                    Dcc = 300.0f
                });
                var coordinates = await _mainWidget.MotorActionAsync(motorActions);
                if (coordinates == 0)
                {
                    float newY = currentY + adjustValue;
                    txtPlateY.Text = newY.ToString("F2");
                    _mainWidget.ShowNotification("设备移动成功", NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification("设备移动失败", NotificationControl.NotificationType.Warn);
                }
            }
            else
            {
                _mainWidget.ShowNotification("坐标格式错误，保存失败", NotificationControl.NotificationType.Error);
            }
        }

        private async void FineZAdd_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtFineAdjust.Text, out float adjustValue) &&
               float.TryParse(txtPlateZ.Text, out float currentZ))
            {
                var motorActions = new List<MotorActionParams>();
                motorActions.Add(new MotorActionParams
                {
                    MotorId = 2,
                    ActionType = 1,
                    Target = adjustValue * -1,
                    Speed = 120.0f,
                    Acc = 300.0f,
                    Dcc = 300.0f
                });
                var coordinates = await _mainWidget.MotorActionAsync(motorActions);
                if (coordinates == 0)
                {
                    float newZ = currentZ - adjustValue;
                    txtPlateZ.Text = newZ.ToString("F2");
                    _mainWidget.ShowNotification("设备移动成功", NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification("设备移动失败", NotificationControl.NotificationType.Warn);
                }
            }
            else
            {
                _mainWidget.ShowNotification("坐标格式错误，保存失败", NotificationControl.NotificationType.Error);
            }
        }

        private async void FineZClose_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtFineAdjust.Text, out float adjustValue) &&
               float.TryParse(txtPlateZ.Text, out float currentZ))
            {
                var motorActions = new List<MotorActionParams>();
                motorActions.Add(new MotorActionParams
                {
                    MotorId = 2,
                    ActionType = 1,
                    Target = adjustValue,
                    Speed = 120.0f,
                    Acc = 300.0f,
                    Dcc = 300.0f
                });
                var coordinates = await _mainWidget.MotorActionAsync(motorActions);
                if (coordinates == 0)
                {
                    float newZ = currentZ + adjustValue;
                    txtPlateZ.Text = newZ.ToString("F2");
                    _mainWidget.ShowNotification("设备移动成功", NotificationControl.NotificationType.Info);
                }
                else
                {
                    _mainWidget.ShowNotification("设备移动失败", NotificationControl.NotificationType.Warn);
                }
            }
            else
            {
                _mainWidget.ShowNotification("坐标格式错误，保存失败", NotificationControl.NotificationType.Error);
            }
        }
        // 移液器控制：吸液
        private async void Aspirate_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtAspirateVol.Text, out int vol) &&
                int.TryParse(txtAspirateSpeed.Text, out int speed))
            {
                _mainWidget.ShowNotification($"执行吸液：{vol}μl，速度：{speed}μl/s", NotificationControl.NotificationType.Info);
                // _pipetteClient.Aspirate(vol, speed); // 示例GRPC调用
                var coordinates = await _mainWidget.AspiratePipeAsync(vol, speed);
                if(coordinates == 0)
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
                var coordinates = await _mainWidget.DispensePipeAsync(vol, speed);
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
            var coordinates = await _mainWidget.BreakPipeAsync();
            if (coordinates == 0)
                _mainWidget.ShowNotification("退头成功", NotificationControl.NotificationType.Info);
        }

        // 移液器控制：复位
        private async void ResetPipette_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification("移液器复位中", NotificationControl.NotificationType.Info);
            var coordinates = await _mainWidget.ResetPipeAsync();
            if (coordinates == 0)
                _mainWidget.ShowNotification("复位成功", NotificationControl.NotificationType.Info);
        }
        // 移液器控制：定标
        private void CalibratePipette_Click(object sender, RoutedEventArgs e)
        {
            _mainWidget.ShowNotification("开始移液器定标", NotificationControl.NotificationType.Info);
            // _pipetteClient.StartCalibration(); // 示例GRPC调用
        }

        //获得定标
        private async void GetCalibration_Click(object sender, RoutedEventArgs e)
        {
            var calibrationParams = await _mainWidget.GetPipeCalibrationAsync("pipette_1");

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
                    PipeName = "pipette_1", // 管道名称固定
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


    }
}
