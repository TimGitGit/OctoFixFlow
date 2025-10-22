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

namespace OctoFixFlow
{
    /// <summary>
    /// QuickFlowWindow.xaml 的交互逻辑
    /// </summary>
    public partial class QuickFlowWindow : Window
    {
        private readonly MainWidget _mainWidget;
        // 父窗口的流程步骤集合
        public ObservableCollection<FlowStep> ParentFlowSteps { get; set; }
        // 父窗口的耗材集合
        public Dictionary<string, ConsumableItem> ParentPlateConsumableMap { get; set; }
        // 父窗口的液体集合
        public ObservableCollection<LiquidSettings> Liquids { get; set; }

        public QuickFlowWindow(MainWidget mainWidget,ObservableCollection<FlowStep> flowSteps, Dictionary<string, ConsumableItem> plateconsumablemap, ObservableCollection<LiquidSettings> liquids)
        {
            InitializeComponent();
            _mainWidget = mainWidget;
            this.DataContext = this;
            ParentFlowSteps = flowSteps;
            ParentPlateConsumableMap = plateconsumablemap;
            Liquids = liquids;
            LoadWidgetSample();
        }
        private void LoadWidgetSample()
        {
            // 确保耗材映射不为空
            if (ParentPlateConsumableMap == null || ParentPlateConsumableMap.Count == 0)
                return;
            var aspDispItems = new List<string>(); //type为0和1的耗材
            var tiponItems = new List<string>(); //type为2的耗材
            var tipoffItems = new List<string>(); //type为2和3的耗材

            // 遍历所有板位耗材
            foreach (var kvp in ParentPlateConsumableMap)
            {
                string position = kvp.Key; // 板位名称（如P1, P2...）
                ConsumableItem consumable = kvp.Value; // 耗材项

                // 确保耗材设置不为空
                if (consumable?.Settings == null)
                    continue;

                int type = consumable.Settings.type; // 获取耗材类型
                string displayText = $"P{position}"; // 显示文本：Px + 耗材名称

                // 根据类型添加到对应的下拉框列表
                if (type == 0 || type == 1)
                {
                    aspDispItems.Add(displayText);
                }else if (type == 2)
                {
                    tiponItems.Add(displayText);
                    tipoffItems.Add(displayText);

                }
                else if (type == 3)
                {
                    tipoffItems.Add(displayText);
                }
            }

            // 绑定到下拉框
            AspiratePositionComboBox.ItemsSource = aspDispItems;
            DispensePositionComboBox.ItemsSource = aspDispItems;

            TipPickPositionComboBox.ItemsSource = tiponItems;
            TipEjectPositionComboBox.ItemsSource = tipoffItems;

            // 默认选择第一个项（如果有数据）
            if (aspDispItems.Count > 0)
                AspiratePositionComboBox.SelectedIndex = 0;
            if (tipoffItems.Count > 0)
                DispensePositionComboBox.SelectedIndex = 0;
        }
        private void SampleCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 验证样本数量输入，确保是1-96之间的整数
            if (int.TryParse(SampleCountTextBox.Text, out int count))
            {
                if (count < 1)
                    SampleCountTextBox.Text = "1";
                else if (count > 96)
                    SampleCountTextBox.Text = "96";
            }
            else if (!string.IsNullOrEmpty(SampleCountTextBox.Text))
            {
                SampleCountTextBox.Text = "1";
            }
        }
        //生成
        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. 验证输入参数
            if (!ValidateInput())
                return;
            // 2. 获取用户输入参数
            int sampleCount = int.Parse(SampleCountTextBox.Text); // 样本数量
            int sampleTimes = (sampleCount + 7) / 8;
            string tipPickPos = TipPickPositionComboBox.SelectedItem?.ToString(); // 取头位置
            bool isChangeTipOn = ChangeTipOnCheckBox.IsChecked ?? false; // 是否取头换头
            int tipOnWell = int.Parse(TipOnWellTextBox.Text); // 取头位置

            string aspiratePos = AspiratePositionComboBox.SelectedItem?.ToString(); // 吸液位置
            string dispensePos = DispensePositionComboBox.SelectedItem?.ToString(); // 注液位置

            bool isDisMixTip = isMixCheckBox.IsChecked ?? false; // 是否注液混吸
            int disMixValue = int.Parse(DispenseMixValueTextBox.Text); //注液混吸容量
            int disMixCount = int.Parse(DispenseMixTextBox.Text); //注液混吸次数

