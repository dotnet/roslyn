// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.SimplifyTypeNames
{
    internal partial class SimplifyTypeNamesCodeFixProvider : CodeFixProvider
    {
        private class SimplifyTypeNamesFixAllProvider : BatchSimplificationFixAllProvider
        {
            internal static new readonly SimplifyTypeNamesFixAllProvider Instance = new SimplifyTypeNamesFixAllProvider();

            protected override SyntaxNode GetNodeToSimplify(SyntaxNode root, SemanticModel model, Diagnostic diagnostic, Workspace workspace, out string codeActionId, CancellationToken cancellationToken)
            {
                codeActionId = null;
                string diagnosticId;
                var node = SimplifyTypeNamesCodeFixProvider.GetNodeToSimplify(root, model, diagnostic.Location.SourceSpan, workspace.Options, out diagnosticId, cancellationToken);
                if (node != null)
                {
                    codeActionId = GetCodeActionId(diagnosticId, node.ToString());
                }

                return node;
            }
        }
    }
}
