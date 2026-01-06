// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractMainMethodSnippetProvider<TMethodDeclarationSyntax, TStatementSyntax, TTypeSyntax> : AbstractSingleChangeSnippetProvider<TMethodDeclarationSyntax>
    where TMethodDeclarationSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TTypeSyntax : SyntaxNode
{
    protected abstract TTypeSyntax GenerateReturnType(SyntaxGenerator generator);

    protected abstract IEnumerable<TStatementSyntax> GenerateInnerStatements(SyntaxGenerator generator);

    protected sealed override Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var generator = SyntaxGenerator.GetGenerator(document);
        var method = generator.MethodDeclaration(
            name: WellKnownMemberNames.EntryPointMethodName,
            parameters: [generator.ParameterDeclaration(
                name: "args",
                type: generator.ArrayTypeExpression(generator.TypeExpression(SpecialType.System_String)))],
            returnType: GenerateReturnType(generator),
            modifiers: DeclarationModifiers.Static,
            statements: GenerateInnerStatements(generator));

        return Task.FromResult(new TextChange(TextSpan.FromBounds(position, position), method.NormalizeWhitespace().ToFullString()));
    }

    protected sealed override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(TMethodDeclarationSyntax node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        => [];
}
