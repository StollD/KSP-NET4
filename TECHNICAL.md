# How to use the new updated mono engine with Kerbal Space Program

**Note:** I have no idea how to apply this to a Mac, so this tutorial is Windows only. Adapting it to Linux shouldn't be too hard

* Install KSP 1.4. This is important, since the version 1.3 uses doesn't support the new runtime
* Download the Unity Installer through this link: https://beta.unity3d.com/download/02d73f71d3bd/UnityDownloadAssistant-2017.1.3p1.exe
* When the installer asks you what to install, select `Unity Editor` and `Windows Build Support`

* After finishing the download and the installation, you should open two explorer windows, one showing your KSP directory and one showing your Unity installation (`C:/Program Files/Unity` in most cases)
* Inside the KSP folder, navigate inside of the `KSP_x64_Data` folder.
* In the Unity folder navigate to `Editor/Data/PlaybackEngines/windowsstandalonesupport/Variations/win64_nondevelopment_mono/Data`. You will find a `MonoBleedingEdge` folder there. Copy it into `KSP_x64_Data`.
* Now, go to `Editor/Data/MonoBleedingEdge/lib/mono/4.5` inside of the Unity folder. You will see a lot of libraries there, you need to collect the following ones

```
Boo.Lang.dll
I18N.CJK.dll
I18N.dll
I18N.MidEast.dll
I18N.Other.dll
I18N.Rare.dll
I18N.West.dll
Mono.Posix.dll
Mono.Security.dll
mscorlib.dll
System.Configuration.dll
System.Core.dll
System.dll
System.Security.dll
System.Xml.dll
UnityScript.Lang.dll
```

* Copy the selected libraries into KSP_x64_Data/Managed. Replace them if they already exist there. **It is important that you don't update the Mono.Cecil library!**
* Now the final step: Create a file called `boot.config` inside of KSP_x64_Data, and put the following into it

```
scripting-runtime-version=latest
```

Now you should be able to run `KSP_x64.exe` and it should start to use the new and updated mono engine. However, there is a small bug in the part loader that we have to fix before it is actually useable. 

The fix is written in form of a KSP plugin, that replaces a method at runtime with a different one. The code is visible in this repository, simply go to releases, download the latest version for your KSP release and install it into GameData

