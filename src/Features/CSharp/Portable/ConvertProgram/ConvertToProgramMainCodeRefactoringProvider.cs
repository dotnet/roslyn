// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Analyzers.ConvertProgram;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertProgram
{
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

            var position = span.Start;
            var root = (CompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);
            var acceptableLocation = GetUseProgramMainDiagnosticLocation(root, isHidden: true);
            if (!acceptableLocation.SourceSpan.IntersectsWith(position))
                return;

            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements);

            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (!CanOfferUseProgramMain(option, root, compilation, forAnalyzer: false))
                return;

            context.RegisterRefactoring(new MyCodeAction(
                c => ConvertToProgramMainAsync(document, c)));
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            internal override CodeActionPriority Priority => CodeActionPriority.Low;

            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Convert_to_Program_Main_style_program, createChangedDocument, nameof(ConvertToProgramMainCodeRefactoringProvider))
            {
            }
        }
    }
}
