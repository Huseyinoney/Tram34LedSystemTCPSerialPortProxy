using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;

namespace Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.TcpServices
{
    public class TcpService : ITcpService
    {
        private TcpListener tcpListener;
        private readonly IConfiguration configuration;

        public TcpService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public TcpListener CreateTcpServer(IPAddress ıPAddress, int port)
        {
            try
            {
                tcpListener = new TcpListener(ıPAddress, port);
                return tcpListener;
            }
            catch (Exception ex)
            {
                throw new Exception("Tcp Server Oluşturulurken Bir Hata Oluştu " + ex.Message.ToString());
            }
        }

        //Valen V6.0 Frame Doğrulama

        public byte[]? ProcessClientBuffer(byte[] tcpBuffer)
        {
            if (tcpBuffer.Length < 7)
                return null;

            // ETX kontrolü
            if (tcpBuffer[^2] != 0x03)
            {
                Console.WriteLine("ETX yok veya hatalı.");
                return null;
            }

            byte receivedChecksum = tcpBuffer[^1];
            int sum = 0;
            for (int i = 0; i < tcpBuffer.Length - 1; i++)
                sum += tcpBuffer[i];

            if ((sum % 256) != receivedChecksum)
            {
                Console.WriteLine("Checksum hatası");
                return null;
            }

            return tcpBuffer;
        }

        //Valen V6.0

        public async Task ReadTcpAsync(TcpClient tcpClient, ISerialPortService serialPortService, CancellationToken token)
        {
            try
            {
                NetworkStream tcpStream = tcpClient.GetStream();

                while (!token.IsCancellationRequested)
                {
                    byte[] header = new byte[5];

                    // 1️⃣ İlk byte mutlaka 0x02 olacak
                    int firstByte = -1;
                    do
                    {
                        firstByte = await ReadOneByteAsync(tcpStream, token);

                        if (firstByte == -1)
                        {
                            tcpClient.Close();
                            return;
                        }

                    } while (firstByte != 0x02);

                    header[0] = (byte)firstByte;

                    // 2️⃣ Header'ın kalan 4 byte'ı
                    int readHeader = 1;
                    while (readHeader < 5)
                    {
                        int n = await tcpStream.ReadAsync(header, readHeader, 5 - readHeader, token);

                        if (n == 0)
                        {
                            tcpClient.Close();
                            return;
                        }

                        readHeader += n;
                    }

                    // 3️⃣ Data uzunluğu
                    int dataLength = (header[3] << 8) | header[4];

                    // 4️⃣ Data + ETX + checksum (ETX=1 byte + checksum=1 byte)
                    byte[] dataPlus = new byte[dataLength + 2];

                    int readData = 0;
                    while (readData < dataPlus.Length)
                    {
                        int n = await tcpStream.ReadAsync(dataPlus, readData, dataPlus.Length - readData, token);

                        if (n == 0)
                        {
                            tcpClient.Close();
                            return;
                        }

                        readData += n;
                    }

                    // 5️⃣ Full frame oluştur
                    byte[] frame = new byte[5 + dataPlus.Length];
                    Array.Copy(header, 0, frame, 0, 5);
                    Array.Copy(dataPlus, 0, frame, 5, dataPlus.Length);

                    // 6️⃣ ETX doğrulaması
                    byte etx = frame[frame.Length - 2];
                    if (etx != 0x03)
                    {
                        Console.WriteLine("TCP'den gelen ETX hatalı → frame drop");
                        continue;
                    }

                    // 7️⃣ Checksum doğrulaması
                    byte receivedChecksum = frame[^1];
                    int sum = 0;
                    for (int i = 0; i < frame.Length - 1; i++)
                        sum += frame[i];

                    byte calcChecksum = (byte)(sum % 256);

                    if (calcChecksum != receivedChecksum)
                    {
                        Console.WriteLine($"TCP checksum HATALI → beklenen {calcChecksum:X2}, gelen {receivedChecksum:X2}");
                        continue;
                    }

                    // 8️⃣ Frame’i seri porta gönder
                    await serialPortService.SendSerialPortData(frame);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TCP okuma hatası: " + ex.Message);
            }
        }

        private async Task<int> ReadOneByteAsync(NetworkStream stream, CancellationToken token)
        {
            byte[] buffer = new byte[1];
            int n = await stream.ReadAsync(buffer, 0, 1, token);
            if (n == 0) return -1;
            return buffer[0];
        }


        public bool StartTcpServer(TcpListener tcpListener)
        {
            try
            {
                tcpListener.Start();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return false;

            }
        }

        public bool StopTcpServer(TcpListener tcpListener)
        {
            try
            {
                tcpListener.Stop();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> SendTcpDataAsync(TcpClient client, byte[] data, CancellationToken token)
        {
            if (client == null || !client.Connected)

            {
                return false;
            }

            try
            {
                NetworkStream stream = client.GetStream();
                await stream.WriteAsync(data, 0, data.Length, token);
                Console.WriteLine($" COM → TCP ({data.Length} byte): {BitConverter.ToString(data)}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("TCP yazma hatası: " + ex.Message);
                return false;
            }
        }
    }
}