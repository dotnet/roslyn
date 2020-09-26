// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences
{
    internal static class UnusedReferenceExtensions
    {
        public static ReferenceUpdate ToReferenceUpdate(this ProjectSystemReferenceUpdate projectSystemReferenceUpdate)
        {
            return new ReferenceUpdate(
                (UpdateAction)projectSystemReferenceUpdate.Action,
                projectSystemReferenceUpdate.ReferenceInfo.ToReferenceInfo());
        }

        public static ReferenceInfo ToReferenceInfo(this ProjectSystemReferenceInfo projectSystemReference)
        {
            return new ReferenceInfo(
                (ReferenceType)projectSystemReference.ReferenceType,
                projectSystemReference.ItemSpecification,
                projectSystemReference.TreatAsUsed);
        }

        public static ProjectSystemReferenceUpdate ToProjectSystemReferenceUpdate(this ReferenceUpdate referenceUpdate)
        {
            return new ProjectSystemReferenceUpdate(
                (ProjectSystemUpdateAction)referenceUpdate.Action,
                referenceUpdate.ReferenceInfo.ToProjectSystemReferenceInfo());
        }

        public static ProjectSystemReferenceInfo ToProjectSystemReferenceInfo(this ReferenceInfo reference)
        {
            return new ProjectSystemReferenceInfo(
                (ProjectSystemReferenceType)reference.ReferenceType,
                reference.ItemSpecification,
                reference.TreatAsUsed);
        }
    }
}
