//ÇALIŞAN VERSİYON

//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.IO.Ports;
//using System.Net;
//using System.Net.Sockets;
//using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService;
//using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;

//namespace Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.TcpSerialPortBackgroundService
//{
//    public class TcpSerialPortBackgroundService : BackgroundService
//    {
//        private readonly ITcpService tcpService;
//        private readonly ISerialPortService serialPortService;
//        private readonly IConfiguration configuration;

//        private TcpListener tcpServer;
//        private TcpClient tcpClient;
//        private SerialPort serialPort;

//        public TcpSerialPortBackgroundService(ITcpService tcpService, ISerialPortService serialPortService, IConfiguration configuration)
//        {
//            this.tcpService = tcpService;
//            this.serialPortService = serialPortService;
//            this.configuration = configuration;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            try
//            {
//                // === TCP yapılandırması ===
//                string? ipString = configuration["TcpServer:Ip"];
//                string? portString = configuration["TcpServer:Port"];

//                if (!IPAddress.TryParse(ipString, out var ipAddress))
//                {
//                    Console.WriteLine($"Geçersiz IP ({ipString}), varsayılan 0.0.0.0 kullanılacak.");
//                    ipAddress = IPAddress.Any;
//                }

//                if (!int.TryParse(portString, out int tcpPort) || tcpPort <= 0 || tcpPort > 65535)
//                {
//                    Console.WriteLine($"Geçersiz TCP Port ({portString}), varsayılan 7000 kullanılacak.");
//                    tcpPort = 7000;
//                }

//                // === SerialPort yapılandırması ===
//                string? portName = configuration["SerialPort:PortName"];
//                string? baudRateStr = configuration["SerialPort:BaudRate"];

//                if (string.IsNullOrWhiteSpace(portName))
//                {
//                    Console.WriteLine("Serial port adı boş — COM12 kullanılacak.");
//                    portName = "COM12";
//                }

//                if (!int.TryParse(baudRateStr, out int baudRate) || baudRate <= 0)
//                {
//                    Console.WriteLine($"Geçersiz baud rate ({baudRateStr}), varsayılan 19200 kullanılacak.");
//                    baudRate = 19200;
//                }

//                // === TCP Sunucu ve SerialPort başlat ===
//                tcpServer = tcpService.CreateTcpServer(ipAddress, tcpPort);
//                tcpService.StartTcpServer(tcpServer);

//                serialPort = serialPortService.CreateSerialPort(portName, baudRate);
//                if (serialPortService.OpenSerialPort(serialPort))
//                    Console.WriteLine($"Serial port açık: {serialPort.PortName} @ {serialPort.BaudRate} baud");

//                Console.WriteLine($"TCP Sunucu dinliyor: {ipAddress}:{tcpPort}");

//                // === İstemci kabul döngüsü ===
//                while (!stoppingToken.IsCancellationRequested)
//                {
//                    if (tcpClient is null)
//                    {
//                        try
//                        {
//                            Console.WriteLine("TCP client bekleniyor...");
//                            tcpClient = await tcpServer.AcceptTcpClientAsync(stoppingToken);
//                            Console.WriteLine($"TCP client bağlandı: {tcpClient.Client.RemoteEndPoint}");

//                            // TCP→Serial ve Serial→TCP task’lerini başlat
//                            _ = Task.Run(async () =>
//                            {
//                                try
//                                {
//                                    await tcpService.ReadTcpAsync(tcpClient, serialPortService, stoppingToken);
//                                }
//                                catch (Exception ex)
//                                {
//                                    Console.WriteLine("TCP okuma task hatası: " + ex.Message);
//                                }
//                                finally
//                                {
//                                    tcpClient?.Close();
//                                    tcpClient = null;
//                                }
//                            }, stoppingToken);

//                            _ = Task.Run(async () =>
//                            {
//                                try
//                                {
//                                    Console.WriteLine("Task SerialPortData Okuma Başlatıldı");
//                                    await serialPortService.ReadSerialPortDataAsync(tcpClient, tcpService, serialPort, stoppingToken);
//                                }
//                                catch (Exception ex)
//                                {
//                                    Console.WriteLine("Serial port task hatası: " + ex.Message);
//                                }
//                            }, stoppingToken);
//                        }
//                        catch (Exception ex)
//                        {
//                            Console.WriteLine("Client kabul hatası: " + ex.Message);
//                            tcpClient?.Close();
//                            tcpClient = null;
//                        }
//                    }

