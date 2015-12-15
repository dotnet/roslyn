// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public enum NumberRadix : byte
    {
        Decimal = 10,
        Hexadecimal = 16,
    }

    internal static class NumberRadixExtensions
    {
        internal static bool IsValid(this NumberRadix radix)
        {
            switch(radix)
            {
                case NumberRadix.Decimal:
                case NumberRadix.Hexadecimal:
                    return true;
                default:
                    return false;
            }
        }
    }
}