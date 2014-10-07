using System.Collections.Generic;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.Formatting;
using Roslyn.Utilities;

namespace Roslyn.Services.CSharp.Formatting
{
    internal abstract class CSharpTriviaData : TriviaData
    {
        public CSharpTriviaData(FormattingOptions options) :
            base(options)
        {
        }

        public virtual List<SyntaxTrivia> TriviaList
        {
            get
            {
                return Contract.FailWithReturn<List<SyntaxTrivia>>("Should be never called");
            }
        }
    }
}
