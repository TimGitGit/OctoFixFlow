using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OctoFixFlow
{
    /// <summary>
    /// GuideWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GuideWindow : Window
    {
        public delegate void GuideCompletedEventHandler();
        public event GuideCompletedEventHandler GuideCompleted;
        private Border _currentHoveredPlate = null;

        private const string PCR_IMAGE_PATH = "/OctoFixFlow;component/images/PCR.png";
        public GuideWindow(bool isAutoLoad)
        {
            InitializeComponent();
            InitPlateModuleMap();
            if (isAutoLoad)
            {
                AutoLoadConfiguredDevices();
            }
        }
        private void AutoLoadConfiguredDevices()
        {
            try
            {
                //// 1. 自动显示第一个移液器容器，并启用相关模块容器（抓手/PCR）
                //pipetteContainer.Visibility = Visibility.Visible;
                //gripperContainer.Visibility = Visibility.Visible;
                //pcrContainer.Visibility = Visibility.Visible;

                //// 2. 启用后续步骤按钮
                //GuideDeckLayoutTitle.IsEnabled = true;
                //GuideExperimentProtocolTitle.IsEnabled = true;
                //GuideConfirmButton.IsEnabled = true;

                //// 3. 自动加载移液器1默认配置（单通道200μL，可根据实际配置调整）
                //cmbPipette1.SelectedIndex = 0; // 单通道
                //cmbPipetteVolume.SelectedIndex = 0; // 200μL

                //// 4. 自动启用抓手（可选：根据实际配置调整）
                //AppGlobalConfig.Instance.IsGripperEnabled = true;
                //btnToggleGripper.Content = new Image
                //{
                //    Source = new BitmapImage(new Uri("/OctoFixFlow;component/images/gou.png", UriKind.Relative))
                //};

                //// 5. 自动启用PCR模块（可选：根据实际配置调整）
                //AppGlobalConfig.Instance.IsPCREnabled = true;
                //EnablePCR_Click(null, null); // 复用原有PCR启用逻辑

                //// 6. 自动填充实验协议默认信息（可选）
                //guideProtocolNameTextBox.Text = "默认实验协议";
                //guideProtocolDescriptionTextBox.Text = "自动加载的仪器默认配置";
                //guideProtocolAuthorTextBox.Text = "系统自动配置";

                //// 可扩展：从配置文件/仪器读取已保存的设备配置，覆盖上述默认值
                //// 示例：var savedConfig = AppGlobalConfig.Instance.LoadSavedDeviceConfig();
                ////       cmbPipette1.SelectedIndex = savedConfig.Pipette1Type;
                ////       cmbPipetteVolume.SelectedIndex = savedConfig.Pipette1Volume;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"自动加载设备配置失败：{ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void InitPlateModuleMap()
        {
            var newModule = new ModuleDatas
            {
                Name = "Waste bin",
                Type = 8,
                PlatePosition = "12",
                PipetteVolume = 0,
                ModuleImage = "/OctoFixFlow;component/images/Trash.png"
            };
            AppGlobalConfig.Instance.AddOrUpdateModule("12", newModule);
        }

        private void GuideConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (guideProtocolNameTextBox.Text == "")
                AppGlobalConfig.Instance.GuideProtocolName = "untitled";
            else
                AppGlobalConfig.Instance.GuideProtocolName = guideProtocolNameTextBox.Text;
            if (guideProtocolAuthorTextBox.Text == "")
                AppGlobalConfig.Instance.GuideProtocolAuthor = "User";
            else
                AppGlobalConfig.Instance.GuideProtocolAuthor = guideProtocolNameTextBox.Text;
            AppGlobalConfig.Instance.GuideProtocolDescription = guideProtocolDescriptionTextBox.Text;

            AppGlobalConfig.Instance.RenameModulesByType();
            //判断一下移液器
            //移液器1
            int pipetteType1 = 0;
            if (cmbPipette1.SelectedIndex == 1)
            {
                pipetteType1 = 1;
            }
            else if (cmbPipette1.SelectedIndex == 2)
            {
                pipetteType1 = 2;
            }
            int pipetteVolume1 = 200;
            if (cmbPipetteVolume.SelectedIndex == 1)
            {
                pipetteVolume1 = 1000;
            }
            var pipetteModule = new ModuleDatas
            {
                Name = "pipette_1",
                Type = pipetteType1,
                PlatePosition = "13",
                PipetteVolume = pipetteVolume1,
                ModuleImage = ""
            };
            AppGlobalConfig.Instance.AddOrUpdateModule("13", pipetteModule);
            if (pipetteContainer2.Visibility == Visibility.Visible)
            {
                //移液器2
                int pipetteType2 = 0;
                if (cmbPipette2.SelectedIndex == 1)
                {
                    pipetteType2 = 1;
                }
                int pipetteVolume2 = 200;
                if (cmbPipetteVolume2.SelectedIndex == 1)
                {
                    pipetteVolume2 = 1000;
                }
                var pipetteModule2 = new ModuleDatas
                {
                    Name = "pipette_2",
                    Type = pipetteType2,
                    PlatePosition = "14",
                    PipetteVolume = pipetteVolume2,
                    ModuleImage = ""
                };
                AppGlobalConfig.Instance.AddOrUpdateModule("14", pipetteModule2);
            }

            // 触发完成事件
            GuideCompleted?.Invoke();
            // 关闭引导窗口
            this.Close();
        }
        //添加移液器
        private void AddPipette_Click(object sender, RoutedEventArgs e)
        {
            if (pipetteContainer.Visibility == Visibility.Collapsed)
            {
                pipetteContainer.Visibility = Visibility.Visible;
                gripperContainer.Visibility = Visibility.Visible;
                pcrContainer.Visibility = Visibility.Visible;
                //trashContainer.Visibility = Visibility.Visible;
                GuideDeckLayoutTitle.IsEnabled = true;
                GuideExperimentProtocolTitle.IsEnabled = true;
                GuideConfirmButton.IsEnabled = true;

            }
            else if (pipetteContainer2.Visibility == Visibility.Collapsed)
            {

                pipetteContainer2.Visibility = Visibility.Visible;
            }
        }
        private void cmbPipette1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool is96ChannelSelected = cmbPipette1.SelectedItem is ComboBoxItem item &&
                                       item.Content.ToString() == ResourceHelper.Instance.SettingManualNineSixChannel;
            btnAddPipette.IsEnabled = !is96ChannelSelected;


            if (is96ChannelSelected)
            {

                cmbPipette2.SelectedIndex = 0;
                cmbPipetteVolume2.SelectedIndex = 0;
                pipetteContainer2.Visibility = Visibility.Collapsed;
            }

        }
        private void RemovePipette1_Click(object sender, RoutedEventArgs e)
        {
            if (pipetteContainer2.Visibility == Visibility.Visible)
            {
                cmbPipette1.SelectedIndex = cmbPipette2.SelectedIndex;
                cmbPipetteVolume.SelectedIndex = cmbPipetteVolume2.SelectedIndex;
                cmbPipette2.SelectedIndex = 0;
                cmbPipetteVolume2.SelectedIndex = 0;
                pipetteContainer2.Visibility = Visibility.Collapsed;
            }
            else
            {
                cmbPipette1.SelectedIndex = 0;
                cmbPipetteVolume.SelectedIndex = 0;
                pipetteContainer.Visibility = Visibility.Collapsed;
                gripperContainer.Visibility = Visibility.Collapsed;
                pcrContainer.Visibility = Visibility.Collapsed;
                //trashContainer.Visibility = Visibility.Collapsed;
                GuideConfirmButton.IsEnabled = false;
                AppGlobalConfig.Instance.IsGripperEnabled = false;
                AppGlobalConfig.Instance.IsPCREnabled = false;
                AppGlobalConfig.Instance.IsTrashEnabled = false;
                btnToggleGripper.Content = "❌";
                btnTogglePCR.Content = "❌";
                //btnToggleTrash.Content = "❌";
                GuideDeckLayoutTitle.IsEnabled = false;
                GuideExperimentProtocolTitle.IsEnabled = false;
                string[] platePositions = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11" };
                foreach (var pos in platePositions)
                {
                    ClearPlateContent(pos);

                }
            }

        }

        private void RemovePipette2_Click(object sender, RoutedEventArgs e)
        {
            cmbPipette2.SelectedIndex = 0;
            cmbPipetteVolume2.SelectedIndex = 0;
            pipetteContainer2.Visibility = Visibility.Collapsed;
        }

        private void EnableGripper_Click(object sender, RoutedEventArgs e)
        {
            AppGlobalConfig.Instance.IsGripperEnabled = !AppGlobalConfig.Instance.IsGripperEnabled;
            string plateId = "15";

            if (AppGlobalConfig.Instance.IsGripperEnabled)
            {
                btnToggleGripper.Content = new Image
                {
                    Source = new BitmapImage(new Uri("/OctoFixFlow;component/images/gou.png", UriKind.Relative))
                };
                var gripperModule = new ModuleDatas
                {
                    Name = "gripper_1",
                    Type = 3,
                    PlatePosition = plateId,
                    PipetteVolume = 0,
                };
                AppGlobalConfig.Instance.AddOrUpdateModule(plateId, gripperModule);
            }
            else
            {
                btnToggleGripper.Content = "❌";
                var newModule = new ModuleDatas
                {
                    Name = "",
                    Type = -1,
                    PlatePosition = plateId,
                    PipetteVolume = 0,
                    ModuleImage = ""
                };
                AppGlobalConfig.Instance.AddOrUpdateModule(plateId, newModule);
            }
        }

        private void EnablePCR_Click(object sender, RoutedEventArgs e)
        {
            AppGlobalConfig.Instance.IsPCREnabled = !AppGlobalConfig.Instance.IsPCREnabled;
            string plateId = "10";
            Border p10Border = FindName("PlateBorder10") as Border;
            Grid p10Grid = FindName($"PlateGrid{plateId}") as Grid;
            if (AppGlobalConfig.Instance.IsPCREnabled)
            {
                if (p10Grid != null)
                {
                    p10Grid.Children.Clear();
                    var pcrImage = new Image
                    {
                        Source = new BitmapImage(new Uri(PCR_IMAGE_PATH, UriKind.Relative)),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Stretch = Stretch.UniformToFill,
                        Margin = new Thickness(2)
                    };
                    p10Grid.Children.Add(pcrImage);
                }

                var pcrModule = new ModuleDatas
                {
                    Name = "PCR",
                    Type = 4,
                    PlatePosition = plateId,
                    PipetteVolume = 0,
                    ModuleImage = "/OctoFixFlow;component/images/PCR.png"
                };
                AppGlobalConfig.Instance.AddOrUpdateModule(plateId, pcrModule);

                if (p10Border != null)
                {
                    p10Border.AllowDrop = false;
                    p10Border.Focusable = false;

                }

                //UpdatePlateToolTip(plateId, "PCR");
                btnTogglePCR.Content = new Image
                {
                    Source = new BitmapImage(new Uri("/OctoFixFlow;component/images/gou.png", UriKind.Relative))
                };
            }
            else
            {
                ClearPlateContent(plateId);

                var newModule = new ModuleDatas
                {
                    Name = "",
                    Type = -1,
                    PlatePosition = plateId,
                    PipetteVolume = 0,
                    ModuleImage = ""
                };
                AppGlobalConfig.Instance.AddOrUpdateModule(plateId, newModule);
                if (p10Border != null)
                {
                    p10Border.AllowDrop = true;
                    p10Border.Focusable = true;

                }
                btnTogglePCR.Content = "❌";
            }
        }


        private void GuideBasic_Click(object sender, RoutedEventArgs e)
        {
            mainGuideTable.SelectedIndex = 0;
        }

        private void GuideDeckLayout_Click(object sender, RoutedEventArgs e)
        {
            mainGuideTable.SelectedIndex = 1;
        }

        private void GuideExperimentProtocol_Click(object sender, RoutedEventArgs e)
        {
            mainGuideTable.SelectedIndex = 2;
        }
        #region 板位设置
        private void ModuleItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border moduleBorder) return;

            // 解析模块Type（直接从Tag获取，对应ModuleDatas.Type）
            if (!int.TryParse(moduleBorder.Tag.ToString(), out int moduleType))
                return;

            // 提取模块名称和图片路径（从UI中获取）
            string moduleName = "";
            string imagePath = "";
            if (moduleBorder.Child is StackPanel stackPanel)
            {
                if (stackPanel.Children[1] is TextBlock txt) moduleName = txt.Text;
                if (stackPanel.Children[0] is Image img) imagePath = img.Source.ToString();
            }

            // 打包拖拽数据（直接包含ModuleDatas的核心字段）
            var dragData = new DataObject();
            dragData.SetData("ModuleType", moduleType); // 对应ModuleDatas.Type
            dragData.SetData("ModuleName", moduleName); // 对应ModuleDatas.Name
            dragData.SetData("ImagePath", imagePath);   // 用于板位显示图片
            dragData.SetData("PipetteVolume", 0);       // 非移液器，设为0

            // 启动拖拽
            DragDrop.DoDragDrop(moduleBorder, dragData, DragDropEffects.Copy);

        }
        private void PlateSlot_Drop(object sender, DragEventArgs e)
        {
            if (sender is not Border plateBorder || !e.Data.GetDataPresent("ModuleType"))
                return;

            // 获取板位编号（P1-P11，跳过P12）
            string platePosition = plateBorder.Tag.ToString();
            if (platePosition == "12") return;

            // 解析拖拽数据
            int moduleType = (int)e.Data.GetData("ModuleType");
            string moduleName = e.Data.GetData("ModuleName").ToString();
            int pipetteVolume = (int)e.Data.GetData("PipetteVolume");
            string imagePath = e.Data.GetData("ImagePath").ToString();

            // 创建ModuleDatas实例（复用你的类）
            var moduleData = new ModuleDatas
            {
                Name = moduleName,
                Type = moduleType,
                PlatePosition = platePosition,
                PipetteVolume = pipetteVolume,
                ModuleImage = imagePath
            };

            AppGlobalConfig.Instance.AddOrUpdateModule(platePosition, moduleData);


            // 更新板位显示（显示模块图片）
            UpdatePlateDisplay(plateBorder, imagePath);
        }
        private void PlateSlot_MouseEnter(object sender, MouseEventArgs e)
        {
            _currentHoveredPlate = sender as Border;
            _currentHoveredPlate.Focus();
        }

        private void PlateSlot_MouseLeave(object sender, MouseEventArgs e)
        {
            _currentHoveredPlate = null;
        }

        private void PlateSlot_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Delete || e.Key == Key.Back) && _currentHoveredPlate != null)
            {
                // 获取板位ID（如"P1"中的"1"）
                string plateId = _currentHoveredPlate.Tag.ToString();
                if (plateId == "12") return;
                if (plateId == "10" && AppGlobalConfig.Instance.IsPCREnabled)
                {
                    e.Handled = true;
                    return;
                }
                ClearPlateContent(plateId);
                //var newModule = new ModuleDatas
                //{
                //    Name = "",
                //    Type = -1,
                //    PlatePosition = plateId, // 用传入的 plateId 赋值
                //    PipetteVolume = 0,
                //    ModuleImage = ""
                //};

                //AppGlobalConfig.Instance.AddOrUpdateModule(plateId, newModule);
                AppGlobalConfig.Instance.DeleteModule(plateId);


                e.Handled = true;
            }
        }
        /// <summary>
        /// 更新板位为模块图片
        /// </summary>
        private void UpdatePlateDisplay(Border plateBorder, string imagePath)
        {
            if (plateBorder.Child is not Grid plateGrid) return;

            plateGrid.Children.Clear();

            var moduleImage = new Image
            {
                Source = new ImageSourceConverter().ConvertFromString(imagePath) as ImageSource,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(2)
            };

            plateGrid.Children.Add(moduleImage);
        }

        /// <summary>
        /// 恢复板位默认显示（文字+P编号）
        /// </summary>
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
        #endregion


    }
}
