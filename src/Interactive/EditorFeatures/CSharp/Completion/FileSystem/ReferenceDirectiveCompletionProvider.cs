// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Completion.FileSystem;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.FileSystem
{
    // TODO(cyrusn): Use a predefined name here.
    [ExportCompletionProvider("ReferenceDirectiveCompletionProvider", LanguageNames.CSharp)]
    internal partial class ReferenceDirectiveCompletionProvider : AbstractReferenceDirectiveCompletionProvider
    {
        public ReferenceDirectiveCompletionProvider()
        {
        }

        protected override bool TryGetStringLiteralToken(SyntaxTree tree, int position, out SyntaxToken stringLiteral, CancellationToken cancellationToken)
        {
            if (tree.IsEntirelyWithinStringLiteral(position, cancellationToken))
            {
                var token = tree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);
                if (token.Kind() == SyntaxKind.EndOfDirectiveToken || token.Kind() == SyntaxKind.EndOfFileToken)
                {
                    token = token.GetPreviousToken(includeSkipped: true, includeDirectives: true);
                }

                if (token.Kind() == SyntaxKind.StringLiteralToken &&
                    token.Parent.Kind() == SyntaxKind.ReferenceDirectiveTrivia)
                {
                    stringLiteral = token;
                    return true;
                }
            }

            stringLiteral = default(SyntaxToken);
            return false;
        }
    }
}
