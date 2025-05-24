using System.Configuration;
using System.Data;
using System.Windows;
using System.Diagnostics;


namespace VKLauncher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        // 终止你可能启动的所有子进程
        StopFrpProcesses();
    }

    private void StopFrpProcesses()
    {
        try
        {
            var frpNames = new[] { "frpc", "frps" };
            foreach (var name in frpNames)
            {
                foreach (var process in Process.GetProcessesByName(name))
                {
                    process.Kill();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"退出时关闭 FRP 进程失败：{ex.Message}");
        }
    }

}

