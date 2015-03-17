// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
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
            private readonly ValueSource<TextAndVersion> _lazyTextAndVersion;
            private readonly VersionStamp _textVersion;

            public TreeTextSource(ValueSource<TextAndVersion> textAndVersion, VersionStamp textVersion)
            {
                _lazyTextAndVersion = textAndVersion;
                _textVersion = textVersion;
            }

            public override Task<TextAndVersion> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken))
            {
                return _lazyTextAndVersion.GetValueAsync(cancellationToken);
            }

            public override TextAndVersion GetValue(CancellationToken cancellationToken = default(CancellationToken))
            {
                return _lazyTextAndVersion.GetValue(cancellationToken);
            }

            public override bool TryGetValue(out TextAndVersion value)
            {
                return _lazyTextAndVersion.TryGetValue(out value);
            }

            public bool TryGetTextVersion(out VersionStamp version)
            {
                version = _textVersion;
                return version != default(VersionStamp);
            }
        }
    }
}