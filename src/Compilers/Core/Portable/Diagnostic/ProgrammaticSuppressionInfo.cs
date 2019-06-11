// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Contains information about the source of a programmatic diagnostic suppression produced by an <see cref="DiagnosticSuppressor"/>.
    /// </summary>
    internal sealed class ProgrammaticSuppressionInfo : IEquatable<ProgrammaticSuppressionInfo>
    {
        public ImmutableHashSet<(string Id, LocalizableString Justification)> Suppressions { get; }

        internal ProgrammaticSuppressionInfo(ImmutableHashSet<(string Id, LocalizableString Justification)> suppressions)
        {
            Suppressions = suppressions;
        }

        public bool Equals(ProgrammaticSuppressionInfo other)
        {
            return other != null &&
                this.Suppressions.SetEquals(other.Suppressions);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ProgrammaticSuppressionInfo);
        }

        public override int GetHashCode()
        {
            var hash = Suppressions.Count.GetHashCode();
            foreach (var suppression in Suppressions.Order())
            {
                hash = Hash.Combine(suppression.GetHashCode(), hash);
            }

            return hash;
        }
    }
}
