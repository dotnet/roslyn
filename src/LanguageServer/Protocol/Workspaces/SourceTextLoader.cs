// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class SourceTextLoader : TextLoader
{
    private readonly SourceText _sourceText;
    private readonly string _fileUri;

    public SourceTextLoader(SourceText sourceText, string fileUri)
    {
        _sourceText = sourceText;
        _fileUri = fileUri;
    }

    internal override string? FilePath
        => _fileUri;

    // TODO (https://github.com/dotnet/roslyn/issues/63583): Use options.ChecksumAlgorithm 
    public override async Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        => TextAndVersion.Create(_sourceText, VersionStamp.Create(), _fileUri);
}
