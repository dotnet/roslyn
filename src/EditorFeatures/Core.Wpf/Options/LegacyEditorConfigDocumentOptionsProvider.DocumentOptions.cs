// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal sealed partial class LegacyEditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
    {
        private class DocumentOptions : IDocumentOptions
        {
            private readonly ICodingConventionsSnapshot _codingConventionSnapshot;
            private readonly IErrorLoggerService _errorLogger;
            private static readonly ConditionalWeakTable<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, string>> s_convertedDictionaryCache =
                new ConditionalWeakTable<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, string>>();

            public DocumentOptions(ICodingConventionsSnapshot codingConventionSnapshot, IErrorLoggerService errorLogger)
            {
                _codingConventionSnapshot = codingConventionSnapshot;
                _errorLogger = errorLogger;
            }

            public bool TryGetDocumentOption(OptionKey option, out object value)
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
                // that the instance doesn't regularly change and thus can be used for further caching.
                var allRawConventions = s_convertedDictionaryCache.GetValue(
                    _codingConventionSnapshot.AllRawConventions,
                    d => new StringConvertingDictionary(d));

                try
                {
                    return editorConfigPersistence.TryGetOption(allRawConventions, option.Option.Type, out value);
                }
                catch (Exception ex)
                {
                    _errorLogger?.LogException(this, ex);
                    value = null;
                    return false;
                }
            }

            /// <summary>
            /// A class that implements <see cref="IReadOnlyDictionary{String, String}" /> atop a <see cref="IReadOnlyDictionary{String, Object}" />
            /// where we just convert the values to strings with ToString(). Ordering of the underlying dictionary is preserved, so that way
            /// code that relies on the underlying ordering of the underlying dictionary isn't affected.
            /// </summary>
            private class StringConvertingDictionary : IReadOnlyDictionary<string, string>
            {
                private readonly IReadOnlyDictionary<string, object> _underlyingDictionary;

                public StringConvertingDictionary(IReadOnlyDictionary<string, object> underlyingDictionary)
                {
                    _underlyingDictionary = underlyingDictionary ?? throw new ArgumentNullException(nameof(underlyingDictionary));
                }

                public string this[string key] => _underlyingDictionary[key]?.ToString();

                public IEnumerable<string> Keys => _underlyingDictionary.Keys;
                public IEnumerable<string> Values => _underlyingDictionary.Values.Select(s => s?.ToString());

                public int Count => _underlyingDictionary.Count;

                public bool ContainsKey(string key) => _underlyingDictionary.ContainsKey(key);

                public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
                {
                    foreach (var pair in _underlyingDictionary)
                    {
                        yield return new KeyValuePair<string, string>(pair.Key, pair.Value?.ToString());
                    }
                }

                public bool TryGetValue(string key, out string value)
                {
                    if (_underlyingDictionary.TryGetValue(key, out var objectValue))
                    {
                        value = objectValue?.ToString();
                        return true;
                    }
                    else
                    {
                        value = null;
                        return false;
                    }
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
            }
        }
    }
}