//                    // TCP client koparsa null yapıp yeniden bekleyecek
//                    if (tcpClient != null && !tcpClient.Connected)
//                    {
//                        Console.WriteLine("TCP client bağlantısı koptu, yeniden bekleniyor...");
//                        tcpClient?.Close();
//                        tcpClient = null;
//                    }
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                Console.WriteLine("Background service durduruldu.");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"TcpSerialPortBackgroundService hatası: {ex.Message}");
//            }
//        }
//    }
//}


// bu da çalışıyor

//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.IO.Ports;
//using System.Net;
//using System.Net.Sockets;
//using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService;
//using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;

//namespace Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.TcpSerialPortBackgroundService
//{
//    public class TcpSerialPortBackgroundService : BackgroundService
//    {
//        private readonly ITcpService tcpService;
//        private readonly ISerialPortService serialPortService;
//        private readonly IConfiguration configuration;

//        private TcpListener tcpServer;
//        private SerialPort serialPort;

//        public TcpSerialPortBackgroundService(ITcpService tcpService, ISerialPortService serialPortService, IConfiguration configuration)
//        {
//            this.tcpService = tcpService;
//            this.serialPortService = serialPortService;
//            this.configuration = configuration;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            try
//            {
//                // === TCP yapılandırması ===
//                string? ipString = configuration["TcpServer:Ip"];
//                string? portString = configuration["TcpServer:Port"];

//                if (!IPAddress.TryParse(ipString, out var ipAddress))
//                    ipAddress = IPAddress.Any;

//                if (!int.TryParse(portString, out int tcpPort) || tcpPort <= 0 || tcpPort > 65535)
//                    tcpPort = 7000;

//                // === SerialPort yapılandırması ===
//                string? portName = configuration["SerialPort:PortName"];
//                string? baudRateStr = configuration["SerialPort:BaudRate"];
//                if (string.IsNullOrWhiteSpace(portName))
//                    portName = "COM12";
//                if (!int.TryParse(baudRateStr, out int baudRate) || baudRate <= 0)
//                    baudRate = 19200;

//                // === TCP Sunucu ve SerialPort başlat ===
//                tcpServer = tcpService.CreateTcpServer(ipAddress, tcpPort);
//                tcpService.StartTcpServer(tcpServer);
//                Console.WriteLine($"TCP Sunucu dinliyor: {ipAddress}:{tcpPort}");

//                serialPort = serialPortService.CreateSerialPort(portName, baudRate);
//                TryOpenSerialPort();

//                // === Ana döngü: client bekleme ve yeniden bağlanma ===
//                while (!stoppingToken.IsCancellationRequested)
//                {
//                    TcpClient tcpClient = null;

//                    try
//                    {
//                        Console.WriteLine("Yeni TCP client bekleniyor...");
//                        tcpClient = await tcpServer.AcceptTcpClientAsync(stoppingToken);
//                        Console.WriteLine($"TCP client bağlandı: {tcpClient.Client.RemoteEndPoint}");

//                        // TCP→Serial ve Serial→TCP task’leri paralel çalışır
//                        var tcpToSerialTask = Task.Run(() => SafeTcpToSerialAsync(tcpClient, stoppingToken), stoppingToken);
//                        var serialToTcpTask = Task.Run(() => SafeSerialToTcpAsync(tcpClient, stoppingToken), stoppingToken);

//                        // **İki task’in de bitmesini bekle**, client task süresince açık kalır
//                        await Task.WhenAll(tcpToSerialTask, serialToTcpTask);

//                        // Task’ler tamamlandıktan sonra client dispose edilir
//                        try { tcpClient.GetStream()?.Close(); } catch { }
//                        try { tcpClient.Close(); } catch { }
//                        try { tcpClient.Dispose(); } catch { }

//                        // Serial port kapalıysa tekrar aç
//                        if (!serialPort.IsOpen)
//                            TryOpenSerialPort();

//                        Console.WriteLine("TCP client kapatıldı, yeniden bekleniyor...");
//                        await Task.Delay(500, stoppingToken); // kısa bekleme
//                    }
//                    catch (OperationCanceledException)
//                    {
//                        break;
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine($"TCP client hatası: {ex.Message}");
//                        try { tcpClient?.Close(); } catch { }
//                    }
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                Console.WriteLine("Background service durduruldu.");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"TcpSerialPortBackgroundService hatası: {ex.Message}");
//            }
//            finally
//            {
//                try
//                {
//                    tcpServer?.Stop();
//                    if (serialPort?.IsOpen == true)
//                        serialPort.Close();
//                }
//                catch { }
//            }
//        }

