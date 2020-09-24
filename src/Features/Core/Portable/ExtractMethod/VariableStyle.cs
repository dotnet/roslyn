// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal class VariableStyle
    {
        public ParameterStyle ParameterStyle { get; private set; }
        public ReturnStyle ReturnStyle { get; private set; }

        public static readonly VariableStyle None =
            new() { ParameterStyle = ParameterStyle.None, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle InputOnly =
            new() { ParameterStyle = ParameterStyle.InputOnly, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle Delete =
            new() { ParameterStyle = ParameterStyle.Delete, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle MoveOut =
            new() { ParameterStyle = ParameterStyle.MoveOut, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle SplitOut =
            new() { ParameterStyle = ParameterStyle.SplitOut, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle MoveIn =
            new() { ParameterStyle = ParameterStyle.MoveIn, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle SplitIn =
            new() { ParameterStyle = ParameterStyle.SplitIn, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle NotUsed =
            new() { ParameterStyle = ParameterStyle.MoveOut, ReturnStyle = ReturnStyle.Initialization };

        public static readonly VariableStyle Ref =
            new() { ParameterStyle = ParameterStyle.Ref, ReturnStyle = ReturnStyle.AssignmentWithInput };

        public static readonly VariableStyle OnlyAsRefParam =
            new() { ParameterStyle = ParameterStyle.Ref, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle Out =
            new() { ParameterStyle = ParameterStyle.Out, ReturnStyle = ReturnStyle.AssignmentWithNoInput };

        public static readonly VariableStyle OutWithErrorInput =
            new() { ParameterStyle = ParameterStyle.Out, ReturnStyle = ReturnStyle.AssignmentWithInput };

        public static readonly VariableStyle OutWithMoveOut =
            new() { ParameterStyle = ParameterStyle.OutWithMoveOut, ReturnStyle = ReturnStyle.Initialization };
    }
}
