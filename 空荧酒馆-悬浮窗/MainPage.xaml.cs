using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
using Windows.Data.Json;
using System.Timers;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace 空荧酒馆_悬浮窗
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public string token = "";
        //public HttpClient httpClientWS = new HttpClient();
        string urlws = "http://localhost:32333/ws/";
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
            
            // 定时器100ms触发一次
            Timer timer = new Timer();
            timer.Interval = 5000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // 检查token是否有效
            if (token != "")
            {
                getPosition();
                //getDirection();
                //getRotation();
            }
            else
            {
                token = await getTokenAsync();
            }
        }
        private async void getPosition()
        {
            string id = DateTime.Now.ToString() + Math.Round(1000 + Math.Round((decimal)100, 0) * 1000).ToString();
            JsonObject body = new JsonObject();
            body.Add("action", JsonValue.CreateStringValue("GetPosition"));
            body.Add("data", JsonValue.CreateStringValue(""));

            JsonObject reqjson = new JsonObject();
            reqjson.Add("id", JsonValue.CreateStringValue(id));
            reqjson.Add("action", JsonValue.CreateStringValue("api"));
            JsonObject data = new JsonObject();
            data.Add("url", JsonValue.CreateStringValue("/api/cvautotrack"));
            data.Add("method", JsonValue.CreateStringValue("POST"));
            data.Add("body", body);
            reqjson.Add("data", data);
            
            HttpClient httpClientWS = new HttpClient();
            httpClientWS.DefaultRequestHeaders.Clear();
            httpClientWS.DefaultRequestHeaders.Add("Origin", "http://localhost:32333");
            HttpResponseMessage response = await httpClientWS.PostAsync(new Uri(urlws + token), new HttpStringContent(reqjson.ToString(), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
            string responseBody = await response.Content.ReadAsStringAsync();

            JsonObject pos = JsonObject.Parse(responseBody);
            if (pos.GetNamedString("body").Contains("status"))
            {
                JsonObject data1 = JsonObject.Parse(pos.GetNamedString("body"));
                if (data1.GetNamedBoolean("status"))
                {
                    JsonArray data2 = JsonArray.Parse(data1.GetNamedString("data"));
                    updatePosition(data2.GetNumberAt(0), data2.GetNumberAt(1));
                }
            }
        }
        private async void getDirection()
        {
            string id = DateTime.Now.ToString() + Math.Round(1000 + Math.Round((decimal)100, 0) * 1000).ToString();
            JsonObject body = new JsonObject();
            body.Add("action", JsonValue.CreateStringValue("GetDirection"));
            body.Add("data", JsonValue.CreateStringValue(""));

            JsonObject reqjson = new JsonObject();
            reqjson.Add("id", JsonValue.CreateStringValue(id));
            reqjson.Add("action", JsonValue.CreateStringValue("api"));
            JsonObject data = new JsonObject();
            data.Add("url", JsonValue.CreateStringValue("/api/cvautotrack"));
            data.Add("method", JsonValue.CreateStringValue("POST"));
            data.Add("body", body);
            reqjson.Add("data", data);
            
            HttpClient httpClientWS = new HttpClient();
            httpClientWS.DefaultRequestHeaders.Clear();
            httpClientWS.DefaultRequestHeaders.Add("Origin", "http://localhost:32333");
            HttpResponseMessage response = await httpClientWS.PostAsync(new Uri(urlws + token), new HttpStringContent(reqjson.ToString(), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
            string responseBody = await response.Content.ReadAsStringAsync();

            JsonObject dir = JsonObject.Parse(responseBody);
            if (dir.GetNamedString("body").Contains("status"))
            {
                JsonObject data1 = JsonObject.Parse(dir.GetNamedString("body"));
                if (data1.GetNamedBoolean("status"))
                {
                    updatePosition(0 - data1.GetNamedNumber("data"));
                }
            }

        }
        private async void getRotation()
        {
            string id = DateTime.Now.ToString() + Math.Round(1000 + Math.Round((decimal)100, 0) * 1000).ToString();
            JsonObject body = new JsonObject();
            body.Add("action", JsonValue.CreateStringValue("GetRotation"));
            body.Add("data", JsonValue.CreateStringValue(""));

            JsonObject reqjson = new JsonObject();
            reqjson.Add("id", JsonValue.CreateStringValue(id));
            reqjson.Add("action", JsonValue.CreateStringValue("api"));
            JsonObject data = new JsonObject();
            data.Add("url", JsonValue.CreateStringValue("/api/cvautotrack"));
            data.Add("method", JsonValue.CreateStringValue("POST"));
            data.Add("body", body);
            reqjson.Add("data", data);
            
            HttpClient httpClientWS = new HttpClient();
            httpClientWS.DefaultRequestHeaders.Clear();
            httpClientWS.DefaultRequestHeaders.Add("Origin", "http://localhost:32333");
            HttpResponseMessage response = await httpClientWS.PostAsync(new Uri(urlws + token), new HttpStringContent(reqjson.ToString(), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
            string responseBody = await response.Content.ReadAsStringAsync();

            JsonObject rot = JsonObject.Parse(responseBody);
            if (rot.GetNamedString("body").Contains("status"))
            {
                JsonObject data1 = JsonObject.Parse(rot.GetNamedString("body"));
                if (data1.GetNamedBoolean("status"))
                {
                    updateFov(0 - data1.GetNamedNumber("data"));
                }
            }
        }

        private async void updatePosition(double x, double y)
        {
            string js = "window.__CVAutoTrack__.updatePosition({ x: "+x.ToString()+", y:"+y.ToString()+"});" ;
            await webView.ExecuteScriptAsync(js);
        }


        private async void updatePosition(double a)
        {
            string js = "window.__CVAutoTrack__.updatePosition({ deg: " + a.ToString() + "});";
            await webView.ExecuteScriptAsync(js);
        }


        private async void updateFov(double a)
        {
            string js = "window.__CVAutoTrack__.updateFov(" + a.ToString() + ");";
            await webView.ExecuteScriptAsync(js);
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            webView.CoreWebView2.OpenDevToolsWindow();
        }


        private void CoreWebView2_DOMContentLoaded(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2DOMContentLoadedEventArgs args)
        {
            string js = @" 
const INJECT_NAME = 'CVAutoTrack';
const INJECT_Version = '1.0';
const INJECT_ICON_URL = 'https://assets.yuanshen.site/icons/MapPoint.png';
const INJECT_ICON_FOV_URL = 'https://assets.yuanshen.site/icons/MapPointFov.png';
const INJECT_ICON_CONFIG = {
  width: 52,
  height: 52,
};
const INJECT_Button_CONFIG = {
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

USER_POSITION_FOLLOW_BUTTON.innerHTML = USER_POSITION_FOLLOW_BUTTON_ICON;
USER_POSITION_FOLLOW_BUTTON.setAttribute('id', 'user-position-follow')
MARKER_STYLE.setAttribute('version', INJECT_Version);
MARKER_STYLE.innerHTML = `
:root {
  --cv-marker-display: none;
  --cv-marker-track-display: none;
  --cv-marker-rotate: 0deg;
  --cv-marker-fov-rotate: 0deg;
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
  top: -100%;
  right: -100%;
  width: 300%;
  height: 300%;
  background-image: url(${INJECT_ICON_FOV_URL});
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
  width: ${INJECT_Button_CONFIG.width}px;
  height: ${INJECT_Button_CONFIG.height}px;
  background: #323232;
  border-radius: 50%;
  border: none;
  position: fixed;
  bottom: 24px;
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
    this._initTrackButton();
    this._hookMapEmitter();


    console.warn(`[${INJECT_NAME}] Injected!`);
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

    this._setProperty('--cv-marker-rotate', `${_deg}deg`);

    if (this.GS_USER_TRACKING) {
      this.GS_MAP.panTo([_x, _y])
    }
  }

  updateFov(fov) {
    this._setProperty('--cv-marker-fov-rotate', `${fov}deg`);
  }
};

window.__CVAutoTrack__ = new CVAutoTrack();
window.__CVAutoTrack__.visible();
";
           webView.ExecuteScriptAsync(js);
        }

        /// <summary>
        /// 地图自动平移到指定位置
        /// </summary>
        /// <param name="x">经度</param>
        /// <param name="y">纬度</param>
        private void flyto_js(float x,float y)
        {
            //工具类.平面坐标 a = 工具类.坐标转换_从经纬度(x, y);
            string x_string = x.ToString();
            string y_string = y.ToString();

            string js = "map.flyTo([" + x_string + "," + y_string + "]);";
            webView.ExecuteScriptAsync(js);
        }

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
