using System;
using System.IO;
using UnityEngine;

namespace BubbleAscent.Core
{
    /// <summary>
    /// Data saved on the device. Versioned: when the shape changes, bump CurrentVersion and migrate in SaveStore.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public float bestHeight;
    }

    /// <summary>
    /// Reads and writes SaveData as JSON, tolerant of missing or corrupt files, with atomic writes.
    /// </summary>
    public static class SaveStore
    {
        public const string FileName = "save.json";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        public static string Serialize(SaveData data) => JsonUtility.ToJson(data);

        /// <summary>Never throws: bad or empty input gives a fresh SaveData.</summary>
        public static SaveData Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new SaveData();
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception)
            {
                return new SaveData();
            }
            return data == null ? new SaveData() : Migrate(data);
        }

        public static SaveData Load(string path) => Load(path, out _);

        /// <summary>Never throws. readable = false when a file exists but could not be read: do not overwrite it.</summary>
        public static SaveData Load(string path, out bool readable)
        {
            readable = true;
            try
            {
                return File.Exists(path) ? Parse(File.ReadAllText(path)) : new SaveData();
            }
            catch (Exception e)
            {
                readable = false;
                Debug.LogWarning($"SaveStore: could not read the save ({e.Message}); it will not be overwritten.");
                return new SaveData();
            }
        }

        /// <summary>Writes to a temp file first, then swaps it in, so a crash never leaves half a save.</summary>
        public static bool Save(string path, SaveData data)
        {
            string tmp = path + ".tmp";
            try
            {
                File.WriteAllText(tmp, Serialize(data));
                if (!File.Exists(path))
                {
                    File.Move(tmp, path);
                    return true;
                }
                try
                {
                    File.Replace(tmp, path, null);
                }
                catch (Exception)
                {
                    // Some platforms lack an atomic replace: fall back to copy over + delete.
                    File.Copy(tmp, path, true);
                    File.Delete(tmp);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveStore: could not save ({e.Message}).");
                return false;
            }
        }

        static SaveData Migrate(SaveData data)
        {
            // Version 0 = files written before versioning existed; same fields.
            if (data.version < SaveData.CurrentVersion) data.version = SaveData.CurrentVersion;
            if (float.IsNaN(data.bestHeight) || data.bestHeight < 0f) data.bestHeight = 0f;
            return data;
        }
    }
}
