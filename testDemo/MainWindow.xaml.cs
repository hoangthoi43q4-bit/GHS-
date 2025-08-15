using GHPHandShake;
using GHPHandShake.Models;
using GHPHandShake.Views;
using GHPHandShake.Windows;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Input;


namespace GHPHandShake
{
    public partial class MainWindow : Window
    {
        private Config _config;
        private MaterialConfig _materialConfig;
        private string commandGenerated;

        public MainWindow()
        {
            InitializeComponent();
            InputTextBox.Text = ""; // 初始内容

            // 加载IP配置文件
            _config = Config.Load();
            AppendMessage($"加载配置: IP={_config.ServerIP}, 端口={_config.ServerPort},站位 ={_config.DeviceName}");
            //加载material配置文件
            LoadMaterialConfig();

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

        // 点击发送按钮
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
            InputTextBox.Clear();
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

        // 点击物料设置按钮
        private void MaterialSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var MaterialSettingsWindow = new MaterialSettingWindow();
            MaterialSettingsWindow.ShowDialog();
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

            // 调用StringProcess处理输入字符串
            if (!string.IsNullOrEmpty(message))
            {
                StringProcessor processor = new StringProcessor();
                
                commandGenerated = processor.GenerateLoadCommand(message, _config.DeviceName , config:_materialConfig);
                
            }

            try
            {
                using (TcpClient client = new TcpClient(_config.ServerIP, _config.ServerPort))
                {
                    NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = 5000; //设置读取超时时间，单位是毫秒

                    string formattedMessage = $"{(char)0x02}{commandGenerated}{(char)0x03}";
                    byte[] dataToSend = Encoding.ASCII.GetBytes(formattedMessage);

                    stream.Write(dataToSend, 0, dataToSend.Length);
                    AppendMessage($"发送: {formattedMessage}");

                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length); // 如果超时，会抛出异常

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

