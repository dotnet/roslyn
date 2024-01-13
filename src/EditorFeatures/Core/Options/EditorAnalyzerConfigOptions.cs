// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class EditorAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IDictionary<string, object> _configOptions;

        internal EditorAnalyzerConfigOptions(IEditorOptions editorOptions)
        {
            _configOptions = (editorOptions.GetOptionValue(DefaultOptions.RawCodingConventionsSnapshotOptionName) as IDictionary<string, object>) ??
                SpecializedCollections.EmptyDictionary<string, object>();
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            // TODO: the editor currently seems to store both lower-cased keys and original casing, the comparer is case-sensitive 
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1556206

            if (_configOptions.TryGetValue(key.ToLowerInvariant(), out var objectValue))
            {
                // TODO: Although the editor exposes values typed to object they are actually strings.
                value = objectValue switch
                {
                    null => null,
                    string s => s,
                    object o => o.ToString()
                };

                return value != null;
            }

            value = null;
            return false;
        }

        public override IEnumerable<string> Keys
            => _configOptions.Keys.Where(IsLowercase);

        private static bool IsLowercase(string str)
        {
            foreach (var c in str)
            {
                if (!char.IsLower(c))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal static partial class EditorOptionsExtensions
    {
        public static StructuredAnalyzerConfigOptions ToAnalyzerConfigOptions(this IEditorOptions editorOptions)
            => StructuredAnalyzerConfigOptions.Create(new EditorAnalyzerConfigOptions(editorOptions));
    }
}
