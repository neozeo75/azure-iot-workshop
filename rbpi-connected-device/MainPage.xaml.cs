using System;

using System.Collections.Generic;

using System.IO;

using System.Linq;

using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;

using Windows.Foundation.Collections;

using Windows.UI.Xaml;

using Windows.UI.Xaml.Controls;

using Windows.UI.Xaml.Controls.Primitives;

using Windows.UI.Xaml.Data;

using Windows.UI.Xaml.Input;

using Windows.UI.Xaml.Media;

using Windows.UI.Xaml.Navigation;

using Windows.Storage;

using System.Diagnostics;

using System.Threading.Tasks;

using System.Threading;

using Newtonsoft.Json;

using Microsoft.Azure.Devices.Client;

using System.Text;

using Windows.ApplicationModel.Core;

using Windows.UI.Core;

using Windows.UI.Popups;



// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409



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

        private CancellationTokenSource source;

        private DeviceClient deviceClient;

        private Random rnd = new Random();

        private DispatcherTimer timer = new DispatcherTimer();

        private SolidColorBrush red = new SolidColorBrush(Windows.UI.Colors.Red);

        private SolidColorBrush green = new SolidColorBrush(Windows.UI.Colors.Green);

        private SolidColorBrush blue = new SolidColorBrush(Windows.UI.Colors.Blue);

        private SolidColorBrush gray = new SolidColorBrush(Windows.UI.Colors.Gray);



        public MainPage()

        {

            this.InitializeComponent();

            this.InitializeDeviceClient();

            Task.Run(SendDataAsync);

            Task.Run(ReceiveDataAsync);

        }

        private async void InitializeDeviceClient()

        {

            deviceClient = DeviceClient.CreateFromConnectionString("HostName=azureiotworkshophub.azure-devices.net;DeviceId=device-005;SharedAccessKey=Wt5L2a+gjoNGbotX5eJ8JF5mW6luJaZF8nkpibWc1BM=");

            await deviceClient.OpenAsync();

        }



        private async Task SendDataAsync()

        {

            while (true)

            {

                var telemetry = new

                {

                    date = DateTime.Now.ToString("o"),

                    deviceId = "device-005",

                    temperature = rnd.NextDouble() * (26.5 - 22.5) + 22.5,

                    humidity = rnd.NextDouble() * (60.5 - 45.5) + 45.5,

                    pressure = rnd.NextDouble() * (1050.5 - 1010.5) + 1010.5,

                    windspeed = rnd.NextDouble() * (45.5 - 0.5) + 0.5,

                    longitude = "37.575869",

                    latitude = "126.976859"

                };

                await deviceClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemetry))));

                await Task.Delay(5000);

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

    }

}