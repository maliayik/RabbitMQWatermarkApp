
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQWeb.Watermark.Services;
using System.Drawing;
using System.Text;
using System.Text.Json;

namespace RabbitMQWeb.Watermark.BackgroundServices
{
    public class ImageWatermarkProcessBackgroundService : BackgroundService
    {
        private readonly RabbitMQClientService _rabbitMQClientService;
        private readonly ILogger<ImageWatermarkProcessBackgroundService> _logger;
        private IModel _channel;

        public ImageWatermarkProcessBackgroundService(RabbitMQClientService rabbitMQClientService, ILogger<ImageWatermarkProcessBackgroundService> logger)
        {
            _rabbitMQClientService = rabbitMQClientService;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _channel= _rabbitMQClientService.Connect();
            _channel.BasicQos(0,1,false); // rabbit mq dan birer birer işlemleri alacak

            return base.StartAsync(cancellationToken);
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            _channel.BasicConsume(RabbitMQClientService.QueueName, false, consumer);

            consumer.Received += Consumer_Received;

            return Task.CompletedTask;

        }


        /// <summary>
        /// Resme yazı ekleme olayını gerçekleştirdiğimiz metot
        /// </summary>   
        private  async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            //5 saniye bekleme işlemi gerçekleştiriyoruz.
             Task.Delay(5000).Wait();
            try
            {
                //kayıtlı resmin adını alıyoruz.
                var productImageCreatedEvent = JsonSerializer.Deserialize<ProductImageCreatedEvent>(Encoding.UTF8.GetString(@event.Body.ToArray()));

                //resmin yolunu alıyoruz.
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images",productImageCreatedEvent.ImageName);

                var siteName = "www.mysite.com";

                //resmi alıyoruz.
                using var img = Image.FromFile(path);

                //resme yazı eklemek için grafik nesnesi oluşturuyoruz.
                using var graphic = Graphics.FromImage(img);

                var font = new Font(FontFamily.GenericMonospace, 40, FontStyle.Bold, GraphicsUnit.Pixel);

                var textSize = graphic.MeasureString(siteName, font);

                var color = Color.FromArgb(128, 255, 255, 255);

                var brush = new SolidBrush(color);

                //resmin sağ alt köşesine denk gelicek şekilde yazı ekliyoruz.
                var position = new Point(img.Width - ((int)textSize.Width + 30), img.Height - ((int)textSize.Height + 30));

                //çizim işlemini gerçekleştiriyoruz.
                graphic.DrawString(siteName, font, brush, position);

                img.Save("wwwroot/images/watermarks/" + productImageCreatedEvent.ImageName);

                //img ve graphic nesnelerini dispose ediyoruz.
                img.Dispose();
                graphic.Dispose();

                _channel.BasicAck(@event.DeliveryTag, false);
            }
            catch (Exception ex)
            {
               _logger.LogError(ex, ex.Message);
            }           

            return Task.CompletedTask;
        }


        //rabbit mq içerisinde dispose ettiğimiz için burada dispose etmeye gerek yok
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
}
