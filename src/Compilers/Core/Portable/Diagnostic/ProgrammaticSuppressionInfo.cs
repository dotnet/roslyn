// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Contains information about the source of a programmatic diagnostic suppression produced by an <see cref="DiagnosticSuppressor"/>.
    /// </summary>
    internal sealed class ProgrammaticSuppressionInfo : IEquatable<ProgrammaticSuppressionInfo?>
    {
        public ImmutableArray<Suppression> Suppressions { get; }

        internal ProgrammaticSuppressionInfo(ImmutableArray<Suppression> suppressions)
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
                this.Suppressions.SetEquals(other.Suppressions, EqualityComparer<Suppression>.Default);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ProgrammaticSuppressionInfo);
        }

        public override int GetHashCode()
        {
            return Suppressions.Length;
        }
    }
}
