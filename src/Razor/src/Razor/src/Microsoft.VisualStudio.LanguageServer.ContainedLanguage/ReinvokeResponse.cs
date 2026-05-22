// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.LanguageServer.Client;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

internal struct ReinvokeResponse<TOut>
{
    public ILanguageClient LanguageClient { get; }

    public TOut Result { get; }

    public bool IsSuccess => LanguageClient != default;

    public ReinvokeResponse(
        ILanguageClient languageClient,
        TOut result)
    {
        LanguageClient = languageClient;
        Result = result;
    }
}
