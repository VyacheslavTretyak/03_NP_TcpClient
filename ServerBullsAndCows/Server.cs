using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerBullsAndCows
{
	class Server
	{
		//private readonly string ip = "127.0.0.1";
		private readonly int port = 8411;
		private Dictionary<string, string> gameData = new Dictionary<string, string>();		

		public void Start()
		{
			TcpListener server = null;
			try
			{
				server = new TcpListener(IPAddress.Any, port);
				server.Start();
				Console.WriteLine("Server started!");
				Console.WriteLine("Wait connection...");
				while (true) {
					TcpClient client = server.AcceptTcpClient();
					NetworkStream stream = client.GetStream();
					byte[] data = new byte[256];
					int bytes = stream.Read(data, 0, data.Length);
					string message = Encoding.Unicode.GetString(data, 0, bytes);
					Console.WriteLine($"{(client.Client.RemoteEndPoint as IPEndPoint).Address.ToString()} -> {message}");
					string[] messages = message.Split(':');					
					switch (messages[1])
					{
						case "start":
							gameData[messages[0]] = messages[2];
							message = "result:ok";
							break;
						case "try":
							message = GetBullsAndCows(messages[0], messages[2]);
							break;
						case "win":
							message = "result:win";
							break;
					}
					Console.WriteLine($"[message]{message}");
					data = Encoding.Unicode.GetBytes(message);
					stream.Write(data, 0, data.Length);
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
			finally
			{
				server.Stop();
			}
		}
		void Send(string[] messages)
		{
			TcpListener server = null;
			try
			{
				server = new TcpListener(IPAddress.Parse(messages[0]), port);
				server.Start();
				TcpClient client = server.AcceptTcpClient();
				NetworkStream stream = client.GetStream();
				string message = "";
				switch (messages[1]) {
					case "start":				
						gameData[messages[0]] = messages[2];
						message = "ok";
						break;
					case "try":
						message = GetBullsAndCows(messages[0],  messages[2]);						
						break;
					case "win":
						message = "win";
						break;
				}
				Console.WriteLine($"[message]{message}" );
				byte[] data = Encoding.Unicode.GetBytes( message);
				stream.Write(data, 0, data.Length);				
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				server.Stop();
			}
		}

		private string GetBullsAndCows(string player, string variant)
		{
			string number = gameData[player];
			int cows = 0;
			int bulls = 0;
			for(int i = 0; i<number.Length; i++)
			{
				if(variant.IndexOf(number[i]) != -1)
				{
					cows++;
				}
				if(variant[i] == number[i])
				{
					bulls++;
				}
			}		
			if(bulls == 4)
			{
				return $"win:win";
			}
			return $"response:{variant}:{cows}C{bulls}B";
		}
	}
}

