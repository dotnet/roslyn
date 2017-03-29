// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Utilities
{
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
                throw new ArgumentException(Microsoft.CodeAnalysis.CodeAnalysisResources.AbsolutePathExpected, argumentName);
            }
        }
    }
}
