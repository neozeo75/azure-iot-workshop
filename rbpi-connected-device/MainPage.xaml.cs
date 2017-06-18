using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using System.Text;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace rbpi_connected_device
{
    public class ControlCommand

    {
        public string target { get; set; }
        public string command { get; set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class MainPage : Page

    {
        private bool isConnected = false;
        private bool isSending = false;
        
        private DeviceClient deviceClient;
        private Random rnd = new Random();
        private DispatcherTimer timer = new DispatcherTimer();
        private SolidColorBrush red = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush green = new SolidColorBrush(Windows.UI.Colors.Green);
        private SolidColorBrush blue = new SolidColorBrush(Windows.UI.Colors.Blue);
        private SolidColorBrush gray = new SolidColorBrush(Windows.UI.Colors.Gray);
        private string dkey;
        private string did;

        public MainPage()
        {
            this.InitializeComponent();
            timer.Tick += Send_TickAsync;
            TxtInterval.Text = "1000";
#if DEBUG
            TxtDeviceId.Text = did = "device-001";
            TxtDeviceKey.Text = dkey = "8KKvMVZQLG8/Mjq64Mn7KrFOB6vTCNJbpozui1xb6NU=";
#else
            TxtDeviceId.Text = did = "";
            TxtDeviceKey.Text = dkey = "";
            did = "";
            dkey = "";
#endif
            BtnSend.IsEnabled = false;

        }

        private async void Send_TickAsync(object sender, object e)
        {
            var telemetry = new
            {
                date = DateTime.Now.ToString("o"),
                deviceId = did,
                temperature = rnd.NextDouble() * (26.5 - 22.5) + 22.5,
                humidity = rnd.NextDouble() * (60.5 - 45.5) + 45.5,
                pressure = rnd.NextDouble() * (1050.5 - 1010.5) + 1010.5,
                windspeed = rnd.NextDouble() * (45.5 - 0.5) + 0.5,
                longitude = "37.575869",
                latitude = "126.976859"
            };
            if (isConnected)
            {
                try
                {
                    var telemetryText = JsonConvert.SerializeObject(telemetry);
                    TxtTelemetries.Text = telemetryText;
                    await deviceClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes(telemetryText)));
                }
                catch (Exception)
                {
                    Debug.WriteLine("An error occurred while sending data telemetry...");
                }
            }
            else
            {
                Debug.WriteLine("Connection must be made to IoT Hub before sending data...");
            }
        }



        private async Task CommandProcessor(string command)
        {
            try
            {
                var cmd = JsonConvert.DeserializeObject<ControlCommand>(command);
                switch (cmd.target)
                {
                    case "LED_GREEN":
                        if (cmd.command == "ON")
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                LED_GREEN.Fill = green;
                                Debug.WriteLine("GREEN_ON");
                            });
                        }

                        else
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                LED_GREEN.Fill = gray;
                                Debug.WriteLine("GREEN_OFF");
                            });
                        }
                        break;

                    case "LED_RED":
                        if (cmd.command == "ON")
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                LED_RED.Fill = red;
                                Debug.WriteLine("RED_ON");
                            });
                        }
                        else
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                LED_RED.Fill = gray;
                                Debug.WriteLine("RED_OFF");
                            });
                        }

                        break;

                    case "LED_BLUE":

                        if (cmd.command == "ON")
                        {

                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                LED_BLUE.Fill = blue;
                                Debug.WriteLine("BLUE_ON");
                            });
                        }
                        else
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                LED_BLUE.Fill = gray;
                                Debug.WriteLine("BLUE_OFF");
                            });
                        }
                        break;
                }
            }
            catch
            {
                Debug.WriteLine("An error occurred while parsing the control command");
            }
        }

        private async Task ReceiveDataAsync()
        {
            if (deviceClient != null)
            {
                while (true)
                {
                    var receivedMessage = await deviceClient.ReceiveAsync();
                    if (receivedMessage != null)
                    {
                        try
                        {
                            var message = Encoding.UTF8.GetString(receivedMessage.GetBytes());
                            //TxtCommands.Text = message;
                            Debug.WriteLine($"[{DateTime.Now.ToString("o")}] : Received {message}");
                            await CommandProcessor(message);
                            var propCount = 0;
                            foreach (var prop in receivedMessage.Properties)
                            {
                                Debug.WriteLine($"Property[{propCount++}> Key={prop.Key} : Value={prop.Value}");
                            }
                            await deviceClient.CompleteAsync(receivedMessage);
                        }
                        catch
                        {
                            await deviceClient.RejectAsync(receivedMessage);
                        }
                    }
                }
            }
        }

        private async void BtnConnect_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (isConnected)
            {
                try
                {
                    await deviceClient.CloseAsync();
                    isConnected = false;
                    BtnConnect.Content = "Connect";
                    BtnSend.IsEnabled = false;
                }
                catch
                {
                    Debug.WriteLine("An error occurred while closing the connection from IoT Hub...");
                }
            }
            else
            {
                try
                {
#if DEBUG
                    TxtDeviceId.Text = did;
                    TxtDeviceKey.Text = dkey;
#else
                    did = TxtDeviceId.Text;
                    dkey = TxtDeviceKey.Text;
#endif
                    deviceClient = DeviceClient.CreateFromConnectionString($"HostName=azureiotworkshophub.azure-devices.net;DeviceId={did};SharedAccessKey={dkey}");
                    await deviceClient.OpenAsync();
                    var t = Task.Run(ReceiveDataAsync);
                  //  t.Start();
                    isConnected = true;
                    BtnConnect.Content = "Disconnect";
                    BtnSend.IsEnabled = true;
                }
                catch
                {
                    Debug.WriteLine("An error occurred while opening the connection to IoT Hub...");
                }
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (isSending)
            {
                timer.Stop();
                isSending = false;
                BtnSend.Content = "Send";
            }
            else
            {
                var milisecond = int.Parse(TxtInterval.Text);
                var span = TimeSpan.FromMilliseconds(milisecond);
                timer.Interval = span;
                timer.Start();
                isSending = true;
                BtnSend.Content = "Stop";
            }
        }
    }
}