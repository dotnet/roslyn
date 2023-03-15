// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

/// <summary>
/// Formatting options for Razor design-time documents.
/// </summary>
internal static class RazorLineFormattingOptionsStorage
{
    internal static readonly Option2<bool> UseTabs = new(
        "RazorDesignTimeDocumentFormattingOptions_UseTabs", LineFormattingOptions.Default.UseTabs);

    internal static readonly Option2<int> TabSize = new(
        "RazorDesignTimeDocumentFormattingOptions_TabSize", LineFormattingOptions.Default.TabSize);
}
