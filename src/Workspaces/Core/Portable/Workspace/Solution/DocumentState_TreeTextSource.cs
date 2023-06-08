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
        private sealed class TreeTextSource : ITextAndVersionSource
        {
            private readonly AsyncLazy<SourceText> _textSource;
            private readonly VersionStamp _version;

            public bool CanReloadText
                => false;

            public TreeTextSource(AsyncLazy<SourceText> textSource, VersionStamp version)
            {
                _textSource = textSource;
                _version = version;
            }

            public async Task<TextAndVersion> GetValueAsync(LoadTextOptions options, CancellationToken cancellationToken)
            {
                var text = await _textSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return TextAndVersion.Create(text, _version);
            }

            public TextAndVersion GetValue(LoadTextOptions options, CancellationToken cancellationToken)
            {
                var text = _textSource.GetValue(cancellationToken);
                return TextAndVersion.Create(text, _version);
            }

            public bool TryGetValue(LoadTextOptions options, [NotNullWhen(true)] out TextAndVersion? value)
            {
                if (_textSource.TryGetValue(out var text))
                {
                    value = TextAndVersion.Create(text, _version);
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }

            public bool TryGetVersion(LoadTextOptions options, out VersionStamp version)
            {
                version = _version;
                return version != default;
            }

            public ValueTask<VersionStamp> GetVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
                => new(_version);
        }
    }
}
