// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// csc /t:library /unsafe DynamicAttribute.cs

public class Base0 { }
public class Base1<T> { }
public class Base2<T, U> { }

public class Outer<T> : Base1<dynamic>
{
    public class Inner<U, V> : Base2<dynamic, V>
    {
        public class InnerInner<W> : Base1<dynamic> { }
    }
}

public class Outer2<T> : Base1<dynamic>
{
    public class Inner2<U, V> : Base0
    {
        public class InnerInner2<W> : Base0 { }
    }
}

public class Outer3
{
    public class Inner3<U>
    {
        public static Outer3.Inner3<dynamic> field1 = null;
    }
}

public class Derived<T> : Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>
where T : Derived<T>
{
    public static dynamic field1;
    public static dynamic[] field2;
    public static dynamic[][] field3;

    public const dynamic field4 = null;
    public const dynamic[] field5 = null;
    public const dynamic[][] field6 = null;
    public const dynamic[][] field7 = null;

    public Outer<T>.Inner<int, T>.InnerInner<Outer<dynamic>> field8 = null;
    public Outer<dynamic>.Inner<T, T>.InnerInner<T> field9 = null;
    public Outer<Outer<dynamic>.Inner<T, dynamic>>.Inner<dynamic, T>.InnerInner<T> field10 = null;
    public Outer<T>.Inner<dynamic, dynamic>.InnerInner<T> field11 = null;
    public Outer<T>.Inner<T, T>.InnerInner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>> field12 = null;
    public Outer<dynamic>.Inner<Outer<T>, T>.InnerInner<dynamic> field13 = null;
    public Outer<dynamic>.Inner<dynamic, dynamic>.InnerInner<dynamic> field14 = null;

    public Outer<dynamic>.Inner<Outer<dynamic>, T>.InnerInner<dynamic>[] field15 = null;
    public Outer<dynamic>.Inner<Outer<dynamic>.Inner<T, dynamic>.InnerInner<int>, dynamic[]>.InnerInner<dynamic>[][] field16 = null;
    public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] field17 = null;

    public static dynamic F1(dynamic x) { return x; }
    public static dynamic F2(ref dynamic x) { return x; }
    public static dynamic[] F3(dynamic[] x) { return x; }
    public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] F4(Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] x) { return x; }

    public static dynamic Prop1 { get { return field1; } }
    public static Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int>[], dynamic>.InnerInner<dynamic>[][] Prop2 { get { return field17; } set { field17 = value; } }
}

public unsafe class UnsafeClass<T> : Base2<int*[], Outer<dynamic>.Inner<Outer<dynamic>.Inner<T[], dynamic>.InnerInner<int*[][]>[], dynamic>.InnerInner<dynamic>[][]> { }

public struct Struct
{
    public static Outer<dynamic>.Inner<dynamic, Struct?> nullableField;
}
