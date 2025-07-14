// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
    Name = PredefinedCodeRefactoringProviderNames.ImplementInterface), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ImplementInterfaceCodeRefactoringProvider() : CodeRefactoringProvider
{
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;

        var helpers = document.GetRequiredLanguageService<IRefactoringHelpersService>();
        var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // We offer the refactoring when the user is between any members of a class/struct and are on a blank line.
        if (!helpers.IsBetweenTypeMembers(sourceText, root, textSpan.Start, out var typeDeclaration))
            return;

        var service = document.GetRequiredLanguageService<IImplementInterfaceService>();
        using var allCodeActions = TemporaryArray<CodeAction>.Empty;

        foreach (var typeNode in service.GetInterfaceTypes(typeDeclaration))
        {
            var codeActions = await service.GetCodeActionsAsync(
                document, typeNode, cancellationToken).ConfigureAwait(false);

            allCodeActions.AddRange(codeActions);
        }

        context.RegisterRefactorings(allCodeActions.ToImmutableAndClear(), textSpan);
    }
}
