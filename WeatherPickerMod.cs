using DnWModLoader;
using DnWModLoader.Config;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using WeatherState = WalkNWashSceneState.WeatherState;

namespace WeatherPicker
{
    public enum WeatherChoice
    {
        LevelDefault,
        Sunny,
        Rainy,
        Night,
    }

    public sealed class WeatherPickerMod : Mod
    {
        public static WeatherPickerMod Instance { get; private set; }

        private ConfigEntry<WeatherChoice> _weather;
        // Level default weather state
        private WeatherState? _requested;

        public override void OnInitialize()
        {
            Instance = this;
            Config.DescribeSection("Weather", "Weather Picker", "Change the weather whenever you like!");
            _weather = Config.Bind("Weather", "Weather", WeatherChoice.LevelDefault, "Level Default.");
            _weather.Changed += _ => Apply();
            Logger.Info("Initialized. Weather: " + _weather.Value);
        }

        public override void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            _requested = HasSceneState() ? WalkNWashSceneState.GetWeatherState() : (WeatherState?)null;
            Apply();
        }

        internal WeatherState Resolve(WeatherState requested)
        {
            _requested = requested;
            WeatherState result = ToWeatherState(_weather.Value) ?? requested;
            if (result != requested) Logger.Debug("Requested " + requested + " weather; using " + result + ".");
            return result;
        }

        private void Apply()
        {
            // Main menu has no weather state
            if (!_requested.HasValue || !HasSceneState()) return;
            WalkNWashSceneState.SetWeatherState(_requested.Value);
        }

        private static WeatherState? ToWeatherState(WeatherChoice choice)
        {
            switch (choice)
            {
                case WeatherChoice.Sunny: return WeatherState.Sunny;
                case WeatherChoice.Rainy: return WeatherState.Rainy;
                case WeatherChoice.Night: return WeatherState.Night;
                default: return null;
            }
        }

        private static bool HasSceneState()
        {
            return Object.FindFirstObjectByType<WalkNWashSceneState>() != null;
        }
    }

    [HarmonyPatch(typeof(WalkNWashSceneState), nameof(WalkNWashSceneState.SetWeatherState))]
    internal static class WalkNWashSceneState_SetWeatherState_Patch
    {
        private static void Prefix(ref WeatherState state)
        {
            var mod = WeatherPickerMod.Instance;
            if (mod != null) state = mod.Resolve(state);
        }
    }
}