//        private async Task SafeTcpToSerialAsync(TcpClient client, CancellationToken token)
//        {
//            try
//            {
//                await tcpService.ReadTcpAsync(client, serialPortService, token);
//            }
//            catch (ObjectDisposedException) { }
//            catch (IOException) { }
//            catch (OperationCanceledException) { }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"TCP→Serial hatası: {ex.Message}");
//            }
//        }

//        private async Task SafeSerialToTcpAsync(TcpClient client, CancellationToken token)
//        {
//            try
//            {
//                if (!serialPort.IsOpen) return;
//                await serialPortService.ReadSerialPortDataAsync(client, tcpService, serialPort, token);
//            }
//            catch (ObjectDisposedException) { }
//            catch (IOException) { }
//            catch (OperationCanceledException) { }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Serial→TCP hatası: {ex.Message}");
//            }
//        }

//        private void TryOpenSerialPort()
//        {
//            try
//            {
//                if (serialPort == null) return;
//                if (!serialPort.IsOpen)
//                    serialPortService.OpenSerialPort(serialPort);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Serial port açma hatası: {ex.Message}");
//            }
//        }
//    }
//}


// BU KOD EN İYİ ÇALIŞAN 

//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.IO.Ports;
//using System.Net;
//using System.Net.Sockets;
//using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService;
//using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;

//namespace Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.TcpSerialPortBackgroundService
//{
//    public class TcpSerialPortBackgroundService : BackgroundService
//    {
//        private readonly ITcpService tcpService;
//        private readonly ISerialPortService serialPortService;
//        private readonly IConfiguration configuration;

//        private TcpListener tcpServer;
//        private TcpClient tcpClient;     // FIELD olarak tutuluyor
//        private SerialPort serialPort;

//        public TcpSerialPortBackgroundService(
//            ITcpService tcpService,
//            ISerialPortService serialPortService,
//            IConfiguration configuration)
//        {
//            this.tcpService = tcpService;
//            this.serialPortService = serialPortService;
//            this.configuration = configuration;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            try
//            {
//                // === TCP Ayarları ===
//                string? ipString = configuration["TcpServer:Ip"];
//                string? portString = configuration["TcpServer:Port"];

//                if (!IPAddress.TryParse(ipString, out var ipAddress))
//                    ipAddress = IPAddress.Any;

//                if (!int.TryParse(portString, out int tcpPort) || tcpPort <= 0 || tcpPort > 65535)
//                    tcpPort = 7000;

//                // === SerialPort Ayarları ===
//                string? portName = configuration["SerialPort:PortName"];
//                string? baudRateStr = configuration["SerialPort:BaudRate"];
//                if (string.IsNullOrWhiteSpace(portName))
//                    portName = "COM12";
//                if (!int.TryParse(baudRateStr, out int baudRate) || baudRate <= 0)
//                    baudRate = 19200;

//                // === TCP Server & SerialPort başlat ===
//                tcpServer = tcpService.CreateTcpServer(ipAddress, tcpPort);
//                tcpService.StartTcpServer(tcpServer);
//                Console.WriteLine($"TCP Sunucu dinliyor: {ipAddress}:{tcpPort}");

//                serialPort = serialPortService.CreateSerialPort(portName, baudRate);
//                TryOpenSerialPort();

//                while (!stoppingToken.IsCancellationRequested)
//                {
//                    try
//                    {
//                        // Bağlı client yoksa yenisini bekle
//                        if (!IsClientConnected())
//                        {
//                            Console.WriteLine("Yeni TCP client bekleniyor...");
//                            var accepted = await tcpServer.AcceptTcpClientAsync(stoppingToken);

//                            // varsa eskiyi kapat
//                            CloseClient();

//                            tcpClient = accepted;
//                            Console.WriteLine($"TCP client bağlandı: {tcpClient.Client.RemoteEndPoint}");

//                            _ = Task.Run(() => RunClientAsync(tcpClient, stoppingToken), stoppingToken);
//                        }

//                        await Task.Delay(200, stoppingToken);
//                    }
//                    catch (OperationCanceledException)
//                    {
//                        break;
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine($"Ana döngü hatası: {ex.Message}");
//                        CloseClient();
//                        await Task.Delay(1000, stoppingToken);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"TcpSerialPortBackgroundService genel hata: {ex.Message}");
//            }
//            finally
//            {
//                try
//                {
//                    tcpServer?.Stop();
//                    CloseClient();
//                    if (serialPort?.IsOpen == true)
//                        serialPort.Close();
//                    Console.WriteLine("Servis kapatıldı.");
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Kapatma hatası: {ex.Message}");
//                }
//            }
//        }

