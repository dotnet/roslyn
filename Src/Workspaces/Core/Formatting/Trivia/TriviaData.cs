using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// it holds onto trivia information between two tokens
    /// </summary>
    internal abstract class TriviaData
    {
        protected const int TokenPairIndexNotNeeded = int.MinValue;

        private readonly FormattingOptions options;

        protected TriviaData(FormattingOptions options)
        {
            Contract.ThrowIfNull(options);
            this.options = options;
        }

        protected FormattingOptions Options { get { return this.options; } }

        public int LineBreaks { get; protected set; }
        public int Space { get; protected set; }

        public bool SecondTokenIsFirstTokenOnLine { get { return this.LineBreaks > 0; } }

        public abstract bool TreatAsElastic { get; }
        public abstract bool IsWhitespaceOnlyTrivia { get; }
        public abstract bool ShouldReplaceOriginalWithNewString { get; }
        public abstract string NewString { get; }

        public abstract TriviaData WithSpace(int space);
        public abstract TriviaData WithLine(int line, int indentation);
        public abstract TriviaData WithIndentation(int indentation);

        public abstract void Format(FormattingContext context, Action<int, TriviaData> formattingResultApplier, int tokenPairIndex = TokenPairIndexNotNeeded);
    }
}
