// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Contains information about the source of diagnostic suppression.
    /// </summary>
    public sealed class SuppressionInfo
    {
        /// <summary>
        /// <see cref="Diagnostic.Id"/> of the suppressed diagnostic.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// If the diagnostic was suppressed by an attribute, then returns that attribute.
        /// Otherwise, returns null.
        /// </summary>
        public AttributeData? Attribute { get; }

        /// <summary>
        /// If the diagnostic was suppressed by one or more programmatic suppressions by <see cref="DiagnosticSuppressor"/>(s),
        /// then returns the corresponding set of <see cref="Suppression"/>s.
        /// Otherwise, returns an empty set.
        /// </summary>
        public ImmutableHashSet<Suppression> Suppressions { get; }

        internal SuppressionInfo(string id, AttributeData? attribute, ImmutableHashSet<Suppression> suppressions)
        {
            Debug.Assert(suppressions.All(suppression => id == suppression.SuppressedDiagnostic.Id));
            Id = id;
            Attribute = attribute;
            Suppressions = suppressions;
        }
    }
}
