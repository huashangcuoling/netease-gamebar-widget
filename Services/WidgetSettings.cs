using System;
using Windows.Data.Json;
using Windows.Storage;

namespace NeteaseGameBarWidget.Services
{
    /// <summary>
    /// Optional user overrides, read from LocalState\settings.json.
    ///
    /// Right now this only carries the taskbar height: the stock widget guesses it
    /// from the OS build, which is wrong if the user runs small taskbar buttons or
    /// a custom shell. Anything missing or out of range falls back to the guess.
    /// </summary>
    internal static class WidgetSettings
    {
        private const string FileName = "settings.json";
        private const string TaskbarHeightKey = "taskbarHeight";

        public static double? TaskbarHeight()
        {
            try
            {
                var root = Load();
                if (root == null)
                {
                    return null;
                }

                IJsonValue value;
                if (!root.TryGetValue(TaskbarHeightKey, out value) || value.ValueType != JsonValueType.Number)
                {
                    return null;
                }

                var height = value.GetNumber();
                return height >= 24 && height <= 96 ? height : (double?)null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static JsonObject Load()
        {
            var path = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, FileName);
            if (!System.IO.File.Exists(path))
            {
                return null;
            }

            JsonObject root;
            return JsonObject.TryParse(System.IO.File.ReadAllText(path), out root) ? root : null;
        }
    }
}
