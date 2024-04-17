using System;
using System.IO;
using Newtonsoft.Json;

namespace ExcelDataSerializerUI.Models;

[Serializable]
public class SettingInfo
{
    private static readonly string _saveFile = Path.Combine(AppContext.BaseDirectory, "SettingInfo.json");

    public string ExcelPath;
    public string CsOutputPath;
    public string DataOutputPath;

    public SettingInfo()
    {
        Console.WriteLine(_saveFile);
    }
    public void Save()
    {
        var json = JsonConvert.SerializeObject(this);
        File.WriteAllText(_saveFile, json);
    }

    public static SettingInfo Load()
    {
        if (File.Exists(_saveFile))
        {
            var json = File.ReadAllText(_saveFile);
            try
            {
                var loaded = JsonConvert.DeserializeObject<SettingInfo>(json);
                if (loaded != null)
                    return loaded;
            }
            catch (Exception _)
            {
                // ignored
            }
        }

        return new SettingInfo();
    }
}