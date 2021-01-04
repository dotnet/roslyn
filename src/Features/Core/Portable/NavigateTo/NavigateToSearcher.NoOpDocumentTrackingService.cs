// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal partial class NavigateToSearcher
    {
        private class NoOpDocumentTrackingService : IDocumentTrackingService
        {
            public static readonly IDocumentTrackingService Instance = new NoOpDocumentTrackingService();

            private NoOpDocumentTrackingService()
            {
            }

#pragma warning disable CS0067
            public event EventHandler<DocumentId>? ActiveDocumentChanged;
            public event EventHandler<EventArgs>? NonRoslynBufferTextChanged;
#pragma warning restore CS0067

            public ImmutableArray<DocumentId> GetVisibleDocuments()
                => ImmutableArray<DocumentId>.Empty;

            public DocumentId? TryGetActiveDocument()
                => null;
        }
    }
}
