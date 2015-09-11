﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.ExtractInterface;
using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.ExtractInterface;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExtractInterface;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractInterface
{
    public class ExtractInterfaceTests : AbstractExtractInterfaceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_Invocation_CaretInMethod()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo()
    {
        $$
    }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_Invocation_CaretAfterClassClosingBrace()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo()
    {
        
    }
}$$";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_Invocation_CaretBeforeClassKeyword()
        {
            var markup = @"
using System;
$$class MyClass
{
    public void Foo()
    {
        
    }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_Invocation_FromInnerClass1()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo()
    {
        
    }

    class AnotherClass
    {
        $$public void Bar()
        {
        }
    }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "Bar");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_Invocation_FromInnerClass2()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo()
    {
        
    }

    $$class AnotherClass
    {
        public void Bar()
        {
        }
    }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "Bar");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_Invocation_FromOuterClass()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo()
    {
        
    }$$

    class AnotherClass
    {
        public void Bar()
        {
        }
    }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "Foo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_Invocation_FromInterface()
        {
            var markup = @"
using System;
interface IMyInterface
{
    $$void Foo();
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "Foo", expectedInterfaceName: "IMyInterface1");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_Invocation_FromStruct()
        {
            var markup = @"
using System;
struct SomeStruct
{
    $$public void Foo() { }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "Foo", expectedInterfaceName: "ISomeStruct");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_Invocation_FromNamespace()
        {
            var markup = @"
using System;

namespace Ns$$
{
    class MyClass
    {
        public void Foo() { }
    }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_ExtractableMembers_DoesNotIncludeFields()
        {
            var markup = @"
using System;
class MyClass
{
    $$public int x;

    public void Foo()
    {
    }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "Foo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGetAndSet()
        {
            var markup = @"
using System;
class MyClass
{
    $$public int Prop { get; set; }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "Prop");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGetAndPrivateSet()
        {
            var markup = @"
using System;
class MyClass
{
    $$public int Prop { get; private set; }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "Prop");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGet()
        {
            var markup = @"
using System;
class MyClass
{
    $$public int Prop { get; }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "Prop");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_ExtractableMembers_ExcludesPublicProperty_WithPrivateGetAndPrivateSet()
        {
            var markup = @"
using System;
class MyClass
{
    $$public int Prop { private get; private set; }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_ExtractableMembers_IncludesPublicIndexer()
        {
            var markup = @"
using System;
class MyClass
{
    $$public int this[int x] { get { return 5; } set { } }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "this[]");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_ExtractableMembers_ExcludesInternalIndexer()
        {
            var markup = @"
using System;
class MyClass
{
    $$internal int this[int x] { get { return 5; } set { } }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_ExtractableMembers_IncludesPublicMethod()
        {
            var markup = @"
using System;
class MyClass
{
    $$public void M()
    {
    }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "M");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_ExtractableMembers_ExcludesInternalMethod()
        {
            var markup = @"
using System;
class MyClass
{
    $$internal void M()
    {
    }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_ExtractableMembers_IncludesAbstractMethod()
        {
            var markup = @"
using System;
abstract class MyClass
{
    $$public abstract void M();
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "M");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_ExtractableMembers_IncludesPublicEvent()
        {
            var markup = @"
using System;
class MyClass
{
    $$public event Action MyEvent;
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedMemberName: "MyEvent");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_ExtractableMembers_ExcludesPrivateEvent()
        {
            var markup = @"
using System;
class MyClass
{
    $$private event Action MyEvent;
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_DefaultInterfaceName_DoesNotConflictWithOtherTypeNames()
        {
            var markup = @"
using System;
class MyClass
{
    $$public void Foo() { }
}

interface IMyClass { }
struct IMyClass1 { }
class IMyClass2 { }";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedInterfaceName: "IMyClass3");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_NamespaceName_NoNamespace()
        {
            var markup = @"
using System;
class MyClass
{
    $$public void Foo() { }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedNamespaceName: "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_NamespaceName_SingleNamespace()
        {
            var markup = @"
using System;
namespace MyNamespace
{
    class MyClass
    {
        $$public void Foo() { }
    }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedNamespaceName: "MyNamespace");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_NamespaceName_NestedNamespaces()
        {
            var markup = @"
using System;
namespace OuterNamespace
{
    namespace InnerNamespace
    {
        class MyClass
        {
            $$public void Foo() { }
        }
    }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedNamespaceName: "OuterNamespace.InnerNamespace");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_ClassesImplementExtractedInterface()
        {
            var markup = @"using System;

class MyClass
{
    $$public void Foo() { }
}";

            var expectedCode = @"using System;

class MyClass : IMyClass
{
    public void Foo() { }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_StructsImplementExtractedInterface()
        {
            var markup = @"
using System;

struct MyStruct
{
    $$public void Foo() { }
}";

            var expectedCode = @"
using System;

struct MyStruct : IMyStruct
{
    public void Foo() { }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_InterfacesDoNotImplementExtractedInterface()
        {
            var markup = @"
using System;

interface MyInterface
{
    $$void Foo();
}";

            var expectedCode = @"
using System;

interface MyInterface
{
    void Foo();
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_Methods()
        {
            var markup = @"
using System;

abstract class MyClass$$
{
    public void ExtractableMethod_Normal() { }
    public void ExtractableMethod_ParameterTypes(System.Diagnostics.CorrelationManager x, Nullable<Int32> y = 7, string z = ""42"") { }
    public abstract void ExtractableMethod_Abstract();
    unsafe public void NotActuallyUnsafeMethod(int p) { }
    unsafe public void UnsafeMethod(int *p) { }
}";

            var expectedInterfaceCode = @"using System.Diagnostics;

interface IMyClass
{
    void ExtractableMethod_Abstract();
    void ExtractableMethod_Normal();
    void ExtractableMethod_ParameterTypes(CorrelationManager x, int? y = 7, string z = ""42"");
    void NotActuallyUnsafeMethod(int p);
    unsafe void UnsafeMethod(int* p);
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_Events()
        {
            var markup = @"
using System;

abstract internal class MyClass$$
{
    public event Action ExtractableEvent1;
    public event Action<Nullable<Int32>> ExtractableEvent2;
}";

            var expectedInterfaceCode = @"using System;

internal interface IMyClass
{
    event Action ExtractableEvent1;
    event Action<int?> ExtractableEvent2;
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_Properties()
        {
            var markup = @"
using System;

abstract class MyClass$$
{
    public int ExtractableProp { get; set; }
    public int ExtractableProp_GetOnly { get { return 1; } }
    public int ExtractableProp_SetOnly { set { } }
    public int ExtractableProp_SetPrivate { get; private set; }
    public int ExtractableProp_GetPrivate { private get; set; }
    public int ExtractableProp_SetInternal { get; internal set; }
    public int ExtractableProp_GetInternal { internal get; set; }
    unsafe public int NotActuallyUnsafeProp { get; set; }
    unsafe public int* UnsafeProp { get; set; }

}";

            var expectedInterfaceCode = @"interface IMyClass
{
    int ExtractableProp { get; set; }
    int ExtractableProp_GetInternal { set; }
    int ExtractableProp_GetOnly { get; }
    int ExtractableProp_GetPrivate { set; }
    int ExtractableProp_SetInternal { get; }
    int ExtractableProp_SetOnly { set; }
    int ExtractableProp_SetPrivate { get; }
    int NotActuallyUnsafeProp { get; set; }
    unsafe int* UnsafeProp { get; set; }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_Indexers()
        {
            var markup = @"
using System;

abstract class MyClass$$
{
    public int this[int x] { set { } }
    public int this[string x] { get { return 1; } }
    public int this[double x] { get { return 1; } set { } }
    public int this[Nullable<Int32> x, string y = ""42""] { get { return 1; } set { } }
}";

            var expectedInterfaceCode = @"interface IMyClass
{
    int this[double x] { get; set; }
    int this[string x] { get; }
    int this[int x] { set; }
    int this[int? x, string y = ""42""] { get; set; }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_Imports()
        {
            var markup = @"
public class Class
{
    $$public System.Diagnostics.BooleanSwitch M1(System.Globalization.Calendar x) { return null; }
    public void M2(System.Collections.Generic.List<System.IO.BinaryWriter> x) { }
    public void M3<T>() where T : System.Net.WebProxy { }
}";

            var expectedInterfaceCode = @"using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;

public interface IClass
{
    BooleanSwitch M1(Calendar x);
    void M2(List<BinaryWriter> x);
    void M3<T>() where T : WebProxy;
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_TypeParameters1()
        {
            var markup = @"
public class Class<A, B, C, D, E, F, G, H, NO1> where E : F
{
	$$public void Foo1(A a) { }
	public B Foo2() { return default(B); }
	public void Foo3(List<C> list) { }
	
	public event Func<D> Foo4;
	
	public List<E> Prop { set { } }
	public List<G> this[List<List<H>> list] { set { } }
	
	public void Bar1() { var x = default(NO1); }
}";

            var expectedInterfaceCode = @"public interface IClass<A, B, C, D, E, F, G, H> where E : F
{
    List<G> this[List<List<H>> list] { set; }

    List<E> Prop { set; }

    event Func<D> Foo4;

    void Bar1();
    void Foo1(A a);
    B Foo2();
    void Foo3(List<C> list);
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [WorkItem(706894)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_TypeParameters2()
        {
            var markup = @"using System.Collections.Generic;

class Program<A, B, C, D, E> where A : List<B> where B : Dictionary<List<D>, List<E>>
{
    $$public void Foo<T>(T t) where T : List<A> { }
}";

            var expectedInterfaceCode = @"using System.Collections.Generic;

interface IProgram<A, B, D, E>
    where A : List<B>
    where B : Dictionary<List<D>, List<E>>
{
    void Foo<T>(T t) where T : List<A>;
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_TypeParameters3()
        {
            var markup = @"
class $$Class1<A, B>
{
    public void method(A P1, Class2 P2)
    {
    }
    public class Class2
    {
    }
}";

            var expectedInterfaceCode = @"interface IClass1<A, B>
{
    void method(A P1, Class1<A, B>.Class2 P2);
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [WorkItem(706894)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_TypeParameters4()
        {
            var markup = @"
class C1<A>
{
    public class C2<B> where B : new()
    {
        public class C3<C> where C : System.Collections.ICollection
        {
            public class C4
            {$$
                public A method() { return default(A); }
                public B property { set { } }
                public C this[int i] { get { return default(C); } }
            }
        }
    }
}";

            var expectedInterfaceCode = @"using System.Collections;

public interface IC4<A, B, C>
    where B : new()
    where C : ICollection
{
    C this[int i] { get; }

    B property { set; }

    A method();
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedInterfaceCode: expectedInterfaceCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_BaseList_NewBaseListNonGeneric()
        {
            var markup = @"
class Program
{
    $$public void Foo() { }
}";

            var expectedCode = @"
class Program : IProgram
{
    public void Foo() { }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_BaseList_NewBaseListGeneric()
        {
            var markup = @"
class Program<T>
{
    $$public void Foo(T t) { }
}";

            var expectedCode = @"
class Program<T> : IProgram<T>
{
    public void Foo(T t) { }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_BaseList_NewBaseListWithWhereClause()
        {
            var markup = @"
class Program<T, U> where T : U
{
    $$public void Foo(T t, U u) { }
}";

            var expectedCode = @"
class Program<T, U> : IProgram<T, U> where T : U
{
    public void Foo(T t, U u) { }
}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_BaseList_LargerBaseList1()
        {
            var markup = @"
class Program : ISomeInterface
{
    $$public void Foo() { }
}

interface ISomeInterface {}";

            var expectedCode = @"
class Program : ISomeInterface, IProgram
{
    public void Foo() { }
}

interface ISomeInterface {}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_BaseList_LargerBaseList2()
        {
            var markup = @"
class Program<T, U> : ISomeInterface<T>
{
    $$public void Foo(T t, U u) { }
}

interface ISomeInterface<T> {}";

            var expectedCode = @"
class Program<T, U> : ISomeInterface<T>, IProgram<T, U>
{
    public void Foo(T t, U u) { }
}

interface ISomeInterface<T> {}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_BaseList_LargerBaseList3()
        {
            var markup = @"
class Program<T, U> : ISomeInterface<T>, ISomeInterface2<T, U>
{
    $$public void Foo(T t, U u) { }
}

interface ISomeInterface<T> {}
interface ISomeInterface2<T, U> {}";

            var expectedCode = @"
class Program<T, U> : ISomeInterface<T>, ISomeInterface2<T, U>, IProgram<T, U>
{
    public void Foo(T t, U u) { }
}

interface ISomeInterface<T> {}
interface ISomeInterface2<T, U> {}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_CodeGen_BaseList_LargerBaseList4()
        {
            var markup = @"
class Program<T, U> : ISomeInterface<T>, ISomeInterface2<T, U> where T : U
{
    $$public void Foo(T t, U u) { }
}

interface ISomeInterface<T> {}
interface ISomeInterface2<T, U> {}";

            var expectedCode = @"
class Program<T, U> : ISomeInterface<T>, ISomeInterface2<T, U>, IProgram<T, U> where T : U
{
    public void Foo(T t, U u) { }
}

interface ISomeInterface<T> {}
interface ISomeInterface2<T, U> {}";

            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedUpdatedOriginalDocumentCode: expectedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_TypeDiscovery_NameOnly1()
        {
            var markup = @"
interface ISomeInterface<T> {}
class Program<T, U> : ISomeInterface<T> where T : U
{
    $$public void Foo(T t, U u) { }
}";

            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_TypeDiscovery_NameOnly2()
        {
            var markup = @"
interface ISomeInterface<T> {}
class Program<T, U> $$: ISomeInterface<T> where T : U
{
    public void Foo(T t, U u) { }
}";

            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_TypeDiscovery_NameOnly3()
        {
            var markup = @"
interface ISomeInterface<T> {}
class$$ Program<T, U> : ISomeInterface<T> where T : U
{
    public void Foo(T t, U u) { }
}";

            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_TypeDiscovery_NameOnly4()
        {
            var markup = @"
interface ISomeInterface<T> {}
class Program<T, U>$$ : ISomeInterface<T> where T : U
{
    public void Foo(T t, U u) { }
}";

            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_TypeDiscovery_NameOnly5()
        {
            var markup = @"
interface ISomeInterface<T> {}
class Program  $$ <T, U> : ISomeInterface<T> where T : U
{
    public void Foo(T t, U u) { }
}";

            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_TypeDiscovery_NameOnly6()
        {
            var markup = @"
interface ISomeInterface<T> {}
class $$Program   <T, U> : ISomeInterface<T> where T : U
{
    public void Foo(T t, U u) { }
}";

            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_TypeDiscovery_NameOnly7()
        {
            var markup = @"
interface ISomeInterface<T> {}
class $$Program : ISomeInterface<object>
{
    public void Foo() { }
}";

            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_TypeDiscovery_NameOnly8()
        {
            var markup = @"
interface ISomeInterface<T> {}
class Program$$ : ISomeInterface<object>
{
    public void Foo() { }
}";

            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_TypeDiscovery_NameOnly9()
        {
            var markup = @"
interface ISomeInterface<T> {}
class$$ Program : ISomeInterface<object>
{
    public void Foo() { }
}";

            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_TypeDiscovery_NameOnly10()
        {
            var markup = @"
interface ISomeInterface<T> {}
class Program $$: ISomeInterface<object>
{
    public void Foo() { }
}";

            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable: false);
        }

        private static void TestTypeDiscovery(
            string markup,
            TypeDiscoveryRule typeDiscoveryRule,
            bool expectedExtractable)
        {
            using (var testState = new ExtractInterfaceTestState(markup, LanguageNames.CSharp, compilationOptions: null))
            {
                var result = testState.GetTypeAnalysisResult(typeDiscoveryRule);
                Assert.Equal(expectedExtractable, result.CanExtractInterface);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_GeneratedNameTypeParameterSuffix1()
        {
            var markup = @"
class $$Test<T>
{
    public void M(T a) { }
}";

            var expectedTypeParameterSuffix = @"<T>";
            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedTypeParameterSuffix: expectedTypeParameterSuffix);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_GeneratedNameTypeParameterSuffix2()
        {
            var markup = @"
class $$Test<T, U>
{
    public void M(T a) { }
}";

            var expectedTypeParameterSuffix = @"<T>";
            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedTypeParameterSuffix: expectedTypeParameterSuffix);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        public void ExtractInterface_GeneratedNameTypeParameterSuffix3()
        {
            var markup = @"
class $$Test<T, U>
{
    public void M(T a, U b) { }
}";

            var expectedTypeParameterSuffix = @"<T, U>";
            TestExtractInterfaceCommandCSharp(markup, expectedSuccess: true, expectedTypeParameterSuffix: expectedTypeParameterSuffix);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractInterface)]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void ExtractInterfaceCommandDisabledInSubmission()
        {
            var exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(typeof(InteractiveDocumentSupportsCodeFixService)));

            using (var workspace = TestWorkspaceFactory.CreateWorkspace(XElement.Parse(@"
                <Workspace>
                    <Submission Language=""C#"" CommonReferences=""true"">  
                        public class $$C
                        {
                            public void M() { }
                        }
                    </Submission>
                </Workspace> "),
                workspaceKind: WorkspaceKind.Interactive,
                exportProvider: exportProvider))
            {
                // Force initialization.
                workspace.GetOpenDocumentIds().Select(id => workspace.GetTestDocument(id).GetTextView()).ToList();

                var textView = workspace.Documents.Single().GetTextView();

                var handler = new ExtractInterfaceCommandHandler();
                var delegatedToNext = false;
                Func<CommandState> nextHandler = () =>
                {
                    delegatedToNext = true;
                    return CommandState.Unavailable;
                };

                var state = handler.GetCommandState(new Commands.ExtractInterfaceCommandArgs(textView, textView.TextBuffer), nextHandler);
                Assert.True(delegatedToNext);
                Assert.False(state.IsAvailable);
            }
        }
    }
}
