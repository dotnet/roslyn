// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.SemanticModelReuse
{
    /// <summary>
    /// Interface only for use by <see cref="ISemanticModelReuseWorkspaceService"/>.  Includes language specific
    /// implementations on how to get an appropriate speculated semantic model given an older semantic model and a
    /// changed method body.
    /// </summary>
    internal interface ISemanticModelReuseLanguageService : ILanguageService
    {
        /// <summary>
        /// Given a node, returns the parent method-body-esque node that we can get a new speculative semantic model
        /// for.  Returns <see langword="null"/> if not in such a location.
        /// </summary>
        SyntaxNode? TryGetContainingMethodBodyForSpeculation(SyntaxNode node);

        /// <summary>
        /// Given a previous semantic model, and a method-eque node in the current tree for that same document, attempts
        /// to create a new speculative semantic model using the top level symbols of <paramref
        /// name="previousSemanticModel"/> but the new body level symbols produced for <paramref
        /// name="currentBodyNode"/>.
        /// <para>
        /// Note: it is critical that no top level changes have occurred between the syntax tree that <paramref
        /// name="previousSemanticModel"/> points at and the syntax tree that <paramref name="currentBodyNode"/> points
        /// at.  In other words, they must be <see cref="SyntaxTree.IsEquivalentTo"/><c>(..., topLevel: true)</c>.  This
        /// function is undefined if they are not.
        /// </para>
        /// </summary>
        Task<SemanticModel?> TryGetSpeculativeSemanticModelAsync(SemanticModel previousSemanticModel, SyntaxNode currentBodyNode, CancellationToken cancellationToken);
    }
}
