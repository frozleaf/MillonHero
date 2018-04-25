using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using TDLib.Extensions;
using MillionHero.Definitions;
using System.Threading;
using System.Net;

namespace MillionHero.Services.QuestionAnalyzers
{
    /// <summary>
    /// 通过关联性算法，来进行分析
    /// </summary>
    class PMIAnalyzer : AbstractQuestionAnalyzer
    {
        /*
         * 参考相关性算法
         * http://blog.csdn.net/luo123n/article/details/48574123
         * 
         * count(Q)，搜索问题得到的结果数
         * count(Ai)，搜索第i选项得到的结果数
         * count(Q&Ai)，搜索问题和第i选项得到的结果数
         * PMI(i) = count(Q&Ai)/(count(Q)*count(Ai))，PMI值越大，表示相关性越高
         * 
         *  解释：
         *  count(Q),查询“题目”返回的结果数            count(QA),查询“题目+选项A”返回的结果数            count(A)，查询“选项A”返回的结果数            题目和选项A的相关性=count(QA)/(count(Q)*count(A))
         * 
         */

        public override Definitions.Answer Analyze(Subject subject, int timeout)
        {
            Answer answer = new Answer();

            var keyword = subject.Question;
            int optionCount = subject.Options.Count;
            List<Searcher> searchers = new List<Searcher>();
            List<Searcher> qaSearchers = new List<Searcher>();
            List<Searcher> aSearchers = new List<Searcher>();

            // 搜索Q
            Searcher qSearcher = new Searcher(CreateGetRequest, subject.Question, timeout);
            new Thread(qSearcher.Query) { IsBackground = true }.Start();

            for (int i = 0; i < optionCount; i++)
            {
                var opt = subject.Options[i];

                // 搜索QA
                var s1 = new Searcher(CreateGetRequest, subject.Question + " " + opt, timeout);
                qaSearchers.Add(s1);
                new Thread(s1.Query) { IsBackground = true }.Start();

                // 搜索A
                var s2 = new Searcher(CreateGetRequest, opt, timeout);
                aSearchers.Add(s2);
                new Thread(s2.Query) { IsBackground = true }.Start();
            }
            searchers.Add(qSearcher);
            searchers.AddRange(qaSearchers);
            searchers.AddRange(aSearchers);

            while (true)
            {
                Thread.Sleep(50);
                bool isDone = true;
                for (int i = 0; i < searchers.Count; i++)
                {
                    var s = searchers[i];
                    if (!s.IsDone)
                    {
                        isDone = false;
                        break;
                    }
                }
                if (isDone)
                {
                    break;
                }
            }

            // 计算PMI(i)
            double pmiTotal = 0d;
            for (int i = 0; i < optionCount; i++)
            {
                double pmi = qaSearchers[i].Result / (aSearchers[i].Result * qSearcher.Result);
                answer.OptionRate.Add(pmi);
                pmiTotal += pmi;
            }
            for (int i = 0; i < answer.OptionRate.Count; i++)
            {
                answer.OptionRate[i] = answer.OptionRate[i] / pmiTotal * 100;
                answer.OptionRate[i] = (int)answer.OptionRate[i];
            }

            return answer;
        }

        class Searcher
        {
            private string _keyword;
            private int _timeout;
            private Func<string, int, HttpWebRequest> _createGetReqAction;
            public double Result = -1;
            public bool IsDone = false;

            public Searcher(Func<string,int,HttpWebRequest> createGetReqAction, string keyword, int timeout)
            {
                _createGetReqAction = createGetReqAction;
                _keyword = keyword;
                _timeout = timeout;
            }

            public void Query()
            {
                try
                {
                    var req = _createGetReqAction("https://www.baidu.com/s?wd=" + _keyword, _timeout);
                    var result = req.GetResponseAsHttp().GetBodyAsText();
                    // 百度为您找到相关结果约8,540,000个
                    int startIndex = result.IndexOf("百度为您找到相关结果约");
                    startIndex += "百度为您找到相关结果约".Length;
                    int endIndex = result.IndexOf("个", startIndex);
                    string str = result.Substring(startIndex, endIndex - startIndex);
                    str = str.Replace(",", "");

                    this.Result = Convert.ToInt64(str);
                }
                catch (Exception ex)
                {
                    this.Result = 1;
                }
                finally
                {
                    this.IsDone = true;
                }
            }
        }

        
    }
}
