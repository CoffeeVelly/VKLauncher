using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Diagnostics;


namespace VKLauncher.Models
{
    public class McService: INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        public string JavaPath { get; set; } = "";
        public string JarPath { get; set; } = "";
        public string WorkingDir { get; set; } = "";
        public bool AutoStart { get; set; } = false;

        public string FrpcExePath { get; set; } = "";
        public string FrpcTomlPath { get; set; } = "";

        public string FrpsExePath { get; set; } = "";
        public string FrpsTomlPath { get; set; } = "";

        public bool UseFrp { get; set; } = false;

        public void Start()
        {
            if (UseFrp)
            {
                StartFrpc();
                MessageBox.Show($"{Name} 的FRP已启动！");
            }
        }

        private void StartFrpc()
        {
            if (!File.Exists(FrpcExePath) || !File.Exists(FrpcTomlPath))
            {
                MessageBox.Show("frpc.exe 或 frpc.toml 不存在！");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = FrpcExePath,
                Arguments = $"-c \"{FrpcTomlPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        private void StartFrps()
        {
            if (!File.Exists(FrpsExePath) || !File.Exists(FrpsTomlPath))
            {
                MessageBox.Show("frps.exe 或 frps.toml 不存在！");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = FrpsExePath,
                Arguments = $"-c \"{FrpsTomlPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        public void Stop()
        {
            if (UseFrp)
            {
                StopProcessByName("frpc");
                StopProcessByName("frps");
                MessageBox.Show($"{Name} 的FRP已停止！");
            }

            // 你还可以在这里添加停止 MC 服务的逻辑（如 Java 进程终止）
            IsRunning = false;
        }

        /// <summary>
        /// 根据进程名结束对应进程
        /// </summary>
        private void StopProcessByName(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    if (!string.IsNullOrEmpty(process.MainModule?.FileName) &&
                        (process.MainModule.FileName.Equals(FrpcExePath, StringComparison.OrdinalIgnoreCase) ||
                        process.MainModule.FileName.Equals(FrpsExePath, StringComparison.OrdinalIgnoreCase)))
                    {
                        process.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止 {processName} 进程时出错：{ex.Message}");
            }
        }


        private bool isRunning;
        public bool IsRunning
        {
            get => isRunning;
            set
            {
                if (isRunning != value)
                {
                    isRunning = value;
                    OnPropertyChanged(nameof(IsRunning));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ServiceConfig : INotifyPropertyChanged
    {
        private string name = string.Empty;
        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private string path = string.Empty;
        public string Path
        {
            get => path;
            set
            {
                if (path != value)
                {
                    path = value;
                    OnPropertyChanged(nameof(Path));
                }
            }
        }

        private bool isRunning;
        public bool IsRunning
        {
            get => isRunning;
            set
            {
                if (isRunning != value)
                {
                    isRunning = value;
                    OnPropertyChanged(nameof(IsRunning));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ServiceGroup
    {
        public string GroupName { get; set; } = string.Empty;
        public List<string> ServiceNames { get; set; } = new(); // 引用 McService.Name

        public override string ToString()
        {
            return GroupName;
        }
    }

    // public class FrpConfig
    // {
    //     public string FrpcPath { get; set; } = "";
    //     public string FrpcTomlPath { get; set; } = "";
    //     public string FrpsPath { get; set; } = "";
    //     public string FrpsTomlPath { get; set; } = "";
    // }

}
