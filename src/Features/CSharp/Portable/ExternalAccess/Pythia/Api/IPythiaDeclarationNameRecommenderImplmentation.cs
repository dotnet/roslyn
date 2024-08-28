// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.ExternalAccess.Pythia.Api;

internal interface IPythiaDeclarationNameRecommenderImplementation
{
    /// <summary>
    /// Order of returned recommendation decides the order of those items in completion list
    /// </summary>
    public Task<ImmutableArray<string>> ProvideRecommendationsAsync(PythiaDeclarationNameContext context, CancellationToken cancellationToken);
}

internal readonly struct PythiaDeclarationNameContext(CSharpSyntaxContext context)
{
    private readonly CSharpSyntaxContext _context = context;

    public Document Document => _context.Document;

    public int Position => _context.Position;

    public SemanticModel SemanticModel => _context.SemanticModel;

    /// <summary>
    /// The token to the left of <see cref="Position"/>. This token may be touching the position.
    /// </summary>
    public SyntaxToken LeftToken => _context.LeftToken;
}
