using System;
using System.Collections.Generic;
using System.Text;

namespace MillionHero.Definitions
{
    /// <summary>
    /// 一道题
    /// </summary>
    public class Subject
    {
        /// <summary>
        /// 题目
        /// </summary>
        public string Question;
        /// <summary>
        /// 选项
        /// </summary>
        public List<string> Options;
        /// <summary>
        /// 修正过的题目
        /// </summary>
        private string _fixedQuestion = null;
        /// <summary>
        /// 修正过的选项
        /// </summary>
        private List<string> _fixedOptions = null;

        public Subject()
        {
            Options = new List<string>();
        }

        public string GetFixedQuestion()
        {
            if (_fixedQuestion == null)
            {
                _fixedQuestion = FixQuestionString(Question);
            }

            return _fixedQuestion;
        }

        public List<string> GetFixedOptions()
        {
            if (_fixedOptions == null)
            {
                if(Options != null)
                {
                    _fixedOptions = new List<string>();
                    for (int i = 0; i < Options.Count; i++)
			        {
                        var opt = Options[i];
                        var fixedOpt = FixOptionString(opt);
                        _fixedOptions.Add(fixedOpt);
			        }
                }
            }

            return _fixedOptions;
        }



        /// <summary>
        /// 修复问题字符串
        /// </summary>
        /// <param name="question"></param>
        protected string FixQuestionString(string question)
        {
            if (question == null)
                return question;

            // 去除问题开始的序号，有时候会影响答案的解析
            int index = question.IndexOf(".");
            if (index == 1 || index == 2)
            {
                // .前面有1个或2个数字
                question = question.Substring(index + 1);
            }

            return question;
        }

        /// <summary>
        /// 修复选项字符串
        /// </summary>
        /// <param name="option"></param>
        protected string FixOptionString(string option)
        {
            if (option == null)
                return option;

            // 解决：选项种存在书名号，导致的分析结果出错
            option = option.Replace("《", "")
                           .Replace("》", "")
                           .Replace(">", "")
                           .Replace("<", "");
            // 解决：选项种存在XX种时，种字导致的分析结果出错
            option = option.Replace("种", "");

            return option;
        }
    }
}
