// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Options;

[ExportVisualStudioStorageReadFallback(NamingStyleOptions.NamingPreferencesOptionName), Shared]
internal sealed class NamingPreferencesReadFallback : IVisualStudioStorageReadFallback
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public NamingPreferencesReadFallback()
    {
    }

    public Optional<object?> TryRead(string? language, TryReadValueDelegate readValue)
    {
        Contract.ThrowIfNull(language);
        return readValue($"TextEditor.{language}.Specific.NamingPreferences", typeof(NamingStylePreferences), NamingStyleOptions.NamingPreferences.DefaultValue);
    }
}
