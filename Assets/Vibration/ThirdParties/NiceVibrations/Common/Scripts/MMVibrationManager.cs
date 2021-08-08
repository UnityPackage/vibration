using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

#if UNITY_IOS 
	using UnityEngine.iOS;
#endif

namespace MoreMountains.NiceVibrations
{
	public enum HapticTypes { Selection, Success, Warning, Failure, LightImpact, MediumImpact, HeavyImpact }

	/// <summary>
	/// This class will allow you to trigger vibrations and haptic feedbacks on both iOS and Android, 
	/// or on each specific platform independently.
	/// 
	/// For haptics patterns, it takes inspiration from the iOS guidelines : 
	/// https://developer.apple.com/ios/human-interface-guidelines/user-interaction/feedback
	/// Of course the iOS haptics are called directly as they are, and they're crudely reproduced on Android.
	/// Feel free to tweak the patterns or create your own.
	/// 
	/// Here's a brief overview of the patterns :
	/// 
	/// - selection : light
	/// - success : light / heavy
	/// - warning : heavy / medium
	/// - failure : medium / medium / heavy / light
	/// - light 
	/// - medium 
	/// - heavy  
	/// 
	/// </summary>
	public static class MMVibrationManager 
	{
		// INTERFACE ---------------------------------------------------------------------------------------------------------

		public static long LightDuration = 20;
		public static long MediumDuration = 40;
		public static long HeavyDuration = 80;
		public static int LightAmplitude = 40;
		public static int MediumAmplitude = 120;
		public static int HeavyAmplitude = 255;
		private static int _sdkVersion = -1;
		private static long[] _successPattern = { 0, LightDuration, LightDuration, HeavyDuration};
		private static int[] _successPatternAmplitude = { 0, LightAmplitude, 0, HeavyAmplitude};
		private static long[] _warningPattern = { 0, HeavyDuration, LightDuration, MediumDuration};
		private static int[] _warningPatternAmplitude = { 0, HeavyAmplitude, 0, MediumAmplitude};
		private static long[] _failurePattern = { 0, MediumDuration, LightDuration, MediumDuration, LightDuration, HeavyDuration, LightDuration, LightDuration};
		private static int[] _failurePatternAmplitude = { 0, MediumAmplitude, 0, MediumAmplitude, 0, HeavyAmplitude, 0, LightAmplitude};

		/// <summary>
		/// Returns true if the current platform is Android, false otherwise.
		/// </summary>
		public static bool Android()
		{
			#if UNITY_ANDROID && !UNITY_EDITOR
				return true;
			#else
				return false;
			#endif
		}

		/// <summary>
		/// Returns true if the current platform is iOS, false otherwise
		/// </summary>
		/// <returns><c>true</c>, if O was ied, <c>false</c> otherwise.</returns>
		public static bool iOS()
		{
			#if UNITY_IOS && !UNITY_EDITOR
				return true;
			#else
				return false;
			#endif
		}

		/// <summary>
		/// Triggers a simple vibration
		/// </summary>
		public static void Vibrate()
		{
			if (Android ())
			{
				AndroidVibrate (MediumDuration);
			} 
			else if (iOS ())
			{
				iOSTriggerHaptics (HapticTypes.MediumImpact);
			} 
		}

		/// <summary>
		/// Triggers a haptic feedback of the specified type
		/// </summary>
		/// <param name="type">Type.</param>
		public static void Haptic(HapticTypes type)
		{
			if (Android ())
			{
				switch (type)
				{
					case HapticTypes.Selection:
						AndroidVibrate (LightDuration, LightAmplitude);
						break;

					case HapticTypes.Success:
						AndroidVibrate(_successPattern, _successPatternAmplitude, -1);
						break;

					case HapticTypes.Warning:
						AndroidVibrate(_warningPattern, _warningPatternAmplitude, -1);
						break;

					case HapticTypes.Failure:
						AndroidVibrate(_failurePattern, _failurePatternAmplitude, -1);
						break;

					case HapticTypes.LightImpact:
						AndroidVibrate (LightDuration, LightAmplitude);
						break;

					case HapticTypes.MediumImpact:
						AndroidVibrate (MediumDuration, MediumAmplitude);
						break;

					case HapticTypes.HeavyImpact:
						AndroidVibrate (HeavyDuration, HeavyAmplitude);
						break;
				}
			} 
			else if (iOS ())
			{
				iOSTriggerHaptics (type);
			} 
		}

		// INTERFACE END ---------------------------------------------------------------------------------------------------------



		// Android ---------------------------------------------------------------------------------------------------------

		// Android Vibration reference can be found at :
		// https://developer.android.com/reference/android/os/Vibrator.html
		// And there starting v26, with support for amplitude :
		// https://developer.android.com/reference/android/os/VibrationEffect.html

