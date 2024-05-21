using RabbitMQ.Client;

namespace RabbitMQWeb.Watermark.Services
{
    //Dispose metodunu implemet ederek  nesnelerimizi bellekten temizliyoruz.
    public class RabbitMQClientService:IDisposable
    {
        //rabbitmq ile ilgili bağlantılarımızı tanımlıyoruz.
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        public static string ExchangeName = "ImageDirectExchange";
        public static string QueueName = "queue-watermark-image";
        public static string RoutingKeyName = "watermark-route-image";


        private readonly ILogger<RabbitMQClientService> _logger;

        public RabbitMQClientService(ConnectionFactory connectionFactory,ILogger<RabbitMQClientService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;         
        }

        public IModel Connect()
        {
            _connection=_connectionFactory.CreateConnection();

            if(_channel is { IsOpen:true})
            {
                return _channel;
            }

            _channel=_connection.CreateModel();

            _channel.ExchangeDeclare(ExchangeName,type:"direct",true,false);

            _channel.QueueDeclare(QueueName,true,false,false,null);

            _channel.QueueBind(exchange:ExchangeName,queue:QueueName,routingKey:RoutingKeyName);

            _logger.LogInformation("RabbitMQ ile bağlantı kuruldu.");

            return _channel;
        }

        public void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
         
            _connection?.Close();
            _connection?.Dispose();

            _logger.LogInformation("RabbitMQ ile bağlantı kapatıldı.");
        }
    }
}
