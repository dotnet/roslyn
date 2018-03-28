// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
