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
            DisableProjectCacheService: globalOptions.GetOption(DisableProjectCacheService),
            DisableRecoverableTrees: globalOptions.GetOption(DisableRecoverableTrees),
            EnableOpeningSourceGeneratedFiles: globalOptions.GetOption(EnableOpeningSourceGeneratedFilesInWorkspace) ??
                                               globalOptions.GetOption(EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag));

    public static readonly Option2<StorageDatabase> Database = new(
        "FeatureManager/Storage", nameof(Database), WorkspaceConfigurationOptions.Default.CacheStorage,
        new LocalUserProfileStorageLocation(@"Roslyn\Internal\OnOff\Features\Database"));

    public static readonly Option2<bool> CloudCacheFeatureFlag = new(
        "FeatureManager/Storage", "CloudCacheFeatureFlag", WorkspaceConfigurationOptions.Default.CacheStorage == StorageDatabase.CloudCache,
        new FeatureFlagStorageLocation("Roslyn.CloudCache3"));

    /// <summary>
    /// Disables if the workspace creates recoverable trees when from its <see cref="ISyntaxTreeFactoryService"/>s.
    /// </summary>
    public static readonly Option2<bool> DisableRecoverableTrees = new(
        "WorkspaceConfigurationOptions", "DisableRecoverableTrees", WorkspaceConfigurationOptions.Default.DisableRecoverableTrees,
        new FeatureFlagStorageLocation("Roslyn.DisableRecoverableTrees"));

    public static readonly Option2<bool> DisableProjectCacheService = new(
        "WorkspaceConfigurationOptions", nameof(DisableProjectCacheService), WorkspaceConfigurationOptions.Default.DisableProjectCacheService,
        new FeatureFlagStorageLocation("Roslyn.DisableProjectCacheService"));

    /// <summary>
    /// This option allows the user to enable this. We are putting this behind a feature flag for now since we could have extensions
    /// surprised by this and we want some time to work through those issues.
    /// </summary>
    public static readonly Option2<bool?> EnableOpeningSourceGeneratedFilesInWorkspace = new(
        "WorkspaceConfigurationOptions", "EnableOpeningSourceGeneratedFilesInWorkspace", defaultValue: null,
        new RoamingProfileStorageLocation("TextEditor.Roslyn.Specific.EnableOpeningSourceGeneratedFilesInWorkspaceExperiment"));

    public static readonly Option2<bool> EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag = new(
        "WorkspaceConfigurationOptions", "EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag", WorkspaceConfigurationOptions.Default.EnableOpeningSourceGeneratedFiles,
        new FeatureFlagStorageLocation("Roslyn.SourceGeneratorsEnableOpeningInWorkspace"));
}
