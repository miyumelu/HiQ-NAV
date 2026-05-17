# HiQ-Nav System

HiQ-Nav is a navigation and infotainment system for Euro Truck Simulator 2 (ATS has not been tested).

Designed for Windows tablets, it provides a multimedia system.

## Functions

**Multimedia**
Thanks to the built-in system, the device can be used as a sort of MP3 player, functioning independently of ETS2.

Designed to mimic the real thing, it is intended to automatically read and play content from USB drives and CDs.

I also plan to implement an audio streaming feature via Bluetooth or Wi-Fi (or both).

**Phone**
I plan to implement a hands-free system to ensure that my hands are truly free while driving.

Specifically, I intend to enable the ability to access contacts from a mobile phone—calling them or dialing a new number—without having to pick up the phone.

This feature will require Bluetooth connectivity on both devices.

Implementing this functionality will take a considerable amount of time.

**Navigation**
The map allows you to see your current location. Thanks to zoom options, you can zoom in on or out of the map.

Credit here goes to [ts-map](https://github.com/dariowouters/ts-map), who developed the map parser. It is possible to transmit live map data to the navigation system via TCP—though this comes at the cost of bandwidth usage.

Thanks to my "Compressed Mad Data" file format, it is possible to compress the ts-map tiles into a single file and access them within HiQ-Nav. This not only saves storage space but also improves performance.

**Speedometer**
Thanks to the built-in speedometer, you have access to a modern display that provides vital information regarding speed, RPM, oil temperature, fuel level, lights, turn signals, vehicle damage, and other details.

Through the use of built-in chimes and full-screen notifications, the system enables you to receive even more detailed information about your vehicle.

## Issues

I am aware of the current issue regarding the .NET Version and the possible issue of XWMS, resulting in a crash of the application. I am working on getting the .NET 6.0 version ready. For XMWS updates, see [XWMS](https://github.com/miyumelu/next-window-management-system)

The current version of the navigation has no UI. It is made for testing only. Currently, I am more focused on creating a touch and controller supported (XInput) interface.
