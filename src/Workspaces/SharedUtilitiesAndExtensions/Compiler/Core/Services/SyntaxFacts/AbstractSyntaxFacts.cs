// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractSyntaxFacts
    {
        public abstract ISyntaxKinds SyntaxKinds { get; }

        protected AbstractSyntaxFacts()
        {
        }

        public bool IsSingleLineCommentTrivia(SyntaxTrivia trivia)
            => SyntaxKinds.SingleLineCommentTrivia == trivia.RawKind;

        public bool IsMultiLineCommentTrivia(SyntaxTrivia trivia)
            => SyntaxKinds.MultiLineCommentTrivia == trivia.RawKind;

        public bool IsSingleLineDocCommentTrivia(SyntaxTrivia trivia)
            => SyntaxKinds.SingleLineDocCommentTrivia == trivia.RawKind;

        public bool IsMultiLineDocCommentTrivia(SyntaxTrivia trivia)
            => SyntaxKinds.MultiLineDocCommentTrivia == trivia.RawKind;

        public bool IsShebangDirectiveTrivia(SyntaxTrivia trivia)
            => SyntaxKinds.ShebangDirectiveTrivia == trivia.RawKind;

        public abstract bool IsPreprocessorDirective(SyntaxTrivia trivia);

        public abstract bool ContainsInterleavedDirective(TextSpan span, SyntaxToken token, CancellationToken cancellationToken);

        public abstract SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode node);

        public abstract bool IsParameterNameXmlElementSyntax(SyntaxNode node);

        public abstract SyntaxList<SyntaxNode> GetContentFromDocumentationCommentTriviaSyntax(SyntaxTrivia trivia);

        public bool HasIncompleteParentMember([NotNullWhen(true)] SyntaxNode? node)
            => node?.Parent?.RawKind == SyntaxKinds.IncompleteMember;
    }
}
