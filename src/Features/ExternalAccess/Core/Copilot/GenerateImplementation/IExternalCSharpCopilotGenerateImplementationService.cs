// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.GenerateImplementation;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

internal interface IExternalCSharpCopilotGenerateImplementationService
{
    Task<ImmutableDictionary<SyntaxNode, ImplementationDetailsWrapper>> ImplementNotImplementedExceptionsAsync(
        Document document,
        ImmutableDictionary<SyntaxNode, ImmutableArray<ReferencedSymbol>> methodOrProperties,
        CancellationToken cancellationToken);
}
