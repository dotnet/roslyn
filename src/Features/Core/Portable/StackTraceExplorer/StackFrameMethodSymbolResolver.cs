// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;

namespace Microsoft.CodeAnalysis.StackTraceExplorer;

internal sealed class StackFrameMethodSymbolResolver : AbstractStackTraceSymbolResolver
{
    public override Task<IMethodSymbol?> TryGetBestMatchAsync(Project project,
        INamedTypeSymbol type,
        StackFrameSimpleNameNode methodNode,
        StackFrameParameterList methodArguments,
        StackFrameTypeArgumentList? methodTypeArguments,
        CancellationToken cancellationToken)
    {
        var methodName = methodNode.ToString();

        var candidateMethods = type
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.Name == methodName)
            .ToImmutableArray();

        var match = TryGetBestMatch(candidateMethods, methodTypeArguments, methodArguments);
        return Task.FromResult(match);
    }
}
