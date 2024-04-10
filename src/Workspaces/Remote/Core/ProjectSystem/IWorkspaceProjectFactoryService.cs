// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote.ProjectSystem;

internal interface IWorkspaceProjectFactoryService
{
    Task<IWorkspaceProject> CreateAndAddProjectAsync(WorkspaceProjectCreationInfo creationInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the list of properties that are understood by the language service and can be passed to
    /// <see cref="IWorkspaceProject.SetBuildSystemPropertiesAsync(IReadOnlyDictionary{string, string}, CancellationToken)"/> and to
    /// <see cref="WorkspaceProjectCreationInfo.BuildSystemProperties"/>.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetSupportedBuildSystemPropertiesAsync(CancellationToken cancellationToken);
}