		#if UNITY_ANDROID && !UNITY_EDITOR
			private static AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			private static AndroidJavaObject CurrentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			private static AndroidJavaObject AndroidVibrator = CurrentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
			private static AndroidJavaClass VibrationEffectClass;
			private static AndroidJavaObject VibrationEffect;
			private static int DefaultAmplitude;
		#else
			private static AndroidJavaClass UnityPlayer;
			private static AndroidJavaObject CurrentActivity;
			private static AndroidJavaObject AndroidVibrator = null;
			private static AndroidJavaClass VibrationEffectClass = null;
			private static AndroidJavaObject VibrationEffect;
			private static int DefaultAmplitude;
		#endif

		/// <summary>
		/// Requests a default vibration on Android, for the specified duration, in milliseconds
		/// </summary>
		/// <param name="milliseconds">Milliseconds.</param>
		public static void AndroidVibrate(long milliseconds)
		{
			if (!Android ()) { return; }
			AndroidVibrator.Call("vibrate", milliseconds);
		}

		/// <summary>
		/// Requests a vibration of the specified amplitude and duration. If amplitude is not supported by the device's SDK, a default vibration will be requested
		/// </summary>
		/// <param name="milliseconds">Milliseconds.</param>
		/// <param name="amplitude">Amplitude.</param>
		public static void AndroidVibrate(long milliseconds, int amplitude)
		{
			if (!Android ()) { return; }
			// amplitude is only supported 
			if ((AndroidSDKVersion() < 26)) 
			{ 
				AndroidVibrate (milliseconds); 
			}
			else
			{
				VibrationEffectClassInitialization ();
				VibrationEffect = VibrationEffectClass.CallStatic<AndroidJavaObject> ("createOneShot", new object[] { milliseconds,	amplitude });
				AndroidVibrator.Call ("vibrate", VibrationEffect);
			}
		}

		// Requests a vibration on Android for the specified pattern and optional repeat
		// Straight out of the Android documentation :
		// Pass in an array of ints that are the durations for which to turn on or off the vibrator in milliseconds. 
		// The first value indicates the number of milliseconds to wait before turning the vibrator on. 
		// The next value indicates the number of milliseconds for which to keep the vibrator on before turning it off. 
		// Subsequent values alternate between durations in milliseconds to turn the vibrator off or to turn the vibrator on.
		// repeat:  the index into pattern at which to repeat, or -1 if you don't want to repeat.
		public static void AndroidVibrate(long[] pattern, int repeat)
		{
			if (!Android ()) { return; }
			if ((AndroidSDKVersion () < 26))
			{ 
				AndroidVibrator.Call ("vibrate", pattern, repeat);
			}
			else
			{
				VibrationEffectClassInitialization ();
				VibrationEffect = VibrationEffectClass.CallStatic<AndroidJavaObject> ("createWaveform", new object[] { pattern,	repeat });
				AndroidVibrator.Call ("vibrate", VibrationEffect);
			}
		}

		/// <summary>
		/// Requests a vibration on Android for the specified pattern, amplitude and optional repeat
		/// </summary>
		/// <param name="pattern">Pattern.</param>
		/// <param name="amplitudes">Amplitudes.</param>
		/// <param name="repeat">Repeat.</param>
		public static void AndroidVibrate(long[] pattern, int[] amplitudes, int repeat )
		{
			if (!Android ()) { return; }
			if ((AndroidSDKVersion () < 26))
			{ 
				AndroidVibrator.Call ("vibrate", pattern, repeat);
			}
			else
			{
				VibrationEffectClassInitialization ();
				VibrationEffect = VibrationEffectClass.CallStatic<AndroidJavaObject> ("createWaveform", new object[] { pattern,	amplitudes, repeat });
				AndroidVibrator.Call ("vibrate", VibrationEffect);
			}
		}

		/// <summary>
		/// Stops all Android vibrations that may be active
		/// </summary>
		public static void AndroidCancelVibrations()
		{
			if (!Android ()) { return; }
			AndroidVibrator.Call("cancel");
		}

		/// <summary>
		/// Initializes the VibrationEffectClass if needed.
		/// </summary>
		private static void VibrationEffectClassInitialization ()
		{
			if (VibrationEffectClass == null) { VibrationEffectClass = new AndroidJavaClass ("android.os.VibrationEffect"); }	
		}

		/// <summary>
		/// Returns the current Android SDK version as an int
		/// </summary>
		/// <returns>The SDK version.</returns>
		public static int AndroidSDKVersion() 
		{
			if (_sdkVersion == -1)
			{
				int apiLevel = int.Parse (SystemInfo.operatingSystem.Substring(SystemInfo.operatingSystem.IndexOf("-") + 1, 3));
				_sdkVersion = apiLevel;
				return apiLevel;	
			}
			else
			{
				return _sdkVersion;
			}
		}
			
		// Android End ---------------------------------------------------------------------------------------------------------

		// iOS ----------------------------------------------------------------------------------------------------------------

		// The following will only work if the iOSHapticInterface.m file is in a Plugins folder in your project.
		// It's a pretty straightforward implementation of iOS's UIFeedbackGenerator's methods.
		// You can learn more about them there : https://developer.apple.com/documentation/uikit/uifeedbackgenerator

