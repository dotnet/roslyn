// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

        private readonly PooledHashSet<string> _hintNames;

        private readonly object _collectionLock = new object();

        internal AdditionalSourcesCollection()
        {
            _hintNames = PooledHashSet<string>.GetInstance();
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

            // allow any identifier character or [.-_ ()[]{}]
            for (int i = 0; i < hintName.Length; i++)
            {
                char c = hintName[i];
                if (!UnicodeCharacterUtilities.IsIdentifierPartCharacter(c)
                    && c != '.'
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
            lock (_collectionLock)
            {
                if (_hintNames.Contains(hintName))
                {
                    throw new ArgumentException(CodeAnalysisResources.HintNameUniquePerGenerator, nameof(hintName));
                }

                _hintNames.Add(hintName);
                _sourcesAdded.Add(new GeneratedSourceText(hintName, source));
            }
        }

        public void RemoveSource(string hintName)
        {
            hintName = AppendExtensionIfRequired(hintName);
            lock (_collectionLock)
            {
                if (_hintNames.Contains(hintName))
                {
                    _hintNames.Remove(hintName);
                    for (int i = 0; i < _sourcesAdded.Count; i++)
                    {
                        // check the hashcodes, as thats the comparison we're using to put the names into _hintNames
                        if (_sourcesAdded[i].HintName.GetHashCode() == hintName.GetHashCode())
                        {
                            _sourcesAdded.Remove(_sourcesAdded[i]);
                            return;
                        }
                    }
                }
            }
        }

        public bool Contains(string hintName)
        {
            lock (_collectionLock)
            {
                return _hintNames.Contains(AppendExtensionIfRequired(hintName));
            }
        }

        internal ImmutableArray<GeneratedSourceText> ToImmutableAndFree()
        {
            _hintNames.Free();
            return _sourcesAdded.ToImmutableAndFree();
        }

        private static string AppendExtensionIfRequired(string hintName)
        {
            if (!Path.GetExtension(hintName).Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                hintName = string.Concat(hintName, ".cs");
            }

            return hintName;
        }
    }
}
