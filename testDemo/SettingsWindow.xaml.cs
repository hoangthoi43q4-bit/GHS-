using System;
using System.Windows;
using System.Windows.Controls;

namespace GHPHandShake
{
    public partial class SettingsWindow : Window
    {
        public string ServerIp { get; private set; }
        public int ServerPort { get; private set; }

        public string EquipmentId { get; private set; }

        public SettingsWindow(string currentIp, int currentPort, string equipmentId)
        {
            InitializeComponent();

            // 初始化为当前设置值
            IpTextBox.Text = currentIp;
            PortTextBox.Text = currentPort.ToString();
            EquipmentTextBox.Text = equipmentId;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(IpTextBox.Text) || !int.TryParse(PortTextBox.Text, out int port) )
            {
                MessageBox.Show("请输入有效的 IP 和端口！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(EquipmentTextBox.Text))
            {
                MessageBox.Show("请输入有效的设备名称！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ServerIp = IpTextBox.Text.Trim();
            ServerPort = port;
            EquipmentId = EquipmentTextBox.Text.Trim().ToUpper();
            // 关闭窗口并返回结果
            DialogResult = true;
            Close();
        }
    }
}

