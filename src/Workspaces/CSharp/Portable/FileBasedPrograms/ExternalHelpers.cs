// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NET
using Roslyn.Utilities;

namespace Microsoft.DotNet.FileBasedPrograms;

/// <summary>Provides implementations of certain methods to the FileBasedPrograms source package.</summary>
internal partial class ExternalHelpers
{
    public static partial int CombineHashCodes(int value1, int value2)
        => Hash.Combine(value1, value2);

    public static partial string GetRelativePath(string relativeTo, string path)
        => PathUtilities.GetRelativePath(relativeTo, path);

    public static partial bool IsPathFullyQualified(string path)
        => PathUtilities.IsAbsolute(path);
}
#endif