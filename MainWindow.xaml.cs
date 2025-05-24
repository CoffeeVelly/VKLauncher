using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using VKLauncher.Models;

namespace VKLauncher
{
    public partial class MainWindow : Window
    {
        private const string ConfigFile = "Config\\services.json";
        private List<McService> services = new();
        private McService? currentService;
        private Dictionary<string, Process> runningProcesses = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadServices();
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
            string json = JsonSerializer.Serialize(services, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }

        private void AddService_Click(object sender, RoutedEventArgs e)
        {
            var newService = new McService { Name = "新服务" };
            services.Add(newService);
            LoadServices();
            ServiceListBox.SelectedItem = newService;
        }

        private void DeleteService_Click(object sender, RoutedEventArgs e)
        {
            if (currentService != null)
            {
                services.Remove(currentService);
                currentService = null;
                LoadServices();
                ClearFields();
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

        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            if (currentService == null) return;

            if (runningProcesses.ContainsKey(currentService.Name))
            {
                MessageBox.Show("该服务已在运行！");
                return;
            }

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = currentService.JavaPath,
                    Arguments = $"-jar \"{currentService.JarPath}\" nogui",
                    WorkingDirectory = currentService.WorkingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            proc.OutputDataReceived += (s, args) =>
            {
                if (args.Data != null)
                    Dispatcher.Invoke(() => ConsoleOutput.AppendText(args.Data + "\n"));
            };
            proc.ErrorDataReceived += (s, args) =>
            {
                if (args.Data != null)
                    Dispatcher.Invoke(() => ConsoleOutput.AppendText("[ERR] " + args.Data + "\n"));
            };

            try
            {
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                runningProcesses[currentService.Name] = proc;
                currentService.IsRunning = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败: " + ex.Message);
            }
        }

        private void StopServer_Click(object sender, RoutedEventArgs e)
        {
            if (currentService == null) return;

            if (runningProcesses.TryGetValue(currentService.Name, out var proc))
            {
                try
                {
                    proc.Kill(true); // true 表示连同子进程一起杀死
                    runningProcesses.Remove(currentService.Name);
                    ConsoleOutput.AppendText($"[{DateTime.Now:T}] 已停止服务：{currentService.Name}\n");
                    currentService.IsRunning = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("停止失败: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("当前服务未在运行中。");
            }
        }

    }
}
