// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client;

internal static class LanguageServicesUtils
{
    private const string LanguageServerProviderServiceName = "languageServerProvider";

    public static string GetLanguageServerProviderServiceName(string[] contentTypes)
    {
        Requires.NotNullOrEmpty(contentTypes, nameof(contentTypes));
        return GetLanguageServerProviderServiceName(GetContentTypesName(contentTypes));
    }

    public static string GetLanguageServerProviderServiceName(string lspServiceName)
        => LanguageServerProviderServiceName + "-" + lspServiceName;

    public static string GetContentTypesName(string[] contentTypes) => string.Join("-", [.. contentTypes.OrderBy(c => c)]);

    public static bool IsContentTypeRemote(string contentType)
        => contentType.EndsWith("-remote");
}
