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

        // 修改为项目列表作为根节点
        public ObservableCollection<ProjectInfo> ProjectsList { get; set; }

        public MaterialSettingControl()
        {
            InitializeComponent();
            LoadConfig();

            // 加载IP配置文件
            config = Config.Load();

            MaterialTreeView.ItemsSource = ProjectsList;
            ProjectComboBox.ItemsSource = ProjectsList;
            MachineComboBox.ItemsSource = config.AllMachines;
        }

        public void SetAvailableMachines(ObservableCollection<MachineInfo> machines)
        {
            MachineComboBox.ItemsSource = machines;
        }

        // 保持此方法名，返回当前所有项目数据
        public ObservableCollection<ProjectInfo> GetMaterialData()
        {
            return ProjectsList;
        }

        #region 数据加载与保存

        private void LoadConfig()
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                var configData = JsonConvert.DeserializeObject<MaterialConfig>(json);
                // 适配新模型：根级是 Projects
                ProjectsList = configData?.Projects ?? new ObservableCollection<ProjectInfo>();
            }
            else
            {
                ProjectsList = new ObservableCollection<ProjectInfo>();
            }
        }

        public void SaveConfig()
        {
            var configData = new MaterialConfig { Projects = this.ProjectsList };
            string json = JsonConvert.SerializeObject(configData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }

        #endregion

        #region 操作逻辑

        // 新增：添加项目逻辑
        private void AddProject_Click(object sender, RoutedEventArgs e)
        {
            string name = ProjectNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name) || ProjectsList.Any(p => p.ProjectName == name))
            {
                MessageBox.Show("项目名不能为空且不能重复");
                return;
            }

            ProjectsList.Add(new ProjectInfo { ProjectName = name });
            ProjectNameBox.Clear();
            SaveConfig();
        }

        // 项目选择变更时，更新位置(Position)下拉框的数据源
        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var project = ProjectComboBox.SelectedItem as ProjectInfo;
            // 对应 XAML 中的 SubTypeParentBox
            SubTypeParentBox.ItemsSource = project?.MaterialTypes;
        }

        // 修改：添加类型(Position)逻辑
        private void AddType_Click(object sender, RoutedEventArgs e)
        {
            var project = ProjectComboBox.SelectedItem as ProjectInfo;
            string typeName = TypeNameBox.Text.Trim(); // 保持使用 TypeNameBox

            if (project == null)
            {
                MessageBox.Show("请先选择一个所属项目");
                return;
            }
            if (string.IsNullOrEmpty(typeName))
            {
                MessageBox.Show("位置名称不能为空");
                return;
            }

            if (!project.MaterialTypes.Any(t => t.TypeName.Equals(typeName, StringComparison.OrdinalIgnoreCase)))
            {
                project.MaterialTypes.Add(new MaterialType { TypeName = typeName });
                TypeNameBox.Clear();
                SaveConfig();
            }
            else
            {
                MessageBox.Show("该位置已存在");
            }
        }

        // 修改：添加子类(物料号)逻辑
        private void AddSubType_Click(object sender, RoutedEventArgs e)
        {
            // 对应 XAML 中的 SubTypeParentBox
            var parentType = SubTypeParentBox.SelectedItem as MaterialType;
            var selectedMachine = MachineComboBox.SelectedItem as MachineInfo;
            string subTypeName = SubTypeNameBox.Text.Trim();

            if (parentType == null) { MessageBox.Show("请选择一个所属类型(位置)。"); return; }
            if (string.IsNullOrEmpty(subTypeName)) { MessageBox.Show("子类名（物料号）不能为空。"); return; }
            if (selectedMachine == null) { MessageBox.Show("请为该物料选择一个关联设备。"); return; }

            if (parentType.SubTypes.Any(st => st.SubTypeName.Equals(subTypeName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"位置 '{parentType.TypeName}' 下已存在名为 '{subTypeName}' 的物料。");
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

        // 导入excel 逻辑 (支持4列：位置, 料号, 设备, 项目)
        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel Files|*.xlsx;*.xlsm;*.xlsb;*.xltx;*.xltm";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook(openFileDialog.FileName))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                        int importCount = 0;
                        foreach (var row in rows)
                        {
                            string posName = row.Cell(1).GetValue<string>().Trim();
                            string subName = row.Cell(2).GetValue<string>().Trim();
                            string macName = row.Cell(3).GetValue<string>().Trim();
                            string proName = row.Cell(4).GetValue<string>().Trim(); // 第四列：项目名称

                            if (string.IsNullOrEmpty(posName) || string.IsNullOrEmpty(subName) || string.IsNullOrEmpty(proName)) continue;

                            // 1. 查找或创建项目
                            var targetProject = ProjectsList.FirstOrDefault(p => p.ProjectName == proName);
                            if (targetProject == null)
                            {
                                targetProject = new ProjectInfo { ProjectName = proName };
                                ProjectsList.Add(targetProject);
                            }

                            // 2. 查找或创建位置 (MaterialType)
                            var targetType = targetProject.MaterialTypes.FirstOrDefault(t => t.TypeName == posName);
                            if (targetType == null)
                            {
                                targetType = new MaterialType { TypeName = posName };
                                targetProject.MaterialTypes.Add(targetType);
                            }

                            // 3. 添加物料号 (查重)
                            if (!targetType.SubTypes.Any(st => st.SubTypeName.Equals(subName, StringComparison.OrdinalIgnoreCase)))
                            {
                                targetType.SubTypes.Add(new MaterialSubType
                                {
                                    SubTypeName = subName,
                                    AssociatedMachineName = macName
                                });
                                importCount++;
                            }
                        }
                        MessageBox.Show($"成功导入 {importCount} 条新纪录！", "导入完成", MessageBoxButton.OK, MessageBoxImage.Information);
                        SaveConfig();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导入失败: " + ex.Message);
                }
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = MaterialTreeView.SelectedItem;
            if (selectedItem == null) { MessageBox.Show("请先选择要删除的项目。"); return; }
            if (MessageBox.Show("确定要删除选中的项目吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;

            if (selectedItem is MaterialSubType subType)
            {
                // 三级删除：遍历项目 -> 遍历类型 -> 移除物料
                foreach (var project in ProjectsList)
                {
                    foreach (var type in project.MaterialTypes)
                    {
                        if (type.SubTypes.Contains(subType))
                        {
                            type.SubTypes.Remove(subType);
                            goto EndDelete;
                        }
                    }
                }
            }
            else if (selectedItem is MaterialType type)
            {
                // 二级删除：遍历项目 -> 移除类型
                foreach (var project in ProjectsList)
                {
                    if (project.MaterialTypes.Contains(type))
                    {
                        project.MaterialTypes.Remove(type);
                        goto EndDelete;
                    }
                }
            }
            else if (selectedItem is ProjectInfo project)
            {
                // 一级删除：直接移除项目
                ProjectsList.Remove(project);
            }

        EndDelete:
            SaveConfig();
        }

        #endregion
    }
}