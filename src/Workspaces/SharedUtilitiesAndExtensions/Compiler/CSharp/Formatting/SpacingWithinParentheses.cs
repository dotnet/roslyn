// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

[Flags]
internal enum SpacePlacementWithinParentheses
{
    None = 0,
    Expressions = 1,
    TypeCasts = 1 << 1,
    ControlFlowStatements = 1 << 2,
    All = (1 << 3) - 1
}

internal static partial class Extensions
{
    public static SpacePlacementWithinParentheses ToSpacingWithinParentheses(this SpacePlacement placement)
        => (placement.HasFlag(SpacePlacement.WithinExpressionParentheses) ? SpacePlacementWithinParentheses.Expressions : 0) |
           (placement.HasFlag(SpacePlacement.WithinCastParentheses) ? SpacePlacementWithinParentheses.TypeCasts : 0) |
           (placement.HasFlag(SpacePlacement.WithinOtherParentheses) ? SpacePlacementWithinParentheses.ControlFlowStatements : 0);

    public static SpacePlacement ToSpacePlacement(this SpacePlacementWithinParentheses placement)
        => (placement.HasFlag(SpacePlacementWithinParentheses.Expressions) ? SpacePlacement.WithinExpressionParentheses : 0) |
           (placement.HasFlag(SpacePlacementWithinParentheses.TypeCasts) ? SpacePlacement.WithinCastParentheses : 0) |
           (placement.HasFlag(SpacePlacementWithinParentheses.ControlFlowStatements) ? SpacePlacement.WithinOtherParentheses : 0);

    public static SpacePlacementWithinParentheses WithFlagValue(this SpacePlacementWithinParentheses flags, SpacePlacementWithinParentheses flag, bool value)
        => (flags & ~flag) | (value ? flag : 0);
}
