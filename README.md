# HouzLinc

This project builds an application that can program an Insteon network of devices. It leverages publicly available documentation on programming the Insteon hub and devices. The concept is similar to the old Houselinc program for Windows that Insteon distributed some years ago.

I have a network of over 100 devices and more than 90 scenes in my house, which I configured back in 2013 using Houselinc. Transitioning to the new Insteon Director app (or its previous incarnation) was not straightforward, prompting me to develop my own app. I called it HouzLinc.

HouzLinc currently runs on Windows and Android. Porting it to iOS should be relatively straightforward (see below for why). On Windows HouzLinc takes advantage of the size of a laptop/desktop screen. On a phone, the UI works more like a mobile app.

Houzlinc does not require a service to run. It can fully run on a local machine connected to the same local network as the Insteon Hub. The house configuration or model representing the programming of the devices is stored in a single file that can reside on your local drive or on an online file service like Microsoft OneDrive, Google Drive, Dropbox, etc. (Only OneDrive is supported at this time). This ensures that your data remains under your control, that is remains private, always accessible, and can be passed on to new house owners.

Multiple instances of this app can run concurrently on multiple devices and share the same configuration file, allowing you to choose what device works best for the task at hand.

HouzLinc also offers the following:
- Updates to the configuration of scenes and devices are performed asynchronously, allowing users to continue making changes while previous updates are being applied to the devices.
- In addition to devices and scenes, HouzLinc shows the All-Links between devices to facilitate investigating issues and fixing the links as needed. 
- HouzLinc includes a Console view which can be used to send commands directly to the hub or to individual devices, providing further investigation capabilities.
- Some batch operations are provided such as removing or replacing a device, duplicating a device configuration to another, cleaning up after device removal, and so on. I expect more of these handy tasks to be added over time as the need arises, for example, creating an off scene for all devices in a scene or multiple scenes. 
- This project is open source with an Apache license. It is in development and as such, features bugs and limitations. Contributions to fix issues and enhance or add functionality are welcome!

## Architecture
This app is written as a C# application for the [Uno Platform](https://platform.uno/). Uno Platform allows to create single-codebase, cross-platform applications that can run on iOS, Android, Web, macOS, Linux and Windows. It achieves this by implementing the Windows App SDK on other platforms. Uno apps use the Windows App SDK on Windows, and Uno's implementation of the same functionality on other platforms. All code is in C# and the UI is in XAML.

This promotes a Windows first development approach, where development can occur on Windows in Visual Studio, using C# and XAML, and the code can be built and distributed to all the platforms supported by Uno. I currently have it building and running acceptably on Android. iOS is next!

### Layering
![](architecture.jpg)
**Command Layer**: (namespace: `Insteon.Commands`) consists in an implementation of Insteon Hub and Device commands, using the Insteon Hub http interface. It abstracts the underlying details of the protocol and exposes a set of easily consumable command classes.

**Physical Device Drivers**: (`Insteon.Driver`) contains drivers for the physical devices on the Insteon Network. They expose the functionality supported by a class of devices, e.g., Switchlinc or Keypadlinc, under a common API. There is an implementation of the API for the Insteon Hub (model 2242), as well as a generic device implementation supporting SwitchLinc, OutletLinc, I/OLinc, In-LineLinc, and others. A mode specific implementation handles KeypadLinc with 6 or 8 buttons and another handles RemoteLinc (Mini-Remote) with 4 channels.

**Model**: (`Insteon.Model`) this is the persisted model of devices, channels, scenes, etc. It is also called "House Configuration". It is persisted as an XML file. Changes to the model trigger background synchronization with the actualy Insteon devices on the Insteon network using the drivers layer. The state is persisted in such a manner that even if the app is closed and later restarted, synchronization will resume until complete.

**Serialization**: (`Insteon.Serialization`) this reads and writes the model to a file. The current format is XML-based, more or less compatible with the old Houselinc.xml format. The model is converted to/from a different data structure for persistence, making it relatively easy to add new persistence formats in the future, such as JSON-based for example.

In addition to writing to and reading to/from a local file, Houzlinc can also work with a file on a personal Microsoft OneDrive. Currently the file name and location is fixed, in the `Apps\HouzLinc` folder off the root of your OneDrive.

An abstraction of Storage Providers (`StorageProvider` and derived classes) should make supporting other storage services such Goodle Drive or Dropbox relatively easy in the future.

**Model Persistance**: (`Insteon.Model`) When modified, the model generates change deltas that can be observed by either the View Model layer or by the persistence subsystem in the Model layer. The View Model layer uses these deltas to update the UI. The persistence subsystem uses them to apply changes to the persisted file and immediately release it for other instances of the app to persist their changes. This allows to run multiple instances of the app on different devices and have them share the same configuration file.

**View** Model: (`ViewModel.*`) the view model does what it does in most MVVM applications, i.e., maintain a set of view oriented data on the model that is used by the UI layer. The view model gets notified of changes to the model as observer and updates the UI using XAML databinding. It also receives user actions from the UI and applies them to the model.

**UI (View)**: (`Houzlinc.*`) written in XAML, the UX uses regular XAML C# databinding to dynamically keep up to date with the View Model and express the user changes to the View Model and Model layers. 

## Overview of the User Interface

## Getting Started
I am currently working on deploying the first version of this app to the relevant stores for public consumption. In the meantime, if you want to try out HouzLinc, you will need to build it yourself. You can build it on a Windows machine using Visual Studio 2022, and then either deploy it locally on that machine, or create a MSIX package and install it on any Windows machine with developer mode turned on. You can also build it for Android and deploy it to a phone or an emulator using Visual Studio 2022.

