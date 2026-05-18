// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal class CohostEndpointAttribute : RazorMethodAttribute
{
    public CohostEndpointAttribute(string method)
        : base(method, Constants.RazorLanguageName)
    {
    }
}
