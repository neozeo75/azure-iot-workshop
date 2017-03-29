
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace CustomEventProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            WorkshopCredentials credentials;
            try
            {
                using (StreamReader sr = new StreamReader(@"..\..\..\azure-iot-workshop-credentials.xld"))
                {
                   credentials = JsonConvert.DeserializeObject<WorkshopCredentials>(Regex.Replace(sr.ReadToEnd(), @"\r\n\t+", ""));
                }
            }
            catch
            {
                Console.WriteLine("An error occurred while reading in credential file.");
                Console.ReadKey();
                return;
            }
            
            string eventHubConnectionString = credentials.EventHubConnectionString.ToString();
            string eventHubName = credentials.EventHubConnectionString.EventHubName;
            string storageAccountName = credentials.StorageAccountConnectionString.AccountName;
            string storageAccountKey = credentials.StorageAccountConnectionString.AccountKey;
            string storageConnectionString = string.Format($"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={storageAccountKey}");
      
            string eventProcessorHostName = Guid.NewGuid().ToString();

            EventProcessorHost eventProcessorHost = new EventProcessorHost(eventProcessorHostName, eventHubName, EventHubConsumerGroup.DefaultGroupName, eventHubConnectionString, storageConnectionString);
            Console.WriteLine("Registering EventProcessor...");
            var options = new EventProcessorOptions();
            options.ExceptionReceived += (sender, e) => { Console.WriteLine(e.Exception); };
            try
            {
                eventProcessorHost.RegisterEventProcessorAsync<CustomEventProcessor>(options).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.InnerException.Message);
            }
            Console.WriteLine("Receiving. Press enter key to stop worker.");
            Console.ReadLine();
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();
        }
    }

    class SensorData
    {
        public string deviceId { get; set; }
        public DateTime date { get; set; }
        public double temperature { get; set; }
        public double humidity { get; set; }
        public double pressure { get; set; }
        public double windspeed { get; set; }
        public string longitude { get; set; }
        public string latitude { get; set; }

        public override string ToString()
        {
            var serialized = JsonConvert.SerializeObject(this);
            Debug.WriteLine($"serialized: {serialized}");
            return serialized;
        }
    }

    class CustomEventProcessor : IEventProcessor
    {
        Stopwatch checkpointStopWatch;

        async Task IEventProcessor.CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine("Processor Shutting Down. Partition '{0}', Reason: '{1}'.", context.Lease.PartitionId, reason);
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        Task IEventProcessor.OpenAsync(PartitionContext context)
        {
            Console.WriteLine("SimpleEventProcessor initialized.  Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset);
            checkpointStopWatch = new Stopwatch();
            checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
         }

        async Task IEventProcessor.ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (EventData eventData in messages)
            {
                var data = Encoding.UTF8.GetString(eventData.GetBytes());
                Console.WriteLine(string.Format("Message received.  Partition: '{0}', Data: '{1}'", context.Lease.PartitionId, data));
                var content = JsonConvert.DeserializeObject<SensorData>(data);
                await Post("https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/7b794932-3ae5-4715-9e65-833f10161e9b/rows?key=ZggTh8tf9JrpX9AAWOEmXsttvQhjXT5QTsgHONqauWowvtSh6%2BNu2V6vHivMXVohaX8VsIZjMzA48keCcI0jvA%3D%3D", content.ToString());
            }

            if (checkpointStopWatch.Elapsed > TimeSpan.FromSeconds(30))
            {
                await context.CheckpointAsync();
                checkpointStopWatch.Restart();
            }
        }

        async Task Post(string URL, string body)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, URL);
                    request.Content = new StringContent(body);

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
           
                        }
                        using (HttpContent content = response.Content)
                        {
                        //    _response.JSON = await content.ReadAsStringAsync().ConfigureAwait(false);
                         //   return _response;
                        }
                    }
                }
            }
            catch (Exception)
            {
              //  _response.ex = ex;
              //  return _response;
            }
        }
    }
}
