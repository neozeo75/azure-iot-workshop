using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace service_backend_device_management
{
    class Program
    {
        private const string IOTHUB_CONNECTION_STRING = "HostName=azure-iot-rm-demob13eb.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=/oDc5W0sEcMLvBscVmw4wqCxT5VBbYKcGm895H7g7e0=";
        private const string DEVICE_ID = "rm-device-01";
        static private string E_TAG;
        static private ServiceClient serviceClient;
        static private RegistryManager registryManager;

        static void Main(string[] args)
        {
            OpenConnectionAsync();
            RetreiveDeviceTwin(DEVICE_ID).Wait();
            RetreiveListOfSupportedMethods(DEVICE_ID).Wait();
            InvokeDeviceMethod().Wait();
            UpdateDesiredProperty().Wait();
            Console.ReadKey();
            CloseConnectionAsync();
        }
        static async Task RetreiveDeviceTwin(string deviceId)
        {
            try
            {
                var twin = await registryManager.GetTwinAsync(deviceId);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Device from: {DEVICE_ID}");
                Console.WriteLine($"{twin.ToJson(Formatting.Indented)}");
                Console.ResetColor();
                E_TAG = twin.ETag;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred while retreiving device twin (message: {ex.Message})");
                Console.ResetColor();
            }
        }
        static async void OpenConnectionAsync()
        {
            try
            {
                serviceClient = ServiceClient.CreateFromConnectionString(IOTHUB_CONNECTION_STRING);
                registryManager = RegistryManager.CreateFromConnectionString(IOTHUB_CONNECTION_STRING);
                await serviceClient.OpenAsync();
                await registryManager.OpenAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while connecting to Azure IoT Hub (Message: {ex.Message}...");
            }
        }
        static async Task RetreiveListOfSupportedMethods(string deviceId)
        {
            Twin twin = await registryManager.GetTwinAsync(deviceId);
            TwinCollection x = twin.Properties.Reported["SupportedMethods"];
            Console.WriteLine(x.ToJson());
        }

        static async Task UpdateDesiredProperty()
        {
            
            Twin x = new Twin(DEVICE_ID);
        
            x.Properties.Desired["Config"] = new
            {
                TemperatureMeanValue = 100.5,
                TelemetryInterval = 50
            };

            try
            {
          

                x = await registryManager.UpdateTwinAsync(DEVICE_ID, x, E_TAG);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Desired Property successfully updated to {x.ToJson()}...");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while updating desired properties of the twin...");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException.Message);
            }
        }
        static async Task InvokeDeviceMethod()
        {
            Console.Write("Enter device id: ");
            var id = Console.ReadLine();
            if (id.Length < 1) id = DEVICE_ID;

            Console.Write("Eneter Method Name: ");
            var method = Console.ReadLine();

            Console.WriteLine();

            CloudToDeviceMethod cloudMethod = new CloudToDeviceMethod(method);
            var result = await serviceClient.InvokeDeviceMethodAsync(id, cloudMethod);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Cloud method has been called (status: {result.Status})");
            Console.WriteLine($"Method Payload: {result.GetPayloadAsJson()}");
            Console.ResetColor();
        }

        static async void CloseConnectionAsync()
        {

        }

    }
}
