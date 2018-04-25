using System;
using System.Collections.Generic;
using System.Text;

namespace MillionHero.Definitions
{
    public class Answer
    {
        public List<string> Tips;
        public List<double> OptionRate;
        /// <summary>
        /// 最佳选项
        /// </summary>
        public int BestOption;
        /// <summary>
        /// 题目
        /// </summary>
        public Subject Subject;

        public Answer()
        {
            this.Tips = new List<string>();
            this.OptionRate = new List<double>();
        }
    }
}
