// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal class VariableStyle
    {
        public ParameterStyle ParameterStyle { get; private set; }
        public ReturnStyle ReturnStyle { get; private set; }

        public static readonly VariableStyle None =
            new VariableStyle() { ParameterStyle = ParameterStyle.None, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle InputOnly =
            new VariableStyle() { ParameterStyle = ParameterStyle.InputOnly, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle Delete =
            new VariableStyle() { ParameterStyle = ParameterStyle.Delete, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle MoveOut =
            new VariableStyle() { ParameterStyle = ParameterStyle.MoveOut, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle SplitOut =
            new VariableStyle() { ParameterStyle = ParameterStyle.SplitOut, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle MoveIn =
            new VariableStyle() { ParameterStyle = ParameterStyle.MoveIn, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle SplitIn =
            new VariableStyle() { ParameterStyle = ParameterStyle.SplitIn, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle NotUsed =
            new VariableStyle() { ParameterStyle = ParameterStyle.MoveOut, ReturnStyle = ReturnStyle.Initialization };

        public static readonly VariableStyle Ref =
            new VariableStyle() { ParameterStyle = ParameterStyle.Ref, ReturnStyle = ReturnStyle.AssignmentWithInput };

        public static readonly VariableStyle OnlyAsRefParam =
            new VariableStyle() { ParameterStyle = ParameterStyle.Ref, ReturnStyle = ReturnStyle.None };

        public static readonly VariableStyle Out =
            new VariableStyle() { ParameterStyle = ParameterStyle.Out, ReturnStyle = ReturnStyle.AssignmentWithNoInput };

        public static readonly VariableStyle OutWithErrorInput =
            new VariableStyle() { ParameterStyle = ParameterStyle.Out, ReturnStyle = ReturnStyle.AssignmentWithInput };

        public static readonly VariableStyle OutWithMoveOut =
            new VariableStyle() { ParameterStyle = ParameterStyle.OutWithMoveOut, ReturnStyle = ReturnStyle.Initialization };
    }
}
