using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace 空荧酒馆_悬浮窗
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class YuanShenSite_悬浮窗 : Page
    {
        public YuanShenSite_悬浮窗()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
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
                        ";
            webView.ExecuteScriptAsync(js);
        }

        private async void WebView_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void flyto(float x, float y)
        {

        }

        private void A_Click(object sender, RoutedEventArgs e)
        {
            string js = @"map.flyTo([4000,5000]);";
            webView.ExecuteScriptAsync(js);
        }
        private void B_Click(object sender, RoutedEventArgs e)
        {
            string js = @"window.injectCursor.style.left = '200px';
                           window.injectCursor.style.top = '200px';
                        window.injectCursor.style.visibility = 'visible'
                        ";
            webView.ExecuteScriptAsync(js);
        }
        private void C_Click(object sender, RoutedEventArgs e)
        {
            string js = @"window.injectCursor.style.transform = 'translate(-50%, -50%) rotate(-30deg)';";
            webView.ExecuteScriptAsync(js);
        }
    }
}
