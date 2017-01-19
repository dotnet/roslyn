// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class ParameterComparer : IEqualityComparer<ParameterSignature>
    {
        internal static readonly ParameterComparer Instance = new ParameterComparer();

        public bool Equals(ParameterSignature x, ParameterSignature y)
        {
            return TypeComparer.Instance.Equals(x.Type, y.Type);
        }

        public int GetHashCode(ParameterSignature obj)
        {
            throw new NotImplementedException();
        }
    }
}
