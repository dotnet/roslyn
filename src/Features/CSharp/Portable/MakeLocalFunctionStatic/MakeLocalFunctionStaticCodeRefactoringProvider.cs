// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MakeLocalFunctionStaticCodeRefactoringProvider)), Shared]
    internal sealed class MakeLocalFunctionStaticCodeRefactoringProvider : CodeRefactoringProvider
    {

        [ImportingConstructor]
        public MakeLocalFunctionStaticCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {

            var (document, textSpan, cancellationToken) = context;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var localFunction = await context.TryGetRelevantNodeAsync<LocalFunctionStatementSyntax>().ConfigureAwait(false);
            if (localFunction == default)
            {
                return;
            }

            var service = document.GetLanguageService<MakeLocalFunctionStaticService>();

            context.RegisterRefactoring(new MyCodeAction(FeaturesResources.Make_local_function_static, c => service.CreateParameterSymbolAsync(document, localFunction, c)));

        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}







