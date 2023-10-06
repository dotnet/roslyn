// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

internal sealed partial class CSharpCodeMapper
{
    private interface IMappingHelper
    {
        bool TryGetValidInsertions(
           SyntaxNode target,
           ImmutableArray<CSharpSourceNode> sourceNodes,
           out CSharpSourceNode[] validInsertions,
           out InvalidInsertion[] invalidInsertions);

        TextSpan? GetInsertSpan(SyntaxNode documentSyntax, CSharpSourceNode insertion, MappingTarget target, out SyntaxNode? adjustedNodeToMap);
    }

    /// <summary>
    /// Represents an invalid insertion operation.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="InvalidInsertion"/> class.
    /// </remarks>
    /// <param name="InsertNode">The source node to insert.</param>
    /// <param name="Reason">The reason why the insertion operation is invalid.</param>
    private record InvalidInsertion(CSharpSourceNode InsertNode, InvalidInsertionReason Reason)
    { }

    private enum InvalidInsertionReason
    {
        /// <summary>
        /// The reason for the failure is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The identifier being inserted already exists in the target context.
        /// </summary>
        InsertIdentifierAlreadyExistsOnTarget,

        /// <summary>
        /// The identifier being replaced does not exist in the target context.
        /// </summary>
        ReplaceIdentifierMissingOnTarget,
    }
}
