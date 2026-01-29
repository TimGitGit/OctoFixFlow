using OctoFixFlow.Properties;
using System.ComponentModel;

namespace OctoFixFlow
{
    public class ResourceHelper : INotifyPropertyChanged
    {
        //private static readonly ResourceHelper _instance = new ResourceHelper();
        //public static ResourceHelper Instance => _instance;
        private static readonly Lazy<ResourceHelper> _lazyInstance = new Lazy<ResourceHelper>(() => new ResourceHelper());
        public static ResourceHelper Instance => _lazyInstance.Value;

        private bool _isEnglish;
        private ResourceHelper()
        {
            _isEnglish = Settings.Default.IsEnglish;
        }

        public bool IsEnglish
        {
            get => _isEnglish;
            set
            {
                if (_isEnglish != value)
                {
                    _isEnglish = value;
                    OnPropertyChanged(nameof(IsEnglish));
                    OnPropertyChanged("");// 通知所有属性更新
                    Settings.Default.IsEnglish = value;
                    Settings.Default.Save();
                }
            }
        }
        //动作区切换    全部/All
        public string WindowActionFunctionAll => IsEnglish ? Resource.Resource_en.WindowActionFunctionAll : Resource.Resource_zh.WindowActionFunctionAll;
        //基础/Basic
        public string WindowActionFunctionBasic => IsEnglish ? Resource.Resource_en.WindowActionFunctionBasic : Resource.Resource_zh.WindowActionFunctionBasic;
        //模块/Module
        public string WindowActionFunctionModule => IsEnglish ? Resource.Resource_en.WindowActionFunctionModule : Resource.Resource_zh.WindowActionFunctionModule;
        //其他/Other
        public string WindowActionFunctionOther => IsEnglish ? Resource.Resource_en.WindowActionFunctionOther : Resource.Resource_zh.WindowActionFunctionOther;
        public string WindowActionAspirate => IsEnglish ? Resource.Resource_en.WindowActionAspirate : Resource.Resource_zh.WindowActionAspirate;
        public string WindowActionDispense => IsEnglish ? Resource.Resource_en.WindowActionDispense : Resource.Resource_zh.WindowActionDispense;
        public string WindowActionFunctionArea => IsEnglish ? Resource.Resource_en.WindowActionFunctionArea : Resource.Resource_zh.WindowActionFunctionArea;
        public string WindowActionInit => IsEnglish ? Resource.Resource_en.WindowActionInit : Resource.Resource_zh.WindowActionInit;
        public string WindowActionLight => IsEnglish ? Resource.Resource_en.WindowActionLight : Resource.Resource_zh.WindowActionLight;
        public string WindowActionLoad => IsEnglish ? Resource.Resource_en.WindowActionLoad : Resource.Resource_zh.WindowActionLoad;
        public string WindowActionPause => IsEnglish ? Resource.Resource_en.WindowActionPause : Resource.Resource_zh.WindowActionPause;
        public string WindowActionStart => IsEnglish ? Resource.Resource_en.WindowActionStart : Resource.Resource_zh.WindowActionStart;
        public string WindowActionStop => IsEnglish ? Resource.Resource_en.WindowActionStop : Resource.Resource_zh.WindowActionStop;
        public string WindowActionTipOff => IsEnglish ? Resource.Resource_en.WindowActionTipOff : Resource.Resource_zh.WindowActionTipOff;
        public string WindowActionTipOn => IsEnglish ? Resource.Resource_en.WindowActionTipOn : Resource.Resource_zh.WindowActionTipOn;
        public string WindowActionUV => IsEnglish ? Resource.Resource_en.WindowActionUV : Resource.Resource_zh.WindowActionUV;
        //摄像头 Camera
        public string WindowActionCamera => IsEnglish ? Resource.Resource_en.WindowActionCamera : Resource.Resource_zh.WindowActionCamera;
        public string WindowActionWait => IsEnglish ? Resource.Resource_en.WindowActionWait : Resource.Resource_zh.WindowActionWait;
        //移载 /Transfer
        public string WindowActionTransfer => IsEnglish ? Resource.Resource_en.WindowActionTransfer : Resource.Resource_zh.WindowActionTransfer;
        //混合 /Mix
        public string WindowActionMix => IsEnglish ? Resource.Resource_en.WindowActionMix : Resource.Resource_zh.WindowActionMix;
        //振荡 /Shake
        public string WindowActionShake => IsEnglish ? Resource.Resource_en.WindowActionShake : Resource.Resource_zh.WindowActionShake;
        //磁吸 /Magnetic
        public string WindowActionMagnetic => IsEnglish ? Resource.Resource_en.WindowActionMagnetic : Resource.Resource_zh.WindowActionMagnetic;
        //温控 /Temp Ctrl
        public string WindowActionTemperature => IsEnglish ? Resource.Resource_en.WindowActionTemperature : Resource.Resource_zh.WindowActionTemperature;
        //热循环 /PCR
        public string WindowActionPCR => IsEnglish ? Resource.Resource_en.WindowActionPCR : Resource.Resource_zh.WindowActionPCR;
        //循环 /Loop
        public string WindowActionLoop => IsEnglish ? Resource.Resource_en.WindowActionLoop : Resource.Resource_zh.WindowActionLoop;
        //结束循环 /endLoop
        public string WindowActionEndLoop => IsEnglish ? Resource.Resource_en.WindowActionEndLoop : Resource.Resource_zh.WindowActionEndLoop;
        public string WindowBoardPosition => IsEnglish ? Resource.Resource_en.WindowBoardPosition : Resource.Resource_zh.WindowBoardPosition;
        public string WindowBoardToopTip => IsEnglish ? Resource.Resource_en.WindowBoardToopTip : Resource.Resource_zh.WindowBoardToopTip;
        public string WindowConsumablesWarehouse => IsEnglish ? Resource.Resource_en.WindowConsumablesWarehouse : Resource.Resource_zh.WindowConsumablesWarehouse;
        public string WindowLoginCancel => IsEnglish ? Resource.Resource_en.WindowLoginCancel : Resource.Resource_zh.WindowLoginCancel;
        public string WindowLoginOK => IsEnglish ? Resource.Resource_en.WindowLoginOK : Resource.Resource_zh.WindowLoginOK;
        //自动加载仪器已配置设备/Auto load preconfigured devices in the instrument
        public string WindowLoginCheck => IsEnglish ? Resource.Resource_en.WindowLoginCheck : Resource.Resource_zh.WindowLoginCheck;
        //记住用户名/Remember Username
        public string WindowLoginRememberUser => IsEnglish ? Resource.Resource_en.WindowLoginRememberUser : Resource.Resource_zh.WindowLoginRememberUser;
        public string WindowLoginPass => IsEnglish ? Resource.Resource_en.WindowLoginPass : Resource.Resource_zh.WindowLoginPass;
        public string WindowLoginPassTitle => IsEnglish ? Resource.Resource_en.WindowLoginPassTitle : Resource.Resource_zh.WindowLoginPassTitle;
        public string WindowLoginUser => IsEnglish ? Resource.Resource_en.WindowLoginUser : Resource.Resource_zh.WindowLoginUser;
        public string WindowLoginUserTitle => IsEnglish ? Resource.Resource_en.WindowLoginUserTitle : Resource.Resource_zh.WindowLoginUserTitle;
        public string WindowLogOut => IsEnglish ? Resource.Resource_en.WindowLogOut : Resource.Resource_zh.WindowLogOut;
        public string WindowOneclickdeletion => IsEnglish ? Resource.Resource_en.WindowOneclickdeletion : Resource.Resource_zh.WindowOneclickdeletion;
        public string WindowProcessList => IsEnglish ? Resource.Resource_en.WindowProcessList : Resource.Resource_zh.WindowProcessList;
        //切换  Toggle
        public string Windowrapidgenerating => IsEnglish ? Resource.Resource_en.Windowrapidgenerating : Resource.Resource_zh.Windowrapidgenerating;
        //设备运行信息	  Device Running Information
        public string RunningTitle => IsEnglish ? Resource.Resource_en.RunningTitle : Resource.Resource_zh.RunningTitle;
        //开始时间	  Start Time
        public string RunningStartTime => IsEnglish ? Resource.Resource_en.RunningStartTime : Resource.Resource_zh.RunningStartTime;
        //设备状态	  Device Status
        public string RunningDeviceStatus => IsEnglish ? Resource.Resource_en.RunningDeviceStatus : Resource.Resource_zh.RunningDeviceStatus;
        //运行时间	  Running Time
        public string RunningRunningTime => IsEnglish ? Resource.Resource_en.RunningRunningTime : Resource.Resource_zh.RunningRunningTime;
        //运行进度	  Running Progress
        public string RunningRunningProgress => IsEnglish ? Resource.Resource_en.RunningRunningProgress : Resource.Resource_zh.RunningRunningProgress;
        public string WindowStepdetails => IsEnglish ? Resource.Resource_en.WindowStepdetails : Resource.Resource_zh.WindowStepdetails;
        public string WindowTitle => IsEnglish ? Resource.Resource_en.WindowTitle : Resource.Resource_zh.WindowTitle;
        public string WindowUser => IsEnglish ? Resource.Resource_en.WindowUser : Resource.Resource_zh.WindowUser;
        public string WindowUserName => IsEnglish ? Resource.Resource_en.WindowUserName : Resource.Resource_zh.WindowUserName;
        public string WindowVersion => IsEnglish ? Resource.Resource_en.WindowVersion : Resource.Resource_zh.WindowVersion;
        // 运行信息（Running Info）
        public string WindowRunningMessage => IsEnglish ? Resource.Resource_en.WindowRunningMessage : Resource.Resource_zh.WindowRunningMessage;