//        private async Task RunClientAsync(TcpClient client, CancellationToken stoppingToken)
//        {
//            try
//            {
//                // TCP bağlandı → Seri portu aç
//                if (!serialPort.IsOpen)
//                {
//                    TryOpenSerialPort();
//                    if (serialPort.IsOpen)
//                        Console.WriteLine("TCP bağlantısı geldi, seri port açıldı.");
//                }

//                var tcpToSerialTask = Task.Run(() => SafeTcpToSerialAsync(client, stoppingToken), stoppingToken);
//                var serialToTcpTask = Task.Run(() => SafeSerialToTcpAsync(client, stoppingToken), stoppingToken);

//                await Task.WhenAny(tcpToSerialTask, serialToTcpTask);
//                await Task.WhenAll(tcpToSerialTask, serialToTcpTask);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"RunClientAsync hata: {ex.Message}");
//            }
//            finally
//            {
//                if (tcpClient == client)
//                {
//                    Console.WriteLine("Client kapatılıyor...");
//                    CloseClient();
//                }
//            }
//        }

//        private async Task SafeTcpToSerialAsync(TcpClient client, CancellationToken token)
//        {
//            try
//            {
//                await tcpService.ReadTcpAsync(client, serialPortService, token);
//            }
//            catch (OperationCanceledException) { }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"TCP→Serial hata: {ex.Message}");
//            }
//        }

//        private async Task SafeSerialToTcpAsync(TcpClient client, CancellationToken token)
//        {
//            try
//            {
//                if (!serialPort.IsOpen)
//                {
//                    TryOpenSerialPort();
//                    if (!serialPort.IsOpen)
//                        return;
//                }

//                await serialPortService.ReadSerialPortDataAsync(client, tcpService, serialPort, token);
//            }
//            catch (OperationCanceledException) { }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Serial→TCP hata: {ex.Message}");
//            }
//        }

//        private bool IsClientConnected()
//        {
//            try
//            {
//                if (tcpClient == null) return false;
//                if (tcpClient.Client == null) return false;
//                if (!tcpClient.Client.Connected) return false;
//                if (tcpClient.Client.Poll(0, SelectMode.SelectRead) && tcpClient.Available == 0) return false;
//                return true;
//            }
//            catch { return false; }
//        }

//        private void CloseClient()
//        {
//            try
//            {
//                if (tcpClient != null)
//                {
//                    try { tcpClient.GetStream()?.Close(); } catch { }
//                    try { tcpClient.Close(); } catch { }
//                    try { tcpClient.Dispose(); } catch { }
//                }

//                // TCP koptuysa seri portu kapat
//                if (serialPort != null && serialPort.IsOpen)
//                {
//                    try
//                    {
//                        serialPort.Close();
//                        Console.WriteLine("TCP bağlantısı koptu, seri port kapatıldı.");
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine($"Seri port kapatma hatası: {ex.Message}");
//                    }
//                }
//            }
//            catch { }
//            finally
//            {
//                tcpClient = null;
//            }
//        }

//        private void TryOpenSerialPort()
//        {
//            try
//            {
//                if (serialPort == null) return;
//                if (!serialPort.IsOpen)
//                    serialPortService.OpenSerialPort(serialPort);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Serial port açma hatası: {ex.Message}");
//            }
//        }
//    }
//}

//ÇALIŞAN KODDUR FİXLENMİŞ

//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.IO.Ports;
//using System.Net;
//using System.Net.Sockets;
//using System.Threading;
//using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService;
//using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;

//namespace Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.TcpSerialPortBackgroundService
//{
//    public class TcpSerialPortBackgroundService : BackgroundService
//    {
//        private readonly ITcpService tcpService;
//        private readonly ISerialPortService serialPortService;
//        private readonly IConfiguration configuration;

//        private TcpListener tcpServer;
//        private volatile TcpClient tcpClient;  // thread-safe erişim için volatile
//        private SerialPort serialPort;

//        public TcpSerialPortBackgroundService(
//            ITcpService tcpService,
//            ISerialPortService serialPortService,
//            IConfiguration configuration)
//        {
//            this.tcpService = tcpService;
//            this.serialPortService = serialPortService;
//            this.configuration = configuration;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            try
//            {
//                // === TCP Ayarları ===
//                string? ipString = configuration["TcpServer:Ip"];
//                string? portString = configuration["TcpServer:Port"];

//                if (!IPAddress.TryParse(ipString, out var ipAddress))
//                    ipAddress = IPAddress.Any;

//                if (!int.TryParse(portString, out int tcpPort) || tcpPort <= 0 || tcpPort > 65535)
//                    tcpPort = 7000;

