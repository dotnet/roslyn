// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
