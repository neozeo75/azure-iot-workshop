using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.Azure.Devices.Shared;
using System.Diagnostics;

namespace device_management_cs_client
{
    class Program
    {
        private static DeviceClient deviceClient;
        private const string deviceId = "device-mgmt-01";
        static Timer timer = new Timer(new TimerCallback(SendTelemetryData));
        static void Main(string[] args)
        {
            deviceClient = DeviceClient.CreateFromConnectionString("HostName=azureiotworkshophub.azure-devices.net;DeviceId=device-mgmt-01;SharedAccessKey=uSrP2XscJPQQ9zeBjw3KUATU4G6cxb8SD+FH5FRV65s=",TransportType.Mqtt);
            deviceClient.OpenAsync();

            GetDeviceTwin();

            deviceClient.SetConnectionStatusChangesHandler(OnDeviceConnectionStatusChanged);
            deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropoertyChanged, null);
            
            deviceClient.SetMethodHandlerAsync("OnDeviceReboot", OnDeviceReboot, null);
            deviceClient.SetMethodHandlerAsync("OnFactoryReset", OnFactoryReset, null);
            deviceClient.SetMethodHandlerAsync("OnDeviceControll", OnDeviceControll, null);

            Console.ReadKey();
        }
        static async void GetDeviceTwin()
        {
            var x = await deviceClient.GetTwinAsync();
            var frequency = x.Properties.Desired["Frequency"];
            var battery = x.Properties.Desired["Battery"];
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Desired Telemetry Frequency: {frequency}");
            Console.WriteLine($"Desired Battery Level: {battery}");
            Console.ResetColor();
            Console.WriteLine($"twin: {x.ToJson()}");
        }

        static void UpdateTimerFrequency(int millisecond)
        {
            timer.Change(0, millisecond);
            Console.WriteLine($"telemetry timer frequency has been set to {millisecond}ms... ");
        }
        static async Task OnDesiredPropoertyChanged(TwinCollection desiredProperties, object userContext)
        {
            Console.WriteLine("device property has changed.");
            Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));
            Console.WriteLine("Sending current time as reported property...");

            var batteryValue = desiredProperties["Battery"];
            int frequencyValue = desiredProperties["Frequency"];

            Console.WriteLine($"Battery Value: {batteryValue}");
            Console.WriteLine($"Frequency Value: {frequencyValue}");
            try
            {

                UpdateTimerFrequency(frequencyValue);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.Message}");
            }
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DesiredPropertyChangeReceived"] = DateTime.Now.ToString("o");
            reportedProperties["Frequency"] = frequencyValue;
            reportedProperties["Battery"] = batteryValue;
            
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }


        static Task<MethodResponse> OnDeviceReboot(MethodRequest request, object context)
        {
            var data = request.DataAsJson;
            Console.WriteLine("Device Reboot Method Called...");
            Console.WriteLine(data);
            MethodResponse response = new MethodResponse(new byte[0], 500);
            return Task.FromResult(response);
        }
        static Task<MethodResponse> OnFactoryReset(MethodRequest request, object context)
        {
            var data = request.DataAsJson;
            Console.WriteLine("Factory Reset Method Called...");
            Console.WriteLine(data);
            MethodResponse response = new MethodResponse(new byte[0], 500);
            return Task.FromResult(response);
        }
        static Task<MethodResponse> OnDeviceControll(MethodRequest request, object context)
        {
            var data = request.DataAsJson;
            Console.WriteLine("Device Controll Method Called...");
            Console.WriteLine(data);
            MethodResponse response = new MethodResponse(new byte[0], 500);
            return Task.FromResult(response);
        }

        static async void  OnDeviceConnectionStatusChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Console.WriteLine();
            Console.WriteLine("Connection Status Changed to {0}", status);
            Console.WriteLine("Connection Status Changed Reason is {0}", reason);
            Console.WriteLine();

            if (status == ConnectionStatus.Connected)
            {
                Twin twin = await deviceClient.GetTwinAsync();
                int frequency = twin.Properties.Desired["Frequency"];
                Console.WriteLine($"intial timer frequency from twin: {frequency}ms");
                UpdateTimerFrequency(frequency);
            }
        }


        static void SendTelemetryData(object state)
        {
            var data = SensorData();
            var message = new Message(Encoding.UTF8.GetBytes(data));
            try
            {
                deviceClient.SendEventAsync(message).Wait();
                Console.WriteLine($"sent ({data.Length} bytes): {data}");
            }
            catch
            {
                Console.WriteLine("Failed sending a message to Azure IoT Hub...");
            }
        }

        static async void CloseConnectionAsync()
        {
            await deviceClient.CloseAsync();
        }
        static private Random rnd = new Random();
        static string SensorData()
        {
            return JsonConvert.SerializeObject(new
            {
                deviceId = deviceId,
                date = DateTime.Now.ToString("o"),
                temperature = 15 * rnd.NextDouble() + 15,
                humidity = 15 * rnd.NextDouble() + 15
            });
        }
    }
}
