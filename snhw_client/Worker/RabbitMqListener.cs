using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Diagnostics;

namespace snhw_client.Worker
{
    public class RabbitMqListener : BackgroundService
    {
        private IConnection _connection;
        private IModel _channel;

        private string host { get; set; } = string.Empty;
        private string queue { get; set; } = string.Empty;
        private string exchange { get; set; } = string.Empty;
        private ConnectionFactory factory { get; set; }

        public RabbitMqListener()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            IConfigurationSection apiSettings = configuration.GetSection("RabbitMQ");
            host = apiSettings.GetValue(typeof(string), "host").ToString();
            queue = apiSettings.GetValue(typeof(string), "queue").ToString();
            exchange = apiSettings.GetValue(typeof(string), "exchange").ToString();

            factory = new ConnectionFactory()
            {
                HostName = host,
                Password = "rabbitmq",
                UserName = "rabbitmq",
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: queue, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());
                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume(queue, false, consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }
}
