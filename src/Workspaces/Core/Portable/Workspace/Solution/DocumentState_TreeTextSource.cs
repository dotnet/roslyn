// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class DocumentState
    {
        /// <summary>
        /// A source for <see cref="TextAndVersion"/> constructed from an syntax tree.
        /// </summary>
        private sealed class TreeTextSource : ValueSource<TextAndVersion>, ITextAndVersionSource, ITextVersionable
        {
            private readonly ValueSource<SourceText> _textSource;
            private readonly VersionStamp _version;
            private readonly string _filePath;

            public SourceHashAlgorithm ChecksumAlgorithm { get; }

            public TreeTextSource(ValueSource<SourceText> textSource, VersionStamp version, string filePath, SourceHashAlgorithm checksumAlgorithm)
            {
                _textSource = textSource;
                _version = version;
                _filePath = filePath;
                ChecksumAlgorithm = checksumAlgorithm;
            }

            public override async Task<TextAndVersion> GetValueAsync(CancellationToken cancellationToken = default)
            {
                var text = await _textSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return TextAndVersion.Create(text, _version, _filePath);
            }

            public override TextAndVersion GetValue(CancellationToken cancellationToken = default)
            {
                var text = _textSource.GetValue(cancellationToken);
                return TextAndVersion.Create(text, _version, _filePath);
            }

            public override bool TryGetValue([NotNullWhen(true)] out TextAndVersion? value)
            {
                if (_textSource.TryGetValue(out var text))
                {
                    value = TextAndVersion.Create(text, _version, _filePath);
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }

            public bool TryGetTextVersion(out VersionStamp version)
            {
                version = _version;
                return version != default;
            }

            public ITextAndVersionSource? TryUpdateChecksumAlgorithm(SourceHashAlgorithm algorithm)
                => null;
        }
    }
}
