using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Shared;
using System.IO;
using System.Text.RegularExpressions;

namespace IdentityManager
{
    internal class Program
    {
        private static RegistryManagerHelper registryManagerHelper;

        private static void Main(string[] args)
        {
#if DEBUG
            var credentials = RegistryManagerHelper.GetCredentials(@"..\..\..\azure-iot-workshop-credentials.xld");
            var connectionString = credentials.IotHubConnectionString.ToString();
#else
            Console.Write("Enter a connection string to IoT Hub: ");
            var connectionString = Console.ReadLine();
#endif
            registryManagerHelper = new RegistryManagerHelper(connectionString);
            registryManagerHelper.OpenAsync().Wait();
            registryManagerHelper.UserPrompt().Wait();
            registryManagerHelper.CloseAsync().Wait();
            Console.ReadKey();
        }
    }

    public class RegistryManagerHelper
    {
        public RegistryManagerHelper(string connectionString)
        {
            ConnectionString = connectionString;
            HostName = connectionString.Split(';')[0].Split('=')[1];
            SharedAccess = connectionString.Split(';')[1].Split('=')[1];
            SharedAcessKey = connectionString.Split(';')[2].Split('=')[1];
        }

        public RegistryManagerHelper(string hostName, string sharedAccess, string sharedAccessKey)
        {
            HostName = hostName;
            SharedAccess = sharedAccess;
            SharedAcessKey = sharedAccessKey;
            ConnectionString = $"HostName={hostName};SharedAccessName={sharedAccess}; SharedAccessKey={sharedAccessKey}";
        }

        public async Task UserPrompt()
        {
            var choice = 0;
            do
            {
                Console.WriteLine("1. Add a Device");
                Console.WriteLine("2. Remove a Device");
                Console.WriteLine("3. List Device(s)");
                Console.WriteLine("4. Twin");
                Console.WriteLine("0. Exit");
                Console.WriteLine("------------------");
                Console.Write("Select Operation: ");
                
                try
                {
                    choice = int.Parse(Console.ReadLine());
                    string deviceId = null;

                    switch (choice)
                    {
                        case 1:
                            {
                                Console.Write("Enter a device Id to be created: ");
                                deviceId = Console.ReadLine();
                                await AddDeviceAsync(deviceId);
                                Console.WriteLine();
                            }
                            break;
                        case 2:
                            {
                                Console.Write("Enter a device Id to be removed: ");
                                deviceId = Console.ReadLine();
                                await DeleteDeviceAsync(deviceId);
                                Console.WriteLine();
                            }
                            break;
                        case 3:
                            {
                                await ListDevicesAsync();
                                Console.WriteLine();
                            }
                            break;
                        case 4:
                            {
                                try
                                {
                                    Console.Write("Enter Twin Query: ");
                                    var condition = Console.ReadLine();
                                    await TwinAsync(condition);
                                }
                                catch (Exception e)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"[{DateTime.Now.ToString("o")}] An error occurred while querying a twin (error: {e.Message})...");
                                    Console.ResetColor();
                                }
                            }
                            break;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Invalid option entered. Please retry...");
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

        public async Task OpenAsync()
        {
            if (!connectionStatus)
            {
                try
                {
                    registryManager = RegistryManager.CreateFromConnectionString(ConnectionString);
                    await registryManager.OpenAsync();
                    Console.WriteLine($"[{DateTime.Now.ToString("o")}] Connection etablished to Azure IoT Hub successfully.");
                    connectionStatus = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{DateTime.Now.ToString("o")}] An error occured while opening connection to IoT Hub (message: {e.Message}).");
                    connectionStatus = false;
                }
            }
        }

        public async Task CloseAsync()
        {
            if (!connectionStatus)
            {
                try
                {
                    await registryManager.CloseAsync();
                    connectionStatus = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{DateTime.Now.ToString("o")}] An error occured while closing connection to IoT Hub (message: {e.Message}).");
                    connectionStatus = true;
                }
            }
        }

        public async Task AddDeviceAsync(string deviceId)
        {
            if (connectionStatus)
            {
                var device = new Device(deviceId);

                try
                {
                    device = await registryManager.AddDeviceAsync(device);
                }
                catch (DeviceAlreadyExistsException)
                {
                    device = await registryManager.GetDeviceAsync(deviceId);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{DateTime.Now.ToString("o")}] An error occured while adding a device to IoT Hub (message: {e.Message}).");
                }
                
                Console.WriteLine("--------------------");
                Console.WriteLine($"  Device ID: {deviceId}");
                Console.WriteLine($"  Primary Key: {device.Authentication.SymmetricKey.PrimaryKey}");
                Console.WriteLine($"  ETag: {device.ETag}");
                Console.WriteLine($"  Generated ID: {device.GenerationId}");
                Console.WriteLine("--------------------");
                Console.WriteLine($"# of devices: {GetDeviceCount().Result}");
                Console.WriteLine();
            }
        }

        public async Task TwinAsync(string twinQueryWithCondition)
        {
            var query = registryManager.CreateQuery(twinQueryWithCondition, 1000);
            while (query.HasMoreResults)
            {
                var page = await query.GetNextAsTwinAsync();
                foreach (var twin in page)
                {
                    try
                    {
                        Console.WriteLine($"[{DateTime.Now.ToString("o")}] Device Twin: {JsonConvert.SerializeObject(twin)}");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"[{DateTime.Now.ToString("o")}] An error occurred while serializing a twin object.");
                    }
                }
            }
        }

        public async Task DeleteDeviceAsync(string deviceId)
        {
            if (connectionStatus)
            {
                try
                {
                    await registryManager.RemoveDeviceAsync(deviceId);
                    Console.WriteLine($"[{DateTime.Now.ToString("o")}] Device: {deviceId} has been deleted from IoT Hub.");
                    Console.WriteLine();
                }
                catch (DeviceNotFoundException e)
                {
                    Console.WriteLine($"[{DateTime.Now.ToString("o")}] An error occured while removing a device from IoT Hub (message: {e.Message}).");
                }
            }
        }

        public async Task ListDevicesAsync()
        {
            if (connectionStatus)
            {
                try
                {
                    var list = (List<Device>) await registryManager.GetDevicesAsync(1000);

                    foreach (var device in list)
                    {
                        Console.WriteLine("--------------------");
                        Console.WriteLine($"Device ID: {device.Id}");
                        Console.WriteLine($"Device Key: {device.Authentication.SymmetricKey.PrimaryKey}");
                        Console.WriteLine($"Device Etag: {device.ETag}");
                        Console.WriteLine($"Generation ID: {device.GenerationId}");
                        Console.WriteLine($"ConnectionStat: {device.ConnectionState}");
                        Console.WriteLine("--------------------");
                    }

                    Console.WriteLine($"# of devices: {GetDeviceCount().Result}");
                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{DateTime.Now.ToString("o")}] An error occured while retrieving a list of devices from IoT Hub (message: {e.Message}).");
                }
            }
        }

        public async Task<int> GetDeviceCount()
        {
           var list = (List<Device>) await registryManager.GetDevicesAsync(1000);
            return list.Count;
        }

        private RegistryManager registryManager;
        public bool connectionStatus { get; private set; }
        public string ConnectionString { get; set; }
        public string HostName { get; private set; }
        public string SharedAccess { get; private set; }
        public string SharedAcessKey { get; private set; }
    }
}
