// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    internal static class DeclarationExtensions
    {
        public static IEnumerable<DeclarationModifiers> Elements(this DeclarationModifiers combined)
        {
            for (int i = 1; i <= (int)DeclarationModifiers.Last; i <<= 1)
            {
                if ((i & (int)combined) != 0)
                {
                    yield return (DeclarationModifiers)i;
                }
            }
        }
    }
}