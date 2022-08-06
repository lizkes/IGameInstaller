using System;
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
using System.Net.Http;
using IGameInstaller.IGameException;

namespace IGameInstaller
{
    public partial class MainWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static CancellationTokenSource installCancelSource;

        public MainWindow()
        {
                Title = $"IGame安装器 V{App.Version.Major}.{App.Version.Minor}.{App.Version.Build}";
                InitializeComponent();
                InitializeAsync();
        }

        async void InitializeAsync()
        {
            try
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
            catch (Exception ex)
            {
                ProcessException("未处理错误", ex).Wait();
            }
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
                var downloadUrl = recvMessage.Payload;
                var currentSV = SystemInfoHelper.GetCurrentSystemVersion();
                if (downloadUrl.Contains("sharepoint.com") && (currentSV == SystemVersion.Win7x32 || currentSV == SystemVersion.Win7x64))
                {
                    WebSendMessage.SendSetError("无法访问服务器", "您当前是Windows 7系统\n由于微软已经停止维护，无法使用更安全的TLS加密算法\n因此您无法访问普通下载的服务器\n您可以使用快速下载或者升级为Windows 10系统");
                    return;
                }

                WindowHelper.DisableWindowCloseButton();
                try
                {
                    WebSendMessage.SendSetProgress("正在初始化下载引擎...", "", -1);
                    installCancelSource = new CancellationTokenSource();
                    await InstallHelper.InstallMainAsync(downloadUrl, App.InstallConfig.InstallPath, installCancelSource.Token);
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
                        await InstallHelper.InstallDependsAsync(App.ResourceInstallInfo.RequireDepends, installCancelSource.Token);
                    }
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
            var baseException = ex.GetBaseException();
            if (baseException is TaskCanceledException)
            {
                WebSendMessage.SendSetError("网络连接超时，请稍后再试", $"错误信息：{baseException.Message}");
            }
            else if (baseException is HttpRequestException)
            {
                WebSendMessage.SendSetError("网络请求失败，请稍后再试", $"错误信息：{baseException.Message}");
            }
            else if (baseException is DownloadExtractCanceledException)
            {
                //如果是下载被取消，什么也不做
                return;
            }
            else
            {
                WebSendMessage.SendSetError(message, $"错误信息：{baseException.Message}");
            }

            Logger.Error(message + "\n错误信息：{BaseExceptionMessage}\n尝试安装的资源ID: {resourceId}\nn错误类型: {BaseExceptionType}\n内部错误: {InnerException}\n错误堆栈：{StackTrace}", baseException.Message, App.ResourceInstallInfo.Id, baseException.GetType(), ex.InnerException, ex.StackTrace);
            await IGameApiHelper.ErrorCollect($"{message}\n错误信息：{baseException.Message}\n尝试安装的资源ID: {App.ResourceInstallInfo.Id}\n错误类型: {baseException.GetType()}\n内部错误: {ex.InnerException}\n错误堆栈：{ex.StackTrace}");
        }
    }
}
