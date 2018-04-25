using System;
using System.Collections.Generic;
using System.Text;
using MillionHero.Definitions;

namespace MillionHero.Services.QuestionAnalyzers
{
    public interface IQuestionAnalyzer
    {
        Answer Analyze(Subject subject);
        Answer Analyze(Subject subject, int timeout);
    }
}
