// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

internal class ReinvocationResponse<TResponseType>
{
    public ReinvocationResponse(string languageClientName, TResponseType? response)
    {
        LanguageClientName = languageClientName;
        Response = response;
    }

    public string LanguageClientName { get; }

    public TResponseType? Response { get; }
}
