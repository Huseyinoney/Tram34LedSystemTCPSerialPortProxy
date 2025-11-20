//using System.IO.Ports;
//using System.Net.Sockets;
//using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService;
//using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;

//namespace Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.SerialPortServices
//{
//    public class SerialPortService : ISerialPortService
//    {
//        private SerialPort serialPort;
//        private TcpClient tcpClient;

//        public bool CloseSerialPort(SerialPort serialPort)
//        {
//            try
//            {
//                if (serialPort.IsOpen)
//                {

//                    serialPort.Close();
//                    if (!serialPort.IsOpen)
//                    {
//                        Console.WriteLine(" Soket Bağlantısı Kapatıldı.\n");
//                        return true;
//                    }
//                }
//                return false;
//            }

//            catch (Exception ex)
//            {
//                Console.WriteLine("Soket Bağlantısı Kapatılırken Bir Hata Oluştu... " + ex.Message.ToString());
//                return false;
//            }
//        }

//        public SerialPort CreateSerialPort(string portName, int baudRate)
//        {
//            try
//            {
//                serialPort = new SerialPort(portName, baudRate);
//                return serialPort;
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("SeriPort Oluşturulurken Bir Hata Oluştu" + ex.Message.ToString());
//            }

//        }
//        public bool OpenSerialPort(SerialPort serialPort)
//        {
//            try
//            {
//                if (!serialPort.IsOpen)
//                {
//                    serialPort.Open();
//                    if (serialPort.IsOpen)
//                    {
//                        Console.WriteLine("Soket Bağlantısı Açıldı");
//                        return true;
//                    }
//                }
//                return false;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Soket Bağlantısı Açılırken Bir Hata Oluştu... " + ex.Message.ToString());
//                return false;
//            }
//        }

//        public async Task<bool> SendSerialPortData(byte[] frame)
//        {
//            try
//            {
//                serialPort.DiscardInBuffer();
//               // serialPort.DiscardOutBuffer();

//                await serialPort.BaseStream.WriteAsync(frame, 0, frame.Length);
//                Console.WriteLine($" TCP to {serialPort.PortName} ({frame.Length} byte): {BitConverter.ToString(frame)}");
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Serial write hatası: " + ex.Message);
//                return false;
//            }
//        }



//        public async Task<bool> ReadSerialPortDataAsync(TcpClient tcpClient, ITcpService tcpService, SerialPort serialPort, CancellationToken cancellationToken)
//        {

//            if (serialPort == null || !serialPort.IsOpen)
//            {
//                Console.WriteLine(" Seri port açık değil veya null!");
//                return false;
//            }

//            try
//            {

//                Console.WriteLine(" Seri port veri bekleniyor...");

//                List<byte> bufferList = new List<byte>();
//                this.tcpClient = tcpClient;

//                // Event tekrar bağlanmasın diye önce temizle
//                serialPort.DataReceived -= (s, e) => { };
//                serialPort.DataReceived +=  (s, e) =>  SerialPort_DataReceived(s, e, tcpService, bufferList, cancellationToken);

//                return true;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(" Seri port okuma hatası: " + ex.Message);
//                return false;
//            }
//        }

//        private  async Task  SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e, ITcpService tcpService, List<byte> bufferList, CancellationToken cancellationToken)
//        {
//            try
//            {
//                Console.WriteLine("ledden veri geldi");
//                var sp = (SerialPort)sender;
//                int bytesToRead = sp.BytesToRead;
//                if (bytesToRead <= 0) return;



//                byte[] temp = new byte[bytesToRead];
//                sp.Read(temp, 0, bytesToRead);
//                foreach (byte b in temp)
//                {
//                    Console.WriteLine($"{b}");
//                }
//                bufferList.AddRange(temp);

//                // Frame kontrolü
//                int start = bufferList.IndexOf(0x02); // STX
//                int etx = bufferList.IndexOf(0x03);   // ETX

