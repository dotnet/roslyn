using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    [Export(typeof(IDeferredQuickInfoContentToFrameworkElementConverter))]
    class ClassifiableDeferredContentConverter : IDeferredQuickInfoContentToFrameworkElementConverter
    {
        private readonly ClassificationTypeMap _typeMap;
        private readonly IClassificationFormatMapService _classificationFormatMapService;

        [ImportingConstructor]
        public ClassifiableDeferredContentConverter(
            ClassificationTypeMap typeMap,
            IClassificationFormatMapService classificationFormatMapService)
        {
            _typeMap = typeMap;
            _classificationFormatMapService = classificationFormatMapService;
        }

        public FrameworkElement CreateFrameworkElement(IDeferredQuickInfoContent deferredContent, DeferredContentFrameworkElementFactory factory)
        {
            var classifiableContent = (ClassifiableDeferredContent)deferredContent;
            var formatMap = _classificationFormatMapService.GetClassificationFormatMap("tooltip");
            var classifiedTextBlock = classifiableContent.ClassifiableContent.ToTextBlock(formatMap, _typeMap);

            if (classifiedTextBlock.Inlines.Count == 0)
            {
                classifiedTextBlock.Visibility = Visibility.Collapsed;
            }

            return classifiedTextBlock;
        }

        public Type GetApplicableType()
        {
            return typeof(ClassifiableDeferredContent);
        }
    }
}
