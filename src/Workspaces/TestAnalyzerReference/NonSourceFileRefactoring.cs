// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
#pragma warning disable RS0034 // Exported parts should be marked with 'ImportingConstructorAttribute'
    [ExportCodeRefactoringProvider(
        LanguageNames.CSharp,
        DocumentKinds = [nameof(TextDocumentKind.AdditionalDocument), nameof(TextDocumentKind.AnalyzerConfigDocument)],
        DocumentExtensions = [".txt", ".editorconfig"])]
    [Shared]
    public sealed class NonSourceFileRefactoring : CodeRefactoringProvider
    {
        public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            context.RegisterRefactoring(CodeAction.Create(nameof(NonSourceFileRefactoring),
                createChangedSolution: async ct =>
                {
                    var document = context.TextDocument;
                    var text = await document.GetTextAsync(ct).ConfigureAwait(false);
                    var newText = SourceText.From(text.ToString() + Environment.NewLine + "# Refactored");
                    if (document is AdditionalDocument)
                        return document.Project.Solution.WithAdditionalDocumentText(document.Id, newText);
                    return document.Project.Solution.WithAnalyzerConfigDocumentText(document.Id, newText);
                }));

            return Task.CompletedTask;
        }
    }
}

