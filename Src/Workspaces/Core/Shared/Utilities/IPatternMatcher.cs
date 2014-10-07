using System.Collections.Generic;

namespace Roslyn.Services.Shared.Utilities
{
    internal interface IPatternMatcher
    {
        MatchResult MatchSingleWordPattern(string candidate, string pattern);
        IEnumerable<MatchResult> MatchMultiWordPattern(string candidate, string pattern);
        int CompareMatchResults(MatchResult result1, MatchResult result2);
    }
}
