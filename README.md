# SingBoxGUI

SingBoxGUI 是一个基于 C# 和 WPF 的开源项目，用于管理和控制 sing-box 程序的图形用户界面 (GUI)。该应用程序允许用户以可视化的方式管理控制台程序的运行和配置。

## 功能

SingBoxGUI 提供以下主要功能：

- 启动和管理 sing-box 程序，并实时显示其控制台输出。
- 将应用程序最小化到系统托盘，以便轻松访问。
- 托盘图标上右键单击菜单包括：
  - 打开仪表盘URL。
  - 设置或取消开机自启。
  - 设置或取消 Windows 系统网络代理配置为指定地址。
  - 退出应用程序。

## 使用

1. 下载最新版本的 SingBoxGUI。
2. 修改config.json的outbounds出站服务器为可用的。
3. 启动 SingBoxGUI。
4. sing-box 程序将自动启动，并将其控制台输出显示在主窗口，双击托盘图标显示或隐藏主窗口。
5. 使用托盘图标上的右键单击菜单来配置应用程序和访问其他功能。

## 配置

SingBoxGUI 使用 TOML 格式的配置文件来自定义行为。默认配置如下：

```toml
ConsoleAppFileName = "sing-box.exe"
ConsoleAppArguments = "-c config.json run"
AutoStart = false
StartupItemName = "SingBoxGUI"
AutoProxy = false
ProxyAddress = "127.0.0.1:2080"
DashboardURL = "http://yacd.haishan.me"
```

你可以通过编辑 `gui_config.toml` 文件来更改这些配置项，或者使用应用程序的图形界面来自定义配置。

## 贡献

如果你想为 SingBoxGUI 做出贡献，欢迎提出问题 (Issues) 或提交拉取请求 (Pull Requests)。我们欢迎并鼓励社区的参与，一起改进这个项目。

## 许可证

SingBoxGUI 采用 MIT 许可证。
