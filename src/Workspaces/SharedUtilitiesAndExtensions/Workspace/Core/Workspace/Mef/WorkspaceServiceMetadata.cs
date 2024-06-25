// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Host.Mef;

/// <summary>
/// MEF metadata class used for finding <see cref="IWorkspaceService"/> and <see cref="IWorkspaceServiceFactory"/> exports.
/// </summary>
internal sealed class WorkspaceServiceMetadata(IDictionary<string, object> data) : ILayeredServiceMetadata
{
    public string ServiceType { get; } = (string)data[nameof(ExportWorkspaceServiceAttribute.ServiceType)];
    public string Layer { get; } = (string)data[nameof(ExportWorkspaceServiceAttribute.Layer)];

    public WorkspaceKinds WorkspaceKinds { get; } = (WorkspaceKinds)data[
#if CODE_STYLE
            "WorkspaceKinds"
#else
            nameof(ExportLanguageServiceAttribute.WorkspaceKinds)
#endif
    ];
}
