# Installation

### Using OpenUPM (For Unity 2018.3 or later)

This package is available on [OpenUPM](https://openupm.com).  
You can install it via [openupm-cli](https://github.com/openupm/openupm-cli).

```
openupm add com.unitypackage.vibration
```

### Using Unity Package Manager (For Unity 2018.3 or later)

Find the manifest.json file in the Packages folder of your project and edit it to look like this:

open Packages/manifest.json

```
{
  "dependencies": {
    "com.unitypackage.vibration":"https://github.com/UnityPackage/vibration.git",
    ...
  },
}
```


# Quick start
```csharp

// Ios 需要在启动时候 初始化
MMVibrationManager.iOSInitializeHaptics();

// 指定类型震动
MMVibrationManager.Haptic(HapticTypes.*);

enum HapticTypes{
	Selection,
    Success,
    Warning,
    Failure,
    LightImpact,
    MediumImpact,
    HeavyImpact,
}

// Update 1次调用1次 或者自定义重复调用持续震动
Handheld.Vibrate();

```


# link
- [Unity Custom Packages](https://docs.unity3d.com/Manual/CustomPackages.html)


