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


        // çalışan v 4.0
        //public byte[]? ProcessClientBuffer(byte[] tcpBuffer)
        //{
        //    // 1️⃣ Frame uzunluğu minimum 7 olmalı
        //    if (tcpBuffer.Length < 7)
        //    {
        //        Console.WriteLine(" Frame çok kısa: " + BitConverter.ToString(tcpBuffer));
        //        return null;
        //    }

        //    // 2️⃣ ETX kontrolü
        //    if (tcpBuffer[tcpBuffer.Length - 2] != 0x03)
        //    {
        //        Console.WriteLine(" ETX hatalı: " + BitConverter.ToString(tcpBuffer));
        //        return null;
        //    }

        //    // 3️⃣ Checksum kontrolü
        //    byte receivedChecksum = tcpBuffer[tcpBuffer.Length - 1];
        //    int sum = 0;
        //    for (int i = 0; i < tcpBuffer.Length - 1; i++)
        //        sum += tcpBuffer[i];
        //    byte calc = (byte)(sum % 256);
        //    if (calc != receivedChecksum)
        //    {
        //        Console.WriteLine($" Checksum hatası: beklenen {calc:X2}, gelen {receivedChecksum:X2}");
        //        return null;
        //    }
        //    Console.WriteLine(tcpBuffer.Length);
        //    // 4️⃣ Seri porta gönder

        //    return tcpBuffer;
        //}

        //Çalışan ReadTcpAsync v4.0 
        //public async Task ReadTcpAsync(TcpClient tcpClient, ISerialPortService serialPortService, CancellationToken token)
        //{
        //    try
        //    {
        //        NetworkStream tcpStream = tcpClient.GetStream();
        //        while (!token.IsCancellationRequested)
        //        {
        //            // 1️⃣ İlk 5 byte (header) oku
        //            byte[] header = new byte[5];
        //            int readHeader = 0;
        //            while (readHeader < 5)
        //            {
        //                int n = await tcpStream.ReadAsync(header, readHeader, 5 - readHeader);
        //                if (n == 0)
        //                {
        //                    tcpClient.Close();
        //                    return;
        //                }  // bağlantı kapandı
        //                readHeader += n;
        //            }

        //            // 2️⃣ Data uzunluğunu al (big-endian)
        //            int dataLength = (header[3] << 8) | header[4];

        //            // 3️⃣ Data + ETX(1) + checksum(1) oku
        //            byte[] dataPlus = new byte[dataLength + 2];
        //            int readData = 0;
        //            while (readData < dataPlus.Length)
        //            {
        //                int n = await tcpStream.ReadAsync(dataPlus, readData, dataPlus.Length - readData);
        //                if (n == 0) return; // bağlantı kapandı
        //                readData += n;
        //            }

        //            // 4️⃣ Tam frame oluştur
        //            byte[] frame = new byte[5 + dataPlus.Length];
        //            Array.Copy(header, 0, frame, 0, 5);
        //            Array.Copy(dataPlus, 0, frame, 5, dataPlus.Length);

        //            // 5️⃣ Frame’i işle
        //            var CheckedFrames = ProcessClientBuffer(frame);
        //            if (CheckedFrames is not null)
        //            {
        //                await serialPortService.SendSerialPortData(CheckedFrames);
        //            }
        //            await Task.Delay(Convert.ToInt32(configuration["SerialPort:DelayMs"]));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(" TCP okuma hatası: " + ex.Message);
        //    }
        //}

        //public async Task<byte[]?> ReadTcpFrameAsync(TcpClient tcpClient, CancellationToken token)
        //{
        //    try
        //    {
        //        NetworkStream tcpStream = tcpClient.GetStream();

        //        // 1️⃣ Header (5 byte)
        //        byte[] header = new byte[5];
        //        int readHeader = 0;
        //        while (readHeader < 5)
        //        {
        //            int n = await tcpStream.ReadAsync(header, readHeader, 5 - readHeader, token);
        //            if (n == 0) return null; // bağlantı kapandı
        //            readHeader += n;
        //        }

        //        // 2️⃣ Data uzunluğu
        //        int dataLength = (header[3] << 8) | header[4];

        //        // 3️⃣ Data + ETX + checksum
        //        byte[] dataPlus = new byte[dataLength + 2];
        //        int readData = 0;
        //        while (readData < dataPlus.Length)
        //        {
        //            int n = await tcpStream.ReadAsync(dataPlus, readData, dataPlus.Length - readData, token);
        //            if (n == 0) return null;
        //            readData += n;
        //        }

        //        // 4️⃣ Frame birleştir
        //        byte[] frame = new byte[5 + dataPlus.Length];
        //        Array.Copy(header, 0, frame, 0, 5);
        //        Array.Copy(dataPlus, 0, frame, 5, dataPlus.Length);

        //        return frame;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"TCP frame okuma hatası: {ex.Message}");
        //        return null;
        //    }
        //}




        // V5.0 TCP frame doğrulama
        public byte[]? ProcessClientBuffer(byte[] tcpBuffer)
        {
            // 1️⃣ Minimum frame uzunluğu: header(5) + checksum(1) = 6
            if (tcpBuffer.Length < 6)
            {
                Console.WriteLine("Frame çok kısa: " + BitConverter.ToString(tcpBuffer));
                return null;
            }

            // 2️⃣ Checksum kontrolü (son byte)
            byte receivedChecksum = tcpBuffer[^1];
            int sum = 0;
            for (int i = 0; i < tcpBuffer.Length - 1; i++)
                sum += tcpBuffer[i];
            byte calc = (byte)(sum % 256);
            if (calc != receivedChecksum)
            {
                Console.WriteLine($"Checksum hatası: beklenen {calc:X2}, gelen {receivedChecksum:X2}");
                return null;
            }

            // 3️⃣ Frame geçerli, return et
            return tcpBuffer;
        }

        // V5.0 TCP okuma ve serial porta gönderme
        //public async Task ReadTcpAsync(TcpClient tcpClient, ISerialPortService serialPortService, CancellationToken token)
        //{
        //    try
        //    {
        //        NetworkStream tcpStream = tcpClient.GetStream();

        //        while (!token.IsCancellationRequested)
        //        {
        //            // 1️⃣ İlk 5 byte: header (0x02 + address + command + length(2))
        //            byte[] header = new byte[5];
        //            int readHeader = 0;
        //            while (readHeader < 5)
        //            {
        //                int n = await tcpStream.ReadAsync(header, readHeader, 5 - readHeader, token);
        //                if (n == 0)
        //                {
        //                    tcpClient.Close();
        //                    return; // bağlantı kapandı
        //                }
        //                readHeader += n;
        //            }

        //            // 2️⃣ Data uzunluğunu al (big-endian)
        //            int dataLength = (header[3] << 8) | header[4];

        //            // 3️⃣ Data + checksum (1 byte)
        //            byte[] dataPlus = new byte[dataLength + 1];
        //            int readData = 0;
        //            while (readData < dataPlus.Length)
        //            {
        //                int n = await tcpStream.ReadAsync(dataPlus, readData, dataPlus.Length - readData, token);
        //                if (n == 0)
        //                {
        //                    tcpClient.Close();
        //                    return;
        //                }
        //                readData += n;
        //            }

        //            // 4️⃣ Tam frame oluştur
        //            byte[] frame = new byte[5 + dataPlus.Length];
        //            Array.Copy(header, 0, frame, 0, 5);
        //            Array.Copy(dataPlus, 0, frame, 5, dataPlus.Length);

        //            // 5️⃣ Frame’i işle
        //            var checkedFrame = ProcessClientBuffer(frame);
        //            if (checkedFrame is not null)
        //            {
        //                await serialPortService.SendSerialPortData(checkedFrame);
        //            }

        //            await Task.Delay(Convert.ToInt32(configuration["SerialPort:DelayMs"]), token);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("TCP okuma hatası: " + ex.Message);
        //    }
        //}

        //v5.0 Düzeltme

        public async Task ReadTcpAsync(TcpClient tcpClient, ISerialPortService serialPortService, CancellationToken token)
        {
            try
            {
                NetworkStream tcpStream = tcpClient.GetStream();

                while (!token.IsCancellationRequested)
                {
                    byte[] header = new byte[5];

                    // 1️⃣ İlk byte kesinlikle 0x02 olmalı
                    int firstByte = -1;
                    do
                    {
                        firstByte = await ReadOneByteAsync(tcpStream, token);

                        if (firstByte == -1)
                        {
                            tcpClient.Close();
                            return; // bağlantı kapandı
                        }

                    } while (firstByte != 0x02); // 0x02 yakalanana kadar çöpe at

                    header[0] = (byte)firstByte;

                    // 2️⃣ Kalan 4 byte
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

                    // 4️⃣ Data + checksum oku
                    byte[] dataPlus = new byte[dataLength + 1];
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

                    // 5️⃣ Tam frame oluştur
                    byte[] frame = new byte[5 + dataPlus.Length];
                    Array.Copy(header, 0, frame, 0, 5);
                    Array.Copy(dataPlus, 0, frame, 5, dataPlus.Length);

                    // 6️⃣ Frame’i işle
                    var checkedFrame = ProcessClientBuffer(frame);
                    if (checkedFrame is not null)
                    {
                        await serialPortService.SendSerialPortData(checkedFrame);
                    }

                    await Task.Delay(Convert.ToInt32(configuration["SerialPort:DelayMs"]), token);
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