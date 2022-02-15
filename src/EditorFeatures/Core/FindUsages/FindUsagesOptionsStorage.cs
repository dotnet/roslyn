﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using System;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal static class FindUsagesOptionsStorage
    {
#pragma warning disable IDE0060 // Remove unused parameter -- TODO
        public static FindUsagesOptions GetFindUsagesOptions(this IGlobalOptionService globalOptions, string language)
            => throw new NotImplementedException();
#pragma warning restore IDE0060 // Remove unused parameter
    }
}
