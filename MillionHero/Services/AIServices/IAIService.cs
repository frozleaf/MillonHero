using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MillionHero.Definitions;

namespace MillionHero.Services.AIServices
{
    interface IAIService
    {
        void Init();
        AIResult Fetch();
    }
}
