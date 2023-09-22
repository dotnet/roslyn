// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Contains information about the source of a programmatic diagnostic suppression produced by an <see cref="DiagnosticSuppressor"/>.
    /// </summary>
    internal sealed class ProgrammaticSuppressionInfo : IEquatable<ProgrammaticSuppressionInfo?>
    {
        public ImmutableHashSet<(string Id, LocalizableString Justification)> Suppressions { get; }

        internal ProgrammaticSuppressionInfo(ImmutableHashSet<(string Id, LocalizableString Justification)> suppressions)
        {
            Suppressions = suppressions;
        }

        public bool Equals(ProgrammaticSuppressionInfo? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                this.Suppressions.SetEqualsWithoutIntermediateHashSet(other.Suppressions);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ProgrammaticSuppressionInfo);
        }

        public override int GetHashCode()
        {
            return Suppressions.Count;
        }
    }
}