        // 通信失败（gRPC communication failed）
        public string WindowGrpcComFail => IsEnglish ? Resource.Resource_en.WindowGrpcComFail : Resource.Resource_zh.WindowGrpcComFail;

        public string SettingTitle => IsEnglish ? Resource.Resource_en.SettingTitle : Resource.Resource_zh.SettingTitle;
        public string SettingConsTitile => IsEnglish ? Resource.Resource_en.SettingConsTitile : Resource.Resource_zh.SettingConsTitile;
        public string SettingPipeTitile => IsEnglish ? Resource.Resource_en.SettingPipeTitile : Resource.Resource_zh.SettingPipeTitile;
        public string SettingManualTitile => IsEnglish ? Resource.Resource_en.SettingManualTitile : Resource.Resource_zh.SettingManualTitile;
        public string SettingConsAdd => IsEnglish ? Resource.Resource_en.SettingConsAdd : Resource.Resource_zh.SettingConsAdd;
        public string SettingConsIn => IsEnglish ? Resource.Resource_en.SettingConsIn : Resource.Resource_zh.SettingConsIn;
        public string SettingConsOut => IsEnglish ? Resource.Resource_en.SettingConsOut : Resource.Resource_zh.SettingConsOut;
        public string SettingConsRemove => IsEnglish ? Resource.Resource_en.SettingConsRemove : Resource.Resource_zh.SettingConsRemove;
        public string SettingConsbasicInfo => IsEnglish ? Resource.Resource_en.SettingConsbasicInfo : Resource.Resource_zh.SettingConsbasicInfo;
        public string SettingConsName => IsEnglish ? Resource.Resource_en.SettingConsName : Resource.Resource_zh.SettingConsName;
        public string SettingConsSerialNum => IsEnglish ? Resource.Resource_en.SettingConsSerialNum : Resource.Resource_zh.SettingConsSerialNum;
        public string SettingConsType => IsEnglish ? Resource.Resource_en.SettingConsType : Resource.Resource_zh.SettingConsType;
        public string SettingConsDescription => IsEnglish ? Resource.Resource_en.SettingConsDescription : Resource.Resource_zh.SettingConsDescription;
        public string SettingCons2D => IsEnglish ? Resource.Resource_en.SettingCons2D : Resource.Resource_zh.SettingCons2D;
        public string SettingConsTypeMicroplate => IsEnglish ? Resource.Resource_en.SettingConsTypeMicroplate : Resource.Resource_zh.SettingConsTypeMicroplate;
        public string SettingConsTypeReservoir => IsEnglish ? Resource.Resource_en.SettingConsTypeReservoir : Resource.Resource_zh.SettingConsTypeReservoir;
        public string SettingConsTypeTipBox => IsEnglish ? Resource.Resource_en.SettingConsTypeTipBox : Resource.Resource_zh.SettingConsTypeTipBox;
        public string SettingConsTypeOther => IsEnglish ? Resource.Resource_en.SettingConsTypeOther : Resource.Resource_zh.SettingConsTypeOther;
        public string SettingConsNotchSetting => IsEnglish ? Resource.Resource_en.SettingConsNotchSetting : Resource.Resource_zh.SettingConsNotchSetting;
        public string SettingConsNotchPosition => IsEnglish ? Resource.Resource_en.SettingConsNotchPosition : Resource.Resource_zh.SettingConsNotchPosition;
        public string SettingConsNotchNW => IsEnglish ? Resource.Resource_en.SettingConsNotchNW : Resource.Resource_zh.SettingConsNotchNW;
        public string SettingConsNotchSW => IsEnglish ? Resource.Resource_en.SettingConsNotchSW : Resource.Resource_zh.SettingConsNotchSW;
        public string SettingConsNotchNE => IsEnglish ? Resource.Resource_en.SettingConsNotchNE : Resource.Resource_zh.SettingConsNotchNE;
        public string SettingConsNotchSE => IsEnglish ? Resource.Resource_en.SettingConsNotchSE : Resource.Resource_zh.SettingConsNotchSE;
        public string SettingConsDimension => IsEnglish ? Resource.Resource_en.SettingConsDimension : Resource.Resource_zh.SettingConsDimension;
        public string SettingConsNumWells => IsEnglish ? Resource.Resource_en.SettingConsNumWells : Resource.Resource_zh.SettingConsNumWells;
        public string SettingConsNumRows => IsEnglish ? Resource.Resource_en.SettingConsNumRows : Resource.Resource_zh.SettingConsNumRows;
        public string SettingConsNumColumns => IsEnglish ? Resource.Resource_en.SettingConsNumColumns : Resource.Resource_zh.SettingConsNumColumns;
        public string SettingConsLength => IsEnglish ? Resource.Resource_en.SettingConsLength : Resource.Resource_zh.SettingConsLength;
        public string SettingConsWidth => IsEnglish ? Resource.Resource_en.SettingConsWidth : Resource.Resource_zh.SettingConsWidth;
        public string SettingConsHeight => IsEnglish ? Resource.Resource_en.SettingConsHeight : Resource.Resource_zh.SettingConsHeight;
        public string SettingConsRowSpacing => IsEnglish ? Resource.Resource_en.SettingConsRowSpacing : Resource.Resource_zh.SettingConsRowSpacing;
        public string SettingConsColumnSpacing => IsEnglish ? Resource.Resource_en.SettingConsColumnSpacing : Resource.Resource_zh.SettingConsColumnSpacing;
        public string SettingConsA1XDistance => IsEnglish ? Resource.Resource_en.SettingConsA1XDistance : Resource.Resource_zh.SettingConsA1XDistance;
        public string SettingConsA1YDistance => IsEnglish ? Resource.Resource_en.SettingConsA1YDistance : Resource.Resource_zh.SettingConsA1YDistance;
        public string SettingConsOffsetGripperPos => IsEnglish ? Resource.Resource_en.SettingConsOffsetGripperPos : Resource.Resource_zh.SettingConsOffsetGripperPos;
        public string SettingConsOffsetX => IsEnglish ? Resource.Resource_en.SettingConsOffsetX : Resource.Resource_zh.SettingConsOffsetX;
        public string SettingConsOffsetY => IsEnglish ? Resource.Resource_en.SettingConsOffsetY : Resource.Resource_zh.SettingConsOffsetY;
        public string SettingConsGripperX => IsEnglish ? Resource.Resource_en.SettingConsGripperX : Resource.Resource_zh.SettingConsGripperX;
        public string SettingConsGripperY => IsEnglish ? Resource.Resource_en.SettingConsGripperY : Resource.Resource_zh.SettingConsGripperY;
        public string SettingConsGripperZ => IsEnglish ? Resource.Resource_en.SettingConsGripperZ : Resource.Resource_zh.SettingConsGripperZ;
        public string SettingConsWellSpec => IsEnglish ? Resource.Resource_en.SettingConsWellSpec : Resource.Resource_zh.SettingConsWellSpec;
        public string SettingConsMaxWellCapacity => IsEnglish ? Resource.Resource_en.SettingConsMaxWellCapacity : Resource.Resource_zh.SettingConsMaxWellCapacity;
        public string SettingConsMaxUsableVolume => IsEnglish ? Resource.Resource_en.SettingConsMaxUsableVolume : Resource.Resource_zh.SettingConsMaxUsableVolume;
        public string SettingConsWellDepth => IsEnglish ? Resource.Resource_en.SettingConsWellDepth : Resource.Resource_zh.SettingConsWellDepth;
        public string SettingConsTopShape => IsEnglish ? Resource.Resource_en.SettingConsTopShape : Resource.Resource_zh.SettingConsTopShape;
        public string SettingConsCylinder => IsEnglish ? Resource.Resource_en.SettingConsCylinder : Resource.Resource_zh.SettingConsCylinder;
        public string SettingConsCube => IsEnglish ? Resource.Resource_en.SettingConsCube : Resource.Resource_zh.SettingConsCube;
        public string SettingConsTopRadius => IsEnglish ? Resource.Resource_en.SettingConsTopRadius : Resource.Resource_zh.SettingConsTopRadius;
        public string SettingConsTopLength => IsEnglish ? Resource.Resource_en.SettingConsTopLength : Resource.Resource_zh.SettingConsTopLength;
        public string SettingConsTopWidth => IsEnglish ? Resource.Resource_en.SettingConsTopWidth : Resource.Resource_zh.SettingConsTopWidth;
        public string SettingConsPipetteTipParams => IsEnglish ? Resource.Resource_en.SettingConsPipetteTipParams : Resource.Resource_zh.SettingConsPipetteTipParams;
        public string SettingConsMaxCap => IsEnglish ? Resource.Resource_en.SettingConsMaxCap : Resource.Resource_zh.SettingConsMaxCap;
        public string SettingConsMaxUsableCap => IsEnglish ? Resource.Resource_en.SettingConsMaxUsableCap : Resource.Resource_zh.SettingConsMaxUsableCap;
        public string SettingConsTipTotalLgth => IsEnglish ? Resource.Resource_en.SettingConsTipTotalLgth : Resource.Resource_zh.SettingConsTipTotalLgth;
        public string SettingConsTipHeadHt => IsEnglish ? Resource.Resource_en.SettingConsTipHeadHt : Resource.Resource_zh.SettingConsTipHeadHt;
        public string SettingConsTipConeLgth => IsEnglish ? Resource.Resource_en.SettingConsTipConeLgth : Resource.Resource_zh.SettingConsTipConeLgth;
        public string SettingConsConeMaxRadius => IsEnglish ? Resource.Resource_en.SettingConsConeMaxRadius : Resource.Resource_zh.SettingConsConeMaxRadius;
        public string SettingConsConeMinRadius => IsEnglish ? Resource.Resource_en.SettingConsConeMinRadius : Resource.Resource_zh.SettingConsConeMinRadius;
        public string SettingConsTipMountPressDpth => IsEnglish ? Resource.Resource_en.SettingConsTipMountPressDpth : Resource.Resource_zh.SettingConsTipMountPressDpth;
        public string SettingLiquidAdd => IsEnglish ? Resource.Resource_en.SettingLiquidAdd : Resource.Resource_zh.SettingLiquidAdd;
        public string SettingLiquidIn => IsEnglish ? Resource.Resource_en.SettingLiquidIn : Resource.Resource_zh.SettingLiquidIn;
        public string SettingLiquidOut => IsEnglish ? Resource.Resource_en.SettingLiquidOut : Resource.Resource_zh.SettingLiquidOut;
        public string SettingLiquidRemove => IsEnglish ? Resource.Resource_en.SettingLiquidRemove : Resource.Resource_zh.SettingLiquidRemove;
        public string SettingLiquidbasicInfo => IsEnglish ? Resource.Resource_en.SettingLiquidbasicInfo : Resource.Resource_zh.SettingLiquidbasicInfo;
        public string SettingLiquidName => IsEnglish ? Resource.Resource_en.SettingLiquidName : Resource.Resource_zh.SettingLiquidName;
        public string SettingLiquidDescription => IsEnglish ? Resource.Resource_en.SettingLiquidDescription : Resource.Resource_zh.SettingLiquidDescription;
        public string SettingLiquidAspirationParams => IsEnglish ? Resource.Resource_en.SettingLiquidAspirationParams : Resource.Resource_zh.SettingLiquidAspirationParams;
        public string SettingLiquidPreAspAir => IsEnglish ? Resource.Resource_en.SettingLiquidPreAspAir : Resource.Resource_zh.SettingLiquidPreAspAir;
        public string SettingLiquidPostAspAir => IsEnglish ? Resource.Resource_en.SettingLiquidPostAspAir : Resource.Resource_zh.SettingLiquidPostAspAir;
        public string SettingLiquidAspSpeed => IsEnglish ? Resource.Resource_en.SettingLiquidAspSpeed : Resource.Resource_zh.SettingLiquidAspSpeed;
        public string SettingLiquidAspDelay => IsEnglish ? Resource.Resource_en.SettingLiquidAspDelay : Resource.Resource_zh.SettingLiquidAspDelay;
        public string SettingLiquidDisttoWellBot => IsEnglish ? Resource.Resource_en.SettingLiquidDisttoWellBot : Resource.Resource_zh.SettingLiquidDisttoWellBot;
        public string SettingLiquidDispensingParams => IsEnglish ? Resource.Resource_en.SettingLiquidDispensingParams : Resource.Resource_zh.SettingLiquidDispensingParams;
        public string SettingLiquidPreDispAir => IsEnglish ? Resource.Resource_en.SettingLiquidPreDispAir : Resource.Resource_zh.SettingLiquidPreDispAir;
        public string SettingLiquidPostDispAir => IsEnglish ? Resource.Resource_en.SettingLiquidPostDispAir : Resource.Resource_zh.SettingLiquidPostDispAir;
        public string SettingLiquidDispSpeed => IsEnglish ? Resource.Resource_en.SettingLiquidDispSpeed : Resource.Resource_zh.SettingLiquidDispSpeed;
        public string SettingLiquidDispDelay => IsEnglish ? Resource.Resource_en.SettingLiquidDispDelay : Resource.Resource_zh.SettingLiquidDispDelay;
        public string SettingManualControlCenter => IsEnglish ? Resource.Resource_en.SettingManualControlCenter : Resource.Resource_zh.SettingManualControlCenter;
        public string SettingManualPlatePositionCalibration => IsEnglish ? Resource.Resource_en.SettingManualPlatePositionCalibration : Resource.Resource_zh.SettingManualPlatePositionCalibration;
        public string SettingManualSelectPlatePosition => IsEnglish ? Resource.Resource_en.SettingManualSelectPlatePosition : Resource.Resource_zh.SettingManualSelectPlatePosition;
        public string SettingManualCurrentPlateCoordinates => IsEnglish ? Resource.Resource_en.SettingManualCurrentPlateCoordinates : Resource.Resource_zh.SettingManualCurrentPlateCoordinates;
        //板位信息获取 Retrieve plate position information
        public string SettingManualGetPlate => IsEnglish ? Resource.Resource_en.SettingManualGetPlate : Resource.Resource_zh.SettingManualGetPlate;
        //板位信息获取成功 Successfully retrieved plate position data
        public string SettingManualGetPlateSucc => IsEnglish ? Resource.Resource_en.SettingManualGetPlateSucc : Resource.Resource_zh.SettingManualGetPlateSucc;
        //板位信息获取失败 Failed to get plate position info
        public string SettingManualGetPlateFail => IsEnglish ? Resource.Resource_en.SettingManualGetPlateFail : Resource.Resource_zh.SettingManualGetPlateFail;
        //请先选择板位 Please select the plate position first
        public string SettingManualSetPlateNull => IsEnglish ? Resource.Resource_en.SettingManualSetPlateNull : Resource.Resource_zh.SettingManualSetPlateNull;
        //板位信息保存成功 Successfully saved plate position data
        public string SettingManualSetPlateSucc => IsEnglish ? Resource.Resource_en.SettingManualSetPlateSucc : Resource.Resource_zh.SettingManualSetPlateSucc;
        //板位信息保存失败 Failed to set plate position info
        public string SettingManualSetPlateFail => IsEnglish ? Resource.Resource_en.SettingManualSetPlateFail : Resource.Resource_zh.SettingManualSetPlateFail;
        public string SettingManualXCoordinate => IsEnglish ? Resource.Resource_en.SettingManualXCoordinate : Resource.Resource_zh.SettingManualXCoordinate;
        public string SettingManualYCoordinate => IsEnglish ? Resource.Resource_en.SettingManualYCoordinate : Resource.Resource_zh.SettingManualYCoordinate;
        public string SettingManualZCoordinate => IsEnglish ? Resource.Resource_en.SettingManualZCoordinate : Resource.Resource_zh.SettingManualZCoordinate;
        public string SettingManualMovetoPlatePosition => IsEnglish ? Resource.Resource_en.SettingManualMovetoPlatePosition : Resource.Resource_zh.SettingManualMovetoPlatePosition;
        public string SettingManualMoveZaxis => IsEnglish ? Resource.Resource_en.SettingManualMoveZaxis : Resource.Resource_zh.SettingManualMoveZaxis;
        public string SettingManualSaveCoordinates => IsEnglish ? Resource.Resource_en.SettingManualSaveCoordinates : Resource.Resource_zh.SettingManualSaveCoordinates;
        public string SettingManualResetX => IsEnglish ? Resource.Resource_en.SettingManualResetX : Resource.Resource_zh.SettingManualResetX;
        public string SettingManualResetY => IsEnglish ? Resource.Resource_en.SettingManualResetY : Resource.Resource_zh.SettingManualResetY;
        public string SettingManualResetZ => IsEnglish ? Resource.Resource_en.SettingManualResetZ : Resource.Resource_zh.SettingManualResetZ;
        //设备X轴复位 Reset the X-axis of the equipment
        public string SettingManualStartResetX => IsEnglish ? Resource.Resource_en.SettingManualStartResetX : Resource.Resource_zh.SettingManualStartResetX;
        //设备Y轴复位 Reset the Y-axis of the equipment
        public string SettingManualStartResetY => IsEnglish ? Resource.Resource_en.SettingManualStartResetY : Resource.Resource_zh.SettingManualStartResetY;
        //设备Z轴复位 Reset the Z-axis of the equipment
        public string SettingManualStartResetZ => IsEnglish ? Resource.Resource_en.SettingManualStartResetZ : Resource.Resource_zh.SettingManualStartResetZ;
        //设备复位成功 Device reset successful
        public string SettingManualResetSucc => IsEnglish ? Resource.Resource_en.SettingManualResetSucc : Resource.Resource_zh.SettingManualResetSucc;
        public string SettingManualCoordinateFineAdjustment => IsEnglish ? Resource.Resource_en.SettingManualCoordinateFineAdjustment : Resource.Resource_zh.SettingManualCoordinateFineAdjustment;
        //方向控制
        public string SettingManualCoordinateDirectionControl => IsEnglish ? Resource.Resource_en.SettingManualCoordinateDirectionControl : Resource.Resource_zh.SettingManualCoordinateDirectionControl;
        //微小
        public string SettingManualTinySize => IsEnglish ? Resource.Resource_en.SettingManualTinySize : Resource.Resource_zh.SettingManualTinySize;
        //小
        public string SettingManualSmallSize => IsEnglish ? Resource.Resource_en.SettingManualSmallSize : Resource.Resource_zh.SettingManualSmallSize;
        //大
        public string SettingManualBigSize => IsEnglish ? Resource.Resource_en.SettingManualBigSize : Resource.Resource_zh.SettingManualBigSize;
        //X和Y方向
        public string SettingManualXAndY => IsEnglish ? Resource.Resource_en.SettingManualXAndY : Resource.Resource_zh.SettingManualXAndY;
        //Ｚ方向　Z－axis
        public string SettingManualZAxis => IsEnglish ? Resource.Resource_en.SettingManualZAxis : Resource.Resource_zh.SettingManualZAxis;
        //Ｚ2方向　Z2－axis
        public string SettingManualZ2Axis => IsEnglish ? Resource.Resource_en.SettingManualZ2Axis : Resource.Resource_zh.SettingManualZ2Axis;
        //Ｚ3方向　Z3－axis
        public string SettingManualZ3Axis => IsEnglish ? Resource.Resource_en.SettingManualZ3Axis : Resource.Resource_zh.SettingManualZ3Axis;
        //设备移动成功　Arm moved successfully
        public string SettingManualMoveSucc => IsEnglish ? Resource.Resource_en.SettingManualMoveSucc : Resource.Resource_zh.SettingManualMoveSucc;
        //设备移动失败　Arm movement failed
        public string SettingManualMoveFail => IsEnglish ? Resource.Resource_en.SettingManualMoveFail : Resource.Resource_zh.SettingManualMoveFail;
        //数据格式错误　Data format error
        public string SettingManualMoveCoordinate => IsEnglish ? Resource.Resource_en.SettingManualMoveCoordinate : Resource.Resource_zh.SettingManualMoveCoordinate;
        //方向键
        public string SettingManualArrowKeys => IsEnglish ? Resource.Resource_en.SettingManualArrowKeys : Resource.Resource_zh.SettingManualArrowKeys;
        //Shift＋方向键
        public string SettingManualSArrowKeys => IsEnglish ? Resource.Resource_en.SettingManualSArrowKeys : Resource.Resource_zh.SettingManualSArrowKeys;
        public string SettingManualPipetteControl => IsEnglish ? Resource.Resource_en.SettingManualPipetteControl : Resource.Resource_zh.SettingManualPipetteControl;
        public string SettingManualAspirationParams => IsEnglish ? Resource.Resource_en.SettingManualAspirationParams : Resource.Resource_zh.SettingManualAspirationParams;
        public string SettingManualVolume => IsEnglish ? Resource.Resource_en.SettingManualVolume : Resource.Resource_zh.SettingManualVolume;
        public string SettingManualSpeed => IsEnglish ? Resource.Resource_en.SettingManualSpeed : Resource.Resource_zh.SettingManualSpeed;
        public string SettingManualAspirate => IsEnglish ? Resource.Resource_en.SettingManualAspirate : Resource.Resource_zh.SettingManualAspirate;
        public string SettingManualDispensingParams => IsEnglish ? Resource.Resource_en.SettingManualDispensingParams : Resource.Resource_zh.SettingManualDispensingParams;
        public string SettingManualDispense => IsEnglish ? Resource.Resource_en.SettingManualDispense : Resource.Resource_zh.SettingManualDispense;
        public string SettingManualEjectTip => IsEnglish ? Resource.Resource_en.SettingManualEjectTip : Resource.Resource_zh.SettingManualEjectTip;
        public string SettingManualReset => IsEnglish ? Resource.Resource_en.SettingManualReset : Resource.Resource_zh.SettingManualReset;
        public string SettingManualCalibrationGear => IsEnglish ? Resource.Resource_en.SettingManualCalibrationGear : Resource.Resource_zh.SettingManualCalibrationGear;
        public string SettingManualGetCalibration => IsEnglish ? Resource.Resource_en.SettingManualGetCalibration : Resource.Resource_zh.SettingManualGetCalibration;
        public string SettingManualSetCalibration => IsEnglish ? Resource.Resource_en.SettingManualSetCalibration : Resource.Resource_zh.SettingManualSetCalibration;
        public string SettingManualBacklash => IsEnglish ? Resource.Resource_en.SettingManualBacklash : Resource.Resource_zh.SettingManualBacklash;
        //抓手控制/Gripper control
        public string SettingManualGripperControl => IsEnglish ? Resource.Resource_en.SettingManualGripperControl : Resource.Resource_zh.SettingManualGripperControl;
        // 打开抓手 (Open the gripper）
        public string SettingManualGripperOpen => IsEnglish ? Resource.Resource_en.SettingManualGripperOpen : Resource.Resource_zh.SettingManualGripperOpen;
        // 关闭抓手 (Close the gripper）
        public string SettingManualGripperClose => IsEnglish ? Resource.Resource_en.SettingManualGripperClose : Resource.Resource_zh.SettingManualGripperClose;
        //磁吸控制/Magnetic control
        public string SettingManualMagneticControl => IsEnglish ? Resource.Resource_en.SettingManualMagneticControl : Resource.Resource_zh.SettingManualMagneticControl;
        // 上升磁吸 (Up the magnetic）
        public string SettingManualMagneticUp => IsEnglish ? Resource.Resource_en.SettingManualMagneticUp : Resource.Resource_zh.SettingManualMagneticUp;
        // 下降磁吸 (Down the mnetic）
        public string SettingManualMagneticDown => IsEnglish ? Resource.Resource_en.SettingManualMagneticDown : Resource.Resource_zh.SettingManualMagneticDown;
        //加热振荡控制/Shaker control
        public string SettingManualShakerControl => IsEnglish ? Resource.Resource_en.SettingManualShakerControl : Resource.Resource_zh.SettingManualShakerControl;
        //温度控制/Temp control
        public string SettingManualShakerTempControl => IsEnglish ? Resource.Resource_en.SettingManualShakerTempControl : Resource.Resource_zh.SettingManualShakerTempControl;
        //当前温度（℃）：/Current Temperature (℃):
        public string SettingManualShakerTempCurrent => IsEnglish ? Resource.Resource_en.SettingManualShakerTempCurrent : Resource.Resource_zh.SettingManualShakerTempCurrent;
        //设定温度（℃）：/Set Temperature (℃):
        public string SettingManualShakerTempSet => IsEnglish ? Resource.Resource_en.SettingManualShakerTempSet : Resource.Resource_zh.SettingManualShakerTempSet;
        //开始温度/Start Temperature
        public string SettingManualStartTemperature => IsEnglish ? Resource.Resource_en.SettingManualStartTemperature : Resource.Resource_zh.SettingManualStartTemperature;
        //停止温度/Stop Temperature
        public string SettingManualStopTemperature => IsEnglish ? Resource.Resource_en.SettingManualStopTemperature : Resource.Resource_zh.SettingManualStopTemperature;
        //振荡控制/Shaking Control	
        public string SettingManualShakerShakingControl => IsEnglish ? Resource.Resource_en.SettingManualShakerShakingControl : Resource.Resource_zh.SettingManualShakerShakingControl;
        //当前转速（RPM）：/Current Speed (RPM):
        public string SettingManualShakerShakingCurrent => IsEnglish ? Resource.Resource_en.SettingManualShakerShakingCurrent : Resource.Resource_zh.SettingManualShakerShakingCurrent;
        //设定转速（RPM）：/Set Speed (RPM):
        public string SettingManualShakerShakingSet => IsEnglish ? Resource.Resource_en.SettingManualShakerShakingSet : Resource.Resource_zh.SettingManualShakerShakingSet;
        //设定时间（秒）：/Set Time (S):
        public string SettingManualShakerShakingTimeSet => IsEnglish ? Resource.Resource_en.SettingManualShakerShakingTimeSet : Resource.Resource_zh.SettingManualShakerShakingTimeSet;
        //开始振荡/Start Shaking
        public string SettingManualStartShaking => IsEnglish ? Resource.Resource_en.SettingManualStartShaking : Resource.Resource_zh.SettingManualStartShaking;
        //停止振荡/Stop Shaking
        public string SettingManualStopShaking => IsEnglish ? Resource.Resource_en.SettingManualStopShaking : Resource.Resource_zh.SettingManualStopShaking;
        //PCR控制/PCR control
        public string SettingManualPCRControl => IsEnglish ? Resource.Resource_en.SettingManualPCRControl : Resource.Resource_zh.SettingManualPCRControl;
        //运行脚本/Run Script
        public string SettingManualPCRScriptRun => IsEnglish ? Resource.Resource_en.SettingManualPCRScriptRun : Resource.Resource_zh.SettingManualPCRScriptRun;
        //开始运行/Start Run
        public string SettingManualPCRStart => IsEnglish ? Resource.Resource_en.SettingManualPCRStart : Resource.Resource_zh.SettingManualPCRStart;
        //停止运行/Stop Run
        public string SettingManualPCRStop => IsEnglish ? Resource.Resource_en.SettingManualPCRStop : Resource.Resource_zh.SettingManualPCRStop;
        //开盖/Open Lid
        public string SettingManualPCROpen => IsEnglish ? Resource.Resource_en.SettingManualPCROpen : Resource.Resource_zh.SettingManualPCROpen;
        //关盖/Close Lid
        public string SettingManualPCRClose => IsEnglish ? Resource.Resource_en.SettingManualPCRClose : Resource.Resource_zh.SettingManualPCRClose;
        //脚本选择/Script Selection
        public string SettingManualPCRSelection => IsEnglish ? Resource.Resource_en.SettingManualPCRSelection : Resource.Resource_zh.SettingManualPCRSelection;
        public string SettingManualClose => IsEnglish ? Resource.Resource_en.SettingManualClose : Resource.Resource_zh.SettingManualClose;
        public string QuickTitile => IsEnglish ? Resource.Resource_en.QuickTitile : Resource.Resource_zh.QuickTitile;
        public string QuickSampleQty => IsEnglish ? Resource.Resource_en.QuickSampleQty : Resource.Resource_zh.QuickSampleQty;
        public string QuickTipPickPos => IsEnglish ? Resource.Resource_en.QuickTipPickPos : Resource.Resource_zh.QuickTipPickPos;
        public string QuickFixed => IsEnglish ? Resource.Resource_en.QuickFixed : Resource.Resource_zh.QuickFixed;
        public string QuickAspPos => IsEnglish ? Resource.Resource_en.QuickAspPos : Resource.Resource_zh.QuickAspPos;
        public string QuickDispPos => IsEnglish ? Resource.Resource_en.QuickDispPos : Resource.Resource_zh.QuickDispPos;
        public string QuickTipChangeSettings => IsEnglish ? Resource.Resource_en.QuickTipChangeSettings : Resource.Resource_zh.QuickTipChangeSettings;
        #region 手动控制-模块添加
        //设备与模块管理  Device and Module Management
        public string SettingManualDeviceModuleManagement => IsEnglish ? Resource.Resource_en.SettingManualDeviceModuleManagement : Resource.Resource_zh.SettingManualDeviceModuleManagement;
        //移液器  Pipettes
        public string SettingManualPipettes => IsEnglish ? Resource.Resource_en.SettingManualPipettes : Resource.Resource_zh.SettingManualPipettes;
        //单通道  Single Channel
        public string SettingManualSingleChannel => IsEnglish ? Resource.Resource_en.SettingManualSingleChannel : Resource.Resource_zh.SettingManualSingleChannel;
        //八通道  Eight Channel
        public string SettingManualEightChannel => IsEnglish ? Resource.Resource_en.SettingManualEightChannel : Resource.Resource_zh.SettingManualEightChannel;
        //96通道  96-Channel
        public string SettingManualNineSixChannel => IsEnglish ? Resource.Resource_en.SettingManualNineSixChannel : Resource.Resource_zh.SettingManualNineSixChannel;
        //抓手  Gripper
        public string SettingManualGripper => IsEnglish ? Resource.Resource_en.SettingManualGripper : Resource.Resource_zh.SettingManualGripper;
        //功能模块  Functional Modules
        public string SettingManualModules => IsEnglish ? Resource.Resource_en.SettingManualModules : Resource.Resource_zh.SettingManualModules;
        //加热振荡模块  Heating & Shaking Module
        public string SettingManualHeatingShaking => IsEnglish ? Resource.Resource_en.SettingManualHeatingShaking : Resource.Resource_zh.SettingManualHeatingShaking;
        //磁吸模块  Magnetic Module
        public string SettingManualMagnetic => IsEnglish ? Resource.Resource_en.SettingManualMagnetic : Resource.Resource_zh.SettingManualMagnetic;
        // 温控模块 Temperature Control Module
        public string SettingManualTemperatureControl => IsEnglish ? Resource.Resource_en.SettingManualTemperatureControl : Resource.Resource_zh.SettingManualTemperatureControl;
        //热循环模块  PCR Module
        public string SettingManualPCR => IsEnglish ? Resource.Resource_en.SettingManualPCR : Resource.Resource_zh.SettingManualPCR;
        //是否启用抓手？  Enable gripper?
        public string SettingManualEnableGripper => IsEnglish ? Resource.Resource_en.SettingManualEnableGripper : Resource.Resource_zh.SettingManualEnableGripper;
        //是否启用PCR？  Enable Thermocycler Module??
        public string SettingManualEnablePCR => IsEnglish ? Resource.Resource_en.SettingManualEnablePCR : Resource.Resource_zh.SettingManualEnablePCR;
        //是否启用垃圾桶？  Enable labware waste bin?
        public string SettingManualEnableTrash => IsEnglish ? Resource.Resource_en.SettingManualEnableTrash : Resource.Resource_zh.SettingManualEnableTrash;
        //确认  Confirm
        public string SettingConfirm => IsEnglish ? Resource.Resource_en.SettingConfirm : Resource.Resource_zh.SettingConfirm;
        #endregion
        #region 引导模块
        // 设备与协议引导设置  Device & Protocol Guided Setup
        public string GuideTitle => IsEnglish ? Resource.Resource_en.GuideTitle : Resource.Resource_zh.GuideTitle;
        // 设备基础配置  Basic Device Configuration
        public string GuideBtnDeviceConfig => IsEnglish ? Resource.Resource_en.GuideBtnDeviceConfig : Resource.Resource_zh.GuideBtnDeviceConfig;
        // 板位布局配置  Deck Layout Configuration
        public string GuideBtnDeckLayout => IsEnglish ? Resource.Resource_en.GuideBtnDeckLayout : Resource.Resource_zh.GuideBtnDeckLayout;
        // 实验协议信息  Experiment Protocol Info
        public string GuideBtnProtocolInfo => IsEnglish ? Resource.Resource_en.GuideBtnProtocolInfo : Resource.Resource_zh.GuideBtnProtocolInfo;
        // 让我们从基础开始吧  Let’s start with the basics
        public string GuideBtnDeviceStart => IsEnglish ? Resource.Resource_en.GuideBtnDeviceStart : Resource.Resource_zh.GuideBtnDeviceStart;
        // 添加您的移液器  Add your pipettes
        public string GuideBtnDeviceAddPipettes => IsEnglish ? Resource.Resource_en.GuideBtnDeviceAddPipettes : Resource.Resource_zh.GuideBtnDeviceAddPipettes;
        // 精准吸液分液，体积实验核心。  Precise aspiration/dispensing, core for volume-accurate experiments.
        public string GuideBtnDeviceAddPipettesToolTip => IsEnglish ? Resource.Resource_en.GuideBtnDeviceAddPipettesToolTip : Resource.Resource_zh.GuideBtnDeviceAddPipettesToolTip;
        // 添加您的抓手  Add your gripper
        public string GuideBtnDeviceAddGripper => IsEnglish ? Resource.Resource_en.GuideBtnDeviceAddGripper : Resource.Resource_zh.GuideBtnDeviceAddGripper;
        // 自动移载耗材，容器转移必备。  Grippers automate labware movement. Necessary for modules with automated container transfer.
        public string GuideBtnDeviceAddGripperToolTip => IsEnglish ? Resource.Resource_en.GuideBtnDeviceAddGripperToolTip : Resource.Resource_zh.GuideBtnDeviceAddGripperToolTip;
        // 添加您的PCR模块  Add your PCR module
        public string GuideBtnDeviceAddPCRModule => IsEnglish ? Resource.Resource_en.GuideBtnDeviceAddPCRModule : Resource.Resource_zh.GuideBtnDeviceAddPCRModule;
        // 基因扩增核心，热循环控制。  Core for gene amplification & thermal cycling.
        public string GuideBtnDeviceAddPCRModuleToolTip => IsEnglish ? Resource.Resource_en.GuideBtnDeviceAddPCRModuleToolTip : Resource.Resource_zh.GuideBtnDeviceAddPCRModuleToolTip;
        // 添加您的耗材回收桶  Add your labware waste bin
        public string GuideBtnDeviceAddWasteBin => IsEnglish ? Resource.Resource_en.GuideBtnDeviceAddWasteBin : Resource.Resource_zh.GuideBtnDeviceAddWasteBin;
        // 收纳废弃耗材，保持实验整洁。  Store waste consumables, keep lab tidy.
        public string GuideBtnDeviceAddWasteBinToolTip => IsEnglish ? Resource.Resource_en.GuideBtnDeviceAddWasteBinToolTip : Resource.Resource_zh.GuideBtnDeviceAddWasteBinToolTip;
        // 配置您的工作站模块  Configure Your Workstation Modules
        public string GuideBtnDeckLayoutTitile => IsEnglish ? Resource.Resource_en.GuideBtnDeckLayoutTitile : Resource.Resource_zh.GuideBtnDeckLayoutTitile;
        // 模块列表  Module List
        public string GuideBtnDeckLayoutModuleList => IsEnglish ? Resource.Resource_en.GuideBtnDeckLayoutModuleList : Resource.Resource_zh.GuideBtnDeckLayoutModuleList;
        // 实验方案信息说明  Specify Your Experiment Protocol Info
        public string GuideBtnExperimentProtocolInfoTitile => IsEnglish ? Resource.Resource_en.GuideBtnExperimentProtocolInfoTitile : Resource.Resource_zh.GuideBtnExperimentProtocolInfoTitile;
        // 名称  Name
        public string GuideBtnExperimentProtocolInfoName => IsEnglish ? Resource.Resource_en.GuideBtnExperimentProtocolInfoName : Resource.Resource_zh.GuideBtnExperimentProtocolInfoName;
        // 描述  Description
        public string GuideBtnExperimentProtocolInfoDescription => IsEnglish ? Resource.Resource_en.GuideBtnExperimentProtocolInfoDescription : Resource.Resource_zh.GuideBtnExperimentProtocolInfoDescription;
        // 作者/机构 Author/Organization
        public string GuideBtnExperimentProtocolInfoAuthor => IsEnglish ? Resource.Resource_en.GuideBtnExperimentProtocolInfoAuthor : Resource.Resource_zh.GuideBtnExperimentProtocolInfoAuthor;
        #endregion

