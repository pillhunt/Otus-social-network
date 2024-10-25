
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace snhw.Rabbit
{
    public class RabbitMqService : IRabbitMqService
    {
        private string host { get; set; } = string.Empty;
        private string queue { get; set; } = string.Empty;
        private string exchange { get; set; } = string.Empty;

        public RabbitMqService() 
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            IConfigurationSection apiSettings = configuration.GetSection("RabbitMQ");
            host = apiSettings.GetValue(typeof(string), "host").ToString();
            queue = apiSettings.GetValue(typeof(string), "queue").ToString();
            exchange = apiSettings.GetValue(typeof(string), "exchange").ToString();
        }

        public void SendMessage(object obj)
        {
            var message = JsonSerializer.Serialize(obj);
            SendMessage(message);
        }

        public void SendMessage(string message)
        {
            var factory = new ConnectionFactory() 
            { 
                HostName = host ,
                Password = "rabbitmq",
                UserName = "rabbitmq",
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queue,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: exchange,
                    routingKey: queue,
                    basicProperties: null,
                    body: body);
            }
        }
    }
}

