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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BullsAndCows
{
	public class ResultData
	{
		public string Number { get; set; }
		public string Result { get; set; }
	}


	public partial class MainWindow : Window
	{
		//public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
		//			"Message",
		//			typeof(string),
		//			typeof(Window),
		//			new FrameworkPropertyMetadata(
		//				string.Empty,
		//				0,
		//				new PropertyChangedCallback(OnTextChanged)));

		//public string Message
		//{
		//	get { return (string)GetValue(MessageProperty); }
		//	set { SetValue(MessageProperty, value); }
		//}

		//private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		//{
		//	MessageBox.Show($"dependence: {d.GetValue(MessageProperty).ToString()}");
		//}

		//Флаг введенного секретного числа
		private bool isStart = false;
		//Строковые константы
		private string messageBeforeStart = "Input your secret number...";
		private string messageAfterStart = "Input number of your oponent...";
		private string messageWait = "Wait for next move...";
		private string messageLost = "Your opponent is Lost!";
		private string clientName = "client";
		private string serverName = "server";

		//Список нажатых кнопок с цифрами
		private List<Button> listButton = new List<Button>();
		//Порт приложения
		public int Port { get; set; } = 8411;
		//Флаг является ли экземпляр приложения сервером
		public bool IsServer { get; set; } = false;
		//Флаг одиночной игры
		private bool isSingle = false;		
		public bool IsSingle {
			get { return isSingle; }
			set { isSingle = value; if (isSingle == true) IsServer = true; }
		}
		//Хранит  ip сервера
		public string ServerIP { get; set; }
		//загаданое число клиентом
		private string clientNumber = "";
		//загаданое число сервером (экземпляр приложения где запущен сервер)
		private string serverNumber = "";
		//попытка отгадать число
		private string clientTryNumber = "";
		private string serverTryNumber = "";
		//введённая часть попытки отгадать число
		private string number = "";
		//количество попыток
		private int tryCount = 0;
		//Флаг если один из игроков отключился
		private bool isLost = false;
		//Список цветов маркеров для пометки цифр
		private List<Color> colorMark = new List<Color>
		{
			Color.FromRgb(248, 248,248),
			Color.FromRgb(128, 222,106),
			Color.FromRgb(255, 248,101),
			Color.FromRgb(251, 108,101),
			Color.FromRgb(60, 137,230)
		};
		//Хранит попытку в клиеетской части
		private string TryNumber
		{
			get { return IsServer ? serverTryNumber : clientTryNumber; }
			set
			{
				if (IsServer) serverTryNumber = value;
				else clientTryNumber = value;
			}
		}
		//Возвращает роль(клиент или сервер)
		private string Role { get { return IsServer ? serverName : clientName; } }
		//Хранит задуманное число
		private string SecretNumber
		{
			get { return IsServer ? serverNumber : clientNumber; }
			set
			{
				if (IsServer) serverNumber = value;
				else clientNumber = value;
			}
		}
		//Ответ сервера
		private string Ok
		{
			get
			{
				return isLost ? $"result:lost" : $"result:ok";
			}
		}
		//Таймер для ожидания следующего хода
		private Timer waitTimer;

		public MainWindow()
		{
			Connect connectWindow = new Connect(this);
			if (connectWindow.ShowDialog() != true)
			{
				Close();
			}

			InitializeComponent();			
			ButtonStart.IsEnabled = false;
			TextBlockNumber.Text = messageBeforeStart;
			ButtonStart.Click += ButtonStart_Click;
			ButtonReset.Click += ButtonReset_Click;
			this.Closing += MainWindow_Closing;


			if (IsServer)
			{
				//Если приложение - сервер, запускаем прослушивание
				Task task = Task.Run(() => Listern());
			}
			if (IsSingle)
			{
				//Настройки одиночной игры
				clientNumber = CreateNumber();
				//TextBlockYourNamber.Text = clientNumber;
				GridOponent.Visibility = Visibility.Hidden;
				ButtonStart.Content = "TRY";
				ButtonStart.IsEnabled = false;
				TextBlockNumber.Text = messageAfterStart;
				TextBlockYourSecretNumber.Text = "";				
				number = "";
				EnableButtons();
				isStart = true;
			}

		}

		//В одиночной игре приложение загадывает число
		private string CreateNumber()
		{
			string number = "";
			Random rnd = new Random();
			while (number.Length < 4)
			{
				
				int n = 0;
				bool find = true;
				do
				{
					find = true;
					n = rnd.Next(0, 9);
					foreach (char ch in number)
					{
						if (ch == n.ToString()[0])
						{
							find = false;
						}
					}
				} while (!find);
				number += n.ToString();
			}
			return number;
		}

		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{				
			//во время закрытия окна отправляем сообщение на сервер
			Send($"{Role}:lost");			
		}

		private void EnableButtons()
		{
			//Включаем нажатые кнопки цифр
			foreach (var button in listButton)
			{
				button.IsEnabled = true;
			}
			listButton.Clear();
		}
		public string GetLocalIPAddress()
		{
			//получение локального  ip адреса компьютера
			var host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (var ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					return ip.ToString();
				}
			}
			throw new Exception("No network adapters with an IPv4 address in the system!");
		}
		private void ButtonStart_Click(object sender, RoutedEventArgs e)
		{
			if (IsSingle)
			{			
				//Обработка нажатия кнопки при одиночной игре
				TextBlockNumber.Text = messageWait;
				TryNumber = number;
				number = "";
				GridButtons.IsEnabled = false;
				ButtonStart.IsEnabled = false;
				string mess = $"{Role}:single:{number}";
				Send(mess);
			}
			else if (!isStart) 
			{
				//Обработка нажатия кнопки если приложение клиент
				ButtonStart.Content = "SEND";
				ButtonStart.IsEnabled = false;
				TextBlockNumber.Text = messageAfterStart;
				SecretNumber = number;
				TextBlockYourNamber.Text = SecretNumber;
				string mess = $"{Role}:start:{SecretNumber}";
				Send(mess);
				number = "";
				EnableButtons();
				isStart = true;
			}
			else
			{
				//Обработка нажатия кнопки если приложение сервер
				string mess = $"{Role}:try:{number}";
				Send(mess);
				TextBlockNumber.Text = messageWait;
				TryNumber = number;
				number = "";
				GridButtons.IsEnabled = false;
				ButtonStart.IsEnabled = false;
				waitTimer = new Timer(WaitResponse);
				waitTimer.Change(0, 3000);
			}
		}		
		private void WaitResponse(object sender)
		{
			//Отправляем запрос что бы узнать когда второй игрок будет готов
			Send($"{Role}:get");
		}
		public void Listern()
		{
			//Прослушивание работает на стороне сервера
			//и отвечант на запросы клиентов
			TcpListener listener = null;
			try
			{
				listener = new TcpListener(IPAddress.Parse(ServerIP), Port);
				listener.Start();
				int gets = 0;
				while (true)
				{
					TcpClient client = listener.AcceptTcpClient();
					NetworkStream stream = client.GetStream();
					byte[] data = new byte[256];
					int bytes = stream.Read(data, 0, data.Length);
					string message = Encoding.Unicode.GetString(data, 0, bytes);
					string responseMessage = "";
					string[] split = message.Split(':');
					switch (split[1])
					{
						//Попытка отгадать число в одиночной игре
						case "single":
							tryCount++;
							string result = GetBullsAndCows(serverName, serverTryNumber);						
							responseMessage = $"result:single:{serverTryNumber}:{result}:{tryCount}";
							break;
						//Клиенты присылают загаданые числа
						case "start":
							//tryCount = 1;
							if (split[0] == clientName)
							{
								clientNumber = split[2];
							}
							else
							{
								serverNumber = split[2];
							}
							responseMessage = Ok;
							break;
						//Клиенты пытаются отгадать число соперника
						case "try":							
							if (split[0] == clientName)
							{
								clientTryNumber = split[2];
							}
							else
							{
								serverTryNumber = split[2];
							}
							responseMessage = Ok;
							break;
						//Клиетн ожидает хода опонента
						case "get":
							if (isLost)
							{
								responseMessage = $"result:lost";
							}
							else if (clientTryNumber == "" || serverTryNumber == "")
							{
								responseMessage = $"result:wait";
							}
							else
							{
								gets++;
								string clientResult = GetBullsAndCows(clientName, clientTryNumber);
								string serverResult = GetBullsAndCows(serverName, serverTryNumber);
								string c = $"{clientTryNumber}:{clientResult}";
								string s = $"{serverTryNumber}:{serverResult}";
								responseMessage = split[0] == serverName ? $"result:response:{s}:{c}:{tryCount+1}" : $"result:response:{c}:{s}:{tryCount+1}";
							}
							break;
						//Клиент отключился
						case "lost":
							responseMessage = Ok;
							isLost = true;
							break;						
					}
					if (gets >= 2)
					{
						//Оба клиента сделали ход
						tryCount++;
						clientTryNumber = "";
						serverTryNumber = "";
						gets = 0;
					}
					data = Encoding.Unicode.GetBytes(responseMessage);
					stream.Write(data, 0, data.Length);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			finally
			{
				listener.Stop();
			}
		}

		private void Send(string message)
		{
			Task.Run(() => SendMessage(message));
		}
		private void SendMessage(string message)
		{
			//отправка запроса с клиента на сервер
			TcpClient tcpClient = null;
			try
			{
				tcpClient = new TcpClient();
				tcpClient.Connect(IPAddress.Parse(ServerIP), Port);
				NetworkStream stream = tcpClient.GetStream();
				byte[] data = Encoding.Unicode.GetBytes(message);
				stream.Write(data, 0, data.Length);
				data = new byte[256];
				int len = stream.Read(data, 0, data.Length);
				if (len == 0)
				{
					throw new Exception("Socket close!");
				}
				string responseServer = Encoding.Unicode.GetString(data, 0, len);
				string[] split = responseServer.Split(':');
				//Обработка ответов сервера
				switch (split[1])
				{
					//Ответ при одиночной игре
					case "single":
						Dispatcher.Invoke(() =>
						{							
							playerResults.Items.Add(new ResultData() { Number = split[2], Result = split[3] });
							playerResults.ScrollIntoView(playerResults.Items.GetItemAt(playerResults.Items.Count - 1));
							if (split[3] == "win")
							{
								TextBlockNumber.Text = $"You WIN from {split[4]} tries!!!";
							}
							else
							{
								GridButtons.IsEnabled = true;
								TextBlockNumber.Text = messageAfterStart;
								Reset();
							}
						});
						break;
					//Ответ на попытку отгадать число
					case "response":
						Dispatcher.Invoke(() =>
						{
							waitTimer.Dispose();
							playerResults.Items.Add(new ResultData() { Number = split[2], Result = split[3] });
							playerResults.ScrollIntoView(playerResults.Items.GetItemAt(playerResults.Items.Count - 1));
							oponentResults.Items.Add(new ResultData() { Number = split[4], Result = split[5] });
							oponentResults.ScrollIntoView(oponentResults.Items.GetItemAt(oponentResults.Items.Count - 1));
							// при выиграше
							if (split[3] == "win" && split[5] == "win")
							{
								TextBlockNumber.Text = $"Double WIN from {split[6]} tries!!!";
							}
							else if (split[3] == "win")
							{
								TextBlockNumber.Text = $"You WIN from {split[6]} tries!!!";
							}
							else if (split[5] == "win")
							{
								TextBlockNumber.Text = $"Your oponent WIN from {split[6]} tries...";
							}
							else
							{
								GridButtons.IsEnabled = true;
								TextBlockNumber.Text = messageAfterStart;
								Reset();
							}
							
						});
						break;
					//Ответ если опонент отключился
					case "lost":
						Dispatcher.Invoke(() =>
						{
							waitTimer?.Dispose();
							TextBlockNumber.Text = messageLost;
							MessageBox.Show(messageLost);
						});
						break;					
					case "ok":
					case "wait":
						break;
					default:
						throw new Exception("Unknown response message!");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			finally
			{
				tcpClient.Close();
			}
		}


		private void ButtonReset_Click(object sender, RoutedEventArgs e)
		{
			//Сброс введённого числа 
			Reset();
		}

		
		private void Reset()
		{
			//Сброс введённого числа 
			TextBlockNumber.Text = isStart ? messageAfterStart : messageBeforeStart;
			number = "";
			ButtonStart.IsEnabled = false;
			EnableButtons();
		}

		private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
		{
			//Обработка маркеров для помощи при отгадывании числа
			TextBlock tb = sender as TextBlock;
			SolidColorBrush brush = tb.Background as SolidColorBrush;
			if (brush == null)
			{
				tb.Background = new SolidColorBrush(colorMark[0]);
				brush = tb.Background as SolidColorBrush;
			}
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				for (int i = 0; i < colorMark.Count; i++)
				{
					if (brush.Color == colorMark[i])
					{						
						tb.Background = new SolidColorBrush(colorMark[i < colorMark.Count-1?i+1:0]);
						break;
					}
				}				
			}
			if (e.RightButton == MouseButtonState.Pressed)
			{
				for (int i = 0; i < colorMark.Count; i++)
				{
					if (brush.Color == colorMark[i])
					{
						tb.Background = new SolidColorBrush(colorMark[i >0 ? i - 1 : colorMark.Count-1]);
						break;
					}
				}
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			//Обработкак начания цифровых кнопок
			Button btn = sender as Button;
			if (number.Length == 0)
			{
				TextBlockNumber.Text = "";
			}
			if (number.Length < 4)
			{
				number += btn.Content.ToString();
				btn.IsEnabled = false;
				listButton.Add(btn);
			}
			if (number.Length == 4)
			{
				ButtonStart.IsEnabled = true;
			}
			TextBlockNumber.Text = number;
		}
		private string GetBullsAndCows(string player, string variant)
		{
			//Подсказка сервера на попытку отгадать число
			string number = player == clientName ? serverNumber : clientNumber;
			int cows = 0;
			int bulls = 0;
			for (int i = 0; i < number.Length; i++)
			{
				if (variant.IndexOf(number[i]) != -1)
				{
					cows++;
				}
				if (variant[i] == number[i])
				{
					bulls++;
				}
			}
			if (bulls == 4)
			{
				return $"win";
			}
			return $"{cows}C{bulls}B";
		}
	}
}
