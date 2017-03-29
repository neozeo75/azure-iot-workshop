using System;
using Newtonsoft.Json;

namespace rbpi_connected_device
{
    public class SimulatedSensor
    {
        public SimulatedSensor(SensorType sensorType)
        {
            SensorType = SensorType;
        }

        public string GetSensorReading()
        {
            object reading = null;
            switch (SensorType)
            {
                case SensorType.Temperature:
                    {
                        reading = new
                        {
                            Unit = "C",
                            Value = rnd.NextDouble() * (26.5 - 22.5) + 22.5,
                        };
                    }
                    break;

                case SensorType.Humidity:
                    {
                        reading = new
                        {
                            Unit = "%",
                            Value = rnd.NextDouble() * (60.5 - 45.5) + 45.5,
                        };
                    }
                    break;

                case SensorType.Pressure:
                    {
                        reading = new
                        {
                            Unit = "MPSL",
                            Value = rnd.NextDouble() * (1050.5 - 1010.5) + 1010.5,
                        };
                    }
                    break;

                case SensorType.WindSpeed:
                    {
                        reading = new
                        {
                            Unit = "Kph",
                            Value = rnd.NextDouble() * (45.5 - 0.5) + 0.5,
                        };
                    }
                    break;

                case SensorType.Vibration:
                    {
                        reading = new
                        {
                            Value = new
                            {
                                X = rnd.NextDouble(),
                                Y = rnd.NextDouble(),
                                Z = rnd.NextDouble()
                            }
                        };
                    }
                    break;

                case SensorType.AirQuality:
                    {
                        reading = new
                        {
                            Unit = "AQI (PM25)",
                            Value = rnd.NextDouble() * (150 - 50) + 50,
                        };
                    }
                    break;
            }
            return JsonConvert.SerializeObject(reading);
        }
        private Random rnd = new Random();
        public SensorType SensorType { get; private set; }
    }
    public enum SensorType
    {
        Temperature = 0,
        Humidity = 1,
        Pressure = 2,
        WindSpeed = 3,
        Vibration = 4,
        AirQuality = 5
    };
}
