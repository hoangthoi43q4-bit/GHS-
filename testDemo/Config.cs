using System.IO;
using Newtonsoft.Json;

namespace testDemo
{
    class Config
    {
        public string ServerIP { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 2001;

        public string DeviceName { get; set; } = "HGW_AG010_LOAD";

        private static readonly string ConfigFilePath = "config.json";

        //save to config.json
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }


        //load from config.json
        public static Config Load()
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                return JsonConvert.DeserializeObject<Config>(json) ?? new Config();
            }

            return new Config();
        }

    }
}
