// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.ConvertConversionOperators
{
    /// <summary>
    /// Refactor:
    ///     var o = (object)1;
    ///
    /// Into:
    ///     var o = 1 as object;
    ///
    /// Or:
    ///     visa versa
    /// </summary>
    internal abstract class AbstractConvertConversionOperatorsRefactoringProvider<TInvocationExpressionSyntax> : CodeRefactoringProvider
        where TInvocationExpressionSyntax : SyntaxNode
    {
        protected abstract string GetTitle();
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var awaitable = await context.TryGetRelevantNodeAsync<TInvocationExpressionSyntax>().ConfigureAwait(false);

            //context.RegisterRefactoring(
            //    new MyCodeAction(
            //        GetTitle(),
            //        c => AddAwaitAsync(document, awaitable, withConfigureAwait: false, c)),
            //    awaitable.Span);
        }
    }
}
