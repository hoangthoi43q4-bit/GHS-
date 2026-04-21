using GHPHandShake.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace GHPHandShake.Views
{
    public partial class MaterialSettingControl : UserControl
    {
        private const string ConfigPath = "materials.json";
        Config config;

        public ObservableCollection<MaterialType> MaterialTypesList { get; set; }


        public MaterialSettingControl()
        {
            InitializeComponent();
            LoadConfig();
            // 加载IP配置文件
            config = Config.Load();

            MaterialTreeView.ItemsSource = MaterialTypesList;
            ParentTypeComboBox.ItemsSource = MaterialTypesList;
            MachineComboBox.ItemsSource = config.AllMachines;
        }

        public void SetAvailableMachines(ObservableCollection<MachineInfo> machines)
        {
            MachineComboBox.ItemsSource = machines;
        }

        public ObservableCollection<MaterialType> GetMaterialData()
        {
            return MaterialTypesList;
        }

        //导入excel 逻辑
        private void ImportExcel_Click(object sender,RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel Files|*.xlsx;*.xlsm;*.xlsb;*.xltx;*.xltm";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    using(var workbook = new XLWorkbook(openFileDialog.FileName))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                        int importCount = 0;
                        foreach (var row in rows)
                        {
                            string typeName = row.Cell(1).GetValue<string>().Trim();
                            string subTypeName = row.Cell(2).GetValue<string>().Trim();
                            string machineName = row.Cell(3).GetValue<string>().Trim();
                            string projectName = row.Cell(4).GetValue<string>().Trim();

                            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(subTypeName) || string.IsNullOrEmpty(projectName)) continue;

                            var targetType = MaterialTypesList.FirstOrDefault(t =>
                                t.ProjectName == projectName && t.TypeName == typeName);

                            if (targetType == null)
                            {
                                targetType = new MaterialType { ProjectName = projectName, TypeName = typeName };
                                MaterialTypesList.Add(targetType);
                            }

                            if (!targetType.SubTypes.Any(st => st.SubTypeName.Equals(subTypeName, StringComparison.OrdinalIgnoreCase)))
                            {
                                targetType.SubTypes.Add(new MaterialSubType
                                {
                                    SubTypeName = subTypeName,
                                    AssociatedMachineName = machineName
                                });
                                importCount++;
                            }


                        }
                        MessageBox.Show($"成功导入 {importCount} 条新纪录！", "导入完成", MessageBoxButton.OK, MessageBoxImage.Information);
                        SaveConfig();

                        ParentTypeComboBox.ItemsSource = null;
                        ParentTypeComboBox.ItemsSource = MaterialTypesList;
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }

        private void LoadConfig()
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                var config = JsonConvert.DeserializeObject<MaterialConfig>(json);
                MaterialTypesList = config?.MaterialTypes ?? new ObservableCollection<MaterialType>();
            }
            else
            {
                MaterialTypesList = new ObservableCollection<MaterialType>();
            }
        }

        public void SaveConfig()
        {
            var config = new MaterialConfig { MaterialTypes = this.MaterialTypesList };
            string json = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }

        private void AddType_Click(object sender, RoutedEventArgs e)
        {
            string name = TypeNameBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) || !int.TryParse(name, out _))
            {
                MessageBox.Show("物料类型必须是数字");
                return;
            }
            if (MaterialTypesList.Count >= 20)
            {
                MessageBox.Show("最多只能添加 20 个物料类型");
                return;
            }
            if (MaterialTypesList.Any(t => t.TypeName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("该类型已存在。");
                return;
            }

            MaterialTypesList.Add(new MaterialType { TypeName = name, ProjectName = "未分类项目" });
            TypeNameBox.Clear();
            SaveConfig();
        }

        private void AddSubType_Click(object sender, RoutedEventArgs e)
        {
            var parentType = ParentTypeComboBox.SelectedItem as MaterialType;
            var selectedMachine = MachineComboBox.SelectedItem as MachineInfo;
            string subTypeName = SubTypeNameBox.Text.Trim();

            if (parentType == null) { MessageBox.Show("请选择一个所属类型。"); return; }
            if (string.IsNullOrEmpty(subTypeName)) { MessageBox.Show("子类名（物料号）不能为空。"); return; }
            if (selectedMachine == null) { MessageBox.Show("请为该物料选择一个关联设备。"); return; }
            if (parentType.SubTypes.Count >= 50) { MessageBox.Show("每种类型最多 50 个子类"); return; }
            if (parentType.SubTypes.Any(st => st.SubTypeName.Equals(subTypeName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"类型 '{parentType.TypeName}' 下已存在名为 '{subTypeName}' 的子类。");
                return;
            }

            var newSubType = new MaterialSubType
            {
                SubTypeName = subTypeName,
                AssociatedMachineName = selectedMachine.EquipmentId
            };
            parentType.SubTypes.Add(newSubType);

            SubTypeNameBox.Clear();
            SaveConfig();
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = MaterialTreeView.SelectedItem;
            if (selectedItem == null) { MessageBox.Show("请先选择要删除的项目。"); return; }
            if (MessageBox.Show("确定要删除选中的项目吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;

            if (selectedItem is MaterialSubType subType)
            {
                foreach (var type in MaterialTypesList)
                {
                    if (type.SubTypes.Contains(subType))
                    {
                        type.SubTypes.Remove(subType);
                        break;
                    }
                }
            }
            else if (selectedItem is MaterialType type)
            {
                MaterialTypesList.Remove(type);
            }
            SaveConfig();
        }
    }
}
