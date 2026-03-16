

//using System.Diagnostics;
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

//        private readonly SemaphoreSlim _serialLock = new(1, 1);
//        private readonly SemaphoreSlim _readSemaphore = new(1, 1);
//        private readonly SemaphoreSlim _sendLock = new(1, 1);

//        private readonly List<byte> _bufferList = new();

//        private TaskCompletionSource<byte[]> _responseTcs;

//        // ===========================
//        // CREATE
//        // ===========================
//        public SerialPort CreateSerialPort(string portName, int baudRate)
//        {
//            serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
//            {
//                ReceivedBytesThreshold = 1
//            };
//            return serialPort;
//        }

//        // ===========================
//        // OPEN
//        // ===========================
//        public async Task<bool> OpenSerialPort()
//        {
//            await _serialLock.WaitAsync();
//            try
//            {
//                if (!serialPort.IsOpen)
//                {
//                    serialPort.Open();
//                    try
//                    {
//                        serialPort.RtsEnable = true;
//                        serialPort.DtrEnable = false;
//                    }
//                    catch { }

//                    serialPort.DataReceived -= OnDataReceived;
//                    serialPort.DataReceived += OnDataReceived;
//                }

//                return serialPort.IsOpen;
//            }
//            finally
//            {
//                _serialLock.Release();
//            }
//        }

//        // ===========================
//        // RESET
//        // ===========================
//        public async Task ResetSerialPortProxy()
//        {
//            await _serialLock.WaitAsync();
//            try
//            {
//                if (serialPort == null)
//                    return;

//                serialPort.DataReceived -= OnDataReceived;

//                if (serialPort.IsOpen)
//                {
//                    serialPort.DiscardInBuffer();
//                    serialPort.DiscardOutBuffer();
//                    serialPort.Close();
//                }
//            }
//            finally
//            {
//                _serialLock.Release();
//            }
//        }

//        // ===========================
//        // TCP BIND
//        // ===========================
//        public async Task<bool> ReadSerialPortDataAsync(
//            TcpClient client,
//            ITcpService tcpService,
//            CancellationToken token)
//        {
//            if (serialPort == null || !serialPort.IsOpen)
//                return false;

//            tcpClient = client;
//            return true;
//        }

//        // ===========================
//        // RS485 SEND (CLEAR + DATA)
//        // ===========================
//        public async Task<bool> SendSerialPortData(byte[] dataFrame)
//        {
//            await _sendLock.WaitAsync();
//            try
//            {
//                if (!serialPort.IsOpen)
//                    await OpenSerialPort();

//                // 1️⃣ DATA FRAME gönder ve yanıt bekle
//                var response = await SendFrameAndWaitResponse(dataFrame, 1000);

//                if (response == null)
//                {
//                    Console.WriteLine(" Yanıt alınamadı, frame atlandı.");
//                    return false;
//                }

//                return true;
//            }
//            finally
//            {
//                _sendLock.Release();
//            }
//        }

//        private async Task<byte[]> SendFrameAndWaitResponse(byte[] frame, int timeoutMs)
//        {
//            // _responseTcs = new TaskCompletionSource<byte[]>();
//            _responseTcs = new TaskCompletionSource<byte[]>(
//     TaskCreationOptions.RunContinuationsAsynchronously);

//            // TX işlemi
//            await SendRawFrame(frame);

//            // Timeout
//            var delayTask = Task.Delay(timeoutMs);
//            var completed = await Task.WhenAny(_responseTcs.Task, delayTask);

//            if (completed == delayTask)
//            {
//                _responseTcs.TrySetResult(null); // timeout
//                return null;
//            }

//            return _responseTcs.Task.Result;
//        }

//        //private async Task SendRawFrame(byte[] frame)
//        //{
//        //    // RX → TX
//        //    serialPort.RtsEnable = true;
//        //    serialPort.DtrEnable = false;
//        //    await Task.Delay(2);

//        //    serialPort.RtsEnable = false;
//        //    serialPort.DtrEnable = true;
//        //    await Task.Delay(5);

//        //    serialPort.Write(frame, 0, frame.Length);
//        //    Console.WriteLine($"TX → {BitConverter.ToString(frame)}");

//        //    var sw = Stopwatch.StartNew();
//        //    while (serialPort.BytesToWrite > 0 && sw.ElapsedMilliseconds < 3000)
//        //        await Task.Delay(1);

//        //    int wait = CalculateTurnaroundDelay(frame.Length);
//        //    await Task.Delay(wait);

//        //    // BACK TO RX
//        //    serialPort.RtsEnable = true;
//        //    serialPort.DtrEnable = false;
//        //}

