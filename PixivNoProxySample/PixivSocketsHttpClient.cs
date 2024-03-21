using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;

namespace PixivNoProxySample;

/// <summary>
/// 创建用于绕过SNI阻断的 <see cref="HttpClient"/>，只能连接Pixiv的服务器。
/// </summary>
public abstract class PixivSocketsHttpClient
{


    /// <summary>
    /// 创建用于绕过SNI阻断的 <see cref="HttpClient"/>，只能连接Pixiv的服务器。
    /// </summary>
    public static HttpClient Create(string host)
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = false,
            ConnectCallback = async (info, token) =>
            {
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(host, 443);
                var stream = new NetworkStream(socket, true);
                var sslstream = new SslStream(stream, false, (_, _, _, _) => true);
                await sslstream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = "",
                    ApplicationProtocols = new List<SslApplicationProtocol>([SslApplicationProtocol.Http2])
                }, token);
                return sslstream;
            },
        };
        var client = new HttpClient(handler)
        {
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
            DefaultRequestVersion = HttpVersion.Version20,
        };
        return client;
    }

}
