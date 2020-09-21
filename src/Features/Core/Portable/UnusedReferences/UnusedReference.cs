// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    internal class UnusedReference : Reference
    {
        /// <summary>
        /// Gets the used transitive references brought in by this reference.
        /// </summary>
        public ImmutableArray<Reference> UsedTransitiveReferences { get; }

        public UnusedReference(ReferenceType referenceType, string itemSpecification, bool treatAsUsed, ImmutableArray<Reference> usedTransitiveReferences)
            : base(referenceType, itemSpecification, treatAsUsed)
        {
            UsedTransitiveReferences = usedTransitiveReferences;
        }
    }
}
