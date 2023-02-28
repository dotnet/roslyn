// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Structure
{
    internal static class OutliningOptionsStorage
    {
        public static readonly PerLanguageOption2<bool> Outlining = new("dotnet_enter_outlining_mode_on_file_open", defaultValue: true);
    }
}
