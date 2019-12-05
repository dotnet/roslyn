// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigStorageLocationExtensions
    {
        public static bool TryGetOption(this IEditorConfigStorageLocation editorConfigStorageLocation, AnalyzerConfigOptions analyzerConfigOptions, Type type, out object value)
        {
            return editorConfigStorageLocation.TryGetOption(new AnalyzerConfigOptionsDictionary(analyzerConfigOptions), type, out value);
        }

        /// <summary>
        /// This is a wrapper around AnalyzerConfigOptions so that it can be used as a read-only Dictionary.
        /// </summary>
        private class AnalyzerConfigOptionsDictionary : IReadOnlyDictionary<string, string>
        {
            private readonly AnalyzerConfigOptions _analyzerConfigOptions;

            public AnalyzerConfigOptionsDictionary(AnalyzerConfigOptions analyzerConfigOptions)
            {
                _analyzerConfigOptions = analyzerConfigOptions;
            }

            public string this[string key]
            {
                get
                {
                    if (_analyzerConfigOptions.TryGetValue(key, out var value))
                    {
                        return value;
                    }

                    throw new KeyNotFoundException();
                }
            }

            public IEnumerable<string> Keys => throw new NotImplementedException();

            public IEnumerable<string> Values => throw new NotImplementedException();

            public int Count => throw new NotImplementedException();

            public bool ContainsKey(string key)
            {
                return _analyzerConfigOptions.TryGetValue(key, out _);
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, out string value)
            {
                return _analyzerConfigOptions.TryGetValue(key, out value);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}
