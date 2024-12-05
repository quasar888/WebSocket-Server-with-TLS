using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocket_Server_with_TLS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Kestrel to use HTTPS with a certificate
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenLocalhost(5001, listenOptions =>
                {
                    listenOptions.UseHttps("C:\\Certificatepfx12\\localhost_certificate.pfx", "Blazor18)");
                });
            });

            var app = builder.Build();

            app.UseWebSockets(); // Enable WebSockets middleware

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await HandleWebSocketAsync(webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });

            app.Run();
        }

        static async Task HandleWebSocketAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result;

            try
            {
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Log the close request and break the loop
                        Console.WriteLine("Close message received.");
                        break;
                    }

                    Console.WriteLine($"Received: {receivedMessage}");

                    // Echo the message back
                    var responseMessage = Encoding.UTF8.GetBytes($"Echo: {receivedMessage}");
                    await webSocket.SendAsync(new ArraySegment<byte>(responseMessage), WebSocketMessageType.Text, true, CancellationToken.None);

                } while (!result.CloseStatus.HasValue);

                // Check the WebSocket state before attempting to close it
                if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
                }
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"WebSocketException: {ex.Message}");
            }
            finally
            {
                // Ensure the WebSocket is disposed of properly
                if (webSocket.State != WebSocketState.Closed)
                {
                    webSocket.Abort(); // Abort the WebSocket if it's not fully closed
                }
                webSocket.Dispose();
                Console.WriteLine("WebSocket connection cleaned up.");
            }
        }


    }
}
