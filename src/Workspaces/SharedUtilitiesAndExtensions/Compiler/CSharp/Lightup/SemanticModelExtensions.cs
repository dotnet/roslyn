// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if CODE_STYLE
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
#endif

namespace Microsoft.CodeAnalysis.CSharp;

internal static class SemanticModelExtensions2
{
#if CODE_STYLE
    public static IMethodSymbol? GetDeclaredSymbol(this SemanticModel? semanticModel, LocalFunctionStatementSyntax node, CancellationToken cancellationToken = default(CancellationToken))
    {
        return (IMethodSymbol?)(semanticModel?.GetDeclaredSymbol((SyntaxNode)node, cancellationToken));
    }
#endif
}
