using System;
using System.IO;
using System.Text.Json;

namespace DofusOrganizer
{
    public class Settings
    {
        public int PositionX { get; set; } = 100;
        public int PositionY { get; set; } = 100;
        public double Opacity { get; set; } = 0.95;
        public bool AutoRefresh { get; set; } = false;
        public int AutoRefreshInterval { get; set; } = 5000;
        public string Theme { get; set; } = "Dark";

        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DofusOrganizer",
            "settings.json"
        );

        public static Settings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des paramètres: {ex.Message}");
            }

            return new Settings();
        }

        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la sauvegarde des paramètres: {ex.Message}");
            }
        }
    }
}
