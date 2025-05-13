// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities;

internal sealed class TestTextLoader : TextLoader
{
    private readonly TextAndVersion _textAndVersion;

    public TestTextLoader(string text = "test", SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithms.Default)
    {
        _textAndVersion = TextAndVersion.Create(SourceText.From(text, encoding: null, checksumAlgorithm), VersionStamp.Create());
    }

    public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        => Task.FromResult(_textAndVersion);
}
