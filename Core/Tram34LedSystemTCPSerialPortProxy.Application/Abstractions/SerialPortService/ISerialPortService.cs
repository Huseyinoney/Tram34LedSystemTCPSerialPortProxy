using System.IO.Ports;
using System.Net.Sockets;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;

namespace Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService
{
    public interface ISerialPortService
    {
        public SerialPort CreateSerialPort(string portName, int baudRate);

        public Task<bool> OpenSerialPort(SerialPort serialPort);

        public Task<bool> CloseSerialPort(SerialPort serialPort);

        public Task<bool> SendSerialPortData(byte[] frame);

        Task<bool> ReadSerialPortDataAsync(TcpClient tcpClient, ITcpService tcpService,SerialPort serialPort, CancellationToken cancellationToken);

        public Task ResetSerialPortProxy(SerialPort serialPort);
    }
}