//        //private int CalculateTurnaroundDelay(int frameLength)
//        //{
//        //    double bitTimeMs = 1000.0 / serialPort.BaudRate;
//        //    double txTime = frameLength * 10 * bitTimeMs;
//        //    return Math.Clamp((int)(txTime + 20), 25, 150);
//        //}



//        private async Task SendRawFrame(byte[] frame, double cableLengthMeters = 0, int dataBits = 8, int stopBits = 1)
//        {
//            // 1️⃣ RX → TX
//            serialPort.RtsEnable = true;
//            serialPort.DtrEnable = false;
//            await Task.Delay(2);

//            serialPort.RtsEnable = false;
//            serialPort.DtrEnable = true;
//            await Task.Delay(5);

//            // 2️⃣ Frame gönder
//            serialPort.Write(frame, 0, frame.Length);
//            Console.WriteLine($"TX → {BitConverter.ToString(frame)}");

//            // 3️⃣ TX tamponu boşalana kadar bekle (frame süresine göre)
//            int bitsPerByte = 1 + dataBits + stopBits;
//            int txTimeMs = (int)Math.Ceiling(bitsPerByte * frame.Length * 1000.0 / serialPort.BaudRate);

//            // Margin ekle, 5 ms güvenlik payı
//            int maxWaitMs = txTimeMs + 5;

//            var sw = Stopwatch.StartNew();
//            while (serialPort.BytesToWrite > 0 && sw.ElapsedMilliseconds < maxWaitMs)
//                await Task.Delay(1);

//            // 4️⃣ Turnaround delay (uzun kablo dikkate alındı)
//            int turnaround = CalculateTurnaroundDelay(frame.Length, cableLengthMeters);
//            await Task.Delay(turnaround);

//            // 5️⃣ RX’e geri dön
//            serialPort.RtsEnable = true;
//            serialPort.DtrEnable = false;
//        }

//        // Turnaround delay hesaplama, kablo gecikmesini de ekliyoruz
//        private int CalculateTurnaroundDelay(int frameLength, double cableLengthMeters = 0)
//        {
//            double bitTimeMs = 1000.0 / serialPort.BaudRate;
//            double txTime = frameLength * 10 * bitTimeMs;

//            // Kablo gecikmesini ekle (bakır hat ~2e8 m/s)
//            double propagationMs = (cableLengthMeters / 2e8) * 1000.0;

//            // TX + 20 ms + propagation, 25–150 ms aralığında clamp
//            return Math.Clamp((int)(txTime + 20 + propagationMs), 25, 150);
//        }


//        private byte[] BuildClearFrame(byte[] source)
//        {
//            var clear = (byte[])source.Clone();

//            clear[2] = 0x35;
//            for (int i = 5; i < clear.Length - 2; i++)
//                clear[i] = 0x00;

//            int sum = 0;
//            for (int i = 0; i < clear.Length - 1; i++)
//                sum += clear[i];

//            clear[^1] = (byte)(sum & 0xFF);
//            return clear;
//        }

//        // ===========================
//        // SERIAL RECEIVE
//        // ===========================
//        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
//        {
//            _ = Task.Run(async () =>
//            {
//                await _readSemaphore.WaitAsync();
//                try
//                {
//                    await HandleDataReceived(sender);
//                }
//                finally
//                {
//                    _readSemaphore.Release();
//                }
//            });
//        }

//        private async Task HandleDataReceived(object sender)
//        {
//            if (tcpClient == null || !tcpClient.Connected)
//                return;

//            var sp = (SerialPort)sender;
//            int count = sp.BytesToRead;
//            if (count <= 0) return;

//            byte[] temp = new byte[count];
//            sp.Read(temp, 0, count);
//            _bufferList.AddRange(temp);

//            const int MaxBufferSize = 4096;

//            if (_bufferList.Count > MaxBufferSize)
//            {
//                Console.WriteLine("Buffer limit aşıldı. Eski veri kırpılıyor.");
//                _bufferList.RemoveRange(0, _bufferList.Count - MaxBufferSize);
//            }


//            const int HeaderLength = 5;
//            const int FooterLength = 2;
//            const int AckFrameLength = 8;

//            while (_bufferList.Count >= HeaderLength)
//            {
//                int start = _bufferList.IndexOf(0x02);
//                if (start < 0)
//                {
//                    _bufferList.Clear();
//                    break;
//                }

//                if (start > 0)
//                    _bufferList.RemoveRange(0, start);

//                if (_bufferList.Count < HeaderLength)
//                    break;

