// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal sealed class LogicalStringComparer : IComparer<string>
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string psz1, string psz2);

    public static readonly IComparer<string> Instance = new LogicalStringComparer();

    private LogicalStringComparer()
    {
    }

    public int Compare(string x, string y)
        => StrCmpLogicalW(x, y);
}
