using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BullsAndCows
{
	/// <summary>
	/// Окно выбора подключения к игре
	/// </summary>
	public partial class Connect : Window
	{
		//порт приложения
		private int port;
		//Таймер разсылки сообщений с локальным адрессом сервера
		private Timer timer;
		//сокет прослушивания
		private Socket listeningSocket;
		//флаг если приложение ждет подключения
		private bool isServer = false;
		//локальный ip адрес
		private string thisIP;
		//ссылка на главное окно приложения
		private MainWindow mainWindow;
		//флаг если нажата клавиша присоединения по глобальному ip
		bool isJoinIp = false;
		//хранит глобальный ip
		string externalip;
		public Connect(MainWindow wnd)
		{
			InitializeComponent();
			mainWindow = wnd;
			port = wnd.Port;
			ButtonJoin.IsEnabled = false;
			ButtonSingle.Click += ButtonSingle_Click;
			ButtonCreate.Click += ButtonCreate_Click;
			ButtonJoin.Click += ButtonJoin_Click;
			ButtonJoinIp.Click += ButtonJoinIp_Click;
			ButtonRefresh.Click += ButtonRefresh_Click;
			ButtonConnect.Click += ButtonConnect_Click;
			ListIp.SelectionChanged += ListIp_SelectionChanged;
			//получение локального ip
			thisIP = mainWindow.GetLocalIPAddress();
			//получение глобального ip
			externalip = new WebClient().DownloadString("http://icanhazip.com");
			externalip = externalip.Substring(0, externalip.Length - 1);
			//скрываем патель подключения по глобальному  ip
			IpPanel.Visibility = Visibility.Hidden;
			//константа текста
			string yourWanIpText = "Your WAN IP : ";
			RunWanIpText.Text = yourWanIpText;
			RunWanIp.Text = externalip;
			//Слушаем сами себя
			StartListern();
		}

		private void ButtonConnect_Click(object sender, RoutedEventArgs e)
		{
			//обработка нажатия  и отправка запроса по глобальному ip
			IPAddress ip = IPAddress.Parse(externalip);
			byte[] data = Encoding.Unicode.GetBytes($"join:{externalip}");
			EndPoint remotePoint = new IPEndPoint(ip, port);
			listeningSocket.SendTo(data, remotePoint);
			mainWindow.ServerIP = externalip;
			DialogResult = true;
		}

		private void ButtonJoinIp_Click(object sender, RoutedEventArgs e)
		{
			//обработка нажатия и настройка интерфейса при подключении
			//по глобальному ip
			var thisButton = sender as Button;
			IpPanel.Visibility = isJoinIp ? Visibility.Hidden : Visibility.Visible;
			thisButton.Content = isJoinIp ? "JOIN TO IP" : "CANCEL";
			ButtonCreate.IsEnabled = isJoinIp;
			ButtonJoin.IsEnabled = isJoinIp ? (ListIp.SelectedItem != null ? true : false) : false;

			ButtonSingle.IsEnabled = isJoinIp;
			isJoinIp = !isJoinIp;
		}
		private void ButtonSingle_Click(object sender, RoutedEventArgs e)
		{
			//обработка нажатия при выборе одиночной игры
			mainWindow.ServerIP = mainWindow.GetLocalIPAddress();
			mainWindow.IsSingle = true;
			DialogResult = true;
		}

		private void ListIp_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//обработка при выборе локального сервера из списка
			ButtonJoin.IsEnabled = true;
		}

		private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
		{
			//обновление скиска локальных серверов
			ListIp.Items.Clear();
		}
	

		private void Listern()
		{
			//слушаем сами себя и обрабатываем полученные сообщения
			try
			{
				IPAddress listernIP = IPAddress.Parse(mainWindow.GetLocalIPAddress());
				IPEndPoint localIp = new IPEndPoint(listernIP, port);
				listeningSocket.Bind(localIp);
				while (true)
				{
					StringBuilder builder = new StringBuilder();
					int bytes = 0;
					byte[] data = new byte[256];
					EndPoint remoteIp = new IPEndPoint(IPAddress.Any, port);
					do
					{
						bytes = listeningSocket.ReceiveFrom(data, ref remoteIp);
						builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
					} while (listeningSocket.Available > 0);
					IPEndPoint remoteFullIp = remoteIp as IPEndPoint;
					string[] split = builder.ToString().Split(':');
					switch (split[0])
					{
						//Добавляем в список локальный сервер
						case "server":
							Dispatcher.Invoke(() =>
							{
								if (!ListIp.Items.Contains(remoteFullIp.Address) && !isServer)
								{
									ListIp.Items.Add(remoteFullIp.Address);
								}
							});
							break;
						//Другой игрок намерен подключится к нашему серверу
						case "join":
							mainWindow.ServerIP = split[1];
							timer.Dispose();
							Dispatcher.Invoke(() => { DialogResult = true; });
							break;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				CloseSocket();

			}
		}
		private void StartListern()
		{
			//Настройка прослушивания
			listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			listeningSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
			Task taskListern = Task.Run(() => Listern());
		}
		private void CloseSocket()
		{
			//Окончание прослушивания
			listeningSocket?.Shutdown(SocketShutdown.Both);
			listeningSocket?.Close();
			listeningSocket = null;
		}

		private void ButtonJoin_Click(object sender, RoutedEventArgs e)
		{
			//обработка нажатия при подключении к локальному серверу
			//и отправка сообщения о желании подключится
			string ipString = ListIp.SelectedItem.ToString();
			IPAddress ip = IPAddress.Parse(ipString);
			byte[] data = Encoding.Unicode.GetBytes($"join:{ipString}");
			EndPoint remotePoint = new IPEndPoint(ip, port);
			listeningSocket.SendTo(data, remotePoint);
			mainWindow.ServerIP = ipString;
			DialogResult = true;
		}

		private void ButtonCreate_Click(object sender, RoutedEventArgs e)
		{
			//обработка нажатию кнопки запуска сервера
			if (!isServer)
			{
				//если сервер еще не запущен
				ListIp.Items.Clear();
				ListIp.Items.Add("Wait client for connection...");
				ButtonRefresh.IsEnabled = false;
				ButtonJoinIp.IsEnabled = false;
				ButtonCreate.Content = "CANCEL SERVER";
				isServer = true;
				timer = new Timer(SendServerMessage);
				timer.Change(0, 3000);

			}
			else
			{
				//если сервер запущен
				isServer = false;
				ButtonCreate.Content = "CREATE GAME";
				ButtonRefresh.IsEnabled = true;
				ButtonJoinIp.IsEnabled = true;
				ListIp.Items.Clear();
				timer.Dispose();
			}
			mainWindow.IsServer = isServer;
		}

		private void SendServerMessage(object sender)
		{
			//широковещательная розсылка сообщений
			byte[] data = Encoding.Unicode.GetBytes("server");
			EndPoint remotePoint = new IPEndPoint(IPAddress.Broadcast, port);
			listeningSocket.SendTo(data, remotePoint);
		}

	}
}