        public string QuickTipChange => IsEnglish ? Resource.Resource_en.QuickTipChange : Resource.Resource_zh.QuickTipChange;
        public string QuickTipEjectPos => IsEnglish ? Resource.Resource_en.QuickTipEjectPos : Resource.Resource_zh.QuickTipEjectPos;
        public string QuickLiquidSelection => IsEnglish ? Resource.Resource_en.QuickLiquidSelection : Resource.Resource_zh.QuickLiquidSelection;
        public string QuickGenerate => IsEnglish ? Resource.Resource_en.QuickGenerate : Resource.Resource_zh.QuickGenerate;
        public string QuickColumn => IsEnglish ? Resource.Resource_en.QuickColumn : Resource.Resource_zh.QuickColumn;
        public string FlowStepSteps => IsEnglish ? Resource.Resource_en.FlowStepSteps : Resource.Resource_zh.FlowStepSteps;
        public string FlowStepStart => IsEnglish ? Resource.Resource_en.FlowStepStart : Resource.Resource_zh.FlowStepStart;
        public string FlowStepEnd => IsEnglish ? Resource.Resource_en.FlowStepEnd : Resource.Resource_zh.FlowStepEnd;
        public string FlowStepWaitContent => IsEnglish ? Resource.Resource_en.FlowStepWaitContent : Resource.Resource_zh.FlowStepWaitContent;
        #region 步骤详情 - 标题类
        // 步骤详情标题后缀（如“Aspirate details”→“details”/“吸液详情”→“详情”）
        public string StepDetailDetails => IsEnglish ? Resource.Resource_en.StepDetailDetails : Resource.Resource_zh.StepDetailDetails;
        // 混合设置标题（“Mixed settings:”/“混合设置：”）
        public string StepDetailMixedSettings => IsEnglish ? Resource.Resource_en.StepDetailMixedSettings : Resource.Resource_zh.StepDetailMixedSettings;
        // 液体参数标题（“Liquid parameters:”/“液体参数：”）
        public string StepDetailLiquidParams => IsEnglish ? Resource.Resource_en.StepDetailLiquidParams : Resource.Resource_zh.StepDetailLiquidParams;
        // 吸液参数组标题（“Aspiration Parameters:”/“吸液参数：”）
        public string StepDetailAspirationParams => IsEnglish ? Resource.Resource_en.StepDetailAspirationParams : Resource.Resource_zh.StepDetailAspirationParams;
        // 注液参数组标题（“Dispensing Parameters:”/“注液参数：”）
        public string StepDetailDispensingParams => IsEnglish ? Resource.Resource_en.StepDetailDispensingParams : Resource.Resource_zh.StepDetailDispensingParams;
        // 孔位选择区标题（“Well Position Selection Area:”/“孔位选择区：”）
        public string StepDetailWellSelectionArea => IsEnglish ? Resource.Resource_en.StepDetailWellSelectionArea : Resource.Resource_zh.StepDetailWellSelectionArea;
        #endregion

