// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
