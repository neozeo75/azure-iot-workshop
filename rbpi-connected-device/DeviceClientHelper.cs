using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Diagnostics;

namespace rbpi_connected_device
{
    public class DeviceClientHelper
    {
        public DeviceClientHelper(string connectionString)
        {
            ConnectionString = connectionString;
        }
        public DeviceClientHelper(string hostName, string deviceId, string deviceKey)
        {
            ConnectionString = $"HostName={hostName};DeviceId={deviceId};SharedAccessKey={deviceId}";
        }

        public async Task OpenConnectionAsync(TransportType transportType = TransportType.Amqp)
        {
            try
            {
                if (deviceClient != null)
                {
                    await deviceClient.OpenAsync();
                }
                else
                {
                    deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, transportType);
                    await deviceClient.OpenAsync();
                }
                Connected = true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task CloseConnectionAsync()
        {
            if (deviceClient != null)
            {
                if (Connected)
                {
                    try
                    {
                        await deviceClient.CloseAsync();
                        Connected = false;
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }
        }
        public string ConnectionString { get; set; }
        public async Task ReceiveEventAsync()
        {
            if (deviceClient != null)
            {
                while (true)
                {
                    var receivedMessage = await deviceClient.ReceiveAsync();
                    if (receivedMessage != null)
                    {
                        var message = Encoding.UTF8.GetString(receivedMessage.GetBytes());
                        Debug.WriteLine($"[{DateTime.Now.ToString("o")}] : Received {message}");

                        var propCount = 0;

                        foreach (var prop in receivedMessage.Properties)
                        {
                            Debug.WriteLine($"Property[{propCount++}> Key={prop.Key} : Value={prop.Value}");
                        }
                    }
                    await deviceClient.CompleteAsync(receivedMessage);
                }
            }
        }

        public async Task SendEventAsync(string message)
        {
            if (Connected)
            {
                try
                {
                    var data = new Message(Encoding.UTF8.GetBytes(message));
                    await deviceClient.SendEventAsync(data);
                    Debug.WriteLine($"[{DateTime.Now.ToString("o")}] data ({message.Length} byte(s)) sent to {HostName}");
                }
                catch (Exception e)
                {
                    throw new Exception($"An error occurred while sending the data to azure iot hub.", e);
                }
            }
            else
            {

            }
        }

        public DeviceClient deviceClient;
        public string HostName { get; set; }
        public string DeviceId { get; set; }
        public string SharedAccessKey { get; set; }
        public bool Connected { set; get; }
    }
}