//                int len = (_bufferList[3] << 8) | _bufferList[4];
//                int frameLen = HeaderLength + len + FooterLength;

//                if (_bufferList.Count < frameLen)
//                    break;

//                if (_bufferList[frameLen - 2] != 0x03)
//                {
//                    _bufferList.RemoveAt(0);
//                    continue;
//                }

//                byte[] frame = _bufferList.Take(frameLen).ToArray();
//                _bufferList.RemoveRange(0, frameLen);

//                // ACK veya normal frame log
//                if (frame.Length == AckFrameLength)
//                    Console.WriteLine($"ACK ← {BitConverter.ToString(frame)}");
//                else
//                    Console.WriteLine($"RX ← {BitConverter.ToString(frame)}");

//                // TCP'ye gönder
//                //if (tcpClient?.Connected == true)
//                //    await tcpClient.GetStream().WriteAsync(frame, 0, frame.Length);

//                if (tcpClient?.Connected == true)
//                {
//                    try
//                    {
//                        await tcpClient.GetStream()
//                            .WriteAsync(frame, 0, frame.Length);
//                    }
//                    catch
//                    {
//                        // client düşmüş olabilir
//                    }
//                }


//                //// Yanıt bekleyen TCS varsa tamamla
//                //if (_responseTcs != null && !_responseTcs.Task.IsCompleted)
//                //    _responseTcs.SetResult(frame);

//                _responseTcs?.TrySetResult(frame);

//            }
//        }
//    }
//}



