using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace OctoFixFlow
{
    // 耗材信息
    //孔数乘积
    public class RowColumnMultiplierConverter : IMultiValueConverter
    {
        // 转换：行数×列数 → 孔数
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 确保输入是有效的整数
            if (values.Length == 2 &&
                int.TryParse(values[0]?.ToString(), out int rows) &&
                int.TryParse(values[1]?.ToString(), out int columns))
            {
                return (rows * columns).ToString(); // 计算乘积并返回字符串
            }
            return "0"; // 无效值时返回0
        }

        // 反向转换：不需要（孔数是只读的）
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class HightAddplierConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 &&
                float.TryParse(values[0]?.ToString(), out float rows) &&
                float.TryParse(values[1]?.ToString(), out float columns))
            {
                return (rows + columns).ToString(); 
            }
            return "0"; 
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //缺角方向
    public class NumberToBooleanConverter : IValueConverter
    {
        // int -> bool?（1→true，0→false）
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int num)
            {
                return num == 1; // 1 → 选中（true）；0 → 未选中（false）
            }
            return false;
        }

        // bool? -> int（true→1，false→0）
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked)
            {
                return isChecked ? 1 : 0; // 选中 → 1；未选中 → 0
            }
            return 0;
        }
    }
    public class ConsumableItem : INotifyPropertyChanged
    {
        private string _name;
        private ConsSettings _settings; // 平面图绘制所需的所有参数

        // 耗材名称
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        // 耗材平面图设置（用于绘制）
        public ConsSettings Settings
        {
            get => _settings;
            set { _settings = value; OnPropertyChanged(); }
        }

        // 属性通知接口（确保UI实时更新）
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class ConsSettings : INotifyPropertyChanged
    {
        private string _name;
        private int _id;
        private int _type;
        private string _description;
        private int _NW;
        private int _SW;
        private int _NE;
        private int _SE;
        private int _numRows;
        private int _numColumns;
        private float _labL;
        private float _labW;
        private float _labH;
        private float _distanceRowY;
        private float _distanceColumnX;
        private float _distanceRow;
        private float _distanceColumn;
        private float _offsetX;
        private float _offsetY;
        private float _RobotX;
        private float _RobotY;
        private float _RobotZ;
        private float _labVolume;
        private float _consMaxAvaiVol;
        private float _consDep;
        private int _topShape;
        private float _topRadius;
        private float _topUpperX;
        private float _topUpperY;
        private float _TIPMAXCapacity;
        private float _TIPMAXAvailable;
        private float _TIPTotalLength;
        private float _TIPHeadHeight;
        private float _TIPConeLength;
        private float _TIPMAXRadius;
        private float _TIPMINRadius;
        private float _TIPDepthOFComp;

        public string name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public int id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        public int type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                }
            }
        }

        public string description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NW
        {
            get => _NW;
            set
            {
                if (_NW != value)
                {
                    _NW = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SW
        {
            get => _SW;
            set
            {
                if (_SW != value)
                {
                    _SW = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NE
        {
            get => _NE;
            set
            {
                if (_NE != value)
                {
                    _NE = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SE
        {
            get => _SE;
            set
            {
                if (_SE != value)
                {
                    _SE = value;
                    OnPropertyChanged();
                }
            }
        }

        public int numRows
        {
            get => _numRows;
            set
            {
                if (_numRows != value)
                {
                    _numRows = value;
                    OnPropertyChanged();
                }
            }
        }

        public int numColumns
        {
            get => _numColumns;
            set
            {
                if (_numColumns != value)
                {
                    _numColumns = value;
                    OnPropertyChanged();
                }
            }
        }

        public float labL
        {
            get => _labL;
            set
            {
                if (_labL != value)
                {
                    _labL = value;
                    OnPropertyChanged();
                }
            }
        }

        public float labW
        {
            get => _labW;
            set
            {
                if (_labW != value)
                {
                    _labW = value;
                    OnPropertyChanged();
                }
            }
        }

        public float labH
        {
            get => _labH;
            set
            {
                if (_labH != value)
                {
                    _labH = value;
                    OnPropertyChanged();
                }
            }
        }

        public float distanceRowY
        {
            get => _distanceRowY;
            set
            {
                if (_distanceRowY != value)
                {
                    _distanceRowY = value;
                    OnPropertyChanged();
                }
            }
        }

        public float distanceColumnX
        {
            get => _distanceColumnX;
            set
            {
                if (_distanceColumnX != value)
                {
                    _distanceColumnX = value;
                    OnPropertyChanged();
                }
            }
        }

        public float distanceRow
        {
            get => _distanceRow;
            set
            {
                if (_distanceRow != value)
                {
                    _distanceRow = value;
                    OnPropertyChanged();
                }
            }
        }

        public float distanceColumn
        {
            get => _distanceColumn;
            set
            {
                if (_distanceColumn != value)
                {
                    _distanceColumn = value;
                    OnPropertyChanged();
                }
            }
        }

        public float offsetX
        {
            get => _offsetX;
            set
            {
                if (_offsetX != value)
                {
                    _offsetX = value;
                    OnPropertyChanged();
                }
            }
        }

        public float offsetY
        {
            get => _offsetY;
            set
            {
                if (_offsetY != value)
                {
                    _offsetY = value;
                    OnPropertyChanged();
                }
            }
        }

        public float RobotX
        {
            get => _RobotX;
            set
            {
                if (_RobotX != value)
                {
                    _RobotX = value;
                    OnPropertyChanged();
                }
            }
        }

        public float RobotY
        {
            get => _RobotY;
            set
            {
                if (_RobotY != value)
                {
                    _RobotY = value;
                    OnPropertyChanged();
                }
            }
        }

        public float RobotZ
        {
            get => _RobotZ;
            set
            {
                if (_RobotZ != value)
                {
                    _RobotZ = value;
                    OnPropertyChanged();
                }
            }
        }

        public float labVolume
        {
            get => _labVolume;
            set
            {
                if (_labVolume != value)
                {
                    _labVolume = value;
                    OnPropertyChanged();
                }
            }
        }

        public float consMaxAvaiVol
        {
            get => _consMaxAvaiVol;
            set
            {
                if (_consMaxAvaiVol != value)
                {
                    _consMaxAvaiVol = value;
                    OnPropertyChanged();
                }
            }
        }

        public float consDep
        {
            get => _consDep;
            set
            {
                if (_consDep != value)
                {
                    _consDep = value;
                    OnPropertyChanged();
                }
            }
        }

        public int topShape
        {
            get => _topShape;
            set
            {
                if (_topShape != value)
                {
                    _topShape = value;
                    OnPropertyChanged();
                }
            }
        }

        public float topRadius
        {
            get => _topRadius;
            set
            {
                if (_topRadius != value)
                {
                    _topRadius = value;
                    OnPropertyChanged();
                }
            }
        }

        public float topUpperX
        {
            get => _topUpperX;
            set
            {
                if (_topUpperX != value)
                {
                    _topUpperX = value;
                    OnPropertyChanged();
                }
            }
        }

        public float topUpperY
        {
            get => _topUpperY;
            set
            {
                if (_topUpperY != value)
                {
                    _topUpperY = value;
                    OnPropertyChanged();
                }
            }
        }

        public float TIPMAXCapacity
        {
            get => _TIPMAXCapacity;
            set
            {
                if (_TIPMAXCapacity != value)
                {
                    _TIPMAXCapacity = value;
                    OnPropertyChanged();
                }
            }
        }

        public float TIPMAXAvailable
        {
            get => _TIPMAXAvailable;
            set
            {
                if (_TIPMAXAvailable != value)
                {
                    _TIPMAXAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        public float TIPTotalLength
        {
            get => _TIPTotalLength;
            set
            {
                if (_TIPTotalLength != value)
                {
                    _TIPTotalLength = value;
                    OnPropertyChanged();
                }
            }
        }

        public float TIPHeadHeight
        {
            get => _TIPHeadHeight;
            set
            {
                if (_TIPHeadHeight != value)
                {
                    _TIPHeadHeight = value;
                    OnPropertyChanged();
                    UpdateTIPTotalLength();
                }
            }
        }

        public float TIPConeLength
        {
            get => _TIPConeLength;
            set
            {
                if (_TIPConeLength != value)
                {
                    _TIPConeLength = value;
                    OnPropertyChanged();
                    UpdateTIPTotalLength();
                }
            }
        }

        public float TIPMAXRadius
        {
            get => _TIPMAXRadius;
            set
            {
                if (_TIPMAXRadius != value)
                {
                    _TIPMAXRadius = value;
                    OnPropertyChanged();
                }
            }
        }

        public float TIPMINRadius
        {
            get => _TIPMINRadius;
            set
            {
                if (_TIPMINRadius != value)
                {
                    _TIPMINRadius = value;
                    OnPropertyChanged();
                }
            }
        }

        public float TIPDepthOFComp
        {
            get => _TIPDepthOFComp;
            set
            {
                if (_TIPDepthOFComp != value)
                {
                    _TIPDepthOFComp = value;
                    OnPropertyChanged();
                }
            }
        }
        private void UpdateTIPTotalLength()
        {
            TIPTotalLength = TIPHeadHeight + TIPConeLength;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // 流程步骤模型
    public class FlowStep : INotifyPropertyChanged
    {
        private int _index;
        private string _name;
        private string _type;
        private int _volume;
        private string _position;
        private string _wellPosition;
        private bool _isSelected;
        private string _selectedColumns;
        private bool _isMixEnabled;
        private int _mixCount;
        private float _mixVolume;
        private LiquidSettings _selectedLiquid;
        public bool _isSystemstep;
        private int _waitTime;
        private string _waitContent;
        private int _firstVol;//第一次的液体
        private int _firstDelay;//第一次的延迟
        public FlowStep()
        {
            // 初始化等待文本（多语言）
            _waitContent = ResourceHelper.Instance.FlowStepWaitContent;
            // 订阅ResourceHelper的PropertyChanged事件（语言切换时触发更新）
            ResourceHelper.Instance.PropertyChanged += (s, e) =>
            {
                // 语言变化时，更新等待文本和步骤名称
                WaitContent = ResourceHelper.Instance.FlowStepWaitContent;
                OnPropertyChanged(nameof(Name)); // 触发Name重新计算
            };
        }
        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                OnPropertyChanged();
            }
        }

        //public string Name
        //{
        //    get => _name;
        //    set
        //    {
        //        _name = value;
        //        OnPropertyChanged();
        //    }
        //}
        public string Name
        {
            get
            {
                // 根据Type值，从ResourceHelper获取对应多语言文本
                string typeText = _type switch
                {
                    "start" => ResourceHelper.Instance.FlowStepStart,    // 开始步骤
                    "end" => ResourceHelper.Instance.FlowStepEnd,        // 结束步骤
                    "Aspirate" => ResourceHelper.Instance.WindowActionAspirate, 
                    "Dispense" => ResourceHelper.Instance.WindowActionDispense,
                    "TipOn" => ResourceHelper.Instance.WindowActionTipOn,
                    "TipOff" => ResourceHelper.Instance.WindowActionTipOff,
                    "Wait" => ResourceHelper.Instance.WindowActionWait,

                    _ => _type // 未知类型时显示原始Type值（避免空值）
                };

                // 生成最终名称：系统步骤（Start/End）无后缀，自定义步骤加“步骤/Steps + 序号”
                return _isSystemstep
                    ? typeText
                    : $"{typeText} {ResourceHelper.Instance.FlowStepSteps} {_index}";
            }
        }
        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }

        public int Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                OnPropertyChanged();
            }
        }

        public string Position
        {
            get => _position;
            set
            {
                _position = value;
                OnPropertyChanged();
            }
        }
        public string WellPosition
        {
            get => _wellPosition;
            set
            {
                _wellPosition = value;
                OnPropertyChanged(); // 触发属性变更通知
            }
        }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(); // 必须触发通知
            }
        }
        public string SelectedColumns
        {
            get => _selectedColumns;
            set
            {
                _selectedColumns = value;
                OnPropertyChanged();
            }
        }
        // 混合相关属性
        public bool IsMixEnabled
        {
            get => _isMixEnabled;
            set { _isMixEnabled = value; OnPropertyChanged(); }
        }
        public int MixCount
        {
            get => _mixCount;
            set { _mixCount = value; OnPropertyChanged(); }
        }
        public float MixVolume
        {
            get => _mixVolume;
            set { _mixVolume = value; OnPropertyChanged(); }
        } 
        public bool IsSystemStep
        {
            get => _isSystemstep;
            set
            {
                _isSystemstep = value;
                OnPropertyChanged(); // 必须触发通知
            }
        }
        public int WaitTime
        {
            get => _waitTime;
            set 
            { _waitTime = value; 
                OnPropertyChanged(); 
            }
        }
        public string WaitContent
        {
            get => _waitContent;
            set 
            { 
                _waitContent = value; 
                OnPropertyChanged(); 
            }
        } 
        public int FirstVol
        {
            get => _firstVol;
            set 
            {
                _firstVol = value; 
                OnPropertyChanged(); 
            }
        }
        public int FirstDelay
        {
            get => _firstDelay;
            set
            {
                _firstDelay = value;
                OnPropertyChanged();
            }
        }
        // 液体相关属性
        public LiquidSettings SelectedLiquid
        {
            get => _selectedLiquid;
            set { _selectedLiquid = value; OnPropertyChanged(); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    //液体参数
    public class LiquidSettings : INotifyPropertyChanged
    {
        private string _name;
         private string _description;
        private float _aisAirB;
        private float _aisAirA;
        private float _aisSpeed;
        private float _aisDelay;
        private float _aisDistance;
        private float _disAirB;
        private float _disAirA;
        private float _disSpeed;
        private float _disDelay;
        private float _disDistance;

        public string name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public float aisAirB
        {
            get => _aisAirB;
            set
            {
                if (_aisAirB != value)
                {
                    _aisAirB = value;
                    OnPropertyChanged();
                }
            }
        }

        public float aisAirA
        {
            get => _aisAirA;
            set
            {
                if (_aisAirA != value)
                {
                    _aisAirA = value;
                    OnPropertyChanged();
                }
            }
        }

        public float aisSpeed
        {
            get => _aisSpeed;
            set
            {
                if (_aisSpeed != value)
                {
                    _aisSpeed = value;
                    OnPropertyChanged();
                }
            }
        }

        public float aisDelay
        {
            get => _aisDelay;
            set
            {
                if (_aisDelay != value)
                {
                    _aisDelay = value;
                    OnPropertyChanged();
                }
            }
        }

        public float aisDistance
        {
            get => _aisDistance;
            set
            {
                if (_aisDistance != value)
                {
                    _aisDistance = value;
                    OnPropertyChanged();
                }
            }
        }

        public float disAirB
        {
            get => _disAirB;
            set
            {
                if (_disAirB != value)
                {
                    _disAirB = value;
                    OnPropertyChanged();
                }
            }
        }

        public float disAirA
        {
            get => _disAirA;
            set
            {
                if (_disAirA != value)
                {
                    _disAirA = value;
                    OnPropertyChanged();
                }
            }
        }

        public float disSpeed
        {
            get => _disSpeed;
            set
            {
                if (_disSpeed != value)
                {
                    _disSpeed = value;
                    OnPropertyChanged();
                }
            }
        }

        public float disDelay
        {
            get => _disDelay;
            set
            {
                if (_disDelay != value)
                {
                    _disDelay = value;
                    OnPropertyChanged();
                }
            }
        }

        public float disDistance
        {
            get => _disDistance;
            set
            {
                if (_disDistance != value)
                {
                    _disDistance = value;
                    OnPropertyChanged();
                }
            }
        }
        public override string ToString()
        {
            return name ?? "未命名液体"; // 优先返回name，为空时显示默认值
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    //grpc
    public class MotorActionParams
    {
        public int MotorId { get; set; }
        public int ActionType { get; set; }
        public float Target { get; set; }
        public float Speed { get; set; }
        public float Acc { get; set; }
        public float Dcc { get; set; }
    }
    public class PipeCalibrationParams
    {
        public string PipeName { get; set; }
        public float BackDiff { get; set; } // 回程差
        public float K10 { get; set; }      // 10挡
        public float K20 { get; set; }      // 20挡
        public float K50 { get; set; }      // 50挡
        public float K100 { get; set; }     // 100挡
        public float K200 { get; set; }     // 200挡
        public float K300 { get; set; }     // 300挡
        public float K400 { get; set; }     // 400挡
        public float K500 { get; set; }     // 500挡
        public float K600 { get; set; }     // 600挡
        public float K700 { get; set; }     // 700挡
        public float K800 { get; set; }     // 800挡
        public float K900 { get; set; }     // 900挡
        public float K1000 { get; set; }    // 1000挡
    }
    public class ScriptMonitorEventArgs : EventArgs
    {
        public int ErrorCode { get; set; }               // 错误码
        public string ErrorInfo { get; set; }            // 错误信息
        public string State { get; set; }                // 脚本状态
        public int CurrentStep { get; set; }             // 当前步骤
        public int MaxStep { get; set; }                 // 总步骤数
        public int MaxTime { get; set; }                 // 最大运行时间
        public int RunTime { get; set; }                 // 已运行时间
    }
    public class LogEntry
    {
        public string Time { get; set; }
        public string Message { get; set; }
        public string Level { get; set; } // Info, Warning, Error
    }
}