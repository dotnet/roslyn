// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SmartIndent
{
    internal static class SmartIndenterOptionsStorage
    {
        public static readonly Option2<bool> SmartIndenter = new("dotnet_smart_indenter", defaultValue: true);
    }
}
