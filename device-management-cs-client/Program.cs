using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.Azure.Devices.Shared;
using System.Diagnostics;

namespace device_management_cs_client
{
    
    class Program
    {
        static int TELEMETRY_FREQUENCY = 1000;
        static private Timer timer;
        private static DeviceClient deviceClient;
        private const string deviceId = "rm-device-01";
        private const string DEVICE_CONNECTION_STRING = "HostName=azure-iot-rm-demob13eb.azure-devices.net;DeviceId=rm-device-01;SharedAccessKey=BSPYuktrVo5HxOphchS6q8j9U+8+Rolii3iIyZx6oyw=";
        static private Random rnd = new Random();
        static private Twin deviceTwin;
        static private double temperatureMeanValue = 55;
        static private int telemetryInterval = 1000;
        static void Main(string[] args)
        {
            timer = new Timer(new TimerCallback(SendTelemetryData));
            OpenConnectionAsync();
            InitializeEventHandlers();
            UpdateReportedProperties(DefaultReportedProperties()).Wait();
            RetreiveDeviceTwinAsync().Wait();
            Console.ReadKey();
        }
        static async Task UpdateReportedProperties(TwinCollection reportedConfig)
        {
            try
            {
                await deviceClient.UpdateReportedPropertiesAsync(reportedConfig);
                Twin updatedtwin = await deviceClient.GetTwinAsync();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Updated Twin: {updatedtwin.ToJson(Formatting.Indented)}");
                Console.ResetColor();
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Debug.WriteLine($"An error occurred while updateing reported configuration from the twin: {ex.Message}");
                Debug.WriteLine($"{ex.InnerException}");
                Console.ResetColor();
            }
        }
        static TwinCollection RetreiveReportedProperties()
        {
            TwinCollection reportedConfig = new TwinCollection();
            try
            {
                reportedConfig = deviceTwin.Properties.Reported["Reported"];
                temperatureMeanValue = reportedConfig["TemperatureMeanValue"];
                telemetryInterval = reportedConfig["TelemetryInterval"];
                Console.WriteLine($"Reported Property: {reportedConfig.ToJson(Formatting.Indented)}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Debug.WriteLine($"An error occurred while getting reported configuration from the twin: {ex.Message}");
                Debug.WriteLine($"{ex.InnerException}");
                Console.ResetColor();
            }
            return reportedConfig;
        }

        static void InitializeEventHandlers()
        {
            deviceClient.SetConnectionStatusChangesHandler(OnDeviceConnectionStatusChanged);
            deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, null);
            deviceClient.SetMethodHandlerAsync("Reboot", OnDeviceReboot, null);
            deviceClient.SetMethodHandlerAsync("FactoryReset", OnFactoryReset, null);
            deviceClient.SetMethodHandlerAsync("FirmwareUpdate", OnFirmwareUpdate, null);
        }
        static async void OpenConnectionAsync()
        {
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(DEVICE_CONNECTION_STRING, TransportType.Mqtt);
                await deviceClient.OpenAsync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Debug.WriteLine($"An error occurred while connecting to Azure IoT Hub: {ex.Message}");
                Debug.WriteLine($"{ex.InnerException}");
                Console.ResetColor();
            }
        }

        static async void CloseConnectionAsync()
        {
            await deviceClient.CloseAsync();
        }

