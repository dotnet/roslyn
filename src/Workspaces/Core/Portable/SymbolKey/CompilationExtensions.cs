// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal static class CompilationExtensions
    {
        public static bool NamesAreEqual(this Compilation compilation, string name1, string name2)
         => string.Equals(name1, name2, compilation.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }
}
