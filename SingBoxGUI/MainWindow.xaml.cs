using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification;
using Nett;

namespace SingBoxGUI
{
    public partial class MainWindow : Window
    {
        string configFilePath = "gui_config.toml";

        private const int ConsoleOutputMaxLines = 1000;

        private static Mutex mutex = new Mutex(true, "{E2B43F6B-CF21-4ED9-8D4B-19D15E862D61}");

        private Process consoleProcess;
        private bool isHidden = false;
        private TaskbarIcon taskbarIcon;
        private TomlTable config;

        private System.Windows.Controls.MenuItem startWithWindowsMenuItem;
        private System.Windows.Controls.MenuItem setProxyMenuItem;

        public MainWindow()
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                InitializeComponent();
                config = LoadConfig();

                // 将 MainWindow_Closing 方法附加到 Closing 事件
                Closing += MainWindow_Closing;

                if (config.Get<bool>("AutoStart"))
                {
                    SetStartup(true);
                }

                if (config.Get<bool>("AutoProxy"))
                {
                    SetProxy(config.Get<string>("ProxyAddress"));
                }

                taskbarIcon = new TaskbarIcon();
                taskbarIcon.Icon = Properties.Resources.TrayIcon;
                taskbarIcon.ToolTipText = "SingBoxGUI";
                taskbarIcon.DoubleClickCommand = new RelayCommand(ShowMainWindow);
                taskbarIcon.ContextMenu = new System.Windows.Controls.ContextMenu();

                var openDashboardMenuItem = new System.Windows.Controls.MenuItem
                {
                    Header = "打开仪表盘",
                    Command = new RelayCommand(() => OpenDashboard(config.Get<string>("DashboardURL")))
                };

                startWithWindowsMenuItem = new System.Windows.Controls.MenuItem
                {
                    Header = "设置开机自启",
                    IsCheckable = true,
                    IsChecked = config.Get<bool>("AutoStart"),
                    Command = new RelayCommand(() => ToggleStartup(startWithWindowsMenuItem.IsChecked))
                };

                setProxyMenuItem = new System.Windows.Controls.MenuItem
                {
                    Header = "设置系统代理",
                    IsCheckable = true,
                    IsChecked = config.Get<bool>("AutoProxy"),
                    Command = new RelayCommand(() => ToggleProxy(setProxyMenuItem.IsChecked))
                };

                var exitMenuItem = new System.Windows.Controls.MenuItem
                {
                    Header = "退出",
                    Command = new RelayCommand(ExitApplication)
                };

                taskbarIcon.ContextMenu.Items.Add(openDashboardMenuItem);
                taskbarIcon.ContextMenu.Items.Add(startWithWindowsMenuItem);
                taskbarIcon.ContextMenu.Items.Add(setProxyMenuItem);
                taskbarIcon.ContextMenu.Items.Add(exitMenuItem);

