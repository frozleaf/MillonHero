using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MillionHero.Definitions
{
    /// <summary>
    /// IE版本
    /// </summary>
    enum IEVersion
    {
        /*
         *  10000(0×2710) IE10
            9999 (0x270F) IE9 忽略html5
            9000 (0×2328) IE9
            8888 (0x22B8) IE8 忽略html5
            8000 (0x1F40) IE8
            7000 (0x1B58) IE7
         */
        IE7 = 0x1B58,
        IE8 = 0x1F40,
        IE8_NoH5 = 0x22B8,
        IE9 = 0x2328,
        IE9_NoH5 = 0x270F,
        IE10 = 0x2710,
    }
}
