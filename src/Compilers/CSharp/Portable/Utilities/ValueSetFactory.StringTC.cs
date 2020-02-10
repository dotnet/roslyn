// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        private struct StringTC : EqualableValueTC<string>
        {
            string EqualableValueTC<string>.FromConstantValue(ConstantValue constantValue) => constantValue.StringValue!;
        }
    }
}
