// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

internal interface ICodeRefactoringService
{
    Task<bool> HasRefactoringsAsync(TextDocument document, TextSpan textSpan, CancellationToken cancellationToken);

    Task<ImmutableArray<CodeRefactoring>> GetRefactoringsAsync(TextDocument document, TextSpan textSpan, CodeActionRequestPriority? priority, CancellationToken cancellationToken);
}

internal static class ICodeRefactoringServiceExtensions
{
    public static Task<ImmutableArray<CodeRefactoring>> GetRefactoringsAsync(this ICodeRefactoringService service, TextDocument document, TextSpan state, CancellationToken cancellationToken)
        => service.GetRefactoringsAsync(document, state, priority: null, cancellationToken);
}