//                if (start >= 0 && etx > start && bufferList.Count > etx + 1)
//                {
//                    int frameLength = etx - start + 2; // ETX + 1 byte checksum
//                    byte[] frame = bufferList.Skip(start).Take(frameLength).ToArray();

//                    // Buffer'dan çıkart
//                    bufferList.RemoveRange(0, start + frameLength);

//                    Console.WriteLine($" Frame bulundu ({frameLength} byte): {BitConverter.ToString(frame)}");

//                    if (tcpClient?.Connected == true)
//                    {
//                        await tcpService.SendTcpDataAsync(tcpClient, frame, cancellationToken);
//                        Console.WriteLine(" Frame TCP'ye gönderildi.");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("DataReceived hatası: " + ex.Message);
//            }
//        }
//    }
//}




//Test

using System.IO.Ports;
using System.Net.Sockets;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;

namespace Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.SerialPortServices
{
    public class SerialPortService : ISerialPortService
    {
        private SerialPort serialPort;
        private TcpClient tcpClient;
        private readonly SemaphoreSlim _serialLock = new(1, 1);
        private SerialDataReceivedEventHandler serialPortDataReceivedWrapper;


        public async Task<bool> CloseSerialPort(SerialPort serialPort)
        {
            await _serialLock.WaitAsync();
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    if (!serialPort.IsOpen)
                    {
                        Console.WriteLine(" Soket Bağlantısı Kapatıldı.\n");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Soket Bağlantısı Kapatılırken Bir Hata Oluştu... " + ex.Message);
                return false;
            }
            finally
            {
                _serialLock.Release();
            }
        }

        public SerialPort CreateSerialPort(string portName, int baudRate)
        {
            try
            {
                serialPort = new SerialPort(portName, baudRate);
                serialPort.ReceivedBytesThreshold = 1;
                return serialPort;
            }
            catch (Exception ex)
            {
                throw new Exception("SeriPort Oluşturulurken Bir Hata Oluştu: " + ex.Message);
            }
        }

        public async Task<bool> OpenSerialPort(SerialPort serialPort)
        {
            await _serialLock.WaitAsync();
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                    if (serialPort.IsOpen)
                    {
                        Console.WriteLine("Soket Bağlantısı Açıldı");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Soket Bağlantısı Açılırken Bir Hata Oluştu... " + ex.Message);
                return false;
            }
            finally
            {
                _serialLock.Release();
            }
        }

        public async Task<bool> SendSerialPortData(byte[] frame)
        {
            await _serialLock.WaitAsync();
            try
            {
                if (!serialPort.IsOpen) await OpenSerialPort(serialPort);
                //  serialPort.DiscardInBuffer();
                await serialPort.BaseStream.WriteAsync(frame, 0, frame.Length);

                /* bunu sonradan ekledim çalışan versiyonda yok*/
                 await serialPort.BaseStream.FlushAsync();

               // serialPort.Write(frame, 0, frame.Length);
                Console.WriteLine($" TCP to {serialPort.PortName} ({frame.Length} byte): {BitConverter.ToString(frame)}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Serial write hatası: " + ex.Message);
                return false;
            }
            finally
            {
                _serialLock.Release();
            }
        }

        //    public async Task<bool> ReadSerialPortDataAsync(
        //TcpClient tcpClient,
        //ITcpService tcpService,
        //SerialPort serialPort,
        //CancellationToken cancellationToken)
        //    {
        //        if (serialPort == null || !serialPort.IsOpen)
        //        {
        //            Console.WriteLine(" Seri port açık değil veya null!");
        //            return false;
        //        }

        //        Console.WriteLine(" Seri port veri bekleniyor...");
        //        List<byte> bufferList = new List<byte>();
        //        this.tcpClient = tcpClient;