//                // === SerialPort Ayarları ===
//                string? portName = configuration["SerialPort:PortName"];
//                string? baudRateStr = configuration["SerialPort:BaudRate"];
//                if (string.IsNullOrWhiteSpace(portName))
//                    portName = "COM12";
//                if (!int.TryParse(baudRateStr, out int baudRate) || baudRate <= 0)
//                    baudRate = 19200;

//                // === TCP Server & SerialPort başlat ===
//                tcpServer = tcpService.CreateTcpServer(ipAddress, tcpPort);
//                tcpService.StartTcpServer(tcpServer);
//                Console.WriteLine($"TCP Sunucu dinliyor: {ipAddress}:{tcpPort}");

//                serialPort = serialPortService.CreateSerialPort(portName, baudRate);
//                TryOpenSerialPort();

//                while (!stoppingToken.IsCancellationRequested)
//                {
//                    try
//                    {
//                        if (!IsClientConnected())
//                        {
//                            Console.WriteLine("Yeni TCP client bekleniyor...");
//                            var accepted = await tcpServer.AcceptTcpClientAsync(stoppingToken);

//                            // Eski client varsa kapat
//                            var oldClient = tcpClient;
//                            if (oldClient != null)
//                                CloseClientInternal(oldClient);

//                            tcpClient = accepted;
//                            Console.WriteLine($"TCP client bağlandı: {tcpClient.Client.RemoteEndPoint}");

//                            _ = Task.Run(() => RunClientAsync(tcpClient, stoppingToken), stoppingToken);
//                        }

//                        await Task.Delay(200, stoppingToken);
//                    }
//                    catch (OperationCanceledException)
//                    {
//                        break;
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine($"Ana döngü hatası: {ex.Message}");
//                        var current = tcpClient;
//                        if (current != null)
//                            CloseClientInternal(current);
//                        await Task.Delay(1000, stoppingToken);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"TcpSerialPortBackgroundService genel hata: {ex.Message}");
//            }
//            finally
//            {
//                try
//                {
//                    tcpServer?.Stop();
//                    var current = tcpClient;
//                    if (current != null)
//                        CloseClientInternal(current);
//                    if (serialPort?.IsOpen == true)
//                        serialPort.Close();
//                    Console.WriteLine("Servis kapatıldı.");
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Kapatma hatası: {ex.Message}");
//                }
//            }
//        }

//        private async Task RunClientAsync(TcpClient client, CancellationToken stoppingToken)
//        {
//            try
//            {
//                // TCP bağlandı → Seri portu aç
//                if (!serialPort.IsOpen)
//                {
//                    TryOpenSerialPort();
//                    if (serialPort.IsOpen)
//                        Console.WriteLine("TCP bağlantısı geldi, seri port açıldı.");
//                }

//                var tcpToSerialTask = Task.Run(() => SafeTcpToSerialAsync(client, stoppingToken), stoppingToken);
//                var serialToTcpTask = Task.Run(() => SafeSerialToTcpAsync(client, stoppingToken), stoppingToken);

//                await Task.WhenAny(tcpToSerialTask, serialToTcpTask);
//                await Task.WhenAll(tcpToSerialTask, serialToTcpTask);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"RunClientAsync hata: {ex.Message}");
//            }
//            finally
//            {
//                // sadece kendi client’ını kapat
//                if (ReferenceEquals(tcpClient, client))
//                {
//                    Console.WriteLine("Client kapatılıyor...");
//                    CloseClientInternal(client);
//                }
//            }
//        }

//        private async Task SafeTcpToSerialAsync(TcpClient client, CancellationToken token)
//        {
//            try
//            {
//                await tcpService.ReadTcpAsync(client, serialPortService, token);
//            }
//            catch (OperationCanceledException) { }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"TCP→Serial hata: {ex.Message}");
//            }
//        }

//        private async Task SafeSerialToTcpAsync(TcpClient client, CancellationToken token)
//        {
//            try
//            {
//                if (!serialPort.IsOpen)
//                {
//                    TryOpenSerialPort();
//                    if (!serialPort.IsOpen)
//                        return;
//                }

//                await serialPortService.ReadSerialPortDataAsync(client, tcpService, serialPort, token);
//            }
//            catch (OperationCanceledException) { }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Serial→TCP hata: {ex.Message}");
//            }
//        }

//        private bool IsClientConnected()
//        {
//            try
//            {
//                var client = tcpClient;
//                if (client == null) return false;
//                if (client.Client == null) return false;
//                if (!client.Client.Connected) return false;
//                if (client.Client.Poll(0, SelectMode.SelectRead) && client.Available == 0) return false;
//                return true;
//            }
//            catch { return false; }
//        }

