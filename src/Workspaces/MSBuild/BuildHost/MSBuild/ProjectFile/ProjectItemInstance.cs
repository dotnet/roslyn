// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class ProjectItemInstance(
    MSB.Execution.ProjectItemInstance? projectItemInstance) :
#if NETFRAMEWORK
    MarshalByRefObject, // We need this object to pass across the AppDomain boundary when on .NET Framework
#endif
    IProjectItemInstance
{
    public string GetMetadataValue(string name)
    {
        if (projectItemInstance is null)
        {
            return string.Empty;
        }

        return projectItemInstance.GetMetadataValue(name);
    }
}
