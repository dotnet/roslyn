// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal class CohostEndpointAttribute : LanguageServerEndpointAttribute
{
    public CohostEndpointAttribute(string method)
        : base(method, LanguageInfoProvider.RazorLanguageName)
    {
    }
}
