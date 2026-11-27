// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

internal sealed class FilePathNormalizingComparer : IEqualityComparer<string>
{
    public static readonly FilePathNormalizingComparer Instance = new();

    private FilePathNormalizingComparer()
    {
    }

    public bool Equals(string? x, string? y) => FilePathNormalizer.AreFilePathsEquivalent(x, y);

    public int GetHashCode(string obj) => FilePathNormalizer.GetHashCode(obj);
}