        #region 步骤详情 - 标签类
        //列前缀（多语言：中文“列：” / 英文“Column: ”）
        public string StepDetailColumnPrefix => IsEnglish ? Resource.Resource_en.StepDetailColumnPrefix : Resource.Resource_zh.StepDetailColumnPrefix;
        //行前缀（多语言：中文“行：” / 英文“Row: ”）
        public string StepDetailRowPrefix => IsEnglish ? Resource.Resource_en.StepDetailRowPrefix : Resource.Resource_zh.StepDetailRowPrefix;
        // "选中移液器" / "Selected Pipette"
        public string StepDetailSelectedPipette => IsEnglish ? Resource.Resource_en.StepDetailSelectedPipette : Resource.Resource_zh.StepDetailSelectedPipette;

        // 操作位置（“Operation Position:”/“操作位置：”）
        public string StepDetailOperationPosition => IsEnglish ? Resource.Resource_en.StepDetailOperationPosition : Resource.Resource_zh.StepDetailOperationPosition;
        // 孔位选择（“Well Position Selection:”/“孔位选择：”）
        public string StepDetailWellPosition => IsEnglish ? Resource.Resource_en.StepDetailWellPosition : Resource.Resource_zh.StepDetailWellPosition;
        // 体积（“Volume(ul):”/“体积(ul)：”）
        public string StepDetailVolume => IsEnglish ? Resource.Resource_en.StepDetailVolume : Resource.Resource_zh.StepDetailVolume;
        // 混合次数（“Mixing Times:”/“混合次数：”）
        public string StepDetailMixCount => IsEnglish ? Resource.Resource_en.StepDetailMixCount : Resource.Resource_zh.StepDetailMixCount;
        // 混合体积（“Mixing Volumes(ul):”/“混合体积(ul)：”）
        public string StepDetailMixVolume => IsEnglish ? Resource.Resource_en.StepDetailMixVolume : Resource.Resource_zh.StepDetailMixVolume;
        // 最后一轮混吸参数（“Last Mix Parameter”）
        public string StepDetailMixFinalCheck => IsEnglish ? Resource.Resource_en.StepDetailMixFinalCheck : Resource.Resource_zh.StepDetailMixFinalCheck;
        // 时间（“Time (seconds):”/“时间(秒)：”）
        public string StepDetailWaitTime => IsEnglish ? Resource.Resource_en.StepDetailWaitTime : Resource.Resource_zh.StepDetailWaitTime;
        // 等待描述（“Waiting description:”/“等待描述：”）
        public string StepDetailWaitDesc => IsEnglish ? Resource.Resource_en.StepDetailWaitDesc : Resource.Resource_zh.StepDetailWaitDesc;
        // 启用混合（CheckBox内容：“Enable mixing”/“启用混合”）
        public string StepDetailEnableMix => IsEnglish ? Resource.Resource_en.StepDetailEnableMix : Resource.Resource_zh.StepDetailEnableMix;
        // 转速（“Speed (RPM):”/“转速(RPM)：”）
        public string StepDetailShakeSpeed => IsEnglish ? Resource.Resource_en.StepDetailShakeSpeed : Resource.Resource_zh.StepDetailShakeSpeed;
        // 温度（“Temp (℃):”/“温度(℃)：”）
        public string StepDetailShakeTemp => IsEnglish ? Resource.Resource_en.StepDetailShakeTemp : Resource.Resource_zh.StepDetailShakeTemp;
        // 是否预热（“启用预热”/“Enable Pre-heat”）
        public string StepDetailShakePreHeat => IsEnglish ? Resource.Resource_en.StepDetailShakePreHeat : Resource.Resource_zh.StepDetailShakePreHeat;
        // （“振荡结束前解锁下一步运动”/“Unlock next step before shaking finished”）
        public string StepDetailShakeUnlockNext => IsEnglish ? Resource.Resource_en.StepDetailShakeUnlockNext : Resource.Resource_zh.StepDetailShakeUnlockNext;
        // （“磁吸动作”/“Magnet Action”）
        public string StepDetailMagnetLiftDrop => IsEnglish ? Resource.Resource_en.StepDetailMagnetLiftDrop : Resource.Resource_zh.StepDetailMagnetLiftDrop;
        // （“上升”/“Up”）
        public string StepDetailMagnetUp => IsEnglish ? Resource.Resource_en.StepDetailMagnetUp : Resource.Resource_zh.StepDetailMagnetUp;
        // （“下降”/“Down”）
        public string StepDetailMagnetDown => IsEnglish ? Resource.Resource_en.StepDetailMagnetDown : Resource.Resource_zh.StepDetailMagnetDown;
        // （“磁吸距离”/“Magnet Distance”）
        public string StepDetailMagnetDistance => IsEnglish ? Resource.Resource_en.StepDetailMagnetDistance : Resource.Resource_zh.StepDetailMagnetDistance;
        // （“起始板位：”/“From Slot:”）
        public string StepDetailTransferFrom => IsEnglish ? Resource.Resource_en.StepDetailTransferFrom : Resource.Resource_zh.StepDetailTransferFrom;
        // （“终止板位：”/“To Slot:”）
        public string StepDetailTransferTo => IsEnglish ? Resource.Resource_en.StepDetailTransferTo : Resource.Resource_zh.StepDetailTransferTo;
        // （“下压距离：”/“Press-Down Distance:”）
        public string StepDetailTransferPosition => IsEnglish ? Resource.Resource_en.StepDetailTransferPosition : Resource.Resource_zh.StepDetailTransferPosition;
        // （“温控动作”/“TempCtrl Action”）
        public string StepDetailTempCtrlAction => IsEnglish ? Resource.Resource_en.StepDetailTempCtrlAction : Resource.Resource_zh.StepDetailTempCtrlAction;
        // （“PCR动作”/“PCR procedure”）
        public string StepDetailPCRprocedure => IsEnglish ? Resource.Resource_en.StepDetailPCRprocedure : Resource.Resource_zh.StepDetailPCRprocedure;
        #endregion

