// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MakeLocalFunctionStatic), Shared]
    internal sealed class MakeLocalFunctionStaticCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public MakeLocalFunctionStaticCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;

            var syntaxTree = (await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false))!;
            if (!MakeLocalFunctionStaticHelper.IsStaticLocalFunctionSupported(syntaxTree.Options.LanguageVersion()))
            {
                return;
            }

            var localFunction = await context.TryGetRelevantNodeAsync<LocalFunctionStatementSyntax>().ConfigureAwait(false);
            if (localFunction == null)
            {
                return;
            }

            if (localFunction.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                return;
            }

            var semanticModel = (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;

            if (MakeLocalFunctionStaticHelper.CanMakeLocalFunctionStaticByRefactoringCaptures(localFunction, semanticModel, out var captures))
            {
                context.RegisterRefactoring(CodeAction.Create(
                    CSharpAnalyzersResources.Make_local_function_static,
                    c => MakeLocalFunctionStaticCodeFixHelper.MakeLocalFunctionStaticAsync(document, localFunction, captures, context.Options, c),
                    nameof(CSharpAnalyzersResources.Make_local_function_static)));
            }
        }
    }
}
