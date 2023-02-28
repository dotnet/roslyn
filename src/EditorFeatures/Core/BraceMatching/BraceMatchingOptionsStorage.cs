// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.BraceMatching
{
    internal static class BraceMatchingOptionsStorage
    {
        public static readonly Option2<bool> BraceMatching = new("dotnet_enable_brace_matching", defaultValue: true);
    }
}
