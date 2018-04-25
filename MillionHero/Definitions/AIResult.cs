using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MillionHero.Definitions
{
    public class AIResult:EventArgs
    {
        public string Title;
        public List<AIResultOption> Options;
        /// <summary>
        /// 正确选项，从0开始
        /// </summary>
        public int Correct;
        /// <summary>
        /// 题目序号，从1开始
        /// </summary>
        public int Round;
        /// <summary>
        /// 是否确认答案
        /// </summary>
        public bool Confirmed;

        public AIResult()
        {
            Confirmed = true;
        }
    }

    public class AIResultOption
    {
        public string title;
        public double confidence;
        public double score;
    }
}
