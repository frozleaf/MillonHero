using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TDLib.Extensions;
using TDLib.Utils;
using MillionHero.Definitions;

namespace MillionHero.Services.AIServices
{
    class UCAIService : IAIService
    {
        private static ILog _logger = LoggerManager.GetLogger<UCAIService>();
        private string _lastLogResult = "";

        public void Init()
        {
        }

        public AIResult Fetch()
        {
            // 请求地址：
            // http://answer.sm.cn/answer/curr?format=json&activity=million&_t=1515993272162&activity=million

            Dictionary<string,object> paras = new Dictionary<string, object>();
            paras.Add("format", "json");
            paras.Add("activity", "million");
            paras.Add("_t", DateTime.Now.ToUnixMillisecond());
            var req = HttpExtension.CreateGetRequest("http://answer.sm.cn/answer/curr", paras, false);
            var result = req.GetResponseAsHttp().GetBodyAsText();

            if (result != _lastLogResult)
            {
                _lastLogResult = result;
                _logger.Info("UCAI Result:" + result);
            }

            JObject obj = JsonConvert.DeserializeObject(result) as JObject;
            var ucResult = JsonConvert.DeserializeObject<UCAIResult>(obj["data"].ToString());
            if (string.IsNullOrEmpty(ucResult.round))
            {
                ucResult.round = "-1";
            }
            if (string.IsNullOrEmpty(ucResult.correct))
            {
                ucResult.correct = "-1";
            }

            AIResult aiResult = new AIResult();
            aiResult.Title = ucResult.title;
            aiResult.Round = Convert.ToInt32(ucResult.round);
            aiResult.Correct = Convert.ToInt32(ucResult.correct);
            aiResult.Options = new List<AIResultOption>();
            for (int i = 0; i < ucResult.options.Count; i++)
            {
                var opt = ucResult.options[i];
                aiResult.Options.Add(new AIResultOption() { 
                    title = opt.title,
                    score = opt.score,
                    confidence = opt.confidence
                });
            }

            return aiResult;
        }
    }

    class UCAIResult
    {
        /// <summary>
        /// 问题
        /// </summary>
        public string title;
        public long time;
        public int status;
        /// <summary>
        /// 第几轮
        /// </summary>
        public string round;
        /// <summary>
        /// 正确答案，从0开始
        /// </summary>
        public string correct;
        public string sid;
        /// <summary>
        /// 选项
        /// </summary>
        public List<UCAIResultOption> options; 
    }

    class UCAIResultOption
    {
        public double confidence;
        public double score;
        public string title;
    }
}
