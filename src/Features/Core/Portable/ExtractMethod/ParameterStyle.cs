// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal sealed class ParameterStyle
{
    public ParameterBehavior ParameterBehavior { get; private set; }
    public DeclarationBehavior DeclarationBehavior { get; private set; }
    public DeclarationBehavior SaferDeclarationBehavior { get; private set; }

    public static readonly ParameterStyle None =
        new ParameterStyle() { ParameterBehavior = ParameterBehavior.None, DeclarationBehavior = DeclarationBehavior.None, SaferDeclarationBehavior = DeclarationBehavior.None };

    public static readonly ParameterStyle InputOnly =
        new ParameterStyle() { ParameterBehavior = ParameterBehavior.Input, DeclarationBehavior = DeclarationBehavior.None, SaferDeclarationBehavior = DeclarationBehavior.None };

    public static readonly ParameterStyle Delete =
        new ParameterStyle() { ParameterBehavior = ParameterBehavior.None, DeclarationBehavior = DeclarationBehavior.Delete, SaferDeclarationBehavior = DeclarationBehavior.None };

    public static readonly ParameterStyle MoveOut =
        new ParameterStyle() { ParameterBehavior = ParameterBehavior.None, DeclarationBehavior = DeclarationBehavior.MoveOut, SaferDeclarationBehavior = DeclarationBehavior.SplitOut };

    public static readonly ParameterStyle SplitOut =
        new ParameterStyle() { ParameterBehavior = ParameterBehavior.None, DeclarationBehavior = DeclarationBehavior.SplitOut, SaferDeclarationBehavior = DeclarationBehavior.SplitOut };

    public static readonly ParameterStyle MoveIn =
        new ParameterStyle() { ParameterBehavior = ParameterBehavior.None, DeclarationBehavior = DeclarationBehavior.MoveIn, SaferDeclarationBehavior = DeclarationBehavior.SplitIn };

    public static readonly ParameterStyle SplitIn =
        new ParameterStyle() { ParameterBehavior = ParameterBehavior.None, DeclarationBehavior = DeclarationBehavior.SplitIn, SaferDeclarationBehavior = DeclarationBehavior.SplitIn };

    public static readonly ParameterStyle Out =
        new ParameterStyle() { ParameterBehavior = ParameterBehavior.Out, DeclarationBehavior = DeclarationBehavior.None, SaferDeclarationBehavior = DeclarationBehavior.None };

    public static readonly ParameterStyle Ref =
        new ParameterStyle() { ParameterBehavior = ParameterBehavior.Ref, DeclarationBehavior = DeclarationBehavior.None, SaferDeclarationBehavior = DeclarationBehavior.None };

    public static readonly ParameterStyle OutWithMoveOut =
        new ParameterStyle() { ParameterBehavior = ParameterBehavior.Out, DeclarationBehavior = DeclarationBehavior.MoveOut, SaferDeclarationBehavior = DeclarationBehavior.MoveOut };
}
