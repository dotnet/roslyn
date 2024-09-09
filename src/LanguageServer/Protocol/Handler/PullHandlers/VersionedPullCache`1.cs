// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Simplified version of <see cref="VersionedPullCache{TCheapVersion, TExpensiveVersion}"/> that only uses a 
    /// single cheap key to check results against.
    /// </summary>
    internal class VersionedPullCache<TVersion> : VersionedPullCache<TVersion, object?>
    {
        public VersionedPullCache(string uniqueKey)
            : base(uniqueKey)
        {
        }

        public Task<string?> GetNewResultIdAsync(
            Dictionary<Document, PreviousPullResult> documentToPreviousDiagnosticParams,
            Document document,
            Func<Task<TVersion>> computeVersionAsync,
            CancellationToken cancellationToken)
        {
            return GetNewResultIdAsync(
                documentToPreviousDiagnosticParams.ToDictionary(kvp => new ProjectOrDocumentId(kvp.Key.Id), kvp => kvp.Value),
                new ProjectOrDocumentId(document.Id),
                document.Project,
                computeVersionAsync,
                computeExpensiveVersionAsync: SpecializedTasks.Null<object>,
                cancellationToken);
        }
    }
}
