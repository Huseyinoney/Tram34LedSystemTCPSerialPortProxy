using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService;

namespace Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp
{
    public interface ITcpService
    {
        public TcpListener CreateTcpServer(IPAddress ıPAddress, int port);

        public bool StartTcpServer(TcpListener tcpListener);

        public bool StopTcpServer(TcpListener tcpListener);

        public Task ReadTcpAsync(TcpClient tcpClient, ISerialPortService serialPortService, CancellationToken token);

        //   Task<byte[]?> ReadTcpFrameAsync(TcpClient tcpClient, CancellationToken token);
        Task<bool> SendTcpDataAsync(TcpClient client, byte[] data, CancellationToken token);

        public byte[]? ProcessClientBuffer(byte[] tcpBuffer);
    }
}
