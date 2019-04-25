using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal sealed partial class LegacyEditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
    {
        private class DocumentOptions : IDocumentOptions
        {
            private ICodingConventionsSnapshot _codingConventionSnapshot;
            private readonly IErrorLoggerService _errorLogger;
            private static readonly ConditionalWeakTable<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, string>> s_convertedDictionaryCache =
                new ConditionalWeakTable<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, string>>();

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

                // Temporarly map our old Dictionary<string, object> to a Dictionary<string, string>. This can go away once we either
                // eliminate the legacy editorconfig support, or we change IEditorConfigStorageLocation.TryGetOption to take
                // some interface that lets us pass both the Dictionary<string, string> we get from the new system, and the
                // Dictionary<string, object> from the old system.
                // 
                // We cache this with a conditional weak table so we're able to maintain the assumptions in EditorConfigNamingStyleParser
                // that the instance doesn't regularly change and thus can be used for further caching
                var allRawConventions = s_convertedDictionaryCache.GetValue(
                    _codingConventionSnapshot.AllRawConventions,
                    d => ImmutableDictionary.CreateRange(d.Select(c => KeyValuePairUtil.Create(c.Key, c.Value.ToString()))));

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
