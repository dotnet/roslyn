// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal interface ICodeRefactoringService
    {
        Task<bool> HasRefactoringsAsync(Document document, TextSpan textSpan, CodeActionOptions options, CancellationToken cancellationToken);

        Task<ImmutableArray<CodeRefactoring>> GetRefactoringsAsync(Document document, TextSpan textSpan, CodeActionRequestPriority priority, CodeActionOptions options, Func<string, IDisposable?> addOperationScope, CancellationToken cancellationToken);
    }

    internal static class ICodeRefactoringServiceExtensions
    {
        public static Task<ImmutableArray<CodeRefactoring>> GetRefactoringsAsync(this ICodeRefactoringService service, Document document, TextSpan state, CodeActionOptions options, CancellationToken cancellationToken)
            => service.GetRefactoringsAsync(document, state, CodeActionRequestPriority.None, options, addOperationScope: _ => null, cancellationToken);
    }
}
