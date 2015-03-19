// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class DocumentState
    {
        /// <summary>
        /// A source for TextAndVersion constructed from an syntax tree
        /// </summary>
        private class TreeTextSource : ValueSource<TextAndVersion>, ITextVersionable
        {
            private readonly ValueSource<SourceText> _lazyText;
            private readonly VersionStamp _version;
            private readonly string _filePath;

            public TreeTextSource(ValueSource<SourceText> text, VersionStamp version, string filePath)
            {
                _lazyText = text;
                _version = version;
                _filePath = filePath;
            }

            public override async Task<TextAndVersion> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken))
            {
                var text = await _lazyText.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return TextAndVersion.Create(text, _version, _filePath);
            }

            public override TextAndVersion GetValue(CancellationToken cancellationToken = default(CancellationToken))
            {
                var text = _lazyText.GetValue(cancellationToken);
                return TextAndVersion.Create(text, _version, _filePath);
            }

            public override bool TryGetValue(out TextAndVersion value)
            {
                SourceText text;
                if (_lazyText.TryGetValue(out text))
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
                return version != default(VersionStamp);
            }
        }
    }
}