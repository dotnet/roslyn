// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Analyzers.ConvertProgram;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertProgram
{
    using static ConvertProgramAnalysis;
    using static ConvertProgramTransform;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToTopLevelStatements), Shared]
    internal class ConvertToTopLevelStatementsCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertToTopLevelStatementsCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            // can only suggest moving to top level statement on c# 9 or above.
            if (document.Project.ParseOptions!.LanguageVersion() < LanguageVersion.CSharp9 ||
                !IsApplication(document.Project.CompilationOptions!))
            {
                return;
            }

            var methodDeclaration = await context.TryGetRelevantNodeAsync<MethodDeclarationSyntax>().ConfigureAwait(false);
            if (methodDeclaration is null)
                return;

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var option = options.GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements);
            if (!CanOfferUseTopLevelStatements(option, forAnalyzer: false))
                return;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;

            if (!IsProgramMainMethod(semanticModel, methodDeclaration, GetMainTypeName(compilation), cancellationToken, out var canConvert) ||
                !canConvert)
            {
                return;
            }

            context.RegisterRefactoring(CodeAction.CreateWithPriority(
                CodeActionPriority.Low,
                CSharpAnalyzersResources.Convert_to_top_level_statements,
                c => ConvertToTopLevelStatementsAsync(document, methodDeclaration, context.Options, c),
                nameof(CSharpAnalyzersResources.Convert_to_top_level_statements)));
        }
    }
}
