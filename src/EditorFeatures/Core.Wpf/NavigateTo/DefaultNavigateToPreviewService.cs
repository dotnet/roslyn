// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal sealed class DefaultNavigateToPreviewService : INavigateToPreviewService
    {
        public int GetProvisionalViewingStatus(Document document)
            => 0;

        public bool CanPreview(Document document)
            => true;

        public void PreviewItem(INavigateToItemDisplay itemDisplay)
        {
        }
    }
}