            bool isDisRinseTip = isRinseCheckBox.IsChecked ?? false; // 是否注液润洗
            int disRinseValue = int.Parse(DispenseRinseValueTextBox.Text); //注液润洗容量
            int disRinseTimes = int.Parse(DispenseRinseTimeTextBox.Text); //注液润洗延时
            if (!isDisMixTip)
            {
                disMixValue = 0;
                disMixCount = 0;

            }
            if (!isDisRinseTip)
            {
                disRinseValue = 0;
                disRinseTimes = 0;

            }
            string tipEjectPos = TipEjectPositionComboBox.SelectedItem?.ToString(); // 退头位置
            int aspirateVolume = int.Parse(AspirateVolumeTextBox.Text); // 吸液体积
            bool isChangeTip = ChangeTipCheckBox.IsChecked ?? false; // 是否换头
            bool isChangeAis = ChangeAisCheckBox.IsChecked ?? false; // 是否固定
            int AisWell = int.Parse(AspirateWellTetBox.Text); // 样本吸液位置

            LiquidSettings selectedLiquid = LiquidComboBox.SelectedItem as LiquidSettings; // 选中液体

            string tipPickPlateId = ExtractPlateId(tipPickPos);
            string aspiratePlateId = ExtractPlateId(aspiratePos);
            string dispensePlateId = ExtractPlateId(dispensePos);
            string tipEjectPlateId = ExtractPlateId(tipEjectPos);

            // 4. 确定步骤插入位置（在结束步骤前）
            int endStepIndex = ParentFlowSteps.Count - 1;
            int insertIndex = endStepIndex;

            // 5. 获取当前最大步骤索引（用于新步骤编号）
            //int currentStepIndex = ParentFlowSteps.Max(s => s.Index) + 1;
            int currentStepIndex = ParentFlowSteps.Any() ? ParentFlowSteps.Max(s => s.Index) + 1 : 1;

            // 6. 初始取头步骤（循环外的首次取头）
            if (isChangeTipOn)
            {
                insertIndex = AddStep(
                    ParentFlowSteps,
                    insertIndex,
                    ref currentStepIndex,
                    "TipOn",
                    tipPickPlateId,
                    tipOnWell, 
                    aspirateVolume,
                    selectedLiquid,
                                            false,
                        0,
                        0, 0,
                    0
                );
            }
            else//
            {
                insertIndex = AddStep(
                    ParentFlowSteps,
                    insertIndex,
                    ref currentStepIndex,
                    "TipOn",
                    tipPickPlateId,
                    1, // 默认第一列
                    aspirateVolume,
                    selectedLiquid, 
                    false,
                        0,
                        0, 0,
                    0
                );

            }

            for (int i = 0;i< sampleTimes;i++)
            {
                int currentCol = i + 1; // 孔位列号（从1开始递增）
                //string wellPosition = $"列：{currentCol}"; // 孔位文本（如"列：1"、"列：2"）

                // 7.1 吸液步骤
                if (isChangeAis)
                {
                    insertIndex = AddStep(
                        ParentFlowSteps,
                        insertIndex,
                        ref currentStepIndex,
                        "Aspirate",
                        aspiratePlateId,
                        AisWell,
                        aspirateVolume,
                        selectedLiquid,
                         false,
                        0,
                        0, 0,
                    0
                    );

                }
                else
                {
                    insertIndex = AddStep(
                        ParentFlowSteps,
                        insertIndex,
                        ref currentStepIndex,
                        "Aspirate",
                        aspiratePlateId,
                        currentCol,
                        aspirateVolume,
                        selectedLiquid,
                        false,
                        0,
                        0, 0,
                    0
                    );
                }


                // 7.2 注液步骤
                insertIndex = AddStep(
                    ParentFlowSteps,
                    insertIndex,
                    ref currentStepIndex,
                    "Dispense",
                    dispensePlateId,
                    currentCol,
                    aspirateVolume,
                    selectedLiquid,
                    isDisMixTip,
                    disMixCount,
                    disMixValue,
                    disRinseValue,
                    disRinseTimes

                );

                // 7.3 换头逻辑（最后一批样本后不执行取头）
                if (isChangeTip && i < sampleTimes - 1)
                {
                    // 退头步骤：P12固定第1列，其他随当前批次列递增
                    int ejectCol = tipEjectPlateId == "12" ? 1 : currentCol;
                    insertIndex = AddStep(
                        ParentFlowSteps,
                        insertIndex,
                        ref currentStepIndex,
                        "TipOff",
                        tipEjectPlateId,
                        ejectCol,
                        aspirateVolume,
                        selectedLiquid, 
                        false,
                    0,
                    0, 0,
                    0
                    );

                    // 新取头步骤：下一批次列（当前列+1）
                    insertIndex = AddStep(
                        ParentFlowSteps,
                        insertIndex,
                        ref currentStepIndex,
                        "TipOn",
                        tipPickPlateId,
                        currentCol + 1, // 取头列随批次递增
                        aspirateVolume,
                        selectedLiquid, 
                        false,
                    0,
                    0, 0,
                    0
                    );
                }
            }
            if (isChangeTip)//是否换头
            {
                // 8. 最终退头步骤（所有批次处理完成后）
                int finalEjectCol = tipEjectPlateId == "1" ? 1 : sampleTimes; // 最后一批对应列
                insertIndex = AddStep(
                    ParentFlowSteps,
                    insertIndex,
                    ref currentStepIndex,
                    "TipOff",
                    tipEjectPlateId,
                    finalEjectCol,
                    aspirateVolume,
                    selectedLiquid, 
                    false,
                    0,
                    0, 0,
                    0

                );
            }
            else
            {
                // 8. 最终退头步骤（所有批次处理完成后）
              //  int finalEjectCol = tipEjectPlateId == "1" ? 1 : sampleTimes; // 最后一批对应列
                insertIndex = AddStep(
                    ParentFlowSteps,
                    insertIndex,
                    ref currentStepIndex,
                    "TipOff",
                    tipEjectPlateId,
                    tipOnWell,
                    aspirateVolume,
                    selectedLiquid,
                    false,
                    0,
                    0 
                    ,0,
                    0
                );
            }

