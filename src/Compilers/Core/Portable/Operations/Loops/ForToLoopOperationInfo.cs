// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Operations
{
    internal class ForToLoopOperationUserDefinedInfo
    {
        public readonly IBinaryOperation Addition;
        public readonly IBinaryOperation Subtraction;
        public readonly IOperation LessThanOrEqual;
        public readonly IOperation GreaterThanOrEqual;

        public ForToLoopOperationUserDefinedInfo(IBinaryOperation addition, IBinaryOperation subtraction, IOperation lessThanOrEqual, IOperation greaterThanOrEqual)
        {
            Addition = addition;
            Subtraction = subtraction;
            LessThanOrEqual = lessThanOrEqual;
            GreaterThanOrEqual = greaterThanOrEqual;
        }
    }
}