        //        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        //        // 🔹 tcs burada, sadece frame tamamlanınca set edilecek
        //        void SerialPortDataReceivedWrapper(object sender, SerialDataReceivedEventArgs e)
        //        {
        //            try
        //            {
        //                bool isFrameCompleated = HandleDataReceived(sender, e, tcpService, bufferList, cancellationToken);
        //                if (isFrameCompleated)
        //                    tcs.TrySetResult(true); // ✅ frame bitince tetikle
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"DataReceivedWrapper hata: {ex.Message}");
        //            }
        //        }

        //        serialPort.DataReceived -= SerialPortDataReceivedWrapper;
        //        serialPort.DataReceived += SerialPortDataReceivedWrapper;

        //        using var reg = cancellationToken.Register(() =>
        //        {
        //            Console.WriteLine("⏰ 5 saniye doldu, token iptal edildi.");
        //            tcs.TrySetResult(true);
        //        });

        //        try
        //        {
        //            await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(500)); // güvenlik payı
        //        }
        //        catch (TimeoutException)
        //        {
        //            Console.WriteLine("⏰ WaitAsync timeout — tcs tetiklenmedi, manuel sonlandırılıyor.");
        //        }
        //        finally
        //        {
        //            try { serialPort.DataReceived -= SerialPortDataReceivedWrapper; } catch { }

        //            if (serialPort.IsOpen)
        //            {
        //                try
        //                {
        //                    serialPort.Close();
        //                    Console.WriteLine(" 🔌 Seri port kapatıldı (frame tamamlandı veya süre doldu).");
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine($" Seri port kapatma hatası: {ex.Message}");
        //                }
        //            }
        //        }

        //        return true;
        //    }




        //// ÇALIŞAN Versiyon 5sn Olmadan
        //public async Task<bool> ReadSerialPortDataAsync(TcpClient tcpClient, ITcpService tcpService, SerialPort serialPort, CancellationToken cancellationToken)
        //{
        //    if (serialPort == null || !serialPort.IsOpen)
        //    {
        //        Console.WriteLine(" Seri port açık değil veya null!");
        //        return false;
        //    }

        //    try
        //    {
        //        Console.WriteLine(" Seri port veri bekleniyor...");
        //        List<byte> bufferList = new List<byte>();
        //        this.tcpClient = tcpClient;

        //        // Önce event'i temizle, sonra güvenli şekilde bağla
        //        serialPort.DataReceived -= SerialPortDataReceivedWrapper;
        //        serialPort.DataReceived += SerialPortDataReceivedWrapper;

        //        void SerialPortDataReceivedWrapper(object sender, SerialDataReceivedEventArgs e)
        //        {
        //            HandleDataReceived(sender, e, tcpService, bufferList, cancellationToken);
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(" Seri port okuma hatası: " + ex.Message);
        //        return false;
        //    }
        //}

        //çalışıyor güncel

        //    public async Task<bool> ReadSerialPortDataAsync(
        //TcpClient tcpClient,
        //ITcpService tcpService,
        //SerialPort serialPort,
        //CancellationToken cancellationToken)
        //    {
        //        if (serialPort == null || !serialPort.IsOpen)
        //        {
        //            Console.WriteLine("Seri port açık değil veya null!");
        //            return false;
        //        }

        //        try
        //        {
        //            Console.WriteLine("Seri port veri bekleniyor...");
        //            List<byte> bufferList = new List<byte>();
        //            this.tcpClient = tcpClient;

        //            // Daha önce bağlandıysa kaldır
        //            serialPort.DataReceived -= SerialPortDataReceivedWrapper;
        //            serialPort.DataReceived += SerialPortDataReceivedWrapper;

        //            async Task SerialPortDataReceivedWrapper(object sender, SerialDataReceivedEventArgs e)
        //            {
        //                try
        //                {
        //                   await  HandleDataReceived(sender, e, tcpService, bufferList, cancellationToken);
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine($"DataReceived wrapper hatası: {ex.Message}");
        //                }
        //            }

        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("Seri port okuma hatası: " + ex.Message);
        //            return false;
        //        }
        //    }

