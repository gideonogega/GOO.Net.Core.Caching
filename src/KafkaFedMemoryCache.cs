using Microsoft.Extensions.Caching.Memory;
using System;

namespace GOO.Net.Core.Caching
{
    public class KafkaFedMemoryCache
    {
        private readonly IMemoryCache memoryCache;
        private readonly KafkaClient kafkaClient;

        public KafkaFedMemoryCache(KafkaClient kafkaClient, IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
            this.kafkaClient = kafkaClient;

            kafkaClient.OnMessageReceived += NotificationHandler;
        }

        void NotificationHandler(object notice)
        {
            Console.WriteLine($"KafkaFedMemoryCache: {notice}");
            //Console
        }
    }
}