        static async Task RetreiveDeviceTwinAsync()
        {
            try
            {
                deviceTwin = await deviceClient.GetTwinAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"device twin: {deviceTwin.ToJson(Formatting.Indented)}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred while retreiving device twin from Azure IoT Hub...");
                Debug.WriteLine($"error: {ex.Message}");
            }
        }

        static void UpdateTimerFrequency(int millisecond)
        {
            timer.Change(0, millisecond);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Telemetry timer frequency has been set to {millisecond}ms... ");
            Console.ResetColor();
        }

        
        static async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            var desiredConfig = desiredProperties["Config"];

            temperatureMeanValue = desiredConfig["TemperatureMeanValue"];
            telemetryInterval = desiredConfig["TelemetryInterval"];

            UpdateTimerFrequency(telemetryInterval);

            TwinCollection reportedConfig = RetreiveReportedProperties();
            TwinCollection updatedConfig = new TwinCollection();

            updatedConfig["TemperatureMeanValue"] = temperatureMeanValue;
            updatedConfig["TelemetryInterval"] = telemetryInterval;

            reportedConfig["Config"] = updatedConfig;

            await UpdateReportedProperties(reportedConfig);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Device's desired property has changed at the service backend.");
            Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));
            Console.ResetColor();
            //var currentTwinConfig = GetTwinConfig();
            //UpdateTwinConfig(currentTwinConfig).Wait();
           // await deviceClient.UpdateReportedPropertiesAsync(desiredProperties);
        }

        static Task<MethodResponse> OnDeviceReboot(MethodRequest request, object context)
        {
            var data = request.DataAsJson;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Device Reboot Method Called...");
            Console.WriteLine("Rebooting...");
            Console.WriteLine(data);
            Console.ResetColor();

            MethodResponse response = new MethodResponse(new byte[0], 500);
            return Task.FromResult(response);
        }

        static Task<MethodResponse> OnFactoryReset(MethodRequest request, object context)
        {
            var data = request.DataAsJson;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Factory Reset Method Called...");
            Console.WriteLine("Rebooting...");
            Console.WriteLine(data);
            Console.ResetColor();

            MethodResponse response = new MethodResponse(new byte[0], 500);
            return Task.FromResult(response);
        }

        static Task<MethodResponse> OnFirmwareUpdate(MethodRequest request, object context)
        {
            var data = request.DataAsJson;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Firmware Update Method Called...");
            Console.WriteLine("Rebooting...");
            Console.WriteLine(data);
            Console.ResetColor();
            MethodResponse response = new MethodResponse(new byte[0], 500);
            return Task.FromResult(response);
        }

        static void OnDeviceConnectionStatusChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Connection Status Changed to {0}", status);
            Console.WriteLine("Connection Status Changed Reason is {0}", reason);
            Console.ResetColor();

            if (status == ConnectionStatus.Connected)
            {
                UpdateTimerFrequency(TELEMETRY_FREQUENCY);
            }
        }

        static void SendData(string data)
        {
            var message = new Message(Encoding.UTF8.GetBytes(data));

            try
            {
                deviceClient.SendEventAsync(message).Wait();
                Console.WriteLine($"sent ({data.Length} bytes): {data}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed sending a message to Azure IoT Hub...");
                Console.WriteLine($"Message: {ex.Message}");
                Debug.WriteLine($"Message: {ex.InnerException.Message}");
                Console.ResetColor();
            }
        }
        static void SendTelemetryData(object state)
        {
            var data = SensorData();
            SendData(data);
        }

        static string SensorData()
        {
            return JsonConvert.SerializeObject(new
            {
                DeviceID = deviceId,
                DateTime = DateTime.Now.ToString("o"),
                Temperature = 15 * rnd.NextDouble() + 15,
                Humidity = 15 * rnd.NextDouble() + 15
            });
        }
        static string DeviceInformation()
        {
            var devicemetadata = new
            {
                ObjectType = "DeviceInfo",
                IsSimulatedDevice = 0,
                Version = "1.0",
                DeviceProperties = new
                {
                    HubEnabledState = 1,
                    DeviceID = deviceId
                },
            };
            return JsonConvert.SerializeObject(devicemetadata);
        }
        static TwinCollection DefaultReportedProperties()
        {

            TwinCollection reportedproperties = new TwinCollection();

            reportedproperties["Device"] = new
            {
                DeviceState = "Normal",
                Location = new
                {
                    Latitude = 37.575869,
                    Longitude = 126.976859
                }
            };

            reportedproperties["Config"] = new
            {
                TemperatureMeanValue = temperatureMeanValue,
                TelemetryInterval = telemetryInterval
            };

            reportedproperties["System"] = new
            {
                Manufacturer = "Microsoft",
                FirmwareVersion = "2.22",
                InstalledRAM = "8 MB",
                ModelNumber = "DB-14",
                Platform = "Plat 9.75",
                Processor = "i3-9",
                SerialNumber = "SER99"
            };

            reportedproperties["Location"] = new
            {
                Latitude = 37.575869,
                Longitude = 126.976859
            };

            reportedproperties["SupportedMethods"] = new
            {
                Reboot = "Reboot the device",
                Reset = "Reset the device",
                InitiateFirmwareUpdate = "Updates device Firmware. Use parameter FwPackageURI to specifiy the URI of the firmware file"
            };

            return reportedproperties;

        }
        /*
        static dynamic ReportedProperties()
        {
            return new
            {
                Device = new
                {
                    DeviceState = "Normal",
                    Location = new
                    {
                        Latitude = 37.575869,
                        Longitude = 126.976859
                    }
                },
                Config = new
                {
                    TemperatureMeanValue = 56.7,
                    TelemetryInterval = 45
                },
                System = new
                {
                    Manufacturer = "Contoso Inc.",
                    FirmwareVersion = "2.22",
                    InstalledRAM = "8 MB",
                    ModelNumber = "DB-14",
                    Platform = "Plat 9.75",
                    Processor = "i3-9",
                    SerialNumber = "SER99"
                },
                Location = new
                {
                    Latitude = 37.575869,
                    Longitude = 126.976859
                },
                SupportedMethods = new
                {
                    Reboot = "Reboot the device",
                    Reset = "Reset the device",
                }
            }; 
        }
        */
    }
}
