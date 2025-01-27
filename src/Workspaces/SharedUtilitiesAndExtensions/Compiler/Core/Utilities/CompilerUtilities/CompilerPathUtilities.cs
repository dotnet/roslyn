// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities;

internal static class CompilerPathUtilities
{
    internal static void RequireAbsolutePath(string path, string argumentName)
    {
        if (path == null)
        {
            throw new ArgumentNullException(argumentName);
        }

        if (!PathUtilities.IsAbsolute(path))
        {
            throw new ArgumentException(CompilerExtensionsResources.Absolute_path_expected, argumentName);
        }
    }
}
