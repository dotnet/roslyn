using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis.AddParameter
{
    internal class RegisterFixData<TArgumentSyntax> where
        TArgumentSyntax : SyntaxNode
    {
        public RegisterFixData(SeparatedSyntaxList<TArgumentSyntax> arguments, ImmutableArray<IMethodSymbol> methodCandidates)
        {
            Arguments = arguments;
            MethodCandidates = methodCandidates;
        }

        public SeparatedSyntaxList<TArgumentSyntax> Arguments { get; }
        public ImmutableArray<IMethodSymbol> MethodCandidates { get; }
    }
}
