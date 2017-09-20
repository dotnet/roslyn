using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo.Converters
{
    [Export(typeof(IDeferredQuickInfoContentToFrameworkElementConverter))]
    internal sealed class DocumentationCommentDeferredContentConverter : IDeferredQuickInfoContentToFrameworkElementConverter
    {
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        public DocumentationCommentDeferredContentConverter(ClassificationTypeMap typeMap)
        {
            _typeMap = typeMap;
        }

        public FrameworkElement CreateFrameworkElement(IDeferredQuickInfoContent deferredContent, DeferredContentFrameworkElementFactory factory)
        {
            var documentationCommentContent = (DocumentationCommentDeferredContent)deferredContent;

            var documentationTextBlock = new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap
            };

            var formatMap = _typeMap.ClassificationFormatMapService.GetClassificationFormatMap("tooltip");
            documentationTextBlock.SetDefaultTextProperties(formatMap);

            // If we have already computed the symbol documentation by now, update

            UpdateDocumentationTextBlock(documentationCommentContent, documentationTextBlock);
            return documentationTextBlock;
        }

        private void UpdateDocumentationTextBlock(DocumentationCommentDeferredContent deferredContent, TextBlock documentationTextBlock)
        {
            if (!string.IsNullOrEmpty(deferredContent.DocumentationComment))
            {
                documentationTextBlock.Text = deferredContent.DocumentationComment;
            }
            else
            {
                documentationTextBlock.Text = string.Empty;
                documentationTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        public Type GetApplicableType()
        {
            return typeof(DocumentationCommentDeferredContent);
        }
    }
}
