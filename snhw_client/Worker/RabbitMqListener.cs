using System.Diagnostics;
using System.Text;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace snhw_client.Worker
{
    public class RabbitMqListener : BackgroundService
    {
        private IConnection connection;
        private IModel channel;
        private string testUserId { get; set; } = "009c9721-174c-4932-880b-f16da4dff351";
        private string apiConnectionString { get; set; } = string.Empty;
        private List<string> usersContacts {  get; set; } = new List<string>();
        private string host { get; set; } = string.Empty;
        private string queue { get; set; } = string.Empty;
        private string exchange { get; set; } = string.Empty;
        private string routingKey { get; set; } = string.Empty;
        private ConnectionFactory rabbitFactory { get; set; }
        private Guid consumerId { get; set; }

        public RabbitMqListener(Guid consumerId)
        {
            this.consumerId = consumerId;

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            IConfigurationSection apiSettings = configuration.GetSection("RabbitMQ");
            host = apiSettings.GetValue(typeof(string), "host").ToString();
            queue = consumerId + "." + apiSettings.GetValue(typeof(string), "queue").ToString();
            exchange = apiSettings.GetValue(typeof(string), "exchange").ToString();
            routingKey = consumerId + ".*";

            apiConnectionString = configuration.GetConnectionString("db_master") ??
                throw new Exception("Не удалось получить настройку подключения к БД");

            rabbitFactory = new ConnectionFactory()
            {
                HostName = host,
                Password = "rabbitmq",
                UserName = "rabbitmq",
            };

            connection = rabbitFactory.CreateConnection();            
            channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange, ExchangeType.Topic);
            channel.QueueDeclare(queue: queue, 
                durable: true, 
                exclusive: false, 
                autoDelete: false, 
                arguments: null);
            channel.QueueBind(queue, exchange, routingKey);            
            // channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false); 

            Console.WriteLine("Rabbit waiting for messages.");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (ch, ea) =>
            {
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());
                var routingKey = ea.RoutingKey;
                Console.WriteLine(routingKey + ":" + content);
                channel.BasicAck(ea.DeliveryTag, false);
            };
            channel.BasicConsume(queue, false, consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            channel.Close();
            connection.Close();
            base.Dispose();
        }
    }
}
