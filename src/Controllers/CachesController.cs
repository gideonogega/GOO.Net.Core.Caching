using AutoFixture;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace GOO.Net.Core.Caching.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CachesController : ControllerBase
    {
        private const int TEST_COUNT = 10;

        private static readonly Fixture Fixture = new Fixture();
        private static long IdCounter = 0;

        private readonly IMemoryCache memoryCache;
        private readonly IDistributedCache distributedCache;
        private readonly KafkaClient kafkaClient;

        public CachesController(IMemoryCache memoryCache, IDistributedCache distributedCache, KafkaClient kafkaClient)
        {
            this.memoryCache = memoryCache;
            this.distributedCache = distributedCache;
            this.kafkaClient = kafkaClient;
        }

        [HttpGet("distributed")]
        public List<WeatherForecast> KafkaFedMemoryCache()
        {
            var output = new List<WeatherForecast>();
            foreach (var forecast in Fixture.CreateMany<WeatherForecast>(TEST_COUNT))
            {
                forecast.Id = Interlocked.Increment(ref IdCounter);
                memoryCache.Set(forecast.Id, forecast);
                output.Add((WeatherForecast)memoryCache.Get(forecast.Id));
            }
            return output;
        }

        [HttpGet("memory")]
        public List<WeatherForecast> MemoryCache()
        {
            var output = new List<WeatherForecast>();
            foreach (var forecast in Fixture.CreateMany<WeatherForecast>(TEST_COUNT))
            {
                forecast.Id = Interlocked.Increment(ref IdCounter);
                memoryCache.Set(forecast.Id, forecast);
                output.Add((WeatherForecast)memoryCache.Get(forecast.Id));
            }
            return output;
        }

        [HttpGet("kafkafedmemory")]
        public List<WeatherForecast> DistributedCache()
        {
            var output = new List<WeatherForecast>();
            foreach (var forecast in Fixture.CreateMany<WeatherForecast>(TEST_COUNT))
            {
                forecast.Id = Interlocked.Increment(ref IdCounter);

                kafkaClient.Publish(forecast.Id);

                /*distributedCache.Set(forecast.Id.ToString(), ToBytes(forecast));
                output.Add(FromBytes<WeatherForecast>(distributedCache.Get(forecast.Id.ToString())));*/
            }
            return output;
        }

        public static byte[] ToBytes(object obj)
        {
            var formatter = new BinaryFormatter();
            using (var memeoryStream = new MemoryStream())
            {
                formatter.Serialize(memeoryStream, obj);
                return memeoryStream.ToArray();
            }
        }

        public static T FromBytes<T>(byte[] arrBytes)
        {
            using (var memoryStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                memoryStream.Write(arrBytes, 0, arrBytes.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);
                var obj = formatter.Deserialize(memoryStream);
                return (T)obj;
            }
        }
    }
}
