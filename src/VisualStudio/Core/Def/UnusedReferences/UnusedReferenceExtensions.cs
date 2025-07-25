// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences;

internal static class UnusedReferenceExtensions
{
    extension(ProjectSystemReferenceInfo projectSystemReference)
    {
        public ReferenceInfo ToReferenceInfo()
        {
            return new ReferenceInfo(
                (ReferenceType)projectSystemReference.ReferenceType,
                projectSystemReference.ItemSpecification,
                projectSystemReference.TreatAsUsed,
                [],
                []);
        }
    }

    extension(ReferenceUpdate referenceUpdate)
    {
        public ProjectSystemReferenceUpdate ToProjectSystemReferenceUpdate()
        {
            var updateAction = referenceUpdate.Action switch
            {
                UpdateAction.TreatAsUsed => ProjectSystemUpdateAction.SetTreatAsUsed,
                UpdateAction.TreatAsUnused => ProjectSystemUpdateAction.UnsetTreatAsUsed,
                UpdateAction.Remove => ProjectSystemUpdateAction.Remove,
                _ => throw ExceptionUtilities.Unreachable()
            };
            return new ProjectSystemReferenceUpdate(
                updateAction,
                referenceUpdate.ReferenceInfo.ToProjectSystemReferenceInfo());
        }
    }

    extension(ReferenceInfo reference)
    {
        public ProjectSystemReferenceInfo ToProjectSystemReferenceInfo()
        {
            return new ProjectSystemReferenceInfo(
                (ProjectSystemReferenceType)reference.ReferenceType,
                reference.ItemSpecification,
                reference.TreatAsUsed);
        }
    }
}
