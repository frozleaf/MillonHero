using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDLib.Extensions;
using System.Net;
using System.Text.RegularExpressions;

namespace MillionHero.Services.QuestionAnalyzers
{
    public abstract class AbstractQuestionAnalyzer : IQuestionAnalyzer
    {
        private static readonly string[] NegativeWords = new string[] { 
            "不能",
            "不是",
            "不可以",
        };
        private static readonly string[] UserAgents = new string[] {  
            "Mozilla/5.0 (X11; Fedora; Linux x86_64; rv:57.0) Gecko/20100101 Firefox/57.0",
            "Mozilla/5.0 (X11; Fedora; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.108 Safari/537.36"
        };

        public Definitions.Answer Analyze(Definitions.Subject subject)
        {
            return this.Analyze(subject, 2000);
        }

        public abstract Definitions.Answer Analyze(Definitions.Subject subject, int timeout);

        protected HttpWebRequest CreateGetRequest(string url, int timeout)
        {
            var req = HttpExtension.CreateGetRequest(url);
            req.Timeout = timeout;
            req.UserAgent = GetUserAgent();
            return req;
        }

        protected string GetUserAgent()
        {
            Random r = new Random();
            int index = r.Next(0,UserAgents.Length);
            return UserAgents[index];
        }

        /// <summary>
        /// 是否包含否定词
        /// </summary>
        /// <param name="question"></param>
        /// <returns></returns>
        protected bool ContainsNegativeWords(string question)
        {
            for (int i = 0; i < NegativeWords.Length; i++)
            {
                if (question.Contains(NegativeWords[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取字串的个数
        /// </summary>
        /// <param name="str"></param>
        /// <param name="subStr"></param>
        /// <returns></returns>
        protected int GetSubstringCount(string str, string subStr)
        {
            return Regex.Matches(str, subStr).Count;
        }
    }
}