using System.Diagnostics;
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
        private readonly SemaphoreSlim _readSemaphore = new(1, 1);
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        private readonly List<byte> _bufferList = new();

        private TaskCompletionSource<byte[]> _responseTcs;
        private byte _waitingCommand;

        // ===========================
        // CREATE
        // ===========================
        public SerialPort CreateSerialPort(string portName, int baudRate)
        {
            serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReceivedBytesThreshold = 1
            };
            return serialPort;
        }

        // ===========================
        // OPEN
        // ===========================
        public async Task<bool> OpenSerialPort()
        {
            await _serialLock.WaitAsync();
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                    try
                    {
                        serialPort.RtsEnable = true;  // RX mode
                        serialPort.DtrEnable = false;
                    }
                    catch { }

                    serialPort.DataReceived -= OnDataReceived;
                    serialPort.DataReceived += OnDataReceived;
                }

                return serialPort.IsOpen;
            }
            finally
            {
                _serialLock.Release();
            }
        }

        // ===========================
        // RESET
        // ===========================
        public async Task ResetSerialPortProxy()
        {
            await _serialLock.WaitAsync();
            try
            {
                if (serialPort == null)
                    return;

                serialPort.DataReceived -= OnDataReceived;

                if (serialPort.IsOpen)
                {
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();
                    serialPort.Close();
                }
            }
            finally
            {
                _serialLock.Release();
            }
        }

        // ===========================
        // TCP BIND
        // ===========================
        public async Task<bool> ReadSerialPortDataAsync(
            TcpClient client,
            ITcpService tcpService,
            CancellationToken token)
        {
            if (serialPort == null || !serialPort.IsOpen)
                return false;

            tcpClient = client;
            return true;
        }

        // ===========================
        // RS485 SEND (DATA FRAME + WAIT RESPONSE)
        // ===========================
        public async Task<bool> SendSerialPortData(byte[] dataFrame)
        {
            await _sendLock.WaitAsync();
            try
            {
                if (!serialPort.IsOpen)
                    await OpenSerialPort();

                // 1️⃣ DATA FRAME gönder ve yanıt bekle
                var response = await SendFrameAndWaitResponse(dataFrame, 1000);

                if (response == null)
                {
                    Console.WriteLine("❌ Yanıt alınamadı, frame atlandı.");
                    return false;
                }

                return true;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task<byte[]> SendFrameAndWaitResponse(byte[] frame, int timeoutMs)
        {
            _waitingCommand = frame[2]; // Gönderilen komutu sakla

            _responseTcs = new TaskCompletionSource<byte[]>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            await SendRawFrame(frame);

            var delayTask = Task.Delay(timeoutMs);
            var completed = await Task.WhenAny(_responseTcs.Task, delayTask);

            if (completed == delayTask)
            {
                return null; // timeout
            }

            return await _responseTcs.Task;
        }

        private async Task SendRawFrame(byte[] frame, double cableLengthMeters = 0, int dataBits = 8, int stopBits = 1)
        {
            // 1️⃣ RX → TX
            serialPort.RtsEnable = true;
            serialPort.DtrEnable = false;
            await Task.Delay(2);

            // 2️⃣ TX mode
            serialPort.RtsEnable = false;
            serialPort.DtrEnable = true;
            await Task.Delay(5);

            // 3️⃣ Frame gönder
            serialPort.Write(frame, 0, frame.Length);
            Console.WriteLine($"TX → {BitConverter.ToString(frame)}");

            // 4️⃣ TX tamponu boşalana kadar bekle
            int bitsPerByte = 1 + dataBits + stopBits;
            int txTimeMs = (int)Math.Ceiling(bitsPerByte * frame.Length * 1000.0 / serialPort.BaudRate);
            int maxWaitMs = txTimeMs + 5;

            var sw = Stopwatch.StartNew();
            while (serialPort.BytesToWrite > 0 && sw.ElapsedMilliseconds < maxWaitMs)
                await Task.Delay(1);

            // 5️⃣ Turnaround delay
            int turnaround = CalculateTurnaroundDelay(frame.Length, cableLengthMeters);
            await Task.Delay(turnaround);

            // 6️⃣ RX’e geri dön
            serialPort.RtsEnable = true;
            serialPort.DtrEnable = false;
        }

        private int CalculateTurnaroundDelay(int frameLength, double cableLengthMeters = 0)
        {
            double bitTimeMs = 1000.0 / serialPort.BaudRate;
            double txTime = frameLength * 10 * bitTimeMs;

            double propagationMs = (cableLengthMeters / 2e8) * 1000.0;

            return Math.Clamp((int)(txTime + 20 + propagationMs), 25, 150);
        }

        private byte[] BuildClearFrame(byte[] source)
        {
            var clear = (byte[])source.Clone();

            clear[2] = 0x35;
            for (int i = 5; i < clear.Length - 2; i++)
                clear[i] = 0x00;

            int sum = 0;
            for (int i = 0; i < clear.Length - 1; i++)
                sum += clear[i];

            clear[^1] = (byte)(sum & 0xFF);
            return clear;
        }

        // ===========================
        // SERIAL RECEIVE
        // ===========================
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await _readSemaphore.WaitAsync();
                try
                {
                    await HandleDataReceived(sender);
                }
                finally
                {
                    _readSemaphore.Release();
                }
            });
        }

        private async Task HandleDataReceived(object sender)
        {
            if (tcpClient == null || !tcpClient.Connected)
                return;

            var sp = (SerialPort)sender;
            int count = sp.BytesToRead;
            if (count <= 0) return;

            byte[] temp = new byte[count];
            sp.Read(temp, 0, count);
            _bufferList.AddRange(temp);

            const int MaxBufferSize = 4096;
            if (_bufferList.Count > MaxBufferSize)
            {
                Console.WriteLine("Buffer limit aşıldı. Eski veri kırpılıyor.");
                _bufferList.RemoveRange(0, _bufferList.Count - MaxBufferSize);
            }

            const int HeaderLength = 5;
            const int FooterLength = 2;
            const int AckFrameLength = 8;

            while (_bufferList.Count >= HeaderLength)
            {
                int start = _bufferList.IndexOf(0x02);
                if (start < 0)
                {
                    _bufferList.Clear();
                    break;
                }

                if (start > 0)
                    _bufferList.RemoveRange(0, start);

                if (_bufferList.Count < HeaderLength)
                    break;

                int len = (_bufferList[3] << 8) | _bufferList[4];
                int frameLen = HeaderLength + len + FooterLength;

                if (_bufferList.Count < frameLen)
                    break;

                if (_bufferList[frameLen - 2] != 0x03)
                {
                    _bufferList.RemoveAt(0);
                    continue;
                }

                byte[] frame = _bufferList.Take(frameLen).ToArray();
                _bufferList.RemoveRange(0, frameLen);

                // ACK veya normal frame log
                if (frame.Length == AckFrameLength)
                    Console.WriteLine($"ACK ← {BitConverter.ToString(frame)}");
                else
                    Console.WriteLine($"RX ← {BitConverter.ToString(frame)}");

                // TCP'ye gönder
                if (tcpClient?.Connected == true)
                {
                    try
                    {
                        await tcpClient.GetStream().WriteAsync(frame, 0, frame.Length);
                    }
                    catch { }
                }

                // ️Komut bazlı yanıt
                if (_responseTcs != null && !_responseTcs.Task.IsCompleted)
                {
                    byte command = frame[2];
                    if (command == _waitingCommand)
                    {
                        _responseTcs.TrySetResult(frame);
                    }
                }
            }
        }
    }
}