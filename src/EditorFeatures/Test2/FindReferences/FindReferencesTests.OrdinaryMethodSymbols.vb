' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethod1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            private void {|Definition:Goo|}() { }

            void Bar()
            {
                [|Go$$o|]();
                [|Goo|]();
                B.Goo();
                new C().[|Goo|]();
                new C().goo();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodCaseSensitivity(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        class C
            private sub {|Definition:Goo|}()
            end sub

            sub Bar()
                [|Go$$o|]()
                [|Goo|]()
                B.Goo()
                Console.WriteLine(new C().[|Goo|]())
                Console.WriteLine(new C().[|goo|]())
            end sub
        end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(18963, "https://github.com/dotnet/roslyn/issues/18963")>
        Public Async Function FindReferences_GetAwaiter(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
public class C
{
    public TaskAwaiter<bool> {|Definition:Get$$Awaiter|}() => Task.FromResult(true).GetAwaiter();

    static async void M(C c)
    {
        [|await|] c;
        [|await|] c;
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(18963, "https://github.com/dotnet/roslyn/issues/18963")>
        Public Async Function FindReferences_GetAwaiter_VB(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices
Public Class C
    Public Function {|Definition:Get$$Awaiter|}() As TaskAwaiter(Of Boolean)
    End Function

    Shared Async Sub M(c As C)
        [|Await|] c
    End Sub
End Class
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(18963, "https://github.com/dotnet/roslyn/issues/18963")>
        Public Async Function FindReferences_GetAwaiterInAnotherDocument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
public class C
{
   public TaskAwaiter<bool> {|Definition:Get$$Awaiter|}() => Task.FromResult(true).GetAwaiter();
}
        ]]></Document>
        <Document><![CDATA[
class D
{
    static async void M(C c)
    {
        [|await|] c;
        [|await|] c;
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(18963, "https://github.com/dotnet/roslyn/issues/18963")>
        Public Async Function FindReferences_Deconstruction(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    public void {|Definition:Decons$$truct|}(out int x1, out int x2) { x1 = 1; x2 = 2; }
    public void M()
    {
        [|var (x1, x2)|] = this;
        foreach ([|var (y1, y2)|] in new[] { this }) { }
        [|(x1, (x2, _))|] = (1, this);
        (x1, x2) = (1, 2);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(24184, "https://github.com/dotnet/roslyn/issues/24184")>
        Public Async Function FindReferences_DeconstructionInAnotherDocument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public static class Extensions
{
    public void {|Definition:Decons$$truct|}(this C c, out int x1, out int x2) { x1 = 1; x2 = 2; }
}
        </Document>
        <Document>
class C
{
    public void M()
    {
        [|var (x1, x2)|] = this;
        foreach ([|var (y1, y2)|] in new[] { this }) { }
        [|(x1, (x2, _))|] = (1, this);
        (x1, x2) = (1, 2);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(18963, "https://github.com/dotnet/roslyn/issues/18963")>
        Public Async Function FindReferences_ForEachDeconstructionOnItsOwn(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public static class Extensions
{
    public void {|Definition:Decons$$truct|}(this C c, out int x1, out int x2) { x1 = 1; x2 = 2; }
}
class C
{
    public void M()
    {
        foreach ([|var (y1, y2)|] in new[] { this }) { }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(18963, "https://github.com/dotnet/roslyn/issues/18963")>
        Public Async Function FindReferences_NestedDeconstruction(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public static class Extensions
{
    public void {|Definition:Decons$$truct|}(this C c, out int x1, out C x2) { x1 = 1; x2 = null; }
}
class C
{
    public void M()
    {
        [|var (y1, (y2, y3))|] = this;
        [|(y1, (y2, y3))|] = (1, this);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(18963, "https://github.com/dotnet/roslyn/issues/18963")>
        Public Async Function FindReferences_NestedDeconstruction2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public static class Extensions
{
    public void Deconstruct(this int i, out int x1, out C x2) { x1 = 1; x2 = null; }
    public void {|Definition:Decons$$truct|}(this C c, out int x1, out int x2) { x1 = 1; x2 = 2; }
}
class C
{
    public void M()
    {
        [|var (y1, (y2, y3))|] = 1;
        var (z1, z2) = 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(18963, "https://github.com/dotnet/roslyn/issues/18963")>
        Public Async Function FindReferences_NestedDeconstruction3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public static class Extensions
{
    public void {|Definition:Decons$$truct|}(this int i, out int x1, out C x2) { x1 = 1; x2 = null; }
    public void Deconstruct(this C c, out int x1, out int x2) { x1 = 1; x2 = 2; }
}
class C
{
    public void M()
    {
        [|var (y1, (y2, y3))|] = 1;
        [|var (z1, z2)|] = 1;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(18963, "https://github.com/dotnet/roslyn/issues/18963")>
        Public Async Function FindReferences_DeconstructionAcrossLanguage(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document><![CDATA[
Public Class Deconstructable
    Public Sub {|Definition:Decons$$truct|}(<System.Runtime.InteropServices.Out> ByRef x1 As Integer, <System.Runtime.InteropServices.Out> ByRef x2 As Integer)
        x1 = 1
        x2 = 2
    End Sub
End Class
        ]]></Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
class C
{
    public void M(Deconstructable d)
    {
        [|var (x1, x2)|] = d;
        foreach ([|var (y1, y2)|] in new[] { d }) { }
        [|(x1, (x2, _))|] = (1, d);
        (x1, x2) = (1, 2);
        d.[|Deconstruct|](out var t1, out var t2);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodOverride1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            public virtual void {|Definition:Go$$o|}() { }
            void Bar() { [|Goo|](); }
        }
        class D : C
        {
            public override void {|Definition:Goo|}() { }
            void Quux() { [|Goo|](); }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodOverride2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            public virtual void {|Definition:Goo|}() { }
            void Bar() { [|Goo|](); }
        }
        class D : C
        {
            public override void {|Definition:Go$$o|}() { }
            void Quux() { [|Goo|](); }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodOverride3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            public virtual void Goo() { }
            void Bar() { Goo(); }
        }
        class D : C
        {
            public override void Goo() { }
            void Quux() { Goo(); }
        }
        class E : D
        {
            public new void {|Definition:Go$$o|}() { }
            void Z() { [|Goo|](); }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodOverride_InMetadata(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            // Will walk up to Object.ToString
            public override string {|Definition:To$$String|}() { }
        }
        class O
        {
            public override string {|Definition:ToString|}() { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodOverrideCrossLanguage(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
        public class C
        {
            public virtual void {|Definition:Go$$o|}() { }
        }
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <ProjectReference>CSharpAssembly</ProjectReference>
        <Document>
        class D : Inherits C
            public overrides sub {|Definition:Goo|}()
            end sub
            private sub Bar()
                [|Goo|]()
            end sub
        sub class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceInheritance_FromReference(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            void {|Definition:Goo|}();
        }

        class C1 : I1
        {
            public void {|Definition:Goo|}()
            {
            }
        }

        interface I2 : I1
        {
            void {|Definition:Goo|}();
            void Bar();
        }

        class C2 : I2
        {
            public void Bar()
            {
                [|Goo$$|]();
            }

            public void {|Definition:Goo|}();
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceInheritance_FromDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            void {|Definition:Go$$o|}();
        }

        class C1 : I1
        {
            public void {|Definition:Goo|}()
            {
            }
        }

        interface I2 : I1
        {
            void {|Definition:Goo|}();
            void Bar();
        }

        class C2 : I2
        {
            public void Bar()
            {
                [|Goo|]();
            }

            public void {|Definition:Goo|}();
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceImplementation1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface IGoo
        {
            void {|Definition:Goo|}();
        }
        class C
        {
            public void {|Definition:Go$$o|}() { }
        }
        class D : C, IGoo
        {
            void Quux()
            {
                IGoo f;
                f.[|Goo|]();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(529616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529616")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceImplementationVB(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Interface IGoo
            Sub {|Definition:TestSub|}()
        End Interface

        Class Goo
            Implements IGoo
            Public Sub {|Definition:MethodWithADifferentName|}() Implements IGoo.[|$$TestSub|]
            End Function
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceImplementation2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface IGoo
        {
            void {|Definition:G$$oo|}();
        }
        class C
        {
            public void {|Definition:Goo|}() { }
            void Zap()
            {
                this.[|Goo|]();
                [|Goo|]();
            }
        }
        class D : C, IGoo
        {
            void Quux()
            {
                IGoo f;
                f.[|Goo|]();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceImplementationSingleFileOnly(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Zap()
            {
                IGoo goo;
                goo.[|Go$$o|]();
            }
        }
        class D : IGoo
        {
            void Quux()
            {
                IGoo f;
                f.[|Goo|]();
            }
        }
        </Document>
        <Document>
        interface IGoo
        {
            void {|Definition:Goo|}();
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host, searchSingleFileOnly:=True)
        End Function

        <WorkItem(522786, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522786")>
        <WorkItem(34107, "https://github.com/dotnet/roslyn/issues/34107")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34107"), CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceDispose1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C : System.IDisposable
        {
            public void {|Definition:Disp$$ose|}() { }
            void Zap()
            {
                [|using|] (new C())
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(522786, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522786")>
        <WorkItem(34107, "https://github.com/dotnet/roslyn/issues/34107")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34107"), CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceDispose2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C : System.IDisposable
        {
            public void {|Definition:Disp$$ose|}() { }
            void Zap()
            {
                [|using|] (new D())
                {
                }
            }
        }
        class D : System.IDisposable
        {
            public void {|Definition:Dispose|}() { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodIEnumerable1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using System.Collections;
        class C : IEnumerable
        {
            public IEnumerator {|Definition:GetEnumera$$tor|}() { }
            void Zap()
            {
                [|foreach|] (var v in this)
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodIEnumerable2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using System.Collections;
        class C
        {
            public struct Enumerator : IEnumerator
            {
                public object Current { get { } }
                public bool {|Definition:MoveNe$$xt|}() { }
            }
            public Enumerator GetEnumerator() { }
            void Zap()
            {
                [|foreach|] (var v in this)
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodIEnumerable3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using System.Collections;
        class C
        {
            public struct Enumerator : IEnumerator
            {
                public object {|Definition:Cu$$rrent|} { get { } }
                public bool MoveNext() { }
            }
            public Enumerator GetEnumerator() { }
            void Zap()
            {
                [|foreach|] (var v in this)
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodIEnumerable4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System.Collections.Generic;
        class C : IEnumerable<int>
        {
            public IEnumerator<int> {|Definition:GetEnumera$$tor|}() { }
            void Zap()
            {
                [|foreach|] (var v in this)
                {
                }
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodIEnumerable5(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System.Collections.Generic;
        class C
        {
            public struct Enumerator<T> : IEnumerator<T>
            {
                public T Current { get { } }
                public bool {|Definition:MoveNe$$xt|}() { }
            }
            public Enumerator<int> GetEnumerator() { }
            void Zap()
            {
                [|foreach|] (var v in this)
                {
                }
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodIEnumerable6(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System.Collections.Generic;
        class C
        {
            public struct Enumerator<T> : IEnumerator<T>
            {
                public object {|Definition:Cu$$rrent|} { get { } }
                public bool MoveNext() { }
            }
            public Enumerator<T> GetEnumerator() { }
            void Zap()
            {
                [|foreach|] (var v in this)
                {
                }
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(634818, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/634818")>
        <WorkItem(34106, "https://github.com/dotnet/roslyn/issues/34106")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34106"), CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodLinqWhere1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
        using System.Collections.Generic;
        class C
        {
            void Zap()
            {
                var q = from v in this
                        [|where|] v > 21
                        select v;
            }
        }

        static class Extensions
        {
            public static IEnumerable<int> {|Definition:Whe$$re|}(this IEnumerable<int> source, Func<int, bool> predicate) => throw null;
            public static IEnumerable<int> Select(this IEnumerable<int> source, Func<int, int> func) => throw null;
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(636943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636943")>
        <WorkItem(34106, "https://github.com/dotnet/roslyn/issues/34106")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34106"), CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodLinqWhere2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
        using System.Collections.Generic;
        class C
        {
            void Zap()
            {
                var q = from v in this
                        [|w$$here|] v > 21
                        select v;
            }
        }

        static class Extensions
        {
            public static IEnumerable<int> {|Definition:Where|}(this IEnumerable<int> source, Func<int, bool> predicate) => throw null;
            public static IEnumerable<int> Select(this IEnumerable<int> source, Func<int, int> func) => throw null;
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(636943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636943")>
        <WorkItem(34106, "https://github.com/dotnet/roslyn/issues/34106")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34106"), CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodLinqSelect1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
        using System.Collections.Generic;
        class C
        {
            void Zap()
            {
                var q = from v in this
                        where v > 21
                        [|select|] v + 1;
            }
        }

        static class Extensions
        {
            public static IEnumerable<int> Where(this IEnumerable<int> source, Func<int, bool> predicate) => throw null;
            public static IEnumerable<int> {|Definition:Sel$$ect|}(this IEnumerable<int> source, Func<int, int> func) => throw null;
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(636943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636943")>
        <WorkItem(34106, "https://github.com/dotnet/roslyn/issues/34106")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34106"), CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodLinqSelect2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
        using System.Collections.Generic;
        class C
        {
            void Zap()
            {
                var q = from v in this
                        where v > 21
                        [|sel$$ect|] v + 1;
            }
        }

        static class Extensions
        {
            public static IEnumerable<int> Where(this IEnumerable<int> source, Func<int, bool> predicate) => throw null;
            public static IEnumerable<int> {|Definition:Select|}(this IEnumerable<int> source, Func<int, int> func) => throw null;
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(528936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528936")>
        <WorkItem(34105, "https://github.com/dotnet/roslyn/issues/34105")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34105"), CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodMonitorEnter(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System.Threading;
        using System.Collections.Generic;
        class C
        {
            void Zap()
            {
                bool lockTaken = false;
                Monitor.[|TryEn$$ter|](null, ref lockTaken);
                [|lock|] (new C())
                {
                }
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(528936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528936")>
        <WorkItem(34105, "https://github.com/dotnet/roslyn/issues/34105")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34105"), CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodMonitorExit(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System.Threading;
        using System.Collections.Generic;
        class C
        {
            void Zap()
            {
                Monitor.[|Ex$$it|](null);
                [|lock|] (new C())
                {
                }
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpInaccessibleInstanceAbstractMethod(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        abstract class C
        {
           public abstract void {|Definition:$$M|}(int i);
        }
        class D
        {
            void Goo()
            {
               C.[|M|](1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_VBInaccessibleInstanceAbstractMethod(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        MustInherit Class C
              public MustOverride Sub {|Definition:$$M|} (ByVal i as Integer)
        End Class
        Class D
              Sub Goo()
                   C.[|M|](1);
              End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(538794, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538794")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpInaccessibleInstancePrivateStaticMethod(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           private static void {|Definition:$$M|}(int i) { }
        }
        class D
        {
            void Goo()
            {
               C.[|M|](1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_VBInaccessibleInstancePrivateStaticMethod(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
              Private shared Sub {|Definition:$$M|} (ByVal i as Integer)
              End Sub
        End Class
        Class D
              Sub Goo()
                   C.[|M|](1)
              End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(538794, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538794")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpInaccessibleInstanceProtectedMethod(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           protected void {|Definition:$$M|}(int i) { }
        }
        class D
        {
            void Goo()
            {
               C.[|M|](1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_VBInaccessibleInstanceProtectedMethod(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
              Protected Sub {|Definition:$$M|} (ByVal i as Integer)
              End Sub
        End Class
        Class D
              Sub Goo()
                   C.[|M|](1)
              End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(2544, "https://github.com/dotnet/roslyn/issues/2544")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInaccessibleMemberOverrideVB(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class C
                Private Sub M(d As D)
                    d.[|$$M|](1)
                End Sub
            End Class
            Class D
                Private Sub {|Definition:M|}(i As Integer)
                End Sub
                Private Sub M(d As Double)
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(2544, "https://github.com/dotnet/roslyn/issues/2544")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInaccessibleMemberOverrideCS(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                private void M(D d)
                {
                    d.[|$$M|](1);
                }
            }

            class D
            {
                private void {|Definition:M|}(int i) { }
                private void M(double d) { }
            }
        </Document>
    </Project>
</Workspace>


            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpAccessibleInstanceProtectedMethod(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           protected void {|Definition:$$M|}(int i) { }
        }
        class D : C
        {
            void Goo()
            {
               D.[|M|](1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpAccessibleStaticProtectedMethod(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           protected static void {|Definition:$$M|}(int i) { }
        }
        class D : C
        {
            void Goo()
            {
               C.[|M|](1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(538726, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538726")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceMethodsDontCascadeThroughOtherInterfaceMethods1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface IControl
        {
            void {|Definition:Pa$$int|}();
        }
        interface ISurface : IControl
        {
            void Paint();
        }
        class SampleClass : IControl
        {
            public void {|Definition:Paint|}()
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(538726, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538726")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceMethodsDontCascadeThroughOtherInterfaceMethods2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface IControl
        {
            void {|Definition:Paint|}();
        }
        interface ISurface : IControl
        {
            void Paint();
        }
        class SampleClass : IControl
        {
            public void {|Definition:Pa$$int|}()
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(538726, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538726")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceMethodsDontCascadeThroughOtherInterfaceMethods3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface IControl
        {
            void Definition();
        }
        interface ISurface : IControl
        {
            void {|Definition:Pa$$int|}();
        }
        class SampleClass : IControl
        {
            public void Paint()
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(538898, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538898")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodMatchEntireInvocation(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module M
    Sub Main
        Dim x As I
        x.Goo(1)
    End Sub
End Module
 
Interface I
    Sub Goo(x as Integer)
    Sub {|Definition:G$$oo|}(x as Date)
End Interface
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539033, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539033")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
    interface I<T>
    {
        void {|Definition:$$F|}();
    }

    class Base<U> : I<U>
    {
        void I<U>.{|Definition:F|}() { }
    }

    class Derived<U, V> : Base<U>, I<V>
    {
        public void {|Definition:F|}()
        {
            [|F|]();
        }
    }
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539033, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539033")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
    interface I<T>
    {
        void {|Definition:F|}();
    }

    class Base<U> : I<U>
    {
        void I<U>.{|Definition:$$F|}() { }
    }

    class Derived<U, V> : Base<U>, I<V>
    {
        public void {|Definition:F|}()
        {
            [|F|]();
        }
    }
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539033, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539033")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
    interface I<T>
    {
        void {|Definition:F|}();
    }

    class Base<U> : I<U>
    {
        void I<U>.{|Definition:F|}() { }
    }

    class Derived<U, V> : Base<U>, I<V>
    {
        public void {|Definition:$$F|}()
        {
            [|F|]();
        }
    }
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539033, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539033")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
    interface I<T>
    {
        void {|Definition:F|}();
    }

    class Base<U> : I<U>
    {
        void I<U>.{|Definition:F|}() { }
    }

    class Derived<U, V> : Base<U>, I<V>
    {
        public void {|Definition:F|}()
        {
            [|$$F|]();
        }
    }
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539046")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_DoNotFindInNonImplementingClass1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I
{
  void {|Definition:$$Goo|}();
}

class C : I
{
  public void {|Definition:Goo|}()
  {
  }
}

class D : C
{
  public void Goo()
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539046")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_DoNotFindInNonImplementingClass2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I
{
  void {|Definition:Goo|}();
}

class C : I
{
  public void {|Definition:$$Goo|}()
  {
  }
}

class D : C
{
  public void Goo()
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539046")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_DoNotFindInNonImplementingClass3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I
{
  void Goo();
}

class C : I
{
  public void Goo()
  {
  }
}

class D : C
{
  public void {|Definition:$$Goo|}()
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_GenericMethod1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Goo|}<T>(IList<T> list);
}

class C : I
{
  public void {|Definition:Goo|}<U>(IList<U> list)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_GenericMethod2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:Goo|}<T>(IList<T> list);
}

class C : I
{
  public void {|Definition:$$Goo|}<U>(IList<U> list)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_GenericMethod3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Goo|}<T>(IList<T> list);
}

class C<T> : I
{
  public void Goo<U>(IList<T> list)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_GenericMethod4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Goo|}<T>(IList<T> list);
}

class C<T> : I
{
  public void Goo(IList<T> list)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_GenericMethod5(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Goo|}<T>(IList<T> list);
}

class C : I
{
  public void Goo<T>(IList<int> list)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_RefOut1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Goo|}(ref int i);
}

class C : I
{
  public void {|Definition:Goo|}(ref System.Int32 j)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_RefOut2_Success(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Goo|}(ref int i);
}

class C : I
{
  public void Goo(out System.Int32 j)
  {
  }

  void I.{|Definition:Goo|}(ref System.Int32 j) 
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_RefOut2_Error(kind As TestKind, host As TestHost) As Task
            ' In non-compiling code, finding an almost-matching definition
            ' seems reasonable.
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Goo|}(ref int i);
}

class C : I
{
  public void {|Definition:Goo|}(out System.Int32 j)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethod_DelegateConstructor1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Program
{
    delegate double DoubleFunc(double x);
    DoubleFunc f = new DoubleFunc([|$$Square|]);
    static float Square(float x)
    {
        return x * x;
    }
    static double {|Definition:Square|}(double x)
    {
        return x * x;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethod_DelegateConstructor2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Program
{
    delegate double DoubleFunc(double x);
    DoubleFunc f = new DoubleFunc(Square);
    static float {|Definition:$$Square|}(float x)
    {
        return x * x;
    }
    static double Square(double x)
    {
        return x * x;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethod_DelegateConstructor3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Program
{
    delegate double DoubleFunc(double x);
    DoubleFunc f = new DoubleFunc([|Square|]);
    static float Square(float x)
    {
        return x * x;
    }
    static double {|Definition:$$Square|}(double x)
    {
        return x * x;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539646, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539646")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDelegateMethod1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
 using System;
class Program
{
    delegate R {|Definition:Func|}<T, R>(T t);
    static void Main(string[] args)
    {
        [|Func|]<int, int> f = (arg) =>
        {
            int s = 3;
            return s;
        };
        f.[|$$BeginInvoke|](2, null, null);
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539646, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539646")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDelegateMethod2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
 using System;
class Program
{
    delegate R {|Definition:$$Func|}<T, R>(T t);
    static void Main(string[] args)
    {
        [|Func|]<int, int> f = (arg) =>
        {
            int s = 3;
            return s;
        };
        f.BeginInvoke(2, null, null);
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539646, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539646")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDelegateMethod3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
 using System;
class Program
{
    delegate R {|Definition:Func|}<T, R>(T t);
    static void Main(string[] args)
    {
        [|$$Func|]<int, int> f = (arg) =>
        {
            int s = 3;
            return s;
        };
        f.BeginInvoke(2, null, null);
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539824")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodGroup1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class C
{
    public delegate int Func(int i);
 
    public Func Goo()
    {
        return [|$$Goo|];
    }
    private int {|Definition:Goo|}(int i)
    {
        return i;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539824")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodGroup2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class C
{
    public delegate int Func(int i);
 
    public Func Goo()
    {
        return [|Goo|];
    }
    private int {|Definition:$$Goo|}(int i)
    {
        return i;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(540349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540349")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNonImplementedInterfaceMethod1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I
    Sub {|Definition:$$Goo|}()
End Interface

Class A
    Implements I
    Public Sub {|Definition:Goo|}() Implements I.[|Goo|]
    End Sub
End Class

Class B
    Inherits A
    Implements I
    Public Sub Goo()
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(540349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540349")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNonImplementedInterfaceMethod2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I
    Sub {|Definition:Goo|}()
End Interface

Class A
    Implements I
    Public Sub {|Definition:$$Goo|}() Implements I.[|Goo|]
    End Sub
End Class

Class B
    Inherits A
    Implements I
    Public Sub Goo()
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(540349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540349")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNonImplementedInterfaceMethod3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I
    Sub Goo()
End Interface

Class A
    Implements I
    Public Sub Goo() Implements I.Goo
    End Sub
End Class

Class B
    Inherits A
    Implements I
    Public Sub {|Definition:$$Goo|}()
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(540359, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540359")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestShadowedMethod1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I1
    Function {|Definition:$$Goo|}() As Integer
End Interface

Interface I2
    Inherits I1
    Shadows Function Goo() As Integer
End Interface

Class C
    Implements I1
    Public Function {|Definition:Goo|}() As Integer Implements I1.[|Goo|]
        Return 1
    End Function
End Class

Class M
    Inherits C
    Implements I2
    Public Overloads Function Goo() As Integer Implements I2.Goo
        Return 1
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(540359, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540359")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestShadowedMethod2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I1
    Function {|Definition:Goo|}() As Integer
End Interface

Interface I2
    Inherits I1
    Shadows Function Goo() As Integer
End Interface

Class C
    Implements I1
    Public Function {|Definition:$$Goo|}() As Integer Implements I1.[|Goo|]
        Return 1
    End Function
End Class

Class M
    Inherits C
    Implements I2
    Public Overloads Function Goo() As Integer Implements I2.Goo
        Return 1
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(540359, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540359")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestShadowedMethod3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I1
    Function Goo() As Integer
End Interface

Interface I2
    Inherits I1
    Shadows Function {|Definition:$$Goo|}() As Integer
End Interface

Class C
    Implements I1
    Public Function Goo() As Integer Implements I1.Goo
        Return 1
    End Function
End Class

Class M
    Inherits C
    Implements I2
    Public Overloads Function {|Definition:Goo|}() As Integer Implements I2.[|Goo|]
        Return 1
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(540359, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540359")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestShadowedMethod4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I1
    Function Goo() As Integer
End Interface

Interface I2
    Inherits I1
    Shadows Function {|Definition:Goo|}() As Integer
End Interface

Class C
    Implements I1
    Public Function Goo() As Integer Implements I1.Goo
        Return 1
    End Function
End Class

Class M
    Inherits C
    Implements I2
    Public Overloads Function {|Definition:$$Goo|}() As Integer Implements I2.[|Goo|]
        Return 1
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(540946, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540946")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAddressOfOverloads1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On

Imports System

Class C
    Shared Sub Main()
        Dim a As Action(Of Integer) = AddressOf [|$$Goo|]
    End Sub

    Sub Goo()
    End Sub

    Shared Sub {|Definition:Goo|}(x As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(540946, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540946")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAddressOfOverloads2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On

Imports System

Class C
    Shared Sub Main()
        Dim a As Action(Of Integer) = AddressOf [|Goo|]
    End Sub

    Sub Goo()
    End Sub

    Shared Sub {|Definition:$$Goo|}(x As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(540946, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540946")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAddressOfOverloads3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On

Imports System

Class C
    Shared Sub Main()
        Dim a As Action(Of Integer) = AddressOf Goo
    End Sub

    Sub {|Definition:$$Goo|}()
    End Sub

    Shared Sub Goo(x As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542034, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542034")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionValue1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Public Class MyClass1
    Public Shared Sub Main()
    End Sub
    Shared Function {|Definition:$$Function1|}(ByRef arg)
        [|Function1|] = arg * 2
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542034, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542034")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionValue2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Public Class MyClass1
    Public Shared Sub Main()
    End Sub
    Shared Function {|Definition:Function1|}(ByRef arg)
        [|$$Function1|] = arg * 2
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(543002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543002")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestForEachGetEnumerator1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class B
{
    public int Current { get; set; }
    public bool MoveNext()
    {
        return false;
    }
}

class C
{
    static void Main()
    {
        [|foreach|] (var x in new C()) { }
    }

    public B {|Definition:$$GetEnumerator|}()
    {
        return null;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(543002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543002")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestForEachMoveNext1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class B
{
    public int Current { get; set; }
    public bool {|Definition:$$MoveNext|}()
    {
        return false;
    }
}

class C
{
    static void Main()
    {
        [|foreach|] (var x in new C()) { }
    }

    public B GetEnumerator()
    {
        return null;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(543002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543002")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestForEachCurrent1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class B
{
    public int {|Definition:$$Current|} { get; set; }
    public bool MoveNext()
    {
        return false;
    }
}

class C
{
    static void Main()
    {
        [|foreach|] (var x in new C()) { }
    }

    public B GetEnumerator()
    {
        return null;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(544439, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544439")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodPartial1_CSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
partial class Class1
{
    partial void {|Definition:$$goo|}<T, U, V>(T x, U y, V z) where T : class where U : Exception, T where V : U;
    partial void {|Definition:goo|}<T, U, V>(T x, U y, V z) where T : class where U : Exception, T where V : U
    {
    }
}]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(544439, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544439")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodPartial2_CSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
partial class Class1
{
    partial void {|Definition:$$goo|}<T, U, V>(T x, U y, V z) where T : class where U : Exception, T where V : U;
    partial void {|Definition:goo|}<T, U, V>(T x, U y, V z) where T : class where U : Exception, T where V : U
    {
    }
}]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(544437, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544437")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodPartial1_VB(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Public Module Module1
    Partial Private Sub {|Definition:$$GOo|}(Of T As Class, U As T, V As {U, Exception})(aa As T, y As U, z As V)
    End Sub
    Private Sub {|Definition:goo|}(Of T As Class, U As T, V As {U, Exception})(aa As T, y As U, z As V)
        Console.WriteLine("goo")
    End Sub
    Sub Main()
    End Sub
End Module
]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(544437, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544437")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodPartial2_VB(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Public Module Module1
    Partial Private Sub {|Definition:GOo|}(Of T As Class, U As T, V As {U, Exception})(aa As T, y As U, z As V)
    End Sub
    Private Sub {|Definition:$$goo|}(Of T As Class, U As T, V As {U, Exception})(aa As T, y As U, z As V)
        Console.WriteLine("goo")
    End Sub
    Sub Main()
    End Sub
End Module
]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInterfaceMethod(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public Main()
{
    var c1 = new Class1();
    var c2 = new Class2();
    var c3 = new Class3();

    PrintMyName(c1);
    PrintMyName(c2);
    PrintMyName(c3);
}
public void PrintMyName(IClass c)
{
    Console.WriteLine(c.$$[|GetMyName|]());
}
public class Class1 : IClass
{
    public string {|Definition:GetMyName|}()
    {
        return "Class1";
    }
}
public class Class2 : IClass
{
    public string {|Definition:GetMyName|}()
    {
        return "Class2";
    }
}
public class Class3 : Class2
{
    public new string GetMyName()
    {
        return "Class3";
    }
}
public interface IClass
{
    string {|Definition:GetMyName|}();
}
</Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefMethod(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    ///  <see cref="Program.[|Main|]"/> to start the program.
    static void {|Definition:Ma$$in|}(string[] args)
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefMethod2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    ///  <see cref="Program.[|Ma$$in|]"/> to start the program.
    static void {|Definition:Main|}(string[] args)
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefMethodAcrossMultipleFiles(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
partial class Program
{
    ///  <see cref="Program.[|Main|]"/> to start the program.
    static void {|Definition:Ma$$in|}(string[] args)
    {
    }
}
]]>
        </Document>
        <Document><![CDATA[
partial class Program
{
    ///  <see cref="Program.[|Main|]"/>
    void goo() {}
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefMethodAcrossMultipleFiles2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
partial class Program
{
    ///  <see cref="Program.[|Main|]"/> to start the program.
    static void {|Definition:Main|}(string[] args)
    {
    }
}
]]>
        </Document>
        <Document><![CDATA[
partial class Program
{
    ///  <see cref="Program.[|Ma$$in|]"/>
    void goo() {}
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(531010, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531010")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrossAssemblyReferencesFromMetadata(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <MetadataReferenceFromSource Language="Visual Basic" CommonReferences="true">
            <Document FilePath="ReferencedDocument">
                                    Public Interface I
                                        Sub Goo()
                                    End Interface

                                    Friend Class F : Implements I
                                        Public Sub Goo() Implements I.Goo
                                        End Sub
                                    End Class
                                </Document>
        </MetadataReferenceFromSource>
        <Document>
Public Class C
    Sub Bar(i As I)
        i.$$[|Goo|]()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(623148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623148")>
        Public Async Function TestFarWithInternalVisibleTo(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="ProjectA" CommonReferences="true">
        <Document>
            <![CDATA[
            <Assembly: Global.System.Runtime.CompilerServices.InternalsVisibleTo("ProjectB")> 

            Friend Class A
                Public Sub {|Definition:$$Goo|}()
                End Sub
            End Class]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="ProjectB" CommonReferences="true">
        <ProjectReference>ProjectA</ProjectReference>
        <Document>
            <![CDATA[
            Public Class B
                Public Sub Bar(a as A)
                    a.[|Goo|]()
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(657262, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/657262")>
        Public Async Function TestMethodInsideMetadataToSourcePrimitiveTypeInCSharpSource(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="mscorlib" CommonReferences="true">
        <Document>
namespace System
{
    struct Int32
    {
        public override string {|Definition:$$ToString|}() { }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(657262, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/657262")>
        Public Async Function TestMethodInsideMetadataToSourcePrimitiveTypeInVisualBasicSource(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="mscorlib" CommonReferences="true">
        <Document>
Namespace System
    Structure Int32
        Public Overrides Function {|Definition:$$ToString|}() As String
        End Function
    End Structure
End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRetargetingMethod_Basic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableClassLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System.Collections.Generic;

namespace PortableClassLibrary
{
    public class Class1
    {
        int x;
        public void {|Definition:Go$$o|}(int x) { }
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
class Class2
{
    int x;
    public void TestMethod1(PortableClassLibrary.Class1 c)
    {
        c.[|Goo|](x);
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRetargetingMethod_GenericType(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableClassLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;

namespace PortableClassLibrary
{
    public class Class1
    {
        Tuple<int> x;
        public void {|Definition:Go$$o|}(Tuple<int> x) { }
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
using System;

class Class2
{
    Tuple<int> x;
    public void TestMethod1(PortableClassLibrary.Class1 c)
    {
        c.[|Goo|](x);
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRetargetingMethod_FARFromReferencingProject(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableClassLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;

namespace PortableClassLibrary
{
    public class Class1
    {
        Tuple<int> x;
        public void {|Definition:Goo|}(Tuple<int> x) { }
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
using System;

class Class2
{
    Tuple<int> x;
    public void TestMethod1(PortableClassLibrary.Class1 c)
    {
        c.[|$$Goo|](x);
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRetargetingMethod_MultipleForwardedTypes(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableClassLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;

namespace PortableClassLibrary
{
    public class Class1
    {
        Tuple<int> x;
        public void {|Definition:$$Goo|}(Tuple<int> x, float y) { }
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
using System;

class Class2
{
    Tuple<int> x;
    public void TestMethod1(PortableClassLibrary.Class1 c)
    {
        c.[|Goo|](x, 0.0);
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRetargetingMethod_NestedType(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableClassLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;

namespace PortableClassLibrary
{
    public class Class1
    {
        public void {|Definition:$$Goo|}(System.Environment.SpecialFolder x) { }
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
using System;

class Class2
{
    System.Environment.SpecialFolder x;
    public void TestMethod1(PortableClassLibrary.Class1 c)
    {
        c.[|Goo|](x);
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(599, "https://github.com/dotnet/roslyn/issues/599")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRefKindRef_FromDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="Lib" CommonReferences="true">
        <Document><![CDATA[
using System;

public class C
{
    public static void {|Definition:$$M|}(ref int x) { }
}
]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="Test" CommonReferences="true">
        <ProjectReference>Lib</ProjectReference>
        <Document><![CDATA[
Imports System

Class Test
    Sub M()
        Dim x As Integer = 0
        C.[|M|](x)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(599, "https://github.com/dotnet/roslyn/issues/599")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRefKindRef_FromReference(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="Lib" CommonReferences="true">
        <Document><![CDATA[
using System;

public class C
{
    public static void {|Definition:M|}(ref int x) { }
}
]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="Test" CommonReferences="true">
        <ProjectReference>Lib</ProjectReference>
        <Document><![CDATA[
Imports System

Class Test
    Sub M()
        Dim x As Integer = 0
        C.[|$$M|](x)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(599, "https://github.com/dotnet/roslyn/issues/599")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRefKindOut_FromDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="Lib" CommonReferences="true">
        <Document><![CDATA[
using System;

public class C
{
    public static void {|Definition:$$M|}(out int x) { }
}
]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="Test" CommonReferences="true">
        <ProjectReference>Lib</ProjectReference>
        <Document><![CDATA[
Imports System

Class Test
    Sub M()
        Dim x As Integer = 0
        C.[|M|](x)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(599, "https://github.com/dotnet/roslyn/issues/599")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRefKindOut_FromReference(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="Lib" CommonReferences="true">
        <Document><![CDATA[
using System;

public class C
{
    public static void {|Definition:M|}(out int x) { }
}
]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="Test" CommonReferences="true">
        <ProjectReference>Lib</ProjectReference>
        <Document><![CDATA[
Imports System

Class Test
    Sub M()
        Dim x As Integer = 0
        C.[|$$M|](x)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(428072, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/428072")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInterfaceMethodImplementedInStruct1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    public interface IGoo
    {
        void {|Definition:$$Goo|}();
    }

    public struct MyStruct : IGoo
    {
        public void {|Definition:Goo|}()
        {
            throw new System.NotImplementedException();
        }
    }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(428072, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/428072")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInterfaceMethodImplementedInStruct2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    public interface IGoo
    {
        void {|Definition:Goo|}();
    }

    public struct MyStruct : IGoo
    {
        public void {|Definition:$$Goo|}()
        {
            throw new System.NotImplementedException();
        }
    }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
