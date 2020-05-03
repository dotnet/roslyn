// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// csc /t:library

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public interface IGoo
{
    void Goo();
}

public class A
{
    public void Goo() { Console.WriteLine("A.Goo"); }
}

public class B : A, IGoo
{
}

public class C : B
{
    public new void Goo() { Console.WriteLine("C.Goo"); }
}