		#if UNITY_IOS && !UNITY_EDITOR
			[DllImport ("__Internal")]
			private static extern void InstantiateFeedbackGenerators();
			[DllImport ("__Internal")]
			private static extern void ReleaseFeedbackGenerators();
			[DllImport ("__Internal")]
			private static extern void SelectionHaptic();
			[DllImport ("__Internal")]
			private static extern void SuccessHaptic();
			[DllImport ("__Internal")]
			private static extern void WarningHaptic();
			[DllImport ("__Internal")]
			private static extern void FailureHaptic();
			[DllImport ("__Internal")]
			private static extern void LightImpactHaptic();
			[DllImport ("__Internal")]
			private static extern void MediumImpactHaptic();
			[DllImport ("__Internal")]
			private static extern void HeavyImpactHaptic();
		#else
			private static void InstantiateFeedbackGenerators() {}
			private static void ReleaseFeedbackGenerators() {}
			private static void SelectionHaptic() {}
			private static void SuccessHaptic() {}
			private static void WarningHaptic() {}
			private static void FailureHaptic() {}
			private static void LightImpactHaptic() {}
			private static void MediumImpactHaptic() {}
			private static void HeavyImpactHaptic() {}
		#endif
		private static bool iOSHapticsInitialized = false;

		/// <summary>
		/// Call this method to initialize the haptics. If you forget to do it, Nice Vibrations will do it for you the first time you
		/// call iOSTriggerHaptics. It's better if you do it though.
		/// </summary>
		public static void iOSInitializeHaptics()
		{
			if (!iOS ()) { return; }
			InstantiateFeedbackGenerators ();
			iOSHapticsInitialized = true;
		}

		/// <summary>
		/// Releases the feedback generators, usually you'll want to call this at OnDisable(); or anytime you know you won't need 
		/// vibrations anymore.
		/// </summary>
		public static void iOSReleaseHaptics ()
		{
			if (!iOS ()) { return; }
			ReleaseFeedbackGenerators ();
		}

		/// <summary>
		/// This methods tests the current device generation against a list of devices that don't support haptics, and returns true if haptics are supported, false otherwise.
		/// </summary>
		/// <returns><c>true</c>, if supported was hapticsed, <c>false</c> otherwise.</returns>
		public static bool HapticsSupported()
		{
			bool hapticsSupported = false;
			#if UNITY_IOS 
			DeviceGeneration generation = Device.generation;
			if ((generation == DeviceGeneration.iPhone3G)
			|| (generation == DeviceGeneration.iPhone3GS)
			|| (generation == DeviceGeneration.iPodTouch1Gen)
			|| (generation == DeviceGeneration.iPodTouch2Gen)
			|| (generation == DeviceGeneration.iPodTouch3Gen)
			|| (generation == DeviceGeneration.iPodTouch4Gen)
			|| (generation == DeviceGeneration.iPhone4)
			|| (generation == DeviceGeneration.iPhone4S)
			|| (generation == DeviceGeneration.iPhone5)
			|| (generation == DeviceGeneration.iPhone5C)
			|| (generation == DeviceGeneration.iPhone5S)
			|| (generation == DeviceGeneration.iPhone6)
			|| (generation == DeviceGeneration.iPhone6Plus)
			|| (generation == DeviceGeneration.iPhone6S)
			|| (generation == DeviceGeneration.iPhone6SPlus))
			{
			hapticsSupported = false;
			}
			else
			{
			hapticsSupported = true;
			}
			#endif

			return hapticsSupported;
		}
	
		/// <summary>
		/// iOS only : triggers a haptic feedback of the specified type
		/// </summary>
		/// <param name="type">Type.</param>
		public static void iOSTriggerHaptics(HapticTypes type)
		{
			if (!iOS ()) { return; }

			if (!iOSHapticsInitialized)
			{
				iOSInitializeHaptics ();
			}

			// this will trigger a standard vibration on all the iOS devices that don't support haptic feedback

			if (HapticsSupported())
			{
				switch (type)
				{
					case HapticTypes.Selection:
						SelectionHaptic ();
						break;

					case HapticTypes.Success:
						SuccessHaptic ();
						break;

					case HapticTypes.Warning:
						WarningHaptic ();
						break;

					case HapticTypes.Failure:
						FailureHaptic ();
						break;

					case HapticTypes.LightImpact:
						LightImpactHaptic ();
						break;

					case HapticTypes.MediumImpact:
						MediumImpactHaptic ();
						break;

					case HapticTypes.HeavyImpact:
						HeavyImpactHaptic ();
						break;
				}
			}
			else
			{
				#if UNITY_IOS 
					Handheld.Vibrate();
				#endif
			}
		}

		/// <summary>
		/// Returns a string containing iOS SDK informations
		/// </summary>
		/// <returns>The OSSDK version.</returns>
		public static string iOSSDKVersion() 
		{
			#if UNITY_IOS && !UNITY_EDITOR
				return Device.systemVersion;
			#else
				return null;
			#endif
		}

		// iOS End ----------------------------------------------------------------------------------------------------------------
	}
}