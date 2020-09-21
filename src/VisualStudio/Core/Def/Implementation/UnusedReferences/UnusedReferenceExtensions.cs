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
                projectSystemReferenceUpdate.Reference.ToReference());
        }

        public static Reference ToReference(this ProjectSystemReference projectSystemReference)
        {
            return new Reference(
                (ReferenceType)projectSystemReference.ReferenceType,
                projectSystemReference.ItemSpecification,
                projectSystemReference.TreatAsUsed);
        }

        public static ProjectSystemReferenceUpdate ToProjectSystemReferenceUpdate(this ReferenceUpdate referenceUpdate)
        {
            return new ProjectSystemReferenceUpdate(
                (ProjectSystemUpdateAction)referenceUpdate.Action,
                referenceUpdate.Reference.ToProjectSystemReference());
        }

        public static ProjectSystemReference ToProjectSystemReference(this Reference reference)
        {
            return new ProjectSystemReference(
                (ProjectSystemReferenceType)reference.ReferenceType,
                reference.ItemSpecification,
                reference.TreatAsUsed);
        }
    }
}
