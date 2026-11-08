using System;

namespace BSCustomClevoController.Utility
{
	internal class CleDef
	{
		public enum MessageType
		{
			System,
			Function,
			HotkeyEvent,
			WMI_Execture
		}

		public enum KbPart
		{
			all,
			left,
			mid,
			right,
			touchpad,
			none,
			volume,
			logo
		}

		public enum KB_Mode
		{
			random,
			custom,
			breath,
			cycle,
			wave,
			dance,
			tempo,
			flash,
			all
		}

		public enum LEDKB_Command
		{
			SendDataArray = 22,
			SetKBLEDSleepTimer,
			SetLEDKBOnOff,
			SetKbBrightness,
			SetKB_Mode,
			WriteToReg,
			LoadDefault
		}

		public enum Kb_Cmd_Index
		{
			L_R = 1,
			L_G,
			L_B,
			M_R,
			M_G,
			M_B,
			R_R,
			R_G,
			R_B,
			TP_R,
			TP_G,
			TP_B,
			Logo_R,
			Logo_G,
			Logo_B,
			LightBar_R,
			LightBar_G,
			LightBar_B,
			Left,
			Mid,
			Right,
			Tp,
			Logo,
			AutoLightBar,
			Status,
			Brightness,
			Mode,
			SleepLEDTimer,
			All_R = 30,
			All_G,
			All_B
		}

		public enum Kb_Sleep
		{
			OFF,
			sec_15,
			sec_30,
			min_1,
			min_5,
			custom = 255
		}

		public enum Kb_Type
		{
			NoLed,
			White,
			RGB,
			Perkey,
			RGBFakeWhile = 6,
			RGBFakeWhileCustom = 22
		}

		public enum Brightness
		{
			Off,
			Lv1,
			Lv2,
			Lv3,
			Lv4,
			Lv5
		}

		public enum HkeyTrayEvent
		{
			HKTrayOnInit = 256,
			HKTrayReady,
			SituationMode,
			TouchPad,
			CCD,
			Airplane,
			FlexiKey,
			CapsLk,
			ScrLk,
			NumLk,
			Fan,
			Changed,
			BatteryFull,
			Docking,
			ACOut,
			FN_3,
			BatteryFullDischarge = 273,
			KeyboardOnInit = 512,
			KeyboardReady,
			KeyboardStatus,
			KbBrightness,
			WhiteKbBrightness,
			KbStatus,
			PerKbBrightness,
			FA_Close = 576,
			FA_Start,
			FA_Connect,
			FA_Disconnect,
			FA_UpdateData
		}

		public enum CCToHKTray
		{
			CameraStatus = 257,
			TouchPadStatus,
			MSHybridStatus,
			LcdOff,
			FanMode,
			WinKey,
			Situation,
			AirplaneMode,
			CapsLck,
			ScrLck,
			NumLck,
			UpdateLiveInfoOn,
			UpdateLiveInfoOff,
			HeadPhone,
			CcReady,
			CcClose,
			MemoryVoltage,
			BatteryRefresh = 285,
			HeadPhonePollingOn = 288,
			HeadPhonePollingOff,
			KbBrightness = 526,
			KbSpeed,
			SleepLEDTimer,
			KbPerkeyClearAll,
			KbPerkeyBrightness,
			KbPerkeyStatic,
			KbPerkeyBreath,
			KbPerkeyBreathRandom,
			KbPerkeyBlink,
			KbPerkeyBlinkRandom,
			KbPerkeyWave,
			KbPerkeyRandom,
			KbPerkeyScan,
			KbPerkeySnake,
			KbPerkeyRipple,
			KbPerkeyRippleRandom,
			KbSendDataArray,
			KbRgbMode,
			KbRgbLoadDefault,
			KbPerkeyReactive,
			KbPerkeyReactiveRandom,
			KbSelectBootEffect,
			FAClose = 576,
			FAStart,
			FAConnect,
			FADisconnect,
			FAUpdateData
		}

		public enum Customer
		{
			Standard,
			Tronic5,
			MCJ,
			Seager,
			Gigabyte,
			EPSON,
			Santech
		}
	}
}
