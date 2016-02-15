// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class InterfaceImplementationTests : CSharpTestBase
    {
        /// <summary>
        /// Test the pre-checks performed by TypeSymbol.FindImplementationForInterfaceMember to
        /// short-circuit interface mapping.  Should be the same for methods, properties, and
        /// events since it never gets to the interface mapping code.
        /// </summary>
        [Fact]
        public void TestObviousNulls()
        {
            var text = @"
class Base
{
    public void Method() { }
    public int Property { get { return 0; } }
    public int this[int x] { get { return 0; } }
    public event Delegate Event;
    public int Field;
    public interface Interface { }
    public class Class { }
    public struct Struct { }
    public enum Enum { Element }
    public delegate void Delegate();
}

interface Interface
{
    void Method();
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @base = (NamedTypeSymbol)global.GetMembers("Base").Single();

            var baseMethod = @base.GetMembers("Method").Single();
            Assert.Null(@base.FindImplementationForInterfaceMember(baseMethod)); //containing type is not an interface

            var baseProperty = @base.GetMember<PropertySymbol>("Property");
            Assert.Null(@base.FindImplementationForInterfaceMember(baseProperty)); //containing type is not an interface

            var baseIndexer = @base.Indexers.Single();
            Assert.Null(@base.FindImplementationForInterfaceMember(baseIndexer)); //containing type is not an interface

            var baseEvent = @base.GetMember<EventSymbol>("Event");
            Assert.Null(@base.FindImplementationForInterfaceMember(baseEvent)); //containing type is not an interface

            var baseField = @base.GetMembers("Field").Single();
            Assert.Null(@base.FindImplementationForInterfaceMember(baseField)); //not a method/property/event

            var baseNestedInterface = @base.GetMembers("Interface").Single();
            Assert.Null(@base.FindImplementationForInterfaceMember(baseNestedInterface)); //not a method/property/event

            var baseNestedClass = @base.GetMembers("Class").Single();
            Assert.Null(@base.FindImplementationForInterfaceMember(baseNestedClass)); //not a method/property/event

            var baseNestedStruct = @base.GetMembers("Struct").Single();
            Assert.Null(@base.FindImplementationForInterfaceMember(baseNestedStruct)); //not a method/property/event

            var baseNestedEnum = @base.GetMembers("Enum").Single();
            Assert.Null(@base.FindImplementationForInterfaceMember(baseNestedEnum)); //not a method/property/event

            var baseNestedDelegate = @base.GetMembers("Delegate").Single();
            Assert.Null(@base.FindImplementationForInterfaceMember(baseNestedDelegate)); //not a method/property/event

            Assert.Throws<ArgumentNullException>(() => @base.FindImplementationForInterfaceMember(null)); //not a method/property/event

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();

            var interfaceMethod = @base.GetMembers("Method").Single();
            Assert.Null(@interface.FindImplementationForInterfaceMember(interfaceMethod)); //type is not a class or struct

            Assert.Null(@interface.FindImplementationForInterfaceMember(@interface)); //symbol containing type is null
        }

        /// <summary>
        /// 1) Explicit implementation beats implicit implementation.
        /// 2) Explicit implementation of a declared interface's base interface.
        /// 3) Explicit implementation of a hidden interface method.
        /// </summary>
        [Fact]
        public void TestExplicitMethodImplementation()
        {
            var text = @"
interface BaseInterface
{
    void Method();
}

interface Interface : BaseInterface
{
    new void Method();
}

class Class : Interface
{
    void BaseInterface.Method() { }
    void Interface.Method() { }
    public void Method() { }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var baseInterface = (NamedTypeSymbol)global.GetMembers("BaseInterface").Single();
            var baseInterfaceMethod = baseInterface.GetMembers("Method").Single();

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceMethod = @interface.GetMembers("Method").Single();

            var @class = (NamedTypeSymbol)global.GetMembers("Class").Single();

            var classExplicitImplementationBase = (MethodSymbol)@class.GetMembers("BaseInterface.Method").Single();
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, classExplicitImplementationBase.MethodKind);

            var classExplicitImplementation = (MethodSymbol)@class.GetMembers("Interface.Method").Single();
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, classExplicitImplementation.MethodKind);

            var classImplicitImplementation = (MethodSymbol)@class.GetMembers("Method").Single();
            Assert.Equal(MethodKind.Ordinary, classImplicitImplementation.MethodKind);

            Assert.NotSame(classImplicitImplementation, classExplicitImplementation);
            Assert.NotSame(classImplicitImplementation, classExplicitImplementationBase);

            var implementingMethodBase = @class.FindImplementationForInterfaceMember(baseInterfaceMethod);
            Assert.Same(classExplicitImplementationBase, implementingMethodBase);

            var implementingMethod = @class.FindImplementationForInterfaceMember(interfaceMethod);
            Assert.Same(classExplicitImplementation, implementingMethod);
        }

        /// <summary>
        /// 1) Explicit implementation beats implicit implementation.
        /// 2) Explicit implementation of a declared interface's base interface.
        /// 3) Explicit implementation of a hidden interface method.
        /// </summary>
        [Fact]
        public void TestExplicitIndexerImplementation()
        {
            var text = @"
interface BaseInterface
{
    int this[int x] { get; }
}

interface Interface : BaseInterface
{
    new int this[int x] { get; }
}

class Class : Interface
{
    int BaseInterface.this[int x] { get { return 0; } }
    int Interface.this[int x] { get { return 0; } }
    public int this[int x] { get { return 0; } }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var baseInterface = (NamedTypeSymbol)global.GetMembers("BaseInterface").Single();
            var baseInterfaceIndexer = baseInterface.Indexers.Single();

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceIndexer = @interface.Indexers.Single();

            var @class = (NamedTypeSymbol)global.GetMembers("Class").Single();

            var classExplicitImplementationBase = @class.GetProperty("BaseInterface.this[]");
            var classExplicitImplementation = @class.GetProperty("Interface.this[]");
            var classImplicitImplementation = @class.GetProperty("this[]");

            Assert.NotSame(classImplicitImplementation, classExplicitImplementation);
            Assert.NotSame(classImplicitImplementation, classExplicitImplementationBase);

            var implementingMethodBase = @class.FindImplementationForInterfaceMember(baseInterfaceIndexer);
            Assert.Same(classExplicitImplementationBase, implementingMethodBase);

            var implementingMethod = @class.FindImplementationForInterfaceMember(interfaceIndexer);
            Assert.Same(classExplicitImplementation, implementingMethod);
        }

        /// <summary>
        /// 1) Implicit implementation of a declared interface.
        /// 2) Implicit implementation of a declared interface's base interface.
        /// 3) Implicit implementation of more than one interface method.
        /// </summary>
        [Fact]
        public void TestImplicitMethodImplementation()
        {
            var text1 = @"
public interface BaseInterface1
{
    void BaseMethod();
}

public interface BaseInterface2
{
    void BaseMethod();
}
";
            var text2 = @"
public interface Interface : BaseInterface1, BaseInterface2
{
    void Method();
}
";
            var text3 = @"
class Class : Interface
{
    public void Method() { }
    public void BaseMethod() { }
}
";

            var comp1 = CreateCompilationWithMscorlib(text1);
            var comp1ref = new CSharpCompilationReference(comp1);
            var refs = new System.Collections.Generic.List<MetadataReference>() { comp1ref };

            var comp2 = CreateCompilationWithMscorlib(text2, references: refs, assemblyName: "Test2");
            var comp2ref = new CSharpCompilationReference(comp2);

            refs.Add(comp2ref);
            var comp = CreateCompilationWithMscorlib(text3, refs, assemblyName: "Test3");

            var global = comp.GlobalNamespace;

            var baseInterface1 = (NamedTypeSymbol)global.GetMembers("BaseInterface1").Single();
            var baseInterface1Method = baseInterface1.GetMembers("BaseMethod").Single();

            var baseInterface2 = (NamedTypeSymbol)global.GetMembers("BaseInterface2").Single();
            var baseInterface2Method = baseInterface2.GetMembers("BaseMethod").Single();

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceMethod = @interface.GetMembers("Method").Single();

            var @class = (NamedTypeSymbol)global.GetMembers("Class").Single();

            var classImplicitImplementation = (MethodSymbol)@class.GetMembers("Method").Single();
            Assert.Equal(MethodKind.Ordinary, classImplicitImplementation.MethodKind);

            var classImplicitImplementationBase = (MethodSymbol)@class.GetMembers("BaseMethod").Single();
            Assert.Equal(MethodKind.Ordinary, classImplicitImplementationBase.MethodKind);

            var implementingMethod = @class.FindImplementationForInterfaceMember(interfaceMethod);
            Assert.Same(classImplicitImplementation, implementingMethod);

            var implementingMethodBase1 = @class.FindImplementationForInterfaceMember(baseInterface1Method);
            Assert.Same(classImplicitImplementationBase, implementingMethodBase1);

            var implementingMethodBase2 = @class.FindImplementationForInterfaceMember(baseInterface2Method);
            Assert.Same(classImplicitImplementationBase, implementingMethodBase2);
        }

        /// <summary>
        /// 1) Implicit implementation of a declared interface.
        /// 2) Implicit implementation of a declared interface's base interface.
        /// 3) Implicit implementation of more than one interface indexer.
        /// </summary>
        [Fact]
        public void TestImplicitIndexerImplementation()
        {
            var text1 = @"
public interface BaseInterface1
{
    int this[int x] { get; }
}

public interface BaseInterface2
{
    int this[int x] { get; }
}
";
            var text2 = @"
public interface Interface : BaseInterface1, BaseInterface2
{
    int this[int x, int y] { get; }
}
";
            var text3 = @"
class Class : Interface
{
    public int this[int x] { get { return 0; } }
    public int this[int x, int y] { get { return 0; } }
}
";

            var comp1 = CreateCompilationWithMscorlib(text1);
            var comp1ref = new CSharpCompilationReference(comp1);
            var refs = new System.Collections.Generic.List<MetadataReference>() { comp1ref };

            var comp2 = CreateCompilationWithMscorlib(text2, references: refs, assemblyName: "Test2");
            var comp2ref = new CSharpCompilationReference(comp2);

            refs.Add(comp2ref);
            var comp = CreateCompilationWithMscorlib(text3, refs, assemblyName: "Test3");

            var global = comp.GlobalNamespace;

            var baseInterface1 = (NamedTypeSymbol)global.GetMembers("BaseInterface1").Single();
            var baseInterface1Indexer = baseInterface1.Indexers.Single();

            var baseInterface2 = (NamedTypeSymbol)global.GetMembers("BaseInterface2").Single();
            var baseInterface2Indexer = baseInterface2.Indexers.Single();

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceIndexer = @interface.Indexers.Single();

            var @class = (NamedTypeSymbol)global.GetMembers("Class").Single();
            var classImplicitImplementation = @class.Indexers.Single(p => p.Parameters.Length == 2);
            var classImplicitImplementationBase = @class.Indexers.Single(p => p.Parameters.Length == 1);

            var implementingIndexer = @class.FindImplementationForInterfaceMember(interfaceIndexer);
            Assert.Same(classImplicitImplementation, implementingIndexer);

            var implementingIndexerBase1 = @class.FindImplementationForInterfaceMember(baseInterface1Indexer);
            Assert.Same(classImplicitImplementationBase, implementingIndexerBase1);

            var implementingIndexerBase2 = @class.FindImplementationForInterfaceMember(baseInterface2Indexer);
            Assert.Same(classImplicitImplementationBase, implementingIndexerBase2);
        }

        /// <summary>
        /// Tests classes that nearly, but do not actually implement interface methods.
        /// </summary>
        [Fact]
        public void TestImplicitMethodImplementationMismatches()
        {
            //UNDONE: type constraint mismatch

            var text = @"
interface Interface
{
    void Method<T>(long l, int i);
}

class Class1 : Interface
{
    private void Method<T>(long l, int i) { } //non-public methods don't participate
    public void Method<T, U>(long l, int i) { } //wrong arity
    public void Method(long l, int i) { } //wrong arity
    public int Method<T>(long l, int i) { } //wrong return type
    public void Method1<T>(long l, int i) { } //wrong name
    public void Method<T>(long l) { } //wrong parameter count
    public void Method<T>(int i, long l) { } //wrong parameter types
    public void Method<T>(long l, ref int i) { } //wrong parameter ref kind
}

class Class2 : Interface
{
    public static void Method<T>(T t, int i) { } //static methods don't participate
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceMethod = @interface.GetMembers("Method").Single();

            var class1 = (NamedTypeSymbol)global.GetMembers("Class1").Single();
            Assert.Null(class1.FindImplementationForInterfaceMember(interfaceMethod));

            var class2 = (NamedTypeSymbol)global.GetMembers("Class2").Single();
            Assert.Null(class2.FindImplementationForInterfaceMember(interfaceMethod));
        }

        /// <summary>
        /// Class implements interface method via explicit implementation in base class.
        /// </summary>
        [Fact]
        public void TestExplicitMethodImplementationInBase()
        {
            var text1 = @"
public interface BaseInterface
{
    void Method();
}

public interface Interface : BaseInterface
{
    new void Method();
}
";
            var text2 = @"
public class BaseClass : Interface
{
    void BaseInterface.Method() { }
    void Interface.Method() { }
    public void Method() { }
}
";
            var text3 = @"
class Class1 : BaseClass, Interface //declares Interface
{
}

class Class2 : BaseClass //does not declare interface
{
}
";

            var comp1 = CreateCompilationWithMscorlib(text1);
            var comp1ref = new CSharpCompilationReference(comp1);
            var refs = new System.Collections.Generic.List<MetadataReference>() { comp1ref };

            var comp2 = CreateCompilationWithMscorlib(text2, references: refs, assemblyName: "Test2");
            var comp2ref = new CSharpCompilationReference(comp2);

            refs.Add(comp2ref);
            var comp = CreateCompilationWithMscorlib(text3, refs, assemblyName: "Test3");

            var global = comp.GlobalNamespace;

            var baseInterface = (NamedTypeSymbol)global.GetMembers("BaseInterface").Single();
            var baseInterfaceMethod = baseInterface.GetMembers("Method").Single();

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceMethod = @interface.GetMembers("Method").Single();

            var baseClass = (NamedTypeSymbol)global.GetMembers("BaseClass").Single();

            var baseClassExplicitImplementationBase = (MethodSymbol)baseClass.GetMembers("BaseInterface.Method").Single();
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, baseClassExplicitImplementationBase.MethodKind);

            var baseClassExplicitImplementation = (MethodSymbol)baseClass.GetMembers("Interface.Method").Single();
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, baseClassExplicitImplementation.MethodKind);

            var baseClassImplicitImplementation = (MethodSymbol)baseClass.GetMembers("Method").Single();
            Assert.Equal(MethodKind.Ordinary, baseClassImplicitImplementation.MethodKind);

            Assert.NotSame(baseClassImplicitImplementation, baseClassExplicitImplementation);
            Assert.NotSame(baseClassImplicitImplementation, baseClassExplicitImplementationBase);

            var class1 = (NamedTypeSymbol)global.GetMembers("Class1").Single();

            var class1ImplementingMethodBase = class1.FindImplementationForInterfaceMember(baseInterfaceMethod);
            Assert.Same(baseClassExplicitImplementationBase, class1ImplementingMethodBase);

            var class1ImplementingMethod = class1.FindImplementationForInterfaceMember(interfaceMethod);
            Assert.Same(baseClassExplicitImplementation, class1ImplementingMethod);

            var class2 = (NamedTypeSymbol)global.GetMembers("Class2").Single();

            var class2ImplementingMethodBase = class2.FindImplementationForInterfaceMember(baseInterfaceMethod);
            Assert.Same(baseClassExplicitImplementationBase, class2ImplementingMethodBase);

            var class2ImplementingMethod = class2.FindImplementationForInterfaceMember(interfaceMethod);
            Assert.Same(baseClassExplicitImplementation, class2ImplementingMethod);
        }

        /// <summary>
        /// Class implements interface method via implicit implementation in base class.
        /// </summary>
        [Fact]
        public void TestImplicitMethodImplementationInBase()
        {
            var text = @"
interface BaseInterface1
{
    void BaseMethod();
}

interface BaseInterface2
{
    void BaseMethod();
}

interface Interface : BaseInterface1, BaseInterface2
{
    void Method();
}

class BaseClass : Interface
{
    public void Method() { }
    public void BaseMethod() { }
}

class Class1 : BaseClass, Interface //declares Interface
{
}

class Class2 : BaseClass //does not declare interface
{
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var baseInterface1 = (NamedTypeSymbol)global.GetMembers("BaseInterface1").Single();
            var baseInterface1Method = baseInterface1.GetMembers("BaseMethod").Single();

            var baseInterface2 = (NamedTypeSymbol)global.GetMembers("BaseInterface2").Single();
            var baseInterface2Method = baseInterface2.GetMembers("BaseMethod").Single();

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceMethod = @interface.GetMembers("Method").Single();

            var baseClass = (NamedTypeSymbol)global.GetMembers("BaseClass").Single();

            var baseClassImplicitImplementation = (MethodSymbol)baseClass.GetMembers("Method").Single();
            Assert.Equal(MethodKind.Ordinary, baseClassImplicitImplementation.MethodKind);

            var baseClassImplicitImplementationBase = (MethodSymbol)baseClass.GetMembers("BaseMethod").Single();
            Assert.Equal(MethodKind.Ordinary, baseClassImplicitImplementationBase.MethodKind);

            var class1 = (NamedTypeSymbol)global.GetMembers("Class1").Single();

            var class1ImplementingMethod = class1.FindImplementationForInterfaceMember(interfaceMethod);
            Assert.Same(baseClassImplicitImplementation, class1ImplementingMethod);

            var class1ImplementingMethodBase1 = class1.FindImplementationForInterfaceMember(baseInterface1Method);
            Assert.Same(baseClassImplicitImplementationBase, class1ImplementingMethodBase1);

            var class1ImplementingMethodBase2 = class1.FindImplementationForInterfaceMember(baseInterface2Method);
            Assert.Same(baseClassImplicitImplementationBase, class1ImplementingMethodBase2);

            var class2 = (NamedTypeSymbol)global.GetMembers("Class2").Single();

            var class2ImplementingMethod = class2.FindImplementationForInterfaceMember(interfaceMethod);
            Assert.Same(baseClassImplicitImplementation, class2ImplementingMethod);

            var class2ImplementingMethodBase1 = class2.FindImplementationForInterfaceMember(baseInterface1Method);
            Assert.Same(baseClassImplicitImplementationBase, class2ImplementingMethodBase1);

            var class2ImplementingMethodBase2 = class2.FindImplementationForInterfaceMember(baseInterface2Method);
            Assert.Same(baseClassImplicitImplementationBase, class2ImplementingMethodBase2);
        }

        /// <summary>
        /// Class implements interface method via implicit implementation in base class (which does not implement the interface).
        /// </summary>
        [Fact]
        public void TestImplicitMethodImplementationViaBase()
        {
            var text = @"
interface Interface
{
    void Method();
}

class BaseClass
{
    public void Method() { }
}

class Class1 : BaseClass, Interface //declares Interface
{
}

class Class2 : BaseClass //does not declare interface
{
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceMethod = @interface.GetMembers("Method").Single();

            var baseClass = (NamedTypeSymbol)global.GetMembers("BaseClass").Single();
            Assert.False(baseClass.AllInterfaces.Contains(@interface));

            var baseClassMethod = (MethodSymbol)baseClass.GetMembers("Method").Single();
            Assert.Equal(MethodKind.Ordinary, baseClassMethod.MethodKind);

            var class1 = (NamedTypeSymbol)global.GetMembers("Class1").Single();
            Assert.Same(baseClass, class1.BaseType);
            Assert.True(class1.Interfaces.Contains(@interface));

            var class2 = (NamedTypeSymbol)global.GetMembers("Class2").Single();
            Assert.Same(baseClass, class2.BaseType);
            Assert.False(class2.AllInterfaces.Contains(@interface));

            Assert.Null(baseClass.FindImplementationForInterfaceMember(interfaceMethod));
            Assert.Same(baseClassMethod, class1.FindImplementationForInterfaceMember(interfaceMethod));
            Assert.Null(class2.FindImplementationForInterfaceMember(interfaceMethod));
        }

        /// <summary>
        /// Class implements interface indexer via implicit implementation in base class (which does not implement the interface).
        /// </summary>
        [Fact]
        public void TestImplicitIndexerImplementationViaBase()
        {
            var text = @"
interface Interface
{
    int this[int x] { get; }
}

class BaseClass
{
    public int this[int x] { get { return 0; } }
}

class Class1 : BaseClass, Interface //declares Interface
{
}

class Class2 : BaseClass //does not declare interface
{
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceIndexer = @interface.Indexers.Single();

            var baseClass = (NamedTypeSymbol)global.GetMembers("BaseClass").Single();
            Assert.False(baseClass.AllInterfaces.Contains(@interface));

            var baseClassIndexer = baseClass.Indexers.Single();

            var class1 = (NamedTypeSymbol)global.GetMembers("Class1").Single();
            Assert.Same(baseClass, class1.BaseType);
            Assert.True(class1.Interfaces.Contains(@interface));

            var class2 = (NamedTypeSymbol)global.GetMembers("Class2").Single();
            Assert.Same(baseClass, class2.BaseType);
            Assert.False(class2.AllInterfaces.Contains(@interface));

            Assert.Null(baseClass.FindImplementationForInterfaceMember(interfaceIndexer));
            Assert.Same(baseClassIndexer, class1.FindImplementationForInterfaceMember(interfaceIndexer));
            Assert.Null(class2.FindImplementationForInterfaceMember(interfaceIndexer));
        }

        /// <summary>
        /// Test remapping of an explicitly implemented interface.
        /// </summary>
        [Fact]
        public void TestExplicitMethodImplementationRemapping()
        {
            var text = @"
interface Interface
{
    void Method();
}

class BaseClass : Interface
{
    void Interface.Method() { }
}

class Class1 : BaseClass, Interface //declares Interface
{
    public void Method() { }
}

class Class2 : BaseClass //does not declare interface
{
    public void Method() { }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceMethod = @interface.GetMembers("Method").Single();

            var baseClass = (NamedTypeSymbol)global.GetMembers("BaseClass").Single();
            var baseClassMethod = (MethodSymbol)baseClass.GetMembers("Interface.Method").Single();
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, baseClassMethod.MethodKind);

            var baseClassImplementingMethod = baseClass.FindImplementationForInterfaceMember(interfaceMethod);
            Assert.Same(baseClassMethod, baseClassImplementingMethod);

            var class1 = (NamedTypeSymbol)global.GetMembers("Class1").Single();
            var class1Method = (MethodSymbol)class1.GetMembers("Method").Single();
            Assert.Equal(MethodKind.Ordinary, class1Method.MethodKind);

            var class1ImplementingMethod = class1.FindImplementationForInterfaceMember(interfaceMethod);
            Assert.Same(class1Method, class1ImplementingMethod);
            Assert.NotSame(baseClassMethod, class1ImplementingMethod);

            var class2 = (NamedTypeSymbol)global.GetMembers("Class2").Single();
            var class2Method = (MethodSymbol)class2.GetMembers("Method").Single();
            Assert.Equal(MethodKind.Ordinary, class2Method.MethodKind);

            var class2ImplementingMethod = class2.FindImplementationForInterfaceMember(interfaceMethod);
            Assert.Same(baseClassMethod, class2ImplementingMethod);
            Assert.NotSame(class2Method, class1ImplementingMethod);
        }

        /// <summary>
        /// Test remapping of an implicitly implemented interface.
        /// </summary>
        [Fact]
        public void TestImplicitMethodImplementationRemapping()
        {
            var text = @"
interface Interface
{
    void Virtual();
    void NonVirtual();
}

class BaseClass : Interface
{
    public virtual void Virtual() { }
    public void NonVirtual() { }
}

class Class1 : BaseClass, Interface //declares Interface
{
    public override void Virtual() { }
    public new void NonVirtual() { }
}

class Class2 : BaseClass //does not declare interface
{
    public override void Virtual() { }
    public new void NonVirtual() { }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceMethodVirtual = @interface.GetMembers("Virtual").Single();
            var interfaceMethodNonVirtual = @interface.GetMembers("NonVirtual").Single();

            var baseClass = (NamedTypeSymbol)global.GetMembers("BaseClass").Single();

            var baseClassMethodVirtual = (MethodSymbol)baseClass.GetMembers("Virtual").Single();
            Assert.Equal(MethodKind.Ordinary, baseClassMethodVirtual.MethodKind);
            Assert.True(baseClassMethodVirtual.IsVirtual);

            var baseClassMethodNonVirtual = (MethodSymbol)baseClass.GetMembers("NonVirtual").Single();
            Assert.Equal(MethodKind.Ordinary, baseClassMethodNonVirtual.MethodKind);
            Assert.False(baseClassMethodNonVirtual.IsVirtual);

            var baseClassImplementingMethodVirtual = baseClass.FindImplementationForInterfaceMember(interfaceMethodVirtual);
            Assert.Same(baseClassMethodVirtual, baseClassImplementingMethodVirtual);

            var baseClassImplementingMethodNonVirtual = baseClass.FindImplementationForInterfaceMember(interfaceMethodNonVirtual);
            Assert.Same(baseClassMethodNonVirtual, baseClassImplementingMethodNonVirtual);

            var class1 = (NamedTypeSymbol)global.GetMembers("Class1").Single();

            var class1MethodVirtual = (MethodSymbol)class1.GetMembers("Virtual").Single();
            Assert.Equal(MethodKind.Ordinary, class1MethodVirtual.MethodKind);
            Assert.True(class1MethodVirtual.IsOverride);

            var class1MethodNonVirtual = (MethodSymbol)class1.GetMembers("NonVirtual").Single();
            Assert.Equal(MethodKind.Ordinary, class1MethodNonVirtual.MethodKind);
            Assert.False(class1MethodNonVirtual.IsOverride);

            var class1ImplementingMethodVirtual = class1.FindImplementationForInterfaceMember(interfaceMethodVirtual);
            Assert.Same(class1MethodVirtual, class1ImplementingMethodVirtual);
            Assert.NotSame(baseClassMethodVirtual, class1ImplementingMethodVirtual);

            var class1ImplementingMethodNonVirtual = class1.FindImplementationForInterfaceMember(interfaceMethodNonVirtual);
            Assert.Same(class1MethodNonVirtual, class1ImplementingMethodNonVirtual);
            Assert.NotSame(baseClassMethodNonVirtual, class1ImplementingMethodNonVirtual);

            var class2 = (NamedTypeSymbol)global.GetMembers("Class2").Single();

            var class2MethodVirtual = (MethodSymbol)class2.GetMembers("Virtual").Single();
            Assert.Equal(MethodKind.Ordinary, class2MethodVirtual.MethodKind);
            Assert.True(class2MethodVirtual.IsOverride);

            var class2MethodNonVirtual = (MethodSymbol)class2.GetMembers("NonVirtual").Single();
            Assert.Equal(MethodKind.Ordinary, class2MethodNonVirtual.MethodKind);
            Assert.False(class2MethodNonVirtual.IsOverride);

            var class2ImplementingMethodVirtual = class2.FindImplementationForInterfaceMember(interfaceMethodVirtual);
            Assert.Same(baseClassMethodVirtual, class2ImplementingMethodVirtual);
            Assert.NotSame(class2MethodVirtual, class2ImplementingMethodVirtual);

            var class2ImplementingMethodNonVirtual = class2.FindImplementationForInterfaceMember(interfaceMethodNonVirtual);
            Assert.Same(baseClassMethodNonVirtual, class2ImplementingMethodNonVirtual);
            Assert.NotSame(class2MethodNonVirtual, class2ImplementingMethodNonVirtual);
        }

        /// <summary>
        /// Test remapping of an implicitly implemented interface in a longer chain of types.
        /// </summary>
        [Fact]
        public void TestImplicitMethodImplementationRemapping2()
        {
            var text = @"
interface Interface
{
    void Method();
}

class NonDeclaringClass1
{
    public void Method() { }
}

class DeclaringClass1 : NonDeclaringClass1, Interface
{
}

class NonDeclaringClass2 : DeclaringClass1
{
    public new void Method() { }
}

class DeclaringClass2 : NonDeclaringClass2, Interface
{
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceMethod = @interface.GetMembers("Method").Single();

            var nonDeclaring1 = (NamedTypeSymbol)global.GetMembers("NonDeclaringClass1").Single();
            Assert.False(nonDeclaring1.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Contains(@interface));

            var nonDeclaring1Method = nonDeclaring1.GetMembers("Method").Single();

            var declaring1 = (NamedTypeSymbol)global.GetMembers("DeclaringClass1").Single();
            Assert.True(declaring1.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Contains(@interface));
            Assert.Equal(nonDeclaring1, declaring1.BaseType);

            var nonDeclaring2 = (NamedTypeSymbol)global.GetMembers("NonDeclaringClass2").Single();
            Assert.False(nonDeclaring2.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Contains(@interface));
            Assert.Equal(declaring1, nonDeclaring2.BaseType);

            var nonDeclaring2Method = nonDeclaring2.GetMembers("Method").Single();

            var declaring2 = (NamedTypeSymbol)global.GetMembers("DeclaringClass2").Single();
            Assert.True(declaring2.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Contains(@interface));
            Assert.Equal(nonDeclaring2, declaring2.BaseType);

            Assert.Null(nonDeclaring1.FindImplementationForInterfaceMember(interfaceMethod));
            Assert.Equal(nonDeclaring1Method, declaring1.FindImplementationForInterfaceMember(interfaceMethod));
            Assert.Equal(nonDeclaring1Method, nonDeclaring2.FindImplementationForInterfaceMember(interfaceMethod));
            Assert.Equal(nonDeclaring2Method, declaring2.FindImplementationForInterfaceMember(interfaceMethod));
        }

        /// <summary>
        /// In metadata, it is possible for a type to explicitly implement a method of an interface
        /// declared by its base type (even if it does not declare the interface itself).
        /// </summary>
        [Fact]
        public void TestExplicitMethodImplementationOnNonDeclaringType()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.IL,
                });

            var global = assemblies.ElementAt(1).GlobalNamespace;

            var @interface = (NamedTypeSymbol)global.GetMembers("I1").Single();
            var interfaceMethod = @interface.GetMembers("Method1").Single();

            var baseClass = (NamedTypeSymbol)global.GetMembers("BaseDeclaresInterface").Single();
            Assert.True(baseClass.Interfaces.Contains(@interface));
            Assert.Null(baseClass.FindImplementationForInterfaceMember(interfaceMethod));

            var derivedClass = (NamedTypeSymbol)global.GetMembers("DerivedExplicitlyImplementsInterface").Single();
            Assert.False(derivedClass.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Contains(@interface));
            Assert.True(derivedClass.AllInterfaces.Contains(@interface));

            var derivedClassMethod = derivedClass.GetMembers("I1.Method1").Single();
            Assert.Same(derivedClassMethod, derivedClass.FindImplementationForInterfaceMember(interfaceMethod));
        }

        [Fact]
        public void TestNonVirtualImplicitImplementationSameAssembly()
        {
            var text = @"
public interface Interface
{
    void Method();
    int Property { get; set; }
}

public class Base
{
    public void Method() { }
    public int Property { get; set; }
}

public class Derived : Base, Interface
{
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            Assert.False(comp.GetDiagnostics().Any(), string.Join("\n", comp.GetDiagnostics()));

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceMethod = (MethodSymbol)@interface.GetMembers("Method").Single();
            var interfaceProperty = (PropertySymbol)@interface.GetMembers("Property").Single();
            var interfacePropertyGetter = interfaceProperty.GetMethod;
            var interfacePropertySetter = interfaceProperty.SetMethod;

            var baseClass = (NamedTypeSymbol)global.GetMembers("Base").Single();
            Assert.False(baseClass.AllInterfaces.Contains(@interface));

            var baseClassMethod = (MethodSymbol)baseClass.GetMembers("Method").Single();
            var baseClassProperty = (PropertySymbol)baseClass.GetMembers("Property").Single();
            var baseClassPropertyGetter = baseClassProperty.GetMethod;
            var baseClassPropertySetter = baseClassProperty.SetMethod;

            Assert.False(baseClassMethod.IsVirtual);
            Assert.False(baseClassProperty.IsVirtual);
            Assert.False(baseClassPropertyGetter.IsVirtual);
            Assert.False(baseClassPropertySetter.IsVirtual);

            var derivedClass = (SourceNamedTypeSymbol)global.GetMembers("Derived").Single();
            Assert.True(derivedClass.Interfaces.Contains(@interface));

            Assert.Same(baseClassMethod, derivedClass.FindImplementationForInterfaceMember(interfaceMethod));
            Assert.Same(baseClassProperty, derivedClass.FindImplementationForInterfaceMember(interfaceProperty));
            Assert.Same(baseClassPropertyGetter, derivedClass.FindImplementationForInterfaceMember(interfacePropertyGetter));
            Assert.Same(baseClassPropertySetter, derivedClass.FindImplementationForInterfaceMember(interfacePropertySetter));

            Assert.True(((Cci.IMethodDefinition)baseClassMethod).IsVirtual);
            Assert.True(((Cci.IMethodDefinition)baseClassPropertyGetter).IsVirtual);
            Assert.True(((Cci.IMethodDefinition)baseClassPropertySetter).IsVirtual);

            Assert.False(derivedClass.GetSynthesizedExplicitImplementations(CancellationToken.None).Any());
        }

        [Fact]
        public void TestNonVirtualImplicitImplementationOtherAssembly()
        {
            var text1 = @"
public interface Interface
{
    void Method();
    int Property { get; set; }
}

public class Base
{
    public void Method() { }
    public int Property { get; set; }
}
";

            var text2 = @"
public class Derived : Base, Interface
{
}
";
            var comp1 = CreateCompilationWithMscorlib(text1,
                assemblyName: "OtherAssembly",
                options: TestOptions.ReleaseDll);
            Assert.False(comp1.GetDiagnostics().Any(), string.Join("\n", comp1.GetDiagnostics()));

            var comp2 = CreateCompilationWithMscorlib(text2,
                references: new MetadataReference[] { new CSharpCompilationReference(comp1) },
                assemblyName: "SourceAssembly",
                options: TestOptions.ReleaseDll);
            Assert.False(comp2.GetDiagnostics().Any(), string.Join("\n", comp2.GetDiagnostics()));

            var global = comp2.GlobalNamespace;

            var @interface = (NamedTypeSymbol)global.GetMembers("Interface").Single();
            var interfaceMethod = (MethodSymbol)@interface.GetMembers("Method").Single();
            var interfaceProperty = (PropertySymbol)@interface.GetMembers("Property").Single();
            var interfacePropertyGetter = interfaceProperty.GetMethod;
            var interfacePropertySetter = interfaceProperty.SetMethod;

            var baseClass = (NamedTypeSymbol)global.GetMembers("Base").Single();
            Assert.False(baseClass.AllInterfaces.Contains(@interface));

            var baseClassMethod = (MethodSymbol)baseClass.GetMembers("Method").Single();
            var baseClassProperty = (PropertySymbol)baseClass.GetMembers("Property").Single();
            var baseClassPropertyGetter = baseClassProperty.GetMethod;
            var baseClassPropertySetter = baseClassProperty.SetMethod;

            Assert.False(baseClassMethod.IsVirtual);
            Assert.False(baseClassProperty.IsVirtual);
            Assert.False(baseClassPropertyGetter.IsVirtual);
            Assert.False(baseClassPropertySetter.IsVirtual);

            var derivedClass = (SourceNamedTypeSymbol)global.GetMembers("Derived").Single();
            Assert.True(derivedClass.Interfaces.Contains(@interface));

            Assert.Same(baseClassMethod, derivedClass.FindImplementationForInterfaceMember(interfaceMethod));
            Assert.Same(baseClassProperty, derivedClass.FindImplementationForInterfaceMember(interfaceProperty));
            Assert.Same(baseClassPropertyGetter, derivedClass.FindImplementationForInterfaceMember(interfacePropertyGetter));
            Assert.Same(baseClassPropertySetter, derivedClass.FindImplementationForInterfaceMember(interfacePropertySetter));

            Assert.False(((Cci.IMethodDefinition)baseClassMethod).IsVirtual);
            Assert.False(((Cci.IMethodDefinition)baseClassPropertyGetter).IsVirtual);
            Assert.False(((Cci.IMethodDefinition)baseClassPropertySetter).IsVirtual);

            // GetSynthesizedExplicitImplementations doesn't guarantee order, so sort to make the asserts easier to write.

            var synthesizedExplicitImpls = (from m in derivedClass.GetSynthesizedExplicitImplementations(CancellationToken.None) orderby m.MethodKind select m).ToArray();
            Assert.Equal(3, synthesizedExplicitImpls.Length);
            Assert.True(synthesizedExplicitImpls.All(s => ReferenceEquals(derivedClass, s.ContainingType)));

            Assert.Same(interfaceMethod, synthesizedExplicitImpls[0].ExplicitInterfaceImplementations.Single());
            Assert.Same(baseClassMethod, synthesizedExplicitImpls[0].ImplementingMethod);

            Assert.Same(interfacePropertyGetter, synthesizedExplicitImpls[1].ExplicitInterfaceImplementations.Single());
            Assert.Same(baseClassPropertyGetter, synthesizedExplicitImpls[1].ImplementingMethod);
            Assert.Equal(MethodKind.PropertyGet, synthesizedExplicitImpls[1].MethodKind);

            Assert.Same(interfacePropertySetter, synthesizedExplicitImpls[2].ExplicitInterfaceImplementations.Single());
            Assert.Same(baseClassPropertySetter, synthesizedExplicitImpls[2].ImplementingMethod);
            Assert.Equal(MethodKind.PropertySet, synthesizedExplicitImpls[2].MethodKind);
        }

        /// <summary>
        /// Layout:
        /// D : C : B : A
        /// All have virtual Method1 and Method2 with the same signatures (modulo custom modifiers)
        /// D has 2 custom modifiers
        /// C has 1 custom modifier
        /// B has 2 custom modifiers, but not the same as D
        /// A has 1 custom modifier for Method1, but not the same as C, and 0 custom modifiers for Method2
        /// </summary>
        [Fact]
        public void TestCustomModifierImplicitImplementation()
        {
            var text = @"
interface Interface
{
    void Method1(int[] x);
    void Method2(int[] x);
}

class Class : CustomModifierOverridingD, Interface
{
}
";
            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll;

            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            Assert.False(comp.GetDiagnostics().Any());

            //IL
            var classD = global.GetTypeMembers("CustomModifierOverridingD").Single();

            var classDMethod1 = (MethodSymbol)classD.GetMembers("Method1").Single();
            var classDMethod2 = (MethodSymbol)classD.GetMembers("Method2").Single();

            //Source
            var @interface = global.GetTypeMembers("Interface").Single();
            var interfaceMethod1 = (MethodSymbol)@interface.GetMembers("Method1").Single();
            var interfaceMethod2 = (MethodSymbol)@interface.GetMembers("Method2").Single();

            var @class = (SourceNamedTypeSymbol)global.GetTypeMembers("Class").Single();

            //ignore custom modifiers (chooses most derived, even though less derived has fewer)
            var classMethod1Impl = @class.FindImplementationForInterfaceMember(interfaceMethod1);
            Assert.Same(classDMethod1, classMethod1Impl);

            //ignore custom modifiers (chooses most derived, even though less derived is exact)
            var classMethod2Impl = @class.FindImplementationForInterfaceMember(interfaceMethod2);
            Assert.Same(classDMethod2, classMethod2Impl);

            // GetSynthesizedExplicitImplementations doesn't guarantee order, so sort to make the asserts easier to write.

            var synthesizedExplicitImpls = (from m in @class.GetSynthesizedExplicitImplementations(CancellationToken.None) orderby m.Name select m).ToArray();
            Assert.Equal(2, synthesizedExplicitImpls.Length);

            var synthesizedExplicitMethod1Impl = synthesizedExplicitImpls[0];
            Assert.Same(interfaceMethod1, synthesizedExplicitMethod1Impl.ExplicitInterfaceImplementations.Single());
            Assert.Same(classDMethod1, synthesizedExplicitMethod1Impl.ImplementingMethod);
            Assert.Same(@class, synthesizedExplicitMethod1Impl.ContainingType);

            var synthesizedExplicitMethod2Impl = synthesizedExplicitImpls[1];
            Assert.Same(interfaceMethod2, synthesizedExplicitMethod2Impl.ExplicitInterfaceImplementations.Single());
            Assert.Same(classDMethod2, synthesizedExplicitMethod2Impl.ImplementingMethod);
            Assert.Same(@class, synthesizedExplicitMethod2Impl.ContainingType);
        }

        [Fact]
        public void TestExplicitImplementationOfStaticMethod()
        {
            var text = @"
class Class : ContainsStatic
{
    void ContainsStatic.Bar() { }
    void ContainsStatic.StaticMethod() { } //CS0539
}
";
            var ilAssemblyReference = TestReferences.SymbolsTests.Interface.StaticMethodInInterface;

            var comp = CreateCompilationWithMscorlib(text, new[] { ilAssemblyReference });

            comp.VerifyDiagnostics(
                // (5,25): error CS0539: 'Class.StaticMethod()' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "StaticMethod").WithArguments("Class.StaticMethod()"));
        }

        [Fact]
        public void TestNoImplementationOfStaticMethod()
        {
            var text = @"
class Class : ContainsStatic
{
    void ContainsStatic.Bar() { }
}
";
            var ilAssemblyReference = TestReferences.SymbolsTests.Interface.StaticMethodInInterface;

            var comp = CreateCompilationWithMscorlib(text, new[] { ilAssemblyReference });

            comp.VerifyDiagnostics();
        }
        [Fact]
        public void MultiLevelPropertyImplementation()
        {
            var text = @"
interface I1
{
    int bar { get; set; }
}
public class c1
{
    public virtual int bar { get { return 1; } set { } }
}
public class c2 : c1, I1
{
    public override int bar { get { return 2; } }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var type = comp.GlobalNamespace.GetTypeMembers("c2").Single();
            Assert.NotEmpty(type.Interfaces);
            Assert.True(type.Interfaces.Any(@interface => @interface.Name == "I1"));
        }

        [Fact]
        public void TestImplementInterfaceInpartialClass()
        {
            var text = @"
interface Interface
{
    void Method1();
}
partial class Base : Interface
{
    public void Method2() { }
}
partial class Base
{
    public void Method1() { }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [WorkItem(540451, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540451")]
        /// <summary>
        /// I -> M(ref int)
        /// B -> M(out int)
        /// D : B, I
        /// 
        /// I source, B source, D source
        /// </summary>
        [Fact]
        public void TestSourceMetadataImplicitImplementation1()
        {
            var csharp = @"
interface Interface
{
    void M(ref int x);
}

class Base
{
    void M(out int x)
    {
        x = 1;
    }
}

class Derived : Base, Interface
{
}

class Program
{
    static void Main()
    {
        Interface id = new Derived();
        int x = 2;
        id.M(ref x);
    }
}
";

            var comp = CreateCompilationWithMscorlib(csharp);
            comp.VerifyDiagnostics(
                // (15,7): error CS0535: 'Derived' does not implement interface member 'Interface.M(ref int)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Derived", "Interface.M(ref int)"));
            var global = comp.GlobalNamespace;
            Assert.Null(global.GetMember<NamedTypeSymbol>("Derived").FindImplementationForInterfaceMember(
                global.GetMember<NamedTypeSymbol>("Interface").GetMember<MethodSymbol>("M")));
        }

        // I source, B source, D metadata - skip: metadata implementing source
        // public void TestSourceMetadataImplicitImplementation2()

        [WorkItem(540451, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540451")]
        /// <summary>
        /// I -> M(ref int)
        /// B -> M(out int)
        /// D : B, I
        /// 
        /// I source, B metadata, D source
        /// </summary>
        [Fact]
        public void TestSourceMetadataImplicitImplementation3()
        {
            var csharp = @"
interface Interface
{
    void M(ref int x);
}

class Derived : Base, Interface
{
}

class Program
{
    static void Main()
    {
        Interface id = new Derived();
        int x = 2;
        id.M(ref x);
    }
}
";
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig virtual instance void 
          M([out] int32& x) cil managed
  {
    // Code size       5 (0x5)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldc.i4.1
    IL_0003:  stind.i4
    IL_0004:  ret
  } // end of method Base::M

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base";

            var comp = CreateCompilationWithCustomILSource(csharp, il);
            comp.VerifyDiagnostics(
                // (7,7): error CS0535: 'Derived' does not implement interface member 'Interface.M(ref int)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Derived", "Interface.M(ref int)"));
            var global = comp.GlobalNamespace;
            Assert.Null(global.GetMember<NamedTypeSymbol>("Derived").FindImplementationForInterfaceMember(
                global.GetMember<NamedTypeSymbol>("Interface").GetMember<MethodSymbol>("M")));
        }

        // I source, B metadata, D metadata - skip: metadata implementing source
        // public void TestSourceMetadataImplicitImplementation4()

        [WorkItem(540451, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540451")]
        /// <summary>
        /// I -> M(ref int)
        /// B -> M(out int)
        /// D : B, I
        /// 
        /// I metadata, B source, D source
        /// </summary>
        [Fact]
        public void TestSourceMetadataImplicitImplementation5()
        {
            var csharp = @"
class Base
{
    void M(out int x)
    {
        x = 1;
    }
}

class Derived : Base, Interface
{
}

class Program
{
    static void Main()
    {
        Interface id = new Derived();
        int x = 2;
        id.M(ref x);
    }
}
";
            var il = @"
.class interface public abstract auto ansi Interface
{
  .method public hidebysig newslot abstract virtual 
          instance void  M(int32& x) cil managed
  {
  } // end of method Interface::M

} // end of class Interface";

            var comp = CreateCompilationWithCustomILSource(csharp, il);
            comp.VerifyDiagnostics(
                // (10,7): error CS0535: 'Derived' does not implement interface member 'Interface.M(ref int)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Derived", "Interface.M(ref int)"));
            var global = comp.GlobalNamespace;
            Assert.Null(global.GetMember<NamedTypeSymbol>("Derived").FindImplementationForInterfaceMember(
                global.GetMember<NamedTypeSymbol>("Interface").GetMember<MethodSymbol>("M")));
        }

        // I metadata, B source, D metadata - skip: metadata extending source
        // public void TestSourceMetadataImplicitImplementation6()

        [WorkItem(540451, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540451")]
        /// <summary>
        /// I -> M(ref int)
        /// B -> M(out int)
        /// D : B, I
        /// 
        /// I metadata, B metadata, D source
        /// </summary>
        [Fact]
        public void TestSourceMetadataImplicitImplementation7()
        {
            var csharp = @"
class Derived : Base, Interface
{
}

class Program
{
    static void Main()
    {
        Interface id = new Derived();
        int x = 2;
        id.M(ref x);
    }
}
";
            var il = @"
.class interface public abstract auto ansi Interface
{
  .method public hidebysig newslot abstract virtual 
          instance void  M(int32& x) cil managed
  {
  } // end of method Interface::M

} // end of class Interface

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig virtual instance void 
          M([out] int32& x) cil managed
  {
    // Code size       5 (0x5)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldc.i4.1
    IL_0003:  stind.i4
    IL_0004:  ret
  } // end of method Base::M

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base";

            var comp = CreateCompilationWithCustomILSource(csharp, il);
            comp.VerifyDiagnostics(
                // (2,7): error CS0535: 'Derived' does not implement interface member 'Interface.M(ref int)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Derived", "Interface.M(ref int)"));
            var global = comp.GlobalNamespace;
            Assert.Null(global.GetMember<NamedTypeSymbol>("Derived").FindImplementationForInterfaceMember(
                global.GetMember<NamedTypeSymbol>("Interface").GetMember<MethodSymbol>("M")));
        }

        [WorkItem(528858, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528858")]
        [Fact(Skip = "528858")]
        public void InconsistentTypeParameters()
        {
            var il = @"
.class interface public abstract I<T>
{
  .class interface abstract auto ansi nested public I2 {  }
}
";
            var csharp = @"
class C : I<int>.I2 { }
";

            var comp = CreateCompilationWithCustomILSource(csharp, il);
            comp.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BogusType));
        }

        [WorkItem(528901, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528901")]
        [Fact(Skip = "528901")]
        public void BaseInterfacesWithWeirdNamesCanBeImplementedThroughInterfaceInheritance()
        {
            var il = @"
.class interface public abstract 'N..A' { }
.class interface public abstract B implements 'N..A' { }
";
            var csharp = @"
class C : B { }
";

            CompileWithCustomILSource(csharp, il);
        }

        [WorkItem(540451, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540451")]
        /// <summary>
        /// I -> M(ref int)
        /// B -> M(out int)
        /// D : B, I
        /// 
        /// I source, B source, D source
        /// </summary>
        [Fact]
        public void TestSourceMetadataImplicitImplementation8()
        {
            var csharp = @"
class Program
{
    static void Main()
    {
        Interface id = new Derived();
        int x = 2;
        id.M(ref x);
    }
}
";
            var il = @"
.class interface public abstract auto ansi Interface
{
  .method public hidebysig newslot abstract virtual 
          instance void  M(int32& x) cil managed
  {
  } // end of method Interface::M

} // end of class Interface

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig virtual instance void 
          M([out] int32& x) cil managed
  {
    // Code size       5 (0x5)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldc.i4.1
    IL_0003:  stind.i4
    IL_0004:  ret
  } // end of method Base::M

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base

.class public auto ansi beforefieldinit Derived
       extends Base
       implements Interface
{

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Base::.ctor()
    IL_0006:  ret
  } // end of method Derived::.ctor

} // end of class Derived";

            var comp = CreateCompilationWithCustomILSource(csharp, il);
            comp.VerifyDiagnostics();

            // CONSIDER: dev10 probably regards the interface method as unimplemented, 
            // but it also doesn't give diagnostics for such cases and this is the behavior at runtime.
            var global = comp.GlobalNamespace;
            Assert.Equal(
                global.GetMember<NamedTypeSymbol>("Base").GetMember<MethodSymbol>("M"),
                global.GetMember<NamedTypeSymbol>("Derived").FindImplementationForInterfaceMember(
                    global.GetMember<NamedTypeSymbol>("Interface").GetMember<MethodSymbol>("M")));
        }

        [WorkItem(540451, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540451")]
        [Fact]
        public void TestImplementRefParamWithOutParam()
        {
            var text = @"
interface I1
{
    void Foo(out int x);
}
class C1 : I1
{
    public void Foo(ref int x) { }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("C1", "I1.Foo(out int)"));
        }

        [Fact]
        public void TestSameInterfaceOverloadUsingGenericArgument()
        {
            var text = @"
interface I1<T>
{
    void Foo(int x);
    void Foo(T x);
}
class C1 : I1<int>
{
    public void Foo(int x) { }
}
static class Program
{
    static void Main()
    {
        I1<int> i = new C1();
        i.Foo(0);       
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();

            var typeSymbol = comp.GlobalNamespace.GetTypeMembers("C1").Single();
            var interfaceSymbol = typeSymbol.Interfaces.First();
            var fooMethod = typeSymbol.GetMember<MethodSymbol>("Foo");

            var interfaceMembers = interfaceSymbol.GetMembers().OfType<MethodSymbol>();
            var firstInterfaceMethod = interfaceMembers.First();
            var secondInterfaceMethod = interfaceMembers.Last();
            Assert.Equal(fooMethod, typeSymbol.FindImplementationForInterfaceMember(firstInterfaceMethod));
            Assert.Equal(fooMethod, typeSymbol.FindImplementationForInterfaceMember(secondInterfaceMethod));
        }

        [WorkItem(540558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540558")]
        /// <summary>
        /// In this case, C# thinks B.M implements I.M for C, but the CLR thinks A.M does.  To make sure that we get the
        /// desired behavior, we have to insert an explicit bridge method.
        /// (See SourceNamedTypeSymbol.IsOverrideOfPossibleImplementationUnderRuntimeRules.)
        /// </summary>
        [Fact]
        public void TestCSharpClrDisagreement_NonOverride()
        {
            var text = @"
interface I 
{ 
    void M(); 
}

class A : I
{
    public virtual void M() { }
}

class B : A
{
    public new virtual void M() { }
}

class C : B, I { }
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @interface = global.GetMember<NamedTypeSymbol>("I");
            var interfaceMethod = @interface.GetMember<MethodSymbol>("M");

            var classA = global.GetMember<NamedTypeSymbol>("A");
            var classAMethod = classA.GetMember<MethodSymbol>("M");

            var classB = global.GetMember<NamedTypeSymbol>("B");
            var classBMethod = classB.GetMember<MethodSymbol>("M");

            var classC = global.GetMember<SourceNamedTypeSymbol>("C");

            Assert.Equal(@interface, classA.AllInterfaces.Single());
            Assert.Equal(@interface, classB.AllInterfaces.Single());
            Assert.Equal(@interface, classC.AllInterfaces.Single());

            Assert.Equal(0, classB.Interfaces.Length);

            Assert.Equal(classB, classC.BaseType);
            Assert.Equal(classA, classB.BaseType);

            Assert.Equal(classAMethod, classA.FindImplementationForInterfaceMember(interfaceMethod));

            Assert.Equal(classBMethod, classC.FindImplementationForInterfaceMember(interfaceMethod));

            var synthesizedExplicitImpl = classC.GetSynthesizedExplicitImplementations(CancellationToken.None).Single();
            Assert.Equal(classC, synthesizedExplicitImpl.ContainingType);
            Assert.Equal(interfaceMethod, synthesizedExplicitImpl.ExplicitInterfaceImplementations.Single());
            Assert.Equal(classBMethod, synthesizedExplicitImpl.ImplementingMethod);
        }

        [WorkItem(540558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540558")]
        /// <summary>
        /// In this case, C# thinks B.M implements I.M for C, but the CLR thinks A.M does.  However,
        /// B.M overrides A.M, so there's no problem (distinguish from TestCSharpClrDisagreement_NonOverride).
        /// (See SourceNamedTypeSymbol.IsOverrideOfPossibleImplementationUnderRuntimeRules.)
        /// </summary>
        [Fact]
        public void TestCSharpClrDisagreement_Override()
        {
            var text = @"
interface I 
{ 
    void M(); 
}

class A : I
{
    public virtual void M() { }
}

class B : A
{
    public override void M() { }
}

class C : B, I { }
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @interface = global.GetMember<NamedTypeSymbol>("I");
            var interfaceMethod = @interface.GetMember<MethodSymbol>("M");

            var classA = global.GetMember<NamedTypeSymbol>("A");
            var classAMethod = classA.GetMember<MethodSymbol>("M");

            var classB = global.GetMember<NamedTypeSymbol>("B");
            var classBMethod = classB.GetMember<MethodSymbol>("M");

            var classC = global.GetMember<SourceNamedTypeSymbol>("C");

            Assert.Equal(@interface, classA.AllInterfaces.Single());
            Assert.Equal(@interface, classB.AllInterfaces.Single());
            Assert.Equal(@interface, classC.AllInterfaces.Single());

            Assert.Equal(0, classB.Interfaces.Length);

            Assert.Equal(classB, classC.BaseType);
            Assert.Equal(classA, classB.BaseType);

            Assert.Equal(classAMethod, classA.FindImplementationForInterfaceMember(interfaceMethod));

            Assert.Equal(classBMethod, classC.FindImplementationForInterfaceMember(interfaceMethod));

            Assert.Equal(0, classC.GetSynthesizedExplicitImplementations(CancellationToken.None).Length);
        }

        [Fact]
        public void ExplicitlyImplementParameterizedProperty()
        {
            var il = @"
.class interface public abstract auto ansi I
{
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_Item(int32 x) cil managed
  {
  } // end of method I::get_Item

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Item(int32 x,
                                  int32 'value') cil managed
  {
  } // end of method I::set_Item

  .property instance int32 Item(int32)
  {
    .get instance int32 I::get_Item(int32)
    .set instance void I::set_Item(int32,
                                   int32)
  } // end of property I::Item
} // end of class I
";

            var csharp = @"
class C : I
{
    int I.get_Item(int x) { return 0; }
    void I.set_Item(int x, int value) { }
}

class D : I
{
    public int get_Item(int x) { return 0; }
    public void set_Item(int x, int value) { }
}
";

            var compilation = CreateCompilationWithCustomILSource(csharp, il);
            compilation.VerifyDiagnostics();

            var globalNamespace = compilation.GlobalNamespace;

            var @interface = globalNamespace.GetMember<NamedTypeSymbol>("I");
            var classC = globalNamespace.GetMember<NamedTypeSymbol>("C");
            var classD = globalNamespace.GetMember<NamedTypeSymbol>("D");

            var interfaceProperty = @interface.GetMember<PropertySymbol>("Item");
            Assert.False(interfaceProperty.IsIndexer);
            Assert.True(interfaceProperty.MustCallMethodsDirectly);

            var interfaceGetter = interfaceProperty.GetMethod;
            var interfaceSetter = interfaceProperty.SetMethod;

            Assert.Null(classC.FindImplementationForInterfaceMember(interfaceProperty));
            Assert.Equal("System.Int32 C.I.get_Item(System.Int32 x)", classC.FindImplementationForInterfaceMember(interfaceGetter).ToTestDisplayString());
            Assert.Equal("void C.I.set_Item(System.Int32 x, System.Int32 value)", classC.FindImplementationForInterfaceMember(interfaceSetter).ToTestDisplayString());

            Assert.Null(classD.FindImplementationForInterfaceMember(interfaceProperty));
            Assert.Equal("System.Int32 D.get_Item(System.Int32 x)", classD.FindImplementationForInterfaceMember(interfaceGetter).ToTestDisplayString());
            Assert.Equal("void D.set_Item(System.Int32 x, System.Int32 value)", classD.FindImplementationForInterfaceMember(interfaceSetter).ToTestDisplayString());
        }

        [WorkItem(528898, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528898")]
        [ClrOnlyFact]
        public void GenericTypeWithObsoleteBangAritySuffixIsNotAvailable()
        {
            var ilSource =
@"
.class public A
{ 
    .class interface nested public abstract I`1<T> { }
}

.class public B extends A
{ 
    .class nested public 'I!1'<T> { }
}
";
            var csharpSource =
@"
class C : object, B.I<string>
{
}
";
            CompileWithCustomILSource(csharpSource, ilSource);
        }

        [WorkItem(528913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528913")]
        [Fact(Skip = "528913")]
        public void StaticTypesCannotBeUsedAsTypeArgumentsInInterfacesImplementedThroughInterfaceInheritance()
        {
            var ilSource =
@"
.class public interface abstract A`1<T> { }
.class public abstract sealed B { }
.class public interface abstract C implements class A`1<class B> {  }
";
            var csharpSource =
@"
class D : C { }
";
            CreateCompilationWithCustomILSource(csharpSource, ilSource).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "B"));
        }

        [WorkItem(530224, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530224")]
        [Fact]
        public void IsMetadataVirtualBeforeForceComplete()
        {
            var source = @"
interface I
{
    void Finalize();
}

class Derived : Base, I
{
    ~Derived()
    {
    }

    static void Main()
    {
    }
}

class Base
{
    public void Finalize()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);

            var global = comp.GlobalNamespace;

            var @interface = global.GetMember<NamedTypeSymbol>("I");
            var @base = global.GetMember<NamedTypeSymbol>("Base");
            var derived = global.GetMember<NamedTypeSymbol>("Derived");

            // Force completion of destructor symbol.  Calls IsMetadataVirtual on Base.Finalize.
            var returnType = derived.GetMember<MethodSymbol>(WellKnownMemberNames.DestructorName).ReturnType;
            Assert.Equal(SpecialType.System_Void, returnType.SpecialType);

            // Force completion of entire symbol.  Calls EnsureMetadataVirtual on Base.Finalize.
            derived.ForceComplete(locationOpt: null, cancellationToken: CancellationToken.None);
        }

        [Fact]
        public void NoBridgeMethodForVirtualImplementation()
        {
            var source1 = @"
public interface I
{
    void Virtual();
    void NonVirtual();
}

public class B
{
    public virtual void Virtual() { }
    public void NonVirtual() { }
}
";

            var source2 = @"
class D : B, I
{
}
";
            var comp1 = CreateCompilationWithMscorlib(source1, options: TestOptions.ReleaseDll, assemblyName: "asm1");
            comp1.VerifyDiagnostics();
            var ref1 = new CSharpCompilationReference(comp1);

            var comp2 = CreateCompilationWithMscorlib(source2, new[] { ref1 }, options: TestOptions.ReleaseDll, assemblyName: "asm2");
            comp2.VerifyDiagnostics();

            var derivedType = comp2.GlobalNamespace.GetMember<SourceNamedTypeSymbol>("D");
            var bridgeMethod = derivedType.GetSynthesizedExplicitImplementations(CancellationToken.None).Single();
            Assert.Equal("NonVirtual", bridgeMethod.ImplementingMethod.Name);
        }

        [WorkItem(530358, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530358")]
        [Fact]
        public void ExplicitImplementationWithoutInterfaceInName()
        {
            var il = @"
.class interface public abstract auto ansi I1
{
  .method public hidebysig newslot abstract virtual 
          instance void  M() cil managed
  {
  }

} // end of class I1

.class interface public abstract auto ansi I2
{
  .method public hidebysig newslot abstract virtual 
          instance void  M() cil managed
  {
  }

} // end of class I2

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
       implements I1, I2
{
  .method public hidebysig newslot virtual final 
          instance void  M() cil managed
  {
    .override I1::M
    ret
  } // end of method Base::I1.M

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method Base::.ctor

} // end of class Base
";

            var source = @"
class Derived : Base, I2
{
}
";

            var comp = CreateCompilationWithCustomILSource(source, il);
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;

            var interface1 = global.GetMember<NamedTypeSymbol>("I1");
            var interface1Method = interface1.GetMember<MethodSymbol>("M");

            var interface2 = global.GetMember<NamedTypeSymbol>("I2");
            var interface2Method = interface2.GetMember<MethodSymbol>("M");

            var baseType = global.GetMember<NamedTypeSymbol>("Base");
            var baseTypeMethod = baseType.GetMember<MethodSymbol>("M");

            var derivedType = global.GetMember<NamedTypeSymbol>("Derived");

            // Both interface methods are implemented by a single base type method - one implicitly and
            // the other explicitly.
            Assert.Equal(baseTypeMethod, derivedType.FindImplementationForInterfaceMember(interface1Method));
            Assert.Equal(baseTypeMethod, derivedType.FindImplementationForInterfaceMember(interface2Method));
        }

        [Fact]
        [WorkItem(530164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530164"), WorkItem(531642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531642"), WorkItem(531643, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531643")]
        public void ImplicitImplementationOfByRefReturn()
        {
            var il = @"
.class interface public abstract auto ansi I
{
  .method public hidebysig newslot abstract virtual 
          instance int32&  M() cil managed
  {
  }

} // end of class I

.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{
  .method public hidebysig instance int32& 
          M() cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class B
";

            var source = @"
public class D : B, I
{
}
";

            var comp = CreateCompilationWithCustomILSource(source, il);
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;

            var interfaceType = global.GetMember<NamedTypeSymbol>("I");
            var interfaceMethod = interfaceType.GetMember<MethodSymbol>("M");

            var baseType = global.GetMember<NamedTypeSymbol>("B");
            var baseMethod = baseType.GetMember<MethodSymbol>("M");

            var derivedType = global.GetMember<SourceNamedTypeSymbol>("D");

            var byRefType = (ByRefReturnErrorTypeSymbol)interfaceMethod.ReturnType.TypeSymbol;

            // General characteristics of type:
            Assert.NotEqual(byRefType, byRefType.ReferencedType);
            Assert.NotEqual(byRefType.GetHashCode(), byRefType.ReferencedType.GetHashCode());

            // Interface implementation:
            Assert.Equal(byRefType, interfaceMethod.ReturnType.TypeSymbol);
            Assert.Equal(baseMethod, derivedType.FindImplementationForInterfaceMember(interfaceMethod));

            var synthesized = derivedType.GetSynthesizedExplicitImplementations(CancellationToken.None).Single();
            Assert.Equal(baseMethod, synthesized.ImplementingMethod);
            Assert.Equal(interfaceMethod, synthesized.ExplicitInterfaceImplementations.Single());


            // Still get a use site error if you actually call the method.
            var source2 = @"
public class D : B, I
{
    static void Main()
    {
        I i = new D();
        var x = i.M();
    }
}
";

            CreateCompilationWithCustomILSource(source2, il).VerifyDiagnostics(
                // (7,17): error CS7085: By-reference return type 'ref int' is not supported.
                //         var x = i.M();
                Diagnostic(ErrorCode.ERR_ByRefReturnUnsupported, "M").WithArguments("int"));
        }

        [WorkItem(547149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547149")]
        [Fact]
        public void BaseTypeDoesNotActuallyImplementInterface()
        {
            var il = @"
.class interface public abstract auto ansi Interface
{
  .method public hidebysig newslot abstract virtual 
          instance void  M() cil managed
  {
  }
} // end of class Interface

.class public auto ansi beforefieldinit Base1
       extends [mscorlib]System.Object
       implements Interface
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class Base
";

            var ilRef = CompileIL(il);

            var source = @"
class Derived1 : Base1, Interface
{
}

class Base2 : Interface
{
}

class Derived2 : Base2, Interface
{
}
";

            // Base1 is in metadata, so we just trust it when it claims to implement Interface.
            // Base2 is identical, but in source.  We produce errors for both Base2 and Derived2.
            CreateCompilationWithMscorlib(source, new[] { ilRef }).VerifyDiagnostics(
                // (6,7): error CS0535: 'Base2' does not implement interface member 'Interface.M()'
                // class Base2 : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Base2", "Interface.M()"),
                // (10,7): error CS0535: 'Derived2' does not implement interface member 'Interface.M()'
                // class Derived2 : Base2, Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Derived2", "Interface.M()"));
        }

        [WorkItem(718115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718115")]
        [ClrOnlyFact]
        public void ExplicitlyImplementedAccessorsWithoutEvent()
        {
            var il = @"
.class interface public abstract auto ansi I
{
  .method public hidebysig newslot specialname abstract virtual 
          instance void  add_E(class [mscorlib]System.Action 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  remove_E(class [mscorlib]System.Action 'value') cil managed
  {
  }

  .event [mscorlib]System.Action E
  {
    .addon instance void I::add_E(class [mscorlib]System.Action)
    .removeon instance void I::remove_E(class [mscorlib]System.Action)
  }
} // end of class I


.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
       implements I
{
  .method private hidebysig newslot specialname virtual final 
          instance void  I.add_E(class [mscorlib]System.Action 'value') cil managed
  {
    .override I::add_E
    ldstr      ""Explicit implementation""
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method private hidebysig newslot specialname virtual final 
          instance void  I.remove_E(class [mscorlib]System.Action 'value') cil managed
  {
    .override I::remove_E
    ret
  }

  .method family hidebysig specialname instance void 
          add_E(class [mscorlib]System.Action 'value') cil managed
  {
    ldstr      ""Protected event""
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method family hidebysig specialname instance void 
          remove_E(class [mscorlib]System.Action 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  // NOTE: No event I.E

  .event [mscorlib]System.Action E
  {
    .addon instance void Base::add_E(class [mscorlib]System.Action)
    .removeon instance void Base::remove_E(class [mscorlib]System.Action)
  }
} // end of class Base
";

            var source = @"
public class Derived : Base, I
{
    public void Test()
    {
        ((I)this).E += null;
    }
}

public class Program
{
    static void Main()
    {
        Derived d = new Derived();
        d.Test();

        I id = new Derived();
        id.E += null;
    }
}
";

            var comp = CreateCompilationWithCustomILSource(source, il, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"
Explicit implementation
Explicit implementation
");

            var global = comp.GlobalNamespace;
            var @interface = global.GetMember<NamedTypeSymbol>("I");
            var baseType = global.GetMember<NamedTypeSymbol>("Base");
            var derivedType = global.GetMember<NamedTypeSymbol>("Derived");

            var interfaceEvent = @interface.GetMember<EventSymbol>("E");
            var interfaceAdder = interfaceEvent.AddMethod;

            var baseAdder = baseType.GetMembers().OfType<MethodSymbol>().
                Where(m => m.MethodKind == MethodKind.ExplicitInterfaceImplementation).
                Single(m => m.ExplicitInterfaceImplementations.Single().MethodKind == MethodKind.EventAdd);

            Assert.Equal(baseAdder, derivedType.FindImplementationForInterfaceMember(interfaceAdder));
            Assert.Equal(baseAdder, baseType.FindImplementationForInterfaceMember(interfaceAdder));

            Assert.Null(derivedType.FindImplementationForInterfaceMember(interfaceEvent));
            Assert.Null(baseType.FindImplementationForInterfaceMember(interfaceEvent));
        }

        [WorkItem(718115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718115")]
        [Fact]
        public void ExplicitlyImplementedParameterizedPropertyAccessor()
        {
            var il = @"
.class interface public abstract auto ansi I
{
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_P(int32 x) cil managed
  {
  }

  .property instance int32 P(int32)
  {
    .get instance int32 I::get_P(int32)
  }
} // end of class I

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
       implements I
{
  .method public hidebysig newslot specialname virtual final 
          instance int32  get_P(int32 x) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 P(int32)
  {
    .get instance int32 Base::get_P(int32)
  }
} // end of class Base
";

            var source = @"
public class Derived : Base, I
{
    int I.get_P(int x) { return 0; }
}
";

            var comp = CreateCompilationWithCustomILSource(source, il);
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var @interface = global.GetMember<NamedTypeSymbol>("I");
            var baseType = global.GetMember<NamedTypeSymbol>("Base");
            var derivedType = global.GetMember<NamedTypeSymbol>("Derived");

            var interfaceProperty = @interface.GetMember<PropertySymbol>("P");
            var interfaceGetter = interfaceProperty.GetMethod;

            var baseProperty = baseType.GetMember<PropertySymbol>("P");
            var baseGetter = baseProperty.GetMethod;

            var derivedGetter = derivedType.GetMembers().OfType<MethodSymbol>().
                Single(m => m.MethodKind == MethodKind.ExplicitInterfaceImplementation);

            Assert.Equal(baseProperty, baseType.FindImplementationForInterfaceMember(interfaceProperty));
            Assert.Equal(baseGetter, baseType.FindImplementationForInterfaceMember(interfaceGetter));

            Assert.Null(derivedType.FindImplementationForInterfaceMember(interfaceProperty)); // Used to return baseProperty, which seems wrong.
            Assert.Equal(derivedGetter, derivedType.FindImplementationForInterfaceMember(interfaceGetter));
        }

        [WorkItem(943542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/943542"), WorkItem(137, "CodePlex")]
        [ClrOnlyFact]
        public void Bug943542()
        {
            var il = @"
.class interface public abstract auto ansi IBase
{
  .method public newslot specialname abstract strict virtual 
          instance object  get_P1() cil managed
  {
  } // end of method IBase::get_P1

  .method public newslot specialname abstract strict virtual 
          instance void  set_P1(object Value) cil managed
  {
  } // end of method IBase::set_P1

  .method public newslot specialname abstract strict virtual 
          instance object  get_P2() cil managed
  {
  } // end of method IBase::get_P2

  .method public newslot specialname abstract strict virtual 
          instance void  set_P2(object Value) cil managed
  {
  } // end of method IBase::set_P2

  .property instance object P1()
  {
    .get instance object IBase::get_P1()
    .set instance void IBase::set_P1(object)
  } // end of property IBase::P1
  .property instance object P2()
  {
    .get instance object IBase::get_P2()
    .set instance void IBase::set_P2(object)
  } // end of property IBase::P2
} // end of class IBase

.class public auto ansi Base
       extends [mscorlib]System.Object
       implements IBase
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance object  g_P1() cil managed
  {
    .override IBase::get_P1
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (object V_0)
    IL_0000:  ldstr      ""g_P1""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(object 'value') cil managed
  {
    .override IBase::set_P1
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      ""set_P1""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance object  get_P2() cil managed
  {
    .override IBase::get_P2
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (object V_0)
    IL_0000:  ldstr      ""get_P2""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  s_P2(object 'value') cil managed
  {
    .override IBase::set_P2
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      ""s_P2""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .property instance object P1()
  {
    .get instance object Base::g_P1()
    .set instance void Base::set_P1(object)
  } // end of property Base::P1
  .property instance object P2()
  {
    .get instance object Base::get_P2()
    .set instance void Base::s_P2(object)
  } // end of property Base::P2
} // end of class Base
";

            var source = @"
interface IDerived
{
	object P1 {get;set;}
	object P2 {get;set;}
}

class Derived : Base, IDerived
{
	static void Main()
	{
		object val;
		IDerived x = new Derived();

		x.P1 = null;
		val = x.P1;

		x.P2 = null;
		val = x.P2;
	}
}";

            var comp = CreateCompilationWithCustomILSource(source, il, options: TestOptions.DebugExe);
            CompileAndVerify(comp, verify: false, expectedOutput: @"set_P1
g_P1
s_P2
get_P2");
        }
    }
}
