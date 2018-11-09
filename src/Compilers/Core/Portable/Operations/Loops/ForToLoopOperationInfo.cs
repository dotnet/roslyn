// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
