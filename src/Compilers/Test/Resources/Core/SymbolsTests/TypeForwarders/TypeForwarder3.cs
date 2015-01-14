// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


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
