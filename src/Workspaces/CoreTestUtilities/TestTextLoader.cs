// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities
{
    internal class TestTextLoader : TextLoader
    {
        private readonly string _text;

        internal override SourceHashAlgorithm ChecksumAlgorithm { get; }

        public TestTextLoader(string text = "test", SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithms.Default)
        {
            _text = text;
            ChecksumAlgorithm = checksumAlgorithm;
        }

        public override Task<TextAndVersion> LoadTextAndVersionAsync(CancellationToken cancellationToken)
            => Task.FromResult(TextAndVersion.Create(SourceText.From(_text, encoding: null, ChecksumAlgorithm), VersionStamp.Create()));
    }
}
