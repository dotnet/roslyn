// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Operations
{
    internal enum PlaceholderKind
    {
        Unspecified = 0,
        SwitchOperationExpression = 1,
        ForToLoopBinaryOperatorLeftOperand = 2,
        ForToLoopBinaryOperatorRightOperand = 3,
        AggregationGroup = 4,
        CollectionBuilderElements = 5,
    }
}
