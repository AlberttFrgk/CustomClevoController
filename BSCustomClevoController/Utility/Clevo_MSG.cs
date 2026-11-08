using System;
using System.Runtime.InteropServices;

namespace BSCustomClevoController.Utility
{
	public class Clevo_MSG
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int RegisterWindowMessage(string lpString);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool SendNotifyMessage(int hWnd, int Msg, int wParam, int lParam);

		[DllImport("User32.dll")]
		private static extern int SendMessage(int hWnd, int Msg, uint wParam, uint lParam);

		[DllImport("User32.dll", EntryPoint = "SendMessage")]
		private static extern int SendMessageData(int hWnd, int Msg, int wParam, ref Clevo_MSG.COPYDATASTRUCT lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("DataAddress.dll")]
		private static extern uint DataAddress(IntPtr data1);

		public Clevo_MSG()
		{
			this.Init();
		}

		private void Init()
		{
			this.msgName = Clevo_MSG.RegisterWindowMessage("ClevoMsg_ControlCenter");
			this.msgName1 = Clevo_MSG.RegisterWindowMessage("ClevoMsg_ControlCenter_WMI");
		}

		public void SendMsg(int CMD, int DATA)
		{
			Clevo_MSG.SendNotifyMessage(65535, this.msgName, CMD, DATA);
		}

		public void SendMsg_WMI(int CMD, int DATA)
		{
			Console.WriteLine("SendMsg_WMI CMD" + CMD.ToString() + " Data=" + DATA.ToString());
			Clevo_MSG.COPYDATASTRUCT copydatastruct = default(Clevo_MSG.COPYDATASTRUCT);
			int hWnd = (int)Clevo_MSG.FindWindow(null, "HkeyTray");
			byte[] array = new byte[]
			{
				Convert.ToByte(CMD),
				Convert.ToByte(DATA & 255),
				(byte)((DATA >> 8) & 255),
				(byte)((DATA >> 16) & 255),
				(byte)((DATA >> 24) & 255)
			};
			copydatastruct.cbData = array.GetUpperBound(0) + 1;
			copydatastruct.lpData = Clevo_MSG.DataAddress(Marshal.UnsafeAddrOfPinnedArrayElement<byte>(array, 0));
			Clevo_MSG.SendMessageData(hWnd, 74, 2, ref copydatastruct);
		}

		public void GetMsg_WMI(int CMD)
		{
			Clevo_MSG.COPYDATASTRUCT copydatastruct = default(Clevo_MSG.COPYDATASTRUCT);
			int hWnd = (int)Clevo_MSG.FindWindow(null, "HkeyTray");
			byte[] array = new byte[] { Convert.ToByte(CMD) };
			copydatastruct.cbData = array.GetUpperBound(0) + 1;
			copydatastruct.lpData = Clevo_MSG.DataAddress(Marshal.UnsafeAddrOfPinnedArrayElement<byte>(array, 0));
			Clevo_MSG.SendMessageData(hWnd, 74, 2, ref copydatastruct);
		}

		public void SendArrayToHkeyTray()
		{
			Clevo_MSG.COPYDATASTRUCT copydatastruct = default(Clevo_MSG.COPYDATASTRUCT);
			copydatastruct.cbData = Global.kled_kb_array.GetUpperBound(0) + 1;
			copydatastruct.lpData = Clevo_MSG.DataAddress(Marshal.UnsafeAddrOfPinnedArrayElement<byte>(Global.kled_kb_array, 0));
			int hWnd = (int)Clevo_MSG.FindWindow(null, "HkeyTray");
			Clevo_MSG.SendMessageData(hWnd, 74, 3, ref copydatastruct);
		}

		public void SendToHKTray(int cmd)
		{
			byte dwData = (byte)((cmd >> 8) & 255);
			Clevo_MSG.COPYDATASTRUCT copydatastruct = default(Clevo_MSG.COPYDATASTRUCT);
			int hWnd = (int)Clevo_MSG.FindWindow(null, "HkeyTray");
			byte[] array = new byte[5];
			array[0] = (byte)(cmd & 255);
			copydatastruct.dwData = (int)dwData;
			copydatastruct.cbData = array.GetUpperBound(0) + 1;
			copydatastruct.lpData = Clevo_MSG.DataAddress(Marshal.UnsafeAddrOfPinnedArrayElement<byte>(array, 0));
			Clevo_MSG.SendMessageData(hWnd, 74, 2, ref copydatastruct);
		}

