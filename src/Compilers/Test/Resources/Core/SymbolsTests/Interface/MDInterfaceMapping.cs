// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

