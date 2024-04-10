// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.InlineRename;

internal sealed class InlineRenameUIOptionsStorage
{
    public static readonly Option2<bool> UseInlineAdornment = new("dotnet_rename_use_inline_adornment", defaultValue: true);
    public static readonly Option2<bool> CollapseUI = new("dotnet_collapse_inline_rename_ui", defaultValue: false);
    public static readonly Option2<bool> CollapseSuggestionsPanel = new("dotnet_collapse_suggestions_in_inline_rename_ui", defaultValue: false);
    public static readonly Option2<bool> GetSuggestionsAutomatically = new("dotnet_rename_get_suggestions_automatically", defaultValue: false);
}
