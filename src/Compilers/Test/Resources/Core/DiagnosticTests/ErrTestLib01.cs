// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
