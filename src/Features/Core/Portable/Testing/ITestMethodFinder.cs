// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Features.Testing;

internal interface ITestMethodFinder : ILanguageService
{
    /// <summary>
    /// Finds potential test methods in the range.  This is not intended to be 100% accurate, but good enough without exploding complexity.
    /// Semantic discovery recognizes derived test attribute types when enabled.
    /// </summary>
    Task<ImmutableArray<SyntaxNode>> GetPotentialTestMethodsAsync(Document document, TextSpan textSpan, bool useSemanticDiscovery, CancellationToken cancellationToken);

    /// <summary>
    /// Finds potential test methods in <paramref name="nodes"/>.
    /// </summary>
    Task<ImmutableArray<SyntaxNode>> GetTestMethodsAsync(
        Document document, ImmutableArray<SyntaxNode> nodes, bool useSemanticDiscovery, CancellationToken cancellationToken);

    /// <summary>
    /// Determines if a node is a likely match for the fully qualified test name.
    /// </summary>
    bool IsMatch(SemanticModel model, SyntaxNode node, string fullyQualifiedTestName, CancellationToken cancellationToken);
}
