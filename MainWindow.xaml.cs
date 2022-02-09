﻿using System;
using System.IO;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Microsoft.Web.WebView2.Core;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;

using IGameInstaller.Model;
using IGameInstaller.Helper;
using IGameInstaller.Extension;


namespace IGameInstaller
{
    public partial class MainWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static CancellationTokenSource installCancelSource;

        public MainWindow()
        {
            Title = $"IGame安装器 v{App.Version}";
            InitializeComponent();
            InitializeAsync();
        }

        async void InitializeAsync()
        {
            App.WebView = webView;
            var userDataFolder = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Infinite Dreams\IGameInstaller";
            var webView2Env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            // 启动webview2
            await webView.EnsureCoreWebView2Async(webView2Env);
            // 闪屏消失
            App.SplashScreen.Close(TimeSpan.FromSeconds(0.2));
            // 丑陋的实现，隐藏闪屏显示时的Webview2窗口
            this.MoveToCenter(1200, 800);
            Width = 1200;
            Height = 800;
            MinWidth = 900;
            MinHeight = 600;
            // 使窗口置顶
            Topmost = true;
            Topmost = false;
#if DEBUG
            webView.Source = new Uri("http://localhost:8500/prepare");
            //var webDirPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");
            //webView.CoreWebView2.SetVirtualHostNameToFolderMapping("IGameInstaller.web", webDirPath, CoreWebView2HostResourceAccessKind.Allow);
            //webView.Source = new Uri("https://IGameInstaller.web/index.html");
#else
            var webDirPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping("IGameInstaller.web", webDirPath, CoreWebView2HostResourceAccessKind.Allow);
            webView.Source = new Uri("https://IGameInstaller.web/index.html");
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
#endif
            webView.CoreWebView2.WebMessageReceived += ProcessMessage;
        }

