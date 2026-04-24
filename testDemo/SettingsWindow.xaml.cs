using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GHPHandShake
{
    

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    // 用于存储单个设备信息的类
    public class MachineInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _serverIp;
        private int _serverPort;
        private string _equipmentId;

        public string ServerIp
        {
            get => _serverIp ?? (_serverIp = string.Empty);
            set { _serverIp = value; OnPropertyChanged(nameof(ServerIp)); }
        }

        public int ServerPort
        {
            get => _serverPort;
            set { _serverPort = value; OnPropertyChanged(nameof(ServerIp)); }
        }

        public string EquipmentId
        {
            get => _equipmentId ?? (_equipmentId = string.Empty);
            set { _equipmentId = value; OnPropertyChanged(nameof(EquipmentId)); }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{EquipmentId} ({ServerIp}:{ServerPort})";
        }
    }
    public partial class SettingsWindow : Window
    {
        // 公开属性，用于从外部访问最终选定的设备信息

        public string SelectedServerIp { get; set; }
        public int SelectedServerPort { get; set; }

        public string SelectedEquipmentId { get; set; }

        // 使用 ObservableCollection，当列表内容变化时，UI会自动更新
        public ObservableCollection<MachineInfo> MachineList { get; set; }


        public SettingsWindow(MachineInfo currentMachine, ObservableCollection<MachineInfo> existingMachines)
        {
            InitializeComponent();

            // 初始化设备列表
            MachineList = new ObservableCollection<MachineInfo>(existingMachines ?? new ObservableCollection<MachineInfo>());
            MachineListBox.ItemsSource = MachineList;
            
            // 初始化输入框为当前设置值
            if (currentMachine != null)
            {
                // 找到并选中列表中的当前设备
                var machineToSelect = MachineList.FirstOrDefault(m => m.EquipmentId == currentMachine.EquipmentId);
                if (machineToSelect != null)
                {
                    MachineListBox.SelectedItem = machineToSelect;
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(EquipmentTextBox.Text))
            {
                MessageBox.Show("请输入有效的站位信息！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(IpTextBox.Text))
            {
                MessageBox.Show("请输入有效的 IP 地址！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!int.TryParse(PortTextBox.Text, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("请输入有效的端口号 (1-65535)！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 检查站位信息是否已存在
            if (MachineList.Any(m => m.EquipmentId.Equals(EquipmentTextBox.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("该站位信息已存在，请使用其他名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            

            // 创建新的设备实例并添加到列表
            var newMachine = new MachineInfo
            {
                EquipmentId = EquipmentTextBox.Text.Trim(),
                ServerIp = IpTextBox.Text.Trim(),
                ServerPort = port
            };
            MachineList.Add(newMachine);

            // 清空输入框，方便下次输入
            EquipmentTextBox.Clear();
            IpTextBox.Clear();
            PortTextBox.Clear();

        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前选中的设备
            var selectedMachine = MachineListBox.SelectedItem as MachineInfo;
            if (selectedMachine == null)
            {
                MessageBox.Show("请先从列表中选择一个要删除的设备。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 显示警告窗口，让用户确认删除
            MessageBoxResult result = MessageBox.Show($"您确定要删除设备 '{selectedMachine.EquipmentId}' 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // 从列表中移除设备
                MachineList.Remove(selectedMachine);
            }
        }

        private void MachineListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMachine = MachineListBox.SelectedItem as MachineInfo;
            if (selectedMachine != null)
            {
                // 将选中设备的信息加载到上方的输入框中，方便用户查看或修改
                // 注意：这里我们不直接修改，因为修改逻辑应该通过一个“更新”按钮完成
                // 为了简化，我们暂时只显示信息
                EquipmentTextBox.Text = selectedMachine.EquipmentId;
                IpTextBox.Text = selectedMachine.ServerIp;
                PortTextBox.Text = selectedMachine.ServerPort.ToString();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedMachine = MachineListBox.SelectedItem as MachineInfo;
            if (selectedMachine == null)
            {
                MessageBox.Show("请选择一个设备作为当前连接目标，然后点击保存。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 将选中的设备信息保存到公开属性中
            SelectedEquipmentId = selectedMachine.EquipmentId;
            SelectedServerIp = selectedMachine.ServerIp;
            SelectedServerPort = selectedMachine.ServerPort;

            // 设置 DialogResult 为 true，表示用户点击了保存
            DialogResult = true;
            Close();
        }
    }


}

