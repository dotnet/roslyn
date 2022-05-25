// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal class LogicalStringComparer : IComparer<string>
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
}
