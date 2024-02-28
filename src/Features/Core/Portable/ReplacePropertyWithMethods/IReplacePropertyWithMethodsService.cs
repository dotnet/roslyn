// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ReplacePropertyWithMethods;

internal interface IReplacePropertyWithMethodsService : ILanguageService
{
    Task<SyntaxNode> GetPropertyDeclarationAsync(CodeRefactoringContext context);

    Task ReplaceReferenceAsync(
        Document document,
        SyntaxEditor editor, SyntaxNode identifierName,
        IPropertySymbol property, IFieldSymbol propertyBackingField,
        string desiredGetMethodName, string desiredSetMethodName,
        CancellationToken cancellationToken);

    Task<ImmutableArray<SyntaxNode>> GetReplacementMembersAsync(
        Document document,
        IPropertySymbol property, SyntaxNode propertyDeclaration,
        IFieldSymbol propertyBackingField,
        string desiredGetMethodName,
        string desiredSetMethodName,
        CodeGenerationOptionsProvider fallbackOptions,
        CancellationToken cancellationToken);

    SyntaxNode GetPropertyNodeToReplace(SyntaxNode propertyDeclaration);
}
