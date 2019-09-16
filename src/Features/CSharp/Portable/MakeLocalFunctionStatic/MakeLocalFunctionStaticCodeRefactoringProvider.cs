// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MakeLocalFunctionStaticCodeRefactoringProvider)), Shared]
    internal sealed class MakeLocalFunctionStaticCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;

            var syntaxTree = (await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false))!;
            if (!MakeLocalFunctionStaticHelper.IsStaticLocalFunctionSupported(syntaxTree))
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

            if (MakeLocalFunctionStaticHelper.TryGetCaputuredSymbols(localFunction, semanticModel, out var captures) &&
                captures.Length > 0 &&
                !captures.Any(s => s.IsThisParameter()))
            {
                context.RegisterRefactoring(new MyCodeAction(
                    FeaturesResources.Make_local_function_static,
                    c => MakeLocalFunctionStaticHelper.MakeLocalFunctionStaticAsync(document, semanticModel, localFunction, captures, c)));
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
