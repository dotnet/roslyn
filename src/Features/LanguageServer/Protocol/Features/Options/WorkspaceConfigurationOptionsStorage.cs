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
            CacheStorage: globalOptions.GetOption(CloudCacheFeatureFlag) ? StorageDatabase.CloudCache : globalOptions.GetOption(Database),
            EnableOpeningSourceGeneratedFiles: globalOptions.GetOption(EnableOpeningSourceGeneratedFilesInWorkspace) ??
                                               globalOptions.GetOption(EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag));

    public static readonly Option2<StorageDatabase> Database = new(
        "Storage", "Database", WorkspaceConfigurationOptions.Default.CacheStorage);

    public static readonly Option2<bool> CloudCacheFeatureFlag = new(
        "Storage", "CloudCacheFeatureFlag", WorkspaceConfigurationOptions.Default.CacheStorage == StorageDatabase.CloudCache);

    public static readonly Option2<bool> DisableCloneWhenProducingSkeletonReferences = new(
        "WorkspaceConfigurationOptions", "DisableCloneWhenProducingSkeletonReferences", WorkspaceConfigurationOptions.Default.DisableCloneWhenProducingSkeletonReferences);

    public static readonly Option2<bool> DisableReferenceManagerRecoverableMetadata = new(
        "WorkspaceConfigurationOptions", "DisableReferenceManagerRecoverableMetadata", WorkspaceConfigurationOptions.Default.DisableReferenceManagerRecoverableMetadata);

    public static readonly Option2<bool> DisableBackgroundCompilation = new(
        "WorkspaceConfigurationOptions", "DisableBackgroundCompilation", WorkspaceConfigurationOptions.Default.DisableBackgroundCompilation);

    public static readonly Option2<bool> DisableSharedSyntaxTrees = new(
        "WorkspaceConfigurationOptions", "DisableSharedSyntaxTrees", WorkspaceConfigurationOptions.Default.DisableSharedSyntaxTrees,
        new FeatureFlagStorageLocation("Roslyn.DisableSharedSyntaxTrees"));

    /// <summary>
    /// This option allows the user to enable this. We are putting this behind a feature flag for now since we could have extensions
    /// surprised by this and we want some time to work through those issues.
    /// </summary>
    public static readonly Option2<bool?> EnableOpeningSourceGeneratedFilesInWorkspace = new(
        "WorkspaceConfigurationOptions", "EnableOpeningSourceGeneratedFilesInWorkspace", defaultValue: null);

    public static readonly Option2<bool> EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag = new(
        "WorkspaceConfigurationOptions", "EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag", WorkspaceConfigurationOptions.Default.EnableOpeningSourceGeneratedFiles);
}
