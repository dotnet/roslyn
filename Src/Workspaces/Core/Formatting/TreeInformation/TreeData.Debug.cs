using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    internal abstract partial class TreeData
    {
        private class Debug : Tree
        {
            private readonly TreeData debugNodeInfo;

            public Debug(CommonSyntaxTree syntaxTree) :
                base(syntaxTree)
            {
                this.debugNodeInfo = TreeData.Create(this.Root);
            }

            public override string GetTextBetween(CommonSyntaxToken token1, CommonSyntaxToken token2)
            {
                var text = base.GetTextBetween(token1, token2);
                Contract.ThrowIfFalse(text == this.debugNodeInfo.GetTextBetween(token1, token2));

                return text;
            }

            public override ValueTuple<TextSpan, TriviaData> GetFirstTriviaDataAndSpan(CommonSyntaxToken token, TriviaData info)
            {
                var triviaInfo = base.GetFirstTriviaDataAndSpan(token, info);
                var debugTriviaInfo = this.debugNodeInfo.GetFirstTriviaDataAndSpan(token, info);

                Contract.ThrowIfFalse(triviaInfo.Item1.Equals(debugTriviaInfo.Item1));
                Contract.ThrowIfFalse(triviaInfo.Item2.LineBreaks == debugTriviaInfo.Item2.LineBreaks &&
                                      triviaInfo.Item2.ShouldReplaceOriginalWithNewString == debugTriviaInfo.Item2.ShouldReplaceOriginalWithNewString &&
                                      triviaInfo.Item2.IsWhitespaceOnlyTrivia == debugTriviaInfo.Item2.IsWhitespaceOnlyTrivia &&
                                      triviaInfo.Item2.SecondTokenIsFirstTokenOnLine == debugTriviaInfo.Item2.SecondTokenIsFirstTokenOnLine &&
                                      triviaInfo.Item2.Space == debugTriviaInfo.Item2.Space);

                if (triviaInfo.Item2.ShouldReplaceOriginalWithNewString)
                {
                    Contract.ThrowIfFalse(triviaInfo.Item2.NewString == debugTriviaInfo.Item2.NewString);
                }

                return triviaInfo;
            }

            public override ValueTuple<TextSpan, TriviaData> GetLastTriviaDataAndSpan(CommonSyntaxToken token, TriviaData info)
            {
                // regular tree case where things are parsed from a file
                var triviaInfo = base.GetLastTriviaDataAndSpan(token, info);
                var debugTriviaInfo = this.debugNodeInfo.GetLastTriviaDataAndSpan(token, info);

                Contract.ThrowIfFalse(triviaInfo.Item1.Equals(debugTriviaInfo.Item1));
                Contract.ThrowIfFalse(triviaInfo.Item2.LineBreaks == debugTriviaInfo.Item2.LineBreaks &&
                                      triviaInfo.Item2.ShouldReplaceOriginalWithNewString == debugTriviaInfo.Item2.ShouldReplaceOriginalWithNewString &&
                                      triviaInfo.Item2.IsWhitespaceOnlyTrivia == debugTriviaInfo.Item2.IsWhitespaceOnlyTrivia &&
                                      triviaInfo.Item2.SecondTokenIsFirstTokenOnLine == debugTriviaInfo.Item2.SecondTokenIsFirstTokenOnLine &&
                                      triviaInfo.Item2.Space == debugTriviaInfo.Item2.Space);

                if (triviaInfo.Item2.ShouldReplaceOriginalWithNewString)
                {
                    Contract.ThrowIfFalse(triviaInfo.Item2.NewString == debugTriviaInfo.Item2.NewString);
                }

                return triviaInfo;
            }
        }
    }
}
