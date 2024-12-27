## Denon DN-2x00F USB Interface

This project allows you to use a Denon DN remote interface such as the RC35B for the Denon 2000F mk II as an input device to control playback in your projects.

The core of the project is **dn-interface** which is a cross-platform library that uses callbacks to trigger events in your code.

A demo application is included which uses the Un4seen libraries for playback.

The project has been upgraded to .NET8 and whilst the intention is cross-platform, only Windows is currently the only platform to be properly implemented.

## Prerequisites

The Un4seen managed libraries in **PlayerDemo** are used for playback. These can be found on NuGet.

Windows: Download the Un4seen bass library from [https://www.un4seen.com/bass.html](https://www.un4seen.com/bass.html). The **bass.dll** file should be copied to the same output folder as **dn-interface.dll**. Note that only Win32 x86 is currently supported.

The interface adapter will need to be built. Details can currently be found on [https://blog.petejefferson.co.uk/](https://blog.petejefferson.co.uk/).

## Building

Windows: It's recommended to open the **denon\_dn/vs/dn-interface.sln** solution and build the solution in Visual Studio. Copy the dn-interface.dll and bass.dll to the output folder of **PlayerDemo**.
