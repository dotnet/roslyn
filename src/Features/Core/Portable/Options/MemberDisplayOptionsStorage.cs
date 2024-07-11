// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Options customizing member display. Used by multiple features.
/// </summary>
internal static class MemberDisplayOptionsStorage
{
    public static readonly OptionGroup Group = new(name: "member_display", description: "");

    public static readonly PerLanguageOption2<bool> HideAdvancedMembers = new(
        "dotnet_hide_advanced_members",
        defaultValue: false,
        isEditorConfigOption: true,
        group: Group);

    /// <summary>
    /// Options that we expect the user to set in editorconfig.
    /// </summary>
    public static readonly ImmutableArray<IOption2> EditorConfigOptions = [HideAdvancedMembers];
}
