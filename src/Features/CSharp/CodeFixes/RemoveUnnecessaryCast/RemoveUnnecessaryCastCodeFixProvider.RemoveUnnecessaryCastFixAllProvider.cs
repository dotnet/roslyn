// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnnecessaryCast
{
    internal partial class RemoveUnnecessaryCastCodeFixProvider : CodeFixProvider
    {
        private class RemoveUnnecessaryCastFixAllProvider : BatchSimplificationFixAllProvider
        {
            internal static new readonly RemoveUnnecessaryCastFixAllProvider Instance = new RemoveUnnecessaryCastFixAllProvider();

            protected override SyntaxNode GetNodeToSimplify(SyntaxNode root, SemanticModel model, Diagnostic diagnostic, Workspace workspace, out string codeActionId, CancellationToken cancellationToken)
            {
                codeActionId = null;
                return GetCastNode(root, model, diagnostic.Location.SourceSpan, cancellationToken);
            }
        }
    }
}
