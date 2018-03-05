// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal sealed class DefaultNavigateToPreviewService : INavigateToPreviewService
    {
        public int GetProvisionalViewingStatus(Document document)
        {
            return 0;
        }

        public bool CanPreview(Document document)
        {
            return true;
        }

        public void PreviewItem(INavigateToItemDisplay itemDisplay)
        {
        }
    }
}
