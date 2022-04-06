using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 空荧酒馆_悬浮窗
{


    internal class 工具类
    {

        public struct 经纬度坐标
        {
            public double 经度;
            public double 纬度;
        }
        public struct 平面坐标
        {
            public double 横坐标;
            public double 纵坐标;

        }

        public 平面坐标 坐标转换_从经纬度(double 传入经度,double 传入纬度)
        {
            经纬度坐标 坐标;
            坐标.纬度 = 传入纬度;
            坐标.经度 = 传入经度;
            return 坐标转换_从经纬度(坐标);
        }
        public 经纬度坐标 坐标转换_从平面(double 传入横坐标,double 传入纵坐标)
        {
            平面坐标 坐标;
            坐标.横坐标 = 传入横坐标;
            坐标.纵坐标 = 传入纵坐标;
            return 坐标转换_从平面(坐标);
        }
        public 平面坐标 坐标转换_从经纬度(经纬度坐标 传入坐标)
        {
            平面坐标 返回坐标;
            返回坐标.横坐标 = 传入坐标.经度;
            返回坐标.纵坐标 = 传入坐标.纬度;
            return 返回坐标;
        }
        public 经纬度坐标 坐标转换_从平面(平面坐标 传入坐标)
        {
            经纬度坐标 返回坐标;
            返回坐标.经度 = 传入坐标.纵坐标;
            返回坐标.纬度 = 传入坐标.横坐标;
            return 返回坐标;
        }
    }
}
