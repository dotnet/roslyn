// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticsOptionsStorage
    {
        public static readonly Option2<bool> Classification = new("dotnet_enable_classification", defaultValue: true);

        public static readonly Option2<bool> Squiggles = new("dotnet_enable_squiggles", defaultValue: true);
    }
}
