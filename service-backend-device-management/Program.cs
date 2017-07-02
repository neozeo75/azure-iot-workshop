using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace service_backend_device_management
{
    class Program
    {
        static ServiceClient serviceClient;
        static RegistryManager registryManager;
        const string ConnectionString = "HostName=azureiotworkshophub.azure-devices.net;DeviceId=device-mgmt-01;SharedAccessKey=uSrP2XscJPQQ9zeBjw3KUATU4G6cxb8SD+FH5FRV65s=";

        static async void UpdateTwinAsync(string deviceId)
        {
            var patch = new
            {
                tags = new
                {
                    location = new
                    {
                        GeoLocation = new
                        {
                            Longitude = 12.3456855,
                            Latitude = 234.233345
                        },
                        Address = new
                        {
                            Region = "Asia Pacific",
                            City = "Seoul/Korea",
                            Plant = "Microsoft Campus Building 29",
                        }
                    }
                }
            };

            var deviceTwin = await registryManager.GetTwinAsync(deviceId);
            var deviceTwinUpdate = await registryManager.UpdateTwinAsync(deviceId, JsonConvert.SerializeObject(patch), deviceTwin.ETag);
        }

        static void Main(string[] args)
        {
            serviceClient = ServiceClient.CreateFromConnectionString("HostName=azureiotworkshophub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=djCNmksyeHvG9MT+ln67Y4e7ghZrGU3iHVLT6SG2ZXQ=");
            registryManager = RegistryManager.CreateFromConnectionString("HostName=azureiotworkshophub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=djCNmksyeHvG9MT+ln67Y4e7ghZrGU3iHVLT6SG2ZXQ=");

            serviceClient.OpenAsync();
            registryManager.OpenAsync();

            UpdateTwinAsync("device-mgmt-01");

            Console.ReadKey();
        }

    }
}
