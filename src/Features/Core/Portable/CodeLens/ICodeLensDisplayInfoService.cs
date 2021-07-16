// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeLens
{
    internal interface ICodeLensDisplayInfoService : ILanguageService
    {
        /// <summary>
        /// Gets the node used for display info
        /// </summary>
        SyntaxNode GetDisplayNode(SyntaxNode node);

        /// <summary>
        /// Gets the DisplayName for the given node
        /// </summary>
        string GetDisplayName(SemanticModel semanticModel, SyntaxNode node);
    }
}
