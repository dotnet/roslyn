// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.OrderModifiers;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStaticWithParameters
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MakeLocalFunctionStaticCodeRefactoringProvider)), Shared]
    internal class MakeLocalFunctionStaticCodeRefactoringProvider : CodeRefactoringProvider
    {
        

        [ImportingConstructor]
        public MakeLocalFunctionStaticCodeRefactoringProvider()
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

            context.RegisterRefactoring(new MyCodeAction("Make local function static", c => service.CreateParameterSymbolAsync(document, localFunction, cancellationToken)));










        }

        private sealed class MyCodeAction : CodeActions.CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}







