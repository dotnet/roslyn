// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static class StringComparers
    {
       public static StringComparer FileSystemNameComparer =  StringComparer.OrdinalIgnoreCase;
       public static StringComparer IdentifierComparer = CaseInsensitiveComparison.Comparer; 
    }
}
