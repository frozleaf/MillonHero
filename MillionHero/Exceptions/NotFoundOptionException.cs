using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MillionHero.Exceptions
{
    /// <summary>
    /// 搜索题目，但从答案中未找到任何选项异常
    /// </summary>
    class NotFoundOptionException:Exception
    {
        public NotFoundOptionException(string msg):base(msg)
        {

        }

        public NotFoundOptionException(string msg, Exception inner):base(msg,inner)
        {

        }
    }
}
