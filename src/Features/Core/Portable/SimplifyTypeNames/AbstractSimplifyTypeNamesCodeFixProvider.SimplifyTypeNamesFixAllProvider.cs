// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SimplifyTypeNames
{
    internal abstract partial class AbstractSimplifyTypeNamesCodeFixProvider
    {
        private class SimplifyTypeNamesFixAllProvider : BatchSimplificationFixAllProvider
        {
            private readonly AbstractSimplifyTypeNamesCodeFixProvider _provider;

            public SimplifyTypeNamesFixAllProvider(AbstractSimplifyTypeNamesCodeFixProvider provider)
            {
                _provider = provider;
            }

            protected override SyntaxNode GetNodeToSimplify(
                SyntaxNode root, SemanticModel model, Diagnostic diagnostic, 
                DocumentOptionSet options, CancellationToken cancellationToken)
            {
                return _provider.GetNodeToSimplify(
                    root, model, diagnostic.Location.SourceSpan, options, out _, cancellationToken);
            }
        }
    }
}
