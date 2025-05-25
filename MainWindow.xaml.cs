using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

using VKLauncher.Models;

namespace VKLauncher
{
    public partial class MainWindow : Window
    {
        private static readonly string ConfigFile = "Config\\services.json";
        private List<McService> services = new();
        private McService? currentService;
        private Dictionary<string, Process> runningProcesses = new();
        private const string GroupFile = "Config\\service_groups.json";
        private List<ServiceGroup> serviceGroups = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadServices();
            LoadServiceGroups();
            GroupComboBox.ItemsSource = serviceGroups;

            StartAutoServices();
        }

        private void LoadServices()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    string json = File.ReadAllText(ConfigFile);
                    services = JsonSerializer.Deserialize<List<McService>>(json) ?? new();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("配置加载失败: " + ex.Message);
                services = new(); // 保证程序能继续运行
            }

            ServiceListBox.ItemsSource = null;
            ServiceListBox.ItemsSource = services;
            //ServiceListBox.DisplayMemberPath = "Name";
        }

        private void SaveServices()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile)!);

            string json = JsonSerializer.Serialize(services, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }

        private void LoadServiceGroups()
        {
            try
            {
                if (File.Exists(GroupFile))
                {
                    string json = File.ReadAllText(GroupFile);
                    serviceGroups = JsonSerializer.Deserialize<List<ServiceGroup>>(json) ?? new();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("服务组加载失败: " + ex.Message);
                serviceGroups = new();
            }
        }

        private void SaveServiceGroups()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GroupFile)!);

            string json = JsonSerializer.Serialize(serviceGroups, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GroupFile, json);
        }


        private void AddService_Click(object sender, RoutedEventArgs e)
        {
            var newService = new McService { Name = "新服务" };
            services.Add(newService);
            SaveServices();
            LoadServices();
            ServiceListBox.SelectedItem = newService;
        }

        private void DeleteService_Click(object sender, RoutedEventArgs e)
        {
            if (currentService != null)
            {
                services.Remove(currentService);
                SaveServices();
                currentService = null;
                LoadServices();
                ClearFields();
            }
        }

        private void BrowseJava_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Java 可执行文件 (*.exe)|*.exe",
                Title = "选择 Java 可执行文件"
            };

            if (dialog.ShowDialog() == true)
            {
                JavaBox.Text = dialog.FileName;
            }
        }

        private void BrowseJar_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JAR 文件 (*.jar)|*.jar",
                Title = "选择服务器 JAR 文件"
            };

            if (dialog.ShowDialog() == true)
            {
                JarBox.Text = dialog.FileName;

                // 自动获取上一级目录作为工作目录
                try
                {
                    string? jarDirectory = Path.GetDirectoryName(dialog.FileName);
                    if (!string.IsNullOrEmpty(jarDirectory))
                    {
                        WorkDirBox.Text = jarDirectory;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("自动获取工作目录失败: " + ex.Message);
                }
            }
        }

        private void BrowseWorkDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "选择服务器工作目录",
                UseDescriptionForTitle = true // 将 Description 用作窗口标题
            };

            if (dialog.ShowDialog() == true)
            {
                WorkDirBox.Text = dialog.SelectedPath;
            }
        }


        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            if (currentService == null) return;
            currentService.Name = NameBox.Text;
            currentService.JavaPath = JavaBox.Text;
            currentService.JarPath = JarBox.Text;
            currentService.WorkingDir = WorkDirBox.Text;
            currentService.AutoStart = AutoStartBox.IsChecked == true;
            currentService.UseFrp = UseFrpBox.IsChecked == true;
            SaveServices();
            LoadServices();
        }

        private void ServiceListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ServiceListBox.SelectedItem is McService service)
            {
                currentService = service;
                NameBox.Text = service.Name;
                JavaBox.Text = service.JavaPath;
                JarBox.Text = service.JarPath;
                WorkDirBox.Text = service.WorkingDir;
                AutoStartBox.IsChecked = service.AutoStart;
                UseFrpBox.IsChecked = service.UseFrp;
                ConsoleOutput.Clear();
            }
        }

        private void ClearFields()
        {
            NameBox.Text = "";
            JavaBox.Text = "";
            JarBox.Text = "";
            WorkDirBox.Text = "";
            AutoStartBox.IsChecked = false;
            ConsoleOutput.Clear();
        }

        private void StartAutoServices()
        {
            foreach (var service in services)
            {
                if (service.AutoStart && !runningProcesses.ContainsKey(service.Name))
                {
                    StartService(service);
                }
            }

            ConsoleOutput.AppendText($"[{DateTime.Now:T}] 自动启动已勾选自启动的服务\n");
        }


        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            if (currentService == null) return;

            StartService(currentService);
        }

        private void StartGroup_Click(object sender, RoutedEventArgs e)
        {
            if (GroupComboBox.SelectedItem is not ServiceGroup group)
            {
                MessageBox.Show("请选择一个服务组！");
                return;
            }

            foreach (var serviceName in group.ServiceNames)
            {
                var service = services.Find(s => s.Name == serviceName);
                if (service != null && !runningProcesses.ContainsKey(service.Name))
                {
                    StartService(service);
                }
            }

            MessageBox.Show($"服务组 [{group.GroupName}] 启动完成");
        }

        private void StartService(McService service)//复用该代码  单个&组服务
        {
            if (runningProcesses.ContainsKey(service.Name))
            {
                MessageBox.Show($"服务 [{service.Name}] 已在运行！");
                return;
            }

            // 如果启用了 FRP
            try
            {
                service.Start(); // 这将自动启动 frpc 或 frps，并启动 Minecraft 服务
                MessageBox.Show($"{service.Name} 的FRP已启动！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败：" + ex.Message);
            }

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = service.JavaPath,
                    Arguments = $"-jar \"{service.JarPath}\" nogui",
                    WorkingDirectory = service.WorkingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            proc.OutputDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Dispatcher.Invoke(() =>
                    {
                        ConsoleOutput.AppendText($"[{service.Name}] {args.Data}\n");
                        ConsoleOutput.ScrollToEnd();
                    });
                }
            };

            proc.ErrorDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Dispatcher.Invoke(() => ConsoleOutput.AppendText($"[ERR] [{service.Name}] {args.Data}\n"));
                }
            };

            try
            {
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                runningProcesses[service.Name] = proc;
                service.IsRunning = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动服务 [{service.Name}] 失败: " + ex.Message);
            }
        }

        private void StopServer_Click(object sender, RoutedEventArgs e)
        {
            if (currentService == null) return;

            StopService(currentService.Name);
            currentService.IsRunning = false;
            SaveServices();
            LoadServices();
        }

        private void StopGroup_Click(object sender, RoutedEventArgs e)
        {
            if (GroupComboBox.SelectedItem is not ServiceGroup group)
            {
                MessageBox.Show("请选择一个服务组！");
                return;
            }

            foreach (var serviceName in group.ServiceNames)
            {
                StopService(serviceName);
            }

            MessageBox.Show($"服务组 [{group.GroupName}] 停止完成");
        }

        private void StopService(string serviceName)//复用该代码  单个&组服务
        {
            // 停止 FRP 进程
            var service = services.Find(s => s.Name == serviceName);
            if (service != null)
            {
                try
                {
                    service.Stop();
                    MessageBox.Show($"{service.Name} 的FRP已停止！");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("停止 FRP 失败：" + ex.Message);
                }
            }
            
            if (runningProcesses.TryGetValue(serviceName, out var proc))
            {
                try
                {
                    proc.Kill(true); // true 表示连同子进程一起杀死
                    runningProcesses.Remove(serviceName);
                    ConsoleOutput.AppendText($"[{DateTime.Now:T}] 已停止服务：{serviceName}\n");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"停止服务 [{serviceName}] 失败: " + ex.Message);
                }
            }
        }


        private void ManageGroups_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前所有服务的名称列表（供编辑器选择）
            var allServices = services.Select(s => s.Name).ToList();

            // 打开服务组管理窗口
            var window = new ManageGroupsWindow(serviceGroups, allServices);
            if (window.ShowDialog() == true)
            {
                // 如果在管理器中保存了修改，更新 serviceGroups，并保存到文件
                serviceGroups = window.UpdatedGroups;
                SaveServiceGroups();

                // 更新下拉列表
                GroupComboBox.ItemsSource = null;
                GroupComboBox.ItemsSource = serviceGroups;
            }
        }

        private void ConfigureFrp_Click(object sender, RoutedEventArgs e)
        {
            if (currentService == null)
            {
                MessageBox.Show("请先选择一个服务！");
                return;
            }
            var configWindow = new FrpConfigWindow();


            if (configWindow.ShowDialog() == true && currentService != null)
            {
                // 用户保存配置后，从窗口中获取数据
                // 保存 frpc.exe 路径（若不为空则更新）
                if (!string.IsNullOrWhiteSpace(configWindow.FrpcPathBox.Text))
                {
                    currentService.FrpcExePath = configWindow.FrpcPathBox.Text;
                }

                // 保存 frpc.toml 路径（若文本框不为空则更新，否则保留旧值）
                if (!string.IsNullOrWhiteSpace(configWindow.FrpcTomlPathBox.Text))
                {
                    currentService.FrpcTomlPath = configWindow.FrpcTomlPathBox.Text;
                }
                else if (string.IsNullOrWhiteSpace(currentService.FrpcTomlPath))
                {
                    // 初次设置默认值
                    currentService.FrpcTomlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frpc.toml");
                }

                // 保存 frps.exe 路径（若不为空则更新）
                if (!string.IsNullOrWhiteSpace(configWindow.FrpsPathBox.Text))
                {
                    currentService.FrpsExePath = configWindow.FrpsPathBox.Text;
                }

                // 保存 frps.toml 路径（若文本框不为空则更新，否则保留旧值）
                if (!string.IsNullOrWhiteSpace(configWindow.FrpsTomlPathBox.Text))
                {
                    currentService.FrpsTomlPath = configWindow.FrpsTomlPathBox.Text;
                }
                else if (string.IsNullOrWhiteSpace(currentService.FrpsTomlPath))
                {
                    // 初次设置默认值
                    currentService.FrpsTomlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frps.toml");
                }

                // 开启 FRP 功能
                currentService.UseFrp = true;


            }
            SaveServices();
        }

    }
}
