// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Host;

internal static class WorkspaceConfigurationOptionsStorage
{
    public static WorkspaceConfigurationOptions GetWorkspaceConfigurationOptions(this IGlobalOptionService globalOptions)
        => new(
            CacheStorage: globalOptions.GetOption(Database),
            SourceGeneratorExecution:
                globalOptions.GetOption(SourceGeneratorExecution) ??
                (globalOptions.GetOption(SourceGeneratorExecutionBalancedFeatureFlag) ? SourceGeneratorExecutionPreference.Balanced : SourceGeneratorExecutionPreference.Automatic),
            ValidateCompilationTrackerStates: globalOptions.GetOption(ValidateCompilationTrackerStates));

    public static readonly Option2<StorageDatabase> Database = new(
        "dotnet_storage_database", WorkspaceConfigurationOptions.Default.CacheStorage, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<StorageDatabase>());

    public static readonly Option2<bool> ValidateCompilationTrackerStates = new(
        "dotnet_validate_compilation_tracker_states", WorkspaceConfigurationOptions.Default.ValidateCompilationTrackerStates);

    public static readonly Option2<SourceGeneratorExecutionPreference?> SourceGeneratorExecution = new(
        "dotnet_source_generator_execution",
        defaultValue: null,
        isEditorConfigOption: true,
        serializer: new EditorConfigValueSerializer<SourceGeneratorExecutionPreference?>(
            s => SourceGeneratorExecutionPreferenceUtilities.Parse(s),
            SourceGeneratorExecutionPreferenceUtilities.GetEditorConfigString));

    public static readonly Option2<bool> SourceGeneratorExecutionBalancedFeatureFlag = new(
        "dotnet_source_generator_execution_balanced_feature_flag", true);
}
