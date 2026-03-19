// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Services provided by the host environment.
/// </summary>
public abstract class HostServices
{
    /// <summary>
    /// Creates a new workspace service. 
    /// </summary>
    protected internal abstract HostWorkspaceServices CreateWorkspaceServices(Workspace workspace);
}
