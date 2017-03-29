using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace ServiceBackend
{
    class Program
    {
        static void Main(string[] args)
        {
            var source = new CancellationTokenSource();
            var token = source.Token;

            var credentials = ServiceClientHelper.GetCredentials(@"..\..\..\azure-iot-workshop-credentials.xld");
            var connectionString = credentials.IotHubConnectionString.ToString();
            var serviceClientHelper = new ServiceClientHelper(connectionString);

            serviceClientHelper.OpenConnectionAsync().Wait();

            Task.Run(async () =>
            {
                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }
                else
                {
                    await serviceClientHelper.ReceiveFeedbackAsyc();
                }
            }, token);
            serviceClientHelper.UserPrompt().Wait();
            serviceClientHelper.CloseConnectionAsync().Wait();
        }
    }

    public class ServiceClientHelper
    {
        public ServiceClientHelper(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public async Task OpenConnectionAsync()
        {
            serviceClient = ServiceClient.CreateFromConnectionString(ConnectionString);
            registryManager = RegistryManager.CreateFromConnectionString(ConnectionString);

            await serviceClient.OpenAsync();
            await registryManager.OpenAsync();
        }

        public async Task CloseConnectionAsync()
        {
            await registryManager.CloseAsync();
            await serviceClient.CloseAsync();
        }

        public async Task UserPrompt()
        {
            var choice = 0;
            do
            {
                Console.WriteLine("1. Send Device Command");
                Console.WriteLine("2. Call Direct Method on Device");
               // Console.WriteLine("3. List Device(s)");
                Console.WriteLine("0. Exit");
                Console.WriteLine("------------------");
                Console.Write("Select Operation: ");

                try
                {
                    choice = int.Parse(Console.ReadLine());

                    string deviceId = null;
                    string message = null;
                    string method = null;
                    switch (choice)
                    {
                        case 1:
                            {
                                Console.Write("Enter a device Id to send message to: ");
                                deviceId = Console.ReadLine();
                                Console.Write("Enter a message string: ");
                                message = Console.ReadLine();
                                await SendAsync(deviceId, message);
                            }
                            break;
                        case 2:
                            {
                                Console.Write("Enter a target device Id: ");
                                deviceId = Console.ReadLine();
                                Console.Write("Enter a method to be called: ");
                                method = Console.ReadLine();
                                await RunDeviceMethod(deviceId, method, 30);
                            }
                            break;
                    }
                }
                catch (Exception)
                {
                    choice = -1;
                    Console.WriteLine($"[{DateTime.Now.ToString("o")}] Invalid option entered.  Please retry...");
                }
            } while (choice != 0);
        }

        public static WorkshopCredentials GetCredentials(string file)
        {
            WorkshopCredentials credentials = null;
            try
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    credentials = JsonConvert.DeserializeObject<WorkshopCredentials>(Regex.Replace(sr.ReadToEnd(), @"\r\n\t+", ""));
                }
            }
            catch (Exception ex)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now.ToString("o")}] An error occurred while reading in credential file: ({ex.Message}).");
                Console.ResetColor();
            }
            return credentials;
        }

        public async Task SendAsync(string deviceId, string message)
        {
            try
            {
                var msg = new Message(Encoding.UTF8.GetBytes(message));
                msg.Ack = DeliveryAcknowledgement.Full;
                await serviceClient.SendAsync(deviceId, msg);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now.ToString("o")}] A message: {message} successfully sent to [{deviceId}].");
                Console.ResetColor();
            }
            catch (Exception)
            {
                Console.WriteLine($"An error occurred while sending message to {deviceId}");
            }
        }

        public async Task ReceiveFeedbackAsyc()
        {
            try
            {
                var feedbackreceiver = serviceClient.GetFeedbackReceiver();

                while (true)
                {
                    var feedbackbatch = await feedbackreceiver.ReceiveAsync();
                    if (feedbackbatch == null) continue;

                    foreach (var record in feedbackbatch.Records)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine(JsonConvert.SerializeObject(record));
                        Console.ResetColor();
                    }
                    await feedbackreceiver.CompleteAsync(feedbackbatch);
                }
            } 
            catch(Exception)
            {
                Console.WriteLine("An error occurred while retrieving statistics from IoT Hub." );
            }
        }

        public async Task RunDeviceMethod(string deviceId, string deviceMethod, int responseTimeout)
        {
           try
            {
                var serviceClientJob = ServiceClient.CreateFromConnectionString(ConnectionString);
                var method = new CloudToDeviceMethod(deviceMethod);
                var result = await serviceClientJob.InvokeDeviceMethodAsync(deviceId, method);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"[{DateTime.Now.ToString("o")}] : An error occurred while calling the method on device {deviceId}: ");
                Console.WriteLine($"[{DateTime.Now.ToString("o")}] : {ex.Message}");
                Console.ResetColor();
                await QueryTwinRebootReported(deviceId);
            }          
        }
        public async Task QueryTwinRebootReported(string deviceId)
        {
            var twin = await registryManager.GetTwinAsync(deviceId);
            Console.WriteLine(twin.Properties.Reported.ToJson());
            Console.WriteLine(twin.Properties.Desired.ToJson());
        }

        public string ConnectionString { get; private set; }
        private ServiceClient serviceClient;
        private RegistryManager registryManager;
    }
}