        #region 步骤详情 - 提示/通知类
        // STL文件未找到（STL file not found）
        public string MainWindowDetailSTL => IsEnglish ? Resource.Resource_en.MainWindowDetailSTL : Resource.Resource_zh.MainWindowDetailSTL;
        // 加载失败（Load fail）
        public string MainWindowDetailLoadFail => IsEnglish ? Resource.Resource_en.MainWindowDetailLoadFail : Resource.Resource_zh.MainWindowDetailLoadFail;
        // 用户名为空（Username is empty）
        public string MainWindowDetailUserEmpty => IsEnglish ? Resource.Resource_en.MainWindowDetailUserEmpty : Resource.Resource_zh.MainWindowDetailUserEmpty;
        // 密码为空（The password is empty）
        public string MainWindowDetailPassEmpty => IsEnglish ? Resource.Resource_en.MainWindowDetailPassEmpty : Resource.Resource_zh.MainWindowDetailPassEmpty;
        // 当前登录：（Logged in is:）
        public string MainWindowDetailLoginIN => IsEnglish ? Resource.Resource_en.MainWindowDetailLoginIN : Resource.Resource_zh.MainWindowDetailLoginIN;
        // GRPC加载成功（GRPC loading is successful）
        public string GrpcLoadSucc => IsEnglish ? Resource.Resource_en.GrpcLoadSucc : Resource.Resource_zh.GrpcLoadSucc;
        // GRPC初始化失败：（GRPC initialization failed:）
        public string GrpcLoadFail => IsEnglish ? Resource.Resource_en.GrpcLoadFail : Resource.Resource_zh.GrpcLoadFail;
        // IP地址加载失败（IP address loading failed）
        public string GrpcIPFail => IsEnglish ? Resource.Resource_en.GrpcIPFail : Resource.Resource_zh.GrpcIPFail;
        // 设备状态加载完成（Equipment status loading completed）
        public string GrpcDeviceLoadSucc => IsEnglish ? Resource.Resource_en.GrpcDeviceLoadSucc : Resource.Resource_zh.GrpcDeviceLoadSucc;
        // 设备状态加载失败：（Device status loading failed:）
        public string GrpcDeviceLoadFail => IsEnglish ? Resource.Resource_en.GrpcDeviceLoadFail : Resource.Resource_zh.GrpcDeviceLoadFail;
        // 数据已更新（Data has been updated.）
        public string SettingDataSave => IsEnglish ? Resource.Resource_en.SettingDataSave : Resource.Resource_zh.SettingDataSave;
        // 开始和结束步骤不能删除（Start and end steps are non-deletable）
        public string GrpcStartEndRemove => IsEnglish ? Resource.Resource_en.GrpcStartEndRemove : Resource.Resource_zh.GrpcStartEndRemove;
        // 设备开始初始化（The device begins initialization.）
        public string GrpcInitStart => IsEnglish ? Resource.Resource_en.GrpcInitStart : Resource.Resource_zh.GrpcInitStart;
        // 设备初始化中...（Device Initializing...）
        public string GrpcIniting => IsEnglish ? Resource.Resource_en.GrpcIniting : Resource.Resource_zh.GrpcIniting;
        // 设备复位成功（Device reset successful）
        public string GrpcInitSucc => IsEnglish ? Resource.Resource_en.GrpcInitSucc : Resource.Resource_zh.GrpcInitSucc;
        // 流程未停止（Process has not stopped）
        public string GrpcStartRunning => IsEnglish ? Resource.Resource_en.GrpcStartRunning : Resource.Resource_zh.GrpcStartRunning;
        // 选择要加载的脚本文件（Select Script File to Load）
        public string OpenFileDialog_Title => IsEnglish ? Resource.Resource_en.OpenFileDialog_Title : Resource.Resource_zh.OpenFileDialog_Title;
        // JSON脚本文件 (*.json)|*.json|所有文件 (*.*)|*.*（JSON Script Files (*.json)|*.json|All Files (*.*)|*.*）
        public string OpenFileDialog_Filter => IsEnglish ? Resource.Resource_en.OpenFileDialog_Filter : Resource.Resource_zh.OpenFileDialog_Filter;
        // 选择的文件为空 (The selected file is empty）
        public string OpenFileDialog_Empty => IsEnglish ? Resource.Resource_en.OpenFileDialog_Empty : Resource.Resource_zh.OpenFileDialog_Empty;
        // 不支持的文件格式！ (Unsupported file format!）
        public string OpenFileDialog_ErrFormal => IsEnglish ? Resource.Resource_en.OpenFileDialog_ErrFormal : Resource.Resource_zh.OpenFileDialog_ErrFormal;
        // 读取文件失败 (Failed to read the file）
        public string OpenFileDialog_Error => IsEnglish ? Resource.Resource_en.OpenFileDialog_Error : Resource.Resource_zh.OpenFileDialog_Error;
        // 脚本加载成功 (Script loading successful）
        public string ScriptLoadSucc => IsEnglish ? Resource.Resource_en.ScriptLoadSucc : Resource.Resource_zh.ScriptLoadSucc;
        // 加载脚本失败 (Script loading failed）
        public string ScriptLoadFail => IsEnglish ? Resource.Resource_en.ScriptLoadFail : Resource.Resource_zh.ScriptLoadFail;
        // 请添加流程步骤后再开始 (Please add process steps before starting）
        public string ScriptStartEmpty => IsEnglish ? Resource.Resource_en.ScriptStartEmpty : Resource.Resource_zh.ScriptStartEmpty;
        // 部分步骤缺少液体参数 (Some steps lack liquid parameters）
        public string ScriptStartLiquidEmpty => IsEnglish ? Resource.Resource_en.ScriptStartLiquidEmpty : Resource.Resource_zh.ScriptStartLiquidEmpty;
        // 正在创建流程脚本... (Creating process scripts...）
        public string ScriptStartCreating => IsEnglish ? Resource.Resource_en.ScriptStartCreating : Resource.Resource_zh.ScriptStartCreating;
        // 流程开始执行 (Process starting execution）
        public string ScriptStartSucc => IsEnglish ? Resource.Resource_en.ScriptStartSucc : Resource.Resource_zh.ScriptStartSucc;
        // 检查流程失败 (Check process failed）
        public string ScriptStartCheckFail => IsEnglish ? Resource.Resource_en.ScriptStartCheckFail : Resource.Resource_zh.ScriptStartCheckFail;
        // 开始流程失败 (Start process failed）
        public string ScriptStartFail => IsEnglish ? Resource.Resource_en.ScriptStartFail : Resource.Resource_zh.ScriptStartFail;
        // 创建脚本失败 (Create script failed）
        public string ScriptStartCreateFail => IsEnglish ? Resource.Resource_en.ScriptStartCreateFail : Resource.Resource_zh.ScriptStartCreateFail;
        // 流程未开始 (Process not started）
        public string ScriptNotStart => IsEnglish ? Resource.Resource_en.ScriptNotStart : Resource.Resource_zh.ScriptNotStart;
        // 暂停流程 (Pause process）
        public string ScriptPause => IsEnglish ? Resource.Resource_en.ScriptPause : Resource.Resource_zh.ScriptPause;
        // 继续流程 (Resume process）
        public string ScriptContinue => IsEnglish ? Resource.Resource_en.ScriptContinue : Resource.Resource_zh.ScriptContinue;
        // 未运行 (Not running）
        public string ScriptUINotRun => IsEnglish ? Resource.Resource_en.ScriptUINotRun : Resource.Resource_zh.ScriptUINotRun;
        // 执行中 (Running）
        public string ScriptUILogRun => IsEnglish ? Resource.Resource_en.ScriptUILogRun : Resource.Resource_zh.ScriptUILogRun;
        // 暂停 (Pause）
        public string ScriptUILogPause => IsEnglish ? Resource.Resource_en.ScriptUILogPause : Resource.Resource_zh.ScriptUILogPause;
        // 待机 (Idle）
        public string ScriptUILogIdle => IsEnglish ? Resource.Resource_en.ScriptUILogIdle : Resource.Resource_zh.ScriptUILogIdle;
        // 错误 (Error）
        public string ScriptUILogError => IsEnglish ? Resource.Resource_en.ScriptUILogError : Resource.Resource_zh.ScriptUILogError;
        // 未知状态 (Unknown state）
        public string ScriptUILogUnknown => IsEnglish ? Resource.Resource_en.ScriptUILogUnknown : Resource.Resource_zh.ScriptUILogUnknown;
        // 打开补光灯 (Turn on the fill light）
        public string DeviceLightOpen => IsEnglish ? Resource.Resource_en.DeviceLightOpen : Resource.Resource_zh.DeviceLightOpen;
        // 关闭补光灯 (Turn off the fill light）
        public string DeviceLightClose => IsEnglish ? Resource.Resource_en.DeviceLightClose : Resource.Resource_zh.DeviceLightClose;
        // 打开紫外灯 (Turn on the UV light）
        public string DeviceUVOpen => IsEnglish ? Resource.Resource_en.DeviceUVOpen : Resource.Resource_zh.DeviceUVOpen;
        // 关闭紫外灯 (Turn off the UV light）
        public string DeviceUVClose => IsEnglish ? Resource.Resource_en.DeviceUVClose : Resource.Resource_zh.DeviceUVClose;
        // 流程已成功生成 (Process successfully generated）
        public string ScriptSuccCrea => IsEnglish ? Resource.Resource_en.ScriptSuccCrea : Resource.Resource_zh.ScriptSuccCrea;
        // 板位名称不能为空 (Plate pos name not empty）
        public string SQLPosNameNotEmpty => IsEnglish ? Resource.Resource_en.SQLPosNameNotEmpty : Resource.Resource_zh.SQLPosNameNotEmpty;
        // 操作失败 (Operation failed:）
        public string DeviceOperationFailure => IsEnglish ? Resource.Resource_en.DeviceOperationFailure : Resource.Resource_zh.DeviceOperationFailure;
        // 操作成功 (Operate successfully）
        public string DeviceOperationSucc => IsEnglish ? Resource.Resource_en.DeviceOperationSucc : Resource.Resource_zh.DeviceOperationSucc;

