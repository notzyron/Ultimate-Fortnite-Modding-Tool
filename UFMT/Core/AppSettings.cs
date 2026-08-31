#pragma warning disable
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Diagnostics;
using UFMT;
using UFMT.Core;

public static class AppSettings
{
    internal static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"UFMT");

    private static readonly string FilePath = Path.Combine(FolderPath, "settings.json");
    private static Dictionary<string, object> _settingsCache = new();

    static AppSettings()
    {
        try
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
                Debug.WriteLine($"[AppSettings] Created folder at: {FolderPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppSettings] Failed to create folder: {ex.Message}");
        }

        Load();
    }

    public static T GetValue<T>(string key, T defaultValue = default)
    {
        if (_settingsCache.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }

            if (value is JsonElement element)
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            }

            try
            {
                string json = JsonSerializer.Serialize(value);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    Log.Error("Failed to convert a value from a .json!");
                    return defaultValue;
                }
            }
        }
        return defaultValue;
    }

    public static void SetValue(string key, object value)
    {
        _settingsCache[key] = value;
        Save();
    }

    private static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                _settingsCache = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
                Debug.WriteLine("[AppSettings] Settings loaded successfully from disk.");
            }
            else
            {
                Debug.WriteLine("[AppSettings] No settings file found. Creating default file...");
                Save();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppSettings] Load error: {ex.Message}");
            _settingsCache = new();
        }
    }

    private static void Save()
    {
        try
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            string json = JsonSerializer.Serialize(_settingsCache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
            Debug.WriteLine($"[AppSettings] Saved settings to: {FilePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppSettings] Save error: {ex.Message}");
        }
    }
}