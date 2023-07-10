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
            EnableOpeningSourceGeneratedFiles: globalOptions.GetOption(EnableOpeningSourceGeneratedFilesInWorkspace) ??
                                               globalOptions.GetOption(EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag),
            DisableSharedSyntaxTrees: globalOptions.GetOption(DisableSharedSyntaxTrees),
            DeferCreatingRecoverableText: globalOptions.GetOption(DeferCreatingRecoverableText),
            DisableRecoverableText: globalOptions.GetOption(DisableRecoverableText));

    public static readonly Option2<StorageDatabase> Database = new(
        "dotnet_storage_database", WorkspaceConfigurationOptions.Default.CacheStorage, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<StorageDatabase>());

    public static readonly Option2<bool> CloudCacheFeatureFlag = new(
        "dotnet_storage_cloud_cache", WorkspaceConfigurationOptions.Default.CacheStorage == StorageDatabase.CloudCache);

    public static readonly Option2<bool> DisableSharedSyntaxTrees = new(
        "dotnet_disable_shared_syntax_trees", WorkspaceConfigurationOptions.Default.DisableSharedSyntaxTrees);

    public static readonly Option2<bool> DeferCreatingRecoverableText = new(
        "dotnet_defer_creating_recoverable_text", WorkspaceConfigurationOptions.Default.DeferCreatingRecoverableText);

    public static readonly Option2<bool> DisableRecoverableText = new(
        "dotnet_disable_recoverable_text", WorkspaceConfigurationOptions.Default.DisableRecoverableText);

    /// <summary>
    /// This option allows the user to enable this. We are putting this behind a feature flag for now since we could have extensions
    /// surprised by this and we want some time to work through those issues.
    /// </summary>
    public static readonly Option2<bool?> EnableOpeningSourceGeneratedFilesInWorkspace = new(
        "dotnet_enable_opening_source_generated_files_in_workspace", defaultValue: null);

    public static readonly Option2<bool> EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag = new(
        "dotnet_enable_opening_source_generated_files_in_workspace_feature_flag", WorkspaceConfigurationOptions.Default.EnableOpeningSourceGeneratedFiles);
}
