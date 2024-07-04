using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zero
{
	public static class Config
	{
		public static bool AutoOpenFile = true;
		public static string cancelMessage = "TRANSFER_CANCELLED";
		public static string printMessage = "TRANSFER_PRINT";
		public static string sevenZipPath
		{
			get
			{
				if (OperatingSystem.IsWindows()) return "7za.exe";
				else return "./7zz";
			}
		}

	}
}
