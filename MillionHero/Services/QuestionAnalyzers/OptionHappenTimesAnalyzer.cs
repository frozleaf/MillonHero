using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using TDLib.Extensions;
using MillionHero.Definitions;
using MillionHero.Exceptions;

namespace MillionHero.Services.QuestionAnalyzers
{
    /// <summary>
    /// 通过搜索问题，并统计每个选项的个数来进行分析
    /// </summary>
    class OptionHappenTimesAnalyzer : AbstractQuestionAnalyzer
    {
        public override Definitions.Answer Analyze(Subject subject, int timeout)
        {
            Answer answer = new Answer();
            answer.Subject = subject;

            // 搜索问题
            var question = subject.GetFixedQuestion();
            var req = CreateGetRequest("https://www.baidu.com/s?wd=" + question, timeout);
            var result = req.GetResponseAsHttp().GetBodyAsText();
            /* 
             * 截取 <div id="content_left">  和   相关搜索  中间的内容
             * 
             * 用于解决选项中为纯数字时，受【搜索热点】和【页数导航栏】的干扰
            */
            int contentStartIndex = result.IndexOf("<div id=\"content_left\">");
            int contentEndIndex = result.IndexOf("相关搜索", contentStartIndex);
            result = result.Substring(contentStartIndex, contentEndIndex - contentStartIndex);

            try
            {
                // 尝试获取搜索结果中的第一条数据
                int firstStartIndex = result.IndexOf("<div class=\"c-abstract\">");
                firstStartIndex = result.IndexOf("日", firstStartIndex) + 1;
                int firstEndIndex = result.IndexOf("...", firstStartIndex);
                string tip = result.Substring(firstStartIndex, firstEndIndex - firstStartIndex);
                firstEndIndex = tip.IndexOf("<br>");
                if (firstEndIndex != -1)
                {
                    tip = tip.Substring(0, firstEndIndex);
                }
                tip = tip.Replace("</em>", "")
                    .Replace("<em>", "")
                    .Replace("&nbsp;", "")
                    .Replace("</span>", "")
                    .Replace("<div class=\"c-abstract\">", "")
                    .Replace("</p>", "")
                    .Replace("<span class=\"m\">", "");
                answer.Tips.Add(tip);
            }
            catch (Exception ex)
            {

            }

            double total = 0d;
            var opts = subject.GetFixedOptions();
            for (int i = 0; i < opts.Count; i++)
            {
                // 选项
                var opt = opts[i];
                // 计算选项在结果中出现的次数
                var count = GetSubstringCount(result, opt);
                answer.OptionRate.Add(count);
                total += count;
            }
            double maxPercent = -1;
            double minPercent = 999;
            int maxPercentIndex = -1;
            int minPercentIndex = -1;
            for (int i = 0; i < answer.OptionRate.Count; i++)
            {
                var percent = answer.OptionRate[i] / total * 100;
                if (percent > maxPercent)
                {
                    maxPercent = percent;
                    maxPercentIndex = i;
                }
                if (percent < minPercent)
                {
                    minPercent = percent;
                    minPercentIndex = i;
                }
                answer.OptionRate[i] = (int)percent;
            }

            // 如果问题中包含否定词，则反转最佳选项
            answer.BestOption = maxPercentIndex;
            if (ContainsNegativeWords(question))
            {
                answer.BestOption = minPercentIndex;
            }

            if (total == 0f)
            {
                // 未找到任何结果
                answer.BestOption = -1;
                for (int i = 0; i < answer.OptionRate.Count; i++)
			    {
                    answer.OptionRate[i] = 0d;
			    }
            }

            return answer;
        }
    }
}
