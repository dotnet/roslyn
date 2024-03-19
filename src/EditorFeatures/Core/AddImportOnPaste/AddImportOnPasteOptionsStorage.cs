// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.AddImportOnPaste;

internal static class AddImportOnPasteOptionsStorage
{
    /// <summary>
    /// This option was previously "bool?" to accomodate different supported defaults
    /// that were being provided via remote settings. The feature has stabalized and moved
    /// to on by default, so the storage location was changed to
    /// TextEditor.%LANGUAGE%.Specific.AddImportsOnPaste2 (note the 2 suffix).
    /// </summary>
    public static readonly PerLanguageOption2<bool> AddImportsOnPaste = new("dotnet_add_imports_on_paste", defaultValue: true);
}
