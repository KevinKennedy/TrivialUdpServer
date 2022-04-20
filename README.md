# TrivialUdpServer
Trivial UDP server in a HoloLens Unity app

## Building
1) Clone the repo to your local machine
2) Use the [Mixed Reality Feature Tool](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/welcome-to-mr-feature-tool) to add the following packages:
    ![packages](/docs/MRTKPackages.png)
3) Open in Unity and go for it :-)

## Running
1) Run the app in the Editor or on the HoloLens
2) You'll see your IP address
3) Use some utility to send UDP messages to that IP address and port

Linux NC command:
![nc command](/docs/NC_Example.png)

What you see on the device or in the Unity Editor:
![app_display](/docs/HoloLensC_Example.png)

## Sample code
The interesting sample code is in the [UdpServer.cpp](/Assets/Scripts/UdpServer.cs) file.  It's just a MonoObject that opens up a UDP port and echos what it receives to a TextMesh instance and the Unity player log

