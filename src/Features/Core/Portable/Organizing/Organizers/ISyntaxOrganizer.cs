// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Organizing.Organizers
{
    internal interface ISyntaxOrganizer
    {
        /// <summary>
        /// syntax node types this organizer is applicable to
        /// </summary>
        IEnumerable<Type> SyntaxNodeTypes { get; }

        /// <summary>
        /// organize given node
        /// </summary>
        SyntaxNode OrganizeNode(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken = default);
    }
}
