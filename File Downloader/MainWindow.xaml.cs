using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MahApps.Metro;
using MahApps.Metro.Controls;
using System.ComponentModel;
using MahApps.Metro.Controls.Dialogs;
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.IO.MemoryMappedFiles;
using Open.Nat;
using System.Text.RegularExpressions;

namespace File_Downloader
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        public static bool ShowConnectWindowButton, WhySendMessage;
        public static int resultConnectionWindow = 0;

        public static bool offOnServer = false, openPort = false, down, lc, MapMemory = false, Encrypt = false, DoubleEncrypt = false;
        public static int portServer, portMessage, count, bufSerializeSize;
        public byte[] nameBuf;
        public static string nameFile, ChooseFileStr, Path = @"Download\", EncryptFilePath = @"Download\", password = "1", passwordDecrypt = "1", 
            InputAddress, ipaddress, indefication, username;

        public static ManualResetEvent manualResetEvent;
        public static ManualResetEvent manualResetEventAceeptFile;

        Option option = new Option();

        public TcpListener tcpListener;
        public TcpClient tcpClient;
        public TcpClient tcpSClient;

        public TcpListener tcpListenerMessager;
        public TcpClient tcpClientMessager;
        public TcpClient tcpSClientMessager;

        public FileStream fs, ChooseFile;
        public static MainWindow Instance { get; private set; }

        public MainWindow() 
        {
            InitializeComponent();
            ShowConnectWindowButton = true;
            ThemeManager.ChangeAppStyle(this, 
                ThemeManager.GetAccent("Purple"), 
                ThemeManager.GetAppTheme("BaseLight"));
            Instance = this;
            option.StartClass();
            Log.Logs();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ShowConnectWindowButton == true)
                {
                    CustomDialog myDialog = new ConnectionWindow();
                    var myDialogSettings = new MetroDialogSettings() { AnimateHide = true, AnimateShow = true };
                    await this.ShowMetroDialogAsync(myDialog, myDialogSettings);

                    Thread ResultConnectionWindowThread = new Thread(ResultConnectionWindow);
                    ResultConnectionWindowThread.Start();

                }
                else if (ShowConnectWindowButton == false)
                {
                    if (resultConnectionWindow == 1)
                    {
                        tcpClient.Close();
                        tcpClient = null;
                    }
                    else if (resultConnectionWindow == 2)
                    {
                        tcpListener.Stop();
                        tcpListener = null;
                        tcpSClient.Close();
                        tcpSClient = null;
                    }
                    SendFileButton.IsEnabled = false;
                    SendMessageButton.IsEnabled = false;
                    TextBoxUserName.IsEnabled = true;
                    InputAddress = "";
                    ipaddress = "";
                    resultConnectionWindow = 0;
                    ShowConnectWindowButton = true;
                    TextboxPortInput.IsEnabled = true;
                    ConnectButton.Content = "Подключиться";
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        void ResultConnectionWindow()
        {
            try
            {
                for (; ; )
                {
                    if (resultConnectionWindow == 1)
                    {
                        //MessageBox.Show("Клиент");
                        TextboxPortInput.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                        {
                            TextboxPortInput.IsEnabled = false;
                        }));
                        ShowConnectWindowButton = false;
                        Thread thread = new Thread(InputAddressForConnect);
                        thread.Start();
                        break;
                    }
                    else if (resultConnectionWindow == 2)
                    {
                        //MessageBox.Show("Сервер");
                        TextboxPortInput.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                        {
                            TextboxPortInput.IsEnabled = false;
                        }));
                        ShowConnectWindowButton = false;
                        ConnectButton.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                        {
                            ConnectButton.Content = "Отключиться";
                        }));

                        Thread thread = new Thread(FileDownload);
                        thread.Start();
                        break;
                    }
                    else if (resultConnectionWindow == 3)
                    {
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        void InputAddressForConnect()
        {
            Application.Current.Dispatcher.Invoke(async delegate
            {
                InputAddress = await Instance.ShowInputAsync("Подключение", "Введите IP-адрес и порт (127.0.0.1:8888):");
            });

            for (; ; )
            {
                if (MainWindow.InputAddress != MainWindow.ipaddress)
                {
                    ipaddress = InputAddress.Remove(InputAddress.IndexOf(@":"));
                    string s = InputAddress.Substring(InputAddress.IndexOf(@":"), 5);
                    portServer = Convert.ToInt32(Regex.Replace(s, @":", ""));
                   // MessageBox.Show(""+portServer);
                    try
                    {
                        try
                        {
                            TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                TexBoxMessage.Text += Environment.NewLine + "Подключение к " + ipaddress + ":" + portServer;
                            }));

                            tcpClient = new TcpClient();
                            tcpClient.Connect(ipaddress, portServer);

                            tcpClientMessager = new TcpClient();
                            tcpClientMessager.Connect(ipaddress, portServer + 1);

                            ConnectButton.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                ConnectButton.Content = "Отключиться";
                            }));

                            Application.Current.Dispatcher.Invoke(async delegate
                            {
                                await Instance.ShowMessageAsync("Подключение", "Подключение прошло успешно!");
                            });

                            Thread threadd = new Thread(Instance.MessageClientServer);
                            threadd.Start();

                            Thread thread = new Thread(Instance.ClientServer);
                            thread.Start();

                            SendFileButton.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                SendFileButton.IsEnabled = true;
                            }));
                            SendMessageButton.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                SendMessageButton.IsEnabled = true;
                            }));

                            TextBoxUserName.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                username = TextBoxUserName.Text;
                                TextBoxUserName.IsEnabled = false;
                            }));

                            break;
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                            MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                            Log.writeLog(ex.Message);
                        }
                    }
                    catch(Exception ex)
                    {
                       // MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                        Log.writeLog(ex.Message);
                        Application.Current.Dispatcher.Invoke(async delegate
                        {
                            await Instance.ShowMessageAsync("Ошибка подключения", "Подключение не прошло успешно!");
                        });
                        ShowConnectWindowButton = true;
                        resultConnectionWindow = 0;
                        ConnectButton.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                        {
                            ConnectButton.Content = "Подключится";
                        }));

                        InputAddress = "";
                        ipaddress = "";
                        break;
                    }
                }
            }
        }

        private void SearchFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "All files (*.*)|*.*";
            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    fs = new FileStream(openDialog.FileName, FileMode.Open);
                    nameFile = openDialog.SafeFileName;

                    TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                    {
                        TexBoxMessage.Text += System.Environment.NewLine + "Выбран файл - " + nameFile + System.Environment.NewLine
                        + "Размер: " + fs.Length / 1024 + " Кб";
                    }));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                    Log.writeLog(ex.Message);
                }
            }
        }

        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (resultConnectionWindow == 1)
                {
                    Thread threadd = new Thread(FileUpload);
                    threadd.Start();
                }
                else if (resultConnectionWindow == 2)
                {
                    Thread thread = new Thread(ServerClient);
                    thread.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (resultConnectionWindow == 1)
                {
                    if(TextBoxMessageInput.Text.Length != 0)
                    {
                        Thread threadd = new Thread(MessageClient);
                        threadd.Start();
                    }
                }
                else if (resultConnectionWindow == 2)
                {
                    if (TextBoxMessageInput.Text.Length != 0)
                    {
                        Thread thread = new Thread(MessageServerClient);
                        thread.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        public async void BufferCheckedClient()
        {
            try
            {
                await this.ShowMessageAsync("Клиент отключился", "Клиент отключился");

                if (resultConnectionWindow == 1)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }
                else if (resultConnectionWindow == 2)
                {
                    tcpListener.Stop();
                    tcpListener = null;
                    tcpSClient.Close();
                    tcpSClient = null;
                }
                SendFileButton.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    SendFileButton.IsEnabled = false;
                }));
                SendMessageButton.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    SendMessageButton.IsEnabled = false;
                }));
                TextBoxUserName.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    TextBoxUserName.IsEnabled = true;
                }));
                TextboxPortInput.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    TextboxPortInput.IsEnabled = true;
                }));
                ConnectButton.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    ConnectButton.Content = "Подключиться";
                }));
                
                InputAddress = "";
                ipaddress = "";
                resultConnectionWindow = 0;
                ShowConnectWindowButton = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        public void MessageClient()
        {
            try
            {
                //отправка сообщения с клиента
                NetworkStream stream = tcpClientMessager.GetStream(); //
                string message = "1";

                TextBoxMessageInput.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    message = DateTime.Now.ToString("HH:mm:ss ") + username + ": " + TextBoxMessageInput.Text;
                }));
                byte[] bytemessage = Encoding.Unicode.GetBytes(message);
                stream.Write(bytemessage, 0, bytemessage.Length);
                TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    TexBoxMessage.Text += System.Environment.NewLine + message;
                }));
                //Log.writeLogClient("Информация о файле отправлена.");
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        public void MessageClientServer()
        {
            try
            {
                while (true)
                {
                    //слушаем сообщения с клиента
                    NetworkStream stream = tcpClientMessager.GetStream();
                    byte[] bytemessage = new byte[2048];
                    int bytes = stream.Read(bytemessage, 0, bytemessage.Length);

                    if (bytes == 0)
                    {
                        BufferCheckedClient();
                        break;
                    }

                    string message = Encoding.Unicode.GetString(bytemessage, 0, bytes);

                    TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                    {
                        TexBoxMessage.Text += System.Environment.NewLine + message;
                    }));
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        public void MessageServer()
        {
            try
            {
                while (true)
                {
                    //слушаем сообщения с сервера

                    NetworkStream stream = tcpSClientMessager.GetStream();
                    byte[] bytemessage = new byte[2048];
                    int bytes = stream.Read(bytemessage, 0, bytemessage.Length);

                    if (bytes == 0)
                    {
                        BufferCheckedClient();
                        break;
                    }

                    string message = Encoding.Unicode.GetString(bytemessage, 0, bytes);

                    TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                    {
                        TexBoxMessage.Text += System.Environment.NewLine + message;
                    }));
                    //Log.writeLogServer("Имя файла - " + nameFile);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        public void MessageServerClient()
        {
            try
            {
                //отправка сообщения с сервера
                NetworkStream stream = tcpSClientMessager.GetStream(); //
                string message = "1";

                TextBoxMessageInput.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    message = DateTime.Now.ToString("HH:mm:ss ") + username + ": " + TextBoxMessageInput.Text;
                }));
                byte[] bytemessage = Encoding.Unicode.GetBytes(message);
                stream.Write(bytemessage, 0, bytemessage.Length);
                TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    TexBoxMessage.Text += System.Environment.NewLine + message;
                }));
                //Log.writeLogClient("Информация о файле отправлена.");
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        public void OpPort()
        {
            OpenPortNat().Wait();
        }

        public static async Task OpenPortNat()
        {
            try
            {
                var nat1 = new NatDiscoverer();
                var cts1 = new CancellationTokenSource(5000);
                var device1 = await nat1.DiscoverDeviceAsync(PortMapper.Upnp, cts1);

                foreach (var mapping in await device1.GetAllMappingsAsync())
                {
                    // Удаляем upnp контейнер FileDownloaderInfo
                    if (mapping.Description.Contains("FileDownloader"))
                    {
                        await device1.DeletePortMapAsync(mapping);
                    }
                }
                foreach (var mapping in await device1.GetAllMappingsAsync())
                {
                    // Удаляем upnp контейнер FileDownloaderInfo
                    if (mapping.Description.Contains("FileDownloaderInfo"))
                    {
                        await device1.DeletePortMapAsync(mapping);
                    }
                }
            }
            catch (Exception me)
            {
                //MessageBox.Show("Ошибка в удалении порта: " + me);
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(me.Message);
            }
            try
            {
                var nat = new NatDiscoverer();
                var cts = new CancellationTokenSource(5000);
                var device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);
                var ip = await device.GetExternalIPAsync();
                int portServerInfo = portServer + 1;
                await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, portServer, portServer, 0, "FileDownloaderFile"));
                await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, portServerInfo, portServerInfo, 0, "FileDownloaderMessage"));
            }
            catch (MappingException me)
            {
                switch (me.ErrorCode)
                {
                    case 718:
                        //MessageBox.Show("Порт уже используется");
                        MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                        Log.writeLog(me.Message);
                        break;
                    case 728:
                        MessageBox.Show("Таблица Upnp заполнена.");
                        Log.writeLog(me.Message);
                        break;
                }
            }
        }

        public void FileUpload()
        {
            try
            {
                try
                {
                    try
                    {
                        if (Encrypt == true)
                        {
                            password = "1";
                            Application.Current.Dispatcher.Invoke(async delegate
                            {
                                password = await Instance.ShowInputAsync("Ключ", "Введите ключ шифрования:");
                            });

                            for(; ; )
                            {
                                if (password.Length != 1)
                                {
                                    TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                    {
                                        TexBoxMessage.Text += System.Environment.NewLine + "Создание подключения...";
                                    }));
                                    Log.writeLog("Создание подключения...");
                                    NetworkStream stream = tcpClient.GetStream();
                                    nameFile = nameFile + ".cry";
                                    nameBuf = Encoding.Unicode.GetBytes(nameFile);
                                    stream.Write(nameBuf, 0, nameBuf.Length);
                                    TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                    {
                                        TexBoxMessage.Text += System.Environment.NewLine + "Информация о файле отправлена!";
                                    }));
                                    Log.writeLog("Информация о файле отправлена.");
                                    Thread.Sleep(500);

                                    byte[] file = new byte[fs.Length];
                                    fs.Read(file, 0, file.Length);
                                    byte[] encrypt = EncryptDecrypt.Encrypt(file, password);
                                    MemoryStream ms = new MemoryStream(encrypt);
                                    BinaryFormatter formatEn = new BinaryFormatter();
                                    byte[] bufEn = new byte[bufSerializeSize];
                                    int countEn;
                                    BinaryReader brEn = new BinaryReader(ms);
                                    long kk = ms.Length;
                                    formatEn.Serialize(stream, kk.ToString());
                                    ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                    {
                                        ProgressBar1.Maximum = kk;
                                    }));
                                    while ((countEn = brEn.Read(bufEn, 0, bufSerializeSize)) > 0)
                                    {
                                        ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                        {
                                            ProgressBar1.Value += countEn;
                                        }));

                                        if (countEn < bufSerializeSize)
                                        {
                                            Array.Resize(ref bufEn, countEn);
                                        }

                                        formatEn.Serialize(stream, bufEn);
                                    }

                                    Application.Current.Dispatcher.Invoke(async delegate
                                    {
                                        await Instance.ShowMessageAsync("Внимание!", "Файл отправлен!");
                                    });
                                    TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                    {
                                        TexBoxMessage.Text += System.Environment.NewLine + "Файл отправлен!";
                                    }));
                                    Log.writeLog("Файл отправлен!");

                                    Thread.Sleep(500);
                                    ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                    {
                                        ProgressBar1.Value = 0;
                                    }));
                                    fs.Close();
                                    MessageBox.Show("Ключ: " + password);
                                    break;
                                }
                                else
                                {

                                }
                                
                            }

                            
                        }
                        else
                        {
                            TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                TexBoxMessage.Text += System.Environment.NewLine + "Создание подключения...";
                            }));
                            Log.writeLog("Создание подключения...");
                            NetworkStream stream = tcpClient.GetStream();
                            nameBuf = Encoding.Unicode.GetBytes(nameFile);
                            stream.Write(nameBuf, 0, nameBuf.Length);

                            TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                TexBoxMessage.Text += System.Environment.NewLine + "Информация о файле отправлена.";
                            }));

                            Log.writeLog("Информация о файле отправлена.");
                            Thread.Sleep(500);

                            BinaryFormatter format = new BinaryFormatter();
                            byte[] buf = new byte[bufSerializeSize];
                            int count;
                            BinaryReader br = new BinaryReader(fs);
                            long k = fs.Length;
                            format.Serialize(stream, k.ToString());

                            ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                ProgressBar1.Maximum = k;
                            }));

                            while ((count = br.Read(buf, 0, bufSerializeSize)) > 0)
                            {

                                ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                {
                                    ProgressBar1.Value += count;
                                }));
                                if (count < bufSerializeSize)
                                {
                                    Array.Resize(ref buf, count);
                                }

                                format.Serialize(stream, buf);
                            }
                            Application.Current.Dispatcher.Invoke(async delegate
                            {
                                await Instance.ShowMessageAsync("Внимание!", "Файл отправлен!");
                            });
                            TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                TexBoxMessage.Text += System.Environment.NewLine + "Файл отправлен!";
                            }));
                            Log.writeLog("Файл отправлен!");

                            Thread.Sleep(500);
                            ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                ProgressBar1.Value = 0;
                            }));
                            fs.Close();
                        }
                    }
                    catch(Exception ex)
                    {
                        BufferCheckedClient();
                        //MessageBox.Show(ex.Message);
                        //MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                        Log.writeLog(ex.Message);
                    }


                }
                catch (ArgumentException ex)
                {
                    BufferCheckedClient();
                    //MessageBox.Show("Ошибка: " + ex);
                    Log.writeLog(ex.Message);
                }
            }
            catch (IOException ioex)
            {
                BufferCheckedClient();
                //MessageBox.Show("Ошибка: " + ioex);
                Log.writeLog(ioex.Message);
            }

            ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
            {
                ProgressBar1.Value = 0;
            }));

        }

        public void FileDownload()
        {
            tcpListener = new TcpListener(IPAddress.Any, portServer);
            tcpListener.Start();
            TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
            {
                TexBoxMessage.Text += System.Environment.NewLine + "Ожидаем подключений!";
            }));

            tcpSClient = new TcpClient();
            tcpSClient = tcpListener.AcceptTcpClient();

            //-----------------чат------------------------
            tcpListenerMessager = new TcpListener(IPAddress.Any, portMessage);
            tcpListenerMessager.Start();

            tcpSClientMessager = new TcpClient();
            tcpSClientMessager = tcpListenerMessager.AcceptTcpClient();

            Thread threadd = new Thread(MessageServer);
            threadd.Start();
            //-----------------чат------------------------

            Application.Current.Dispatcher.Invoke(async delegate
            {
                await Instance.ShowMessageAsync("Подключение", "Новое подключение!");
            });

            TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
            {
                TexBoxMessage.Text += System.Environment.NewLine + "Новое подключение!";
            }));

            SendFileButton.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
            {
                SendFileButton.IsEnabled = true;
            }));
            SendMessageButton.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
            {
                SendMessageButton.IsEnabled = true;
            }));

            TextBoxUserName.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
            {
                username = TextBoxUserName.Text;
                TextBoxUserName.IsEnabled = false;
            }));

            Log.writeLog("Новое подключение!");

            down = true;
            byte[] nameBuffer = new byte[bufSerializeSize];
            try
            {
                try
                {
                    try
                    {
                        try
                        {
                            while (true)
                            {
                                OpPort();
                                NetworkStream stream = tcpSClient.GetStream();
                                int bytes = stream.Read(nameBuffer, 0, nameBuffer.Length);
                                if (bytes == 0)
                                {
                                    BufferCheckedClient();
                                    break;
                                }

                                nameFile = Encoding.Unicode.GetString(nameBuffer, 0, bytes);

                                TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                {
                                    TexBoxMessage.Text += System.Environment.NewLine + "Принимаем файл: Имя - " + nameFile;
                                }));
                                Log.writeLog("Имя файла - " + nameFile);

                                ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                {
                                    ProgressBar1.Value = 0;
                                }));

                                BinaryFormatter outformat = new BinaryFormatter();

                                if (MapMemory)
                                {
                                    try
                                    {
                                        var file = MemoryMappedFile.CreateOrOpen(nameFile, 262144000);
                                        using (BinaryWriter bw = new BinaryWriter(file.CreateViewStream()))
                                        {
                                            //file.Dispose();
                                            count = int.Parse(outformat.Deserialize(stream).ToString());
                                            ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                            {
                                                ProgressBar1.Maximum = count;
                                            }));
                                            int i = 0;
                                            for (; i < count; i += bufSerializeSize)
                                            {
                                                byte[] buf = (byte[])(outformat.Deserialize(stream));
                                                bw.Write(buf);

                                                ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                                {
                                                    ProgressBar1.Value += buf.Length;
                                                }));
                                            }
                                            bw.Close();
                                            if (Encrypt == true)
                                            {
                                                passwordDecrypt = "1";
                                                Application.Current.Dispatcher.Invoke(async delegate
                                                {
                                                    passwordDecrypt = await Instance.ShowInputAsync("Ключ", "Введите ключ дешифрования:");
                                                });

                                                for (; ; )
                                                {
                                                    if(passwordDecrypt.Length != 1)
                                                    {
                                                        var fileRead = MemoryMappedFile.OpenExisting(nameFile);
                                                        using (BinaryReader br = new BinaryReader(fileRead.CreateViewStream()))
                                                        {
                                                            byte[] fileMMF = new byte[count];
                                                            br.Read(fileMMF, 0, fileMMF.Length);
                                                            using (BinaryWriter bwDecrypt = new BinaryWriter(fileRead.CreateViewStream()))
                                                            {
                                                                byte[] decrypt = EncryptDecrypt.Decrypt(fileMMF, passwordDecrypt);
                                                                bwDecrypt.Write(decrypt);
                                                            }
                                                        }
                                                        break;
                                                    }
                                                }
                                            }
                                            
                                            Log.writeLog("Размер файла: " + count + " байт.");
                                            Log.writeLog("Файл сохранен.");
                                            TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                            {
                                                TexBoxMessage.Text += System.Environment.NewLine + "Файл сохранен в память!";
                                            }));
                                            Application.Current.Dispatcher.Invoke(async delegate
                                            {
                                                await Instance.ShowMessageAsync("Подключение", "Файл сохранен в памяти!");
                                            });

                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        BufferCheckedClient();
                                        //MessageBox.Show("Ошибка: " + ex);
                                        //MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                                        Log.writeLog(ex.Message);
                                    }
                                }
                                else if (!MapMemory)
                                {
                                    FileStream fsServer = new FileStream(@"" + Path + @"\" + nameFile, FileMode.OpenOrCreate);
                                    BinaryWriter bw = new BinaryWriter(fsServer);

                                    count = int.Parse(outformat.Deserialize(stream).ToString());
                                    ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                    {
                                        ProgressBar1.Maximum = count;
                                    }));
                                    int i = 0;
                                    for (; i < count; i += bufSerializeSize)
                                    {
                                        byte[] buf = (byte[])(outformat.Deserialize(stream));
                                        bw.Write(buf);

                                        ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                        {
                                            ProgressBar1.Value += buf.Length;
                                        }));
                                    }
                                    bw.Close();
                                    fsServer.Close();

                                    if (Encrypt == true)
                                    {
                                        passwordDecrypt = "1";
                                        Application.Current.Dispatcher.Invoke(async delegate
                                        {
                                            passwordDecrypt = await Instance.ShowInputAsync("Ключ", "Введите ключ дешифрования:");
                                        });
                                        for (; ; )
                                        {
                                            if(passwordDecrypt.Length != 1)
                                            {
                                                byte[] fileDecrypt = File.ReadAllBytes(@"" + Path + @"\" + nameFile);
                                                byte[] decrypt = EncryptDecrypt.Decrypt(fileDecrypt, passwordDecrypt);
                                                nameFile = nameFile.Replace(".cry", "");
                                                FileStream fs = new FileStream(@"" + Path + @"\" + nameFile, FileMode.OpenOrCreate);
                                                BinaryWriter binaryWriterDecrypt = new BinaryWriter(fs);
                                                binaryWriterDecrypt.Write(decrypt);
                                                binaryWriterDecrypt.Close();
                                                File.Delete(@"" + Path + @"\" + nameFile + ".cry");
                                                break;
                                            }
                                        }
                                    }

                                    Log.writeLog("Размер файла: " + count + " байт.");
                                    Log.writeLog("Файл сохранен.");
                                    TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                    {
                                        TexBoxMessage.Text += System.Environment.NewLine + "Файл сохранен в папку " + Path +
                                        Environment.NewLine + "Размер: " + count / 1024 + " Кб";
                                    }));
                                    Application.Current.Dispatcher.Invoke(async delegate
                                    {
                                        await Instance.ShowMessageAsync("Внимание!", "Файл сохранен в папке - " + Environment.NewLine + Path);
                                    });
                                    fsServer.Close();
                                }

                                ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                {
                                    ProgressBar1.Value = 0;
                                }));
                                
                            }
                        }
                        catch (Exception ex)
                        {
                            BufferCheckedClient();
                            //MessageBox.Show("Ошибка: " + ex);
                            Log.writeLog(ex.Message);
                        }
                    }
                    catch (NullReferenceException)
                    {
                        BufferCheckedClient();
                        //MessageBox.Show("Ошибка: " + nrex);
                    }

                }
                catch (DirectoryNotFoundException)
                {
                    BufferCheckedClient();
                    //MessageBox.Show("Ошибка: " + dnfex);
                }
            }
            catch (IOException)
            {
                BufferCheckedClient();
                //MessageBox.Show("Ошибка: " + ioex);
            }
        }

        public void ClientServer()
        {
            //Сервер
            byte[] nameBuffer = new byte[bufSerializeSize];
            try
            {
                try
                {
                    try
                    {
                        while (true)
                        {
                            OpPort();
                            NetworkStream stream = tcpClient.GetStream();
                            int bytes = stream.Read(nameBuffer, 0, nameBuffer.Length);
                            if (bytes == 0)
                            {
                                BufferCheckedClient();
                                break;
                            }

                            nameFile = Encoding.Unicode.GetString(nameBuffer, 0, bytes);

                            Log.writeLog("Имя файла - " + nameFile);
                            TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                TexBoxMessage.Text += System.Environment.NewLine + "Принимаем файл: Имя - " + nameFile;
                            }));

                            ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                ProgressBar1.Value = 0;
                            }));

                            BinaryFormatter outformat = new BinaryFormatter();

                            if (MapMemory)
                            {
                                try
                                {
                                    var file = MemoryMappedFile.CreateOrOpen(nameFile, 262144000);
                                    using (BinaryWriter bw = new BinaryWriter(file.CreateViewStream()))
                                    {
                                        //file.Dispose();
                                        count = int.Parse(outformat.Deserialize(stream).ToString());
                                        ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                        {
                                            ProgressBar1.Maximum = count;
                                        }));
                                        int i = 0;
                                        for (; i < count; i += bufSerializeSize)
                                        {
                                            byte[] buf = (byte[])(outformat.Deserialize(stream));
                                            bw.Write(buf);

                                            ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                            {
                                                ProgressBar1.Value += buf.Length;
                                            }));
                                        }
                                        bw.Close();

                                        if (Encrypt == true)
                                        {
                                            passwordDecrypt = "1";
                                            Application.Current.Dispatcher.Invoke(async delegate
                                            {
                                                passwordDecrypt = await Instance.ShowInputAsync("Ключ", "Введите ключ дешифрования:");
                                            });
                                            for (; ; )
                                            {
                                                if(passwordDecrypt.Length != 1)
                                                {
                                                    var fileRead = MemoryMappedFile.OpenExisting(nameFile);
                                                    using (BinaryReader br = new BinaryReader(fileRead.CreateViewStream()))
                                                    {
                                                        byte[] fileMMF = new byte[count];
                                                        br.Read(fileMMF, 0, fileMMF.Length);
                                                        using (BinaryWriter bwDecrypt = new BinaryWriter(fileRead.CreateViewStream()))
                                                        {
                                                            byte[] decrypt = EncryptDecrypt.Decrypt(fileMMF, passwordDecrypt);
                                                            manualResetEvent.WaitOne();
                                                            bwDecrypt.Write(decrypt);
                                                        }
                                                    }
                                                    break;
                                                }
                                            }
                                        }

                                        Log.writeLog("Размер файла: " + count + " байт.");
                                        Log.writeLog("Файл сохранен.");
                                        TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                        {
                                            TexBoxMessage.Text += System.Environment.NewLine + "Файл сохранен в память!";
                                        }));
                                        Application.Current.Dispatcher.Invoke(async delegate
                                        {
                                            await Instance.ShowMessageAsync("Внимание!", "Файл сохранен в памяти!");
                                        });

                                    }
                                }
                                catch (Exception ex)
                                {
                                    BufferCheckedClient();
                                    // MessageBox.Show("Ошибка: " + ex);
                                    Log.writeLog(ex.Message);
                                }
                            }
                            else if (!MapMemory)
                            {
                                FileStream fsServer = new FileStream(@"" + Path + @"\" + nameFile, FileMode.OpenOrCreate);
                                BinaryWriter bw = new BinaryWriter(fsServer);

                                count = int.Parse(outformat.Deserialize(stream).ToString());
                                ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                {
                                    ProgressBar1.Maximum = count;
                                }));
                                int i = 0;
                                for (; i < count; i += bufSerializeSize)
                                {
                                    byte[] buf = (byte[])(outformat.Deserialize(stream));
                                    bw.Write(buf);

                                    ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                    {
                                        ProgressBar1.Value += buf.Length;
                                    }));
                                }
                                bw.Close();
                                fsServer.Close();

                                if (Encrypt == true)
                                {
                                    passwordDecrypt = "1";
                                    Application.Current.Dispatcher.Invoke(async delegate
                                    {
                                        passwordDecrypt = await Instance.ShowInputAsync("Ключ", "Введите ключ дешифрования:");
                                    });
                                    for (; ; )
                                    {
                                        if(passwordDecrypt.Length != 1)
                                        {
                                            FileStream fileStream = new FileStream(@"" + Path + @"\" + nameFile, FileMode.Open);
                                            byte[] fileDecrypt = new byte[fileStream.Length];
                                            fileStream.Write(fileDecrypt, 0, fileDecrypt.Length);
                                            byte[] decrypt = EncryptDecrypt.Decrypt(fileDecrypt, passwordDecrypt);
                                            BinaryWriter bwDecrypt = new BinaryWriter(fileStream);
                                            bwDecrypt.Write(decrypt);
                                            fileStream.Close();
                                            break;
                                        }
                                    }
                                }

                                Log.writeLog("Размер файла: " + count + " байт.");
                                Log.writeLog("Файл сохранен.");

                                TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                {
                                    TexBoxMessage.Text += System.Environment.NewLine + "Файл сохранен в папку " + Path +
                                    Environment.NewLine + "Размер: " + count / 1024 + " Кб";
                                }));
                                Application.Current.Dispatcher.Invoke(async delegate
                                {
                                    await Instance.ShowMessageAsync("Внимание!", "Файл сохранен в папке - " + Environment.NewLine + Path);
                                });
                                fsServer.Close();
                            }

                            ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                ProgressBar1.Value = 0;
                            }));


                        }
                    }
                    catch (ArgumentException ex)
                    {
                        BufferCheckedClient();
                        //MessageBox.Show("Ошибка: " + ex);
                        Log.writeLog(ex.Message);
                    }
                }
                catch (NullReferenceException nrex)
                {
                    BufferCheckedClient();
                    //MessageBox.Show("Ошибка: " + nrex);
                    Log.writeLog(nrex.Message);
                }
            }
            catch (IOException ioex)
            {
                BufferCheckedClient();
                //MessageBox.Show("Ошибка: " + ioex);
                Log.writeLog(ioex.Message);
            }
        }

        public void ServerClient()
        {
            //Отправка как клиент
            try
            {
                if(Encrypt == true)
                {
                    password = "1";
                    Application.Current.Dispatcher.Invoke(async delegate
                    {
                        password = await Instance.ShowInputAsync("Ключ", "Введите ключ шифрования:");
                    });

                    for (; ; )
                    {
                        if (password.Length != 1)
                        {
                            TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                TexBoxMessage.Text += System.Environment.NewLine + "Создание подключения...";
                            }));
                            Log.writeLog("Создание подключения...");
                            NetworkStream stream = tcpSClient.GetStream();
                            nameFile = nameFile + ".cry";
                            nameBuf = Encoding.Unicode.GetBytes(nameFile);
                            stream.Write(nameBuf, 0, nameBuf.Length);
                            TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                TexBoxMessage.Text += System.Environment.NewLine + "Информация о файле отправлена!";
                            }));
                            Log.writeLog("Информация о файле отправлена.");
                            Thread.Sleep(500);

                            byte[] file = new byte[fs.Length];
                            fs.Read(file, 0, file.Length);
                            byte[] encrypt = EncryptDecrypt.Encrypt(file, password);
                            MemoryStream ms = new MemoryStream(encrypt);
                            BinaryFormatter formatEn = new BinaryFormatter();
                            byte[] bufEn = new byte[bufSerializeSize];
                            int countEn;
                            BinaryReader brEn = new BinaryReader(ms);
                            long kk = ms.Length;
                            formatEn.Serialize(stream, kk.ToString());
                            ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                ProgressBar1.Maximum = kk;
                            }));
                            while ((countEn = brEn.Read(bufEn, 0, bufSerializeSize)) > 0)
                            {
                                ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                                {
                                    ProgressBar1.Value += countEn;
                                }));

                                if (countEn < bufSerializeSize)
                                {
                                    Array.Resize(ref bufEn, countEn);
                                }

                                formatEn.Serialize(stream, bufEn);
                            }

                            Application.Current.Dispatcher.Invoke(async delegate
                            {
                                await Instance.ShowMessageAsync("Внимание!", "Файл отправлен!");
                            });
                            TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                TexBoxMessage.Text += System.Environment.NewLine + "Файл отправлен!";
                            }));
                            Log.writeLog("Файл отправлен!");

                            Thread.Sleep(500);
                            ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                            {
                                ProgressBar1.Value = 0;
                            }));

                            //MessageBox.Show("Ключ: " + password);
                            fs.Close();
                            break;
                        }
                        else
                        {
                        }
                    }
                }
                else
                {
                    TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                    {
                        TexBoxMessage.Text += System.Environment.NewLine + "Создание подключения...";
                    }));
                    Log.writeLog("Создание подключения...");
                    NetworkStream stream = tcpSClient.GetStream();
                    nameBuf = Encoding.Unicode.GetBytes(nameFile);
                    stream.Write(nameBuf, 0, nameBuf.Length);

                    TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                    {
                        TexBoxMessage.Text += System.Environment.NewLine + "Информация о файле отправлена!";
                    }));
                    Log.writeLog("Информация о файле отправлена.");
                    Thread.Sleep(500);

                    BinaryFormatter format = new BinaryFormatter();
                    byte[] buf = new byte[bufSerializeSize];
                    int count;
                    BinaryReader br = new BinaryReader(fs);
                    long k = fs.Length;
                    format.Serialize(stream, k.ToString());

                    ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                    {
                        ProgressBar1.Maximum = k;
                    }));

                    while ((count = br.Read(buf, 0, bufSerializeSize)) > 0)
                    {
                        ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                        {
                            ProgressBar1.Value += count;
                        }));

                        if (count < bufSerializeSize)
                        {
                            Array.Resize(ref buf, count);
                        }
                        format.Serialize(stream, buf);
                    }

                    Log.writeLog("Файл отправлен!");
                    TexBoxMessage.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                    {
                        TexBoxMessage.Text += System.Environment.NewLine + "Файл отправлен!";
                    }));
                    Application.Current.Dispatcher.Invoke(async delegate
                    {
                        await Instance.ShowMessageAsync("Внимание!", "Файл отправлен!");
                    });

                    Thread.Sleep(500);
                    ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                    {
                        ProgressBar1.Value = 0;
                    }));
                }
                
            }
            catch (Exception ex)
            {
                BufferCheckedClient();
                //MessageBox.Show("Ошибка: " + ex);
                Log.writeLog(ex.Message);
            }

            ProgressBar1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
            {
                ProgressBar1.Value = 0;
            }));
        }
        //---------------------------------------------------Шифрование------------------------------------------------------------
        byte[] FileEn;
        string passwordE = "1";
        private void FolderButton_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.FolderBrowserDialog fb = new System.Windows.Forms.FolderBrowserDialog();
                if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    EncryptFilePath = fb.SelectedPath;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "All files (*.*)|*.*";
            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    ChooseFile = new FileStream(openDialog.FileName, FileMode.Open);
                    ChooseFileStr = openDialog.SafeFileName;
                    FileEn = new byte[ChooseFile.Length];
                    int k = Convert.ToInt32(ChooseFile.Length);
                    ChooseFile.Write(FileEn, 0, k);
                    ChooseFile.Close();
                    TextBoxPathFile.Text = openDialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                    Log.writeLog(ex.Message);
                }
            }
        }

        private void OpenFolderButton_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(EncryptFilePath);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(TextBoxPathFile.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        private void DencryptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBoxStatus.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    TextBoxStatus.Text = "";
                }));
                passwordE = "1";
                Application.Current.Dispatcher.Invoke(async delegate
                {
                    passwordE = await Instance.ShowInputAsync("Дешифрование", "Введите ключ:");

                });
                Thread thread = new Thread(DecryptFile);
                thread.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        void DecryptFile()
        {
            try
            {
                for (; ; )
                {
                    if (passwordE.Length != 1)
                    {
                        byte[] decrypt = EncryptDecrypt.Decrypt(FileEn, passwordE);
                        FileStream fs = new FileStream(@"" + EncryptFilePath + @"\" + ChooseFileStr, FileMode.OpenOrCreate);
                        BinaryWriter binaryWriterDecrypt = new BinaryWriter(fs);
                        Thread.Sleep(500);
                        binaryWriterDecrypt.Write(decrypt);
                        binaryWriterDecrypt.Close();
                        break;
                    }
                }

                TextBoxStatus.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    TextBoxStatus.Text = "Готово";
                }));
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        private void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBoxStatus.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    TextBoxStatus.Text = "";
                }));
                passwordE = "1";
                Application.Current.Dispatcher.Invoke(async delegate
                {
                    passwordE = await Instance.ShowInputAsync("Дешифрование", "Введите ключ:");

                });

                Thread thread = new Thread(EncryptFile);
                thread.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        void EncryptFile()
        {
            try
            {
                for (; ; )
                {
                    if (passwordE.Length != 1)
                    {
                        byte[] encrypt = EncryptDecrypt.Encrypt(FileEn, passwordE);
                        FileStream fs = new FileStream(@"" + EncryptFilePath + @"\" + ChooseFileStr, FileMode.OpenOrCreate);
                        BinaryWriter binaryWriterDecrypt = new BinaryWriter(fs);
                        Thread.Sleep(500);
                        binaryWriterDecrypt.Write(encrypt);
                        binaryWriterDecrypt.Close();
                        break;
                    }
                }

                TextBoxStatus.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
                {
                    TextBoxStatus.Text = "Готово";
                }));
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        private void EnryptMessageButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DecryptMessageButton_Click(object sender, RoutedEventArgs e)
        {

        }


        //----------------------------------------------------Настройки-----------------------------------------------------------------

        public string  OffOnLog, OnOffMapMemory;

        private void OnEncryptSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            Encrypt = true;
        }

        private void OnEncryptSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            Encrypt = false;
        }

        private void OnDoubleEncryptSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            DoubleEncrypt = true;
        }

        private void OnDoubleEncryptSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            DoubleEncrypt = false;
        }

        private async void Button_Password_Click(object sender, RoutedEventArgs e)
        {
            password = passwordDecrypt = await this.ShowInputAsync("Ключ шифрования", "Введите ключ шифрования/дешифрования:");

        }

        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.FolderBrowserDialog fb = new System.Windows.Forms.FolderBrowserDialog();
                if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Path = fb.SelectedPath;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Path);
        }

        private void ToggleSwitchMemory_IsCheckedChanged(object sender, EventArgs e)
        {
            MapMemory = true;
            OnOffMapMemory = "1";
        }

        private void ToggleSwitchMemory_Unchecked(object sender, RoutedEventArgs e)
        {
            MapMemory = false;
            OnOffMapMemory = "0";
        }

        private void ToggleSwitchLog_IsCheckedChanged(object sender, EventArgs e)
        {
            OffOnLog = "1";
            Log.enableLog = "1";
        }

        private void ToggleSwitchLog_Unchecked(object sender, RoutedEventArgs e)
        {
            OffOnLog = "0";
            Log.enableLog = "0";
        }

        private void BufferSerializeNumirc_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            bufSerializeSize = Convert.ToInt32(BufferSerializeNumirc.Value);
            MessageBox.Show(""+bufSerializeSize);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Option.SaveCfg(TextboxPortInput.Text, bufSerializeSize.ToString(), Path, OffOnLog, OnOffMapMemory);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка. Подробнее в лог-файле.");
                Log.writeLog(ex.Message);
            }
        }

    }
}
