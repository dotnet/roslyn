// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace NS
{
    // for CS1714
    public class E<T> : D<T> { }

    public class Ref
    {
        public static A GetA() { return new A(); }
        // for CS1682 - nested
        public static A.B GetB() { return null; }
        // for CS1684
        public static C GetC() { return null; }
        // for CS1714
        public static E<int> GetE() { return null; }
    }
}

namespace N1
{
    public class N2
    {
        public class A { }
    }
}
