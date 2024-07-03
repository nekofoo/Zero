using Makaretu.Dns;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Zero
{
	public class BackgroundReceiver
	{
		ServiceDiscovery sd = new ServiceDiscovery();
		ServiceProfile profile;
		List<TcpListener> listeners = new List<TcpListener>();		
		public BackgroundReceiver()
		{
			profile = new ServiceProfile($"\"{Dns.GetHostName()}\"", "_fd._tcp", 5010);
			sd.Mdns.QueryReceived += MMdns_QueryReceived;
			sd.Advertise(profile);
			
		}
		public void Start()
		{
			try
			{
				setListener();
			}
			catch (Exception)
			{
				throw new Exception("fail to set listner");
			}
			
			Task.Run(WaitForConnection);
		}
		void setListener()
		{
			foreach (var ip in MulticastService.GetIPAddresses())
			{
				var listener = new TcpListener(ip, 4005);
				listener.Start();
				listeners.Add(listener);
			}

		}
		private void MMdns_QueryReceived(object? sender, MessageEventArgs e)
		{
			if (!e.Message.Questions.First().Name.ToString().Contains("fd"))
			{
				return;
			}
			//Console.WriteLine("Query type: " + e.Message.Questions[0].Type.ToString() + $" from {e.Message.Questions.First().Name}");

		}
		void WaitForConnection()
		{
			while (true)
			{
				List<Task<TcpClient>> tasks = new List<Task<TcpClient>>() { };
				CancellationTokenSource cts = new();
				foreach (var l in listeners)
				{
					try
					{
						l.Start();
						tasks.Add(l.AcceptTcpClientAsync(cts.Token).AsTask());
					}
					catch (Exception e)
					{
						Console.WriteLine(e.Message);
					}
				}
				//Console.WriteLine("waiting for connection");
				var index = Task.WaitAny(tasks.ToArray());
				var client = tasks[index].Result;
				cts.Cancel();
				//Console.WriteLine($"remote client accepted:{client.Client.RemoteEndPoint}");
				try
				{
					Receive(client);
				}
				catch (Exception ex)
				{

					Console.WriteLine(ex.Message);
				}
				
			}
		}
		bool Receive(TcpClient client)
		{
			Console.WriteLine("||====Receiveing====");
			var cursorPosition = Console.GetCursorPosition().Top;
			
			var stream = client.GetStream();
			byte[] buffer = new byte[1024 * 1024 * 5];
			int zerocount = 0;
			
			double size = 0;
			var receiveInterrupted = false;
			bool printfile = false;
			try
			{
				stream.Read(buffer, 0, sizeof(int));
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}
			int name_length_inbyte = BitConverter.ToInt32(buffer, 0);
			if (name_length_inbyte == 0)
			{
				return false;
			}
			stream.Read(buffer, 0, name_length_inbyte);
			var filename = Encoding.UTF8.GetString(buffer, 0, name_length_inbyte);
			FileStream fsw;
			try
			{
				if (filename.Contains(".gzip"))
				{
					fsw = File.Create(Sevenzip.ArchiveReceivePath);
				}
				else
				{
					fsw = File.Create(Path.Combine(Sevenzip.ExtractPath, filename));
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}
			var fsw_startpos = 0;
			while (zerocount < 1)
			{
				int count;
				try
				{
					count = stream.Read(buffer, fsw_startpos, buffer.Length - fsw_startpos);
				}
				catch (Exception)
				{
					Console.WriteLine("Interrupted");
					fsw.Close(); client.Close(); File.Delete(fsw.Name);
					return false;
				}

				size += count / 1024.0 / 1024.0;

				if (count > 0)
				{
					if (count > 0.05 * 1024 * 1024)
					{
						Console.SetCursorPosition(0, cursorPosition);
						Console.WriteLine("||Progress:" + size.ToString("0.00") + " MB");
					}
					fsw_startpos += count;

					if (fsw_startpos == buffer.Length)
					{
						fsw.Write(buffer, 0, buffer.Length);
						fsw_startpos = 0;
					}
				}
				else
				{
					zerocount++;
					int start = Math.Max(0, fsw_startpos - 20);					
					var lastmessage = Encoding.UTF8.GetString(buffer[start .. fsw_startpos]);
					if (lastmessage.StartsWith(Config.cancelMessage))
					{
						Console.WriteLine(Config.cancelMessage);
						receiveInterrupted = true;
					}
					else if (lastmessage.StartsWith(Config.printMessage))
					{
						printfile = true;
					}
					fsw.Write(buffer, 0, start);
				}
			}
			
			fsw.Flush(); fsw.Close(); client.Close();
			if (receiveInterrupted)
			{
				File.Delete(fsw.Name);
				return false;
			}
			if (filename.Contains(".gzip"))
			{				
				Console.WriteLine("||====unpacking====");
				Sevenzip.Extract();
				File.Delete(Sevenzip.ArchiveReceivePath);
			}	
			
			var savePath = Path.Combine(Sevenzip.ExtractPath, filename.Replace(".gzip", ""));

			Console.WriteLine($"||= = = = = = Saved to: {savePath} {(printfile? "(Print)" : "" )}= = = = = = =\n");
			if (OperatingSystem.IsWindows()) openFileOrFolder_Win(savePath, printfile);
			return true;
		}

		void openFileOrFolder_Win(string arg,bool print)
		{
			if (!Config.AutoOpenFile && !print) return;
			
			Process process;
			if (File.Exists(arg) && Path.GetFileName(arg).Equals("Message.txt"))
			{
				process = Process.Start("notepad.exe", arg);
			}
			else if (Directory.Exists(arg))
			{
				process = Process.Start("explorer.exe", arg);
			}
			else
			{
				if (!print) Process.Start("explorer.exe", $"{Sevenzip.ExtractPath}");
				ProcessStartInfo processStartInfo = new ProcessStartInfo
				{
					FileName = arg,
					UseShellExecute = true,					
				};
				if (print) processStartInfo.Verb = "print";

				process = new Process
				{
					StartInfo = processStartInfo
				};

				try
				{
					process.Start();
				}
				catch (System.ComponentModel.Win32Exception ex)
				{					
					Console.WriteLine($"An error occurred: {ex.Message}");
				}
				
			}			
		}
	}
}
