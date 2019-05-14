﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.FindUsages
Imports Microsoft.CodeAnalysis.Editor.GoToBase

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToBase
    <[UseExportProvider]>
    Public Class GoToBaseTests
        Private Async Function TestAsync(workspaceDefinition As XElement, Optional shouldSucceed As Boolean = True) As Task
            Await GoToHelpers.TestAsync(
                workspaceDefinition,
                Async Function(document As Document, position As Integer, context As SimpleFindUsagesContext)
                    Dim gotoBaseService = document.GetLanguageService(Of IGoToBaseService)
                    Await gotoBaseService.FindBasesAsync(document, position, context)
                End Function,
                shouldSucceed)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestEmptyFile() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
$$
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, shouldSucceed:=False)
        End Function

#Region "Classes And Interfaces"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSingleClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class $$C { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithAbstractClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
abstract class [|C|]
{
}

class $$D : C
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithAbstractClassFromInterface() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface [|I|] { }
abstract class [|C|] : I { }
class $$D : C { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSealedClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|D|] { }
sealed class $$C : D
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithEnum() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
enum $$C
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithNonAbstractClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|C|]
{
}

class $$D : C
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSingleClassImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class $$C : I { }
interface [|I|] { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithTwoClassImplementations() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class $$C : I { }
class D : I { }
interface [|I|] { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestClassHierarchyWithParentSiblings() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class E : D { }
class $$D : B { }
class [|B|] : A { }
class C : A { }
class [|A|] : I2 { }
interface [|I2|] : I { }
interface I1 : I { }
interface [|I|] : J1, J2 { }
interface [|J1|] { }
interface [|J2|] { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#End Region

#Region "Structs"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithStruct() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
struct $$C
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSingleStructImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
struct $$C : I { }
interface [|I|] { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestStructWithInterfaceHierarchy() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
struct $$S : I2 { }
interface [|I2|] : I { }
interface I1 : I { }
interface [|I|] : J1, J2 { }
interface [|J1|] { }
interface [|J2|] { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#End Region

#Region "Methods"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_01() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void $$M() { } }
interface I { void [|M|](); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_02() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void $$M() { } }
interface I { void [|M|]() {} }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_03() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { void I.$$M() { } }
interface I { void [|M|]() {} }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_04() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface C : I 
{
    void I.$$M() { }
    void M();
}
interface I { void [|M|](); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_05() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface C : I
{
    void I.$$M() { }
    void M();
}
interface I { void [|M|]() {} }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_06() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface C : I 
{
    void I.M() { }
    void $$M();
}
interface I { void M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_07() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface C : I
{
    void I.M() { }
    void $$M();
}
interface I { void M() {} }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementationInStruct() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
struct S : I { public void $$M() { } }
interface I { void [|M|](); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithTwoMethodImplementations() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void $$M() { } }
class D : I { public void M() { } }
interface I { void [|M|](); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestOverrideWithOverloads_01() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : D { public override void $$M() { } }
class D 
{ 
    public virtual void [|M|]() { } 
    public virtual void M(int a) { } 
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestOverrideWithOverloads_02() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : D { public override void $$M(int a) { } }
class D 
{ 
    public virtual void M() { } 
    public virtual void [|M|](int a) { } 
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestImplementWithOverloads_01() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I 
{ 
    public void $$M() { } 
    public void M(int a) { } 
}
interface I
{ 
    void [|M|]();
    void M(int a);
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestImplementWithOverloads_02() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I 
{ 
    public void M() { } 
    public void $$M(int a) { } 
}
interface I
{ 
    void M();
    void [|M|](int a);
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithNonInheritedImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C { public void $$M() { } }
class D : C, I { }
interface I { void M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodImplementationWithInterfaceOnBaseClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public virtual void [|M|]() { } }
class D : C { public override void $$M() { } }
interface I { void [|M|](); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodHiddenWithInterfaceOnBaseClass() As Task
            ' We should not find a hidden method.
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public virtual void M() { } }
class D : C { public new void $$M() { } }
interface I { void [|M|](); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodImplementationWithInterfaceOnDerivedClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C { public virtual void [|M|]() { } }
class D : C, I { public override void $$M() { } }
interface I { void [|M|](); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodHiddenWithInterfaceOnDerivedClass() As Task
            ' We should not find a hidden method.
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C { public virtual void M() { } }
class D : C, I { public new void $$M() { } }
interface I { void [|M|](); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodImplementationAndInterfaceImplementedOnDerivedType() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public virtual void [|M|]() { } }
class D : C, I { public override void $$M() { } }
interface I { void [|M|](); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodHiddenAndInterfaceImplementedOnDerivedType() As Task
            ' We should not find a hidden method.
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public virtual void M() { } }
class D : C, I { public new void $$M() { } }
interface I { void [|M|](); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithAbstractMethodImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public abstract void [|M|]() { } }
class D : C { public override void $$M() { } }}
interface I { void [|M|](); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSimpleMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C 
{
    public void $$M() { }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOverridableMethodOnBase() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C 
{
    public virtual void [|M|]() { }
}

class D : C
{
    public override void $$M() { }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOverridableMethodOnImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C 
{
    public virtual void $$M() { }
}

class D : C
{
    public override void M() { }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithIntermediateAbstractOverrides() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    abstract class A {
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
    }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#End Region

#Region "Properties and Events"
        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneEventImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C : I { public event EventHandler $$E; }
interface I { event EventHandler [|E|]; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneEventImplementationInStruct() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

struct S : I { public event EventHandler $$E; }
interface I { event EventHandler [|E|]; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneEventExplicitImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C : I { event EventHandler I.$$E; }
interface I { event EventHandler [|E|]; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneEventExplicitImplementationInStruct() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

struct S : I { event EventHandler I.$$E; }
interface I { event EventHandler [|E|]; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOnePropertyImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C : I { public int $$P { get; set; } }
interface I { int [|P|] { get; set; } }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOnePropertyExplicitImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C : I { int I.$$P { get; set; } }
interface I { int [|P|] { get; set; } }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOnePropertyImplementationInStruct() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

struct S : I { public int $$P { get; set; } }
interface I { int [|P|] { get; set; } }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOnePropertyExplicitImplementationInStruct() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

struct S : I { int I.$$P { get; set; } }
interface I { int [|P|] { get; set; } }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#End Region

    End Class
End Namespace
