using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Zero
{
	public class ZeroFile
	{
		ZeroServiceDiscovry serviceDiscovry;
		List<NewServerEventArgs> servers = new() { };		
		CancellationTokenSource cts = new();
		List<string> entries = new List<string>();
		string[] args;
		int serverCount;
		bool printFile = false;
		bool isMessage(string entry)
		{
			return !(File.Exists(entry) || Directory.Exists(entry));
		}
		string comp_path = "";
		bool noEntry { get => entries.Count() == 0; }
		public ZeroFile(string[]? args)
		{
			this.args = args == null ? [] : args;
			serviceDiscovry = new(NewServerDiscovered, cts);
		}

		void NewServerDiscovered(object? sender, NewServerEventArgs e)
		{
			servers.Add(e);
			Console.WriteLine($"[{serverCount++}] {e.ServerName}");
		}

		CancellationTokenSource GetCancellationTokenSource()
		{
			var c = new CancellationTokenSource();
			cts = c;
			return c;
		}
		public void start()
		{			
            requireEntry(string.Join(" ", args));
            while (true)
			{
				if (noEntry) requireEntry();

				send();

				resetCmds();

				if (args.Length > 0) break;
			}
		}

		void resetCmds()
		{
			printFile = false;
		}

		/// <summary>
		/// If it's a folder, compress it.
		/// </summary>		
		void preprocess(string originalPath, out Stream? compressStream)
		{

			if (Directory.Exists(originalPath))
			{
				Console.WriteLine("====packing====");
				Stream s;
				Sevenzip.Compress([originalPath], out s);
				compressStream = s;
			}
			else
			{
				compressStream = null;
			}

		}		
		void parseEntry(string? entry)
		{
			if (entry is null) return;
			if (entry == "-p")
			{
				printFile = true;
				return;
			}
			entries.Add(entry);
		}
		void send()
		{
			serviceDiscovry.Start();
			IPAddress? des = requireDestination();
			if (des == null) { return; }
			while (entries.Count > 0)
			{
				TcpClient client = new TcpClient();
				client.Connect(des, 4005);
				var entry = entries.First();
				entries.RemoveAt(0);
				_send(client, entry);
			}	
		}

		private IPAddress? requireDestination()
		{
			for (int i = 0; i < servers.Count; i++)
			{
				Console.WriteLine($"[{i}] {servers[i].ServerName}");
			}
			var selection = Console.ReadKey().KeyChar;
			int selectionNum = 0;
			if (int.TryParse(selection.ToString(), out selectionNum) && selectionNum < servers.Count && selectionNum >= 0)
			{
				var ips = servers[selectionNum].Address;
				Ping ping = new Ping();
				IPAddress? ip = null;
				for (int i = 0; i < ips.Length; i++)
				{
					if (ping.Send(ips[i], 1000).Status == IPStatus.Success)
					{
						ip = ips[i];
						break;
					}
				}
				if (ip is null)
				{
					Console.WriteLine($"{servers[selectionNum].ServerName} is unreachable, may blocked by Firewall");
					return null;
				}
				return ip;
			}
			else { return null; }
		}

		void readCancelKey(CancellationToken canRead,ref bool canceled)
		{
			Console.WriteLine("Press i to cancel");
			while (!canRead.IsCancellationRequested)
			{
				char c= ' ';
				if (Console.KeyAvailable)
				{
					c = Console.ReadKey(intercept:true).KeyChar;
				}
				
				try
				{
					canRead.ThrowIfCancellationRequested();
				}
				catch (Exception)
				{
					break;
				}
				if (c == 'i') 
				{
					canceled = true;
					break;
				}
				Task.Delay(500).Wait();
			}
			
		}
		bool _send(TcpClient client, string entry)
		{
			Stream? compressStream;
			preprocess(entry, out compressStream);

			Console.WriteLine("Sending " + entry);

			var netStream = client.GetStream();
			var read_buffer = new byte[10 * 1024 * 1024];
			bool canceled = false;

			string filename;
			if (isMessage(entry)) filename = "Message.txt";
			else filename = Path.GetFileName(entry.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
					+ (compressStream is not null ? ".gzip" : "");

			var filename_inbytes = Encoding.UTF8.GetBytes(filename);
			netStream.Write(BitConverter.GetBytes(filename_inbytes.Length), 0, sizeof(int));
			netStream.Write(filename_inbytes, 0, filename_inbytes.Length);

			Stream? fileStream = null;
			if (!isMessage(entry))
			{
				if (compressStream is null) fileStream = File.OpenRead(entry);
				else fileStream = compressStream;
			}

			CancellationTokenSource allowTransferInterrupt = new();
			Task.Run(() => readCancelKey(allowTransferInterrupt.Token, ref canceled));

			byte[] mesa = new byte[20];//bytes with additional info
			if (isMessage(entry)) netStream.Write(Encoding.UTF8.GetBytes(entry));
			else
			{
				double sum = 0;
				while (!canceled)
				{
					var count = fileStream!.Read(read_buffer, 0, read_buffer.Length);
					sum += ((double)count) / 1024 / 1024;
					Console.WriteLine("progress:" + sum.ToString("0.00") + " MB");
					Console.SetCursorPosition(0, Console.GetCursorPosition().Top - 1);
					if (count == 0) break;
					netStream.Write(read_buffer, 0, count);
				}
				allowTransferInterrupt.Cancel();
				Console.WriteLine();
				
				if (canceled)
				{
					
					byte[] cancelData = Encoding.UTF8.GetBytes(Config.cancelMessage);					
					cancelData.CopyTo(mesa, 0);					
					Console.WriteLine("\n"+ Config.cancelMessage);
				}
				else if (printFile)
				{					
					byte[] printData = Encoding.UTF8.GetBytes(Config.printMessage);					
					printData.CopyTo(mesa, 0);					
					Console.WriteLine("\n" + Config.printMessage);
				}				
			}
			netStream.Write(mesa, 0, 20);
			fileStream?.Close();
			client.Close();
			return true;
		}
        private void requireEntry(string? arg = null)
        {
            string? entry = null;

            string[] p;

            if (arg is null)
            {
                Console.WriteLine("Drop a file or type a message: ");
                entry = Console.ReadLine();
                p = ParseFilePaths(entry);
            }
            else
            {
                p = ParseFilePaths(arg);
            }

            if (p.Length > 0)
            {
                var first = p[0];
                if (!File.Exists(first) && !Directory.Exists(first))
                {
                    parseEntry(arg is null ? entry : arg);
                    return;
                }
            }
            foreach (var item in p)
            {
                parseEntry(item);
            }
        }
        string[] ParseFilePaths(string? input)
		{
			if (input is null) return [];
			List<string> result = new List<string>();
			bool inQuotes = false;
			int startIndex = 0;

			for (int i = 0; i < input.Length; i++)
			{
				if (input[i] == '\"')
				{
					inQuotes = !inQuotes;
				}
				else if (input[i] == ' ' && !inQuotes)
				{
					if (i > startIndex)
					{
						result.Add(input.Substring(startIndex, i - startIndex).Trim('\"'));
					}
					startIndex = i + 1;
				}
			}

			if (startIndex < input.Length)
			{
				result.Add(input.Substring(startIndex).Trim('\"'));
			}

			return result.ToArray();
		}
	}
}
