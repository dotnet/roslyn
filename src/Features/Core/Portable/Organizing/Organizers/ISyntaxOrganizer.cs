// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        SyntaxNode OrganizeNode(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken));
    }
}
