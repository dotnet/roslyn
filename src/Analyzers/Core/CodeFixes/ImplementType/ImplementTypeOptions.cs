// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementType;

[DataContract]
internal readonly record struct ImplementTypeOptions()
{
    [DataMember]
    public ImplementTypeInsertionBehavior InsertionBehavior { get; init; } = ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind;

    [DataMember]
    public ImplementTypePropertyGenerationBehavior PropertyGenerationBehavior { get; init; } = ImplementTypePropertyGenerationBehavior.PreferThrowingProperties;

    public static readonly ImplementTypeOptions Default = new();
}

internal static class ImplementTypeOptionsStorage
{
    public static readonly PerLanguageOption2<ImplementTypeInsertionBehavior> InsertionBehavior = new(
        "dotnet_member_insertion_location",
        defaultValue: ImplementTypeOptions.Default.InsertionBehavior,
        group: MemberDisplayOptionsStorage.TypeMemberGroup,
        isEditorConfigOption: true,
        serializer: EditorConfigValueSerializer.CreateSerializerForEnum(
            entries:
            [
                ("at_the_end", ImplementTypeInsertionBehavior.AtTheEnd),
                ("with_other_members_of_the_same_kind", ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind),
            ],
            alternativeEntries:
            [
                ("AtTheEnd", ImplementTypeInsertionBehavior.AtTheEnd),
                ("WithOtherMembersOfTheSameKind", ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind),
            ]));

    public static readonly PerLanguageOption2<ImplementTypePropertyGenerationBehavior> PropertyGenerationBehavior = new(
        "dotnet_property_generation_behavior",
        defaultValue: ImplementTypeOptions.Default.PropertyGenerationBehavior,
        group: MemberDisplayOptionsStorage.TypeMemberGroup,
        isEditorConfigOption: true,
        serializer: EditorConfigValueSerializer.CreateSerializerForEnum(
            entries:
            [
                ("prefer_throwing_properties", ImplementTypePropertyGenerationBehavior.PreferThrowingProperties),
                ("prefer_auto_properties", ImplementTypePropertyGenerationBehavior.PreferAutoProperties),
            ],
            alternativeEntries:
            [
                ("PreferThrowingProperties", ImplementTypePropertyGenerationBehavior.PreferThrowingProperties),
                ("PreferAutoProperties", ImplementTypePropertyGenerationBehavior.PreferAutoProperties),
            ]));

    /// <summary>
    /// Options that we expect the user to set in editorconfig.
    /// </summary>
    public static readonly ImmutableArray<IOption2> EditorConfigOptions = [InsertionBehavior, PropertyGenerationBehavior];
}

internal static class ImplementTypeOptionsProviders
{
    public static ImplementTypeOptions GetImplementTypeOptions(this IOptionsReader reader, string language)
        => new()
        {
            InsertionBehavior = reader.GetOption(ImplementTypeOptionsStorage.InsertionBehavior, language),
            PropertyGenerationBehavior = reader.GetOption(ImplementTypeOptionsStorage.PropertyGenerationBehavior, language)
        };

    public static async ValueTask<ImplementTypeOptions> GetImplementTypeOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetImplementTypeOptions(document.Project.Language);
    }
}
