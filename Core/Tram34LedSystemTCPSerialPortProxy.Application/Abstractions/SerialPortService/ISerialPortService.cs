using System.IO.Ports;
using System.Net.Sockets;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;

namespace Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService
{
    public interface ISerialPortService
    {
        public SerialPort CreateSerialPort(string portName, int baudRate);

        public Task<bool> OpenSerialPort();

        public Task<bool> CloseSerialPort();

        public Task<bool> SendSerialPortData(byte[] frame);

        Task<bool> ReadSerialPortDataAsync(TcpClient tcpClient, ITcpService tcpService, CancellationToken cancellationToken);

        public Task ResetSerialPortProxy();

        public Task ResetDummySerialPort(string portName = "COM12", int baudRate = 19200);
    }
}
