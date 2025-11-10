// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

internal static class Constants
{
    public const string RazorLanguageName = LanguageInfoProvider.RazorLanguageName;

    // These UI contexts are provided by Razor, so must match https://github.com/dotnet/razor/blob/main/src/Razor/src/Microsoft.VisualStudio.LanguageServices.Razor/RazorConstants.cs
    public static readonly Guid RazorCohostingUIContext = new Guid("6d5b86dc-6b8a-483b-ae30-098a3c7d6774");
}
