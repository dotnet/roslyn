// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Structure
{
    internal static class OutliningOptions
    {
        public static readonly PerLanguageOption2<bool> Outlining = new("dotnet_outlining_options_outlining", defaultValue: true);
    }
}
