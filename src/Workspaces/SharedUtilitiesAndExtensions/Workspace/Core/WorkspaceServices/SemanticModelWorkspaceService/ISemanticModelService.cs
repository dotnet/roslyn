﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SemanticModelWorkspaceService
{
    /// <summary>
    /// a service that provides a semantic model that will re-use last known compilation if
    /// semantic version hasn't changed.
    /// </summary>
    internal interface ISemanticModelService : IWorkspaceService
    {
        /// <summary>
        /// Don't call this directly. use Document extension method GetSemanticModelForNodeAsync or GetSemanticModelForSpanAsync instead.
        /// 
        /// see the descriptions on the extension methods
        /// </summary>
        Task<SemanticModel> GetSemanticModelForNodeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken);
    }
}
