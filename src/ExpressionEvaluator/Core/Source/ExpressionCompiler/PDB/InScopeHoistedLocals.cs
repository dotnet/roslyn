// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class InScopeHoistedLocals
    {
        public static readonly InScopeHoistedLocals Empty = new EmptyInScopeHoistedLocals();

        public abstract bool IsInScope(string fieldName);

        private sealed class EmptyInScopeHoistedLocals : InScopeHoistedLocals
        {
            public override bool IsInScope(string fieldName)
            {
                return false;
            }
        }
    }
}
