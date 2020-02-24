// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        private struct DecimalTC : EqualableValueTC<decimal>
        {
            decimal EqualableValueTC<decimal>.FromConstantValue(ConstantValue constantValue) => constantValue.DecimalValue;
        }
    }
}
