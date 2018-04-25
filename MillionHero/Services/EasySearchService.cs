using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDLib.Extensions;
using System.Windows.Forms;
using TDLib.Utils;

namespace MillionHero.Services
{
    /// <summary>
    /// 百度简单搜索APP相关接口
    /// </summary>
    public class EasySearchService
    {
        private static ILog _logger = LoggerManager.GetLogger<EasySearchService>();
        public const string EasySearch_HomePage_HtmlFile = "简单搜索首页.html";
        public const string EasySearch_BaiWanYingXiong_HtmlFile = "简单搜索(百万英雄专场).html";
        public const string EasySearch_ChongDingDaHui_HtmlFile = "简单搜索(冲顶大会专场).html";
        public const string EasySearch_HuaJiaoZhiBo_HtmlFile = "简单搜索(花椒直播专场).html";
        public const string EasySearch_ZhiShiChaoRen_HtmlFile = "简单搜索(芝士超人专场).html";

        public void UpdateHtmlFiles()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add(AppGlobals.EasySearchHomeUrl, EasySearch_HomePage_HtmlFile);
            dic.Add("http://secr.baidu.com/answer?app=xiguashipin", EasySearch_BaiWanYingXiong_HtmlFile);
            dic.Add("http://secr.baidu.com/answer?app=chongdingdahui", EasySearch_ChongDingDaHui_HtmlFile);
            dic.Add("http://secr.baidu.com/answer?app=huajiao", EasySearch_HuaJiaoZhiBo_HtmlFile);
            dic.Add("http://secr.baidu.com/answer?app=zhishichaoren", EasySearch_ZhiShiChaoRen_HtmlFile);

            string startupPath = Application.StartupPath;
            int total = dic.Count;
            int succ = 0;
            foreach (var item in dic)
            {
                string url = item.Key;
                string htmlFile = startupPath + "\\" + item.Value;
                try
                {
                    string result = GetHtml(url, true);
                    System.IO.File.WriteAllText(htmlFile, result);
                    succ++;
                }
                catch (Exception ex)
                {
                    _logger.Error("获取" + url + "内容失败", ex);
                }
            }
            if (total != succ)
            {
                // 存在失败
                throw new Exception(string.Format("{0}个文件更新成功,{1}个文件更新失败", succ, total - succ));
            }
        }

        private string GetHtml(string url, bool isFixHtml)
        {
            var req = HttpExtension.CreateGetRequest(url);
            req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            req.Headers.Add("Upgrade-Insecure-Requests", "1");
            req.UserAgent = "Mozilla/5.0 (Linux; Android 7.1.1; 1607-A01 Build/NMF26F; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/48.0.2564.116 Mobile Safari/537.36 T7/9.3 SearchCraft/1.6.2 (Baidu; P1 7.1.1)";
            req.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            req.Headers.Add("Accept-Language", "zh-CN,en-US;q=0.8");
            req.SetCookie("BAIDUID=F2B3F8A07DC67FA5AF5CF62FCD0988FB:FG=1; BAIDULOC=0.0_0.0_1000_null_1515921302763; io=6ot-BGSqxiUmM1-5AXLh; BAIDUCUID=luHi80usv8lF8vfql82etg8SBf_oa2fH0OvCigid2a8Xa28Jguvg8_ag2iYua2fHA");
            var resp = req.GetResponseAsHttp();
            var statusCode = resp.StatusCode;
            string statusDesc = resp.StatusDescription;
            var result = resp.GetBodyAsText();

            if (isFixHtml)
            {
                result = result.Replace("\"//", "\"http://")
                              .Replace("url('//", "url('http://");
            }

            /*
             * 解决由于一下代码parseUrl().app处理错误，导致所有的页面都指向了【百万英雄】
             * function urlMaker () {
                    var url = "//secr.baidu.com/nv/{path}/answer";
                    var path = parseUrl().app;
                    if (path) {
                        url = url.replace('{path}', path);
                    } else {
                        url = "//secr.baidu.com/nv/xiguashipin/answer";
                    }
                    return url;
                }
             */

            if (result.Contains("parseUrl().app"))
            {
                string pathValue = GetParaValueFromUrl(url, "app");
                if (pathValue != null)
                {
                    result = result.Replace("parseUrl().app", "\"" + pathValue + "\"");
                }
            }

            return result;
        }

        /// <summary>
        /// 从URL中获取参数值
        /// </summary>
        /// <param name="url">url地址</param>
        /// <param name="paraKey">参数名</param>
        /// <returns>如果不存在则返回null</returns>
        private string GetParaValueFromUrl(string url, string paraKey)
        {
            int index = url.IndexOf("?");
            if (index == -1)
            {
                return null;
            }

            string paras = url.Substring(index + 1);
            Dictionary<string,string> dic = paras.Split("&", "=");
            if (dic.ContainsKey(paraKey))
            {
                return dic[paraKey];
            }

            return null;
        }
    }
}
