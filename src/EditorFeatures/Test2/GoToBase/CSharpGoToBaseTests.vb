﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToBase
    <[UseExportProvider]>
    Public Class CSharpGoToBaseTests
        Inherits GoToBaseTestsBase
        Private Overloads Shared Async Function TestAsync(source As String, Optional shouldSucceed As Boolean = True,
                                                   Optional metadataDefinitions As String() = Nothing) As Task
            Await GoToBaseTestsBase.TestAsync(source, LanguageNames.CSharp, shouldSucceed, metadataDefinitions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestEmptyFile() As Task
            Await TestAsync("$$", shouldSucceed:=False)
        End Function

#Region "Classes And Interfaces"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSingleClass() As Task
            Await TestAsync("class $$C { }", metadataDefinitions:={"mscorlib:Object"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithAbstractClass() As Task
            Await TestAsync(
"abstract class [|C|]
{
}

class $$D : C
{
}", metadataDefinitions:={"mscorlib:Object"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithAbstractClassFromInterface() As Task
            Await TestAsync(
"interface [|I|] { }
abstract class [|C|] : I { }
class $$D : C { }", metadataDefinitions:={"mscorlib:Object"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSealedClass() As Task
            Await TestAsync(
"class [|D|] { }
sealed class $$C : D
{
}", metadataDefinitions:={"mscorlib:Object"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithEnum() As Task
            Await TestAsync(
"enum $$C
{
}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithNonAbstractClass() As Task
            Await TestAsync(
"class [|C|]
{
}

class $$D : C
{
}", metadataDefinitions:={"mscorlib:Object"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSingleClassImplementation() As Task
            Await TestAsync(
"class $$C : I { }
interface [|I|] { }", metadataDefinitions:={"mscorlib:Object"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithTwoClassImplementations() As Task
            Await TestAsync(
"class $$C : I { }
class D : I { }
interface [|I|] { }", metadataDefinitions:={"mscorlib:Object"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestClassHierarchyWithParentSiblings() As Task
            Await TestAsync(
"class E : D { }
class $$D : B { }
class [|B|] : A { }
class C : A { }
class [|A|] : I2 { }
interface [|I2|] : I { }
interface I1 : I { }
interface [|I|] : J1, J2 { }
interface [|J1|] { }
interface [|J2|] { }", metadataDefinitions:={"mscorlib:Object"})
        End Function

#End Region

#Region "Structs"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithStruct() As Task
            Await TestAsync(
"struct $$C
{
}", metadataDefinitions:={"mscorlib:Object", "mscorlib:ValueType"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSingleStructImplementation() As Task
            Await TestAsync(
"struct $$C : I { }
interface [|I|] { }", metadataDefinitions:={"mscorlib:Object", "mscorlib:ValueType"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestStructWithInterfaceHierarchy() As Task
            Await TestAsync(
"struct $$S : I2 { }
interface [|I2|] : I { }
interface I1 : I { }
interface [|I|] : J1, J2 { }
interface [|J1|] { }
interface [|J2|] { }", metadataDefinitions:={"mscorlib:Object", "mscorlib:ValueType"})
        End Function

#End Region

#Region "Methods"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_01() As Task
            Await TestAsync(
"class C : I { public void $$M() { } }
interface I { void [|M|](); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_02() As Task
            Await TestAsync(
"class C : I { public void $$M() { } }
interface I { void [|M|]() {} }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_03() As Task
            Await TestAsync(
"class C : I { void I.$$M() { } }
interface I { void [|M|]() {} }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_04() As Task
            Await TestAsync(
"interface C : I 
{
    void I.$$M() { }
    void M();
}
interface I { void [|M|](); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_05() As Task
            Await TestAsync(
"interface C : I
{
    void I.$$M() { }
    void M();
}
interface I { void [|M|]() {} }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_06() As Task
            Await TestAsync(
"interface C : I 
{
    void I.M() { }
    void $$M();
}
interface I { void M(); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_07() As Task
            Await TestAsync(
"interface C : I
{
    void I.M() { }
    void $$M();
}
interface I { void M() {} }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementationInStruct() As Task
            Await TestAsync(
"struct S : I { public void $$M() { } }
interface I { void [|M|](); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithTwoMethodImplementations() As Task
            Await TestAsync(
"class C : I { public void $$M() { } }
class D : I { public void M() { } }
interface I { void [|M|](); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestOverrideWithOverloads_01() As Task
            Await TestAsync(
"class C : D { public override void $$M() { } }
class D 
{ 
    public virtual void [|M|]() { } 
    public virtual void M(int a) { } 
}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestOverrideWithOverloads_02() As Task
            Await TestAsync(
"class C : D { public override void $$M(int a) { } }
class D 
{ 
    public virtual void M() { } 
    public virtual void [|M|](int a) { } 
}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestImplementWithOverloads_01() As Task
            Await TestAsync(
"class C : I 
{ 
    public void $$M() { } 
    public void M(int a) { } 
}
interface I
{ 
    void [|M|]();
    void M(int a);
}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestImplementWithOverloads_02() As Task
            Await TestAsync(
"class C : I 
{ 
    public void M() { } 
    public void $$M(int a) { } 
}
interface I
{ 
    void M();
    void [|M|](int a);
}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithNonInheritedImplementation() As Task
            Await TestAsync(
"class C { public void $$M() { } }
class D : C, I { }
interface I { void M(); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodImplementationWithInterfaceOnBaseClass() As Task
            Await TestAsync(
"class C : I { public virtual void [|M|]() { } }
class D : C { public override void $$M() { } }
interface I { void [|M|](); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodHiddenWithInterfaceOnBaseClass() As Task
            ' We should not find hidden methods 
            ' and methods in interfaces if hidden below but the nested class does not implement the interface.
            Await TestAsync(
"class C : I { public virtual void M() { } }
class D : C { public new void $$M() { } }
interface I { void M(); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodImplementationWithInterfaceOnDerivedClass() As Task
            Await TestAsync(
"class C { public virtual void [|M|]() { } }
class D : C, I { public override void $$M() { } }
interface I { void [|M|](); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodHiddenWithInterfaceOnDerivedClass() As Task
            ' We should not find a hidden method.
            Await TestAsync(
"class C { public virtual void M() { } }
class D : C, I { public new void $$M() { } }
interface I { void [|M|](); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodImplementationAndInterfaceImplementedOnDerivedType() As Task
            Await TestAsync(
"class C : I { public virtual void [|M|]() { } }
class D : C, I { public override void $$M() { } }
interface I { void [|M|](); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodHiddenAndInterfaceImplementedOnDerivedType() As Task
            ' We should not find hidden methods 
            ' but should find methods in interfaces if hidden below but the nested class implements the interface.
            Await TestAsync(
"class C : I { public virtual void M() { } }
class D : C, I { public new void $$M() { } }
interface I { void [|M|](); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithAbstractMethodImplementation() As Task
            Await TestAsync(
"abstract class C : I { public abstract void [|M|]() { } }
class D : C { public override void $$M() { } }
interface I { void [|M|](); }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSimpleMethod() As Task
            Await TestAsync(
"class C 
{
    public void $$M() { }
}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOverridableMethodOnBase() As Task
            Await TestAsync(
"class C 
{
    public virtual void [|M|]() { }
}

class D : C
{
    public override void $$M() { }
}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOverridableMethodOnImplementation() As Task
            Await TestAsync(
"class C 
{
    public virtual void $$M() { }
}

class D : C
{
    public override void M() { }
}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithIntermediateAbstractOverrides() As Task
            Await TestAsync(
"abstract class A {
    public virtual void [|M|]() { }
}
abstract class B : A {
    public abstract override void M();
}
sealed class C1 : B {
    public override void M() { }
}
sealed class C2 : A {
    public override void $$M() => base.M();
}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOverloadsOverrdiesAndInterfaceImplementation_01() As Task
            Await TestAsync(
"abstract class C : I { public virtual void [|M|]() { } public virtual void M(int i) { }}
class D : C { public override void $$M() { } public override void M(int i) { }}
interface I { void [|M|](); void M(int i};")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOverloadsOverrdiesAndInterfaceImplementation_02() As Task
            Await TestAsync(
"abstract class C : I { public virtual void M() { } public virtual void [|M|](int i) { }}
class D : C { public override void M() { } public override void $$M(int i) { }}
interface I { void M(); void [|M|](int i};")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestOverrideOfMethodFromMetadata() As Task
            Await TestAsync(
"using System;
class C { public override string $$ToString() { return base.ToString(); } }
", metadataDefinitions:={"mscorlib:Object.ToString"})
        End Function

#End Region

#Region "Properties and Events"
        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneEventImplementation() As Task
            Await TestAsync(
"using System;

class C : I { public event EventHandler $$E; }
interface I { event EventHandler [|E|]; }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneEventImplementationInStruct() As Task
            Await TestAsync(
"using System;

struct S : I { public event EventHandler $$E; }
interface I { event EventHandler [|E|]; }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneEventExplicitImplementation() As Task
            Await TestAsync(
"using System;

class C : I { event EventHandler I.$$E; }
interface I { event EventHandler [|E|]; }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneEventExplicitImplementationInStruct() As Task
            Await TestAsync(
"using System;

struct S : I { event EventHandler I.$$E; }
interface I { event EventHandler [|E|]; }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOnePropertyImplementation() As Task
            Await TestAsync(
"using System;

class C : I { public int $$P { get; set; } }
interface I { int [|P|] { get; set; } }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOnePropertyExplicitImplementation() As Task
            Await TestAsync(
"using System;

class C : I { int I.$$P { get; set; } }
interface I { int [|P|] { get; set; } }")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOnePropertyImplementationInStruct() As Task
            Await TestAsync(
"using System;

struct S : I { public int $$P { get; set; } }
interface I { int [|P|] { get; set; } }
        ")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOnePropertyExplicitImplementationInStruct() As Task
            Await TestAsync(
"using System;

struct S : I { int I.$$P { get; set; } }
interface I { int [|P|] { get; set; } }")
        End Function

#End Region

    End Class
End Namespace
