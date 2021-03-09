// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api
{
    internal sealed class ProjectSystemReferenceInfo
    {
        /// <summary>
        /// Indicates the type of reference.
        /// </summary>
        public ProjectSystemReferenceType ReferenceType { get; }

        /// <summary>
        /// Uniquely identifies the reference.
        /// </summary>
        /// <remarks>
        /// Should match the Include or Name attribute used in the project file.
        /// </remarks>
        public string ItemSpecification { get; }

        /// <summary>
        /// Indicates that this reference should be treated as if it were used.
        /// </summary>
        public bool TreatAsUsed { get; }

        public ProjectSystemReferenceInfo(ProjectSystemReferenceType referenceType, string itemSpecification, bool treatAsUsed)
        {
            ReferenceType = referenceType;
            ItemSpecification = itemSpecification;
            TreatAsUsed = treatAsUsed;
        }
    }
}
