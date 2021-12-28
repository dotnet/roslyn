// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(Base))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(GenericBase<>))]

public class Derived : Base
{
};

public class GenericDerived<S> : GenericBase<S>
{
};

public class GenericDerived1<S1, S2> : GenericBase<S1>.NestedGenericBase<S2>
{
};
