# HiQ-NAV

HiQ-NAV is a navigation system for Euro Truck Simulator 2.

## Functions

The map allows you to see your current location. Thanks to zoom options, you can zoom in on or out of the map.

Credit here goes to [ts-map](https://github.com/dariowouters/ts-map), who developed the map parser. It is possible to transmit live map data to the navigation system via TCP—though this comes at the cost of bandwidth usage.

Thanks to my "Compressed Mad Data" and "Universal Map Data" (in development) file format, it is possible to compress the ts-map tiles into a single file and access them within HiQ-Nav. This not only saves storage space but also improves performance.

## Using the System

Very simple. You can see everything in the left-hand column.

There will most likely be three versions of the map:

Business – approx. 300 MB (Recommended for low end tablets etc.)

Professional – approx. 3 GB (Recommended for DVD)

Premium – approx. 16 GB (Recommended for SD-Cards/USBs/Integrated drives)

RealFeel – N/A

I recommend that everyone parse the RealFeel map themselves using ts-map (I'll add the color palette at some point) and compress it via CMD/UMF, as it is very difficult to create, upload, and download.

I am currently thinking of a Blu-ray version of the UMD. It would hold more data and offer better zoom levels. However, it would be a distinct UMD version, developed and tested independently of the others.

The TruckersMP versions of the maps have been dropped because the process is currently too inconsistent regarding updates, and it isn't worth potentially creating a new map for every update. I might consider it as an option again in the future, but until then, things will remain as they are.

Map sizes may vary slightly depending on the game version, dlc and mods like TruckersMP. Maps may be missing certain DLC content. For those who have a CMD file ready, I kindly ask that you send it to me so that I can upload it to a server.

## Issues

- The current version of the navigation has a simple Mouse based UI. It is made for testing only. Currently, I am more focused on creating a touch and controller supported interface. Controller support is currently being added via "Local Keyboard Network." However, this is still in the design phase.

- The map view currently has some rendering issues. It performs more slowly than the bird's-eye view.

- Issues with managing the LKN module and the windows (focus problem)

## New formating

Alongside "Compressed Map Data," the new "Universal Map Data" (UMD) is now being tested in stages. This format aims to improve compression efficiency while providing high-quality data.

Consequently, all "Road TS-Map" cards will function only with the NT1000 series.

From now on, all UMD variants will run under the new "neXt Generation Experience" design, which features an improved UI compared to the original NT. UMD series 200 and up will soon support only Universal Map Data and newer "Hi-Drive" map materials. Depending on the specific model, DVD functionality may be included or enabled via ESCE.

Due to ongoing development, the current UND-100 variant still operates via CMD. The "Bird View" is currently available only for the UMD-100, the NT-1000 will not receive it.

I am currently testing a "Map Onboard" variant, also known as Interive (Internal Drive System). This is designed to allow maps to be loaded onto the navigation system via DVD, USB, or SD card (each system has its own unique copy-protection code). The new UND-100 variant now supports Interive. It includes new event handlers to activate this feature.

For the time being, this is only available via ESCE (External Software Coding Environment), as it is the first app capable of generating a "material security key." I should note that a DCC cable (Data Communication Cable) is required for this: essentially, it consists of two USB-to-RJ45 adapters connecting the two devices.

An app for creating map media is planned, though I am still a long way off from completing it. It is intended to enable the generation of a "material security key" as well.

## Divisioning

HiQ-NAV is now being continued as a standalone component.

It is being moved under the SAT-TEQ (short for Satellite Technologies Germany) branch to separate it from my desktop apps and user interfaces.

This repository will now contain only the data for the navigation system and NetService (provided the latter's functionality is in use).

The multimedia interface is being introduced in a new repository as "MEXIA" or H...k/Melu Multimedia Architecture (MMA) (the architecture for the entire system is currently under development).

## Used Project(s)

[ts-map](https://github.com/dariowouters/ts-map)

## Own APIs
[Compressed-Map-Data](https://github.com/miyumelu/compressed-map-data)

[Core Dictionary Module](https://github.com/miyumelu/core-dictionary-module)

[Power-Distribution-Management-System](https://github.com/miyumelu/Power-Distribution-Management-System)

[Touch Gesture Recognition System](https://github.com/miyumelu/Touch-Gesture-Recognition-System)

[neXt Motion Engine](https://github.com/miyumelu/next-motion-engine)

[neXt Voice Engine](https://github.com/miyumelu/next-voice-engine)
