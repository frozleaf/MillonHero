using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace MillionHero.Services.Ocrs
{
    public class BaiduOcr : AbstractOcr
    {
        private Baidu.Aip.Ocr.Ocr _client;

        public BaiduOcr(string apiKey, string secretKey)
        {
            _client = new Baidu.Aip.Ocr.Ocr(apiKey, secretKey);
            _client.SetConnectionTimeoutInMillis(3000);
        }

        public override string[] Ocr(byte[] imageData)
        {
            List<string> words = new List<string>();

            // 调用通用文字识别, 图片参数为本地图片，可能会抛出网络等异常，请使用try/catch捕获
            var result = _client.GeneralBasic(imageData);
            var options = new Dictionary<string, object>{
	            {"language_type", "CHN_ENG"},
	        };
            result = _client.GeneralBasic(imageData, options);
            if (result.ToString().Contains("error_code"))
                return words.ToArray();

            JArray array = result["words_result"] as JArray;
            for (int i = 0; i < array.Count; i++)
            {
                words.Add(array[i]["words"].ToString());
            }
            return words.ToArray();
        }
    }
}
