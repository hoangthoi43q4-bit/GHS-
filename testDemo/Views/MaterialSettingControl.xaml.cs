using GHPHandShake.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
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
        private Config config;
        private ObservableCollection<string> _projectNames = new ObservableCollection<string>();
        private ObservableCollection<MaterialProjectNode> _projectTreeNodes = new ObservableCollection<MaterialProjectNode>();

        public ObservableCollection<MaterialType> MaterialTypesList { get; set; }


        public MaterialSettingControl()
        {
            InitializeComponent();
            LoadConfig();
            // 加载IP配置文件
            config = Config.Load();

            MachineComboBox.ItemsSource = config.AllMachines;
            RefreshAllBindings();
        }

        public void SetAvailableMachines(ObservableCollection<MachineInfo> machines)
        {
            MachineComboBox.ItemsSource = machines;
        }

        public ObservableCollection<MaterialType> GetMaterialData()
        {
            return MaterialTypesList;
        }

        private void RefreshAllBindings()
        {
            if (MaterialTypesList == null)
            {
                MaterialTypesList = new ObservableCollection<MaterialType>();
            }
            if (_projectNames == null)
            {
                _projectNames = new ObservableCollection<string>();
            }

            // 从已有类型中补齐项目名，避免历史配置缺失。
            foreach (var project in MaterialTypesList
                         .Where(t => !string.IsNullOrWhiteSpace(t.ProjectName))
                         .Select(t => t.ProjectName.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!_projectNames.Any(p => string.Equals(p, project, StringComparison.OrdinalIgnoreCase)))
                {
                    _projectNames.Add(project);
                }
            }

            var sortedProjects = _projectNames
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p)
                .ToList();

            TypeProjectComboBox.ItemsSource = sortedProjects;
            ParentProjectComboBox.ItemsSource = sortedProjects;

            if (sortedProjects.Count > 0)
            {
                if (TypeProjectComboBox.SelectedItem == null || !sortedProjects.Contains(TypeProjectComboBox.SelectedItem.ToString()))
                {
                    TypeProjectComboBox.SelectedItem = sortedProjects[0];
                }
                if (ParentProjectComboBox.SelectedItem == null || !sortedProjects.Contains(ParentProjectComboBox.SelectedItem.ToString()))
                {
                    ParentProjectComboBox.SelectedItem = sortedProjects[0];
                }
            }
            else
            {
                TypeProjectComboBox.SelectedItem = null;
                ParentProjectComboBox.SelectedItem = null;
                ParentTypeComboBox.ItemsSource = null;
            }

            RefreshParentTypeOptions();
            RebuildTree();
        }

        private void RefreshParentTypeOptions()
        {
            string selectedProject = ParentProjectComboBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selectedProject))
            {
                ParentTypeComboBox.ItemsSource = null;
                ParentTypeComboBox.SelectedItem = null;
                return;
            }

            var types = MaterialTypesList
                .Where(t => string.Equals(t.ProjectName, selectedProject, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.TypeName)
                .ToList();

            ParentTypeComboBox.ItemsSource = types;
            if (types.Count > 0)
            {
                ParentTypeComboBox.SelectedItem = types[0];
            }
            else
            {
                ParentTypeComboBox.SelectedItem = null;
            }
        }

        private void RebuildTree()
        {
            var nodes = new ObservableCollection<MaterialProjectNode>();
            var orderedProjects = _projectNames
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p)
                .ToList();

            foreach (var projectName in orderedProjects)
            {
                var projectNode = new MaterialProjectNode
                {
                    ProjectName = projectName
                };

                foreach (var type in MaterialTypesList
                             .Where(t => string.Equals(t.ProjectName, projectName, StringComparison.OrdinalIgnoreCase))
                             .OrderBy(t => t.TypeName))
                {
                    projectNode.Types.Add(type);
                }

                nodes.Add(projectNode);
            }

            _projectTreeNodes = nodes;
            MaterialTreeView.ItemsSource = _projectTreeNodes;
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
                                string.Equals(t.TypeName, typeName, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(t.ProjectName, projectName, StringComparison.OrdinalIgnoreCase));
                            if (targetType == null)
                            {
                                targetType = new MaterialType { ProjectName = projectName, TypeName = typeName };
                                MaterialTypesList.Add(targetType);
                            }
                            if (!_projectNames.Any(p => string.Equals(p, projectName, StringComparison.OrdinalIgnoreCase)))
                            {
                                _projectNames.Add(projectName);
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
                        RefreshAllBindings();
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
                _projectNames = config?.ProjectNames ?? new ObservableCollection<string>();
            }
            else
            {
                MaterialTypesList = new ObservableCollection<MaterialType>();
                _projectNames = new ObservableCollection<string>();
            }
        }

        public void SaveConfig()
        {
            var config = new MaterialConfig
            {
                MaterialTypes = this.MaterialTypesList,
                ProjectNames = _projectNames
            };
            string json = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }

        private void AddProject_Click(object sender, RoutedEventArgs e)
        {
            string projectName = ProjectNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(projectName))
            {
                MessageBox.Show("项目名不能为空");
                return;
            }

            if (_projectNames.Any(p => string.Equals(p, projectName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("该项目已存在。");
                return;
            }

            _projectNames.Add(projectName);
            ProjectNameBox.Clear();
            SaveConfig();
            RefreshAllBindings();
        }

        private void AddType_Click(object sender, RoutedEventArgs e)
        {
            string selectedProject = TypeProjectComboBox.SelectedItem as string;
            string name = TypeNameBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(selectedProject))
            {
                MessageBox.Show("请先选择所属项目。");
                return;
            }
            if (string.IsNullOrWhiteSpace(name) || !int.TryParse(name, out _))
            {
                MessageBox.Show("物料类型必须是数字");
                return;
            }
            if (MaterialTypesList.Any(t =>
                string.Equals(t.ProjectName, selectedProject, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.TypeName, name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("该项目下类型已存在。");
                return;
            }

            MaterialTypesList.Add(new MaterialType { ProjectName = selectedProject, TypeName = name });
            TypeNameBox.Clear();
            SaveConfig();
            RefreshAllBindings();
        }

        private void AddSubType_Click(object sender, RoutedEventArgs e)
        {
            var parentProject = ParentProjectComboBox.SelectedItem as string;
            var parentType = ParentTypeComboBox.SelectedItem as MaterialType;
            var selectedMachine = MachineComboBox.SelectedItem as MachineInfo;
            string subTypeName = SubTypeNameBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(parentProject)) { MessageBox.Show("请选择项目。"); return; }
            if (parentType == null) { MessageBox.Show("请选择一个所属类型。"); return; }
            if (string.IsNullOrEmpty(subTypeName)) { MessageBox.Show("子类名（物料号）不能为空。"); return; }
            if (selectedMachine == null) { MessageBox.Show("请为该物料选择一个关联设备。"); return; }
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
            RefreshAllBindings();
        }

        private void ParentProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshParentTypeOptions();
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
            else if (selectedItem is MaterialProjectNode projectNode)
            {
                for (int i = MaterialTypesList.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(MaterialTypesList[i].ProjectName, projectNode.ProjectName, StringComparison.OrdinalIgnoreCase))
                    {
                        MaterialTypesList.RemoveAt(i);
                    }
                }

                var projectToRemove = _projectNames.FirstOrDefault(p =>
                    string.Equals(p, projectNode.ProjectName, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(projectToRemove))
                {
                    _projectNames.Remove(projectToRemove);
                }
            }
            else if (selectedItem is MaterialType type)
            {
                MaterialTypesList.Remove(type);
            }
            SaveConfig();
            RefreshAllBindings();
        }
    }
}
