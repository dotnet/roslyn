// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal sealed record class VariableStyle(
    ParameterStyle ParameterStyle,
    ReturnStyle ReturnStyle)
{
    public static readonly VariableStyle None =
        new(ParameterStyle.None, ReturnStyle.None);

    public static readonly VariableStyle InputOnly =
        new(ParameterStyle.InputOnly, ReturnStyle.None);

    public static readonly VariableStyle MoveOut =
        new(ParameterStyle.MoveOut, ReturnStyle.None);

    public static readonly VariableStyle SplitOut =
        new(ParameterStyle.SplitOut, ReturnStyle.None);

    public static readonly VariableStyle MoveIn =
        new(ParameterStyle.MoveIn, ReturnStyle.None);

    public static readonly VariableStyle SplitIn =
        new(ParameterStyle.SplitIn, ReturnStyle.None);

    public static readonly VariableStyle NotUsed =
        new(ParameterStyle.MoveOut, ReturnStyle.Initialization);

    public static readonly VariableStyle Ref =
        new(ParameterStyle.Ref, ReturnStyle.AssignmentWithInput);

    public static readonly VariableStyle OnlyAsRefParam =
        new(ParameterStyle.Ref, ReturnStyle.None);

    public static readonly VariableStyle Out =
        new(ParameterStyle.Out, ReturnStyle.AssignmentWithNoInput);

    public static readonly VariableStyle OutWithErrorInput =
        new(ParameterStyle.Out, ReturnStyle.AssignmentWithInput);

    public static readonly VariableStyle OutWithMoveOut =
        new(ParameterStyle.OutWithMoveOut, ReturnStyle.Initialization);
}
