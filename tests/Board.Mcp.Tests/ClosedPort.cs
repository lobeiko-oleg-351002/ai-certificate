using System.Net;
using System.Net.Sockets;

namespace Board.Mcp.Tests;

public static class ClosedPort
{
    public static string Url()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return $"http://127.0.0.1:{port}";
    }
}
