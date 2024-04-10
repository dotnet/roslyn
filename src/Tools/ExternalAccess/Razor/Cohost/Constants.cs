// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

internal static class Constants
{
    public const string RazorLSPContentType = "Razor";

    public const string RazorLanguageContract = ProtocolConstants.RazorCohostContract;

    public static readonly ImmutableArray<string> RazorLanguage = ImmutableArray.Create("Razor");
}
