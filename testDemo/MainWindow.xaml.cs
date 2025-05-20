using System;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Input;
using testDemo;

namespace testDemo
{
    public partial class MainWindow : Window
    {
        private Config _config;
        private string commandGenerated;
        public MainWindow()
        {
            InitializeComponent();
            InputTextBox.Text = "默认发送的消息内容"; // 初始内容

            // 加载配置文件
            _config = Config.Load();
            AppendMessage($"加载配置: IP={_config.ServerIP}, 端口={_config.ServerPort},站位 ={_config.DeviceName}");
        }

        // 按下回车键
        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        // 点击发送按钮
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        // 点击设置按钮
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_config.ServerIP, _config.ServerPort, _config.DeviceName);
            if (settingsWindow.ShowDialog() == true)
            {
                _config.ServerIP = settingsWindow.ServerIp;
                _config.ServerPort = settingsWindow.ServerPort;
                _config.DeviceName = settingsWindow.EquipmentId;
                _config.Save();
                AppendMessage($"设置已更新: IP={_config.ServerIP}, 端口={_config.ServerPort}, 站位={_config.DeviceName}");
            }
        }

        // 发送消息逻辑
        private void SendMessage()
        {
            string message = InputTextBox.Text.Trim();
            string EquipmentID= _config.DeviceName.Trim();
                    

            if (string.IsNullOrEmpty(message))
            {
                MessageBox.Show("发送内容不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(message))
            {
                StringProcessor processor = new StringProcessor();
                
                commandGenerated = processor.GenerateLoadCommand(message, _config.DeviceName);

            }

            try
            {
                using (TcpClient client = new TcpClient(_config.ServerIP, _config.ServerPort))
                {
                    NetworkStream stream = client.GetStream();
                    string formattedMessage = $"{(char)0x02}{commandGenerated}{(char)0x03}";
                    byte[] dataToSend = Encoding.ASCII.GetBytes(formattedMessage);

                    stream.Write(dataToSend, 0, dataToSend.Length);
                    AppendMessage($"发送: {formattedMessage}");

                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

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
            MessageListBox.Items.Add(message);
            MessageListBox.ScrollIntoView(message);
        }
    }
}

