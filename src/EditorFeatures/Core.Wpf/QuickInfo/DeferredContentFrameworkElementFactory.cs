using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    [Export]
    internal class DeferredContentFrameworkElementFactory
    {
        private readonly Dictionary<Type, IDeferredQuickInfoContentToFrameworkElementConverter> _convertersByType;

        [ImportingConstructor]
        public DeferredContentFrameworkElementFactory([ImportMany] IEnumerable<IDeferredQuickInfoContentToFrameworkElementConverter> converters)
        {
            _convertersByType = converters.ToDictionary(c => c.GetApplicableType());
        }

        internal FrameworkElement CreateElement(IDeferredQuickInfoContent deferredContent)
        {
            return _convertersByType[deferredContent.GetType()].CreateFrameworkElement(deferredContent, this);
        }
    }
}
