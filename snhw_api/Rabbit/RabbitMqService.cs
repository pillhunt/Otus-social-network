
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Channels;

namespace snhw_api.Rabbit
{
    public class RabbitMqService : IRabbitMqService
    {
        private IConnection connection;
        private IModel channel;
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

            var factory = new ConnectionFactory()
            {
                HostName = host,
                Password = "rabbitmq",
                UserName = "rabbitmq",
            };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange, ExchangeType.Topic);
        }

        public void SendMessage(object obj)
        {
            var message = System.Text.Json.JsonSerializer.Serialize(obj);
            SendMessage(message);
        }

        public void SendMessage(string message)
        {
            dynamic messageObject = JsonConvert.DeserializeObject(message);
            string consumerId = messageObject.consumerId;

            var bodyObject = new
            {
                messageObject.time,
                messageObject.messageHead,
                messageObject.status
            };

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bodyObject));
            string routingKey = consumerId + "." + queue;

            channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: null,
                body: body);
            Console.WriteLine(exchange + ":" + routingKey + ":" + body);
        }
    }
}

