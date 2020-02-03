// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
