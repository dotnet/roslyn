// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Represents the arguments passed to an implicit object creation expression <c>new(...)</c>.  These arguments are
/// initially analyzed in the binder, but not fully bound until the conversion pass when the final type being
/// constructed can be found, which determines which actual constructors to bind the arguments against.
/// </summary>
internal readonly struct BoundUnconvertedArguments(
    ImmutableArray<BoundExpression> expressions,
    ImmutableArray<(string Name, Location Location)?> namesOpt,
    ImmutableArray<RefKind> refKindsOpt)
{
    /// <summary>
    /// The expressions as they exist in the source, left to right.
    /// </summary>
    public readonly ImmutableArray<BoundExpression> Expressions = expressions;

    /// <summary>
    /// Optional names provided with the arguments, or <see langword="default"/> if no names were provided.
    /// </summary>
    public readonly ImmutableArray<(string Name, Location Location)?> NamesOpt = namesOpt;

    /// <summary>
    /// Optional ref-kinds provided with the arguments, or <see langword="default"/> if no ref-kinds were provided.
    /// </summary>
    public readonly ImmutableArray<RefKind> RefKindsOpt = refKindsOpt;

    public static bool operator ==(in BoundUnconvertedArguments left, in BoundUnconvertedArguments right)
        => left.Expressions == right.Expressions &&
           left.NamesOpt == right.NamesOpt &&
           left.RefKindsOpt == right.RefKindsOpt;

    public static bool operator !=(in BoundUnconvertedArguments left, in BoundUnconvertedArguments right)
        => !(left == right);

    public override bool Equals(object? obj)
        => obj is BoundUnconvertedArguments other && this == other;

    public override int GetHashCode()
        => Hash.Combine(this.Expressions.GetHashCode(), Hash.Combine(this.NamesOpt.GetHashCode(), this.RefKindsOpt.GetHashCode()));
}