        //    public async Task<bool> ReadSerialPortDataAsync(
        //TcpClient tcpClient,
        //ITcpService tcpService,
        //SerialPort serialPort,
        //CancellationToken cancellationToken)
        //    {
        //        if (serialPort == null || !serialPort.IsOpen)
        //        {
        //            Console.WriteLine("Seri port açık değil veya null!");
        //            return false;
        //        }

        //        try
        //        {
        //            Console.WriteLine("Seri port veri bekleniyor...");
        //            List<byte> bufferList = new List<byte>();
        //            this.tcpClient = tcpClient;

        //            // Daha önce bağlandıysa kaldır
        //            serialPort.DataReceived -= SerialPortDataReceivedWrapper;
        //            serialPort.DataReceived += SerialPortDataReceivedWrapper;

        //            void SerialPortDataReceivedWrapper(object sender, SerialDataReceivedEventArgs e)
        //            {
        //                // Task.Run ile arka planda çalıştır
        //                _ = Task.Run(async () =>
        //                {
        //                    try
        //                    {
        //                        await HandleDataReceived(sender, e, tcpService, bufferList, cancellationToken);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Console.WriteLine($"DataReceived Task hatası: {ex.Message}");
        //                    }
        //                });
        //            }

        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("Seri port okuma hatası: " + ex.Message);
        //            return false;
        //        }
        //    }


        // Class seviyesinde ekle


