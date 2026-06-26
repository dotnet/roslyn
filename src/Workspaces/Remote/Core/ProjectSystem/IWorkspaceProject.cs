// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote.ProjectSystem;

// We aren't actually running in an AOT process, but we want to be compatible with other libraries that are
// configured for AOT. Thus suppressing the warnings for now.
#pragma warning disable StreamJsonRpc0002 // Declare partial interface
#pragma warning disable StreamJsonRpc0008 // Add methods to PolyType shape for RPC contract interface

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

    [Obsolete($"Dynamic files are ignored; callers can remove calls to this method.")]
    Task AddDynamicFilesAsync(IReadOnlyList<string> dynamicFilePaths, CancellationToken cancellationToken);

    [Obsolete($"Dynamic files are ignored; callers can remove calls to this method.")]
    Task RemoveDynamicFilesAsync(IReadOnlyList<string> dynamicFilePaths, CancellationToken cancellationToken);

    Task SetProjectHasAllInformationAsync(bool hasAllInformation, CancellationToken cancellationToken);

    Task<IWorkspaceProjectBatch> StartBatchAsync(CancellationToken cancellationToken);
}

#pragma warning restore StreamJsonRpc0008 // Add methods to PolyType shape for RPC contract interface
#pragma warning restore StreamJsonRpc0002 // Declare partial interface
