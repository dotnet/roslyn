// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServer;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

internal static class RazorUri
{
    [Obsolete("Use RazorUri.GetUriFromFilePath instead")]
    public static Uri CreateAbsoluteUri(string absolutePath)
        => ProtocolConversions.CreateAbsoluteUri(absolutePath);

    public static DocumentUri CreateAbsoluteDocumentUri(string absolutePath)
        => ProtocolConversions.CreateAbsoluteDocumentUri(absolutePath);

    public static string GetDocumentFilePathFromUri(Uri uri)
        => ProtocolConversions.GetDocumentFilePathFromUri(uri);
}