            // 9. 重新编号所有步骤
            RebuildStepIndexes(ParentFlowSteps);

            this.DialogResult = true;
            this.Close();
        }
        // 验证输入参数
        private bool ValidateInput()
        {
            // 验证下拉框选中
            if (TipPickPositionComboBox.SelectedItem == null)
            {
                _mainWidget.ShowNotification(_mainWidget._res.QuickTipOnPos, NotificationControl.NotificationType.Warn);
                return false;
            }
            if (AspiratePositionComboBox.SelectedItem == null)
            {
                _mainWidget.ShowNotification(_mainWidget._res.QuickAisPos, NotificationControl.NotificationType.Warn);
                return false;
            }
            if (DispensePositionComboBox.SelectedItem == null)
            {
                _mainWidget.ShowNotification(_mainWidget._res.QuickDisPos, NotificationControl.NotificationType.Warn);
                return false;
            }
            if (TipEjectPositionComboBox.SelectedItem == null)
            {
                _mainWidget.ShowNotification(_mainWidget._res.QuickTipOffPos, NotificationControl.NotificationType.Warn);
                return false;
            }
            if (LiquidComboBox.SelectedItem == null)
            {
                _mainWidget.ShowNotification(_mainWidget._res.QuickSelectLiquid, NotificationControl.NotificationType.Warn);
                return false;
            }

            // 验证体积
            if (!int.TryParse(AspirateVolumeTextBox.Text, out int volume) || volume <= 0)
            {
                _mainWidget.ShowNotification(_mainWidget._res.QuickValidAspirationVolume, NotificationControl.NotificationType.Warn);
                return false;
            }

            // 验证样本数量
            if (!int.TryParse(SampleCountTextBox.Text, out int count) || count < 1 || count > 96)
            {
                _mainWidget.ShowNotification(_mainWidget._res.QuickOne96Samples, NotificationControl.NotificationType.Warn);
                return false;
            }

            return true;
        }
        //提取板位ID（如从"P1 吸液槽"中提取"1"）
        private string ExtractPlateId(string displayText)
        {
            if (string.IsNullOrEmpty(displayText))
                return "";
            // 匹配P后的数字（支持"P1"、"P1 吸液槽"等格式）
            var match = System.Text.RegularExpressions.Regex.Match(displayText, @"P(\d+)");
            return match.Success ? match.Groups[1].Value : displayText;
        }
        //添加单个步骤到流程
        private int AddStep(ObservableCollection<FlowStep> steps, int insertIndex, ref int stepIndex,
                           string type, string plateId, int currentCol, int volume, LiquidSettings liquid,bool mixFlag,int mixCount,int mixValue, int RinseValue, int RinseTimes)
        {
            steps.Insert(insertIndex++, new FlowStep
            {
                Index = stepIndex++,
                //Name = $"{type}步骤{stepIndex - 1}",
                Type = type,
                Position = $"P{plateId}", // 板位格式：P1、P2...
                WellPosition = $"列：{currentCol}", // 孔位：列1、列2...
                Volume = volume,
                SelectedColumns = currentCol.ToString(),
                SelectedLiquid = liquid,
                IsSelected = false,
                IsSystemStep = false,
                IsMixEnabled = mixFlag,
                MixCount = mixCount,
                MixVolume = mixValue,
                FirstVol = RinseValue,
                FirstDelay = RinseTimes,
            });
            return insertIndex;
        }
        //重新编号所有步骤
        private void RebuildStepIndexes(ObservableCollection<FlowStep> steps)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                steps[i].Index = i + 1;
                //// 更新步骤名称（系统步骤除外）
                //if (!steps[i].IsSystemStep)
                //{
                //    steps[i].Name = $"{steps[i].Type}步骤{i + 1}";
                //}
            }
        }
    }
}
