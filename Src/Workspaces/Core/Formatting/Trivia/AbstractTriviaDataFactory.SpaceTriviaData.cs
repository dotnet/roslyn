using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    internal abstract partial class AbstractTriviaDataFactory
    {
        /// <summary>
        /// represents a simple space case between two tokens
        /// </summary>
        protected class SpaceTriviaData : WhitespaceTriviaData
        {
            public SpaceTriviaData(FormattingOptions options, int space, bool elastic) :
                base(options, lineBreaks: 0, indentation: space, elastic: elastic)
            {
                Contract.ThrowIfFalse(space >= 0);
            }
        }
    }
}
