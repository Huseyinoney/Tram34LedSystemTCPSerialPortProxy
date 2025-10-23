using System.Net;
using System.Net.Sockets;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;

namespace Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.TcpServices
{
    public class TcpService : ITcpService
    {
        private TcpListener tcpListener;

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

        public byte[]? ProcessClientBuffer(byte[] tcpBuffer)
        {
            // 1️⃣ Frame uzunluğu minimum 7 olmalı
            if (tcpBuffer.Length < 7)
            {
                Console.WriteLine(" Frame çok kısa: " + BitConverter.ToString(tcpBuffer));
                return null;
            }

            // 2️⃣ ETX kontrolü
            if (tcpBuffer[tcpBuffer.Length - 2] != 0x03)
            {
                Console.WriteLine(" ETX hatalı: " + BitConverter.ToString(tcpBuffer));
                return null;
            }

            // 3️⃣ Checksum kontrolü
            byte receivedChecksum = tcpBuffer[tcpBuffer.Length - 1];
            int sum = 0;
            for (int i = 0; i < tcpBuffer.Length - 1; i++)
                sum += tcpBuffer[i];
            byte calc = (byte)(sum % 256);
            if (calc != receivedChecksum)
            {
                Console.WriteLine($" Checksum hatası: beklenen {calc:X2}, gelen {receivedChecksum:X2}");
                return null;
            }
            Console.WriteLine(tcpBuffer.Length);
            // 4️⃣ Seri porta gönder

            return tcpBuffer;
        }

        public async Task ReadTcpAsync(TcpClient tcpClient, ISerialPortService serialPortService, CancellationToken token)
        {
            try
            {
                NetworkStream tcpStream = tcpClient.GetStream();
                while (!token.IsCancellationRequested)
                {
                    // 1️⃣ İlk 5 byte (header) oku
                    byte[] header = new byte[5];
                    int readHeader = 0;
                    while (readHeader < 5)
                    {
                        int n = await tcpStream.ReadAsync(header, readHeader, 5 - readHeader);
                        if (n == 0)
                        {
                            tcpClient.Close();
                            return;
                        }  // bağlantı kapandı
                        readHeader += n;
                    }

                    // 2️⃣ Data uzunluğunu al (big-endian)
                    int dataLength = (header[3] << 8) | header[4];

                    // 3️⃣ Data + ETX(1) + checksum(1) oku
                    byte[] dataPlus = new byte[dataLength + 2];
                    int readData = 0;
                    while (readData < dataPlus.Length)
                    {
                        int n = await tcpStream.ReadAsync(dataPlus, readData, dataPlus.Length - readData);
                        if (n == 0) return; // bağlantı kapandı
                        readData += n;
                    }

                    // 4️⃣ Tam frame oluştur
                    byte[] frame = new byte[5 + dataPlus.Length];
                    Array.Copy(header, 0, frame, 0, 5);
                    Array.Copy(dataPlus, 0, frame, 5, dataPlus.Length);

                    // 5️⃣ Frame’i işle
                    var CheckedFrames = ProcessClientBuffer(frame);
                    if (CheckedFrames is not null)
                    {
                        await serialPortService.SendSerialPortData(CheckedFrames);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" TCP okuma hatası: " + ex.Message);
            }
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