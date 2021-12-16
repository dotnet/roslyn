// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
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

        public void AddRange(ImmutableArray<GeneratedSourceText> texts) => _sourcesAdded.AddRange(texts);

        public void AddRange(ImmutableArray<GeneratedSyntaxTree> trees) => _sourcesAdded.AddRange(trees.SelectAsArray(t => new GeneratedSourceText(t.HintName, t.Text)));

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
