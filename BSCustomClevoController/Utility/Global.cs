using System;
using System.Windows.Media;
using BSCustomClevoController.Controls;


namespace BSCustomClevoController.Utility
{
	public class Global
	{
		public static RWReg RW_REG = new RWReg();

		public static Support_BIT support_bit = new Support_BIT();

		public static Clevo_MSG clevo_msg;

		public static InsydeDriver insyde = new InsydeDriver();

		public static FanControl fan = new FanControl();

		public static byte[] kled_kb_array = new byte[255];

		public static Global.KB kb_color = new Global.KB();

		public static language MultiLang;

		public static RWIni RW_ini;

		public static int Flexikey_LED_Language;

		public class KB
		{
			public Color all = default(Color);

			public Color left = default(Color);

			public Color mid = default(Color);

			public Color right = default(Color);

			public Color TP = default(Color);

			public Color LOGO = default(Color);

			public Color LightBar = default(Color);

			public int _iKbBrightness = 0;

			public int _iKbStatus = 0;

			public int _iKbMode = 0;

			public int _iKbLeft = 0;

			public int _iKbMid = 0;

			public int _iKbRight = 0;

			public int _iKbTp = 0;

			public int _iVolume = 0;

			public int _iKbLogo = 0;

			public int _iKbAutoLightBar = 0;

			public int _iKbSleepLEDTimer = 0;
		}

		public class FanArgs
		{
			public byte T1;

			public byte T2;

			public byte T3;

			public byte T4;

			public byte T2_Default;

			public byte T3_Default;

			public byte D1;

			public byte D2;

			public byte D3;

			public byte D4;

			public byte D2_Default;

			public byte D3_Default;

			public int R12;

			public int R23;

			public int R34;

			public int rpm;

			public int local;

			public int remote;

			public byte duty;
		}
	}
}
