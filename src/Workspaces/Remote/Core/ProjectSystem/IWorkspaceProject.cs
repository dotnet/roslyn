// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote.ProjectSystem;

[RpcMarshalable]
internal interface IWorkspaceProject : IDisposable
{
    Task SetDisplayNameAsync(string displayName, CancellationToken cancellationToken);

    Task SetCommandLineArgumentsAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken);
    Task SetBuildSystemPropertiesAsync(IReadOnlyDictionary<string, string> properties, CancellationToken cancellationToken);

    Task AddSourceFilesAsync(IReadOnlyList<SourceFileInfo> sourceFiles, CancellationToken cancellationToken);
    Task RemoveSourceFilesAsync(IReadOnlyList<string> sourceFiles, CancellationToken cancellationToken);

    Task AddMetadataReferencesAsync(IReadOnlyList<MetadataReferenceInfo> metadataReferences, CancellationToken cancellationToken);
    Task RemoveMetadataReferencesAsync(IReadOnlyList<MetadataReferenceInfo> metadataReferences, CancellationToken cancellationToken);

    [Obsolete($"Call the {nameof(AddAdditionalFilesAsync)} overload that takes {nameof(SourceFileInfo)}.")]
    Task AddAdditionalFilesAsync(IReadOnlyList<string> additionalFilePaths, CancellationToken cancellationToken);
    Task AddAdditionalFilesAsync(IReadOnlyList<SourceFileInfo> additionalFiles, CancellationToken cancellationToken);
    Task RemoveAdditionalFilesAsync(IReadOnlyList<string> additionalFilePaths, CancellationToken cancellationToken);

    Task AddAnalyzerReferencesAsync(IReadOnlyList<string> analyzerPaths, CancellationToken cancellationToken);
    Task RemoveAnalyzerReferencesAsync(IReadOnlyList<string> analyzerPaths, CancellationToken cancellationToken);

    Task AddAnalyzerConfigFilesAsync(IReadOnlyList<string> analyzerConfigPaths, CancellationToken cancellationToken);
    Task RemoveAnalyzerConfigFilesAsync(IReadOnlyList<string> analyzerConfigPaths, CancellationToken cancellationToken);

    Task AddDynamicFilesAsync(IReadOnlyList<string> dynamicFilePaths, CancellationToken cancellationToken);
    Task RemoveDynamicFilesAsync(IReadOnlyList<string> dynamicFilePaths, CancellationToken cancellationToken);

    Task SetProjectHasAllInformationAsync(bool hasAllInformation, CancellationToken cancellationToken);

    Task<IWorkspaceProjectBatch> StartBatchAsync(CancellationToken cancellationToken);
}
