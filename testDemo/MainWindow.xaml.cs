using GHPHandShake;
using GHPHandShake.Models;
using GHPHandShake.Views;
using GHPHandShake.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace GHPHandShake
{
    public partial class MainWindow : Window
    {
        private Config _config;
        private MaterialConfig _materialConfig;
        private string commandGenerated;
        private MaterialSettingControl materialSettingControl = new MaterialSettingControl();
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

            public string TypeName { get; set; } //新增：Excel 中的位置/类型 (1, 2, 3...)
            public string AssociatedMachineName { get; set; } // 这是找到的设备的 EquipmentId
        }

        public MainWindow()
        {
            InitializeComponent();
            InputTextBox.Text = ""; // 初始内容

            // 加载IP配置文件
            _config = Config.Load();
            AppendMessage($"加载配置: IP={_config.ServerIP}, 端口={_config.ServerPort},站位 ={_config.DeviceName}");
            //加载material配置文件
            LoadMaterialConfig();
            //初始化完成后刷新一次项目列表
            this.Loaded += (s, e) => RefreshProjects();

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

        // ==========================================
        // 【新增】：根据导入的数据刷新项目选择下拉框
        // ==========================================
        private void RefreshProjects()
        {
            if (materialSettingControl?.MaterialTypesList == null) return;

            // 提取所有不重复的项目名称 (Excel 第四列)
            var projectList = materialSettingControl.MaterialTypesList
                .Select(t => t.ProjectName)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .OrderBy(p => p)
                .Select(p => new { Name = p })
                .ToList();

            ProjectSelector.ItemsSource = projectList;
            if (projectList.Count > 0)
            {
                ProjectSelector.SelectedIndex = 0;
                AppendMessage($"项目列表更新: 发现 {projectList.Count} 个项目。");
            }
        }

        // 点击刷新按钮时调用
        private void RefreshProjectsBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshProjects();
        }

        private void ProjectSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dynamic selected = ProjectSelector.SelectedItem;
            if (selected != null)
            {
                AppendMessage($"[切换项目] 当前锁定为: {selected.Name}");
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

                // 设置变更后，更新物料控件的设备关联
                materialSettingControl.SetAvailableMachines(_config.AllMachines);
            }
        }

        // 点击物料设置按钮
        private void MaterialSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var MaterialSettingsWindow = new MaterialSettingWindow();
            MaterialSettingsWindow.ShowDialog();
            // 窗体关闭后重新加载配置并刷新项目
            LoadMaterialConfig();
            RefreshProjects();
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

            // 获取当前选中的项目
            dynamic selectedItem = ProjectSelector.SelectedItem;
            string currentProject = selectedItem?.Name;

            if (string.IsNullOrEmpty(currentProject))
            {
                MessageBox.Show("请先选择当前运行的项目！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //传入当前选中的项目名进行精准查找
            MessageRoutingInfo routingInfo = FindRoutingForMaterial(Matlabel, currentProject);

            // 步骤 1: 根据输入的物料号查找其路由信息（目标IP、端口等）。


            if (string.IsNullOrEmpty(Matlabel))
            {
                MessageBox.Show("发送内容中不包含物料信息！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            

            // 步骤 2: 如果在配置中找不到该物料，则停止执行。
            if (!routingInfo.IsFound)
            {
                return;
            }

            // 步骤 3: 使用查找到的信息来生成最终的指令。
            
            string commandGenerated = processor.GenerateLoadCommand(message, routingInfo.AssociatedMachineName, config: _materialConfig);

            // 【新增调试信息】在连接前，明确打印出将要使用的IP和端口。
            AppendMessage($"准备连接到查找到的目标: {routingInfo.TargetIp}:{routingInfo.TargetPort}");

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
                        string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        AppendMessage($"响应: {response}");
                    }
                    else
                    {
                        AppendMessage("响应: 服务器未返回数据");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendMessage($"错误: {ex.Message}");
            }
        }

        // 更新消息列表
        private void AppendMessage(string message)
        {
            MessageListBox.Items.Add(DateTime.Now +" "+ message);
            MessageListBox.ScrollIntoView(message);
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
        public MessageRoutingInfo FindRoutingForMaterial(string receivedMaterialNumber , string projectName)
        {
            // 记录查找起始日志
            AppendMessage($"[检索] 项目: {projectName} | 目标料号: {receivedMaterialNumber}");

            var materialTypes = materialSettingControl.MaterialTypesList;
            if (materialTypes == null || materialTypes.Count == 0)
            {
                AppendMessage("[错误] 物料配置列表为空，请先在设置中导入数据。");
                return new MessageRoutingInfo { IsFound = false };
            }

            // 1. 【高亮修改】：首先根据项目名称过滤出所有属于该项目的配置块
            var projectFilteredBlocks = materialTypes
                .Where(t => t.ProjectName == projectName)
                .ToList();

            if (projectFilteredBlocks.Count == 0)
            {
                AppendMessage($"[错误] 未找到项目 '{projectName}' 对应的配置数据。");
                return new MessageRoutingInfo { IsFound = false };
            }

            MaterialType targetTypeBlock = null;
            MaterialSubType targetSubType = null;

            // 2. 【高亮修改】：仅在属于该项目的块中查找物料号
            foreach (var block in projectFilteredBlocks)
            {
                // 使用 Exact Match 或 Contains 视业务而定，此处建议 Equals 以防混淆
                targetSubType = block.SubTypes.FirstOrDefault(st =>
                    st.SubTypeName.Equals(receivedMaterialNumber, StringComparison.OrdinalIgnoreCase));

                if (targetSubType != null)
                {
                    targetTypeBlock = block;
                    break; // 找到即止，因为已经限定了项目
                }
            }

            // 3. 结果验证与机器匹配
            if (targetSubType == null)
            {
                AppendMessage($"[错误] 在项目 '{projectName}' 中未找到料号 '{receivedMaterialNumber}'。");
                return new MessageRoutingInfo { IsFound = false };
            }

            string machineName = targetSubType.AssociatedMachineName;
            var machine = _config.AllMachines?.FirstOrDefault(m =>
                m.EquipmentId.Equals(machineName, StringComparison.OrdinalIgnoreCase));

            if (machine == null)
            {
                AppendMessage($"[错误] 匹配到位置 {targetTypeBlock.TypeName}，但关联设备 '{machineName}' 未定义。");
                return new MessageRoutingInfo { IsFound = false };
            }

            // 4. 返回查找到的路由信息
            AppendMessage($"[命中] 项目:{projectName} | 位置:{targetTypeBlock.TypeName} | 设备:{machine.EquipmentId}");

            return new MessageRoutingInfo
            {
                IsFound = true,
                TargetIp = machine.ServerIp,
                TargetPort = machine.ServerPort,
                MessageToSend = targetSubType.SubTypeName,
                TypeName = targetTypeBlock.TypeName, // 这里的 TypeName 就是 Excel 第一列的位置
                AssociatedMachineName = machine.EquipmentId
            };
        }


    }
}

