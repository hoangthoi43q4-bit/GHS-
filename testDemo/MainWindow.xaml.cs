using GHPHandShake.Models;
using GHPHandShake.Services;
using GHPHandShake.Views;
using GHPHandShake.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GHPHandShake
{
    public partial class MainWindow : Window
    {
        private Config _config;
        private MaterialConfig _materialConfig;
        private readonly MaterialUploadHistoryService _uploadHistoryService = new MaterialUploadHistoryService();
        private readonly ObservableCollection<ProjectMaterialStatusItem> _projectMaterialStatuses = new ObservableCollection<ProjectMaterialStatusItem>();

        public class ProjectMaterialStatusItem
        {
            public string ProjectName { get; set; }
            public string TypeName { get; set; }
            public string SubTypeName { get; set; }
            public string MachineName { get; set; }
            public string UploadStatus { get; set; } = "未上传";
            public string UploadStatusColor { get; set; } = "#9CA3AF";
        }

        /// <summary>
        /// 一个辅助类，用于在方法之间传递物料查找的结果。
        /// A helper class to pass the results of the material lookup between methods.
        /// </summary>
        public class MessageRoutingInfo
        {
            public bool IsFound { get; set; }
            public string TargetIp { get; set; }
            public int TargetPort { get; set; }
            public string MessageToSend { get; set; } // 这是物料的 SubTypeName
            public string AssociatedMachineName { get; set; } // 这是找到的设备的 EquipmentId
            public string MatLabel { get; set; }
            public string ProjectName { get; set; }
            public string TypeName { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            InputTextBox.Text = ""; // 初始内容
            ProjectMaterialsGrid.ItemsSource = _projectMaterialStatuses;
            FileLogger.Initialize();

            // 加载IP配置文件
            _config = Config.Load();
            AppendMessage($"加载配置: IP={_config.ServerIP}, 端口={_config.ServerPort},站位 ={_config.DeviceName}");
            //加载material配置文件
            LoadMaterialConfig();
            LoadProjectSelector();

        }

        //读区 material config
        public void LoadMaterialConfig()
        {
            const string configPath = "materials.json";
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                _materialConfig = JsonConvert.DeserializeObject<MaterialConfig>(json);
            }
            else
            {
                MessageBox.Show("配置文件 materials.json 不存在，无法初始化！");
                _materialConfig = new MaterialConfig();
            }

            if (_materialConfig == null)
            {
                _materialConfig = new MaterialConfig();
            }
            if (_materialConfig.MaterialTypes == null)
            {
                _materialConfig.MaterialTypes = new ObservableCollection<MaterialType>();
            }
        }
        // 按下回车键
        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
                InputTextBox.Clear();
            }
        }

        // 点击发送按钮
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
            InputTextBox.Clear();
        }

        // 点击设置按钮
        // 点击设置按钮
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. 创建一个代表当前配置的 MachineInfo 对象
            //    This tells the settings window which machine should be selected when it opens.
            var currentMachine = new MachineInfo
            {
                ServerIp = _config.ServerIP,
                ServerPort = _config.ServerPort,
                EquipmentId = _config.DeviceName
            };

            // 2. 准备要传递给设置窗口的完整设备列表
            //    You will need to load this list from your config file.
            //    For this example, let's assume your _config object has a list property called "AllMachines".
            //    If _config.AllMachines is null (first time running), create a new empty list.
            var allMachinesList = _config.AllMachines ?? new ObservableCollection<MachineInfo>();

            // 3. 使用新的构造函数创建和打开窗口
            var settingsWindow = new SettingsWindow(currentMachine, allMachinesList);

            // 4. 显示窗口，并检查用户是否点击了 "保存并应用选择"
            if (settingsWindow.ShowDialog() == true)
            {
                // 5. 从窗口的公开属性中获取选定的设备信息，并更新主配置
                _config.ServerIP = settingsWindow.SelectedServerIp;
                _config.ServerPort = settingsWindow.SelectedServerPort;
                _config.DeviceName = settingsWindow.SelectedEquipmentId;

                // 6. (重要!) 获取在设置窗口中被修改过的完整列表，并更新到主配置中
                _config.AllMachines = settingsWindow.MachineList;

                // 7. 保存所有更改 (包括当前选择和完整的设备列表)
                _config.Save();

                AppendMessage($"设置已更新: IP={_config.ServerIP}, 端口={_config.ServerPort}, 站位={_config.DeviceName}");
            }
        }

        // 点击物料设置按钮
        private void MaterialSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var MaterialSettingsWindow = new MaterialSettingWindow();
            MaterialSettingsWindow.ShowDialog();
            LoadMaterialConfig();
            LoadProjectSelector();
        }

        private void MaterialUploadHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            _uploadHistoryService.Load();
            string currentProject = ProjectComboBox.SelectedItem as string;
            var historyWindow = new MaterialUploadHistoryWindow(_uploadHistoryService, currentProject);
            historyWindow.ShowDialog();
        }

        // 发送消息逻辑
        private async void SendMessage()
        {
            string message = InputTextBox.Text.Trim();
            StringProcessor processor = new StringProcessor();

            string Matlabel = processor.FindMaterial(message);

            if (string.IsNullOrEmpty(message))
            {
                MessageBox.Show("发送内容不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 步骤 1: 根据输入的物料号查找其路由信息（目标IP、端口等）。
            

            if (string.IsNullOrEmpty(Matlabel))
            {
                MessageBox.Show("发送内容中不包含物料信息！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageRoutingInfo routingInfo = FindRoutingForMaterial(Matlabel);

            // 步骤 2: 如果在配置中找不到该物料，则停止执行。
            if (!routingInfo.IsFound)
            {
                return;
            }

            SetMaterialStatus(routingInfo.MatLabel, "等待ACK");

            // 步骤 3: 使用查找到的信息来生成最终的指令。
            string commandGenerated = processor.GenerateLoadCommand(message, routingInfo );

            // 【新增调试信息】在连接前，明确打印出将要使用的IP和端口。
            AppendMessage($"准备连接到查找到的目标: {routingInfo.TargetIp}:{routingInfo.TargetPort}");

            bool ackSuccess = false;
            string responseText = string.Empty;

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // 【异步连接】不会阻塞UI
                    await client.ConnectAsync(routingInfo.TargetIp, routingInfo.TargetPort);

                    NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = 5000;

                    string formattedMessage = $"{(char)0x02}{commandGenerated}{(char)0x03}";
                    byte[] dataToSend = Encoding.ASCII.GetBytes(formattedMessage);

                    // 【异步发送】不会阻塞UI
                    await stream.WriteAsync(dataToSend, 0, dataToSend.Length);
                    AppendMessage($"发送到 {routingInfo.TargetIp}:{routingInfo.TargetPort} -> {formattedMessage}");

                    byte[] buffer = new byte[1024];
                    // 【异步读取】不会阻塞UI
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        responseText = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        AppendMessage($"响应: {responseText}");
                        if (responseText.IndexOf("ACK") >= 0)
                        {
                            ackSuccess = true;
                            SetMaterialStatus(routingInfo.MatLabel, "上传成功(ACK)");
                        }
                        else
                        {
                            SetMaterialStatus(routingInfo.MatLabel, "上传未成功");
                        }
                    }
                    else
                    {
                        responseText = "服务器未返回数据";
                        AppendMessage("响应: 服务器未返回数据");
                        SetMaterialStatus(routingInfo.MatLabel, "上传未成功");
                    }
                }
            }
            catch (Exception ex)
            {
                responseText = ex.Message;
                AppendMessage($"错误: {ex.Message}");
                SetMaterialStatus(routingInfo.MatLabel, "上传未成功");
            }

            RecordUploadHistory(message, routingInfo, ackSuccess, responseText);
        }

        private void RecordUploadHistory(string scanContent, MessageRoutingInfo routingInfo, bool ackSuccess, string response)
        {
            _uploadHistoryService.AddEntry(new MaterialUploadLogEntry
            {
                Timestamp = DateTime.Now,
                ProjectName = routingInfo.ProjectName,
                TypeName = routingInfo.TypeName,
                SubTypeName = routingInfo.MatLabel,
                MachineName = routingInfo.AssociatedMachineName,
                ScanContent = scanContent,
                IsAckSuccess = ackSuccess,
                Response = response
            });

            string resultText = ackSuccess ? "ACK成功" : "未成功";
            AppendMessage($"[上料记录] 项目={routingInfo.ProjectName}, Pos={routingInfo.TypeName}, 物料={routingInfo.MatLabel}, 结果={resultText}");
        }

        // 更新消息列表
        private void AppendMessage(string message)
        {
            string line = DateTime.Now + " " + message;
            MessageListBox.Items.Add(line);
            if (MessageListBox.Items.Count > 0)
            {
                MessageListBox.ScrollIntoView(MessageListBox.Items[MessageListBox.Items.Count - 1]);
            }
            FileLogger.Log(message);
        }

        //
        private void ReleaseInfoButton_Click(object sender, EventArgs e)
        {
            var ReleaseInfoWindow = new ReleaseInfoWindow();
            ReleaseInfoWindow.ShowDialog();
        }

        /// <summary>
        /// 根据物料号查找其配置信息。此函数现在返回一个包含所有需要信息的结果对象。
        /// </summary>
        /// <param name="receivedMaterialNumber">要查找的物料号</param>
        /// <returns>一个 MessageRoutingInfo 对象</returns>
        public MessageRoutingInfo FindRoutingForMaterial(string receivedMaterialNumber)
        {
            AppendMessage($"接收到物料号: {receivedMaterialNumber}。正在查找...");

            var materialTypes = _materialConfig?.MaterialTypes;
            if (materialTypes == null)
            {
                AppendMessage("[错误] 物料配置未加载。");
                return new MessageRoutingInfo { IsFound = false };
            }

            string selectedProject = ProjectComboBox.SelectedItem?.ToString();
            var scopedTypes = materialTypes.Where(t =>
                string.IsNullOrWhiteSpace(selectedProject) ||
                string.Equals(t.ProjectName, selectedProject, StringComparison.OrdinalIgnoreCase));

            MaterialType matchedType = null;
            MaterialSubType foundMaterial = null;
            foreach (var type in scopedTypes)
            {
                foundMaterial = type.SubTypes.FirstOrDefault(st => st.SubTypeName.Contains(receivedMaterialNumber));
                if (foundMaterial != null)
                {
                    matchedType = type;
                    break;
                }
            }

            if (foundMaterial == null)
            {
                AppendMessage($"[错误] 配置中未找到物料 '{receivedMaterialNumber}'。");
                return new MessageRoutingInfo { IsFound = false };
            }

            string machineName = foundMaterial.AssociatedMachineName;
            var machine = _config.AllMachines?.FirstOrDefault(m => m.EquipmentId.Equals(machineName, StringComparison.OrdinalIgnoreCase));

            if (machine == null)
            {
                AppendMessage($"[错误] 物料 '{receivedMaterialNumber}' 关联的设备 '{machineName}' 不存在。");
                return new MessageRoutingInfo { IsFound = false };
            }

            AppendMessage($"匹配成功! 目标设备: {machine.EquipmentId} ({machine.ServerIp}:{machine.ServerPort})。");

            // 成功！返回包含所有信息的对象。
            return new MessageRoutingInfo
            {
                IsFound = true,
                TargetIp = machine.ServerIp,
                TargetPort = machine.ServerPort,
                MessageToSend = foundMaterial.SubTypeName,
                AssociatedMachineName = machine.EquipmentId,
                MatLabel = foundMaterial.SubTypeName,
                ProjectName = matchedType?.ProjectName,
                TypeName = matchedType?.TypeName
            };
        }

        private void LoadProjectSelector()
        {
            var projectsFromTypes = _materialConfig.MaterialTypes
                .Where(mt => !string.IsNullOrWhiteSpace(mt.ProjectName))
                .Select(mt => mt.ProjectName.Trim())
                .ToList();
            var projectsFromConfig = _materialConfig.ProjectNames == null
                ? new List<string>()
                : _materialConfig.ProjectNames.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
            var projects = projectsFromTypes
                .Concat(projectsFromConfig)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p)
                .ToList();

            ProjectComboBox.ItemsSource = projects;
            if (projects.Count > 0)
            {
                ProjectComboBox.SelectedItem = projects[0];
            }
            else
            {
                ProjectComboBox.SelectedItem = null;
                _projectMaterialStatuses.Clear();
            }
        }

        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RebuildProjectMaterialPanel();
        }

        private void RebuildProjectMaterialPanel()
        {
            string selectedProject = ProjectComboBox.SelectedItem?.ToString();
            _projectMaterialStatuses.Clear();
            if (string.IsNullOrWhiteSpace(selectedProject) || _materialConfig?.MaterialTypes == null)
            {
                return;
            }

            var orderedRows = _materialConfig.MaterialTypes
                .Where(t => string.Equals(t.ProjectName, selectedProject, StringComparison.OrdinalIgnoreCase))
                .SelectMany(type => type.SubTypes.Select(sub => new { Type = type, Sub = sub }))
                .OrderBy(x => x.Sub.AssociatedMachineName)
                .ThenBy(x => x.Type.TypeName)
                .ThenBy(x => x.Sub.SubTypeName);

            foreach (var row in orderedRows)
            {
                _projectMaterialStatuses.Add(new ProjectMaterialStatusItem
                {
                    ProjectName = selectedProject,
                    TypeName = row.Type.TypeName,
                    SubTypeName = row.Sub.SubTypeName,
                    MachineName = row.Sub.AssociatedMachineName,
                    UploadStatus = "未上传",
                    UploadStatusColor = GetStatusColor("未上传")
                });
            }
        }

        private void SetMaterialStatus(string matLabel, string status)
        {
            if (string.IsNullOrWhiteSpace(matLabel))
            {
                return;
            }

            var target = _projectMaterialStatuses.FirstOrDefault(item =>
                item.SubTypeName != null &&
                item.SubTypeName.IndexOf(matLabel, StringComparison.OrdinalIgnoreCase) >= 0);

            if (target != null)
            {
                target.UploadStatus = status;
                target.UploadStatusColor = GetStatusColor(status);
                RefreshProjectMaterialGrid();
            }
        }

        private string GetStatusColor(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "#9CA3AF";
            }

            if (status.IndexOf("上传成功", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(status.Trim(), "ACK", StringComparison.OrdinalIgnoreCase))
            {
                return "#22C55E";
            }

            if (status.IndexOf("失败", StringComparison.OrdinalIgnoreCase) >= 0 ||
                status.IndexOf("未成功", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "#EF4444";
            }

            return "#9CA3AF";
        }

        private void RefreshProjectMaterialGrid()
        {
            var currentItems = _projectMaterialStatuses.ToList();
            _projectMaterialStatuses.Clear();
            foreach (var item in currentItems)
            {
                _projectMaterialStatuses.Add(item);
            }
        }

    }
}

