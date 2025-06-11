// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Microsoft.CodeAnalysis.UnitTests;

internal sealed class GlobalizationUtilities
{
    /// <summary>
    /// The final ordering of kana depends on what globalization mode is being used.
    /// On .net framework it is NLS, but on .net core + newer versions of windows, it defers to ICU
    /// See https://docs.microsoft.com/en-us/dotnet/core/extensions/globalization-icu
    /// and https://github.com/dotnet/runtime/blob/78065413b2d1b4f0ed26343567379e992a3e26ee/src/libraries/System.Globalization/tests/CompareInfo/CompareInfoTests.cs#L100
    /// 
    /// This helper allows us to figure out at runtime which mode is being used so tests can behave accordingly.
    /// </summary>
    public static bool ICUMode()
    {
        // Copied from https://docs.microsoft.com/en-us/dotnet/core/extensions/globalization-icu#determine-if-your-app-is-using-icu
        var sortVersion = CultureInfo.InvariantCulture.CompareInfo.Version;
        var bytes = sortVersion.SortId.ToByteArray();
        var version = bytes[3] << 24 | bytes[2] << 16 | bytes[1] << 8 | bytes[0];
        return version != 0 && version == sortVersion.FullVersion;
    }
}