        public async Task<bool> ReadSerialPortDataAsync(
            TcpClient tcpClient,
            ITcpService tcpService,
            SerialPort serialPort,
            CancellationToken cancellationToken)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                Console.WriteLine("Seri port açık değil veya null!");
                return false;
            }

            try
            {
                Console.WriteLine("Seri port veri bekleniyor...");
                List<byte> bufferList = new List<byte>();
                this.tcpClient = tcpClient;

                // Eğer daha önce bağlanmamışsa event handler oluştur
                if (serialPortDataReceivedWrapper == null)
                {
                    serialPortDataReceivedWrapper = (sender, e) =>
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await HandleDataReceived(sender, e, tcpService, bufferList, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"DataReceived Task hatası: {ex.Message}");
                            }
                        });
                    };
                }

                // Event'i önce kaldır, sonra ekle (safe)
                serialPort.DataReceived -= serialPortDataReceivedWrapper;
                serialPort.DataReceived += serialPortDataReceivedWrapper;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Seri port okuma hatası: " + ex.Message);
                return false;
            }
        }




        //Çalışan Handle
        //private async void HandleDataReceived(object sender, SerialDataReceivedEventArgs e, ITcpService tcpService, List<byte> bufferList, CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        var sp = (SerialPort)sender;
        //        int bytesToRead = sp.BytesToRead;
        //        if (bytesToRead <= 0) return;

        //        byte[] temp = new byte[bytesToRead];
        //        sp.Read(temp, 0, bytesToRead);
        //        bufferList.AddRange(temp);

        //        // Frame kontrolü
        //        int start = bufferList.IndexOf(0x02); // STX
        //        int etx = bufferList.IndexOf(0x03);   // ETX

        //        if (start >= 0 && etx > start && bufferList.Count > etx + 1)
        //        {
        //            int frameLength = etx - start + 2; // ETX + 1 byte checksum
        //            byte[] frame = bufferList.Skip(start).Take(frameLength).ToArray();

        //            // Buffer'dan çıkar
        //            bufferList.RemoveRange(0, start + frameLength);

        //            Console.WriteLine($" Frame bulundu ({frameLength} byte): {BitConverter.ToString(frame)}");

        //            if (tcpClient?.Connected == true)
        //            {
        //                await tcpService.SendTcpDataAsync(tcpClient, frame, cancellationToken);
        //                Console.WriteLine(" Frame TCP'ye gönderildi.");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("DataReceived hatası: " + ex.Message);
        //    }
        //}


        public async Task ResetSerialPortProxy(SerialPort serialPort)
        {
            await _serialLock.WaitAsync();
            try
            {
                if (serialPort != null)
                {
                    // DataReceived eventlerini kaldır
                    serialPort.DataReceived -= serialPortDataReceivedWrapper;

                    if (serialPort.IsOpen)
                    {
                        try
                        {
                            // Buffer'ları port kapanmadan temizle
                            serialPort.DiscardInBuffer();
                            serialPort.DiscardOutBuffer();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Buffer temizleme hatası (önemsiz): {ex.Message}");
                        }

                        // Portu kapat
                        serialPort.Close();
                        Console.WriteLine("🔌 Seri port resetlendi ve kapatıldı.");
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ResetSerialPortProxy hatası: {ex.Message}");
            }
            finally
            {
                _serialLock.Release();
            }
        }


        private bool _isReading = false;

        private async Task HandleDataReceived(
            object sender,
            SerialDataReceivedEventArgs e,
            ITcpService tcpService,
            List<byte> bufferList,
            CancellationToken cancellationToken)
        {
            if (_isReading)
                return; // önceki okuma bitmeden girme

            _isReading = true;
            try
            {
                var sp = (SerialPort)sender;
                int bytesToRead = sp.BytesToRead;
                if (bytesToRead <= 0)
                    return;

                byte[] temp = new byte[bytesToRead];
                sp.Read(temp, 0, bytesToRead);
                bufferList.AddRange(temp);

                const int HeaderLength = 5;
                const int FooterLength = 2;
                const int AckFrameLength = 8;   // LED ACK cevabı
                const int MinimumFrameLength = 23; // Gerçek veri frame'i

                int start = bufferList.IndexOf(0x02);
                if (start < 0)
                {
                    bufferList.Clear();
                    return;
                }

                if (bufferList.Count < start + HeaderLength)
                    return;

                int dataLength = (bufferList[start + 3] << 8) | bufferList[start + 4];
                int frameLength = HeaderLength + dataLength + FooterLength;

                // frame henüz tam gelmemişse bekle
                if (bufferList.Count < start + frameLength)
                    return;

                // 0x03 bitiş kontrolü
                if (bufferList[start + frameLength - 2] != 0x03)
                {
                    Console.WriteLine("Geçersiz frame (ETX hatalı), buffer temizleniyor.");
                    bufferList.Clear();
                    return;
                }

                // Frame çıkar
                byte[] frame = bufferList.Skip(start).Take(frameLength).ToArray();
                bufferList.RemoveRange(0, start + frameLength);

                foreach (var item in frame)
                {
                    Console.Write(item);
                }
                // 8 byte'lık ACK frame'leri sadece logla, TCP'ye gönderme
                if (frameLength == AckFrameLength)
                {
                    Console.WriteLine($"ACK frame alındı ({frameLength} byte): {BitConverter.ToString(frame)}");
                }
                else if (frameLength >= MinimumFrameLength)
                {
                    Console.WriteLine($"Veri frame bulundu ({frameLength} byte): {BitConverter.ToString(frame)}");

                    if (tcpClient?.Connected == true)
                    {
                        await tcpService.SendTcpDataAsync(tcpClient, frame, cancellationToken);
                        Console.WriteLine("Frame TCP'ye gönderildi.");
                    }
                }
                else
                {
                    Console.WriteLine($"Geçersiz frame uzunluğu ({frameLength}), atlanıyor.");
                }

                if (bufferList.Count > 2000)
                {
                    Console.WriteLine("Buffer temizlendi (overflow koruması).");
                    bufferList.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HandleDataReceived hatası: {ex.Message}");
            }
            finally
            {
                _isReading = false;
            }
        }
    }
}