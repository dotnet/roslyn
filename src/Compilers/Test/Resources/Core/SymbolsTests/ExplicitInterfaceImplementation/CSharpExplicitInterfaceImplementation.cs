// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//csc /target:library

interface Interface
{
    void Method();
}

class Class : Interface
{
    void Interface.Method() { }
}

interface IGeneric<T>
{
    void Method<U, Z>(T t, U u); //wrong number of type parameters
    void Method<U>(T t); //wrong number of parameters
    void Method<U>(U u, T t); //wrong parameter types
    void Method<U>(T t, ref U u); //wrong parameter refness
    T Method<U>(T t1, T t2); //wrong return type

    void Method<U>(T t, U u); //match
}

class Generic<S> : IGeneric<S>
{
    void IGeneric<S>.Method<U, Z>(S s, U u) { }
    void IGeneric<S>.Method<U>(S s) { }
    void IGeneric<S>.Method<U>(U u, S s) { }
    void IGeneric<S>.Method<U>(S s, ref U u) { }
    S IGeneric<S>.Method<U>(S s1, S s2) { return s1; }

    void IGeneric<S>.Method<V>(S s, V v) { }
}

class Constructed : IGeneric<int>
{
    void IGeneric<int>.Method<U, Z>(int i, U u) { }
    void IGeneric<int>.Method<U>(int i) { }
    void IGeneric<int>.Method<U>(U u, int i) { }
    void IGeneric<int>.Method<U>(int i, ref U u) { }
    int IGeneric<int>.Method<U>(int i1, int i2) { return i1; }

    void IGeneric<int>.Method<W>(int i, W w) { }
}

interface IGenericInterface<T> : Interface
{
}

//we'll see a type def for this class, a type ref for IGenericInterface<int>,
//and then a type def for Interface (i.e. back and forth)
class IndirectImplementation : IGenericInterface<int>
{
    void Interface.Method() { }
}

interface IGeneric2<T>
{
    void Method(T t);
}

class Outer<T>
{
    internal interface IInner<U>
    {
        void Method(U u);
    }

    internal class Inner1<A> : IGeneric2<A> //outer interface, inner type param
    {
        void IGeneric2<A>.Method(A a) { }
    }

    internal class Inner2<B> : IGeneric2<T> //outer interface, outer type param
    {
        void IGeneric2<T>.Method(T t) { }
    }

    internal class Inner3<C> : IInner<C> //inner interface, inner type param
    {
        void IInner<C>.Method(C b) { }
    }

    internal class Inner4<D> : IInner<T> //inner interface, outer type param
    {
        void IInner<T>.Method(T t) { }
    }
}