        // 吸液/注液支持耗材提示（“Only supports microplates or reservoirs”/“吸液/注液步骤仅支持微孔板或储液槽”）
        public string StepDetailAspDispConsTip => IsEnglish ? Resource.Resource_en.StepDetailAspDispConsTip : Resource.Resource_zh.StepDetailAspDispConsTip;
        // 取头/退头支持耗材提示（“Only supports tip boxes or waste bins”/“取头/退头步骤仅支持TIP盒或垃圾桶”）
        public string StepDetailTipOnOffConsTip => IsEnglish ? Resource.Resource_en.StepDetailTipOnOffConsTip : Resource.Resource_zh.StepDetailTipOnOffConsTip;
        // 当前耗材（“（Current：{0}）”/“（当前：{0}）”，用于格式化耗材名称）
        public string StepDetailCurrentCons => IsEnglish ? Resource.Resource_en.StepDetailCurrentCons : Resource.Resource_zh.StepDetailCurrentCons;
        #endregion
        #region QuickFlowWindow类
        // 请选择取头位置（Please select tip pickup position）
        public string QuickTipOnPos => IsEnglish ? Resource.Resource_en.QuickTipOnPos : Resource.Resource_zh.QuickTipOnPos;
        // 请选择吸液位置（Please select aspiration position）
        public string QuickAisPos => IsEnglish ? Resource.Resource_en.QuickAisPos : Resource.Resource_zh.QuickAisPos;
        // 请选择注液位置（Please select dispensing position）
        public string QuickDisPos => IsEnglish ? Resource.Resource_en.QuickDisPos : Resource.Resource_zh.QuickDisPos;
        // 请选择退头位置（Please select tip ejection position）
        public string QuickTipOffPos => IsEnglish ? Resource.Resource_en.QuickTipOffPos : Resource.Resource_zh.QuickTipOffPos;
        // 请选择液体参数（Please select liquid parameters）
        public string QuickSelectLiquid => IsEnglish ? Resource.Resource_en.QuickSelectLiquid : Resource.Resource_zh.QuickSelectLiquid;
        // 请输入有效的吸液体积（Please enter valid aspiration volume）
        public string QuickValidAspirationVolume => IsEnglish ? Resource.Resource_en.QuickValidAspirationVolume : Resource.Resource_zh.QuickValidAspirationVolume;
        // 请输入1-96之间的样本数量（Please enter sample count between 1-96）
        public string QuickOne96Samples => IsEnglish ? Resource.Resource_en.QuickOne96Samples : Resource.Resource_zh.QuickOne96Samples;

