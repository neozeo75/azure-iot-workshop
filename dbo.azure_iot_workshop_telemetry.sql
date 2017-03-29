CREATE TABLE [dbo].[azure_iot_workshop_telemetry] (
    [DeviceId]       NVARCHAR (50) NULL,
    [Date]           DATETIME NULL,
    [MaxTemperature] FLOAT (53)    NULL,
    [AvgTemperature] FLOAT (53)    NULL,
    [MinTemperature] FLOAT (53)    NULL,
    [MaxHumidity]    FLOAT (53)    NULL,
    [AvgHumidity]    FLOAT (53)    NULL,
    [MinHumidity]    FLOAT (53)    NULL,
    [MaxPressure]    FLOAT (53)    NULL,
    [AvgPressure]    FLOAT (53)    NULL,
    [MinPressure]    FLOAT (53)    NULL
);

