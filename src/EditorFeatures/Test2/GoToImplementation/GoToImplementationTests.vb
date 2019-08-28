' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.FindUsages

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToImplementation
    <[UseExportProvider]>
    Public Class GoToImplementationTests

        Private Async Function TestAsync(workspaceDefinition As XElement, Optional shouldSucceed As Boolean = True) As Task
            Await GoToHelpers.TestAsync(
            workspaceDefinition,
            Async Function(document As Document, position As Integer, context As SimpleFindUsagesContext) As Task
                Dim findUsagesService = document.GetLanguageService(Of IFindUsagesService)
                Await findUsagesService.FindImplementationsAsync(document, position, context).ConfigureAwait(False)
            End Function,
            shouldSucceed)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithSingleClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|$$C|] { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithAbstractClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
abstract class [|$$C|]
{
}

class [|D|] : C
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithAbstractClassFromInterface() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface $$I { }
abstract class [|C|] : I { }
class [|D|] : C { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithSealedClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
sealed class [|$$C|]
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithStruct() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
struct [|$$C|]
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithEnum() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
enum [|$$C|]
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithNonAbstractClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|$$C|]
{
}

class [|D|] : C
{
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithSingleClassImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|C|] : I { }
interface $$I { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithTwoClassImplementations() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|C|] : I { }
class [|D|] : I { }
interface $$I { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneMethodImplementation_01() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneMethodImplementation_02() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void [|M|]() { } }
interface I { void [|$$M|]() {} }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneMethodImplementation_03() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { void I.[|M|]() { } }
interface I { void [|$$M|]() {} }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneMethodImplementation_04() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface C : I 
{
    void I.[|M|]() { }
    void M();
}
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneMethodImplementation_05() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface C : I
{
    void I.[|M|]() { }
    void M();
}
interface I { void [|$$M|]() {} }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneEventImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C : I { public event EventHandler [|E|]; }
interface I { event EventHandler $$E; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithTwoMethodImplementations() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void [|M|]() { } }
class D : I { public void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithNonInheritedImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C { public void [|M|]() { } }
class D : C, I { }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(6752, "https://github.com/dotnet/roslyn/issues/6752")>
        Public Async Function TestWithVirtualMethodImplementationWithInterfaceOnBaseClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public virtual void [|M|]() { } }
class D : C { public override void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(6752, "https://github.com/dotnet/roslyn/issues/6752")>
        Public Async Function TestWithVirtualMethodImplementationWithInterfaceOnDerivedClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C { public virtual void M() { } }
class D : C, I { public override void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(6752, "https://github.com/dotnet/roslyn/issues/6752")>
        Public Async Function TestWithVirtualMethodImplementationAndInterfaceImplementedOnDerivedType() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public virtual void [|M|]() { } }
class D : C, I { public override void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(6752, "https://github.com/dotnet/roslyn/issues/6752")>
        Public Async Function TestWithAbstractMethodImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public abstract void [|M|]() { } }
class D : C { public override void [|M|]() { } }}
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithInterfaceMemberFromMetdataAtUseSite() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C : IDisposable
{
    public void [|Dispose|]()
    {
        IDisposable d;
        d.$$Dispose();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithSimpleMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C 
{
    public void [|$$M|]() { }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOverridableMethodOnBase() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C 
{
    public virtual void [|$$M|]() { }
}

class D : C
{
    public override void [|M|]() { }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOverridableMethodOnImplementation() As Task
            ' Our philosophy is to only show derived in this case, since we know the implementation of 
            ' D could never call C.M here
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C 
{
    public virtual void M() { }
}

class D : C
{
    public override void [|$$M|]() { }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        <WorkItem(19700, "https://github.com/dotnet/roslyn/issues/19700")>
        Public Async Function TestWithIntermediateAbstractOverrides() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    abstract class A {
        public virtual void $$[|M|]() { }
    }
    abstract class B : A {
        public abstract override void [|M|]();
    }
    sealed class C1 : B {
        public override void [|M|]() { }
    }
    sealed class C2 : A {
        public override void [|M|]() => base.M();
    }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function
    End Class
End Namespace
