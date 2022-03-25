// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class IDocumentTrackingServiceExtensions
    {
        /// <summary>
        /// Gets the active <see cref="Document"/> the user is currently working in. May be null if
        /// there is no active document or the active document is not in this <paramref name="solution"/>.
        /// </summary>
        public static Document? GetActiveDocument(this IDocumentTrackingService service, Solution solution)
        {
            // Note: GetDocument checks that the DocId is contained in the solution, and returns null if not.
            return solution.GetDocument(service.TryGetActiveDocument());
        }

        /// <summary>
        /// Get a read only collection of all the unique visible documents in the workspace that are
        /// contained within <paramref name="solution"/>.
        /// </summary>
        public static ImmutableArray<Document> GetVisibleDocuments(this IDocumentTrackingService service, Solution solution)
            => service.GetVisibleDocuments()
                      .Select(d => solution.GetDocument(d))
                      .WhereNotNull()
                      .Distinct()
                      .ToImmutableArray();

        /// <summary>
        /// Waits the specified amount of time while the specified <paramref name="document"/> is not visible.  If any
        /// document visibility changes happen, the delay will cancel.
        /// </summary>
        public static Task DelayWhileNonVisibleAsync(this IDocumentTrackingService service, Document document, TimeSpan timeSpan, CancellationToken cancellationToken)
            => DelayWhileNonVisibleAsync(service, ImmutableArray.Create(document), timeSpan, cancellationToken);

        public static async Task DelayWhileNonVisibleAsync(this IDocumentTrackingService service, ImmutableArray<Document> documents, TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            // Only add a delay if we have access to a service that will tell us when the documents become visible or not.
            if (!service.SupportsDocumentTracking)
                return;

            // ensure we listen for visibility changes before checking.  That way we don't have a race where we check
            // something see it is not visible, but then do not hear about its visibility change because we've hooked up
            // our event after that happens.
            var visibilityChangedTaskSource = new TaskCompletionSource<bool>();
            service.ActiveDocumentChanged += OnActiveDocumentChanged;

            try
            {
                // if any of the docs are visible, then no delay.
                var visibleDocuments = service.GetVisibleDocuments();
                var anyVisible = documents.Select(d => d.Id).Intersect(visibleDocuments).Any();
                if (anyVisible)
                    return;

                // Listen to when the active document changed so that we startup work on a document once it becomes visible.

                var delayTask = Task.Delay(timeSpan, cancellationToken);

                await Task.WhenAny(delayTask, visibilityChangedTaskSource.Task).ConfigureAwait(false);
            }
            finally
            {
                service.ActiveDocumentChanged -= OnActiveDocumentChanged;
            }

            return;

            void OnActiveDocumentChanged(object? sender, DocumentId? e)
            {
                visibilityChangedTaskSource.SetResult(true);
            }
        }
    }
}