//        private void CloseClientInternal(TcpClient client)
//        {
//            try
//            {
//                if (client != null)
//                {
//                    try { client.GetStream()?.Close(); } catch { }
//                    try { client.Close(); } catch { }
//                    try { client.Dispose(); } catch { }
//                }

//                // TCP koptuysa seri portu kapat
//                if (ReferenceEquals(tcpClient, client))
//                {
//                    tcpClient = null;
//                    if (serialPort != null && serialPort.IsOpen)
//                    {
//                        try
//                        {
//                            serialPort.Close();
//                            Console.WriteLine("TCP bağlantısı koptu, seri port kapatıldı.");
//                        }
//                        catch (Exception ex)
//                        {
//                            Console.WriteLine($"Seri port kapatma hatası: {ex.Message}");
//                        }
//                    }
//                }
//            }
//            catch { }
//        }

//        private void TryOpenSerialPort()
//        {
//            try
//            {
//                if (serialPort == null) return;
//                if (!serialPort.IsOpen)
//                    serialPortService.OpenSerialPort(serialPort);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Serial port açma hatası: {ex.Message}");
//            }
//        }
//    }
//}

// ÇALIŞAN KOD BU EN GÜNCEL ÇALIŞAN
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;

namespace Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.TcpSerialPortBackgroundService
{
    public class TcpSerialPortBackgroundService : BackgroundService
    {
        private readonly ITcpService tcpService;
        private readonly ISerialPortService serialPortService;
        private readonly IConfiguration configuration;

        private TcpListener tcpServer;
        private TcpClient tcpClient;
        private SerialPort serialPort;

        public TcpSerialPortBackgroundService(
            ITcpService tcpService,
            ISerialPortService serialPortService,
            IConfiguration configuration)
        {
            this.tcpService = tcpService;
            this.serialPortService = serialPortService;
            this.configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // === TCP Ayarları ===
                string? ipString = configuration["TcpServer:Ip"];
                string? portString = configuration["TcpServer:Port"];

                if (!IPAddress.TryParse(ipString, out var ipAddress))
                    ipAddress = IPAddress.Any;

                if (!int.TryParse(portString, out int tcpPort) || tcpPort <= 0 || tcpPort > 65535)
                    tcpPort = 7000;

                // === SerialPort Ayarları ===
                string? portName = configuration["SerialPort:PortName"];
                string? baudRateStr = configuration["SerialPort:BaudRate"];
                if (string.IsNullOrWhiteSpace(portName))
                    portName = "COM12";
                if (!int.TryParse(baudRateStr, out int baudRate) || baudRate <= 0)
                    baudRate = 19200;

                // === TCP Server başlat ===
                tcpServer = tcpService.CreateTcpServer(ipAddress, tcpPort);
                tcpService.StartTcpServer(tcpServer);
                Console.WriteLine($"TCP Sunucu dinliyor: {ipAddress}:{tcpPort}");

                await serialPortService.ResetDummySerialPort();

                serialPort = serialPortService.CreateSerialPort(portName, baudRate);
                await serialPortService.ResetSerialPortProxy();

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        Console.WriteLine("Yeni TCP client bekleniyor...\n");
                        var client = await tcpServer.AcceptTcpClientAsync(stoppingToken);
                        tcpClient = client;
                        Console.WriteLine($"TCP client bağlandı: {tcpClient.Client.RemoteEndPoint}");

                        await TryOpenSerialPort();
                        if (serialPort.IsOpen)
                            Console.WriteLine("Seri port açıldı.");

                        // Client bağlantısı bitene kadar burada kal
                        await RunClientAsync(tcpClient, stoppingToken);

                        Console.WriteLine("Client bağlantısı sona erdi, tekrar bekleniyor...\n");
                        CloseClient();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ana döngü hatası: {ex.Message}");
                        CloseClient();
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TcpSerialPortBackgroundService genel hata: {ex.Message}");
            }
            finally
            {
                tcpServer?.Stop();
                CloseClient();
                if (serialPort?.IsOpen == true)
                    await serialPortService.ResetSerialPortProxy(); // temizle
                if (serialPort?.IsOpen == true)
                    await serialPortService.CloseSerialPort();
                Console.WriteLine("Servis kapatıldı.");
            }
        }

