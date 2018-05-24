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
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo.Converters
{
    [Export(typeof(IDeferredQuickInfoContentToFrameworkElementConverter))]
    [QuickInfoConverterMetadata(typeof(DocumentationCommentDeferredContent))]
    internal sealed class DocumentationCommentDeferredContentConverter : IDeferredQuickInfoContentToFrameworkElementConverter
    {
        private readonly ClassificationTypeMap _typeMap;
        private readonly IClassificationFormatMapService _classificationFormatMapService;

        [ImportingConstructor]
        public DocumentationCommentDeferredContentConverter(ClassificationTypeMap typeMap, IClassificationFormatMapService classificationFormatMapService)
        {
            _typeMap = typeMap;
            _classificationFormatMapService = classificationFormatMapService;
        }

        public FrameworkElement CreateFrameworkElement(IDeferredQuickInfoContent deferredContent, DeferredContentFrameworkElementFactory factory)
        {
            var documentationCommentContent = (DocumentationCommentDeferredContent)deferredContent;

            var documentationTextBlock = new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap
            };

            var formatMap = _classificationFormatMapService.GetClassificationFormatMap("tooltip");
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
