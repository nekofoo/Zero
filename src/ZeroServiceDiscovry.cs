using Makaretu.Dns;
using System.Collections.Concurrent;
using System.Net;


namespace Zero
{
	public class ZeroServiceDiscovry(NewServerEvent newServerEvent, CancellationTokenSource cts)
	{
		DomainName mFdDomain = new DomainName("_fd._tcp.local");
		DomainName mFdService = new DomainName("_fd._tcp");
		DomainName mARecordName = new DomainName("fd.local");
		MulticastService mMdns = new MulticastService(new((list) =>
			{
				return list;
			}));
		ServiceDiscovery? mSd;
		public ConcurrentDictionary<string, IPAddress[]> Servers = new();

		public event NewServerEvent NewServerDiscovered = newServerEvent;
		public CancellationTokenSource CancellationTokenSource { get => cts; set { cts = value; } }
		bool started = false;

		public void Start()
		{
			if (started) return;

			mMdns.AnswerReceived += Mdns_AnswerReceived;

			mMdns.Start();

			mSd = new ServiceDiscovery(mMdns);

			Console.WriteLine("...");

			Task.Run(QueryServiceInstances, cts.Token);

			mSd.ServiceInstanceDiscovered += Sd_ServiceInstanceDiscovered;

			started = true;
		}

		private void QueryServiceInstances()
		{
			while (!cts.IsCancellationRequested)
			{
				mSd?.QueryServiceInstances(mFdService);
				Task.Delay(2000).Wait();
			}
		}


		private void Mdns_AnswerReceived(object? sender, MessageEventArgs e)
		{

			var records = e.Message.Answers.OfType<ARecord>();
			if (records.Count() == 0) { return; }
			List<IPAddress> addresses = new List<IPAddress>();
			string serverName = "Unknown";
			foreach (var item in records)
			{
				var a = item;
				if (!a.Name.BelongsTo(mARecordName)) return;
				//Console.WriteLine($"{a.Name}:{a.Address}");
				serverName = a.Name.Labels[0];
				addresses.Add(a.Address);
			}
			if (serverName == "Unknown") return;
			bool added = Servers.TryAdd(serverName, addresses.ToArray());
			if (added)
			{
				NewServerDiscovered?.Invoke(this, new NewServerEventArgs(serverName, Servers[serverName]));
			}
		}

		private void Sd_ServiceInstanceDiscovered(object? sender, ServiceInstanceDiscoveryEventArgs e)
		{
			if (!e.ServiceInstanceName.BelongsTo(mFdDomain))
			{
				//Console.WriteLine($"Ignore {e.ServiceInstanceName}");
				return;
			}
			else if (!Servers.ContainsKey(e.ServiceInstanceName.Labels[0]))
			{
				Console.WriteLine($"Query for A");
				mMdns.SendQuery($"{e.ServiceInstanceName.Labels[0]}.fd.local", DnsClass.IN, DnsType.ANY);
			}
			//Console.WriteLine(e.ServiceInstanceName);
		}
	}

	public delegate void NewServerEvent(object? sender, NewServerEventArgs e);

	public class NewServerEventArgs(string serverName, IPAddress[] address) : EventArgs
	{
		public string ServerName { get => serverName; }
		public IPAddress[] Address { get => address; }

	}
}
