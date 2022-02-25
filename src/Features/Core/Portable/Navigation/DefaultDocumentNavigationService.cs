// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal sealed class DefaultDocumentNavigationService : IDocumentNavigationService
    {
        public bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
            => false;

        public Task<bool> CanNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public bool CanNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken)
            => false;

        public bool CanNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken)
            => false;

        public bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, bool allowInvalidSpan, CancellationToken cancellationToken)
            => false;

        public Task<bool> TryNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, bool allowInvalidSpan, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, NavigationOptions options, CancellationToken cancellationToken)
            => false;

        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, NavigationOptions options, CancellationToken cancellationToken)
            => false;

        public Task<bool> TryNavigateToPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, NavigationOptions options, CancellationToken cancellationToken)
            => SpecializedTasks.False;
    }
}
