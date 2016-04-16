// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            return StrCmpLogicalW(x, y);
        }
    }
}
