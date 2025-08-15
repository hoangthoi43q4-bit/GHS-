using GHPHandShake.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace GHPHandShake.Views
{
    public partial class MaterialSettingControl : UserControl
    {
        private MaterialConfig config = new MaterialConfig();
        private const string ConfigPath = "materials.json";

        public MaterialSettingControl()
        {
            InitializeComponent();
            LoadConfig();
            RefreshUI();
        }

        private void LoadConfig()
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                config = JsonConvert.DeserializeObject<MaterialConfig>(json);
            }
        }

        private void SaveConfig()
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented );
            File.WriteAllText(ConfigPath, json);
        }

        private void RefreshUI()
        {
            TypeList.Items.Clear();
            foreach (var type in config.MaterialTypes)
            {
                var panel = new StackPanel();
                panel.Children.Add(new TextBlock { Text = type.TypeName, FontWeight = FontWeights.Bold });

                foreach (var sub in type.SubTypes)
                {
                    panel.Children.Add(new TextBlock { Text = "- " + sub.Name });
                }

                TypeList.Items.Add(panel);
            }
        }

        private void AddType_Click(object sender, RoutedEventArgs e)
        {
            string name = TypeNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name) || !int.TryParse(name, out _))
            {
                MessageBox.Show("物料类型必须是数字");
                return;
            }

            if (config.MaterialTypes.Count >= 20)
            {
                MessageBox.Show("最多只能添加 20 个物料类型");
                return;
            }

            config.MaterialTypes.Add(new MaterialType { TypeName = name });
            TypeNameBox.Clear();
            RefreshUI();
            SaveConfig();
        }

        private void AddSubType_Click(object sender, RoutedEventArgs e)
        {
            string parent = SubTypeParentBox.Text.Trim();
            string subName = SubTypeNameBox.Text.Trim();

            var type = config.MaterialTypes.Find(t => t.TypeName == parent);
            if (type == null)
            {
                MessageBox.Show("找不到指定的物料类型");
                return;
            }

            if (type.SubTypes.Count >= 10)
            {
                MessageBox.Show("每种类型最多 10 个子类");
                return;
            }

            type.SubTypes.Add(new MaterialSubType { Name = subName });
            SubTypeNameBox.Clear();
            RefreshUI();
            SaveConfig();
        }
    }
}
