// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;

namespace Microsoft.CodeAnalysis.ImplementInterface;

internal readonly record struct ImplementInterfaceConfiguration
{
    public bool ImplementDisposePattern { get; init; }
    public bool Explicitly { get; init; }
    public bool Abstractly { get; init; }
    public bool OnlyRemaining { get; init; }
    public ISymbol? ThroughMember { get; init; }
}

internal interface IImplementInterfaceService : ILanguageService
{
    Task<Document> ImplementInterfaceAsync(Document document, ImplementTypeOptions options, SyntaxNode node, CancellationToken cancellationToken);

    /// <summary>
    /// Produces the symbol that implements that provided <paramref name="interfaceMember"/> within the corresponding
    /// <see cref="ImplementInterfaceInfo.ClassOrStructType"/>, based on the provided <paramref name="options"/> and
    /// <paramref name="configuration"/>.
    /// </summary>
    ImmutableArray<ISymbol> ImplementInterfaceMember(
        Document document,
        ImplementInterfaceInfo info,
        ImplementTypeOptions options,
        ImplementInterfaceConfiguration configuration,
        Compilation compilation,
        ISymbol interfaceMember);

    Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
        Document document, SyntaxNode? interfaceType, CancellationToken cancellationToken);

    ImmutableArray<SyntaxNode> GetInterfaceTypes(SyntaxNode typeDeclaration);
}
