// AboutViewModel.cs
using System;
using System.ComponentModel;
using System.Reflection;

public class AboutViewModel : INotifyPropertyChanged
{
    // 公开的属性，用于绑定到 XAML 界面
    public string VersionString { get; set; }
    public string ReleaseNotes { get; set; }
    public string ReleaseDate { get; set; }

    public AboutViewModel()
    {
        // --- 自动获取程序集的版本号 ---
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        // 格式化为主版本.次版本，例如 1.0
        //VersionString = $"Version : v{version.Major}.{version.Minor}";
        VersionString = "V1.1";
        // 你也可以获取完整的版本号: version.ToString() -> 1.0.0.0

        // --- 硬编码的更新说明和日期 ---
        // 在实际项目中，这些信息可以从一个嵌入的资源文件或服务器读取
        ReleaseNotes = $"{VersionString} The Released version.\n\n- Added feature for multiple equipments.\n- Fixed bug B.\n- Improved performance.";
        ReleaseDate = "2025.8.27";
    }

    // INotifyPropertyChanged 的实现（如果你的版本信息需要在运行时变化）
    // 对于静态的“关于”窗口，这个接口不是必需的，但加上总是个好习惯。
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
