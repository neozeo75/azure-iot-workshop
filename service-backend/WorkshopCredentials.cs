using System;
using Newtonsoft.Json;

public class IotHubConnectionString
{
    public string HostName { get; set; }
    public string SharedAccessKeyName { get; set; }
    public string SharedAccessKey { get; set; }
    public override string ToString()
    {
        return $"HostName={HostName};SharedAccessKeyName={SharedAccessKeyName};SharedAccessKey={SharedAccessKey}";
    }
}

public class DeviceConnectionString
{
    public string HostName { get; set; }
    public string DeviceId { get; set; }
    public string SharedAccessKey { get; set; }
    public override string ToString()
    {
        return $"HostName={HostName};DeviceId={DeviceId};SharedAccessKey={SharedAccessKey}";
    }
}

public class EventHubConnectionString
{
    public string EventHubName { get; set; }
    public string EndPoint { get; set; }
    public string SharedAccessKeyName { get; set; }
    public string SharedAccessKey { get; set; }
    public override string ToString()
    {
        return $"Endpoint=sb://{EndPoint}/;SharedAccessKeyName={SharedAccessKeyName};SharedAccessKey={SharedAccessKey}";
    }
}

public class StorageAccountConnectionString
{
    public string AccountName { get; set; }
    public string AccountKey { get; set; }
    public override string ToString()
    {
        return $"DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={AccountKey}";
    }
}

public class WorkshopCredentials
{
    public IotHubConnectionString IotHubConnectionString { get; set; }
    public DeviceConnectionString DeviceConnectionString { get; set; }
    public EventHubConnectionString EventHubConnectionString { get; set; }
    public StorageAccountConnectionString StorageAccountConnectionString { get; set; }
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}