// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace NS
{
    public class A
    {
        public void Test() { }

        // for CS1682
        public class B { }
    }
    // for CS1684
    public class C { }
    // for CS1714
    public class D<T> { }
}
