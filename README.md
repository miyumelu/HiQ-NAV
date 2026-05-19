# HiQ-Nav System

HiQ-Nav is a navigation and infotainment system for Euro Truck Simulator 2 (ATS has not been tested).

Designed for Windows tablets, it provides a multimedia system.

## Functions

### Multimedia
Thanks to the built-in system, the device can be used as a sort of MP3 player, functioning independently of ETS2.

Designed to mimic the real thing, it is intended to automatically read and play content from USB drives and CDs.

I also plan to implement an audio streaming feature via Bluetooth or Wi-Fi (or both).

### Phone
I plan to implement a hands-free system to ensure that my hands are truly free while driving.

Specifically, I intend to enable the ability to access contacts from a mobile phone—calling them or dialing a new number—without having to pick up the phone.

This feature will require Bluetooth connectivity on both devices.

Implementing this functionality will take a considerable amount of time.

### Navigation
The map allows you to see your current location. Thanks to zoom options, you can zoom in on or out of the map.

Credit here goes to [ts-map](https://github.com/dariowouters/ts-map), who developed the map parser. It is possible to transmit live map data to the navigation system via TCP—though this comes at the cost of bandwidth usage.

Thanks to my "Compressed Mad Data" and "Universal Map Format" file format, it is possible to compress the ts-map tiles into a single file and access them within HiQ-Nav. This not only saves storage space but also improves performance.

### Speedometer
Thanks to the built-in speedometer, you have access to a modern display that provides vital information regarding speed, RPM, oil temperature, fuel level, lights, turn signals, vehicle damage, and other details.

Through the use of built-in chimes and full-screen notifications, the system enables you to receive even more detailed information about your vehicle.

## Using the System

Currently, there is no direct information available regarding the specific method.

The current version can run on a miniDVD (like the SD card/USB version). This will be an exception, as the map data requires significantly more storage space.

A genuine DVD version of the system is currently being worked on.

This is intended to consist of two components: a "SOFTWARE UPDATE AND INSTALL DVD"—designed solely for initial setup(as miniDVD at 1,4GB max.) and a "NAVIGATION MAP DATA DVD", which is required to use the navigation system offline.

There is also planned to be an SD card/USB version. Unlike the DVD version, this will not be split into two parts, but will instead exist as a single "NAVIGATION SYSTEM SOFTWARE" package, similar to the miniDVD.

I have opted against installing the map data directly onto the device's internal storage. This is because the data may be subject to frequent updates, and—depending on the specific map version—the storage requirements can range from as little as 300 MB to over 150 GB. (Currently, there is no viable workaround for this—aside from "live parsing" directly on the navigation unit itself; a method I still need to test to see how low-power devices, such as the Surface Go, handle it.) Storing the data internally could potentially cause the device to reach its TBW (Total Bytes Written) limit much sooner; replacing an SD card or USB drive is a far simpler solution.

There will be a "Create HiQ-Nav Disk" tool to simplify the process.

There will most likely be three versions of the map:

Business – approx. 300 MB (Recommended for low end tablets etc.)

Professional – approx. 4 GB (Recommended for DVDs)

Premium – approx. 16 GB (Recommended for SD-Cards/USBs/Integrated drives)

RealFeel – approx. 150 GB (Recommended testing purposes, or high end devices with a SSD)

I recommend that everyone parse the RealFeel map themselves using ts-map (I'll add the color palette at some point) and compress it via CMD/UMF, as it is very difficult to create, upload, and download.

TruckersMP versions of these maps are also provided. These may exhibit discrepancies in size.

Map sizes may vary slightly depending on the game version, dlc and mods like TruckersMP. Maps may be missing certain DLC content. For those who have a CMD file ready, I kindly ask that you send it to me so that I can upload it to a server.

## Issues

- I am aware of the current issue regarding the .NET Version and the possible issue of XWMS, resulting in a crash of the application. I am working on getting the .NET 6.0 version ready. For XMWS updates, see [XWMS](https://github.com/miyumelu/next-window-management-system)

- The current version of the navigation has no UI. It is made for testing only. Currently, I am more focused on creating a touch and controller supported (XInput) interface.

- After realizing that the UDP broadcast was generating excessive network traffic—leading to a slowdown of the entire system—I have implemented a handshake mechanism and a targeted UDP broadcast, rather than sending the data to every IP address. In doing so, I also introduced a transmission limit to restrict the number of broadcasts per second. However, this came at the expense of TCP performance, as TCP traffic is also subject to this same limit. Consequently, the map stream operates significantly more slowly; being asynchronous, it only transmits data when it finds an available slot (since parsing the data takes some time). To resolve this, I will most likely implement the TCP stream within its own dedicated class.

- For some reason, the system is having trouble displaying smaller zoom levels while in offline mode. I noticed this while testing with a DVD. I will check to see if this also affects SD cards and USB drives. The issue does not occur in online mode. Could this possibly be due to CPU throttling? I will look into implementing multithreading if the problem turns out to affect SD cards and USB drives as well.

## Used Project(s)

[ts-map](https://github.com/dariowouters/ts-map)

[32feet](https://github.com/inthehand/32feet)

## Own APIs
[Compressed-Map-Data](https://github.com/miyumelu/compressed-map-data)

[Core Dictionary Module](https://github.com/miyumelu/core-dictionary-module)

[Power-Distribution-Management-System](https://github.com/miyumelu/Power-Distribution-Management-System)

[Touch Gesture Recognition System](https://github.com/miyumelu/Touch-Gesture-Recognition-System)

[neXt Motion Engine](https://github.com/miyumelu/next-motion-engine)

[neXt-Window-Management-System](https://github.com/miyumelu/next-window-management-system)

[neXt Voice Engine](https://github.com/miyumelu/next-voice-engine)
