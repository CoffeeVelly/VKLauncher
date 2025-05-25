using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Navigation;

namespace VKLauncher
{
    public partial class FrpConfigWindow : Window
    {
        public bool IsConfigSaved { get; private set; } = false;

        public FrpConfigWindow()
        {
            InitializeComponent();
        }

        private void BrowseFrpc_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "FRPC 执行文件|frpc.exe"
            };
            if (dlg.ShowDialog() == true)
                FrpcPathBox.Text = dlg.FileName;
        }

        private void BrowseFrps_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "FRPS 执行文件|frps.exe"
            };
            if (dlg.ShowDialog() == true)
                FrpsPathBox.Text = dlg.FileName;
        }

        private void SelectFrpcToml_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "TOML 文件|*.toml",
                Title = "选择已有的 frpc.toml 文件"
            };
            if (dlg.ShowDialog() == true)
                FrpcTomlPathBox.Text = dlg.FileName;
        }

        private void SelectFrpsToml_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "TOML 文件|*.toml",
                Title = "选择已有的 frps.toml 文件"
            };
            if (dlg.ShowDialog() == true)
                FrpsTomlPathBox.Text = dlg.FileName;
        }


        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(FrpcTomlPathBox.Text)){
                if (string.IsNullOrWhiteSpace(ServerAddrBox.Text) ||
                    string.IsNullOrWhiteSpace(ServerPortBox.Text) ||
                    string.IsNullOrWhiteSpace(NameBox.Text))
                {
                    MessageBox.Show("未选择本地toml配置文件，请填写基础配置的所有必要字段！");
                    return;
                }
            }


            string frpcContent = $"""
            serverAddr = "{ServerAddrBox.Text}"
            serverPort = {ServerPortBox.Text}

            [[proxies]]
            name = "{NameBox.Text}"
            type = "{TypeBox.Text}"
            localIP = "{LocalIPBox.Text}"
            localPort = {LocalPortBox.Text}
            remotePort = {RemotePortBox.Text}
            """;

            string frpsContent = $"""
            bindPort = {BindPortBox.Text}
            vhostHTTPSPort = {VhostHTTPSPortBox.Text}
            """;

            try
            {
                string frpcPath = string.IsNullOrWhiteSpace(FrpcTomlPathBox.Text)
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frpc.toml")
                    : FrpcTomlPathBox.Text;

                string frpsPath = string.IsNullOrWhiteSpace(FrpsTomlPathBox.Text)
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frps.toml")
                    : FrpsTomlPathBox.Text;

                File.WriteAllText(frpcPath, frpcContent);
                File.WriteAllText(frpsPath, frpsContent);

                IsConfigSaved = true;
                DialogResult = true; // 重要！
                Close();

                MessageBox.Show("配置文件保存成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
