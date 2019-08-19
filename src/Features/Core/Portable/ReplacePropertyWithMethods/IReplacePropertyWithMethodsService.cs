// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ReplacePropertyWithMethods
{
    internal interface IReplacePropertyWithMethodsService : ILanguageService
    {
        Task<SyntaxNode> GetPropertyDeclarationAsync(CodeRefactoringContext context);

        Task ReplaceReferenceAsync(
            Document document,
            SyntaxEditor editor, SyntaxNode identifierName,
            IPropertySymbol property, IFieldSymbol propertyBackingField,
            string desiredGetMethodName, string desiredSetMethodName,
            CancellationToken cancellationToken);

        Task<IList<SyntaxNode>> GetReplacementMembersAsync(
            Document document,
            IPropertySymbol property, SyntaxNode propertyDeclaration,
            IFieldSymbol propertyBackingField,
            string desiredGetMethodName,
            string desiredSetMethodName,
            CancellationToken cancellationToken);

        SyntaxNode GetPropertyNodeToReplace(SyntaxNode propertyDeclaration);
    }
}
