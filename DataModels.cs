using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

namespace OctoFixFlow
{

    public class LevelToMarginConverter : IValueConverter
    {
        // 可配置：每级缩进的像素数（默认20）
        public double IndentPerLevel { get; set; } = 20;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                // 仅左边缩进，其他方向留白为0
                return new Thickness(level * IndentPerLevel, 0, 0, 0);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
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
        private float _ThreeWellThickness;//壁厚
        private float _ThreeSkirtHeight;//裙边高
        private float _ThreeTopLength;//顶部长
        private float _ThreeTopWidth;//顶部宽
        private int _botType;//底部类型 0圆形 1锥形 2平底
        private float _ThreeBotTaperDepth;//锥形深度
        private int _botShape;//顶部形状  0圆 1长方形
        private float _botRadius;
        private float _botHoleX;
        private float _botHoleY;

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
        public float ThreeWellThickness
        {
            get => _ThreeWellThickness;
            set
            {
                if (_ThreeWellThickness != value)
                {
                    _ThreeWellThickness = value;
                    OnPropertyChanged();
                }
            }
        }
        public float ThreeSkirtHeight
        {
            get => _ThreeSkirtHeight;
            set
            {
                if (_ThreeSkirtHeight != value)
                {
                    _ThreeSkirtHeight = value;
                    OnPropertyChanged();
                }
            }
        }
        public float ThreeTopLength
        {
            get => _ThreeTopLength;
            set
            {
                if (_ThreeTopLength != value)
                {
                    _ThreeTopLength = value;
                    OnPropertyChanged();
                }
            }
        }
        public float ThreeTopWidth
        {
            get => _ThreeTopWidth;
            set
            {
                if (_ThreeTopWidth != value)
                {
                    _ThreeTopWidth = value;
                    OnPropertyChanged();
                }
            }
        }
        public int botType
        {
            get => _botType;
            set
            {
                if (_botType != value)
                {
                    _botType = value;
                    OnPropertyChanged();
                }
            }
        }
        public float ThreeBotTaperDepth
        {
            get => _ThreeBotTaperDepth;
            set
            {
                if (_ThreeBotTaperDepth != value)
                {
                    _ThreeBotTaperDepth = value;
                    OnPropertyChanged();
                }
            }
        }
        public int botShape
        {
            get => _botShape;
            set
            {
                if (_botShape != value)
                {
                    _botShape = value;
                    OnPropertyChanged();
                }
            }
        }
        public float botRadius
        {
            get => _botRadius;
            set
            {
                if (_botRadius != value)
                {
                    _botRadius = value;
                    OnPropertyChanged();
                }
            }
        }

        public float botHoleX
        {
            get => _botHoleX;
            set
            {
                if (_botHoleX != value)
                {
                    _botHoleX = value;
                    OnPropertyChanged();
                }
            }
        }

