// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    internal class ForToLoopOperationUserDefinedInfo
    {
        public readonly Lazy<IBinaryOperation> Addition;
        public readonly Lazy<IBinaryOperation> Subtraction;
        public readonly Lazy<IOperation> LessThanOrEqual;
        public readonly Lazy<IOperation> GreaterThanOrEqual;

        public ForToLoopOperationUserDefinedInfo(Lazy<IBinaryOperation> addition, Lazy<IBinaryOperation> subtraction, Lazy<IOperation> lessThanOrEqual, Lazy<IOperation> greaterThanOrEqual)
        {
            Addition = addition;
            Subtraction = subtraction;
            LessThanOrEqual = lessThanOrEqual;
            GreaterThanOrEqual = greaterThanOrEqual;
        }
    }
}
