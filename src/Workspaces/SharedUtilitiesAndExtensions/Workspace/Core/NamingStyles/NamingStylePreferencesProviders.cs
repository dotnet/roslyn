﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal static class NamingStylePreferencesProviders
{
    public static async ValueTask<NamingStylePreferences> GetNamingStylePreferencesAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetOption(NamingStyleOptions.NamingPreferences, document.Project.Language);
    }
}
