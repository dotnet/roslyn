// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities
{
    internal class TestTextLoader : TextLoader
    {
        private readonly string _text;

        public TestTextLoader(string text = "test")
            => _text = text;

        public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            => Task.FromResult(TextAndVersion.Create(SourceText.From(_text), VersionStamp.Create()));
    }
}
