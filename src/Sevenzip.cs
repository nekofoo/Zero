using System.Diagnostics;

namespace Zero
{
	public static class Sevenzip
	{
		public static string ArchiveFolder { get; set; } = Environment.GetEnvironmentVariable("TEMP") ?? "./";
		public static string ArchivePath => Path.Combine(ArchiveFolder, "temp.gzip"); // Path to the archive
		public static string TarPath => Path.Combine(ArchiveFolder, "temp.tar"); // Path to the archive
		public static string ArchiveReceivePath => Path.Combine(ArchiveFolder, "temprecv.gzip");
		public static string ExtractPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		public static string TarExtractPath { get; set; } = Path.Combine(ArchiveFolder, "tarrecv");
		public static string ExtractedTarPath { get; set; } = Path.Combine(TarExtractPath, "temp.tar");
		static Process process;
		public static void Compress(string[] args, out Stream s)
		{
			Tar(args);
			string sevenZipPath = Config.sevenZipPath; 		

			string files = string.Join(" ", args);
			// Create a process to run 7z.exe
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = sevenZipPath;
			processStartInfo.Arguments = $"a dummy.gzip -tgzip -mx3 -so {TarPath}";
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = false;

			process = Process.Start(processStartInfo);
			
			s = process.StandardOutput.BaseStream;			

			Task.Run(() =>
			{
				process.WaitForExit();
				File.Delete(TarPath);
			});

			Console.WriteLine("Streaming Compression stream.");
		}
		public static void Tar(string[] args)
		{
			if (File.Exists(TarPath)) File.Delete(TarPath);
			string sevenZipPath = Config.sevenZipPath; // Path to the 7z.exe file			

			string files = string.Join(" ", args);
			// Create a process to run 7z.exe
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = sevenZipPath;
			processStartInfo.Arguments = $"a {TarPath} -ttar -aoa \"{files}\"";
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = false;

			using (Process process = Process.Start(processStartInfo))
			{
				process.WaitForExit();
			}

			Console.WriteLine("Tar completed.");
		}
		public static void UnTar(string? to = null)
		{
			string sevenZipPath = Config.sevenZipPath; // Path to the 7z.exe file			

			// Create a process to run 7z.exe
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = sevenZipPath;
			processStartInfo.Arguments = $"x {ExtractedTarPath} -o{(to is null ? ExtractPath : to)} -aoa";
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardInput = true;
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;



			using (Process process = Process.Start(processStartInfo))
			{
				EventHandler kill = (sender, args) => { process.Kill(); };
				AppDomain.CurrentDomain.ProcessExit += kill;
				// Read the output (or errors)

				Task.Run(() =>
				{
					while (true)
					{
						var line = process.StandardOutput.ReadLine();
						if (line == null) break;
						Console.WriteLine("||"+line);
					}
				});
				process.WaitForExit();
				AppDomain.CurrentDomain.ProcessExit -= kill;
			}
			File.Delete(ExtractedTarPath);
		}
		public static void Extract(string? to = null)
		{
			string sevenZipPath = Config.sevenZipPath; // Path to the 7z.exe file			

			// Create a process to run 7z.exe
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = sevenZipPath;
			processStartInfo.Arguments = $"x {ArchiveReceivePath} -o{TarExtractPath} -aoa";
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardInput = true;
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;



			using (Process process = Process.Start(processStartInfo))
			{
				EventHandler kill = (sender, args) => { process.Kill(); };
				AppDomain.CurrentDomain.ProcessExit += kill;
				// Read the output (or errors)

				Task.Run(() =>
				{
					while (true)
					{
						var line = process.StandardOutput.ReadLine();
						if (line == null) break;
						Console.WriteLine("||"+line);
					}
				});
				process.WaitForExit();
				AppDomain.CurrentDomain.ProcessExit -= kill;
			}
			UnTar(to);
			Console.WriteLine("||Extract completed.");
			//Console.WriteLine("Saved to :{0}", (to is null ? ExtractPath : to));
		}


	}
}
