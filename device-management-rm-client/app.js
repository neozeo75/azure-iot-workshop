'use strict';
var FileSystem = require('fs');
var Protocol = require('azure-iot-device-mqtt').Mqtt;
var Client = require('azure-iot-device').Client;
var ConnectionString = require('azure-iot-device').ConnectionString;
var Message = require('azure-iot-device').Message;

var connection_string = FileSystem.readFileSync('../device-management-rm-client-connectionstring.xld', 'utf8').trim().toString();
var deviceId = ConnectionString.parse(connection_string).DeviceId;
var client = Client.fromConnectionString(connection_string, Protocol);

function printErrorFor(op) {
    return function printError(err) {
        if (err) console.log(op + ' error: ' + err.toString());
    };
}

var deviceMetaData = {
    'ObjectType': 'DeviceInfo',
    'IsSimulatedDevice': 0,
    'Version': '1.0',
    'DeviceProperties': {
        'DeviceID': deviceId,
        'HubEnabledState': 1
    }
};

var reportedProperties = {
    "Device": {
        "DeviceState": "normal",
        "Location": {
            "Latitude": 37.575869,
            "Longitude": 126.976859
        }
    },
    "Config": {
        "TemperatureMeanValue": 56.7,
        "TelemetryInterval": 45
    },
    "System": {
        "Manufacturer": "Contoso Inc.",
        "FirmwareVersion": "2.22",
        "InstalledRAM": "8 MB",
        "ModelNumber": "DB-14",
        "Platform": "Plat 9.75",
        "Processor": "i3-9",
        "SerialNumber": "SER99"
    },
    "Location": {
        "Latitude": 37.575869,
        "Longitude": 126.976859
    },
    "SupportedMethods": {
        "Reboot": "Reboot the device",
        "Reset" : "Reset the device",
        "InitiateFirmwareUpdate--FwPackageURI-string": "Updates device Firmware. Use parameter FwPackageURI to specifiy the URI of the firmware file"
    },
}

function onReboot(request, response) {
    // Implement actual logic here.
    console.log('Simulated reboot...');

    // Complete the response
    response.send(200, "Rebooting device", function(err) {
        if (!!err) {
            console.error('An error ocurred when sending a method response:\n' + err.toString());
        } else {
            console.log('Response to method \'' + request.methodName + '\' sent successfully.');
        }
    });
}


function onReset(request, response) {
    console.log('Simulator has been reset');

   }

function onInitiateFirmwareUpdate(request, response) {
    console.log('Simulated firmware update initiated, using: ' + request.payload.FwPackageURI);

    // Complete the response
    response.send(200, "Firmware update initiated", function(err) {
        if (!!err) {
            console.error('An error ocurred when sending a method response:\n' + err.toString());
        } else {
            console.log('Response to method \'' + request.methodName + '\' sent successfully.');
        }
    });

    // Add logic here to perform the firmware update asynchronously
}
client.open(function(err) {
    if (err) {
        printErrorFor('open')(err);
    } else {
        console.log('Sending device metadata:\n' + JSON.stringify(deviceMetaData));
        client.sendEvent(new Message(JSON.stringify(deviceMetaData)), printErrorFor('send metadata'));

        // Create device twin
        client.getTwin(function(err, twin) {
            if (err) {
                console.error('Could not get device twin');
            } else {
                console.log('Device twin created');

                twin.on('properties.desired', function(delta) {
                    console.log('Received new desired properties:');
                    console.log(JSON.stringify(delta));
                });

                // Send reported properties
                twin.properties.reported.update(reportedProperties, function(err) {
                    if (err) throw err;
                    console.log('twin state reported');
                });

                // Register handlers for direct methods
                client.onDeviceMethod('Reboot', onReboot);
                client.onDeviceMethod('InitiateFirmwareUpdate', onInitiateFirmwareUpdate);
                client.onDeviceMethod('Reset', onReset);
            }
        });

        // Start sending telemetry
        var sendInterval = setInterval(function() {
            var date = new Date().toISOString();
            var temperature = 15 + Math.random() * 35; // range: [10, 14]
            var humidity = 50 + Math.random() * 65;
            var pressure = 10 + Math.random() * 5;
            var windspeed = 0.5 + Math.random() * 45.5;
            var longitude = "37.575869";
            var latitude = "126.976859";
            var data = JSON.stringify({
                deviceId: deviceId,
                date: date,
                temperature: temperature,
                humidity: humidity,
                pressure: pressure,
                windspeed: windspeed,
                longitude: longitude,
                latitude: latitude
            });
            console.log('Sending device event data:\n' + data);
            client.sendEvent(new Message(data), printErrorFor('send event'));
        }, 1000);

        client.on('error', function(err) {
            printErrorFor('client')(err);
            if (sendInterval) clearInterval(sendInterval);
            client.close(printErrorFor('client.close'));
        });
    }
});