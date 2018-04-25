using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using TDLib.Extensions;
using MillionHero.Definitions;

namespace MillionHero.Services.QuestionAnalyzers
{
    /// <summary>
    /// 通过搜索问题，并返回前两个搜索到的结果
    /// </summary>
    class TopResultTipQuestionAnalyzer : AbstractQuestionAnalyzer
    {
        public override Definitions.Answer Analyze(Subject subject, int timeout)
        {
            Answer answer = new Answer();

            var keyword = subject.Question;
            var req = CreateGetRequest("https://www.baidu.com/s?wd=" + keyword, timeout);
            var result = req.GetResponseAsHttp().GetBodyAsText();
            HtmlAgilityPack.HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(result);
            // <div class="c-abstract">
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='c-abstract']");
            foreach (var node in nodes)
            {
                if (answer.Tips.Count == 2)
                    break;

                string nodeText = node.InnerText;
                nodeText = nodeText.Replace("<em>", "")
                    .Replace("</em>", "")
                    .Replace("&nbsp;", "")
                    .Replace("&quot;", "")
                    .Replace("&gt;", "");

                answer.Tips.Add(nodeText);
            }

            return answer;
        }
    }
}