        public float botHoleY
        {
            get => _botHoleY;
            set
            {
                if (_botHoleY != value)
                {
                    _botHoleY = value;
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
        // 加热振荡模块转速
        private const int MIN_SHAKE_RPM = 100;
        private const int MAX_SHAKE_RPM = 2500;
        // 加热振荡模块温度
        private const int MIN_SHAKE_TEMP = 4;
        private const int MAX_SHAKE_TEMP = 105;

        private int _index;
        private string _name;
        private string _type;
        private int _level; //步骤的嵌套层级（0=外层，1=Loop内第一层）
        public string _displayIndex;
        private float _volume;
        private string _position;
        private string _consName;
        private int _consRows;
        private int _consCols;
        private string _wellPosition;
        private bool _isSelected;
        private bool _isError;
        private string _selectedColumns;
        private string _selectedCells;
        private int _mixCount;
        private float _mixVolume;
        private float _pushOutvolume;
        private float _inhaVolume;
        private LiquidSettings _selectedLiquid;
        private float _liquidAisAirB;
        private float _liquidAisAirA;
        private float _liquidAisSpeed;
        private float _liquidAisDelay;
        private string _liquidAisDistance;
        private float _liquidDisAirB;
        private float _liquidDisAirA;
        private float _liquidDisSpeed;
        private float _liquidDisDelay;
        private string _liquidDisDistance;
        public bool _isSystemstep;
        private int _waitTime;
        private string _waitContent;
        private string _selectedPipetteName;
        private string _moduleName;
        private int _shakeRPM;
        private float _shakeTemp;
        private bool _isMagnetUp = true;
        private bool _isMagnetDown = false;
        private float _magnetNums;
        private float _tempCtrlTemp;
        private bool _isTempCtrlOpen = true;
        private bool _isTempCtrlClose = false;
        private string _fromPos;
        private string _toPos;
        private float _transferPosition;
        private string _pcrStep;
        private string _pcrScriptAdress;
        private string _stepDescription;
        private int _loopStartNum = 1;
        private int _loopEndNum = 10;
        private int _loopAddNum = 1;
        private string _annoValue;
        private string _variateScriptName;
        private string _variateStep;
        private float _variateNum = 1;

        #region 变量振荡
        private string _shakerVariateTimeName;
        private string _shakerVariateTimeValue;
        private string _shakerVariateSpeedName;
        private string _shakerVariateSpeedValue;
        private string _shakerVariateTempName;
        private string _shakerVariateTempValue;
        #endregion
        #region 变量温控
        private string _tempControlVariateTempName;
        private string _tempControlVariateTempValue;
        #endregion
        #region 变量磁吸
        private string _magnetVariateName;
        private string _magnetVariateValue;
        #endregion
        #region 变量等待
        private string _waitVariateName;
        private string _waitVariateValue;
        #endregion
        #region 变量孔位
        // 行变量（对应界面的「行」）
        private string _wellRowVariateName;
        private string _wellRowVariateValue;
        // 列变量（对应界面的「列」）
        private string _wellColVariateName;
        private string _wellColVariateValue;
        #endregion
        public readonly ResourceHelper _res;

        public FlowStep()
        {
            _res = ResourceHelper.Instance;
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
        public string Name
        {
            get
            {
                string typeText = _type switch
                {
                    "start" => ResourceHelper.Instance.FlowStepStart,
                    "end" => ResourceHelper.Instance.FlowStepEnd,
                    "Aspirate" => ResourceHelper.Instance.WindowActionAspirate,
                    "Dispense" => ResourceHelper.Instance.WindowActionDispense,
                    "TipOn" => ResourceHelper.Instance.WindowActionTipOn,
                    "TipOff" => ResourceHelper.Instance.WindowActionTipOff,
                    "Wait" => ResourceHelper.Instance.WindowActionWait,
                    "Shake" => ResourceHelper.Instance.WindowActionShake,
                    "Magnetic" => ResourceHelper.Instance.WindowActionMagnetic,
                    "Temp Ctrl" => ResourceHelper.Instance.WindowActionTemperature,
                    "PCR" => ResourceHelper.Instance.WindowActionPCR,
                    "Transfer" => ResourceHelper.Instance.WindowActionTransfer,
                    "Mix" => ResourceHelper.Instance.WindowActionMix,
                    "Loop" => ResourceHelper.Instance.WindowActionLoop,
                    "endLoop" => ResourceHelper.Instance.WindowActionEndLoop,
                    "Annotation" => ResourceHelper.Instance.WindowActionAnno,
                    "Variate" => ResourceHelper.Instance.WindowActionVariate,
                    "Fluo" => ResourceHelper.Instance.WindowActionFluo,

                    _ => _type // 未知类型时显示原始Type值（避免空值）
                };

                // 生成最终名称：系统步骤（Start/End）无后缀，自定义步骤加“步骤/Steps + 序号”
                //return _isSystemstep
                //    ? typeText
                //    : $"{typeText} {ResourceHelper.Instance.FlowStepSteps} {_index}";
                return _isSystemstep
                  ? typeText
                  : $"{typeText} {ResourceHelper.Instance.FlowStepSteps}";
            }
        }
        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Name));
                UpdateStepDescription();
            }
        }
        public int Level
        {
            get => _level;
            set
            {
                _level = value;
                OnPropertyChanged();
            }
        }
        public string DisplayIndex
        {
            get => _displayIndex;
            set
            {
                _displayIndex = value;
                OnPropertyChanged();
            }
        }
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }

