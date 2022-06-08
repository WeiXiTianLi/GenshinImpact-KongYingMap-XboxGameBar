﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Microsoft.Gaming.XboxGameBar;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.ApplicationModel;
using Windows.UI.Popups;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace 空荧酒馆_悬浮窗
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class YuanShenSite_悬浮窗 : Page
    {
        public string token = "";
        string urlws = "ws://localhost:32333/ws/";
        public Windows.Networking.Sockets.MessageWebSocket messageWebSocket;

        public bool isInGameBar = false;
        public bool hasInputBox = false;
        public int origWidth = 0;
        public int origHeight = 0;
        public bool isOversea = false;
        public string mapCN = "https://yuanshen.site/index.html";
        public string mapOS = "https://yuanshen.site/index_en.html";
        public XboxGameBarWidget gamebarWindow = null;

        public YuanShenSite_悬浮窗()
        {
            this.InitializeComponent();
            WebView2Init();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var param = e.Parameter;
            if (param != null && typeof(XboxGameBarWidget) == param.GetType())
            {
                isInGameBar = true;
                gamebarWindow=param as XboxGameBarWidget;
            }
        }

        public IAsyncAction OpenInFullTrust(string url)
        {
            ApplicationData.Current.LocalSettings.Values["parameters"] = url;
            return FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        /// <summary>
        /// 切换最大化模式
        /// </summary>
        public async void ToggleMaximize()
        {
            if(gamebarWindow==null)return;
            if (origHeight > 0 && origWidth > 0)
            {
                Size size = new Size(origWidth, origHeight);
                bool res = await gamebarWindow.TryResizeWindowAsync(size);
                await webView.CoreWebView2.ExecuteScriptAsync("consle.log('RESIZE:" + res + "')");
                origWidth = 0;
                origHeight = 0;
            }
            else
            {
                string ret=await webView.CoreWebView2.ExecuteScriptAsync("({w:window.innerWidth,h:window.innerHeight,mh:screen.availHeight-100,mw:screen.availWidth*0.9})");
                var json = JsonObject.Parse("ret");
                origHeight = (int) json.GetNamedNumber("h");
                origWidth = (int) json.GetNamedNumber("w");
                Size size = new Size(json.GetNamedNumber("mw"), json.GetNamedNumber("mh"));
                bool res = await gamebarWindow.TryResizeWindowAsync(size);
                await webView.CoreWebView2.ExecuteScriptAsync("consle.log('RESIZE:" + res + "')");
            }
        }

        /// <summary>
        /// 初始化webview2的js
        /// </summary>
        public async void WebView2Init()
        {
            await webView.EnsureCoreWebView2Async();
            // 打开地图的网站
            //webView.CoreWebView2.Navigate(isOversea ? mapOS : mapCN);

            //s.src = 'http://8.134.219.60:5244/WeiXiTianLi/download/KYJG-XuanFuChuang/js/last/cvAutoTrack_impl.js?t='+Math.floor(new Date().getTime()/(1000*3600*24))*(3600*24)
            
            // 给网页注入定制化js
            // TODO: js内容
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                window.alert = (msg)=>{window.chrome.webview.postMessage({action:'ALERT',msg:msg.toString()})};
                !function(){const s = document.createElement('script')

                s.src = 'https://zhiqiong.vercel.app/sharedmap.user.js?t='+Math.floor(new Date().getTime()/(1000*3600*24))*(3600*24)

                s.onerror = () => { alert('共享地图加载失败，请检查是否可以连接到 https://zhiqiong.vercel.app '); }
                window.addEventListener('DOMContentLoaded',()=>{document.head.appendChild(s);window.addEventListener('contextmenu', (e)=>{e.stopImmediatePropagation()},true);})}()
                document.addEventListener('focus',(e)=>{if(e.target.tagName==='INPUT'||e.target.tagName==='TEXTAREA')window.chrome.webview.postMessage({action:'INPUT'})}, true);
                window.onload = ()=>{window.chrome.webview.postMessage({action:'LOAD'})};
            ");
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequestedAsync;
            webView.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
            webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceivedAsync;
            webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            webView.CoreWebView2.AddWebResourceRequestedFilter("http://localhost:32333/*",CoreWebView2WebResourceContext.All);
            webView.CoreWebView2.AddWebResourceRequestedFilter("https://yuanshen.site/index.html*", CoreWebView2WebResourceContext.All);

            webView.CoreWebView2.Navigate(isOversea?mapOS:mapCN);

        }
        /// <summary>
        /// 设置Header
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CoreWebView2_WebResourceRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs args)
        {
           if (args.Request.Uri.ToString().Contains("yuanshen.site/index"))
           {
               //
               var def = args.GetDeferral();
               var client = new HttpClient();
               var response = client.GetAsync(new Uri(args.Request.Uri)).GetAwaiter().GetResult();
               var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
               //
               responseContent = responseContent.Replace("-Policy", "");
               //
               InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
               //
               DataWriter writer = new DataWriter(stream);
               writer.WriteString(responseContent);
               writer.StoreAsync().GetAwaiter().GetResult();
                //
                CoreWebView2WebResourceResponse newres = webView.CoreWebView2.Environment.CreateWebResourceResponse(stream, 200, "OK", "Content-Type: text/html");
                args.Response = newres;
               def.Complete();
               return;
           }
           args.Request.Headers.SetHeader("Origin", "@kyjg");

        }

        /// <summary>
        /// 获取Handle信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void CoreWebView2_WebMessageReceivedAsync(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
        {
            string jsonStr=args.WebMessageAsJson;
            var jsonObject = JsonObject.Parse(jsonStr);
            // 获取action
            string action = jsonObject.GetNamedString("action");
            // 如果 action 等于 PLUGIN
            if (action == "PLUGIN")
            {
                string pluginToken = jsonObject.GetNamedString("token");
                string pluginQuery = pluginToken == "" ? "" : ("?local-auth=" + pluginToken);
                string pluginLaunch = "cocogoat-control://launch" + pluginQuery;
                if (this.isInGameBar)
                {
                    await OpenInFullTrust(pluginLaunch);
                }
                else
                {
                    var uri = new Uri(pluginLaunch);
                    await Windows.System.Launcher.LaunchUriAsync(uri);
                }
            }
            // 如果 action 等于 INPUT
            if (action == "INPUT" && isInGameBar && !hasInputBox)
            {
                InputBox();
            }
            // 如果 action 等于 MAXIMIZE
            if (action=="MAXIMIZE" && isInGameBar)
            {
                ToggleMaximize();
            }
            // 如果 action 等于 LOAD
            if (action == "LOAD")
            {
                // 检查更新
                await webView.CoreWebView2.ExecuteScriptAsync(@"
                    console.log('Zhiqiong-UWP: Load');
                    webControlMAP.ev.on('hotkey',(e)=>{if(e==='AltZ')window.chrome.webview.postMessage({action:'MAXIMIZE'})})
                    fetch('https://77.cocogoat.work/upgrade/zhiqiong-uwp.json?t='+Math.round(Date.now()/1000/3600)).then(e=>e.json()).then(e=>{
                        const targetVer = e.version;
                        const curVer = (navigator.userAgent.match(/zhiqiong-uwp\/([0-9.]*)/)||[])[1]||'0.0.0.0'
                        if($map.control.versionCompare(targetVer,curVer)>0){
                            window.chrome.webview.postMessage({action:'COPYALERT',url:'https://zhiqiong.cocogoat.work',msg:'发现新版本 v'+targetVer+'（当前版本 v'+curVer+'），请按Win+G打开Xbox Game Bar后复制下方地址手动下载更新'})
                        }
                    })
                ");
            }
            // 如果 action 等于 ALERT
            if (action == "ALERT")
            {
                string msg = jsonObject.GetNamedString("msg");
                await new MessageDialog(msg).ShowAsync();

            }
            // 如果 action 等于 COPYALERT
            if (action == "COPYALERT")
            {
                ContentDialog dialog = new ContentDialog();
                TextBox inputTextBox = new TextBox();
                dialog.Content = inputTextBox;
                inputTextBox.Text = jsonObject.GetNamedString("url");
                dialog.Title = jsonObject.GetNamedString("msg");
                dialog.PrimaryButtonText = "复制并关闭";
                dialog.SecondaryButtonText = "取消";
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    // copy to clipboard
                    Windows.ApplicationModel.DataTransfer.DataPackage dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    dataPackage.SetText(inputTextBox.Text);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                }
            }
        }

        /// <summary>
        /// 改变UserAgent
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CoreWebView2_NavigationStarting(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            // 获取版本 version
            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            var settings = webView.CoreWebView2.Settings;
            string strver = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            // don't change if modified
            if (settings.UserAgent.Contains("weixitianli-kongyingjiuguan-uwp/"))
            {
                return;
            }
            settings.UserAgent = settings.UserAgent + " weixitianli-kongyingjiuguan-uwp/" + strver;
            settings.UserAgent = settings.UserAgent + " weixitianli-kongyingjiuguan-uwp/" + (isInGameBar?"gamebar":"webview");
        }

        /// <summary>
        /// 更改上下文菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CoreWebView2_ContextMenuRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2ContextMenuRequestedEventArgs args)
        {
            IList<CoreWebView2ContextMenuItem> menuList = args.MenuItems;
            // 移除
            bool hasSelectAll = false;
            bool hasReload = false;
            string[] ignoredList =
            {
                "other", "saveImageAs", "copyImage", "copyImageLocation", "createQrCode", "saveAs", "print", "back",
                "forward"
            };
            for (int index = 0; index < menuList.Count; index++)
            {
                if (ignoredList.Contains(menuList[index].Name))
                {
                    menuList.RemoveAt(index);
                    index--;
                }
                else if (menuList[index].Name == "selectAll") 
                {
                    hasSelectAll = true;
                }
                else if (menuList[index].Name == "reload") 
                {
                    hasReload = true;
                }
            }

            if (hasSelectAll && !hasReload)
            {
                // 非输入元素上无ctxmenu
                args.Handled = true;
            }
            // 添加
            CoreWebView2ContextMenuItem subItem =
                    webView.CoreWebView2.Environment.CreateContextMenuItem("切换到" + (isOversea ? "中文" : "Enghish"),
                null,CoreWebView2ContextMenuItemKind.Command);
            subItem.CustomItemSelected += delegate(CoreWebView2ContextMenuItem send, object e)
            {
                isOversea = !isOversea;
                webView.CoreWebView2.Navigate(isOversea ? mapOS : mapCN);

                
            };
            menuList.Insert(0, subItem);
        }

        /// <summary>
        /// 改为打开默认浏览器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void CoreWebView2_NewWindowRequestedAsync(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs args)
        {            
            // prevent default
            args.Handled = true;
            if (this.isInGameBar)
            {
                await OpenInFullTrust(args.Uri);
            }
            else
            {
                var uri = new Uri(args.Uri);
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }

        /// <summary>
        /// 输入框调整
        /// </summary>
        public async void InputBox()
        {
            hasInputBox = true;
            // Get value of current input
            var jCurrentValue = await webView.CoreWebView2.ExecuteScriptAsync(@"(document.activeElement.tagName==='INPUT'||document.activeElement.tagName==='TEXTAREA')?document.activeElement.value:''");
            var currentValue = Windows.Data.Json.JsonValue.Parse(jCurrentValue).GetString();
            // Get type of current input
            var jCurrentType = await webView.CoreWebView2.ExecuteScriptAsync(@"(document.activeElement.tagName==='INPUT')?document.activeElement.type:'other'");
            var currentType = Windows.Data.Json.JsonValue.Parse(jCurrentType).GetString();
            // Prompt using ContentDialog
            ContentDialog dialog = new ContentDialog();
            if (currentType == "password")
            {
                PasswordBox inputTextBox = new PasswordBox();
                dialog.Content = inputTextBox;
                inputTextBox.Password = currentValue;
            }
            else
            {
                TextBox inputTextBox = new TextBox();
                dialog.Content = inputTextBox;
                inputTextBox.Text = currentValue;
            }
            dialog.Title = "请按Win+G打开Xbox Game Bar后，在此输入" + (currentType == "password" ? "密码" : "内容");
            dialog.IsSecondaryButtonEnabled = true;
            dialog.PrimaryButtonText = "输入";
            dialog.SecondaryButtonText = "取消";
            IAsyncOperation<ContentDialogResult> tsk = dialog.ShowAsync();
            if (await tsk == ContentDialogResult.Primary)
            {
                // escape single quote
                string escapedInput = "";
                if (currentType == "password")
                {
                    PasswordBox inputTextBox = (PasswordBox)dialog.Content;
                    escapedInput = inputTextBox.Password.Replace("'", "\\'");
                }
                else
                {
                    TextBox inputTextBox = (TextBox)dialog.Content;
                    escapedInput = inputTextBox.Text.Replace("'", "\\'");
                }
                await webView.CoreWebView2.ExecuteScriptAsync(@"if(document.activeElement.tagName==='INPUT'||document.activeElement.tagName==='TEXTAREA'){
                            document.activeElement.value = '" + escapedInput + @"';
                            document.activeElement.dispatchEvent(new Event('input'));
                            document.activeElement.blur();
                        }");
            }
            else
            {
                // cancel
                await webView.CoreWebView2.ExecuteScriptAsync(@"if(document.activeElement.tagName==='INPUT'||document.activeElement.tagName==='TEXTAREA'){
                            document.activeElement.blur();
                        }");
            }
            hasInputBox = false;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
        }

        private void CoreWebView2_DOMContentLoaded(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2DOMContentLoadedEventArgs args)
        {
            string js = @"  
                            let cursor = document.createElement('div')
                            cursor.id = 'inject-user-cursor'
                            window.injectCursor = cursor 
                            document.body.appendChild(cursor)

                            let style = document.createElement('style')
                            style.innerHTML = `
                                .danjixiazai, .enindex-btn, .jpindex-btn, .switch_content, .fankui, .genshin_pub, .genshin_art, .feixiaoqiu, .wiki{
                                    display: none!important;
                                } 
                                .control-containor{
                                    transform:scale(0.4);
                                }
                                #inject-user-cursor{
                                    position: fixed;
                                    visibility: hidden;
                                    left: 50vw;
                                    top: 50vh;
                                    width: 36px;
                                    height: 36px;
                                    transform: translate(-50%, -50%);
                                    transition: all .5s;
                                    transform-origin: center;
                                    z-index: 999999999999999;
                                    background-image: url(https://assets.yuanshen.site/icons/483.png);
                                    background-repeat: no-repeat;                                
                                    background-position: center;        
                                }  
                            `
                            document.head.appendChild(style)

const INJECT_NAME = 'CVAutoTrack';
const INJECT_Version = '1.0';
const INJECT_ICON_URL = 'https://assets.yuanshen.site/icons/MapPoint.png';
const INJECT_ICON_FOV_URL = 'https://assets.yuanshen.site/icons/MapPointFov.png';
const INJECT_ICON_CONFIG = {
  width: 52,
  height: 52,
};
const INJECT_BUTTON_CONFIG = {
  width: 54,
  height: 54,
};
const MARKER_STYLE = document.createElement('style');
const USER_POSITION_FOLLOW_BUTTON = document.createElement('button');
const USER_POSITION_FOLLOW_BUTTON_ICON = `
<svg width='24' height='24' viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg' class='_icon'>
  <path d='M13.67 22H13.61C13.3899 21.9867 13.1804 21.9011 13.014 21.7564C12.8476 21.6117 12.7337 21.4161 12.69 21.2L11.15 13.63C11.1108 13.4378 11.016 13.2614 10.8773 13.1227C10.7386 12.984 10.5622 12.8892 10.37 12.85L2.8 11.31C2.58347 11.267 2.38726 11.1535 2.24196 10.9874C2.09666 10.8212 2.01043 10.6115 1.99673 10.3912C1.98303 10.1709 2.04263 9.95221 2.16622 9.76929C2.28982 9.58637 2.47047 9.44949 2.68 9.38L18.68 4.05C18.8596 3.97584 19.0569 3.95561 19.2478 3.99179C19.4387 4.02796 19.615 4.11896 19.755 4.25368C19.895 4.3884 19.9927 4.56099 20.0363 4.75036C20.0798 4.93973 20.0672 5.13767 20 5.32L14.67 21.32C14.6004 21.527 14.465 21.7055 14.2844 21.8283C14.1039 21.9511 13.8881 22.0114 13.67 22Z' fill='#13E0ED'/>
</svg>
`;


const parseDom = (arg) => {
  const _DOM_CONTAINER = document.createElement(""div"");
  _DOM_CONTAINER.innerHTML = arg;
  return _DOM_CONTAINER.childNodes[0];
};

const APP_LOGO_CONTAINER = parseDom(
  `<svg width='339' height='53' viewBox='0 0 339 53' fill='none' xmlns='http://www.w3.org/2000/svg' id='app-logo'>
  <g filter='url(#filter0_d_224_55)'>
    <path d='M27.89 32.182H23.002V24.148H29.866C30.23 24.148 30.516 24.018 30.594 23.732C29.45 22.64 27.5 20.976 27.5 20.976L25.758 23.42H11.718L11.926 24.148H19.102V32.182H8.98804L9.19604 32.91H32.206C32.596 32.91 32.882 32.78 32.96 32.494C31.764 31.35 29.71 29.66 29.71 29.66L27.89 32.182ZM16.45 15.126C15.436 17.388 12.68 20.976 9.66404 22.926L9.79404 23.108C13.98 22.198 17.724 20.144 19.908 18.116C20.792 18.168 21.208 17.934 21.364 17.57L16.45 15.126ZM30.152 11.772L28.176 13.748H22.222C24.354 12.838 24.666 9.01601 18.114 9.79601L18.01 9.92601C18.92 10.654 19.544 12.032 19.518 13.358C19.752 13.514 20.012 13.644 20.246 13.748H12.732C12.55 13.15 12.29 12.526 11.952 11.876L11.64 11.902C11.848 13.358 10.964 14.684 10.158 15.204C9.17004 15.646 8.46804 16.504 8.80604 17.648C9.19604 18.844 10.522 19.26 11.562 18.662C12.628 18.09 13.304 16.582 12.914 14.476H28.41C28.332 15.152 28.202 15.958 28.072 16.686C26.59 16.218 24.692 15.906 22.3 15.906L22.118 16.114C24.692 17.57 27.76 20.196 29.372 22.562C32.466 23.602 33.844 19.546 29.502 17.284C30.62 16.686 31.738 15.932 32.492 15.308C33.064 15.282 33.324 15.204 33.532 14.944L30.152 11.772Z' fill='white' />
    <path d='M54.306 22.172C55.554 21.418 57.218 20.196 58.232 19.364C58.778 19.338 59.038 19.26 59.246 19.026L55.918 15.88L53.968 17.804H38.81C38.654 17.31 38.42 16.764 38.134 16.218L37.848 16.244C38.03 17.31 37.198 18.324 36.496 18.74C35.456 19.13 34.702 19.962 34.962 21.21C35.3 22.484 36.678 22.952 37.718 22.458C38.758 21.964 39.408 20.534 38.992 18.532H54.254C54.228 19.624 54.124 21.028 54.046 21.99L52.59 21.028C51.914 22.9 51.082 24.928 50.328 26.462C49.444 25.526 48.846 24.46 48.482 23.264C48.56 22.354 48.612 21.392 48.664 20.378C49.262 20.3 49.522 20.04 49.574 19.676L44.738 19.286C44.66 25.838 44.946 30.492 34.364 34.132L34.52 34.47C44.868 32.442 47.468 29.14 48.274 24.798C49.158 28.88 51.264 32.598 56.334 34.522C56.542 32.416 57.582 31.35 59.428 30.96V30.648C55.606 29.92 52.928 28.776 51.134 27.242C52.72 26.332 54.176 25.214 55.502 23.992C56.1 24.174 56.516 24.018 56.698 23.758L54.306 22.172ZM41.098 20.898C40.864 22.64 39.434 24.304 38.472 24.954C37.536 25.578 37.042 26.592 37.536 27.684C38.16 28.906 39.954 29.088 40.812 28.126C42.034 26.8 42.528 24.33 41.384 20.924L41.098 20.898ZM54.618 13.228H53.11V10.732C53.786 10.628 53.994 10.394 54.02 10.03L49.366 9.64001V13.228H44.53V10.732C45.206 10.628 45.414 10.368 45.44 10.03L40.864 9.64001V13.228H34.546L34.728 13.956H40.864V17.154H41.462C43.074 17.154 44.53 16.634 44.53 16.322V13.956H49.366V16.972H49.964C51.628 16.972 53.11 16.504 53.11 16.192V13.956H58.882C59.272 13.956 59.558 13.826 59.61 13.54C58.466 12.422 56.412 10.784 56.412 10.784L54.618 13.228Z' fill='white' />
    <path d='M62.392 10.264C63.224 11.33 64.212 12.942 64.55 14.424C67.722 16.452 70.374 10.524 62.574 10.134L62.392 10.264ZM60.494 16.01C61.248 17.05 61.95 18.61 62.028 20.066C64.914 22.328 68.008 16.79 60.702 15.88L60.494 16.01ZM68.216 13.878C63.588 24.746 63.588 24.746 63.016 25.76C62.704 26.306 62.6 26.306 62.184 26.306H61.014V26.774C61.56 26.826 62.028 26.956 62.392 27.216C63.016 27.632 63.12 30.31 62.574 33.066C62.834 34.132 63.588 34.444 64.264 34.444C65.694 34.444 66.734 33.482 66.786 32.078C66.838 29.634 65.642 28.828 65.564 27.294C65.564 26.618 65.72 25.604 65.928 24.694C66.214 23.16 67.722 17.206 68.606 13.982L68.216 13.878ZM75.782 16.478V12.838H76.77V16.478H75.782ZM80.774 17.206V21.574H80.41H79.942C79.656 21.574 79.604 21.47 79.604 21.158V17.206H80.774ZM72.038 31.272V27.658H80.774V31.272H72.038ZM80.046 24.46C80.306 24.46 80.54 24.46 80.774 24.434V26.93H72.038V25.63C75.522 23.706 75.782 20.872 75.782 18.142V17.206H76.77V22.068C76.77 23.862 77.03 24.46 78.954 24.46H80.046ZM72.896 17.206V18.168C72.896 20.534 72.922 22.848 72.038 24.694V17.206H72.896ZM79.604 12.838H84.856C85.22 12.838 85.506 12.708 85.584 12.422C84.414 11.304 82.412 9.66601 82.412 9.66601L80.644 12.11H67.93L68.138 12.838H72.896V16.478H72.35L68.762 15.152V34.444H69.36C71.024 34.444 72.038 33.82 72.038 33.586V32H80.774V34.288H81.372C83.088 34.288 84.206 33.586 84.206 33.378V17.518C84.83 17.414 85.116 17.206 85.324 16.946L82.308 14.554L80.644 16.478H79.604V12.838Z' fill='white' />
    <path d='M92.916 19.754C93.306 19.676 93.462 19.494 93.488 19.26L89.536 18.87V29.114C89.536 29.712 89.38 29.998 88.314 30.57L90.368 34.366C90.784 34.132 91.226 33.69 91.486 32.988C93.852 30.804 95.646 28.776 96.582 27.684L96.478 27.45C95.256 27.944 94.034 28.464 92.916 28.906V19.754ZM106.696 16.062L105.188 17.674H101.444L98.766 16.608C98.974 16.036 99 15.256 98.792 14.372H107.294L107.554 16.686L106.696 16.062ZM101.314 22.38V18.402H105.448V22.38H101.314ZM106.332 26.8V31.506H101.314V26.8H106.332ZM108.672 11.902L106.93 13.644H104.486C105.942 12.708 105.916 9.92601 100.794 9.48401L100.612 9.61401C101.262 10.55 101.834 12.006 101.86 13.358C101.99 13.462 102.146 13.566 102.276 13.644H98.584C98.428 13.176 98.194 12.708 97.908 12.188L97.57 12.214C97.882 12.994 97.388 13.878 96.946 14.294L96.79 14.372L94.84 12.682L93.306 14.268H91.174C91.668 13.228 92.084 12.214 92.474 11.252C93.15 11.304 93.358 11.148 93.462 10.862L88.6 9.71801C88.288 13.228 87.456 18.272 86.494 21.158L86.728 21.288C88.314 19.572 89.692 17.284 90.81 14.996H93.514C93.358 16.192 93.098 17.882 92.812 18.87H93.124C93.956 18.298 94.866 17.388 95.62 16.53C95.932 17.57 97.024 17.96 97.83 17.648V34.418H98.376C99.884 34.418 101.314 33.612 101.314 33.222V32.234H106.332V33.95H106.93C108.074 33.95 109.738 33.248 109.738 33.014V27.268C110.232 27.19 110.544 26.956 110.674 26.774L107.58 24.46L106.072 26.072H101.314V23.108H105.448V24.356H106.046C107.112 24.356 108.828 23.81 108.854 23.628V18.896C109.348 18.792 109.66 18.584 109.816 18.402L108.022 17.05C108.906 16.504 109.998 15.672 110.7 15.1C111.246 15.048 111.48 14.996 111.688 14.762L108.672 11.902Z' fill='white' />
    <path fill-rule='evenodd' clip-rule='evenodd' d='M133.688 10.003C127.061 10.003 121.688 15.3756 121.688 22.003C121.688 28.6304 127.061 34.003 133.688 34.003H177.688C184.315 34.003 189.688 28.6304 189.688 22.003C189.688 15.3756 184.315 10.003 177.688 10.003H133.688ZM145.864 16.303C146.176 17.103 146.384 17.695 146.488 18.079L147.652 17.743C147.452 17.151 147.248 16.571 147.04 16.003L145.864 16.303ZM155.068 17.803H152.608V16.147H151.408V17.803H148.948V22.303C148.596 21.983 148.108 21.587 147.484 21.115C147.796 20.603 148.088 20.059 148.36 19.483V18.199H144.988V19.435H147.052C146.748 20.067 146.404 20.639 146.02 21.151C145.636 21.655 145.192 22.127 144.688 22.567L145.12 23.947C145.56 23.539 145.936 23.155 146.248 22.795V27.127H147.424V22.567C147.856 22.935 148.14 23.187 148.276 23.323L148.948 22.483V24.499H151.408V27.139H152.608V24.499H155.068V17.803ZM151.408 18.991V20.551H150.112V18.991H151.408ZM153.916 18.991V20.551H152.608V18.991H153.916ZM150.112 23.251V21.751H151.408V23.251H150.112ZM152.608 23.251V21.751H153.916V23.251H152.608ZM140.104 23.383H142.732V18.643H139.42C139.516 18.371 139.584 18.167 139.624 18.031L139.72 17.743H143.404V16.519H133.84V20.659C133.84 21.675 133.808 22.479 133.744 23.071C133.688 23.663 133.58 24.199 133.42 24.679C133.268 25.159 133.024 25.711 132.688 26.335L133.78 27.199C134.14 26.591 134.412 25.995 134.596 25.411C134.788 24.819 134.924 24.155 135.004 23.419C135.084 22.675 135.124 21.743 135.124 20.623V17.743H138.34C138.236 18.079 138.124 18.379 138.004 18.643H136.024V23.383H138.808V25.183C138.808 25.455 138.756 25.635 138.652 25.723C138.556 25.811 138.348 25.855 138.028 25.855H137.02L137.272 27.031H138.532C138.94 27.031 139.256 26.987 139.48 26.899C139.704 26.819 139.864 26.683 139.96 26.491C140.056 26.299 140.104 26.023 140.104 25.663V23.383ZM141.556 19.723V20.551H137.224V19.723H141.556ZM137.224 21.463H141.556V22.279H137.224V21.463ZM136.804 25.531C137.244 25.059 137.588 24.651 137.836 24.307L136.876 23.599C136.612 23.959 136.284 24.351 135.892 24.775C135.5 25.191 135.096 25.571 134.68 25.915L135.508 26.839C135.94 26.439 136.372 26.003 136.804 25.531ZM142.528 24.703C142.08 24.255 141.712 23.899 141.424 23.635L140.608 24.391L140.788 24.571C141.788 25.571 142.464 26.271 142.816 26.671L143.668 25.855C143.356 25.527 142.976 25.143 142.528 24.703ZM167.512 23.899C167.512 24.043 167.508 24.195 167.5 24.355C167.492 24.515 167.484 24.667 167.476 24.811C167.468 24.915 167.46 25.027 167.452 25.147C167.444 25.267 167.44 25.387 167.44 25.507C167.416 25.851 167.348 26.115 167.236 26.299C167.124 26.491 166.948 26.627 166.708 26.707C166.468 26.787 166.132 26.827 165.7 26.827H162.46C161.844 26.827 161.404 26.711 161.14 26.479C160.884 26.247 160.756 25.823 160.756 25.207V21.199L159.988 21.439L159.688 20.335H159.172V24.103L160.276 23.803L160.324 24.271L160.348 24.991L156.952 25.951L156.736 24.775L157.972 24.439V20.335H156.904V19.099H157.972V16.375H159.172V19.099H160.12V20.119L160.756 19.915V17.083H161.968V19.531L163.156 19.147V16.063H164.356V18.763L166.912 17.947L166.768 22.423C166.752 22.767 166.7 23.031 166.612 23.215C166.524 23.391 166.38 23.519 166.18 23.599C165.988 23.679 165.712 23.719 165.352 23.719H164.8L164.56 22.507H164.932C165.14 22.507 165.288 22.495 165.376 22.471C165.464 22.439 165.52 22.383 165.544 22.303C165.576 22.223 165.596 22.087 165.604 21.895C165.628 21.527 165.648 21.091 165.664 20.587C165.688 20.075 165.696 19.779 165.688 19.699L164.356 20.107V24.739H163.156V20.467L161.968 20.839V24.763C161.968 25.019 161.988 25.207 162.028 25.327C162.068 25.439 162.148 25.519 162.268 25.567C162.388 25.607 162.576 25.627 162.832 25.627H165.208C165.52 25.627 165.744 25.607 165.88 25.567C166.024 25.527 166.12 25.455 166.168 25.351C166.216 25.239 166.244 25.063 166.252 24.823L166.288 23.623L167.512 23.899ZM169.288 16.495V27.127H170.524V26.563H177.772V27.127H179.008V16.495H169.288ZM170.524 25.459V17.623H177.772V25.459H170.524ZM177.58 21.379C176.612 21.235 175.812 21.047 175.18 20.815C175.724 20.479 176.312 20.007 176.944 19.399V18.535H173.32L173.668 18.019L172.66 17.791C172.06 18.663 171.412 19.351 170.716 19.855L171.316 20.623C171.652 20.375 171.94 20.131 172.18 19.891C172.54 20.267 172.864 20.563 173.152 20.779C172.392 21.067 171.564 21.311 170.668 21.511L171.088 22.531C171.536 22.411 172.012 22.259 172.516 22.075L172.36 22.867C172.856 22.947 173.452 23.055 174.148 23.191C174.844 23.319 175.444 23.443 175.948 23.563L176.08 22.639C175.44 22.495 174.324 22.283 172.732 22.003C173.188 21.843 173.668 21.643 174.172 21.403C174.9 21.771 175.904 22.103 177.184 22.399L177.58 21.379ZM174.136 20.335C173.688 20.063 173.292 19.751 172.948 19.399H175.516C175.148 19.743 174.688 20.055 174.136 20.335ZM174.232 23.875C173.192 23.675 172.304 23.511 171.568 23.383L171.436 24.343C172.188 24.479 173.072 24.647 174.088 24.847C175.112 25.039 175.904 25.199 176.464 25.327L176.656 24.367C176.08 24.231 175.272 24.067 174.232 23.875Z' fill='white' />
  </g>
  <g filter='url(#filter1_d_224_55)'>
    <path fill-rule='evenodd' clip-rule='evenodd' d='M218.445 22L223.536 16.9088L222.405 15.7774L217.314 20.8686L212.223 15.7775L211.091 16.9088L216.182 22L211.091 27.0911L212.223 28.2225L217.314 23.1314L222.405 28.2226L223.536 27.0912L218.445 22Z' fill='white' />
  </g>
  <g filter='url(#filter2_d_224_55)'>
    <path fill-rule='evenodd' clip-rule='evenodd' d='M293.684 28.6836C303.086 19.2815 307.874 8.82559 304.378 5.32975C301.914 2.86556 295.991 4.51741 289.417 9.00001H292.476C296.172 7.14725 299.247 6.73642 300.729 8.21781C303.511 11.0002 299.618 19.4043 292.034 26.9888C284.449 34.5734 276.045 38.4663 273.263 35.6839C272.829 35.2508 272.558 34.6814 272.437 34H269.429C268.93 36.3496 269.187 38.2354 270.33 39.3779C273.826 42.8738 284.281 38.0858 293.684 28.6836ZM294.385 31H294.183C294.49 30.7045 294.795 30.4045 295.1 30.1L295.111 30.0891V30.2756C295.111 30.4826 295.043 30.655 294.908 30.793C294.772 30.931 294.598 31 294.385 31ZM296.854 28.7647V28.2795C297.797 27.2622 298.689 26.236 299.526 25.2097V28.3715C299.526 28.5784 299.622 28.7509 299.816 28.8889C300.01 29.0269 300.252 29.0959 300.542 29.0959H307.571C307.862 29.0959 308.104 29.1648 308.297 29.3028C308.491 29.4408 308.588 29.6133 308.588 29.8203V30.2756C308.588 30.4688 308.491 30.6344 308.297 30.7723C308.104 30.9241 307.862 31 307.571 31H299.99C299.7 31 299.39 30.9517 299.061 30.8551C298.732 30.7585 298.48 30.6344 298.306 30.4826L297.58 29.9651C297.367 29.841 297.192 29.6616 297.057 29.427C296.921 29.1924 296.854 28.9717 296.854 28.7647ZM301.268 26.9434V22.9621C302.268 21.604 303.162 20.2574 303.94 18.9433V26.9434C303.94 27.1365 303.834 27.309 303.621 27.4608C303.427 27.6126 303.195 27.6885 302.924 27.6885H302.256C301.985 27.6885 301.752 27.6126 301.559 27.4608C301.365 27.309 301.268 27.1365 301.268 26.9434ZM305.683 16.2015C305.618 16.1549 305.548 16.1161 305.473 16.0852C305.547 15.9316 305.618 15.7788 305.688 15.6267C305.924 15.1106 306.142 14.5978 306.337 14.0904H307.658C307.929 14.0904 308.162 14.1594 308.355 14.2974C308.549 14.4354 308.646 14.6078 308.646 14.8148V26.1776C308.646 26.3707 308.549 26.5432 308.355 26.695C308.162 26.833 307.929 26.902 307.658 26.902H305.567C305.296 26.902 305.054 26.833 304.841 26.695C304.647 26.5432 304.55 26.3707 304.55 26.1776V25.7222C304.55 25.5153 304.618 25.3497 304.754 25.2255C304.889 25.0875 305.064 25.0185 305.277 25.0185C305.47 25.0185 305.635 24.9426 305.77 24.7908C305.906 24.6391 305.974 24.4666 305.974 24.2734V16.6983C305.974 16.5051 305.877 16.3395 305.683 16.2015ZM298.456 14.964C298.908 13.9892 299.219 13.1187 299.407 12.367C299.362 12.3101 299.305 12.2567 299.235 12.207C299.042 12.069 298.8 12 298.509 12H297.87C297.58 12 297.338 12.069 297.144 12.207C296.95 12.345 296.854 12.5105 296.854 12.7037V13.366C296.854 13.5592 296.795 13.7317 296.679 13.8834C296.582 14.0214 296.457 14.0904 296.302 14.0904C296.147 14.0904 296.021 14.1594 295.924 14.2974C295.847 14.4354 295.808 14.6078 295.808 14.8148V15.2702C295.808 15.4771 295.847 15.6496 295.924 15.7876C296.021 15.9256 296.147 15.9946 296.302 15.9946C296.457 15.9946 296.582 16.0635 296.679 16.2015C296.795 16.3395 296.854 16.5051 296.854 16.6983V17.8871C297.492 16.868 298.029 15.8867 298.456 14.964ZM291.045 25.1488C292.007 24.1642 292.899 23.1719 293.717 22.1876V16.9466C293.717 16.7534 293.784 16.5879 293.92 16.4499C294.056 16.2981 294.21 16.2222 294.385 16.2222C294.598 16.2222 294.772 16.1532 294.908 16.0153C295.043 15.8773 295.111 15.7117 295.111 15.5185V15.0632C295.111 14.8562 295.043 14.6837 294.908 14.5457C294.772 14.4078 294.598 14.3388 294.385 14.3388C294.21 14.3388 294.056 14.2698 293.92 14.1318C293.784 13.98 293.717 13.8076 293.717 13.6144V12.7037C293.717 12.5105 293.62 12.345 293.426 12.207C293.233 12.069 292.991 12 292.7 12H292.032C291.761 12 291.529 12.069 291.335 12.207C291.141 12.345 291.045 12.5105 291.045 12.7037V13.6144C291.045 13.8076 290.967 13.98 290.812 14.1318C290.657 14.2698 290.464 14.3388 290.231 14.3388C290.018 14.3388 289.825 14.4078 289.65 14.5457C289.496 14.6837 289.418 14.8562 289.418 15.0632V15.5185C289.418 15.7117 289.496 15.8773 289.65 16.0153C289.825 16.1532 290.018 16.2222 290.231 16.2222C290.464 16.2222 290.657 16.2981 290.812 16.4499C290.967 16.5879 291.045 16.7534 291.045 16.9466V25.1488ZM286.059 29.3028C286.133 29.3552 286.192 29.4125 286.238 29.4748C285.538 30.0235 284.844 30.5326 284.163 31H275.138C274.867 31 274.625 30.9241 274.412 30.7723C274.219 30.6344 274.122 30.4688 274.122 30.2756V29.8203C274.122 29.6133 274.219 29.4408 274.412 29.3028C274.625 29.1648 274.867 29.0959 275.138 29.0959H277.985C278.256 29.0959 278.488 29.0269 278.682 28.8889C278.895 28.7509 279.001 28.5784 279.001 28.3715V27.6885C279.001 27.4815 278.895 27.309 278.682 27.171C278.488 27.0192 278.256 26.9434 277.985 26.9434H275.138C274.867 26.9434 274.625 26.8744 274.412 26.7364C274.219 26.5984 274.122 26.4259 274.122 26.219V25.7843C274.122 25.5911 274.219 25.4187 274.412 25.2669C274.625 25.1151 274.867 25.0392 275.138 25.0392H277.985C278.256 25.0392 278.488 24.9702 278.682 24.8322C278.895 24.6943 279.001 24.5287 279.001 24.3355V23.8595C279.001 23.6663 278.895 23.5007 278.682 23.3627C278.488 23.2248 278.256 23.1558 277.985 23.1558H275.138C274.867 23.1558 274.625 23.0868 274.412 22.9488C274.219 22.797 274.122 22.6245 274.122 22.4314V12.7037C274.122 12.5105 274.219 12.345 274.412 12.207C274.625 12.069 274.867 12 275.138 12H285.333C285.623 12 285.865 12.069 286.059 12.207C286.253 12.345 286.35 12.5105 286.35 12.7037V20.9205C286.35 21.1137 286.282 21.3275 286.146 21.5621C286.011 21.7967 285.836 21.9898 285.623 22.1416L284.926 22.6383C284.752 22.7901 284.491 22.9143 284.142 23.0109C283.813 23.1075 283.503 23.1558 283.213 23.1558H282.661C282.39 23.1558 282.157 23.2248 281.964 23.3627C281.77 23.5007 281.673 23.6663 281.673 23.8595V24.3355C281.673 24.5287 281.77 24.6943 281.964 24.8322C282.157 24.9702 282.39 25.0392 282.661 25.0392H285.333C285.623 25.0392 285.865 25.1151 286.059 25.2669C286.253 25.4049 286.35 25.5773 286.35 25.7843V26.219C286.35 26.4259 286.253 26.5984 286.059 26.7364C285.865 26.8744 285.623 26.9434 285.333 26.9434H282.661C282.39 26.9434 282.157 27.0192 281.964 27.171C281.77 27.309 281.673 27.4815 281.673 27.6885V28.3715C281.673 28.5784 281.77 28.7509 281.964 28.8889C282.157 29.0269 282.39 29.0959 282.661 29.0959H285.333C285.623 29.0959 285.865 29.1648 286.059 29.3028ZM245 12.7037C245 12.5105 245.097 12.345 245.29 12.207C245.484 12.069 245.716 12 245.988 12H263.385C263.656 12 263.889 12.069 264.082 12.207C264.276 12.345 264.373 12.5105 264.373 12.7037V13.1797C264.373 13.3867 264.276 13.5592 264.082 13.6972C263.889 13.8351 263.656 13.9041 263.385 13.9041H257.054C256.783 13.9041 256.54 13.98 256.327 14.1318C256.134 14.2698 256.037 14.4354 256.037 14.6285V18.1057C256.037 18.3126 256.134 18.4851 256.327 18.6231C256.54 18.7611 256.783 18.8301 257.054 18.8301H263.385C263.656 18.8301 263.889 18.906 264.082 19.0577C264.276 19.1957 264.373 19.3613 264.373 19.5545V20.0305C264.373 20.2375 264.276 20.41 264.082 20.5479C263.889 20.6859 263.656 20.7549 263.385 20.7549H257.054C256.783 20.7549 256.54 20.7825 256.327 20.8377C256.134 20.8929 256.037 20.9619 256.037 21.0447C256.037 21.1964 256.192 21.5069 256.502 21.976L261.207 28.4336C261.343 28.6129 261.556 28.7647 261.846 28.8889C262.136 29.0131 262.427 29.0752 262.717 29.0752H263.385C263.656 29.0752 263.889 29.1442 264.082 29.2821C264.276 29.4201 264.373 29.5926 264.373 29.7996V30.2756C264.373 30.4688 264.276 30.6344 264.082 30.7723C263.889 30.9241 263.656 31 263.385 31H261.091C260.8 31 260.51 30.9379 260.219 30.8137C259.929 30.6895 259.716 30.5309 259.58 30.3377L255.166 24.3148C255.03 24.1216 254.856 24.0251 254.643 24.0251C254.469 24.0251 254.323 24.1216 254.207 24.3148L249.792 30.3377C249.657 30.5309 249.444 30.6895 249.153 30.8137C248.882 30.9379 248.602 31 248.311 31H245.988C245.716 31 245.484 30.9241 245.29 30.7723C245.097 30.6344 245 30.4688 245 30.2756V29.7996C245 29.5926 245.097 29.4201 245.29 29.2821C245.484 29.1442 245.716 29.0752 245.988 29.0752H246.685C246.975 29.0752 247.256 29.0131 247.527 28.8889C247.817 28.7647 248.03 28.6129 248.166 28.4336L252.871 21.9967C252.91 21.9277 252.997 21.776 253.133 21.5414C253.287 21.293 253.365 21.1274 253.365 21.0447C253.365 20.9619 253.258 20.8929 253.045 20.8377C252.852 20.7825 252.61 20.7549 252.319 20.7549H245.988C245.716 20.7549 245.484 20.6859 245.29 20.5479C245.097 20.41 245 20.2375 245 20.0305V19.5545C245 19.3613 245.097 19.1957 245.29 19.0577C245.484 18.906 245.716 18.8301 245.988 18.8301H252.319C252.61 18.8301 252.852 18.7611 253.045 18.6231C253.258 18.4851 253.365 18.3126 253.365 18.1057V14.6285C253.365 14.4354 253.258 14.2698 253.045 14.1318C252.832 13.98 252.59 13.9041 252.319 13.9041H245.988C245.716 13.9041 245.484 13.8351 245.29 13.6972C245.097 13.5592 245 13.3867 245 13.1797V12.7037ZM271.595 18.9749C271.595 19.1819 271.682 19.3544 271.856 19.4924C272.03 19.6304 272.234 19.6993 272.466 19.6993C272.699 19.6993 272.902 19.7752 273.076 19.927C273.25 20.065 273.337 20.2306 273.337 20.4237V20.8791C273.337 21.0723 273.25 21.2447 273.076 21.3965C272.902 21.5345 272.699 21.6035 272.466 21.6035C272.234 21.6035 272.03 21.6794 271.856 21.8312C271.682 21.9691 271.595 22.1347 271.595 22.3279V28.3715C271.595 28.5784 271.682 28.7509 271.856 28.8889C272.03 29.0269 272.234 29.0959 272.466 29.0959C272.699 29.0959 272.902 29.1648 273.076 29.3028C273.25 29.4408 273.337 29.6133 273.337 29.8203V30.2756C273.337 30.4688 273.241 30.6344 273.047 30.7723C272.853 30.9241 272.621 31 272.35 31H268.197C267.925 31 267.693 30.9241 267.499 30.7723C267.306 30.6344 267.209 30.4688 267.209 30.2756V29.8203C267.209 29.6133 267.286 29.4408 267.441 29.3028C267.616 29.1648 267.838 29.0959 268.109 29.0959C268.342 29.0959 268.535 29.0269 268.69 28.8889C268.845 28.7509 268.923 28.5784 268.923 28.3715V22.3279C268.923 22.1347 268.845 21.9691 268.69 21.8312C268.535 21.6794 268.342 21.6035 268.109 21.6035C267.838 21.6035 267.616 21.5345 267.441 21.3965C267.286 21.2585 267.209 21.0861 267.209 20.8791V20.4237C267.209 20.2168 267.286 20.0443 267.441 19.9063C267.616 19.7683 267.838 19.6993 268.109 19.6993C268.342 19.6993 268.535 19.6304 268.69 19.4924C268.845 19.3544 268.923 19.1819 268.923 18.9749V14.6078C268.923 14.4147 268.845 14.2491 268.69 14.1111C268.535 13.9593 268.342 13.8834 268.109 13.8834C267.838 13.8834 267.616 13.8214 267.441 13.6972C267.286 13.5592 267.209 13.3867 267.209 13.1797V12.7037C267.209 12.5105 267.306 12.345 267.499 12.207C267.693 12.069 267.925 12 268.197 12H272.35C272.621 12 272.853 12.069 273.047 12.207C273.241 12.345 273.337 12.5105 273.337 12.7037V13.1797C273.337 13.3729 273.25 13.5385 273.076 13.6765C272.902 13.8144 272.699 13.8834 272.466 13.8834C272.234 13.8834 272.03 13.9593 271.856 14.1111C271.682 14.2491 271.595 14.4147 271.595 14.6078V18.9749ZM277.81 13.8834C277.52 13.8834 277.278 13.9593 277.084 14.1111C276.891 14.2491 276.794 14.4147 276.794 14.6078V16.1394C276.794 16.3464 276.891 16.5189 277.084 16.6569C277.278 16.7948 277.52 16.8638 277.81 16.8638H277.985C278.256 16.8638 278.488 16.7948 278.682 16.6569C278.895 16.5189 279.001 16.3464 279.001 16.1394V14.6078C279.001 14.4147 278.895 14.2491 278.682 14.1111C278.488 13.9593 278.256 13.8834 277.985 13.8834H277.81ZM283.677 14.6078C283.677 14.4147 283.581 14.2491 283.387 14.1111C283.193 13.9593 282.951 13.8834 282.661 13.8834C282.39 13.8834 282.157 13.9593 281.964 14.1111C281.77 14.2491 281.673 14.4147 281.673 14.6078V16.1394C281.673 16.3464 281.77 16.5189 281.964 16.6569C282.157 16.7948 282.39 16.8638 282.661 16.8638C282.951 16.8638 283.193 16.7948 283.387 16.6569C283.581 16.5189 283.677 16.3464 283.677 16.1394V14.6078ZM277.985 21.2516C278.256 21.2516 278.488 21.1826 278.682 21.0447C278.895 20.8929 279.001 20.7204 279.001 20.5272V19.5131C279.001 19.3061 278.895 19.1336 278.682 18.9956C278.488 18.8439 278.256 18.768 277.985 18.768H277.81C277.52 18.768 277.278 18.8439 277.084 18.9956C276.891 19.1336 276.794 19.3061 276.794 19.5131V20.5272C276.794 20.7204 276.891 20.8929 277.084 21.0447C277.278 21.1826 277.52 21.2516 277.81 21.2516H277.985ZM282.661 21.2516C282.951 21.2516 283.193 21.1826 283.387 21.0447C283.581 20.8929 283.677 20.7204 283.677 20.5272V19.5131C283.677 19.3061 283.581 19.1336 283.387 18.9956C283.193 18.8439 282.951 18.768 282.661 18.768C282.39 18.768 282.157 18.8439 281.964 18.9956C281.77 19.1336 281.673 19.3061 281.673 19.5131V20.5272C281.673 20.7204 281.77 20.8929 281.964 21.0447C282.157 21.1826 282.39 21.2516 282.661 21.2516ZM329.606 30.4826C329.393 30.6344 329.122 30.7585 328.793 30.8551C328.463 30.9517 328.154 31 327.863 31H312.615C312.344 31 312.111 30.9724 311.918 30.9172C311.724 30.862 311.627 30.793 311.627 30.7102V12.7037C311.627 12.5105 311.724 12.345 311.918 12.207C312.111 12.069 312.344 12 312.615 12H330.012C330.284 12 330.516 12.069 330.71 12.207C330.903 12.345 331 12.5105 331 12.7037V28.744C331 28.9372 330.932 29.1511 330.797 29.3856C330.661 29.634 330.497 29.8203 330.303 29.9444L329.606 30.4826ZM328.299 14.6285C328.299 14.4354 328.202 14.2698 328.008 14.1318C327.815 13.98 327.573 13.9041 327.282 13.9041H315.345C315.054 13.9041 314.812 13.98 314.619 14.1318C314.425 14.2698 314.328 14.4354 314.328 14.6285V28.3301C314.328 28.537 314.425 28.7095 314.619 28.8475C314.812 28.9855 315.054 29.0545 315.345 29.0545L327.282 29.0752C327.573 29.0752 327.815 29.0062 328.008 28.8682C328.202 28.7302 328.299 28.5577 328.299 28.3508V14.6285ZM319.382 22.0381C319.15 22.1485 318.85 22.2451 318.482 22.3279C318.114 22.4107 317.794 22.4521 317.523 22.4521H316.129C315.839 22.4521 315.597 22.3831 315.403 22.2451C315.209 22.0933 315.113 21.9208 315.113 21.7277V21.2516C315.113 21.0447 315.209 20.8722 315.403 20.7342C315.597 20.5962 315.839 20.5272 316.129 20.5272H316.884C317.175 20.5272 317.446 20.5134 317.698 20.4858C317.969 20.4444 318.143 20.403 318.22 20.3617C318.278 20.3341 318.307 20.2996 318.307 20.2582C318.307 20.1754 318.153 20.0719 317.843 19.9477C317.63 19.8787 317.349 19.8235 317 19.7821C316.671 19.7269 316.371 19.6993 316.1 19.6993C315.829 19.6993 315.597 19.6304 315.403 19.4924C315.209 19.3406 315.113 19.1612 315.113 18.9542V18.5403C315.113 18.3333 315.209 18.1609 315.403 18.0229C315.597 17.8711 315.839 17.7952 316.129 17.7952H316.826C317.31 17.7952 317.93 17.9056 318.685 18.1264L320.282 18.6645C320.476 18.7473 320.718 18.7887 321.009 18.7887C321.396 18.7887 321.735 18.7197 322.025 18.5817L324 17.6503C324.542 17.3882 324.813 17.2295 324.813 17.1743C324.813 17.1053 324.484 17.0708 323.826 17.0708H318.249C317.978 17.0708 317.668 17.0225 317.32 16.9259C316.991 16.8293 316.729 16.7121 316.536 16.5741L315.839 16.0773C315.626 15.9256 315.451 15.7462 315.316 15.5392C315.18 15.3322 315.113 15.1598 315.113 15.0218C315.113 14.87 315.209 14.7389 315.403 14.6285C315.597 14.5182 315.839 14.463 316.129 14.463H316.826C317.117 14.463 317.349 14.4975 317.523 14.5664C317.717 14.6216 317.814 14.7044 317.814 14.8148C317.814 14.9252 317.91 15.0149 318.104 15.0839C318.298 15.1391 318.54 15.1667 318.83 15.1667H326.527C326.798 15.1667 327.031 15.2426 327.224 15.3943C327.437 15.5323 327.544 15.6979 327.544 15.8911V17.3606C327.544 17.5675 327.466 17.7814 327.311 18.0022C327.156 18.2091 326.943 18.3747 326.672 18.4989L324.61 19.5131C324.378 19.6097 324.262 19.7338 324.262 19.8856C324.262 20.0236 324.378 20.134 324.61 20.2168C324.843 20.3134 325.143 20.3893 325.511 20.4444C325.898 20.4996 326.237 20.5272 326.527 20.5272C326.798 20.5272 327.031 20.5962 327.224 20.7342C327.437 20.8722 327.544 21.0447 327.544 21.2516V21.7277C327.544 21.9208 327.437 22.0933 327.224 22.2451C327.031 22.3831 326.798 22.4521 326.527 22.4521H325.917C325.646 22.4521 325.327 22.4245 324.959 22.3693C324.591 22.3003 324.271 22.2175 324 22.1209L322.141 21.4586C321.909 21.3758 321.657 21.3344 321.386 21.3344C320.98 21.3344 320.641 21.4103 320.37 21.5621L319.382 22.0381ZM326.091 24.915C325.917 25.0668 325.656 25.191 325.307 25.2876C324.978 25.3704 324.668 25.4118 324.378 25.4118H316.129C315.839 25.4118 315.597 25.3428 315.403 25.2048C315.209 25.0668 315.113 24.8943 315.113 24.6874V24.232C315.113 24.0389 315.209 23.8733 315.403 23.7353C315.597 23.5835 315.839 23.5076 316.129 23.5076H323.826C324.116 23.5076 324.358 23.48 324.552 23.4248C324.746 23.3558 324.842 23.2662 324.842 23.1558C324.842 23.0454 324.939 22.9626 325.133 22.9074C325.327 22.8384 325.578 22.8039 325.888 22.8039H326.527C326.798 22.8039 327.031 22.8591 327.224 22.9695C327.437 23.0799 327.544 23.211 327.544 23.3627C327.544 23.5007 327.476 23.6732 327.34 23.8802C327.205 24.0871 327.031 24.2665 326.818 24.4183L326.091 24.915ZM316.129 28.2473C315.839 28.2473 315.597 28.1783 315.403 28.0403C315.209 27.9023 315.113 27.7298 315.113 27.5229V27.0675C315.113 26.8744 315.209 26.7088 315.403 26.5708C315.597 26.419 315.839 26.3431 316.129 26.3431H323.826C324.116 26.3431 324.358 26.3086 324.552 26.2397C324.746 26.1707 324.842 26.0879 324.842 25.9913C324.842 25.8809 324.939 25.7912 325.133 25.7222C325.346 25.6532 325.598 25.6187 325.888 25.6187H326.527C326.856 25.6187 327.098 25.6739 327.253 25.7843C327.427 25.8947 327.515 26.0396 327.515 26.219C327.515 26.3707 327.447 26.5432 327.311 26.7364C327.195 26.9158 327.031 27.0813 326.818 27.2331L326.091 27.7298C325.917 27.8816 325.656 28.0058 325.307 28.1024C324.978 28.199 324.668 28.2473 324.378 28.2473H316.129Z' fill='white' />
  </g>
  <defs>
    <filter id='filter0_d_224_55' x='0.720093' y='5.48401' width='196.968' height='41.038' filterUnits='userSpaceOnUse' color-interpolation-filters='sRGB'>
      <feFlood flood-opacity='0' result='BackgroundImageFix' />
      <feColorMatrix in='SourceAlpha' type='matrix' values='0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0' result='hardAlpha' />
      <feOffset dy='4' />
      <feGaussianBlur stdDeviation='4' />
      <feComposite in2='hardAlpha' operator='out' />
      <feColorMatrix type='matrix' values='0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.2 0' />
      <feBlend mode='normal' in2='BackgroundImageFix' result='effect1_dropShadow_224_55' />
      <feBlend mode='normal' in='SourceGraphic' in2='effect1_dropShadow_224_55' result='shape' />
    </filter>
    <filter id='filter1_d_224_55' x='203.091' y='11.7774' width='28.4451' height='28.4451' filterUnits='userSpaceOnUse' color-interpolation-filters='sRGB'>
      <feFlood flood-opacity='0' result='BackgroundImageFix' />
      <feColorMatrix in='SourceAlpha' type='matrix' values='0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0' result='hardAlpha' />
      <feOffset dy='4' />
      <feGaussianBlur stdDeviation='4' />
      <feComposite in2='hardAlpha' operator='out' />
      <feColorMatrix type='matrix' values='0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.2 0' />
      <feBlend mode='normal' in2='BackgroundImageFix' result='effect1_dropShadow_224_55' />
      <feBlend mode='normal' in='SourceGraphic' in2='effect1_dropShadow_224_55' result='shape' />
    </filter>
    <filter id='filter2_d_224_55' x='237' y='0.186157' width='102' height='52.3354' filterUnits='userSpaceOnUse' color-interpolation-filters='sRGB'>
      <feFlood flood-opacity='0' result='BackgroundImageFix' />
      <feColorMatrix in='SourceAlpha' type='matrix' values='0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0' result='hardAlpha' />
      <feOffset dy='4' />
      <feGaussianBlur stdDeviation='4' />
      <feComposite in2='hardAlpha' operator='out' />
      <feColorMatrix type='matrix' values='0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.2 0' />
      <feBlend mode='normal' in2='BackgroundImageFix' result='effect1_dropShadow_224_55' />
      <feBlend mode='normal' in='SourceGraphic' in2='effect1_dropShadow_224_55' result='shape' />
    </filter>
  </defs>
</svg>`
);

USER_POSITION_FOLLOW_BUTTON.innerHTML = USER_POSITION_FOLLOW_BUTTON_ICON;
USER_POSITION_FOLLOW_BUTTON.setAttribute('id', 'user-position-follow')
MARKER_STYLE.setAttribute('version', INJECT_Version);
MARKER_STYLE.innerHTML = `
* {
  user-select: none;
}

:root {
  --cv-marker-display: none;
  --cv-marker-track-display: none;
  --cv-marker-rotate: 0deg;
  --cv-marker-fov-rotate: -45deg;
  --cv-marker-track-saturate: 0%;
}

.user-position {
  display: var(--cv-marker-display);
  position: relative;
  width: ${INJECT_ICON_CONFIG.width}px;
  height: ${INJECT_ICON_CONFIG.height}px;
}

.user-position::after {
  top: 0;
  right: 0;
  width: 100%;
  height: 100%;
  background-image: url(${INJECT_ICON_URL});
  transform: rotate(var(--cv-marker-rotate));
}

.user-position::before {
  bottom: 50%;
  left: 50%;
  width: 250%;
  height: 250%;
  background-image: radial-gradient(100% 100% at 0% 100%, rgba(255,255,255,.8) 0%, rgba(255,255,255,.8) 0%, rgba(255,255,255,0.00) 100%, rgba(255,255,255,0.00) 100%);
  transform-origin: bottom left;
  transform: rotate(var(--cv-marker-fov-rotate));
}

.user-position,
.user-position::after,
.user-position::before {
  content: '';
  position: absolute;
  background-size: 100%;
  transition: transform 150ms linear;
}


#user-position-follow {
  --button-scale: 100%;
  display: var(--cv-marker-track-display);
  align-items: center;
  justify-content: center;
  z-index: 1000;
  width: ${INJECT_BUTTON_CONFIG.width}px;
  height: ${INJECT_BUTTON_CONFIG.height}px;
  background: rgb(50 50 50 / 86%);
  backdrop-filter: blur(5px);
  border-radius: 50%;
  border: none;
  position: fixed;
  bottom: 28px;
  left: 50%;
  transform: translateX(-50%) scale(var(--button-scale));
  outline: none;
  user-select: none;
  box-shadow: 0px 7px 10px rgba(0, 0, 0, 0.3);

  transition: transform 150ms, opacity 150ms;
}

#user-position-follow:hover {
  --button-scale: 115%;
}

#user-position-follow:active {
  transition: transform 50ms, opacity 50ms;
  --button-scale: 110%;
  opacity: 70%
}

#user-position-follow ._icon {
  display: inline-block;
  filter: saturate(var(--cv-marker-track-saturate));
}

#app-logo {
  pointer-events: none;
  position: fixed;
  top: 23px;
  left: 28px;
  z-index: 1000;
  width: 339px;
  height: 53px;
}

.switch_content {
  left: 32px !important;
  bottom: 32px !important;
}

#map::after {
  content: """";
  position: absolute;
  z-index: 400;
  top: 0;
  left: 0;
  bottom: 0;
  right: 0;
  pointer-events: none;
  background-image: radial-gradient(transparent 44%,rgba(0,0,0,.5));
}
`;

class CVAutoTrack {
  // Init
  constructor() {
    this._last_x = 0;
    this._last_y = 0;
    this._last_deg = 0;
    this.GS_MAP = null;
    this.GS_L = null;
    this.GS_USER_MARKER = null;
    this.GS_USER_MARKER_VISIBLE = false;
    this.GS_USER_TRACKING = false;

    if (map && L) this._init();
    else window.addEventListener('load', () => {
      this._init()
    });
  }

  _init() {
    if (!map || !L) return;

    this.GS_MAP = map;
    this.GS_L = L;

    this._initMarker();
    this._initStyle();
    this._pageStyleRebuild();
    this._initTrackButton();
    this._hookMapEmitter();


    console.warn(`[${INJECT_NAME}] Injected!`);
  }

  _pageStyleRebuild() {
    [
      "".leaflet-control-attribution"",
      "".genshin_pub"",
      "".genshin_art"",
      "".feixiaoqiu"",
      "".wiki"",
      "".danjixiazai"",
      "".fankui"",
      "".enindex-btn"",
      "".jpindex-btn""
    ].forEach(className => {
      document
        .querySelector(className)
        .style
        .display = ""none""
    });
    document.body.appendChild(APP_LOGO_CONTAINER);
  }

  _hookMapEmitter() {
    const MAP_DOCUMENT_NODE = document.querySelector('#map');
    const MAP_AREALIST = document.querySelector('.area-list-containor');


    if (!MAP_DOCUMENT_NODE) {
      console.error(`[${INJECT_NAME}] Map container can't found!`);
      return;
    }

    const cancelTracking = () => {
      this.track(false);
    }

    // cancel tracking
    MAP_DOCUMENT_NODE.addEventListener('mousedown', cancelTracking);
    MAP_AREALIST.addEventListener('click', cancelTracking);
    MAP_DOCUMENT_NODE.addEventListener('wheel', cancelTracking);
  }

  _initMarker() {
    const _MARKER = L
      .divIcon({
        iconSize: [
          INJECT_ICON_CONFIG.width,
          INJECT_ICON_CONFIG.height
        ],
        className: 'user-position',
        alt: '',
      });

    this.GS_USER_MARKER = this.GS_L
      .marker([0, 0], { icon: _MARKER })
      .addTo(this.GS_MAP);
  }

  _initStyle() {
    document.head.appendChild(MARKER_STYLE);
  }

  _initTrackButton() {
    document.body.appendChild(USER_POSITION_FOLLOW_BUTTON);

    // follow
    USER_POSITION_FOLLOW_BUTTON.addEventListener('click', (e) => {
      this.track(!this.GS_USER_TRACKING);
    })
  }

  _setProperty(key, value) {
    document.documentElement.style.setProperty(key, value);
  }

  getTrackState() {
    return this.GS_USER_TRACKING;
  }

  getVisible() {
    return this.GS_USER_MARKER_VISIBLE;
  }

  visible(visible = true) {
    this.GS_USER_MARKER_VISIBLE = visible;
    this._setProperty('--cv-marker-display', visible ? 'block' : 'none');
    this._setProperty('--cv-marker-track-display', visible ? 'flex' : 'none');

  }

  track(track = true) {
    this.GS_USER_TRACKING = track;
    this.updatePosition({});

    this._setProperty('--cv-marker-track-saturate', `${track ? 100 : 0}%`);
  }

  updatePosition({ x, y, deg }) {
    const _x = x ? x : this._last_x;
    const _y = y ? y : this._last_y;
    const _deg = deg ? deg : this._last_deg;

    this._last_x = _x;
    this._last_y = _y;
    this._last_deg = _deg;
    this.GS_USER_MARKER.setLatLng([_x, _y]);

    this._setProperty('--cv-marker-rotate', `${-_deg}deg`);

    if (this.GS_USER_TRACKING) {
      this.GS_MAP.panTo([_x, _y])
    }
  }

  updateFov(fov) {
    this._setProperty('--cv-marker-fov-rotate', `${-fov-45}deg`);
  }
};

window.__CVAutoTrack__ = new CVAutoTrack();

window.__CVAutoTrack__.visible();
                        ";
            webView.ExecuteScriptAsync(js);


            messageWebSocket = new Windows.Networking.Sockets.MessageWebSocket();
            messageWebSocket.Control.MessageType = Windows.Networking.Sockets.SocketMessageType.Utf8;
            messageWebSocket.MessageReceived += MessageWebSocket_MessageReceived;
            messageWebSocket.SetRequestHeader("Origin", "http://localhost:32333");
            
            Task connectTask = messageWebSocket.ConnectAsync(new Uri(urlws + token)).AsTask();

                connectTask.ContinueWith(_ =>
                  {
                      {
                          string id = DateTime.Now.ToString() + Math.Round(1000 + Math.Round((decimal)100, 0) * 1000).ToString();
                          JsonObject reqjson = new JsonObject();
                          reqjson.Add("id", JsonValue.CreateStringValue(id));
                          reqjson.Add("action", JsonValue.CreateStringValue("api"));
                          JsonObject data = new JsonObject();
                          data.Add("url", JsonValue.CreateStringValue("/api/cvautotrack"));
                          data.Add("method", JsonValue.CreateStringValue("POST"));
                          data.Add("body", JsonValue.CreateStringValue(@"{""action"":""load"",""data"":[]}"));
                          reqjson.Add("data", data);
                          SendMessageUsingMessageWebSocketAsync(reqjson.ToString());
                      }
                      {
                          string id = DateTime.Now.ToString() + Math.Round(1000 + Math.Round((decimal)100, 0) * 1000).ToString();
                          JsonObject reqjson = new JsonObject();
                          reqjson.Add("id", JsonValue.CreateStringValue(id));
                          reqjson.Add("action", JsonValue.CreateStringValue("api"));
                          JsonObject data = new JsonObject();
                          data.Add("url", JsonValue.CreateStringValue("/api/cvautotrack"));
                          data.Add("method", JsonValue.CreateStringValue("POST"));
                          data.Add("body", JsonValue.CreateStringValue(@"{""action"":""init"",""data"":[]}"));
                          reqjson.Add("data", data);
                          SendMessageUsingMessageWebSocketAsync(reqjson.ToString());
                      }
                      {
                          string id = DateTime.Now.ToString() + Math.Round(1000 + Math.Round((decimal)100, 0) * 1000).ToString();
                          JsonObject reqjson = new JsonObject();
                          reqjson.Add("id", JsonValue.CreateStringValue(id));
                          reqjson.Add("action", JsonValue.CreateStringValue("api"));
                          JsonObject data = new JsonObject();
                          data.Add("url", JsonValue.CreateStringValue("/api/cvautotrack"));
                          data.Add("method", JsonValue.CreateStringValue("POST"));
                          data.Add("body", JsonValue.CreateStringValue(@"{""action"":""sub"",""data"":[]}"));
                          reqjson.Add("data", data);
                          SendMessageUsingMessageWebSocketAsync(reqjson.ToString());
                      }
                  });
        }

        private async Task SendMessageUsingMessageWebSocketAsync(string message)
        {            // 检查token是否有效
            if (token != "")
            {
                using (var dataWriter = new DataWriter(this.messageWebSocket.OutputStream))
                {
                    dataWriter.WriteString(message);
                    await dataWriter.StoreAsync();
                    dataWriter.DetachStream();
                }
            }
            else
            {
                token = await getTokenAsync();
            }
        }
        private void MessageWebSocket_MessageReceived(Windows.Networking.Sockets.MessageWebSocket sender, Windows.Networking.Sockets.MessageWebSocketMessageReceivedEventArgs args)
        {
            // 获取接收到的字符串
            using (DataReader dataReader = args.GetDataReader())
            {
                //字符串解析为json
                string json = dataReader.ReadString(dataReader.UnconsumedBufferLength);
                
                double x = 0;
                double y = 0;
                double a = 0;
                double a2 = 0;

                // 解析json中的action是否为cvautotrack
                JsonObject body = JsonObject.Parse(json);
                string action = body.GetNamedString("action");
                if (action == "cvautotrack")
                {
                    // 解析json中的data，如果不为空就解析其中的x,y,a,a2
                    JsonArray data = body.GetNamedArray("data");
                    x = data[0].GetNumber();
                    y = data[1].GetNumber();
                    a = data[2].GetNumber();
                    a2 = data[3].GetNumber();

                    updateAvatarInfo(x, y, a, a2);


                }
            }


        }
        private async void updateAvatarInfo(double x, double y, double a, double a2)
        {
            x = x / 1.5;
            y = y / 1.5;

            string js = "window.__CVAutoTrack__.updatePosition({ x: " + x.ToString() + ", y:" + y.ToString() + ", deg: " + a.ToString() + "});\n"
                + "window.__CVAutoTrack__.updateFov(" + a2.ToString() + ");";

            bool uiAccess = webView.Dispatcher.HasThreadAccess;
            if (uiAccess)
                webView.ExecuteScriptAsync(js);
            else
                await webView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    webView.ExecuteScriptAsync(js);
                });

        }

        /// <summary>
        /// 获取椰羊服务端的token
        /// </summary>
        /// <returns>token</returns>
        private async Task<string> getTokenAsync()
        {
            string url = "http://localhost:32333/token";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var kvp = new List<KeyValuePair<string, string>>
                    { };
                    client.DefaultRequestHeaders.Add(new KeyValuePair<string, string>("Origin", "http://localhost:32333"));
                    HttpResponseMessage response = await client.PostAsync(new Uri(url), new HttpFormUrlEncodedContent(kvp));
                    if (response.EnsureSuccessStatusCode().StatusCode.ToString() == "Created")
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        JsonObject body = JsonObject.Parse(responseBody);
                        string token = body["token"].GetString();
                        return token;
                    }
                }
                catch (Exception ex)
                {
                    return "";
                }
            }
            return "";
        }
    }
}
