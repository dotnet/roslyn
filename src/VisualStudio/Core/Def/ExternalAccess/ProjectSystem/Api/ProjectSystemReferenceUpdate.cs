// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

internal sealed class ProjectSystemReferenceUpdate
{
    /// <summary>
    /// Indicates action to perform on the reference.
    /// </summary>
    public ProjectSystemUpdateAction Action { get; }

    /// <summary>
    /// Gets the reference to be updated.
    /// </summary>
    public ProjectSystemReferenceInfo ReferenceInfo { get; }

    public ProjectSystemReferenceUpdate(ProjectSystemUpdateAction action, ProjectSystemReferenceInfo referenceInfo)
    {
        Action = action;
        ReferenceInfo = referenceInfo;
    }
}
