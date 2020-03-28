// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


public class Base
{ 
};

public class Derived : Base
{ 
};

public class GenericBase<T>
{
    public class NestedGenericBase<T1>
    {
    };
};

public class GenericDerived<S> : GenericBase<S>
{
};

public class GenericDerived1<S1, S2> : GenericBase<S1>.NestedGenericBase<S2>
{
};
