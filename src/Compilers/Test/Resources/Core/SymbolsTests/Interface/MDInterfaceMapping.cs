// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// csc /t:library

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public interface IFoo
{
    void Foo();
}

public class A
{
    public void Foo() { Console.WriteLine("A.Foo"); }
}

public class B : A, IFoo
{
}

public class C : B
{
    public new void Foo() { Console.WriteLine("C.Foo"); }
}