        private async Task RunClientAsync(TcpClient client, CancellationToken stoppingToken)
        {
            try
            {
                var tcpToSerial = SafeTcpToSerialAsync(client, stoppingToken);
                var serialToTcp = SafeSerialToTcpAsync(client, stoppingToken);

                // Biri hata verirse bağlantı kopar
                await Task.WhenAll(tcpToSerial, serialToTcp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RunClientAsync hata: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Client bağlantısı kapatılıyor...");
                CloseClient();
                if (serialPort != null)
                    await serialPortService.ResetSerialPortProxy();
            }
        }

        private async Task SafeTcpToSerialAsync(TcpClient client, CancellationToken token)
        {
            try
            {
                await tcpService.ReadTcpAsync(client, serialPortService, token);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"TCP→Serial bağlantı koptu: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TCP→Serial hata: {ex.Message}");
            }
        }

        private async Task SafeSerialToTcpAsync(TcpClient client, CancellationToken token)
        {
            try
            {
                if (!serialPort.IsOpen)
                {
                    await TryOpenSerialPort();
                    if (!serialPort.IsOpen)
                        return;
                }

                await serialPortService.ReadSerialPortDataAsync(client, tcpService, token);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Serial→TCP bağlantı koptu: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serial→TCP hata: {ex.Message}");
            }
        }

        private void CloseClient()
        {
            try
            {
                if (tcpClient != null)
                {
                    try { tcpClient.GetStream()?.Close(); } catch { }
                    try { tcpClient.Close(); } catch { }
                    try { tcpClient.Dispose(); } catch { }
                }

                if (serialPort != null && serialPort.IsOpen)
                {
                    try
                    {
                        serialPort.Close();
                        Console.WriteLine("TCP bağlantısı koptu, seri port kapatıldı.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Seri port kapatma hatası: {ex.Message}");
                    }
                }
            }
            catch { }
            finally
            {
                tcpClient = null;
            }
        }

        private async Task TryOpenSerialPort()
        {
            try
            {
                if (serialPort == null) return;

                // Eğer port daha önce takılı kaldıysa, temizlemeden açma deneme.
                // ResetSerialPortProxy smart; açık değilse no-op (veya sadece event detach yapar).
                await serialPortService.ResetSerialPortProxy();

                if (!serialPort.IsOpen)
                    await serialPortService.OpenSerialPort();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serial port açma hatası: {ex.Message}");
            }
        }



        //private async Task TryOpenSerialPort()
        //{
        //    try
        //    {
        //        if (serialPort == null) return;
        //        if (!serialPort.IsOpen)
        //            await serialPortService.OpenSerialPort(serialPort);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Serial port açma hatası: {ex.Message}");
        //    }
        //}
    }
}


//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.IO.Ports;
//using System.Net;
//using System.Net.Sockets;
//using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService;
//using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;

//namespace Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.TcpSerialPortBackgroundService
//{
//    public class TcpSerialPortBackgroundService : BackgroundService
//    {
//        private readonly ITcpService tcpService;
//        private readonly ISerialPortService serialPortService;
//        private readonly IConfiguration configuration;

//        private TcpListener tcpServer;
//        private TcpClient tcpClient;
//        private SerialPort serialPort;

//        public TcpSerialPortBackgroundService(
//            ITcpService tcpService,
//            ISerialPortService serialPortService,
//            IConfiguration configuration)
//        {
//            this.tcpService = tcpService;
//            this.serialPortService = serialPortService;
//            this.configuration = configuration;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            try
//            {
//                // === TCP Ayarları ===
//                string? ipString = configuration["TcpServer:Ip"];
//                string? portString = configuration["TcpServer:Port"];

//                if (!IPAddress.TryParse(ipString, out var ipAddress))
//                    ipAddress = IPAddress.Any;

//                if (!int.TryParse(portString, out int tcpPort) || tcpPort <= 0 || tcpPort > 65535)
//                    tcpPort = 7000;

//                // === SerialPort Ayarları ===
//                string? portName = configuration["SerialPort:PortName"];
//                string? baudRateStr = configuration["SerialPort:BaudRate"];
//                if (string.IsNullOrWhiteSpace(portName))
//                    portName = "COM12";
//                if (!int.TryParse(baudRateStr, out int baudRate) || baudRate <= 0)
//                    baudRate = 19200;

//                // === TCP Server başlat ===
//                tcpServer = tcpService.CreateTcpServer(ipAddress, tcpPort);
//                tcpService.StartTcpServer(tcpServer);
//                Console.WriteLine($"TCP Sunucu dinliyor: {ipAddress}:{tcpPort}");

//                // SerialPort oluştur ama açma burada değil, client bağlanınca açacağız
//                serialPort = serialPortService.CreateSerialPort(portName, baudRate);

//                while (!stoppingToken.IsCancellationRequested)
//                {
//                    try
//                    {
//                        Console.WriteLine("Yeni TCP client bekleniyor...\n");
//                        var client = await tcpServer.AcceptTcpClientAsync(stoppingToken);
//                        tcpClient = client;
//                        Console.WriteLine($"TCP client bağlandı: {tcpClient.Client.RemoteEndPoint}");

