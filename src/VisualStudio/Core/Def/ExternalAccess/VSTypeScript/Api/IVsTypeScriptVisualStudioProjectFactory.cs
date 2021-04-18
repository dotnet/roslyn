// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api
{
    internal interface IVsTypeScriptVisualStudioProjectFactory
    {
        [Obsolete("Use CreateAndAddToWorkspaceAsync instead")]
        VSTypeScriptVisualStudioProjectWrapper CreateAndAddToWorkspace(string projectSystemName, string language, string projectFilePath, IVsHierarchy hierarchy, Guid projectGuid);

        ValueTask<VSTypeScriptVisualStudioProjectWrapper> CreateAndAddToWorkspaceAsync(string projectSystemName, string language, string projectFilePath, IVsHierarchy hierarchy, Guid projectGuid, CancellationToken cancellationToken);
    }
}
