// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;

internal sealed class ProjectSystemReferenceInfo(ProjectSystemReferenceType referenceType, string itemSpecification, bool treatAsUsed)
{
    public ProjectSystemReferenceType ReferenceType { get; } = referenceType;
    public string ItemSpecification { get; } = itemSpecification;
    public bool TreatAsUsed { get; } = treatAsUsed;
}
