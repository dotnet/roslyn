// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    internal enum PlaceholderKind
    {
        Unspecified = 0,
        SwitchOperationExpression = 1,
        ForToLoopBinaryOperatorLeftOperand = 2,
        ForToLoopBinaryOperatorRightOperand = 3,
        AggregationGroup = 4,
    }
}
