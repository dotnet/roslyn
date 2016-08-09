// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeLens
{
    internal interface IDisplayInfoLanguageServices : ILanguageService
    {
        /// <summary>
        /// Indicates if the given node is a declaration of some kind of symbol. 
        /// For example a class for a method declaration.
        /// </summary>
        bool IsDeclaration(SyntaxNode node);

        /// <summary>
        /// Indicates if the given node is a namespace import.
        /// </summary>
        bool IsDirectiveOrImport(SyntaxNode node);

        /// <summary>
        /// Indicates if the given node is an assembly level attribute "[assembly: MyAttribute]"
        /// </summary>
        bool IsGlobalAttribute(SyntaxNode node);

        /// <summary>
        /// Indicates if given node is DocumentationCommentTriviaSyntax
        /// </summary>
        bool IsDocumentationComment(SyntaxNode node);

        /// <summary>
        /// Gets the node used for display info
        /// </summary>
        SyntaxNode GetDisplayNode(SyntaxNode node);

        /// <summary>
        /// Gets the DisplayName for the given node for given display format
        /// </summary>
        string GetDisplayName(SemanticModel semanticModel, SyntaxNode node, DisplayFormat displayFormat);
    }
}
