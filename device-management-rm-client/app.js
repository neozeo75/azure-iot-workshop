'use strict';

var FileSystem = require('fs');
var Client = require('azure-iot-device').Client;
var Protocol = require('azure-iot-device-mqtt').Mqtt;
var Message = require('azure-iot-device').Message;
var connection_string = FileSystem.readFileSync('../device-management-rm-client-connectionstring.xld', 'utf8').trim().toString();
var client = Client.fromConnectionString(connection_string, Protocol);

var onReboot = function (request, response) {
    response.send(200, 'Device Reboot Started...', function (err) {
        if (!err) {
            console.error('An error occured while sending a method response:\n' + err.toString());
        } else {
            console.log('Response to method \'' + request.methodName + '\' sent successfully.');
        }
    });

    // Report the reboot before the physical restart
    var date = new Date();
    var patch = {
        iothubDM: {
            reboot: {
                lastReboot: date.toISOString()
            }
        }
    };

    // Get device Twin
    client.getTwin(function (err, twin) {
        if (err) {
            console.error('Could not get the device twin');
        } else {
            console.log('Device twin acquired');
            twin.properties.reported.update(patch, function (err) {
                if (err) throw err;
                console.log('Device reboot twin state reported');
            });
        }
    });
    console.log('Rebooting!');
};
client.open(function (err) {
    if (err) {
        console.error('Could not open IotHub client');
    } else {
        console.log('Client opened.  Waiting for reboot method.');
        client.open(connectCallback);
        client.sendEvent(new Message(JSON.stringify(deviceMetaData)), printErrorFor('send metadata'));
        client.onDeviceMethod('reboot', onReboot);
    }
});

var connectCallback = function (err) {
    if (err) {
        console.error('Could not connect as IoT device: ' + err.message);
    } else {
        console.log('Device client connected');
        client.on('message', function (msg) {
            console.log('Id: ' + msg.messageId + ' Body: ' + msg.data);
            client.complete(msg, printResultFor('completed'));
        });

        var sendInterval = setInterval(function () {

            var deviceId = 'device-001';
            var date = new Date().toISOString();

            var temperature = 15 + Math.random() * 35; // range: [10, 14]
            var humidity = 50 + Math.random() * 65;
            var pressure = 10 + Math.random() * 5;

            var data = JSON.stringify({ deviceId: deviceId, date: date, temperature: temperature, humidity: humidity, pressure: pressure });
            var message = new Message(data);

            message.properties.add('key', 'value');
            console.log('Sending message: ' + message.getData());
            client.sendEvent(message, printResultFor('send'));
        }, 5000);

        client.on('error', function (err) {
            console.error(err.message);
        });

        client.on('disconnect', function () {
            clearInterval(sendInterval);
            client.removeAllListeners();
            client.open(connectCallback);
        });
    }
};

var deviceMetaData = {
  'ObjectType': 'DeviceInfo',
  'IsSimulatedDevice': 0,
  'Version': '1.0',
  'DeviceProperties': {
    'DeviceID': 'device-rm-001',
    'HubEnabledState': 1,
    'CreatedTime': '2015-09-21T20:28:55.5448990Z',
    'DeviceState': 'normal',
    'UpdatedTime': null,
    'Manufacturer': 'Contoso Inc.',
    'ModelNumber': 'MD-909',
    'SerialNumber': 'SER9090',
    'FirmwareVersion': '1.10',
    'Platform': 'node.js',
    'Processor': 'ARM',
    'InstalledRAM': '64 MB',
    'Latitude': 47.617025,
    'Longitude': -122.191285
  },
  'Commands': [{
    'Name': 'SetTemperature',
    'Parameters': [{
      'Name': 'Temperature',
      'Type': 'double'
    }]
  },
    {
      'Name': 'SetHumidity',
      'Parameters': [{
        'Name': 'Humidity',
        'Type': 'double'
      }]
    }]
};

function printResultFor(op) {
    return function printResult(err, res) {
        if (err) console.log(op + ' error: ' + err.toString());
        if (res) console.log(op + ' status: ' + res.constructor.name);
    };
}

// Helper function to print results for an operation
function printErrorFor(op) {
  return function printError(err) {
    if (err) console.log(op + ' error: ' + err.toString());
  };
}
