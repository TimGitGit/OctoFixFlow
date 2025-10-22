using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
namespace OctoFixFlow
{
    public class ResourceHelper : INotifyPropertyChanged
    {
        private static readonly ResourceHelper _instance = new ResourceHelper();
        public static ResourceHelper Instance => _instance;

        private bool _isEnglish = true;

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
                }
            }
        }
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
        public string WindowActionWait => IsEnglish ? Resource.Resource_en.WindowActionWait : Resource.Resource_zh.WindowActionWait;
        public string WindowBoardPosition => IsEnglish ? Resource.Resource_en.WindowBoardPosition : Resource.Resource_zh.WindowBoardPosition;
        public string WindowConsumablesWarehouse => IsEnglish ? Resource.Resource_en.WindowConsumablesWarehouse : Resource.Resource_zh.WindowConsumablesWarehouse;
        public string WindowLoginCancel => IsEnglish ? Resource.Resource_en.WindowLoginCancel : Resource.Resource_zh.WindowLoginCancel;
        public string WindowLoginOK => IsEnglish ? Resource.Resource_en.WindowLoginOK : Resource.Resource_zh.WindowLoginOK;
        public string WindowLoginPass => IsEnglish ? Resource.Resource_en.WindowLoginPass : Resource.Resource_zh.WindowLoginPass;
        public string WindowLoginPassTitle => IsEnglish ? Resource.Resource_en.WindowLoginPassTitle : Resource.Resource_zh.WindowLoginPassTitle;
        public string WindowLoginUser => IsEnglish ? Resource.Resource_en.WindowLoginUser : Resource.Resource_zh.WindowLoginUser;
        public string WindowLoginUserTitle => IsEnglish ? Resource.Resource_en.WindowLoginUserTitle : Resource.Resource_zh.WindowLoginUserTitle;
        public string WindowLogOut => IsEnglish ? Resource.Resource_en.WindowLogOut : Resource.Resource_zh.WindowLogOut;
        public string WindowOneclickdeletion => IsEnglish ? Resource.Resource_en.WindowOneclickdeletion : Resource.Resource_zh.WindowOneclickdeletion;
        public string WindowProcessList => IsEnglish ? Resource.Resource_en.WindowProcessList : Resource.Resource_zh.WindowProcessList;
        public string Windowrapidgenerating => IsEnglish ? Resource.Resource_en.Windowrapidgenerating : Resource.Resource_zh.Windowrapidgenerating;
        public string WindowStepdetails => IsEnglish ? Resource.Resource_en.WindowStepdetails : Resource.Resource_zh.WindowStepdetails;
        public string WindowStepdetails2 => IsEnglish ? Resource.Resource_en.WindowStepdetails2 : Resource.Resource_zh.WindowStepdetails2;
        public string WindowTitle => IsEnglish ? Resource.Resource_en.WindowTitle : Resource.Resource_zh.WindowTitle;
        public string WindowUser => IsEnglish ? Resource.Resource_en.WindowUser : Resource.Resource_zh.WindowUser;
        public string WindowUserName => IsEnglish ? Resource.Resource_en.WindowUserName : Resource.Resource_zh.WindowUserName;
        public string WindowVersion => IsEnglish ? Resource.Resource_en.WindowVersion : Resource.Resource_zh.WindowVersion;
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
        public string SettingManualXCoordinate => IsEnglish ? Resource.Resource_en.SettingManualXCoordinate : Resource.Resource_zh.SettingManualXCoordinate;
        public string SettingManualYCoordinate => IsEnglish ? Resource.Resource_en.SettingManualYCoordinate : Resource.Resource_zh.SettingManualYCoordinate;
        public string SettingManualZCoordinate => IsEnglish ? Resource.Resource_en.SettingManualZCoordinate : Resource.Resource_zh.SettingManualZCoordinate;
        public string SettingManualMovetoPlatePosition => IsEnglish ? Resource.Resource_en.SettingManualMovetoPlatePosition : Resource.Resource_zh.SettingManualMovetoPlatePosition;
        public string SettingManualMoveZaxis => IsEnglish ? Resource.Resource_en.SettingManualMoveZaxis : Resource.Resource_zh.SettingManualMoveZaxis;
        public string SettingManualSaveCoordinates => IsEnglish ? Resource.Resource_en.SettingManualSaveCoordinates : Resource.Resource_zh.SettingManualSaveCoordinates;
        public string SettingManualResetX => IsEnglish ? Resource.Resource_en.SettingManualResetX : Resource.Resource_zh.SettingManualResetX;
        public string SettingManualResetY => IsEnglish ? Resource.Resource_en.SettingManualResetY : Resource.Resource_zh.SettingManualResetY;
        public string SettingManualResetZ => IsEnglish ? Resource.Resource_en.SettingManualResetZ : Resource.Resource_zh.SettingManualResetZ;
        public string SettingManualCoordinateFineAdjustment => IsEnglish ? Resource.Resource_en.SettingManualCoordinateFineAdjustment : Resource.Resource_zh.SettingManualCoordinateFineAdjustment;
        public string SettingManualXDirection => IsEnglish ? Resource.Resource_en.SettingManualXDirection : Resource.Resource_zh.SettingManualXDirection;
        public string SettingManualYDirection => IsEnglish ? Resource.Resource_en.SettingManualYDirection : Resource.Resource_zh.SettingManualYDirection;
        public string SettingManualZDirection => IsEnglish ? Resource.Resource_en.SettingManualZDirection : Resource.Resource_zh.SettingManualZDirection;
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
        public string SettingManualClose => IsEnglish ? Resource.Resource_en.SettingManualClose : Resource.Resource_zh.SettingManualClose;
        public string QuickTitile => IsEnglish ? Resource.Resource_en.QuickTitile : Resource.Resource_zh.QuickTitile;
        public string QuickSampleQty => IsEnglish ? Resource.Resource_en.QuickSampleQty : Resource.Resource_zh.QuickSampleQty;
        public string QuickTipPickPos => IsEnglish ? Resource.Resource_en.QuickTipPickPos : Resource.Resource_zh.QuickTipPickPos;
        public string QuickFixed => IsEnglish ? Resource.Resource_en.QuickFixed : Resource.Resource_zh.QuickFixed;
        public string QuickAspPos => IsEnglish ? Resource.Resource_en.QuickAspPos : Resource.Resource_zh.QuickAspPos;
        public string QuickDispPos => IsEnglish ? Resource.Resource_en.QuickDispPos : Resource.Resource_zh.QuickDispPos;
        public string QuickMixAsp => IsEnglish ? Resource.Resource_en.QuickMixAsp : Resource.Resource_zh.QuickMixAsp;
        public string QuickMixAspTimes => IsEnglish ? Resource.Resource_en.QuickMixAspTimes : Resource.Resource_zh.QuickMixAspTimes;
        public string QuickMixAspVol => IsEnglish ? Resource.Resource_en.QuickMixAspVol : Resource.Resource_zh.QuickMixAspVol;
        public string QuickRinse => IsEnglish ? Resource.Resource_en.QuickRinse : Resource.Resource_zh.QuickRinse;
        public string QuickRinseVol => IsEnglish ? Resource.Resource_en.QuickRinseVol : Resource.Resource_zh.QuickRinseVol;
        public string QuickRinseDelay => IsEnglish ? Resource.Resource_en.QuickRinseDelay : Resource.Resource_zh.QuickRinseDelay;
        public string QuickTipChangeSettings => IsEnglish ? Resource.Resource_en.QuickTipChangeSettings : Resource.Resource_zh.QuickTipChangeSettings;
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
        // 初始体积（“Initial volume(ul):”/“初始体积(ul)：”）
        public string StepDetailInitialVol => IsEnglish ? Resource.Resource_en.StepDetailInitialVol : Resource.Resource_zh.StepDetailInitialVol;
        // 初始延迟（“Initial delay(ms):”/“初始延迟(ms)：”）
        public string StepDetailInitialDelay => IsEnglish ? Resource.Resource_en.StepDetailInitialDelay : Resource.Resource_zh.StepDetailInitialDelay;
        // 等待时间（“Waiting time (seconds):”/“等待时间(秒)：”）
        public string StepDetailWaitTime => IsEnglish ? Resource.Resource_en.StepDetailWaitTime : Resource.Resource_zh.StepDetailWaitTime;
        // 等待描述（“Waiting description:”/“等待描述：”）
        public string StepDetailWaitDesc => IsEnglish ? Resource.Resource_en.StepDetailWaitDesc : Resource.Resource_zh.StepDetailWaitDesc;
        // 启用混合（CheckBox内容：“Enable mixing”/“启用混合”）
        public string StepDetailEnableMix => IsEnglish ? Resource.Resource_en.StepDetailEnableMix : Resource.Resource_zh.StepDetailEnableMix;
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
        // 设备门未关闭（Device door not closed）
        public string GrpcFailDoor => IsEnglish ? Resource.Resource_en.GrpcFailDoor : Resource.Resource_zh.GrpcFailDoor;
        // 流程未停止（Process has not stopped）
        public string GrpcStartRunning => IsEnglish ? Resource.Resource_en.GrpcStartRunning : Resource.Resource_zh.GrpcStartRunning;
        // 选择要加载的脚本文件（Select Script File to Load）
        public string OpenFileDialog_Title => IsEnglish ? Resource.Resource_en.OpenFileDialog_Title : Resource.Resource_zh.OpenFileDialog_Title;
        // JSON脚本文件 (*.json)|*.json|所有文件 (*.*)|*.*（JSON Script Files (*.json)|*.json|All Files (*.*)|*.*）
        public string OpenFileDialog_Filter => IsEnglish ? Resource.Resource_en.OpenFileDialog_Filter : Resource.Resource_zh.OpenFileDialog_Filter;
        // 选择的文件为空 (The selected file is empty）
        public string OpenFileDialog_Empty => IsEnglish ? Resource.Resource_en.OpenFileDialog_Empty : Resource.Resource_zh.OpenFileDialog_Empty;
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

        // 等待时间提示（“Note: Waiting time will be auto-converted to milliseconds.”/“提示：等待时间将自动转换为毫秒执行”）
        public string StepDetailWaitNote => IsEnglish ? Resource.Resource_en.StepDetailWaitNote : Resource.Resource_zh.StepDetailWaitNote;
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
