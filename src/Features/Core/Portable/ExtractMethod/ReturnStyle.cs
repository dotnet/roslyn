// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal class ReturnStyle
    {
        public ParameterBehavior ParameterBehavior { get; private set; }
        public ReturnBehavior ReturnBehavior { get; private set; }
        public DeclarationBehavior DeclarationBehavior { get; private set; }

        public static readonly ReturnStyle None =
            new ReturnStyle() { ParameterBehavior = ParameterBehavior.None, ReturnBehavior = ReturnBehavior.None, DeclarationBehavior = DeclarationBehavior.None };

        public static readonly ReturnStyle AssignmentWithInput =
            new ReturnStyle() { ParameterBehavior = ParameterBehavior.Input, ReturnBehavior = ReturnBehavior.Assignment, DeclarationBehavior = DeclarationBehavior.None };

        public static readonly ReturnStyle AssignmentWithNoInput =
            new ReturnStyle() { ParameterBehavior = ParameterBehavior.None, ReturnBehavior = ReturnBehavior.Assignment, DeclarationBehavior = DeclarationBehavior.SplitIn };

        public static readonly ReturnStyle Initialization =
            new ReturnStyle() { ParameterBehavior = ParameterBehavior.None, ReturnBehavior = ReturnBehavior.Initialization, DeclarationBehavior = DeclarationBehavior.SplitOut };
    }
}
