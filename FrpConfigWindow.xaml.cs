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
            // 如果没有选择 frpc.toml，本地基础配置必须填写
            if (string.IsNullOrWhiteSpace(FrpcTomlPathBox.Text))
            {
                if (string.IsNullOrWhiteSpace(ServerAddrBox.Text) ||
                    string.IsNullOrWhiteSpace(ServerPortBox.Text) ||
                    string.IsNullOrWhiteSpace(NameBox.Text)||
                    string.IsNullOrWhiteSpace(TypeBox.Text) ||
                    string.IsNullOrWhiteSpace(LocalIPBox.Text) ||
                    string.IsNullOrWhiteSpace(LocalPortBox.Text) ||
                    string.IsNullOrWhiteSpace(RemotePortBox.Text) ||
                    string.IsNullOrWhiteSpace(BindPortBox.Text) ||
                    string.IsNullOrWhiteSpace(VhostHTTPSPortBox.Text)
                    )
                {
                    MessageBox.Show("未选择 frpc.toml 文件，请填写 frpc 的基础配置中所有必填项！");
                    return;
                }
            }

            try
            {
                // 如果没有选择本地 frpc.toml，生成并写入
                if (string.IsNullOrWhiteSpace(FrpcTomlPathBox.Text))
                {
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

                    string frpcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frpc.toml");
                    File.WriteAllText(frpcPath, frpcContent);
                }

                // 如果没有选择本地 frps.toml，生成并写入
                if (string.IsNullOrWhiteSpace(FrpsTomlPathBox.Text))
                {
                    string frpsContent = $"""
                    bindPort = {BindPortBox.Text}
                    vhostHTTPSPort = {VhostHTTPSPortBox.Text}
                    """;

                    string frpsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frps.toml");
                    File.WriteAllText(frpsPath, frpsContent);
                }

                IsConfigSaved = true;
                DialogResult = true;
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
