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

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    [Export(typeof(IDeferredQuickInfoContentToFrameworkElementConverter))]
    class ClassifiableDeferredContentConverter : IDeferredQuickInfoContentToFrameworkElementConverter
    {
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        public ClassifiableDeferredContentConverter(
            ClassificationTypeMap typeMap)
        {
            _typeMap = typeMap;
        }

        public FrameworkElement CreateFrameworkElement(IDeferredQuickInfoContent deferredContent, DeferredContentFrameworkElementFactory factory)
        {
            var classifiableContent = (ClassifiableDeferredContent)deferredContent;
            var classifiedTextBlock = classifiableContent.ClassifiableContent.ToTextBlock(_typeMap);

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
