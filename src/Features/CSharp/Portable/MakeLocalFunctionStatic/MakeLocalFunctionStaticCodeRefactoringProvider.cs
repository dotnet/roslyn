// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.OrderModifiers;

namespace Microsoft.CodeAnalysis.CSharp.OrderModifiers
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
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            //Gets the code from the document
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var localFunction = await context.TryGetSelectedNodeAsync<LocalFunctionStatementSyntax>().ConfigureAwait(false);
            if (localFunction == default)
            {
                return;
            }


            //Need to make a call to GetCaptures and add the modifer static
            //Need to register refactoring



        }
}
}
