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
        /// then returns the corresponding <see cref="Suppression"/>s, in no specific order.
        /// Otherwise, returns an empty array.
        /// </summary>
        public ImmutableArray<Suppression> ProgrammaticSuppressions { get; }

        internal SuppressionInfo(string id, AttributeData? attribute, ImmutableArray<Suppression> programmaticSuppressions)
        {
            Debug.Assert(programmaticSuppressions.All(suppression => id == suppression.SuppressedDiagnostic.Id));
            Id = id;
            Attribute = attribute;
            ProgrammaticSuppressions = programmaticSuppressions;
        }
    }
}
