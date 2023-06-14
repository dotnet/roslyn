// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    // Define dummy priority to avoid ifdefs.
    // 'CodeActionPriority' is not a public API, hence not supported in CodeStyle layer.
    // https://github.com/dotnet/roslyn/issues/42431 tracks adding a public API.
#if CODE_STYLE
    internal enum CodeActionPriority
    {
        Lowest = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }
#endif

    internal static class CustomCodeActions
    {
        internal abstract class SimpleCodeAction(
            string title,
            string? equivalenceKey) : CodeAction
        {
            public sealed override string Title { get; } = title;
            public sealed override string? EquivalenceKey { get; } = equivalenceKey;
        }

        internal class DocumentChangeAction(
            string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string? equivalenceKey,
            CodeActionPriority priority) : SimpleCodeAction(title, equivalenceKey)
        {

#if CODE_STYLE
            internal CodeActionPriority Priority { get; } = priority;
#else
            internal override CodeActionPriority Priority { get; } = priority;

#endif

            protected sealed override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
                => createChangedDocument(cancellationToken);
        }

        internal class SolutionChangeAction(
            string title,
            Func<CancellationToken, Task<Solution>> createChangedSolution,
            string? equivalenceKey) : SimpleCodeAction(title, equivalenceKey)
        {
            protected sealed override Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
                => createChangedSolution(cancellationToken).AsNullable();
        }
    }
}