        private async void ProcessMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var recvMessageJsonString = e.TryGetWebMessageAsString();
            var recvMessage = WebRecvMessage.FromJsonString(recvMessageJsonString);
            if (recvMessage.Type == WebRecvMessageType.StartPrepare)
            {
                // 获取资源ID
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length == 2)
                {
                    try
                    {
                        App.resourceId = Convert.ToInt32(args[1]);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"传入参数解析不正确\n错误信息：{ex.Message}", "IGame安装器错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown(2);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("传入参数数量不正确", "IGame安装器错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown(1);
                    return;
                }

                // 检查网络
                WebSendMessage.SendSetPrompt("正在检查网络连接...");
                var networkIsOk = await HttpClientHelper.CheckNetworkAsync();
                if (!networkIsOk)
                {
                    WebSendMessage.SendSetError("网络故障", "本软件需要网络来正常运行\n请联网后重新打开本软件");
                    Logger.Debug("网络故障\n本软件需要网络来正常运行\n请联网后重新打开本软件");
                    return;
                }

                // 检测版本
                var needUpdate = false;
                try
                {
                    WebSendMessage.SendSetPrompt("正在检查软件版本...");
                    needUpdate = await UpdateHelper.NeedUpdateAsync();
                }
                catch (Exception ex)
                {
                    await ProcessException("获取版本信息失败", ex);
                    return;
                }

                // 尝试更新
                try
                {
                    if (needUpdate)
                    {
                        WebSendMessage.SendStartUpdate();
                        await UpdateHelper.UpdateAsync();
                    }
                }
                catch (Exception ex)
                {
                    await ProcessException("程序更新失败", ex);
                    return;
                }

                // 获取 resource installInfo
                try
                {
                    WebSendMessage.SendSetPrompt("正在获取资源安装配置...");
                    App.ResourceInstallInfo = await IGameApiHelper.GetResourceInstallInfo(App.resourceId);
                    WebSendMessage.SendSetResourceInstallInfo(App.ResourceInstallInfo);
                } 
                catch (Exception ex)
                {
                    await ProcessException("获取资源安装配置失败", ex);
                    return;
                }

                // check os
                    WebSendMessage.SendSetPrompt("正在检查系统配置...");
                var currentSV = SystemInfoHelper.GetCurrentSystemVersion();
                var match = false;
                foreach(var requireSV in App.ResourceInstallInfo.RequireSystems)
                {
                    if (currentSV == requireSV)
                    {
                        match = true;
                        break;
                    }
                }
                if (match)
                {
                    new WebSendMessage(WebSendMessageType.PrepareDone).Send();
                }
                else
                {
                    var typeString = App.ResourceInstallInfo.Type == ResourceType.Game ? "游戏" : "拓展";
                    var readableCurrentSV = SystemInfoHelper.SystemVersionToReadable(currentSV);
                    var readableRequireSV = string.Join(" 或者 ", App.ResourceInstallInfo.RequireSystems.Select(sv => SystemInfoHelper.SystemVersionToReadable(sv)).ToArray());
                    WebSendMessage.SendSetError($"不满足安装该{typeString}的配置要求", $"您当前的操作系统为{readableCurrentSV}\n安装该{typeString}所需的操作系统为{readableRequireSV}\n请更新Windows版本后重试");
                    Logger.Debug($"不满足安装该{typeString}的配置要求\n您当前的操作系统为{readableCurrentSV}\n安装该{typeString}所需的操作系统为{readableRequireSV}\n请更新Windows版本后重试");
                    return;
                }
            }
            else if (recvMessage.Type == WebRecvMessageType.GenerateInstallConfig)
            {
                App.InstallConfig = new InstallConfig();
                WebSendMessage.SendSetInstallConfig(App.InstallConfig);
            } 
            else if (recvMessage.Type == WebRecvMessageType.GetInstallPath)
            {
                string pathDriver = App.InstallConfig.InstallPath.Substring(0, 3);
                var dlg = new CommonOpenFileDialog
                {
                    Title = "选择安装路径",
                    IsFolderPicker = true,
                    InitialDirectory = pathDriver,
                    AddToMostRecentlyUsedList = false,
                    AllowNonFileSystemItems = false,
                    DefaultDirectory = pathDriver,
                    EnsureFileExists = true,
                    EnsurePathExists = true,
                    EnsureReadOnly = false,
                    EnsureValidNames = true,
                    Multiselect = false,
                    ShowPlacesList = true
                };
                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string fixFileName = dlg.FileName;
                    if (dlg.FileName.EndsWith(@"\")) fixFileName = fixFileName.Remove(fixFileName.Length - 1);
                    if (App.ResourceInstallInfo.Type == ResourceType.Expansion)
                    {
                        App.InstallConfig.SetPath(fixFileName);
                    } 
                    else
                    {
                        string path = $@"{fixFileName}\{App.ProjectName}\{App.ResourceInstallInfo.EnglishName}";
                        App.InstallConfig.SetPath(path);
                    }
                    WebSendMessage.SendSetInstallConfig(App.InstallConfig);
                }
            }
            else if (recvMessage.Type == WebRecvMessageType.SetInstallConfig)
            {
                App.InstallConfig = recvMessage.DeserializePayload<InstallConfig>();
            }
            else if (recvMessage.Type == WebRecvMessageType.TaskCancel)
            {
                installCancelSource.Cancel();
            }
            else if (recvMessage.Type == WebRecvMessageType.StartDownload)
            {
                WindowHelper.DisableWindowCloseButton();

                try
                {
                    WebSendMessage.SendSetProgress("正在初始化下载引擎...", "", -1);
                    var downloadUrl = recvMessage.Payload;
                    installCancelSource = new CancellationTokenSource();
                    await InstallHelper.InstallMainAsync(downloadUrl, App.InstallConfig.InstallPath, installCancelSource.Token);
                }
                catch (AggregateException ae)
                {
                    foreach (Exception ex in ae.InnerExceptions)
                    {
                        if (ex is TaskCanceledException)
                        {
                            WindowHelper.EnableWindowCloseButton();
                            return;
                        }
                    }
                    await ProcessException("下载引擎错误", ae);
                    WindowHelper.EnableWindowCloseButton();
                    return;
                }
                catch (Exception ex)
                {
                    await ProcessException("下载引擎错误", ex);
                    WindowHelper.EnableWindowCloseButton();
                    return;
                }

                try
                {
                    if (App.ResourceInstallInfo.RequireDepends.Length > 0)
                    {
                        WebSendMessage.SendSetProgress("正在准备安装必要的运行环境...", "", -1);
                        installCancelSource = new CancellationTokenSource();
                        //await InstallHelper.InstallDependsAsync(new Depend[] { Depend.VC2005, Depend.VC2008, Depend.VC2010, Depend.VC2012, Depend.VC2013, Depend.VC2015, Depend.VC2017, Depend.VC2015to2022, Depend.ParadoxLauncher, Depend.ParadoxLauncherCrack, Depend.DirectX9 });
                        await InstallHelper.InstallDependsAsync(App.ResourceInstallInfo.RequireDepends, installCancelSource.Token);
                    }
                }
                catch (AggregateException ae)
                {
                    foreach (Exception ex in ae.InnerExceptions)
                    {
                        if (ex is TaskCanceledException)
                        {
                            WindowHelper.EnableWindowCloseButton();
                            return;
                        }
                    }
                    await ProcessException("安装运行环境失败", ae);
                    WindowHelper.EnableWindowCloseButton();
                    return;
                }
                catch (Exception ex)
                {
                    await ProcessException("安装运行环境失败", ex);
                    WindowHelper.EnableWindowCloseButton();
                    return;
                }

                try
                {
                    var scriptPath = Path.Combine(App.InstallConfig.InstallPath, "IGame", "Install.cs");
                    if (File.Exists(scriptPath))
                    {
                        WebSendMessage.SendSetProgress("正在初始化脚本引擎...", "", -1);
                        await InstallHelper.ExecuteScriptAsync(scriptPath);
                    }
                }
                catch (AggregateException ae)
                {
                    foreach (Exception ex in ae.InnerExceptions)
                    {
                        if (ex is TaskCanceledException)
                        {
                            WindowHelper.EnableWindowCloseButton();
                            return;
                        }
                    }
                    await ProcessException("脚本引擎错误", ae);
                    WindowHelper.EnableWindowCloseButton();
                    return;
                }
                catch (Exception ex)
                {
                    await ProcessException("脚本引擎错误", ex);
                    WindowHelper.EnableWindowCloseButton();
                    return;
                }

                try
                {
                    WebSendMessage.SendSetProgress("正在进行收尾工作...", "", -1);
                    await InstallHelper.FinalWork();
                }
                catch (AggregateException ae)
                {
                    foreach (Exception ex in ae.InnerExceptions)
                    {
                        if (ex is TaskCanceledException)
                        {
                            WindowHelper.EnableWindowCloseButton();
                            return;
                        }
                    }
                    await ProcessException("执行收尾工作失败", ae);
                    WindowHelper.EnableWindowCloseButton();
                    return;
                }
                catch (Exception ex)
                {
                    await ProcessException("执行收尾工作失败", ex);
                    WindowHelper.EnableWindowCloseButton();
                    return;
                }

                WindowHelper.EnableWindowCloseButton();
                new WebSendMessage(WebSendMessageType.InstallDone).Send();
            }
            else if (recvMessage.Type == WebRecvMessageType.OpenBrowser)
            {
                ProcessHelper.StartProcess(recvMessage.Payload, "", true, false, "", "");
            }
            else if (recvMessage.Type == WebRecvMessageType.OpenGame)
            {
                var workDirPath = App.InstallConfig.InstallPath;
                if (App.ResourceInstallInfo.WorkDirPath != null && App.ResourceInstallInfo.WorkDirPath != "")
                {
                    workDirPath = Path.Combine(App.InstallConfig.InstallPath, App.ResourceInstallInfo.WorkDirPath);
                }
                var shortCutArgument = "";
                if (App.ResourceInstallInfo.WorkDirPath != null && App.ResourceInstallInfo.WorkDirPath != "")
                {
                    shortCutArgument = App.ResourceInstallInfo.ShortCutArgument;
                }
                ProcessHelper.StartProcess(Path.Combine(App.InstallConfig.InstallPath, App.ResourceInstallInfo.ExePath), shortCutArgument, false, false, workDirPath, "");
            }
            else if (recvMessage.Type == WebRecvMessageType.Exit)
            {
                Application.Current.Shutdown(0);
            }
        }

        private async Task ProcessException(string message, Exception ex)
        {
            WebSendMessage.SendSetError(message, $"错误信息：{ex.Message}\n原始错误信息：{ex.GetBaseException().Message}");
            Logger.Error(message + "\n错误信息：{Message}\n错误类型: {Type}\n原始错误信息：{BaseExceptionMessage}\nn原始错误类型: {BaseExceptionType}\n错误堆栈：{StackTrace}", ex.Message, ex.GetType(), ex.GetBaseException().Message, ex.GetBaseException().GetType(), ex.StackTrace);
            await IGameApiHelper.ErrorCollect($"{message}\n错误信息：{ex.Message}\n错误类型: {ex.GetType()}\n原始错误信息：{ex.GetBaseException().Message}\n原始错误类型: {ex.GetBaseException().GetType()}\n错误堆栈：{ex.StackTrace}");
        }
    }
}
