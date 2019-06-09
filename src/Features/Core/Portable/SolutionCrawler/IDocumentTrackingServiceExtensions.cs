// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class IDocumentTrackingServiceExtensions
    {
        /// <summary>
        /// Gets the active <see cref="Document"/> the user is currently working in. May be null if
        /// there is no active document or the active document is not in this <paramref name="solution"/>.
        /// </summary>
        public static Document GetActiveDocument(this IDocumentTrackingService service, Solution solution)
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
    }
}
