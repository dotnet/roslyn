// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis.ExternalAccess.Unified.Copilot.GenerateImplementation;

namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Copilot;
#else
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.GenerateImplementation;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;
#endif

internal interface IExternalCSharpCopilotGenerateImplementationService
{
    Task<ImmutableDictionary<SyntaxNode, ImplementationDetailsWrapper>> ImplementNotImplementedExceptionsAsync(
        Document document,
        ImmutableDictionary<SyntaxNode, ImmutableArray<ReferencedSymbol>> methodOrProperties,
        CancellationToken cancellationToken);
}
