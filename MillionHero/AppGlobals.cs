using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using TDLib.Utils;
using MillionHero.Definitions;
using Newtonsoft.Json;

namespace MillionHero
{
    class AppGlobals
    {
        public static List<QuestionAppParams> BaiWanYingXiongs = null;
        public static List<QuestionAppParams> ZhiShiChaoRens = null;
        public static Dictionary<QuestionAppType, string[]> QuestionAppApkDic = null;

        public static bool IsDebug { get; private set; }
        public static string BaiduApiKey { get; private set; }
        public static string BaiduSecretKey { get; private set; }
        public static string EasySearchHomeUrl { get; private set; }
        public static bool ShowCutImage { get; private set; }

        /// <summary>
        /// 调试模式发生改变事件
        /// </summary>
        public static event EventHandler DebugModeChanged;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static AppGlobals()
        {
            ReadAppParams();

            QuestionAppApkDic = new Dictionary<QuestionAppType, string[]>();
            QuestionAppApkDic.Add(QuestionAppType.BaiWanYingXiong, new string[] { "com.ss.android.article.video", "com.ss.android.article.video.activity.SplashActivity" });
            QuestionAppApkDic.Add(QuestionAppType.ZhiShiChaoRen, new string[] { "com.inke.trivia", "com.inke.trivia.splash.SplashActivity" });

            IsDebug = AppSettingsUtil.Instance.GetValue<bool>("debug", false);
            BaiduApiKey = AppSettingsUtil.Instance.GetValue<string>("baiduApiKey", null);
            BaiduSecretKey = AppSettingsUtil.Instance.GetValue<string>("baiduSecretKey", null);
            EasySearchHomeUrl = AppSettingsUtil.Instance.GetValue("easySearchHomeUrl", "http://secr.baidu.com/entry?status=1-1-1-1&version=5");
            ShowCutImage = AppSettingsUtil.Instance.GetValue<bool>("showCutImage", false);

            // 更新配置信息
            SaveEasySearchHomeUrl(EasySearchHomeUrl);
        }

        public static void ReadAppParams()
        {
            BaiWanYingXiongs = GetQuestionAppParamsList(QuestionAppType.BaiWanYingXiong);
            ZhiShiChaoRens = GetQuestionAppParamsList(QuestionAppType.ZhiShiChaoRen);
        }

        public static void SaveQuestionAppParams(QuestionAppType type, QuestionAppParams appParams)
        {
            string key = "questionAppParams_" + type + "_" + 
                appParams.ScreenSize.Width + "x" + appParams.ScreenSize.Height;
            string value = JsonConvert.SerializeObject(appParams);
            value = Base64Util.EncodeText(value);
            AppSettingsUtil.Instance.SetValue(key, value);
        }

        public static List<QuestionAppParams> GetQuestionAppParamsList(QuestionAppType type)
        {
            List<QuestionAppParams> list = new List<QuestionAppParams>();
            var keys = AppSettingsUtil.Instance.GetKeys();
            foreach (var key in keys)
            {
                if (key.StartsWith("questionAppParams_" + type))
                {
                    string value = AppSettingsUtil.Instance.GetValue<string>(key,null);
                    if (value != null)
                    {
                        value = Base64Util.DecodeText(value);
                        list.Add(JsonConvert.DeserializeObject<QuestionAppParams>(value));
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// 设置是否显示图片
        /// </summary>
        /// <param name="showCutImage"></param>
        public static void SaveShowCutImage(bool showCutImage)
        {
            ShowCutImage = showCutImage;
            AppSettingsUtil.Instance.SetValue("showCutImage", showCutImage);
        }

        /// <summary>
        /// 设置简单搜索主页
        /// </summary>
        /// <param name="easySearchHomeUrl"></param>
        public static void SaveEasySearchHomeUrl(string easySearchHomeUrl)
        {
            EasySearchHomeUrl = easySearchHomeUrl;
            AppSettingsUtil.Instance.SetValue("easySearchHomeUrl", easySearchHomeUrl);
        }

        /// <summary>
        /// 设置百度ApiKey
        /// </summary>
        /// <param name="baiduApiKey"></param>
        public static void SaveBaiduApiKey(string baiduApiKey)
        {
            BaiduApiKey = baiduApiKey;
            AppSettingsUtil.Instance.SetValue("baiduApiKey", baiduApiKey);
        }

        /// <summary>
        /// 设置百度SecretKey
        /// </summary>
        /// <param name="baiduSecretKey"></param>
        public static void SaveBaiduSecretKey(string baiduSecretKey)
        {
            BaiduSecretKey = baiduSecretKey;
            AppSettingsUtil.Instance.SetValue("baiduSecretKey", baiduSecretKey);
        }

        /// <summary>
        /// 设置IsDebug
        /// </summary>
        /// <param name="isDebug"></param>
        public static void SaveIsDebug(bool isDebug)
        {
            IsDebug = isDebug;
            AppSettingsUtil.Instance.SetValue("debug", isDebug);
            if (DebugModeChanged != null)
            {
                DebugModeChanged(new object(), EventArgs.Empty);
            }
        }
    }
}