//                        // 🔹 Client bağlanınca serial portu aç
//                        TryOpenSerialPort();

//                        await RunClientAsync(tcpClient, stoppingToken);

//                        Console.WriteLine("Client bağlantısı sona erdi, tekrar bekleniyor...\n");
//                        CloseClient();
//                    }
//                    catch (OperationCanceledException)
//                    {
//                        break;
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine($"Ana döngü hatası: {ex.Message}");
//                        CloseClient();
//                        await Task.Delay(1000, stoppingToken);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"TcpSerialPortBackgroundService genel hata: {ex.Message}");
//            }
//            finally
//            {
//                tcpServer?.Stop();
//                CloseClient();
//                if (serialPort?.IsOpen == true)
//                    serialPort.Close();
//                Console.WriteLine("Servis kapatıldı.");
//            }
//        }

//        private async Task RunClientAsync(TcpClient client, CancellationToken stoppingToken)
//        {
//            try
//            {
//                // TCP→Serial veri yönü
//                var tcpToSerial = HandleTcpToSerialAsync(client, stoppingToken);

//                // Serial→TCP veri yönü
//                var serialToTcp = HandleSerialToTcpAsync(client, stoppingToken);

//                await Task.WhenAll(tcpToSerial, serialToTcp);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"RunClientAsync hata: {ex.Message}");
//            }
//            finally
//            {
//                Console.WriteLine("Client bağlantısı kapatılıyor...");
//                CloseClient();
//            }
//        }

//        private async Task HandleTcpToSerialAsync(TcpClient client, CancellationToken token)
//        {
//            try
//            {
//                while (!token.IsCancellationRequested)
//                {
//                    var frame = await tcpService.ReadTcpFrameAsync(client, token);
//                    if (frame == null)
//                    {
//                        Console.WriteLine("TCP bağlantısı kapandı.");
//                        break;
//                    }

//                    // ✅ Gelen frame’i doğrula
//                    var checkedFrame = tcpService.ProcessClientBuffer(frame);
//                    if (checkedFrame == null)
//                        continue;

//                    // ✅ Seri port açık değilse aç
//                    TryOpenSerialPort();

//                    // ✅ Seri porta gönder
//                    await serialPortService.SendSerialPortData(checkedFrame);

//                    // ✅ Şimdi 5 saniye boyunca seri port dinle
//                    Console.WriteLine("Seri port 5 saniye boyunca dinleniyor...");
//                    using var lifetime = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
//                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token);

//                    await serialPortService.ReadSerialPortDataAsync(client, tcpService, serialPort, linked.Token);

//                    Console.WriteLine("5 saniye doldu, seri port kapatılıyor...");
//                    serialPortService.CloseSerialPort(serialPort);
//                }
//            }
//            catch (IOException ex)
//            {
//                Console.WriteLine($"TCP→Serial bağlantı koptu: {ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"TCP→Serial hata: {ex.Message}");
//            }
//        }


//        private async Task HandleSerialToTcpAsync(TcpClient client, CancellationToken token)
//        {
//            try
//            {
//                if (serialPort?.IsOpen == true)
//                    await serialPortService.ReadSerialPortDataAsync(client, tcpService, serialPort, token);
//            }
//            catch (IOException ex)
//            {
//                Console.WriteLine($"Serial→TCP bağlantı koptu: {ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Serial→TCP hata: {ex.Message}");
//            }
//        }

//        private void CloseClient()
//        {
//            try
//            {
//                if (tcpClient != null)
//                {
//                    try { tcpClient.GetStream()?.Close(); } catch { }
//                    try { tcpClient.Close(); } catch { }
//                    try { tcpClient.Dispose(); } catch { }
//                }

//                if (serialPort != null && serialPort.IsOpen)
//                {
//                    try
//                    {
//                        serialPort.Close();
//                        Console.WriteLine("TCP bağlantısı koptu, seri port kapatıldı.");
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine($"Seri port kapatma hatası: {ex.Message}");
//                    }
//                }
//            }
//            catch { }
//            finally
//            {
//                tcpClient = null;
//            }
//        }

//        private void TryOpenSerialPort()
//        {
//            try
//            {
//                if (serialPort == null) return;
//                if (!serialPort.IsOpen)
//                {
//                    serialPortService.OpenSerialPort(serialPort);
//                    Console.WriteLine($"Serial port {serialPort.PortName} açıldı.");
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Serial port açma hatası: {ex.Message}");
//            }
//        }
//    }
//}