        #endregion
        #region 步骤详情 - 液体参数子标签（与LiquidSettings对应）
        // 吸液前空气（“Air Aspiration Before Aspiration:”/“吸液前吸空气：”）
        public string StepDetailAspAirB => IsEnglish ? Resource.Resource_en.StepDetailAspAirB : Resource.Resource_zh.StepDetailAspAirB;
        // 吸液后空气（“Air Aspiration After Aspiration:”/“吸液后吸空气：”）
        public string StepDetailAspAirA => IsEnglish ? Resource.Resource_en.StepDetailAspAirA : Resource.Resource_zh.StepDetailAspAirA;
        // 吸液速度（“Aspiration Speed:”/“吸液速度：”）
        public string StepDetailAspSpeed => IsEnglish ? Resource.Resource_en.StepDetailAspSpeed : Resource.Resource_zh.StepDetailAspSpeed;
        // 吸液延迟（“Aspiration Delay:”/“吸液延迟：”）
        public string StepDetailAspDelay => IsEnglish ? Resource.Resource_en.StepDetailAspDelay : Resource.Resource_zh.StepDetailAspDelay;
        // 吸液距离（“Aspiration Distance:”/“吸液距离：”）
        public string StepDetailAspDist => IsEnglish ? Resource.Resource_en.StepDetailAspDist : Resource.Resource_zh.StepDetailAspDist;
        // 注液前空气（“Air Aspiration Before Dispensing:”/“注液前吸空气：”）
        public string StepDetailDispAirB => IsEnglish ? Resource.Resource_en.StepDetailDispAirB : Resource.Resource_zh.StepDetailDispAirB;
        // 注液后空气（“Air Aspiration After Dispensing:”/“注液后吸空气：”）
        public string StepDetailDispAirA => IsEnglish ? Resource.Resource_en.StepDetailDispAirA : Resource.Resource_zh.StepDetailDispAirA;
        // 注液速度（“Dispensing Speed:”/“注液速度：”）
        public string StepDetailDispSpeed => IsEnglish ? Resource.Resource_en.StepDetailDispSpeed : Resource.Resource_zh.StepDetailDispSpeed;
        // 注液延迟（“Dispensing Delay:”/“注液延迟：”）
        public string StepDetailDispDelay => IsEnglish ? Resource.Resource_en.StepDetailDispDelay : Resource.Resource_zh.StepDetailDispDelay;
        // 注液距离（“Dispensing Distance:”/“注液距离：”）
        public string StepDetailDispDist => IsEnglish ? Resource.Resource_en.StepDetailDispDist : Resource.Resource_zh.StepDetailDispDist;
        #endregion

        public void SwitchToChinese()
        {
            IsEnglish = false;
        }

        public void SwitchToEnglish()
        {
            IsEnglish = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
