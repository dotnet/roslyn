// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Analyzers.ConvertProgram;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertProgram;

using static ConvertProgramAnalysis;
using static ConvertProgramTransform;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToProgramMain), Shared]
internal class ConvertToProgramMainCodeRefactoringProvider : CodeRefactoringProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ConvertToProgramMainCodeRefactoringProvider()
    {
    }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, span, cancellationToken) = context;
        if (!span.IsEmpty)
            return;

        if (!IsApplication(document.Project.CompilationOptions!))
            return;

        var position = span.Start;
        var root = (CompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (!root.IsTopLevelProgram())
            return;

        var acceptableLocation = GetUseProgramMainDiagnosticLocation(root, isHidden: true);
        if (!acceptableLocation.SourceSpan.IntersectsWith(position))
            return;

        var options = await document.GetCSharpCodeFixOptionsProviderAsync(cancellationToken).ConfigureAwait(false);

        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (!CanOfferUseProgramMain(options.PreferTopLevelStatements, root, compilation, forAnalyzer: false))
            return;

        context.RegisterRefactoring(CodeAction.Create(
            CSharpAnalyzersResources.Convert_to_Program_Main_style_program,
            c => ConvertToProgramMainAsync(document, options.AccessibilityModifiersRequired.Value, c),
            nameof(CSharpAnalyzersResources.Convert_to_Program_Main_style_program),
            CodeActionPriority.Low));
    }
}
