// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

class BaseTypeSpecifierClass : global::System.IComparable
{
    public int CompareTo(object o) { return 0; }
}
class GooAttribute : System.Attribute { }
interface I1
{
    int Method();
}
class Test : I1
{
    struct N1 { }
    int I1.Method()
    {
        return 5;
    }
}
