// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MoveDeclarationNearReference;

internal abstract class AbstractMoveDeclarationNearReferenceCodeRefactoringProvider<TLocalDeclaration> : CodeRefactoringProvider where TLocalDeclaration : SyntaxNode
{
    [ImportingConstructor]
    public AbstractMoveDeclarationNearReferenceCodeRefactoringProvider()
    {
    }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, _, cancellationToken) = context;
        var declaration = await context.TryGetRelevantNodeAsync<TLocalDeclaration>().ConfigureAwait(false);
        if (declaration == null)
        {
            return;
        }

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(declaration);
        if (variables.Count != 1)
        {
            return;
        }

        var service = document.GetRequiredLanguageService<IMoveDeclarationNearReferenceService>();
        if (!await service.CanMoveDeclarationNearReferenceAsync(document, declaration, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        context.RegisterRefactoring(
            CodeAction.Create(
                FeaturesResources.Move_declaration_near_reference,
                c => MoveDeclarationNearReferenceAsync(document, declaration, c),
                nameof(FeaturesResources.Move_declaration_near_reference),
                CodeActionPriority.Low),
            declaration.Span);
    }

    private static async Task<Document> MoveDeclarationNearReferenceAsync(
        Document document, SyntaxNode statement, CancellationToken cancellationToken)
    {
        var service = document.GetRequiredLanguageService<IMoveDeclarationNearReferenceService>();
        return await service.MoveDeclarationNearReferenceAsync(document, statement, cancellationToken).ConfigureAwait(false);
    }
}