                StartConsoleProcess();
                HideMainWindow();
            }
            else
            {
                // 如果互斥体已被占用，说明已经有一个实例在运行，不启动新的实例
                System.Windows.MessageBox.Show("已经有一个SingBoxGUI实例在运行", "SingBoxGUI", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // 取消窗口的默认关闭操作
            HideMainWindow(); // 隐藏窗口
        }

        private void StartConsoleProcess()
        {
            string consoleAppFileName = config.Get<string>("ConsoleAppFileName");

            if (IsProcessRunning(consoleAppFileName))
            {
                var result = System.Windows.MessageBox.Show($"A process with the name '{consoleAppFileName}' is already running. Do you want to terminate it first?",
                    "进程已运行", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    TerminateProcessByName(consoleAppFileName);
                }
            }
            consoleProcess = new Process();
            consoleProcess.StartInfo.FileName = config.Get<string>("ConsoleAppFileName");
            consoleProcess.StartInfo.Arguments = config.Get<string>("ConsoleAppArguments");
            consoleProcess.StartInfo.UseShellExecute = false;
            consoleProcess.StartInfo.CreateNoWindow = true;
            consoleProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            consoleProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            consoleProcess.StartInfo.RedirectStandardOutput = true;
            consoleProcess.StartInfo.RedirectStandardError = true;
            consoleProcess.OutputDataReceived += (sender, e) => Dispatcher.Invoke(() => HandleConsoleOutput(e.Data));
            consoleProcess.ErrorDataReceived += (sender, e) => Dispatcher.Invoke(() => HandleConsoleOutput(e.Data));
            consoleProcess.Start();
            consoleProcess.BeginOutputReadLine();
            consoleProcess.BeginErrorReadLine();
        }

        private bool IsProcessRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));
            return processes.Length > 0;
        }

        private void TerminateProcessByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    // 处理终止进程时可能出现的异常
                    System.Windows.MessageBox.Show($"Failed to terminate process '{processName}': {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void HandleConsoleOutput(string output)
        {
            // 处理控制台输出
            if (ConsoleOutputTextBox.LineCount >= ConsoleOutputMaxLines)
            {
                int numLinesToRemove = ConsoleOutputTextBox.LineCount - ConsoleOutputMaxLines + 1;
                int indexToRemove = ConsoleOutputTextBox.GetCharacterIndexFromLineIndex(numLinesToRemove);
                ConsoleOutputTextBox.Select(0, indexToRemove);
                ConsoleOutputTextBox.SelectedText = string.Empty;
            }
            // 处理控制台输出
            ConsoleOutputTextBox.AppendText(output + Environment.NewLine);
            ConsoleOutputTextBox.ScrollToEnd();
        }
        private void ToggleStartup(bool startWithWindows)
        {
            config.Update("AutoStart", startWithWindows);
            SaveConfig();
            SetStartup(startWithWindows);
        }

        private void ToggleProxy(bool setProxy)
        {
            config.Update("AutoProxy", setProxy);
            SaveConfig();
            if (setProxy)
            {
                // 设置系统代理的代码
                string proxyAddress = config.Get<string>("ProxyAddress");
                if(string.IsNullOrEmpty(proxyAddress)) {
                    System.Windows.MessageBox.Show("ProxyAddress配置为空！");
                } 
                else
                {
                    SetProxy(proxyAddress);
                }
            }
            else
            {
                // 取消系统代理的代码
                SetProxy(null);
            }
        }

        private void SaveConfig()
        {
            Toml.WriteFile(config, configFilePath);
        }


        private void ExitApplication()
        {
            // 退出软件
            if (consoleProcess != null && !consoleProcess.HasExited)
            {
                consoleProcess.Kill();
            }

            System.Windows.Application.Current.Shutdown();
        }


        private void SetStartup(bool startWithWindows)
        {
            string exePath = Assembly.GetExecutingAssembly().Location;

            string startupName = config.Get<string>("StartupItemName");
            // 设置或取消开机自启
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (startWithWindows)
                {
                    key.SetValue(startupName, exePath);
                }
                else
                {
                    key.DeleteValue(startupName, false);
                }
            }
            
        }

        private void SetProxy(string proxyAddress)
        {
            // 设置或取消系统代理
            // 设置 Windows 系统的全局代理
            const string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
            const string proxyServerValueName = "ProxyServer";
            const string proxyEnableValueName = "ProxyEnable";

            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryKeyPath, true))
            {
                if (string.IsNullOrEmpty(proxyAddress))
                {
                    // 取消代理
                    key.SetValue(proxyServerValueName, "");
                    key.SetValue(proxyEnableValueName, 0);
                }
                else
                {
                    // 启用代理
                    key.SetValue(proxyServerValueName, proxyAddress);
                    key.SetValue(proxyEnableValueName, 1);
                }
            }
        }

        private void OpenDashboard(string url)
        {
            // 打开仪表盘URL
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // 处理异常，例如 URL 无效或没有默认浏览器时
                System.Windows.MessageBox.Show($"Error opening the dashboard: {ex.Message}");
            }
        }

        private void HideMainWindow()
        {
            // 隐藏主窗口
            Hide();
            isHidden = true;
        }

        private void ShowMainWindow()
        {
            // 显示或隐藏主窗口
            if (isHidden)
            {
                Show();
                isHidden = false;
                WindowState = WindowState.Normal;
            }
            else
            {
                HideMainWindow();
            }
        }
        private TomlTable LoadConfig()
        {
            // 读取配置文件或使用默认配置
            TomlTable defaultConfig = Toml.ReadString(@"
        ConsoleAppFileName = 'sing-box.exe'
        ConsoleAppArguments = '-c config.json run'
        AutoStart = false
        StartupItemName = 'SingBoxGUI'
        AutoProxy = false
        ProxyAddress = '127.0.0.1:2080'
        DashboardURL = 'http://yacd.haishan.me'
    ");

            if (File.Exists(configFilePath))
            {
                try
                {
                    TomlTable fileConfig = Toml.ReadFile(configFilePath);

                    // 手动合并配置
                    foreach (var kvp in defaultConfig)
                    {
                        if (!fileConfig.ContainsKey(kvp.Key))
                        {
                            fileConfig.Add(kvp.Key, kvp.Value);
                        }
                    }

                    return fileConfig;
                }
                catch (Exception)
                {
                    // 配置文件无效，使用默认配置
                    return defaultConfig;
                }
            }
            else
            {
                // 配置文件不存在，使用默认配置
                return defaultConfig;
            }
        }
    }

    public class RelayCommand : System.Windows.Input.ICommand
    {
        private Action action;

        public RelayCommand(Action action)
        {
            this.action = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            action();
        }
    }
}
