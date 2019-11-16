using Confluent.Kafka;
using System;
using System.Threading;

namespace GOO.Net.Core.Caching
{

    public delegate void NotificationHandler(object notice);

    public class KafkaClient : IDisposable
    {
        private const string BOOTSTRAP_SERVERS = "localhost:9092";
        private const string KAFKA_TOPIC = "GOO.Net.Core.Caching.kafka.forecasts";

        private readonly ProducerConfig producerConfig = new ProducerConfig { BootstrapServers = BOOTSTRAP_SERVERS };
        private readonly IProducer<Null, string> producer;

        private readonly ConsumerConfig consumerConfig = new ConsumerConfig { 
            GroupId = KAFKA_TOPIC, 
            BootstrapServers = BOOTSTRAP_SERVERS 
        };
        private readonly IConsumer<Ignore, string> consumer;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private bool polling;

        public NotificationHandler OnMessageReceived { get; set; }

        public KafkaClient()
        {
            producer = new ProducerBuilder<Null, string>(producerConfig).Build();
            consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        }

        public void Start()
        {
            consumer.Subscribe(KAFKA_TOPIC);

            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true; // prevent the process from terminating.
                cancellationTokenSource.Cancel();
            };

            polling = true;
            Thread thread = new Thread(Poll);
            thread.Start();
        }

        public void Stop()
        {
            polling = false;
        }

        public void Poll()
        {
            try
            {
                while (polling)
                {
                    try
                    {
                        var cr = consumer.Consume(cancellationTokenSource.Token);
                        Console.WriteLine($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}'.");
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"Error occured: {e.Error.Reason}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ensure the consumer leaves the group cleanly and final offsets are committed.
                consumer.Close();
            }
        }

        public void Publish(object item)
        {
            Action<DeliveryReport<Null, string>> handler = r =>
                Console.WriteLine(!r.Error.IsError
                    ? $"Delivered message to {r.TopicPartitionOffset}"
                    : $"Delivery Error: {r.Error.Reason}");

            producer.Produce(KAFKA_TOPIC, new Message<Null, string> { Value = item.ToString() }, handler);
            // wait for up to 10 seconds for any inflight messages to be delivered.
            //producer.Flush(TimeSpan.FromSeconds(10));
        }

        public void Dispose()
        {
            producer.Flush();
            producer.Dispose();

            consumer.Close();
            consumer.Dispose();
        }
    }
}
