// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Host;

internal static class WorkspaceConfigurationOptionsStorage
{
    public static WorkspaceConfigurationOptions GetWorkspaceConfigurationOptions(this IGlobalOptionService globalOptions)
        => new(
            CacheStorage: globalOptions.GetOption(CloudCacheFeatureFlag) ? StorageDatabase.CloudCache : globalOptions.GetOption(Database),
            EnableOpeningSourceGeneratedFiles: globalOptions.GetOption(EnableOpeningSourceGeneratedFilesInWorkspace),
            DisableSharedSyntaxTrees: globalOptions.GetOption(DisableSharedSyntaxTrees),
            DisableRecoverableText: globalOptions.GetOption(DisableRecoverableText),
            ValidateCompilationTrackerStates: globalOptions.GetOption(ValidateCompilationTrackerStates),
            RunSourceGeneratorsInSameProcessOnly: globalOptions.GetOption(RunSourceGeneratorsInSameProcessOnly));

    public static readonly Option2<StorageDatabase> Database = new(
        "dotnet_storage_database", WorkspaceConfigurationOptions.Default.CacheStorage, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<StorageDatabase>());

    public static readonly Option2<bool> CloudCacheFeatureFlag = new(
        "dotnet_storage_cloud_cache", WorkspaceConfigurationOptions.Default.CacheStorage == StorageDatabase.CloudCache);

    public static readonly Option2<bool> DisableSharedSyntaxTrees = new(
        "dotnet_disable_shared_syntax_trees", WorkspaceConfigurationOptions.Default.DisableSharedSyntaxTrees);

    public static readonly Option2<bool> DisableRecoverableText = new(
        "dotnet_disable_recoverable_text", WorkspaceConfigurationOptions.Default.DisableRecoverableText);

    public static readonly Option2<bool> ValidateCompilationTrackerStates = new(
        "dotnet_validate_compilation_tracker_states", WorkspaceConfigurationOptions.Default.ValidateCompilationTrackerStates);

    public static readonly Option2<bool> RunSourceGeneratorsInSameProcessOnly = new(
        "dotnet_run_source_generators_in_same_process_only", WorkspaceConfigurationOptions.Default.RunSourceGeneratorsInSameProcessOnly);

    /// <summary>
    /// This option allows the user to disable this, in case they have some extension that breaks with this on. The expectation is to remove
    /// this in 17.9 or so.
    /// </summary>
    public static readonly Option2<bool> EnableOpeningSourceGeneratedFilesInWorkspace = new(
        "dotnet_enable_opening_source_generated_files_in_workspace", defaultValue: true);
}
