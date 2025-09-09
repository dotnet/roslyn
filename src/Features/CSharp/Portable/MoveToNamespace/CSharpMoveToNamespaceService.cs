// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveToNamespace;

namespace Microsoft.CodeAnalysis.CSharp.MoveToNamespace;

[ExportLanguageService(typeof(IMoveToNamespaceService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpMoveToNamespaceService(
    [Import(AllowDefault = true)] IMoveToNamespaceOptionsService optionsService) :
    AbstractMoveToNamespaceService<CompilationUnitSyntax, BaseNamespaceDeclarationSyntax, BaseTypeDeclarationSyntax>(optionsService)
{
    protected override BaseTypeDeclarationSyntax? GetNamedTypeDeclarationSyntax(SyntaxNode node)
        => node switch
        {
            BaseTypeDeclarationSyntax namedTypeDeclaration => namedTypeDeclaration,
            ParameterListSyntax parameterList => parameterList.Parent as BaseTypeDeclarationSyntax,
            _ => null
        };

    protected override string GetNamespaceName(SyntaxNode container)
        => container switch
        {
            BaseNamespaceDeclarationSyntax namespaceSyntax => namespaceSyntax.Name.ToString(),
            CompilationUnitSyntax _ => string.Empty,
            _ => throw ExceptionUtilities.UnexpectedValue(container)
        };

    protected override bool IsContainedInNamespaceDeclaration(BaseNamespaceDeclarationSyntax baseNamespace, int position)
    {
        var namespaceDeclarationStart = baseNamespace.NamespaceKeyword.SpanStart;
        var namespaceDeclarationEnd = baseNamespace switch
        {
            NamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.OpenBraceToken.SpanStart,
            FileScopedNamespaceDeclarationSyntax fileScopedNamespace => fileScopedNamespace.SemicolonToken.Span.End,
            _ => throw ExceptionUtilities.UnexpectedValue(baseNamespace.Kind()),
        };

        return position >= namespaceDeclarationStart && position < namespaceDeclarationEnd;
    }
}
