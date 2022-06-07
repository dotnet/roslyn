// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//csc /target:library

interface Interface
{
    int Property { get; set; }
}

class Class : Interface
{
    int Interface.Property { get; set; }
}

interface IGeneric<T>
{
    T Property { get; set; }
}

class Generic<S> : IGeneric<S>
{
    S IGeneric<S>.Property { get; set; }
}

class Constructed : IGeneric<int>
{
    int IGeneric<int>.Property { get; set; }
}

interface IGenericInterface<T> : Interface
{
}

//we'll see a type def for this class, a type ref for IGenericInterface<int>,
//and then a type def for Interface (i.e. back and forth)
class IndirectImplementation : IGenericInterface<int>
{
    int Interface.Property { get; set; }
}

interface IGeneric2<T>
{
    T Property { get; set; }
}

class Outer<T>
{
    public interface IInner<U>
    {
        U Property { get; set; }
    }

    public class Inner1<A> : IGeneric2<A> //outer interface, inner type param
    {
        A IGeneric2<A>.Property { get; set; }
    }

    public class Inner2<B> : IGeneric2<T> //outer interface, outer type param
    {
        T IGeneric2<T>.Property { get; set; }
    }

    internal class Inner3<C> : IInner<C> //inner interface, inner type param
    {
        C IInner<C>.Property { get; set; }
    }

    protected class Inner4<D> : IInner<T> //inner interface, outer type param
    {
        T IInner<T>.Property { get; set; }
    }
}
