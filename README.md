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

Thanks to my "Compressed Mad Data" file format, it is possible to compress the ts-map tiles into a single file and access them within HiQ-Nav. This not only saves storage space but also improves performance.

### Speedometer
Thanks to the built-in speedometer, you have access to a modern display that provides vital information regarding speed, RPM, oil temperature, fuel level, lights, turn signals, vehicle damage, and other details.

Through the use of built-in chimes and full-screen notifications, the system enables you to receive even more detailed information about your vehicle.

## Using the System

Currently, there is no direct information available regarding the specific method.

The current version can run on a miniDVD (like the SD card/USB version). This will be an exception, as the map data requires significantly more storage space.

A genuine DVD version of the system is planned.

This is intended to consist of two components: a "SOFTWARE UPDATE AND INSTALL DVD"—designed solely for initial setup(as miniDVD at 1,4GB max.) and a "NAVIGATION DATA DVD" (NOTE: DUAL LAYER/DVD9), which is required to use the navigation system offline.

There is also planned to be an SD card/USB version. Unlike the DVD version, this will not be split into two parts, but will instead exist as a single "NAVIGATION SYSTEM SOFTWARE" package.

I have opted against installing the map data directly onto the device's internal storage. This is because the data may be subject to frequent updates, and—depending on the specific map version—the storage requirements can range from as little as 1 MB to over 150 GB. (Currently, there is no viable workaround for this—aside from "live parsing" directly on the navigation unit itself; a method I still need to test to see how low-power devices, such as the Surface Go, handle it.) Storing the data internally could potentially cause the device to reach its TBW (Total Bytes Written) limit much sooner; replacing an SD card or USB drive is a far simpler solution.

There will be a "Create HiQ-Nav Disk" tool to simplify the process.

There will most likely be three versions of the map:

Business – approx. 150 MB
Professional – approx. 5–6 GB
Premium – approx. 40–50 GB
RealFeel – approx. 150–200 GB
(It is recommended to generate this yourself using ts-map and the CMD tool.)

Map sizes may vary depending on the game version. Maps may be missing certain DLC content. For those who have a CMD file ready, I kindly ask that you send it to me so that I can upload it to a server.

## Issues

I am aware of the current issue regarding the .NET Version and the possible issue of XWMS, resulting in a crash of the application. I am working on getting the .NET 6.0 version ready. For XMWS updates, see [XWMS](https://github.com/miyumelu/next-window-management-system)

The current version of the navigation has no UI. It is made for testing only. Currently, I am more focused on creating a touch and controller supported (XInput) interface.

## Used Project(s)

[ts-map](https://github.com/dariowouters/ts-map)
[32feet](https://github.com/inthehand/32feet)

## Own APIs
[XWMS](https://github.com/miyumelu/next-window-management-system)
[Compressed-Map-Data](https://github.com/miyumelu/compressed-map-data)
