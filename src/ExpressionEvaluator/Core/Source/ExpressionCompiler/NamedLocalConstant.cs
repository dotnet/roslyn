// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct NamedLocalConstant
    {
        internal readonly string Name;
        internal readonly byte[] Signature;
        internal readonly ConstantValue Value;

        internal NamedLocalConstant(string name, byte[] signature, ConstantValue value)
        {
            this.Name = name;
            this.Signature = signature;
            this.Value = value;
        }
    }
}
