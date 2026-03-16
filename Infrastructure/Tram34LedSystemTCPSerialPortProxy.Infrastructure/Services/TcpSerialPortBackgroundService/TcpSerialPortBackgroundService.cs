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

        private string portName;
        private int baudRate;

        public TcpSerialPortBackgroundService(ITcpService tcpService, ISerialPortService serialPortService, IConfiguration configuration)
        {
            this.tcpService = tcpService;
            this.serialPortService = serialPortService;
            this.configuration = configuration;

            portName = configuration["SerialPort:PortName"] ?? "COM12";
            baudRate = int.TryParse(configuration["SerialPort:BaudRate"], out var br) ? br : 19200;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string ipString = configuration["TcpServer:Ip"];
            int tcpPort = int.TryParse(configuration["TcpServer:Port"], out var tp) ? tp : 7000;

            IPAddress ipAddress = IPAddress.TryParse(ipString, out var ip) ? ip : IPAddress.Any;

            try
            {
                tcpServer = tcpService.CreateTcpServer(ipAddress, tcpPort);
                tcpService.StartTcpServer(tcpServer);
                Console.WriteLine($"TCP Sunucu dinliyor: {ipAddress}:{tcpPort}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TCP server başlatma hatası: {ex.Message}");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("Yeni TCP client bekleniyor...");
                    tcpClient = await tcpServer.AcceptTcpClientAsync(stoppingToken);
                    Console.WriteLine($"TCP client bağlandı: {tcpClient.Client.RemoteEndPoint}");

                    await OpenSerialPortForClient(); // Client bağlanınca serial port aç
                    await RunClientAsync(tcpClient, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ana döngü hatası: {ex.Message}");
                }
                finally
                {
                    CloseClient();                   // TCP client kapat
                    await CloseSerialPortForClient(); // Serial port kapat
                }
            }
        }

        private async Task OpenSerialPortForClient()
        {
            if (serialPort != null)
            {
                await serialPortService.ResetSerialPortProxy();
                serialPort = null;
            }

            serialPort = serialPortService.CreateSerialPort(portName, baudRate);
            await serialPortService.OpenSerialPort();
            Console.WriteLine("SerialPort client için açıldı.");
        }

        private async Task CloseSerialPortForClient()
        {
            if (serialPort != null)
            {
                try
                {
                    if (serialPort.IsOpen)
                        serialPort.Close();
                }
                catch { }

                serialPort.Dispose();
                serialPort = null;

                Console.WriteLine("SerialPort client kapandı, port kapatıldı.");
            }
        }

        private async Task RunClientAsync(TcpClient client, CancellationToken token)
        {
            var tcpToSerial = SafeTcpToSerialAsync(client, token);
            var serialToTcp = SafeSerialToTcpAsync(client, token);
            await Task.WhenAll(tcpToSerial, serialToTcp);
        }

        private async Task SafeTcpToSerialAsync(TcpClient client, CancellationToken token)
        {
            try
            {
                await tcpService.ReadTcpAsync(client, serialPortService, token);
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
                await serialPortService.ReadSerialPortDataAsync(client, tcpService, token);
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
                tcpClient?.GetStream()?.Close();
                tcpClient?.Close();
            }
            catch { }

            tcpClient = null;
            Console.WriteLine("TCP client kapatıldı.");
        }
    }
}
