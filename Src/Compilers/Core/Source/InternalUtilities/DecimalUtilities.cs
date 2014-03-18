// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class DecimalUtilities
    {
        public static int GetScale(this decimal d)
        {
            var bits = (uint[])(object)decimal.GetBits(d);

            return (int)((bits[3] >> 16) & 31);
        }
    }
}