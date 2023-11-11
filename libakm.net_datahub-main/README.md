# AKM.NET library

This library serves as a .NET wrapper for AKM process. 
In this repository we also added two sample applications serwing as an example of client-server comminucation with AKM process. 
Unlike in the other AKM.NET project here the service application serves only as a data transmission hub and does not participare in AKM process. This means that the service doesn't decrypt the AKM frames and we have full end-to-end encryption provided between the Relationship Nodes.

## Getting Started

There are two solution files - one for sample client and one for sample service. They show practical use of AKM in network messages transmissions.
Simply use Visual Studio to open them and navigate between different components


### Prerequisites

AKM.NET library is using .NET Standard 2.1 and sample applications are written for .NET Core 3.1
External nuget packages are used for JSON serialization (Newtonsoft) or Logging (Serilog) - they will be restored automatically when solution is built.

C library with AKM implementation is required for proper AKM workflow. Once obtained please place it in "akmlib" folder next to the solution files. 
There is an automated build operation that will move the file to proper folder. If you wish to change the C library location please remember to update this line in sample project configuration:

```
<Exec Command="xcopy &quot;$(SolutionDir)\akmlib\*.dll&quot; &quot;$(TargetDir)&quot; /S /Y" />
```

### Usage

In order to start using AKM in network communication between nodes you will need an AKM Relationship group defined in your application settings file (most likely appsettings.json). Both sample applications have such file provided.

In order to get default relationship group config you can use 
```
AkmAppConfig akmConfig = AkmSetup.AkmAppCfg.FirstOrDefault().Value;
```
Later on it's up to you to provide an active Socket object for communication that will be used to populate available Sender objects collection

```
AkmSenderManager.AddSender(akmConfig.RelationshipId, akmConfig.SelfAddressValue, socket, _logger);
sender = AkmSenderManager.GetDefaultSender(cts.Token);
```

Later on you can send data by simply using
```
var message = $"Sample message number {++i}";
byte[] messageBytes = Encoding.UTF8.GetBytes(message);

sender.SendData(messageBytes, targetAddress);
```
This will make Sender object create a proper AKM frame based on provided configuration values, encrypt it using currently used key and send over using socket provided earlier.



To create a Receiver object you can do
```
var receiver = new Receiver(socket, cts.Token, _logger);
receiver.DataReceived += Receiver_DataReceived;
receiver.StartReceiving();
```
The DataReceived event is fired after receiver gets AKM frame from provided socket object, you can then use data from the AKM frame in any way you like. 
This sample logs the received message

```
if ((e?.FrameData?.Length ?? 0) > 0)
{
	var message = Encoding.UTF8.GetString(e.FrameData);
	_logger.LogDebug($"Decrypted Frame data: {message}");
}
else
{
	_logger.LogWarning("Data received event fired with empty frame.");
}
```


## Running the tests

Sample service application solution includes a project with set of unit tests - you can run them from within VS.


## Authors

* **Olympus Sky** - [OlympusSky](https://olympussky.com)

