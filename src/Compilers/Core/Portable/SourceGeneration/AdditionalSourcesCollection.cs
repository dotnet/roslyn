// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
namespace Microsoft.CodeAnalysis
{
    internal sealed class AdditionalSourcesCollection
    {
        private readonly ArrayBuilder<GeneratedSourceText> _sourcesAdded;
        private readonly string _fileExtension;

        private const StringComparison _hintNameComparison = StringComparison.OrdinalIgnoreCase;

        private static readonly StringComparer s_hintNameComparer = StringComparer.OrdinalIgnoreCase;

        // Matches "/" at the beginning, relative path segments ("../", "./", "//"),
        // and " /" (directories ending with space cause problems).
        private static readonly Regex s_invalidSegmentPattern = new Regex(@"(\.{1,2}|/|^| )/", RegexOptions.Compiled);

        internal AdditionalSourcesCollection(string fileExtension)
        {
            Debug.Assert(fileExtension.Length > 0 && fileExtension[0] == '.');
            _sourcesAdded = ArrayBuilder<GeneratedSourceText>.GetInstance();
            _fileExtension = fileExtension;
        }

        public void Add(string hintName, SourceText source)
        {
            if (string.IsNullOrWhiteSpace(hintName))
            {
                throw new ArgumentNullException(nameof(hintName));
            }

            // allow any identifier character or [.,-+`_ ()[]{}/\\]
            for (int i = 0; i < hintName.Length; i++)
            {
                char c = hintName[i];
                if (!UnicodeCharacterUtilities.IsIdentifierPartCharacter(c)
                    && c != '.'
                    && c != ','
                    && c != '-'
                    && c != '+'
                    && c != '`'
                    && c != '_'
                    && c != ' '
                    && c != '('
                    && c != ')'
                    && c != '['
                    && c != ']'
                    && c != '{'
                    && c != '}'
                    && c != '/'
                    && c != '\\')
                {
                    throw new ArgumentException(string.Format(CodeAnalysisResources.HintNameInvalidChar, hintName, c, i), nameof(hintName));
                }
            }

            hintName = hintName.Replace('\\', '/');

            if (s_invalidSegmentPattern.Match(hintName) is { Success: true } match)
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.HintNameInvalidSegment, hintName, match.Value, match.Index), nameof(hintName));
            }

            hintName = AppendExtensionIfRequired(hintName);
            if (this.Contains(hintName))
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.HintNameUniquePerGenerator, hintName), nameof(hintName));
            }

            if (source.Encoding is null)
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.SourceTextRequiresEncoding, hintName), nameof(source));
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

        public void CopyTo(AdditionalSourcesCollection asc)
        {
            // we know the individual hint names are valid, but we do need to check that they
            // don't collide with any we already have
            if (asc._sourcesAdded.Count == 0)
            {
                asc._sourcesAdded.AddRange(this._sourcesAdded);
            }
            else
            {
                foreach (var source in this._sourcesAdded)
                {
                    if (asc.Contains(source.HintName))
                    {
                        throw new ArgumentException(string.Format(CodeAnalysisResources.HintNameUniquePerGenerator, source.HintName), "hintName");
                    }
                    asc._sourcesAdded.Add(source);
                }
            }
        }

        internal ImmutableArray<GeneratedSourceText> ToImmutableAndFree() => _sourcesAdded.ToImmutableAndFree();

        internal ImmutableArray<GeneratedSourceText> ToImmutable() => _sourcesAdded.ToImmutable();

        internal void Free() => _sourcesAdded.Free();

        private string AppendExtensionIfRequired(string hintName)
        {
            if (!hintName.EndsWith(_fileExtension, _hintNameComparison))
            {
                hintName = string.Concat(hintName, _fileExtension);
            }

            return hintName;
        }
    }
}
