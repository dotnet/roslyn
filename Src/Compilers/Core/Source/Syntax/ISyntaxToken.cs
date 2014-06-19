using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
#if false
    internal interface ISyntaxToken : IBaseSyntaxNodeExt
    {
        int ContextualKind { get; }
        string Text { get; }
        object Value { get; }
        string ValueText { get; }
        IBaseSyntaxNodeExt GetLeadingTrivia();
        IBaseSyntaxNodeExt GetTrailingTrivia();
        AbstractSyntaxNavigator Navigator { get; }

        ISyntaxToken WithLeadingTrivia(IEnumerable<SyntaxTrivia> trivia);
        ISyntaxToken WithTrailingTrivia(IEnumerable<SyntaxTrivia> trivia);
    }
#endif
}