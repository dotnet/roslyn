// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal sealed record class ParameterStyle(
    ParameterBehavior ParameterBehavior,
    DeclarationBehavior DeclarationBehavior)
{
    public static readonly ParameterStyle None =
        new(ParameterBehavior.None, DeclarationBehavior.None);

    public static readonly ParameterStyle InputOnly =
        new(ParameterBehavior.Input, DeclarationBehavior.None);

    public static readonly ParameterStyle MoveOut =
        new(ParameterBehavior.None, DeclarationBehavior.MoveOut);

    public static readonly ParameterStyle SplitOut =
        new(ParameterBehavior.None, DeclarationBehavior.SplitOut);

    public static readonly ParameterStyle MoveIn =
        new(ParameterBehavior.None, DeclarationBehavior.MoveIn);

    public static readonly ParameterStyle SplitIn =
        new(ParameterBehavior.None, DeclarationBehavior.SplitIn);

    public static readonly ParameterStyle Out =
        new(ParameterBehavior.Out, DeclarationBehavior.None);

    public static readonly ParameterStyle Ref =
        new(ParameterBehavior.Ref, DeclarationBehavior.None);

    public static readonly ParameterStyle OutWithMoveOut =
        new(ParameterBehavior.Out, DeclarationBehavior.MoveOut);
}
