using System;
using System.Linq;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal sealed partial class EditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
    {
        private class DocumentOptions : IDocumentOptions
        {
            private ICodingConventionsSnapshot _codingConventionSnapshot;
            private readonly IErrorLoggerService _errorLogger;

            public DocumentOptions(ICodingConventionsSnapshot codingConventionSnapshot, IErrorLoggerService errorLogger)
            {
                _codingConventionSnapshot = codingConventionSnapshot;
                _errorLogger = errorLogger;
            }

            public bool TryGetDocumentOption(OptionKey option, OptionSet underlyingOptions, out object value)
            {
                var editorConfigPersistence = option.Option.StorageLocations.OfType<IEditorConfigStorageLocation>().SingleOrDefault();
                if (editorConfigPersistence == null)
                {
                    value = null;
                    return false;
                }

                var allRawConventions = _codingConventionSnapshot.AllRawConventions;
                try
                {
                    var underlyingOption = underlyingOptions.GetOption(option);
                    return editorConfigPersistence.TryGetOption(underlyingOption, allRawConventions, option.Option.Type, out value);
                }
                catch (Exception ex)
                {
                    _errorLogger?.LogException(this, ex);
                    value = null;
                    return false;
                }
            }
        }
    }
}
