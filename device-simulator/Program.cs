using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Shared;
using System.IO;
using System.Text.RegularExpressions;

namespace DeviceSimulator
{
    class Program
    {
     private static void Main(string[] args)
        {
            var source = new CancellationTokenSource();
            var token = source.Token;
#if DEBUG
            var credentials = DeviceClientHelper.GetCredentials(@"..\..\..\azure-iot-workshop-credentials.xld");
            var connectionString = credentials.DeviceConnectionString.ToString();
#else
             Console.Write("Enter a device connection string: ");
            var connectionString = Console.ReadLine();
#endif
            deviceClientHelper = new DeviceClientHelper(connectionString);
            deviceClientHelper.OpenAsync().Wait(token);

            Task.Run(async () =>
            {
                if (token.IsCancellationRequested) { 
                    token.ThrowIfCancellationRequested();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now.ToString("o")}] sending thread [{Thread.CurrentThread.ManagedThreadId}] started...");
                    Console.ResetColor();
                    await deviceClientHelper.SendAsync(5000);
                }
            }, token);

            Task.Run(async () =>
            {
                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now.ToString("o")}] receiving thread [{Thread.CurrentThread.ManagedThreadId}] started...");
                    Console.ResetColor();
                    await deviceClientHelper.ReceiveAsync();
                }
            }, token);

            Console.ReadKey();
            deviceClientHelper.CloseAsyc().Wait(token);
            source.Cancel();
        }

        private static DeviceClientHelper deviceClientHelper;
    }

    internal class DeviceClientHelper
    {
        public DeviceClientHelper(string connectionString)
        {
            ConnectionString = connectionString;
            HostName = connectionString.Split(';')[0].Split('=')[1];
            DeviceId = connectionString.Split(';')[1].Split('=')[1];
            SharedAccessKey = connectionString.Split(';')[2].Split('=')[1];
        }

        public DeviceClientHelper(string hostName, string deviceId, string sharedAccessKey)
        {
            this.HostName = hostName;
            this.DeviceId = deviceId;
            this.SharedAccessKey = sharedAccessKey;
            this.ConnectionString = $"HostName={HostName};DeviceID={DeviceId};SharedAccessKey={SharedAccessKey}";
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
            catch(Exception ex)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred while reading in credential file: ({ex.Message}).");
                Console.ResetColor();
            }
            return credentials;
        }
        public async Task OpenAsync()
        {
            if (deviceClient != null) return;
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, TransportType.Mqtt);
                await deviceClient.OpenAsync();
                ConnectionStatus = true;
            }
            catch (AggregateException ex)
            {
                foreach (var exception in ex.InnerExceptions)
                {
                    ConsoleMessage($"An error occurred while initialization: {exception}", ConsoleMessageType.Error);
                }
            }
            catch (Exception ex)
            {
                ConsoleMessage($"An error occurred while opening connection to IoT Hub (message: {ex.Message}).", ConsoleMessageType.Error);
            }
        }
 
        public async Task SendAsync(int interval)
        {
            if (deviceClient != null)
            {
                while (true)
                {
                    try
                    {
                        var telemetry = PseudoSensorDataGenerator();
                        var message = new Message(Encoding.UTF8.GetBytes(telemetry));
                        await deviceClient.SendEventAsync(message);
                        ConsoleMessage(telemetry, ConsoleMessageType.Normal);
                    }
                    catch (Exception ex)
                    {
                        ConsoleMessage($"An error while opening connection to IoT Hub (message: {ex.Message}).",ConsoleMessageType.Error);
                    }
                    Thread.Sleep(interval);
                }
            }
            else
            {
                ConsoleMessage("A connection must be estabilished before sending message(s) to IoT Hub.", ConsoleMessageType.Warning);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task ReceiveAsync()
        {
            if (deviceClient != null)
            {
                while (true)
                {
                    var receivedMessage = await deviceClient.ReceiveAsync();
                    if (receivedMessage != null)
                    {
                        var message = Encoding.UTF8.GetString(receivedMessage.GetBytes());
                        ConsoleMessage($"[{DateTime.Now.ToString("o")}] : Received {message}", ConsoleMessageType.Information);
                        
                        var propCount = 0;

                        foreach (var prop in receivedMessage.Properties)
                        {
                            ConsoleMessage($"Property[{propCount++}> Key={prop.Key} : Value={prop.Value}", ConsoleMessageType.Information);
                        }
                    }
                    await deviceClient.CompleteAsync(receivedMessage);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsyc()
        {
            if (deviceClient != null)
            {
                try
                {
                    await deviceClient.CloseAsync();
                    await deviceClient?.SetMethodHandlerAsync("WriteToConsole", null, null);
                    ConnectionStatus = false;
                }
                catch (Exception ex)
                {
                    ConsoleMessage($"An error while closing connection from IoT Hub (message: {ex.Message}).", ConsoleMessageType.Error);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string PseudoSensorDataGenerator()
        {
            var sensor = new
            {
                deviceId = DeviceId,

                date = DateTime.Now.ToString("o"),

                temperature = rnd.NextDouble() * (26.5 - 22.5) + 22.5,

                humidity = rnd.NextDouble() * (60.5 - 45.5) + 45.5,

                pressure = rnd.NextDouble() * (1050.5 - 1010.5) + 1010.5,

                windspeed = rnd.NextDouble() * (45.5 - 0.5) + 0.5,

                longitude = "37.575869",

                latitude = "126.976859"
            };
            return JsonConvert.SerializeObject(sensor);
        }

        private readonly Random rnd = new Random();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageType"></param>
        private static void ConsoleMessage(string message, ConsoleMessageType messageType)
        {
            switch (messageType)
            {
                case ConsoleMessageType.Information:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case ConsoleMessageType.Normal:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case ConsoleMessageType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case ConsoleMessageType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }
            Console.WriteLine(message);
            Debug.WriteLine(message);
            Console.ResetColor();
        }
        private enum ConsoleMessageType
        {
            Normal,
            Error,
            Warning,
            Information
        }

        private DeviceClient deviceClient;
        public bool ConnectionStatus { get; private set; }
        public string ConnectionString { get; private set; }
        public string HostName { get; }
        public string DeviceId { get; }
        public string SharedAccessKey { get; }
    }
}
