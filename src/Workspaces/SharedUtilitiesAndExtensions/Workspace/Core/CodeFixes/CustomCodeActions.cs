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
        internal abstract class SimpleCodeAction : CodeAction
        {
            public SimpleCodeAction(
                string title,
                string? equivalenceKey)
            {
                Title = title;
                EquivalenceKey = equivalenceKey;
            }

            public sealed override string Title { get; }
            public sealed override string? EquivalenceKey { get; }
        }

        internal class DocumentChangeAction : SimpleCodeAction
        {
            private readonly Func<CancellationToken, Task<Document>> _createChangedDocument;

#if CODE_STYLE
            internal CodeActionPriority Priority { get; }
#else
            internal override CodeActionPriority Priority { get; }
#endif

            public DocumentChangeAction(
                string title,
                Func<CancellationToken, Task<Document>> createChangedDocument,
                string? equivalenceKey,
                CodeActionPriority priority)
                : base(title, equivalenceKey)
            {
                _createChangedDocument = createChangedDocument;
                Priority = priority;
            }

            protected sealed override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
                => _createChangedDocument(cancellationToken);
        }

        internal class SolutionChangeAction : SimpleCodeAction
        {
            private readonly Func<CancellationToken, Task<Solution>> _createChangedSolution;

            public SolutionChangeAction(
                string title,
                Func<CancellationToken, Task<Solution>> createChangedSolution,
                string? equivalenceKey)
                : base(title, equivalenceKey)
            {
                _createChangedSolution = createChangedSolution;
            }

            protected sealed override Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
                => _createChangedSolution(cancellationToken).AsNullable();
        }
    }
}
