// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis;

[DataContract]
internal readonly record struct MemberDisplayOptions()
{
    public static readonly MemberDisplayOptions Default = new();

    [DataMember]
    public bool HideAdvancedMembers { get; init; } = false;
}

/// <summary>
/// Options customizing member display. Used by multiple features.
/// </summary>
internal static class MemberDisplayOptionsStorage
{
    public static readonly OptionGroup TypeMemberGroup = new(name: "type_members", description: WorkspaceExtensionsResources.Type_members, priority: 3, parent: null);

    public static readonly PerLanguageOption2<bool> HideAdvancedMembers = new(
        "dotnet_hide_advanced_members",
        MemberDisplayOptions.Default.HideAdvancedMembers,
        isEditorConfigOption: true,
        group: TypeMemberGroup);

    /// <summary>
    /// Options that we expect the user to set in editorconfig.
    /// </summary>
    public static readonly ImmutableArray<IOption2> EditorConfigOptions = [HideAdvancedMembers];
}

internal static class MemberDisplayOptionsProviders
{
    public static MemberDisplayOptions GetMemberDisplayOptions(this IOptionsReader reader, string language)
        => new()
        {
            HideAdvancedMembers = reader.GetOption(MemberDisplayOptionsStorage.HideAdvancedMembers, language)
        };

    public static async ValueTask<MemberDisplayOptions> GetMemberDisplayOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetMemberDisplayOptions(document.Project.Language);
    }
}
