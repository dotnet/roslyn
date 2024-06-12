// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

internal interface IWorkspaceConfigurationService : IWorkspaceService
{
    WorkspaceConfigurationOptions Options { get; }
}

[ExportWorkspaceService(typeof(IWorkspaceConfigurationService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultWorkspaceConfigurationService() : IWorkspaceConfigurationService
{
    public WorkspaceConfigurationOptions Options => WorkspaceConfigurationOptions.Default;
}

/// <summary>
/// Options that affect behavior of workspace core APIs (<see cref="Solution"/>, <see cref="Project"/>, <see
/// cref="Document"/>, <see cref="SyntaxTree"/>, etc.) to which it would be impractical to flow these options
/// explicitly. The options are instead provided by <see cref="IWorkspaceConfigurationService"/>. The remote
/// instance of this service is initialized based on the in-proc values (which themselves are loaded from global
/// options) when we establish connection from devenv to ServiceHub process. If another process connects to our
/// ServiceHub process before that the remote instance provides a predefined set of options <see
/// cref="RemoteDefault"/> that can later be updated when devenv connects to the ServiceHub process.
/// </summary>
[DataContract]
internal readonly record struct WorkspaceConfigurationOptions(
    [property: DataMember(Order = 0)] bool EnableOpeningSourceGeneratedFiles = false,
    [property: DataMember(Order = 1)] SourceGeneratorExecutionPreference SourceGeneratorExecution = SourceGeneratorExecutionPreference.Automatic,
    [property: DataMember(Order = 2)] bool ValidateCompilationTrackerStates =
#if DEBUG // We will default this on in DEBUG builds
        true
#else
        false
#endif
    )
{
    public static readonly WorkspaceConfigurationOptions Default = new();

    /// <summary>
    /// These values are such that the correctness of remote services is not affected if these options are changed from defaults
    /// to non-defaults while the services have already been executing.
    /// </summary>
    public static readonly WorkspaceConfigurationOptions RemoteDefault = new(
        EnableOpeningSourceGeneratedFiles: false);
}
