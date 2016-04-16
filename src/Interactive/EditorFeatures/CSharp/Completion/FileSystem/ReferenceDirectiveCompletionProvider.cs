// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Completion.FileSystem;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.FileSystem
{
    // TODO(cyrusn): Use a predefined name here.
    [ExportCompletionProvider("ReferenceDirectiveCompletionProvider", LanguageNames.CSharp)]
    // Using TextViewRole here is a temporary work-around to prevent this component from being loaded in
    // regular C# contexts.  We will need to remove this and implement a new "CSharp Script" Content type
    // in order to fix #r completion in .csx files (https://github.com/dotnet/roslyn/issues/5325).
    [TextViewRole(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole)]
    internal partial class ReferenceDirectiveCompletionProvider : AbstractReferenceDirectiveCompletionProvider
    {
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
