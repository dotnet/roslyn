// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    internal sealed class AdditionalSourcesCollection
    {
        private readonly ArrayBuilder<GeneratedSourceText> _sourcesAdded;

        private const StringComparison _hintNameComparison = StringComparison.OrdinalIgnoreCase;

        private static readonly StringComparer s_hintNameComparer = StringComparer.OrdinalIgnoreCase;

        internal AdditionalSourcesCollection()
        {
            _sourcesAdded = ArrayBuilder<GeneratedSourceText>.GetInstance();
        }

        internal AdditionalSourcesCollection(ImmutableArray<GeneratedSourceText> existingSources)
            : this()
        {
            _sourcesAdded.AddRange(existingSources);
        }

        public void Add(string hintName, SourceText source)
        {
            if (string.IsNullOrWhiteSpace(hintName))
            {
                throw new ArgumentNullException(nameof(hintName));
            }

            // allow any identifier character or [.,-_ ()[]{}]
            for (int i = 0; i < hintName.Length; i++)
            {
                char c = hintName[i];
                if (!UnicodeCharacterUtilities.IsIdentifierPartCharacter(c)
                    && c != '.'
                    && c != ','
                    && c != '-'
                    && c != '_'
                    && c != ' '
                    && c != '('
                    && c != ')'
                    && c != '['
                    && c != ']'
                    && c != '{'
                    && c != '}')
                {
                    throw new ArgumentException(string.Format(CodeAnalysisResources.HintNameInvalidChar, c, i), nameof(hintName));
                }
            }

            hintName = AppendExtensionIfRequired(hintName);
            if (this.Contains(hintName))
            {
                throw new ArgumentException(CodeAnalysisResources.HintNameUniquePerGenerator, nameof(hintName));
            }

            if (source.Encoding is null)
            {
                throw new ArgumentException(CodeAnalysisResources.SourceTextRequiresEncoding, nameof(source));
            }

            _sourcesAdded.Add(new GeneratedSourceText(hintName, source));
        }

        public void RemoveSource(string hintName)
        {
            hintName = AppendExtensionIfRequired(hintName);
            for (int i = 0; i < _sourcesAdded.Count; i++)
            {
                if (s_hintNameComparer.Equals(_sourcesAdded[i].HintName, hintName))
                {
                    _sourcesAdded.RemoveAt(i);
                    return;
                }
            }
        }

        public bool Contains(string hintName)
        {
            hintName = AppendExtensionIfRequired(hintName);
            for (int i = 0; i < _sourcesAdded.Count; i++)
            {
                if (s_hintNameComparer.Equals(_sourcesAdded[i].HintName, hintName))
                {
                    return true;
                }
            }
            return false;
        }

        internal ImmutableArray<GeneratedSourceText> ToImmutableAndFree() => _sourcesAdded.ToImmutableAndFree();

        private static string AppendExtensionIfRequired(string hintName)
        {
            if (!hintName.EndsWith(".cs", _hintNameComparison))
            {
                hintName = string.Concat(hintName, ".cs");
            }

            return hintName;
        }
    }
}
