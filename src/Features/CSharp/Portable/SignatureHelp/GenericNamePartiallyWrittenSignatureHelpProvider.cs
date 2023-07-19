// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("GenericNamePartiallyWrittenSignatureHelpProvider", LanguageNames.CSharp), Shared]
    internal class GenericNamePartiallyWrittenSignatureHelpProvider : GenericNameSignatureHelpProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GenericNamePartiallyWrittenSignatureHelpProvider()
        {
        }

        protected override bool TryGetGenericIdentifier(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out SyntaxToken genericIdentifier, out SyntaxToken lessThanToken)
            => root.SyntaxTree.IsInPartiallyWrittenGeneric(position, cancellationToken, out genericIdentifier, out lessThanToken);

        protected override TextSpan GetTextSpan(SyntaxToken genericIdentifier, SyntaxToken lessThanToken)
        {
            var lastToken = genericIdentifier.FindLastTokenOfPartialGenericName();
            var nextToken = lastToken.GetNextNonZeroWidthTokenOrEndOfFile();
            Contract.ThrowIfTrue(nextToken.Kind() == 0);
            return TextSpan.FromBounds(genericIdentifier.SpanStart, nextToken.SpanStart);
        }
    }
}
