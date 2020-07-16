// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
