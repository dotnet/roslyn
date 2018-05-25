using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    [Export]
    internal class DeferredContentFrameworkElementFactory
    {
        private readonly Dictionary<string, Lazy<IDeferredQuickInfoContentToFrameworkElementConverter>> _convertersByTypeFullName
            = new Dictionary<string, Lazy<IDeferredQuickInfoContentToFrameworkElementConverter>>();
        private readonly IEnumerable<Lazy<IDeferredQuickInfoContentToFrameworkElementConverter>> _convertersWithoutMetadata;

        [ImportingConstructor]
        public DeferredContentFrameworkElementFactory(
            [ImportMany] IEnumerable<Lazy<IDeferredQuickInfoContentToFrameworkElementConverter, QuickInfoConverterMetadata>> converters,
            [ImportMany] IEnumerable<Lazy<IDeferredQuickInfoContentToFrameworkElementConverter>> convertersWithoutMetadata)
        {
            _convertersByTypeFullName = converters
                .Where(i => !string.IsNullOrEmpty(i.Metadata.DeferredTypeFullName))
                .ToDictionary(
                    lazy => lazy.Metadata.DeferredTypeFullName,
                    lazy => (Lazy<IDeferredQuickInfoContentToFrameworkElementConverter>)lazy);

            _convertersWithoutMetadata = convertersWithoutMetadata;
        }

        internal FrameworkElement CreateElement(IDeferredQuickInfoContent deferredContent)
        {
            var deferredContentFullName = deferredContent.GetType().FullName;
            Lazy<IDeferredQuickInfoContentToFrameworkElementConverter> converter;

            if (!_convertersByTypeFullName.TryGetValue(deferredContentFullName, out converter))
            {
                // The content must be of a type we didn't have MEF deferred metadata for. Realize the
                // ones without MEF metadata, forcing everything to load.
                foreach (var converterWithoutMetadata in _convertersWithoutMetadata)
                {
                    _convertersByTypeFullName[converterWithoutMetadata.Value.GetApplicableType().FullName] =
                        new Lazy<IDeferredQuickInfoContentToFrameworkElementConverter>(() => converterWithoutMetadata.Value);
                }

                Contract.ThrowIfFalse(_convertersByTypeFullName.TryGetValue(deferredContentFullName, out converter));
            }

            return converter.Value.CreateFrameworkElement(deferredContent, this);
        }

        internal class QuickInfoConverterMetadata
        {
            public QuickInfoConverterMetadata(IDictionary<string, object> data)
            {
                DeferredTypeFullName = (string)data.GetValueOrDefault(nameof(QuickInfoConverterMetadataAttribute.DeferredTypeFullName));
            }

            public string DeferredTypeFullName { get; set; }
        }
    }
}
