// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal static class NamingStyleOptionsStorage
{
    public static NamingStylePreferences GetNamingStylePreferences(this IGlobalOptionService globalOptions, string language)
        => globalOptions.GetOption(NamingStyleOptions.NamingPreferences, language);

    public static NamingStylePreferencesProvider GetNamingStylePreferencesProvider(this IGlobalOptionService globalOptions)
        => languageServices => globalOptions.GetNamingStylePreferences(languageServices.Language);
}
