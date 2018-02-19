// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class ParameterSignature
    {
        internal ParameterSignature(TypeSignature type, bool isByRef)
        {
            Type = type;
            IsByRef = isByRef;
        }

        internal readonly TypeSignature Type;
        internal readonly bool IsByRef;
    }
}
