﻿
using System.Text;
using System.Net.WebSockets;
using Microsoft.Extensions.Hosting;

namespace snhw_client.Worker
{
    public class WebSocketListener : BackgroundService
    {
        private ClientWebSocket clientWebSocket {  get; set; } 

        public WebSocketListener() 
        {
            clientWebSocket = new ClientWebSocket();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json")
                        .Build();

                IConfigurationSection apiSettings = configuration.GetSection("WebSocket");
                string uri = apiSettings.GetValue(typeof(string), "uri").ToString();

                await clientWebSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
                Console.WriteLine("WebSocketListener > Подключение: успешно");
                byte[] buf = new byte[1056];

                while (!stoppingToken.IsCancellationRequested)
                {
                    while (clientWebSocket.State == WebSocketState.Open)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            break;

                        WebSocketReceiveResult result = await clientWebSocket.ReceiveAsync(buf, CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                            Console.WriteLine(result.CloseStatusDescription);
                        }
                        else
                        {
                            Console.WriteLine(Encoding.ASCII.GetString(buf, 0, result.Count));
                        }
                    }

                    Thread.Sleep(10000);

                    if (stoppingToken.IsCancellationRequested)
                        break;
                }

            }
            catch (Exception ex) 
            {
                Console.WriteLine($"WebSocketListener > Ошибка: {ex.Message}; внутреняя ошибка: {ex.InnerException?.Message}");
            }
        }
    }
}