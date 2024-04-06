// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ReplaceMethodWithProperty;

internal interface IReplaceMethodWithPropertyService : ILanguageService
{
    Task<SyntaxNode> GetMethodDeclarationAsync(CodeRefactoringContext context);

    void ReplaceGetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged);
    void ReplaceSetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged);

    void ReplaceGetMethodWithProperty(
        CodeGenerationOptions options, ParseOptions parseOptions,
        SyntaxEditor editor, SemanticModel semanticModel,
        GetAndSetMethods getAndSetMethods, string propertyName, bool nameChanged, CancellationToken cancellationToken);

    void RemoveSetMethod(SyntaxEditor editor, SyntaxNode setMethodDeclaration);
}

internal readonly struct GetAndSetMethods(
    IMethodSymbol getMethod, IMethodSymbol setMethod,
    SyntaxNode getMethodDeclaration, SyntaxNode setMethodDeclaration)
{
    public readonly IMethodSymbol GetMethod = getMethod;
    public readonly IMethodSymbol SetMethod = setMethod;
    public readonly SyntaxNode GetMethodDeclaration = getMethodDeclaration;
    public readonly SyntaxNode SetMethodDeclaration = setMethodDeclaration;
}