		public void SendToHKTray(int cmd, byte arr1)
		{
			byte dwData = (byte)((cmd >> 8) & 255);
			Clevo_MSG.COPYDATASTRUCT copydatastruct = default(Clevo_MSG.COPYDATASTRUCT);
			int hWnd = (int)Clevo_MSG.FindWindow(null, "HkeyTray");
			byte[] array = new byte[5];
			array[0] = (byte)(cmd & 255);
			array[1] = arr1;
			copydatastruct.dwData = (int)dwData;
			copydatastruct.cbData = array.GetUpperBound(0) + 1;
			copydatastruct.lpData = Clevo_MSG.DataAddress(Marshal.UnsafeAddrOfPinnedArrayElement<byte>(array, 0));
			Clevo_MSG.SendMessageData(hWnd, 74, 2, ref copydatastruct);
		}

		public void SendToHKTray(int cmd, byte arr1, byte arr2)
		{
			byte dwData = (byte)((cmd >> 8) & 255);
			Clevo_MSG.COPYDATASTRUCT copydatastruct = default(Clevo_MSG.COPYDATASTRUCT);
			int hWnd = (int)Clevo_MSG.FindWindow(null, "HkeyTray");
			byte[] array = new byte[5];
			array[0] = (byte)(cmd & 255);
			array[1] = arr1;
			array[2] = arr2;
			copydatastruct.dwData = (int)dwData;
			copydatastruct.cbData = array.GetUpperBound(0) + 1;
			copydatastruct.lpData = Clevo_MSG.DataAddress(Marshal.UnsafeAddrOfPinnedArrayElement<byte>(array, 0));
			Clevo_MSG.SendMessageData(hWnd, 74, 2, ref copydatastruct);
		}

		public void SendToHKTray(int cmd, byte arr1, byte arr2, byte arr3)
		{
			byte dwData = (byte)((cmd >> 8) & 255);
			Clevo_MSG.COPYDATASTRUCT copydatastruct = default(Clevo_MSG.COPYDATASTRUCT);
			int hWnd = (int)Clevo_MSG.FindWindow(null, "HkeyTray");
			byte[] array = new byte[5];
			array[0] = (byte)(cmd & 255);
			array[1] = arr1;
			array[2] = arr2;
			array[3] = arr3;
			copydatastruct.dwData = (int)dwData;
			copydatastruct.cbData = array.GetUpperBound(0) + 1;
			copydatastruct.lpData = Clevo_MSG.DataAddress(Marshal.UnsafeAddrOfPinnedArrayElement<byte>(array, 0));
			Clevo_MSG.SendMessageData(hWnd, 74, 2, ref copydatastruct);
		}

		public void SendToHKTray(int cmd, byte arr1, byte arr2, byte arr3, byte arr4)
		{
			byte dwData = (byte)((cmd >> 8) & 255);
			Clevo_MSG.COPYDATASTRUCT copydatastruct = default(Clevo_MSG.COPYDATASTRUCT);
			int hWnd = (int)Clevo_MSG.FindWindow(null, "HkeyTray");
			byte[] array = new byte[]
			{
				(byte)(cmd & 255),
				arr1,
				arr2,
				arr3,
				arr4
			};
			copydatastruct.dwData = (int)dwData;
			copydatastruct.cbData = array.GetUpperBound(0) + 1;
			copydatastruct.lpData = Clevo_MSG.DataAddress(Marshal.UnsafeAddrOfPinnedArrayElement<byte>(array, 0));
			Clevo_MSG.SendMessageData(hWnd, 74, 2, ref copydatastruct);
		}

		private const int HWND_BROADCAST = 65535;

		private int msgName;

		private int msgName1;

		public struct COPYDATASTRUCT
		{
			public int dwData;

			public int cbData;

			public uint lpData;
		}
	}
}