### Repository and toolchain
First, you need to install the development environment, including the Uno Platform. Refer to the [Uno Platform documentation](https://platform.uno/docs/articles/getting-started.html) to get started with Uno Platform.

I used Visual Studio 2022, version 17.8.0 and above. You can download Visual Studio Community from [here](https://visualstudio.microsoft.com/vs/community/). In theory, you should also be able to use VSCode if you like that IDE better, but I have not had a chance to try it yet.

Once your development environment is set up, clone the HouzLinc repository from:
```
git clone https://github.com/christianfo/houzlinc.git
```
Look for Visual Studio solution file `HouzLinc.sln` at the root of the repo.

### App Configuration
#### Enabling OneDrive sign-in
HouzLinc allows you to store the house configuration on OneDrive. If you are building it yourself and want that functionality, you will need to register an application with Microsoft Entra ID to obtain a Client Id (as well as a redirect URI on certain platform). This is necessary to allow users to sign-in to OneDrive to let Houzlinc access the configuration file. Go to the [Azure portal](https://portal.azure.com/) and [register a new app](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app?tabs=certificate) for Microsoft Entra ID. If you already have an application registered with Microsoft Entra, you can use that one instead. You will need the client Id and the redirect URI for a mobile app (Android, iOS).

Create a file named appsettings.json in the project HouzLinc folder:
```
<your repo root>/HouzLinc/appsettings.json
```
Containing the following section:
```
"Msal": {
    "ClientId": "<Your MSAL client Id>",
    "AndroidRedirectURI": <Your Android Redirect URI>"
}
```
Where you enter your client Id and redirect URI for Android obtain from your Azure app registration.

Note that HouzLinc only asks for access to the `App\HouzLinc` folder in your OneDrive (scope: `Files.ReadWrite.AppFolder`). It does not have permission to read or write anything outside of that folder. 

If you do not want to use OneDrive, either do not create appsettings.json or leave out the Msal section.

The appsettings.json file conatins application configuration settings that are specific to you as a developer. Over time, more settings will likely be added. Note that this file is not part of the repository since it contains custom settings which should remain private to you, and in certain cases should not be exposed (such as your Microsoft Entra Client Id).

### Building and Running the App for Development on Windows
Building in Visual Studio is easy: select a configuration (Debug or Release), select `Any CPU` as the architecture and `HouzLinc (WinAppSDK Packaged)` as the profile. Press F5 to build and debug the app, Ctrl F5 to run without the debugger. Once built, Visual Studio will install the package locally and run it. You can also run the latest installed build from the Start menu.

It is also possible to build the app from the command line. The following will build the app and install it on the local machine. From the HouzLinc folder at the top of the repo (where the app project file `HouzLinc.csproj` is) run the following:
```
dotnet build --framework net8.0-windows10.0.19041 -c Debug|Release
```
In theory, you should be able to run the app from the command line as well, but some package identity issue is currently making the code fault immediately.
```
dotnet run --framework net8.0-windows10.0.19041 -c Debug|Release
```

### Building and Running the App for Development on Android
Again, building in Visual Studio is easy: set the configuration, select `Any CPU` for the architecture and select 

### Building an Unsigned App Package for Sideloading on Windows
Using Visual Studio `msbuild`, you can create an MSIX installer package that can be sideloaded on any Windows machine with developer mode turned on. Proceed as follows (see [here](https://platform.uno/docs/articles/uno-publishing-windows-packaged-unsigned.html) for more details):
- In a Developer Powershell window (either View|Terminal or Tools|Command Line|Developer Powershell), navigate to the `HouzLinc` folder where the `HouzLinc.csproj` project file is located.
- Run the following command to restore the correct dependency packages:
```
msbuild /r /t:Restore /p:Configuration=Release
```
- Then run the following to build the package:
```
msbuild /p:TargetFramework=net8.0-windows10.0.19041 /p:Configuration=Release /p:Platform=x64 /p:PublishUnsignedPackage=true /p:AppxPackageDir="<output directory>"
```
This creates an `.msix` file in `<output directory>`, for example `c:\temp\output`, that you can install on your machine or any other machine with developer mode turned on. To install, open a Powershell window running as administrator and run the following:
```
Add-AppPackage -AllowUnsigned -path "<path to msix file>"
```
### Building a signed App Package
TBD

### Limitations
- Only i2 devices and up are supported (Insteon Engine version 2). This can be checked with GetInsteonEngineVersion command in Console.
- I have not tested with i3 devices yet.
- Timers are not supported for lack of public documentation. Will try to reverse engineer, but would welcome Insteon sharing the protocol.
- To make the Android emulator work acceptably [see here](https://stackoverflow.com/questions/69134922/google-chrome-browser-in-android-12-emulator-doesnt-load-any-webpages-internet#:~:text=It%27s%20caused%20by%20vulkan.%20To%20fix%20it%2C%20you,exist%20already%29%3A%20Vulkan%20%3D%20off%20GLDirectMem%20%3D%20on)
- Detaching the load on a keypadlinc is not supported. We could support it but it is very limited in that there is no way to control the load from one of the buttons on the keypadlinc itself in that configuration. So I am not sure if it's worth the effort. If you have a use case for this, let me know.

### What I would like to build next
- Move the app to Microsoft Store and Google Play Store.
- Support for iOS and the Apple Store.
- Support for more devices, e.g., i3 devices, and more device types.
- Support for Google Drive and DropBox as places to store the houzlinc configuration file.
- Support for schedules.
- One click batch operations, e.g., create an All-Off scene for all devices in one scene or multiple scenes, or an All-Off-Button scene that turns off the light on an All-Off button when a device on the scene is activated.

### Contribution
You are welcome to contribute changes by submitting Pull Requests to the 'main' branch. For the time being, I'll be the sole approver of changes. I am in the process of building a pipeline for building and validating changes. At this time, that process is manual, and has to be done by me.
