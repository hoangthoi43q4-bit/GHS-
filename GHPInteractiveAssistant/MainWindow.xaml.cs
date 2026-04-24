using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace GHPInteractiveAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string serverIp = "10.103.32.52"; // TCP服务器IP地址
            int serverPort = 2001;       // TCP服务器端口

            try
            {
                // 创建TcpClient并连接到服务器
                using (TcpClient client = new TcpClient(serverIp, serverPort))
                {
                    Console.WriteLine("已连接到服务器");

                    // 获取网络流
                    NetworkStream stream = client.GetStream();

                    // 准备要发送的消息
                    string message = "LOAD_MATERIAL,FAM101_AG050_P1,3,20240321081648226,<LoadMaterial tokens=\"3\" mat_pos_1=\"1\" mat_uid_1=\"015-1423-0250-2-00@0702024679@UCHN000000030\"/>";

                    // 将消息拼接成带有STX和ETX的格式
                    byte stx = 0x02; // <STX>
                    byte etx = 0x03; // <ETX>
                    byte[] messageData = Encoding.ASCII.GetBytes(message);

                    // 创建完整的数据包
                    byte[] dataToSend = new byte[messageData.Length + 2];
                    dataToSend[0] = stx; // 开头添加<STX>
                    Array.Copy(messageData, 0, dataToSend, 1, messageData.Length); // 插入正文
                    dataToSend[dataToSend.Length - 1] = etx; // 结尾添加<ETX>

                    // 发送数据
                    //Console.WriteLine("发送数据: " + BitConverter.ToString(dataToSend));
                    // 拼接完整消息内容（包括STX和ETX）
                    string formattedMessage = $"{(char)0x02}{message}{(char)0x03}";

                    // 显示发送的字符串内容
                    //Console.WriteLine("发送内容 (字符串形式): " + formattedMessage);
                    stream.Write(dataToSend, 0, dataToSend.Length);

                    //Console.WriteLine("数据发送完成");

                    //接收数据
                    //Console.WriteLine("等待服务器响应");

                    byte[] buffer = new byte[1024];// 接收缓冲区
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        //Console.WriteLine("收到服务器响应：" + response);
                        ResponesLabel.Content = response;
                    }
                    else
                    {
                        //Console.WriteLine("未收到服务器响应");
                        ResponesLabel.Content = "未收到服务器响应"; 
                    }


                    // 关闭流
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生错误: " + ex.Message);
            }
        }
    }
}