        public string Position
        {
            get => _position;
            set
            {
                _position = value;
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }
        public string ConsName
        {
            get => _consName;
            set
            {
                _consName = value;
                OnPropertyChanged();
            }
        }
        public int ConsRows
        {
            get => _consRows;
            set
            {
                _consRows = value;
                OnPropertyChanged();
            }
        }
        public int ConsCols
        {
            get => _consCols;
            set
            {
                _consCols = value;
                OnPropertyChanged();
            }
        }
        public string WellPosition
        {
            get => _wellPosition;
            set
            {
                _wellPosition = value;
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
        public bool IsError
        {
            get => _isError;
            set
            {
                _isError = value;
                OnPropertyChanged();
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
        public string SelectedCells
        {
            get => _selectedCells;
            set
            {
                _selectedCells = value;
                OnPropertyChanged();
            }
        }
        // 混合相关属性
        public int MixCount
        {
            get => _mixCount;
            set { _mixCount = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        public float MixVolume
        {
            get => _mixVolume;
            set { _mixVolume = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        public float PushOutvolume
        {
            get => _pushOutvolume;
            set { _pushOutvolume = value; OnPropertyChanged(); }
        }
        public float InhaVolume
        {
            get => _inhaVolume;
            set { _inhaVolume = value; OnPropertyChanged(); }
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
            {
                _waitTime = value;
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }
        public string WaitContent
        {
            get => _waitContent;
            set
            {
                _waitContent = value;
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }

        public string SelectedPipetteName
        {
            get => _selectedPipetteName;
            set
            {
                _selectedPipetteName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedPipetteMaxVolume));
                UpdateStepDescription();
            }
        }
        public string SelectedPipetteMaxVolume
        {
            get
            {
                var pipetteModule = AppGlobalConfig.Instance.PlateModuleMap
                                    .Values
                                    .FirstOrDefault(module => module.Name == _selectedPipetteName);
                string pipetteMaxVolume = "0-" + pipetteModule.PipetteVolume.ToString() + "μL";

                return pipetteMaxVolume;
            }
        }
        public string ModuleName
        {
            get => _moduleName;
            set
            {
                _moduleName = value;
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }
        public int ShakeRPM
        {
            get => _shakeRPM;
            set
            {
                int correctedValue = Math.Clamp(value, MIN_SHAKE_RPM, MAX_SHAKE_RPM);
                if (_shakeRPM != correctedValue)
                {
                    _shakeRPM = correctedValue;
                    OnPropertyChanged();
                    UpdateStepDescription();
                }
            }
        }
        public float ShakeTemp
        {
            get => _shakeTemp;
            set
            {
                float correctedValue = Math.Clamp(value, MIN_SHAKE_TEMP, MAX_SHAKE_TEMP);
                if (_shakeTemp != correctedValue)
                {
                    _shakeTemp = correctedValue;
                    OnPropertyChanged();
                    UpdateStepDescription();
                }
            }
        }
        public float TempCtrlTemp
        {
            get => _tempCtrlTemp;
            set
            {
                float correctedValue = Math.Clamp(value, MIN_SHAKE_TEMP, MAX_SHAKE_TEMP);
                if (_tempCtrlTemp != correctedValue)
                {
                    _tempCtrlTemp = correctedValue;
                    OnPropertyChanged();
                    UpdateStepDescription();
                }
            }
        }
        public bool IsTempCtrlOpen
        {
            get => _isTempCtrlOpen;
            set { _isTempCtrlOpen = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        public bool IsTempCtrlClose
        {
            get => _isTempCtrlClose;
            set { _isTempCtrlClose = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        public bool IsMagnetUp
        {
            get => _isMagnetUp;
            set { _isMagnetUp = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        public bool IsMagnetDown
        {
            get => _isMagnetDown;
            set { _isMagnetDown = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        public float MagnetNums
        {
            get => _magnetNums;
            set
            {
                float correctedValue = Math.Clamp(value, 0, 25);
                if (_magnetNums != correctedValue)
                {
                    _magnetNums = correctedValue;
                    OnPropertyChanged();
                    UpdateStepDescription();
                }
            }
        }
        public string FromPos
        {
            get => _fromPos;
            set
            {
                _fromPos = value;
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }
        public string ToPos
        {
            get => _toPos;
            set
            {
                _toPos = value;
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }
        public float TransferPosition
        {
            get => _transferPosition;
            set
            {
                _transferPosition = value;
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }
        public string PcrStep
        {
            get => _pcrStep;
            set
            {
                _pcrStep = value;
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }
        public string PcrScriptAdress
        {
            get => _pcrScriptAdress;
            set
            {
                _pcrScriptAdress = value;
                OnPropertyChanged();
            }
        }
        public string StepDescription
        {
            get => _stepDescription;
            set
            {
                _stepDescription = value;
                OnPropertyChanged();
            }
        }
        public int LoopStartNum
        {
            get => _loopStartNum;
            set
            {
                int newStart = Math.Max(1, value);
                _loopStartNum = newStart;
                if (_loopEndNum <= newStart)
                {
                    _loopEndNum = newStart + 1;
                    OnPropertyChanged(nameof(LoopEndNum));
                }
                if (_loopAddNum > _loopEndNum - _loopStartNum)
                {
                    _loopAddNum = Math.Max(1, _loopEndNum - _loopStartNum);
                    OnPropertyChanged(nameof(LoopAddNum));
                }
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }
        public int LoopEndNum
        {
            get => _loopEndNum;
            set
            {
                int minEnd = _loopStartNum + 1;
                _loopEndNum = Math.Max(value, minEnd);
                if (_loopAddNum > _loopEndNum - _loopStartNum)
                {
                    _loopAddNum = Math.Max(1, _loopEndNum - _loopStartNum);
                    OnPropertyChanged(nameof(LoopAddNum));
                }

                OnPropertyChanged();
                UpdateStepDescription();
            }
        }
        public int LoopAddNum
        {
            get => _loopAddNum;
            set
            {
                int minStep = 1;
                int maxStep = _loopEndNum - _loopStartNum;
                _loopAddNum = Math.Clamp(value, minStep, maxStep);
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }
        public string AnnoValue
        {
            get => _annoValue;
            set
            {
                _annoValue = value;
                OnPropertyChanged();
                UpdateStepDescription();
            }
        }
        public string VariateScriptName
        {
            get => _variateScriptName;
            set
            {
                _variateScriptName = value;
                OnPropertyChanged();
            }
        }
        public string VariateStep
        {
            get => _variateStep;
            set
            {
                _variateStep = value;
                OnPropertyChanged();
            }
        }
        public float VariateNum
        {
            get => _variateNum;
            set { _variateNum = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        #region 变量振荡
        public string ShakerVariateTimeName
        {
            get => _shakerVariateTimeName;
            set
            {
                _shakerVariateTimeName = value;
                OnPropertyChanged();
            }
        }
        public string ShakerVariateTimeValue
        {
            get => _shakerVariateTimeValue;
            set { _shakerVariateTimeValue = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        public string ShakerVariateSpeedName
        {
            get => _shakerVariateSpeedName;
            set
            {
                _shakerVariateSpeedName = value;
                OnPropertyChanged();
            }
        }
        public string ShakerVariateSpeedValue
        {
            get => _shakerVariateSpeedValue;
            set { _shakerVariateSpeedValue = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        public string ShakerVariateTempName
        {
            get => _shakerVariateTempName;
            set
            {
                _shakerVariateTempName = value;
                OnPropertyChanged();
            }
        }
        public string ShakerVariateTempValue
        {
            get => _shakerVariateTempValue;
            set { _shakerVariateTempValue = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        #endregion
        #region 变量温控
        public string TempControlVariateTempName
        {
            get => _tempControlVariateTempName;
            set
            {
                _tempControlVariateTempName = value;
                OnPropertyChanged();
            }
        }
        public string TempControlVariateTempValue
        {
            get => _tempControlVariateTempValue;
            set { _tempControlVariateTempValue = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        #endregion
        #region 变量磁吸
        public string MagnetVariateName
        {
            get => _magnetVariateName;
            set
            {
                _magnetVariateName = value;
                OnPropertyChanged();
            }
        }
        public string MagnetVariateValue
        {
            get => _magnetVariateValue;
            set { _magnetVariateValue = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        #endregion
        #region 变量等待
        public string WaitVariateName
        {
            get => _waitVariateName;
            set
            {
                _waitVariateName = value;
                OnPropertyChanged();
            }
        }
        public string WaitVariateValue
        {
            get => _waitVariateValue;
            set { _waitVariateValue = value; OnPropertyChanged(); UpdateStepDescription(); }
        }
        #endregion
        #region 变量孔位  罗贤全
        public string WellRowVariateName
        {
            get => _wellRowVariateName;
            set
            {
                _wellRowVariateName = value;
                OnPropertyChanged();
            }
        }
        public string WellRowVariateValue
        {
            get => _wellRowVariateValue;
            set
            {
                _wellRowVariateValue = value;
                OnPropertyChanged();
                // 行变化时，自动同步组合后的WellPosition，兼容老逻辑
                //UpdateCombinedWellPosition();
                UpdateStepDescription();
            }
        }
        public string WellColVariateName
        {
            get => _wellColVariateName;
            set
            {
                _wellColVariateName = value;
                OnPropertyChanged();
            }
        }
        public string WellColVariateValue
        {
            get => _wellColVariateValue;
            set
            {
                _wellColVariateValue = value;
                OnPropertyChanged();
                // 列变化时，自动同步组合后的WellPosition，兼容老逻辑
                //UpdateCombinedWellPosition();
                UpdateStepDescription();
            }
        }
        #endregion
        // 液体相关属性
        public LiquidSettings SelectedLiquid
        {
            get => _selectedLiquid;
            set { _selectedLiquid = value; OnPropertyChanged(); }
        }
        public float LiquidAisAirB
        {
            get => _liquidAisAirB;
            set
            {
                _liquidAisAirB = value;
                OnPropertyChanged();
            }
        }
        public float LiquidAisAirA
        {
            get => _liquidAisAirA;
            set
            {
                _liquidAisAirA = value;
                OnPropertyChanged();
            }
        }
        public float LiquidAisSpeed
        {
            get => _liquidAisSpeed;
            set
            {
                _liquidAisSpeed = value;
                OnPropertyChanged();
            }
        }
        public float LiquidAisDelay
        {
            get => _liquidAisDelay;
            set
            {
                _liquidAisDelay = value;
                OnPropertyChanged();
            }
        }
        public string LiquidAisDistance
        {
            get => _liquidAisDistance;
            set
            {
                _liquidAisDistance = value;
                OnPropertyChanged();
            }
        }
        public float LiquidDisAirB
        {
            get => _liquidDisAirB;
            set
            {
                _liquidDisAirB = value;
                OnPropertyChanged();
            }
        }
        public float LiquidDisAirA
        {
            get => _liquidDisAirA;
            set
            {
                _liquidDisAirA = value;
                OnPropertyChanged();
            }
        }
        public float LiquidDisSpeed
        {
            get => _liquidDisSpeed;
            set
            {
                _liquidDisSpeed = value;
                OnPropertyChanged();
            }
        }
        public float LiquidDisDelay
        {
            get => _liquidDisDelay;
            set
            {
                _liquidDisDelay = value;
                OnPropertyChanged();
            }
        }
        public string LiquidDisDistance
        {
            get => _liquidDisDistance;
            set
            {
                _liquidDisDistance = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateStepDescription()
        {

            //// 系统步骤（开始/结束）处理
            //if (IsSystemStep)
            //{
            //    StepDescription = Type switch
            //    {
            //        "start" => $"# {_res.FlowStepStart}",
            //        "end" => $"# {_res.FlowStepEnd}",
            //        _ => ""
            //    };
            //    return;
            //}

            //// 非系统步骤，完全对齐CreateScriptPython里的注释生成逻辑
            //StepDescription = Type switch
            //{
            //    "Aspirate" => $"# {_res.WindowActionAspirate}（{Position} {WellPosition}，{Volume:F2}μL）",
            //    "Dispense" => $"#  {_res.WindowActionDispense}（{Position} {WellPosition}，{Volume:F2}μL）",
            //    "TipOn" => $"# {_res.WindowActionTipOn}（{Position} {WellPosition}）",
            //    "TipOff" => $"# {_res.WindowActionTipOff}（{Position} {WellPosition}）",
            //    "Wait" => $"# {_res.WindowActionWait}（{(!string.IsNullOrEmpty(WaitContent) ? WaitContent : $"{WaitTime}秒")}）",
            //    "Mix" => $"# {_res.WindowActionMix}（{MixVolume:F2}μL，{MixCount}轮）",
            //    "Transfer" => $"# {_res.WindowActionTransfer}（{FromPos} → {ToPos}）",
            //    "Shake" => $"# {_res.WindowActionShake}（{ShakeRPM}rpm，{WaitTime}秒，{ShakeTemp:F1}℃）",
            //    "Magnetic" => $"# {_res.WindowActionMagnetic}{(IsMagnetUp ? "上升" : "下降")}",
            //    "Temp Ctrl" => $"# {_res.WindowActionTemperature}（{ModuleName}，{TempCtrlTemp:F1}℃）{(IsTempCtrlOpen ? " 启动" : " 停止")}",
            //    "PCR" => $"# PCR（{PcrStep}）",
            //    "Loop" => $"# {_res.WindowActionLoop}",
            //    "endLoop" => $"# {_res.WindowActionEndLoop}",
            //    _ => $"# {Type}"
            //};

            // 如需多语言适配，把中文替换为ResourceHelper的资源键即可，例如：
            // "Aspirate" => $"# {_res.StepAspirate}（{Position} {WellPosition}，{Volume:F2}μL）",
        }
        //private void UpdateCombinedWellPosition()
        //{
        //    // 行和列都有值才组合，否则清空
        //    if (!string.IsNullOrEmpty(WellRowVariateValue) && !string.IsNullOrEmpty(WellColVariateValue))
        //    {
        //        // 完全对齐你现有界面的格式："行: X 列: Y"，复用多语言资源，无硬编码
        //        WellPosition = $"{ResourceHelper.Instance.StepDetailRowPrefix}{WellRowVariateValue} {ResourceHelper.Instance.StepDetailColumnPrefix}{WellColVariateValue}";
        //    }
        //    else
        //    {
        //        WellPosition = "";
        //        SelectedCells = "";
        //    }
        //}
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
        private string _aisDistance;
        private float _disAirB;
        private float _disAirA;
        private float _disSpeed;
        private float _disDelay;
        private string _disDistance;

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

        public string aisDistance
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

        public string disDistance
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
    public class PlateConfig
    {
        //微孔板
        public double QbLength { get; set; } = 127.23;
        public double QbWidth { get; set; } = 85.15;
        public double QbHeight { get; set; } = 2.46;

        public double Length { get; set; } = 125.39;
        public double Width { get; set; } = 83.20;
        public double Height { get; set; } = 42.36;

        public double TopLength { get; set; } = 120.21;
        public double TopWidth { get; set; } = 77.39;

        public int Rows { get; set; } = 8;
        public int Cols { get; set; } = 12;

        public double WallThickness { get; set; } = 1.0;

        public double HoleDiameter { get; set; } = 5.0;
        public double ConeHeight { get; set; } = 15.48;

        public double RowSpacing { get; set; } = 9.0;
        public double ColSpacing { get; set; } = 9.0;

        //
        public double GTopHeight { get; set; } = 5.87;
        public double TopRadius { get; set; } = 3.4;

        public double PassageHeight { get; set; } = 62.36;
        public double TailConeRadius { get; set; } = 2.6;
        public double FilterHeight { get; set; } = 3.48;
        public double FilterinHeight { get; set; } = 77.20;
        public double TailEndRadiu { get; set; } = 0.5;
        public double TailConeHeight { get; set; } = 27.98;
        public double PassageTopRadius { get; set; } = 3.09;
    }
    /// <summary>
    /// 全局配置单例类：管理全局共享数据
    /// </summary>
    /// 
    public class AppGlobalConfig : INotifyPropertyChanged
    {
        // 单例实例（线程安全）
        private static readonly Lazy<AppGlobalConfig> _instance = new Lazy<AppGlobalConfig>(() => new AppGlobalConfig());
        public static AppGlobalConfig Instance => _instance.Value;

        // 私有构造函数：禁止外部创建实例
        private AppGlobalConfig()
        {
            _guideProtocolName = "";
            _guideProtocolDescription = "";
            _guideProtocolAuthor = "";
            _guideProtocolStartTime = "";

            _isGripperEnabled = false;
            _isPCREnabled = false;
            _isFluoEnabled = false;
            _isTrashEnabled = true;
            _plateModuleMap = new Dictionary<string, ModuleDatas>();
        }

        #region 全局属性
        //项目名称
        private string _guideProtocolName;
        public string GuideProtocolName
        {
            get => _guideProtocolName;
            set
            {
                if (_guideProtocolName != value)
                {
                    _guideProtocolName = value;
                    OnPropertyChanged();
                }
            }
        }
        //项目描述
        private string _guideProtocolDescription;
        public string GuideProtocolDescription
        {
            get => _guideProtocolDescription;
            set
            {
                if (_guideProtocolDescription != value)
                {
                    _guideProtocolDescription = value;
                    OnPropertyChanged();
                }
            }
        }
        //项目作者
        private string _guideProtocolAuthor;
        public string GuideProtocolAuthor
        {
            get => _guideProtocolAuthor;
            set
            {
                if (_guideProtocolAuthor != value)
                {
                    _guideProtocolAuthor = value;
                    OnPropertyChanged();
                }
            }
        }
        //项目时间
        private string _guideProtocolStartTime;
        public string GuideProtocolStartTime
        {
            get => _guideProtocolStartTime;
            set
            {
                if (_guideProtocolStartTime != value)
                {
                    _guideProtocolStartTime = value;
                    OnPropertyChanged();
                }
            }
        }
        // 抓手启用状态
        private bool _isGripperEnabled;
        public bool IsGripperEnabled
        {
            get => _isGripperEnabled;
            set
            {
                if (_isGripperEnabled != value)
                {
                    _isGripperEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        // PCR启用状态
        private bool _isPCREnabled;
        public bool IsPCREnabled
        {
            get => _isPCREnabled;
            set
            {
                if (_isPCREnabled != value)
                {
                    _isPCREnabled = value;
                    OnPropertyChanged();
                }
            }
        }
        // 荧光检测模块启用状态
        private bool _isFluoEnabled;
        public bool IsFluoEnabled
        {
            get => _isFluoEnabled;
            set
            {
                if (_isFluoEnabled != value)
                {
                    _isFluoEnabled = value;
                    OnPropertyChanged();
                }
            }
        }
        // 垃圾桶启用状态
        private bool _isTrashEnabled;
        public bool IsTrashEnabled
        {
            get => _isTrashEnabled;
            set
            {
                if (_isTrashEnabled != value)
                {
                    _isTrashEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        // 设备模块列表
        private Dictionary<string, ModuleDatas> _plateModuleMap;
        private readonly Dictionary<int, string> _moduleTypePrefixMap = new Dictionary<int, string>
    {
        { 5, "shaker_" },
        { 6, "magnetic_" },
        { 7, "tempctrl_" }
    };
        public IReadOnlyDictionary<string, ModuleDatas> PlateModuleMap
        {
            get => _plateModuleMap;
        }
        //扩展三个板位
        public readonly Dictionary<int, bool> _addablePlateState = new Dictionary<int, bool>()
{
    {13, false},
    {14, false},
    {15, false}
};


        #endregion

        #region INotifyPropertyChanged（属性通知接口）
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // 统一添加/修改模块（内部操作私有Dictionary，外部调用即可）
        public void AddOrUpdateModule(string plateId, ModuleDatas module)
        {
            if (_plateModuleMap.ContainsKey(plateId))
                _plateModuleMap[plateId] = module; // 已有则更新
            else
                _plateModuleMap.Add(plateId, module); // 没有则添加

            OnPropertyChanged(nameof(PlateModuleMap)); // 触发通知，UI同步
        }
        public void DeleteModule(string plateId)
        {

            if (_plateModuleMap.ContainsKey(plateId))
                _plateModuleMap.Remove(plateId);

            OnPropertyChanged(nameof(PlateModuleMap)); // 触发通知，UI同步
        }
        //判断加热振荡有没有
        public bool HasHeatingShaking() => PlateModuleMap.Values.Any(m => m.Type == 5);

        //判断磁吸有没有
        public bool HasMagnetic() => PlateModuleMap.Values.Any(m => m.Type == 6);

        //判断温控有没有
        public bool HasTemperatureControl() => PlateModuleMap.Values.Any(m => m.Type == 7);
        /// <summary>
        /// 遍历 PlateModuleMap，按类型分类排序后重新命名（shaker_序号、magnetic_序号等）
        /// </summary>
        public void RenameModulesByType()
        {
            // 1. 过滤出需要重命名的模块（排除移液器模块 pipette_1/pipette_2）
            var targetModules = _plateModuleMap.Values
                .Where(module => !module.Name.StartsWith("pipette_")) // 排除移液器
                .Where(module => _moduleTypePrefixMap.ContainsKey(module.Type)) // 只处理目标类型
                .ToList();

            // 2. 按模块类型分组 → 同类型内排序（按原模块Key的数字升序，可调整排序规则）
            var groupedModules = targetModules
                .GroupBy(module => module.Type) // 按 Type 分组
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(module => int.Parse(_plateModuleMap.First(kv => kv.Value == module).Key)) // 按原模块ID（如"13""14"）升序
                        .ToList()
                );

            // 3. 遍历每组，按顺序编号并重新命名
            foreach (var group in groupedModules)
            {
                var moduleType = group.Key;
                var prefix = _moduleTypePrefixMap[moduleType]; // 获取类型前缀
                var sortedModules = group.Value;

                // 同类型内按排序结果编号（1、2、3...）
                for (int i = 0; i < sortedModules.Count; i++)
                {
                    var module = sortedModules[i];
                    var newName = $"{prefix}{i + 1}"; // 命名格式：前缀+序号（如 shaker_1）
                    module.Name = newName; // 更新模块名称
                }
            }
            //// 4. 触发配置变更通知（如果需要UI实时更新）
            //OnConfigChanged(); // 若有配置变更事件，可在此触发
        }
        #endregion
    }
    // 模块数据类
    public class ModuleDatas
    {
        public string Name { get; set; } // 名称（用于流程步骤）
        public int Type { get; set; } // 类型：-1:空；0：单通道移液器；1：八通道移液器；2：96通道移液器；3：抓手；4：PCR；5：加热振荡；6：磁吸；7：温控;8:垃圾桶;9:荧光检测模块
        public string PlatePosition { get; set; } // 板位（P1-P12）
        public int PipetteVolume { get; set; } // 移液器的最大容量（200，1000）
        public string ModuleImage { get; set; } // 模块图片地址

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
        // 回程差
        public double backdiff { get; set; }
        // 10挡
        public double k_10 { get; set; }
        // 20挡
        public double k_20 { get; set; }
        // 50挡
        public double k_50 { get; set; }
        // 100挡
        public double k_100 { get; set; }
        // 200挡
        public double k_200 { get; set; }
        // 300挡
        public double k_300 { get; set; }
        // 400挡
        public double k_400 { get; set; }
        // 500挡
        public double k_500 { get; set; }
        // 600挡
        public double k_600 { get; set; }
        // 700挡
        public double k_700 { get; set; }
        // 800挡
        public double k_800 { get; set; }
        // 900挡
        public double k_900 { get; set; }
        // 1000挡
        public double k_1000 { get; set; }
        // 额外字段（用户示例未用到，可保留或删除）
        public double k_1 { get; set; }
        public double k_2 { get; set; }
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
    public class DeviceModuleSettings
    {
        public int Id { get; set; }
        public string ModuleType { get; set; } // 模块类型：Pipette、Gripper、HeaterShaker、Magnetic、Temperature、Thermocycler
        public string Name { get; set; }       // 模块名称：如 single_channel、shaker_1 等
        public bool Enabled { get; set; }      // 是否启用
        public string Position { get; set; }   // 位置：如 P1
        public int? ChannelCount { get; set; } // 通道数（仅移液枪使用）
        public string Description { get; set; }
    }
}