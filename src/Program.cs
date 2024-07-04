using IniParser;
using IniParser.Model;
using System.Net;
using System.Text;

namespace Zero
{
	internal static class Program
	{		
		static BackgroundReceiver backgroundReceiver = new BackgroundReceiver();
		static void Main(string[] args)
		{			
			Loadsettings();
			for (int i = 0; i < args.Length; i++)
			{
				args[i] = args[i].Replace(" ", "?zs?");
			}
			Console.WriteLine($"* Your ID is \"{Dns.GetHostName()}\"");
			if (args.Length == 0)
			{
				try
				{
					backgroundReceiver.Start();
					Console.WriteLine("Start background receiver");
				}
				catch (Exception)
				{
					Console.WriteLine("Send only");
				}
				
			}
			Share(args);
			Console.WriteLine("About to exit");
			Task.Delay(1000).Wait();			
		}
		static void Loadsettings()
		{
			FileIniDataParser parser = new();
			IniData data;
			var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			var iniFile = Path.Combine(baseDirectory, "settings.ini");
			try
			{
				data = parser.ReadFile(iniFile, Encoding.UTF8);
			}
			catch (Exception)
			{
				data = CreateIni();
				parser.WriteFile(iniFile, data, Encoding.UTF8);
			}
			
			try
			{
				string s = data["Common"]["SavePath"];
				if (Path.IsPathFullyQualified(s)) { Sevenzip.ExtractPath = s; Directory.CreateDirectory(s); }
				else Console.WriteLine($"SavePath is not qualified, defaults to {Sevenzip.ExtractPath}");
				bool autoOpen = data["Common"]["AutoOpenFile"] == "0" ? false : true;
				Config.AutoOpenFile = autoOpen;

			}
			catch (Exception)
			{
				Console.WriteLine($"Invalid setting.ini, restore to default");
				data = CreateIni();
				parser.WriteFile(iniFile, data, Encoding.UTF8);				
			}
			Console.WriteLine("* Your files will be saved to:");
			Console.WriteLine("*   " + Sevenzip.ExtractPath);
			Console.WriteLine("* You can change it in settings.ini:");

		}
		static IniData CreateIni()
		{
			IniData data = new();
			data.Sections.Add(new SectionData("Common"));
			data["Common"].AddKey(new KeyData("SavePath"));
			data["Common"]["SavePath"] = Sevenzip.ExtractPath;
			data["Common"].AddKey(new KeyData("AutoOpenFile"));
			data["Common"]["AutoOpenFile"] = "true";
			return data;
		}
		static void Share(string[]? args)
		{
			ZeroFile zeroFile = new(args);
			zeroFile.start();
		}
	}
}
