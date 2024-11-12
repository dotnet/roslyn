// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

internal enum ProjectSystemUpdateAction
{
    /// <summary>
    /// Indicates the reference should be updated with `TreatAsUsed="true"`
    /// </summary>
    SetTreatAsUsed,

    /// <summary>
    /// Indicates the reference should be updated with `TreatAsUsed="false"`
    /// </summary>
    UnsetTreatAsUsed,

    /// <summary>
    /// Indicates the reference should be removed from the project.
    /// </summary>
    Remove,
}
