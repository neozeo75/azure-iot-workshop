
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

            string eventHubConnectionString = credentials.WeatherEventHubConnectionString.ToString();
            string eventHubName = credentials.WeatherEventHubConnectionString.EventHubName;
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
        public DateTime time { get; set; }
        public double temperature { get; set; }
        public double humidity { get; set; }
        public double probability { get; set; }

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
            Console.WriteLine("Weather Data EventProcessor initialized.  Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset);
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
                SensorData content = JsonConvert.DeserializeObject<SensorData>(data);
                await Post("https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/3aa7aa98-3432-49d0-bebf-65d6fb1f3709/rows?key=jWIWF51Ca5VrRUhqKJpYf5VGV1pNEQn%2FIRlKIEV6aZGmbSzSHpHqdZW2nO9W9xKeXuevQs9oGaOyB%2Fn7V4kGNw%3D%3D", content.ToString());
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
                        else
                        {
                            Console.WriteLine($"posted content: {body}");
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
