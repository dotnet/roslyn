// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("GenericNamePartiallyWrittenSignatureHelpProvider", LanguageNames.CSharp), Shared]
    internal class GenericNamePartiallyWrittenSignatureHelpProvider : GenericNameSignatureHelpProvider
    {
        [ImportingConstructor]
        public GenericNamePartiallyWrittenSignatureHelpProvider()
        {
        }

        protected override bool TryGetGenericIdentifier(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out SyntaxToken genericIdentifier, out SyntaxToken lessThanToken)
        {
            return root.SyntaxTree.IsInPartiallyWrittenGeneric(position, cancellationToken, out genericIdentifier, out lessThanToken);
        }

        protected override TextSpan GetTextSpan(SyntaxToken genericIdentifier, SyntaxToken lessThanToken)
        {
            var lastToken = genericIdentifier.FindLastTokenOfPartialGenericName();
            var nextToken = lastToken.GetNextNonZeroWidthTokenOrEndOfFile();
            Contract.ThrowIfTrue(nextToken.Kind() == 0);
            return TextSpan.FromBounds(genericIdentifier.SpanStart, nextToken.SpanStart);
        }
    }
}
