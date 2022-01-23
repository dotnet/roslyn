// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//csc /target:library

using System;

interface Interface
{
    event Action<int> Event;
}

class Class : Interface
{
    event Action<int> Interface.Event { add { } remove { } }
}

interface IGeneric<T>
{
    event Action<T> Event;
}

class Generic<S> : IGeneric<S>
{
    event Action<S> IGeneric<S>.Event { add { } remove { } }
}

class Constructed : IGeneric<int>
{
    event Action<int> IGeneric<int>.Event { add { } remove { } }
}

interface IGenericInterface<T> : Interface
{
}

//we'll see a type def for this class, a type ref for IGenericInterface<int>,
//and then a type def for Interface (i.e. back and forth)
class IndirectImplementation : IGenericInterface<int>
{
    event Action<int> Interface.Event { add { } remove { } }
}

interface IGeneric2<T>
{
    event Action<T> Event;
}

class Outer<T>
{
    public interface IInner<U>
    {
        event Action<U> Event;
    }

    public class Inner1<A> : IGeneric2<A> //outer interface, inner type param
    {
        event Action<A> IGeneric2<A>.Event { add { } remove { } }
    }

    public class Inner2<B> : IGeneric2<T> //outer interface, outer type param
    {
        event Action<T> IGeneric2<T>.Event { add { } remove { } }
    }

    internal class Inner3<C> : IInner<C> //inner interface, inner type param
    {
        event Action<C> IInner<C>.Event { add { } remove { } }
    }

    protected class Inner4<D> : IInner<T> //inner interface, outer type param
    {
        event Action<T> IInner<T>.Event { add { } remove { } }
    }
}
