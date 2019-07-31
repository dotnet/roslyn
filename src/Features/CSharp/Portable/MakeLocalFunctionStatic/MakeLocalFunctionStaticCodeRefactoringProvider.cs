// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.GetCapturedVariables;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStaticWithParams
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MakeLocalFunctionStaticWithParamsCodeRefactoringProvider)), Shared]
    internal class MakeLocalFunctionStaticWithParamsCodeRefactoringProvider : CodeRefactoringProvider
    {

        [ImportingConstructor]
        public MakeLocalFunctionStaticWithParamsCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {

            var (document, textSpan, cancellationToken) = context;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            //Gets the local function statement
            var localFunction = await context.TryGetSelectedNodeAsync<LocalFunctionStatementSyntax>().ConfigureAwait(false);
            if (localFunction == default)
            {
                return;
            }

            var service = document.GetLanguageService<GetCaptures>();

            //Need to register refactoring and add the modifier static
            context.RegisterRefactoring(new MyCodeAction("Make Local Function Static", c => service.CreateParameterSymbolAsync(document, localFunction, c)));


        }

        private sealed class MyCodeAction : CodeActions.CodeAction.SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution)
            {
            }
        }
    }
}







