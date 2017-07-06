// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal class DocumentationCommentDeferredContent : IDeferredQuickInfoContent
    {
        private readonly string _documentationComment;
        private readonly ClassificationTypeMap _typeMap;

        internal void WaitForDocumentationCommentTask_ForTestingPurposesOnly()
        {
        }

        public DocumentationCommentDeferredContent(
            string documentationComment,
            ClassificationTypeMap typeMap)
        {
            _documentationComment = documentationComment;
            _typeMap = typeMap;
        }

        public FrameworkElement Create()
        {
            var documentationTextBlock = new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap
            };

            var formatMap = _typeMap.ClassificationFormatMapService.GetClassificationFormatMap("tooltip");
            documentationTextBlock.SetDefaultTextProperties(formatMap);

            // If we have already computed the symbol documentation by now, update

            UpdateDocumentationTextBlock(documentationTextBlock);
            return documentationTextBlock;
        }

        private void UpdateDocumentationTextBlock(TextBlock documentationTextBlock)
        {
            if (!string.IsNullOrEmpty(_documentationComment))
            {
                documentationTextBlock.Text = _documentationComment;
            }
            else
            {
                documentationTextBlock.Text = string.Empty;
                documentationTextBlock.Visibility = Visibility.Collapsed;
            }
        }
    }
}
