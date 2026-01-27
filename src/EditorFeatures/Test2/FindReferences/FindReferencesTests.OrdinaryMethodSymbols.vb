' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18963")>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18963")>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18963")>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18963")>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/24184")>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18963")>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18963")>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18963")>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18963")>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18963")>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodOverride_InMetadata_Api(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodOverride_InMetadata_Feature(host As TestHost) As Task
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
            public override string ToString() { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodInterfaceInheritance_FromReference_Api(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodInterfaceInheritance_FromReference_Feature(host As TestHost) As Task
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
            public void Goo()
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
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodInterfaceInheritance_FromDefinition_Api(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodInterfaceInheritance_FromDefinition_Feature(host As TestHost) As Task
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
            void Goo();
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
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529616")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522786")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34107")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34107"), CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522786")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34107")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34107"), CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/634818")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34106")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34106"), CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636943")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34106")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34106"), CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636943")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34106")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34106"), CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636943")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34106")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34106"), CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528936")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34105")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34105"), CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528936")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34105")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/34105"), CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538794")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538794")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/2544")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/2544")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538726")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodInterfaceMethodsDoNotCascadeThroughOtherInterfaceMethods1(kind As TestKind, host As TestHost) As Task
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538726")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodInterfaceMethodsDoNotCascadeThroughOtherInterfaceMethods2(kind As TestKind, host As TestHost) As Task
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538726")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodInterfaceMethodsDoNotCascadeThroughOtherInterfaceMethods3(kind As TestKind, host As TestHost) As Task
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538898")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539033")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539033")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface2_Api(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539033")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface2_Feature(host As TestHost) As Task
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
        public void F()
        {
            F();
        }
    }
]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539033")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface3_Api(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539033")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface3_Feature(host As TestHost) As Task
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
        void I<U>.F() { }
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
            Await TestStreamingFeature(input, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539033")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface4_Api(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539033")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface4_Feature(host As TestHost) As Task
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
        void I<U>.F() { }
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
            Await TestStreamingFeature(input, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539046")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539046")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539046")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539646")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539646")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539646")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539824")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539824")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540349")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540349")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540349")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540359")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540359")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540359")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540359")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540946")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540946")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540946")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542034")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542034")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543002")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
        Public Async Function TestForEachGetEnumeratorViaExtension(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
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
}

public static class Extensions
{
    public static B {|Definition:$$GetEnumerator|}(this C c)
    {
        return null;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543002")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543002")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544439")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544439")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544439")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodExtendedPartial1_CSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
partial class Class1
{
    public partial void {|Definition:$$goo|}<T, U, V>(T x, U y, V z) where T : class where U : Exception, T where V : U;
    public partial void {|Definition:goo|}<T, U, V>(T x, U y, V z) where T : class where U : Exception, T where V : U
    {
    }
}]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544439")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodExtendedPartial2_CSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
partial class Class1
{
    public partial void {|Definition:$$goo|}<T, U, V>(T x, U y, V z) where T : class where U : Exception, T where V : U;
    public partial void {|Definition:goo|}<T, U, V>(T x, U y, V z) where T : class where U : Exception, T where V : U
    {
    }
}]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544437")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544437")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531010")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623148")>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/657262")>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/657262")>
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

        <WpfTheory, CombinatorialData>
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
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true">
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

        <WpfTheory, CombinatorialData>
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
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true">
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

        <WpfTheory, CombinatorialData>
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
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true">
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

        <WpfTheory, CombinatorialData>
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
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true">
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/55955")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestRetargetingInheritanceAcrossProjects(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableInterfaceLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System;

public interface IInterface
{
    void {|Definition:$$Method|}(string s) { }
}
]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="PortableClassLibrary1" CommonReferencesPortable="true">
        <ProjectReference>PortableInterfaceLibrary</ProjectReference>
        <Document><![CDATA[
using System;

public class PortableClass : IInterface
{
    public virtual void {|Definition:Method|}(string s) {}
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="NormalClassLibrary1" CommonReferences="true">
        <ProjectReference>PortableInterfaceLibrary</ProjectReference>
        <Document><![CDATA[
using System;

public class NormalClass : IInterface
{
    public virtual void {|Definition:Method|}(string s) {}
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="ReferencesBoth" CommonReferences="true">
        <ProjectReference>PortableInterfaceLibrary</ProjectReference>
        <ProjectReference>PortableClassLibrary1</ProjectReference>
        <ProjectReference>NormalClassLibrary1</ProjectReference>
        <Document><![CDATA[
using System;

public class C
{
    void X(string s)
    {
        new PortableClass().[|Method|](s);
        new NormalClass().[|Method|](s);
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
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
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true">
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/599")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/599")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/599")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/599")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/428072")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/428072")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestMethodReferenceInGlobalSuppression(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:C.[|M|]")]

        class C
        {
            private void {|Definition:$$M|}() { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestMethodReferenceInGlobalSuppression_MethodWithParameters(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:C.[|M|](System.String)")]
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:C.M(System.Int32)")]

        class C
        {
            private void {|Definition:$$M|}(string s) { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodWithMissingReferences_CSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="false">
        <Document>
        class C
        {
            // string will be an error type because we have no actual references.
            private void {|Definition:Goo|}(string s) { }

            void Bar()
            {
                [|Go$$o|]("");
                [|Goo|](s);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodWithMissingReferences_VB(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="false">
        <Document>
        class C
            ' string will be an error type because we have no actual references.
            private sub {|Definition:Goo|}(s as string)
            end sub

            sub Bar()
                [|Go$$o|]("")
                [|Goo|](s)
            end sub
        end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestOrdinaryMethodUsedInSourceGenerator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        partial class C
        {
            private void {|Definition:Goo|}() { }
        }
        </Document>
        <DocumentFromSourceGenerator>

        partial class C
        {
            void Bar()
            {
                [|Go$$o|]();
                [|Goo|]();
                B.Goo();
                new C().[|Goo|]();
                new C().goo();
            }
        }

        </DocumentFromSourceGenerator>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestFeatureHierarchyCascade1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            void {|Definition:$$Goo|}();
        }

        interface I2
        {
            void Goo();
        }

        class B : I1
        {
            public virtual void {|Definition:Goo|}() {}
        }

        class D1 : B, I1, I2
        {
            public override void {|Definition:Goo|}() {}
        }

        class D2 : B, I1
        {
            public override void {|Definition:Goo|}() {}
        }
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestFeatureHierarchyCascade2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            void Goo();
        }

        interface I2
        {
            void {|Definition:$$Goo|}();
        }

        class B : I1
        {
            public virtual void Goo() {}
        }

        class D1 : B, I1, I2
        {
            public override void {|Definition:Goo|}() {}
        }

        class D2 : B, I1
        {
            public override void Goo() {}
        }
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestFeatureHierarchyCascade3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            void {|Definition:Goo|}();
        }

        interface I2
        {
            void Goo();
        }

        class B : I1
        {
            public virtual void {|Definition:$$Goo|}() {}
        }

        class D1 : B, I1, I2
        {
            public override void {|Definition:Goo|}() {}
        }

        class D2 : B, I1
        {
            public override void {|Definition:Goo|}() {}
        }
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestFeatureHierarchyCascade4(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            void {|Definition:Goo|}();
        }

        interface I2
        {
            void {|Definition:Goo|}();
        }

        class B : I1
        {
            public virtual void {|Definition:Goo|}() {}
        }

        class D1 : B, I1, I2
        {
            public override void {|Definition:$$Goo|}() {}
        }

        class D2 : B, I1
        {
            public override void Goo() {}
        }
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestFeatureHierarchyCascade5(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            void {|Definition:Goo|}();
        }

        interface I2
        {
            void Goo();
        }

        class B : I1
        {
            public virtual void {|Definition:Goo|}() {}
        }

        class D1 : B, I1, I2
        {
            public override void Goo() {}
        }

        class D2 : B, I1
        {
            public override void {|Definition:$$Goo|}() {}
        }
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestMemberStaticAbstractMethodFromInterface(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            static abstract void {|Definition:M$$1|}();
        }
        class C1_1 : I1
        {
            public static void {|Definition:M1|}() { }
        }
        class C1_2 : I1
        {
            static void I1.{|Definition:M1|}() { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestDerivedMemberStaticAbstractMethodViaFeature1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            static abstract void {|Definition:M1|}();
        }
        class C1_1 : I1
        {
            public static void {|Definition:M$$1|}() { }
        }
        class C1_2 : I1
        {
            static void I1.M1() { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestDerivedMemberStaticAbstractMethodViaFeature2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            static abstract void {|Definition:M1|}();
        }
        class C1_1 : I1
        {
            public static void M1() { }
        }
        class C1_2 : I1
        {
            static void I1.{|Definition:M$$1|}() { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestDerivedMemberStaticAbstractMethodViaAPI1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            static abstract void {|Definition:M1|}();
        }
        class C1_1 : I1
        {
            public static void {|Definition:M$$1|}() { }
        }
        class C1_2 : I1
        {
            static void I1.{|Definition:M1|}() { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestDerivedMemberStaticAbstractMethodViaAPI2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            static abstract void {|Definition:M1|}();
        }
        class C1_1 : I1
        {
            public static void {|Definition:M1|}() { }
        }
        class C1_2 : I1
        {
            static void I1.{|Definition:M$$1|}() { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34107")>
        Public Async Function CSharp_TestDisposeUsedInUsingDeclaration1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        struct S
        {
            public void {|Definition:$$Dispose|}() { }
        }

        class C
        {
            void M()
            {
                [|using|] (var s = new S())
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34107")>
        Public Async Function CSharp_TestDisposeUsedInUsingDeclaration2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        struct S
        {
            public void {|Definition:Dispose|}() { }
        }

        class C
        {
            void M()
            {
                [|$$using|] (var s = new S())
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34107")>
        Public Async Function CSharp_TestDisposeUsedInUsingDeclaration3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        struct S
        {
            public void {|Definition:$$Dispose|}() { }
        }

        class C
        {
            void M()
            {
                [|using|] var s = new S();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34107")>
        Public Async Function CSharp_TestDisposeUsedInUsingDeclaration4(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        struct S
        {
            public void {|Definition:Dispose|}() { }
        }

        class C
        {
            void M()
            {
                [|$$using|] var s = new S();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34107")>
        Public Async Function CSharp_TestDisposeUsedInUsingDeclaration5(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        struct S
        {
            public void {|Definition:$$Dispose|}() { }
        }

        class C
        {
            void M()
            {
                [|using|] (new S())
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34107")>
        Public Async Function CSharp_TestDisposeUsedInUsingDeclaration6(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        struct S
        {
            public void {|Definition:Dispose|}() { }
        }

        class C
        {
            void M()
            {
                [|$$using|] (new S())
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34107")>
        Public Async Function VisualBasic_TestDisposeUsedInUsingDeclaration1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        imports System
        structure S
            implements IDisposable

            public sub {|Definition:$$Dispose|}() Implements IDisposable.[|Dispose|]
            end sub
        end structure

        class C
            sub M()
                [|using|] (new S())
                end using
            end sub
        end class
        </Document>
    </Project>
</Workspace>

            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34107")>
        Public Async Function VisualBasic_TestDisposeUsedInUsingDeclaration2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        imports System
        structure S
            implements IDisposable

            public sub {|Definition:Dispose|}() Implements IDisposable.[|Dispose|]
            end sub
        end structure

        class C
            sub M()
                [|$$using|] (new S())
                end using
            end sub
        end class
        </Document>
    </Project>
</Workspace>

            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34107")>
        Public Async Function VisualBasic_TestDisposeUsedInUsingDeclaration3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        imports System
        structure S
            implements IDisposable

            public sub {|Definition:$$Dispose|}() Implements IDisposable.[|Dispose|]
            end sub
        end structure

        class C
            sub M()
                [|using|] x = new S()
                end using
            end sub
        end class
        </Document>
    </Project>
</Workspace>

            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/34107")>
        Public Async Function VisualBasic_TestDisposeUsedInUsingDeclaration4(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        imports System
        structure S
            implements IDisposable

            public sub {|Definition:Dispose|}() Implements IDisposable.[|Dispose|]
            end sub
        end structure

        class C
            sub M()
                [|$$using|] x = new S()
                end using
            end sub
        end class
        </Document>
    </Project>
</Workspace>

            Await TestAPI(input, host)
        End Function

#Region "Collection Initializers"

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/22449")>
        Public Async Function CollectionInitializer1_CSharp(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System.Collections.Generic;

internal static class C
{
    private static void {|Definition:$$Add|}(this Dictionary<int, int> dictionary, int value) =>
        dictionary.Add(value, value * 2);

    public static Dictionary<int, int> GetMapping() =>
        new Dictionary<int, int> { [|1|], [|2|], [|3|] };

    public static Dictionary<int, int> GetMapping2()
    {
        var dictionary = new Dictionary<int, int>();
        dictionary.[|Add|](1);
        dictionary.[|Add|](2);
        dictionary.[|Add|](3);
        return dictionary;
    }
}

        ]]></Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/22449")>
        Public Async Function CollectionInitializer2_CSharp(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System.Collections.Generic;

internal class X
{
    public Dictionary<int, int> Y { get; }
}

internal static class C
{
    private static void {|Definition:$$Add|}(this Dictionary<int, int> dictionary, int value) =>
        dictionary.Add(value, value * 2);

    public static Dictionary<int, int> GetMapping2()
    {
        new X
        {
            Y = { [|1|], [|2|], [|3|] }
        };
    }
}

        ]]></Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/22449")>
        Public Async Function CollectionInitializer1_VisualBasic(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
imports System.Collections.Generic
imports System.Runtime.CompilerServices

Friend Module M
    <Extension>
    Public Sub Add(dict As Dictionary(Of Integer, Integer), value As Integer)
        dict.Add(value, value * 2)
    End Sub
End Module

class C
    public shared function GetMapping() as Dictionary(Of Integer, Integer)
        return new Dictionary(Of Integer, Integer) From {
            [|1|],
            [|2|],
            [|3|]
        }
    end function
end class

        ]]></Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

#End Region

#Region "Collection Expressions"

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81767")>
        Public Async Function CollectionExpression_Builder1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesNet9="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

MyCollection<int> mc = [|[]|];

[CollectionBuilder(typeof(MyCollection), "Create")]
public class MyCollection<T> : IEnumerable<T>
{
}

public class MyCollection
{
    public static MyCollection<T> {|Definition:$$Create|}<T>(ReadOnlySpan<T> elements) => null;
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81767")>
        Public Async Function CollectionExpression_Builder2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesNet9="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

MyCollection<int> mc1 = [|[]|];
MyCollection<int> mc2 = [|[with(1)]|];

[CollectionBuilder(typeof(MyCollection), "Create")]
public class MyCollection<T> : IEnumerable<T>
{
}

public class MyCollection
{
    public static MyCollection<T> {|Definition:$$Create|}<T>(int arg, ReadOnlySpan<T> elements) => null;
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81767")>
        Public Async Function CollectionExpression_Builder3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesNet9="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

MyCollection<int> mc1 = [];
MyCollection<int> mc2 = [|[with(1)]|];

[CollectionBuilder(typeof(MyCollection), "Create")]
public class MyCollection<T> : IEnumerable<T>
{
}

public class MyCollection
{
    public static MyCollection<T> Create<T>(ReadOnlySpan<T> elements) => null;
    public static MyCollection<T> {|Definition:$$Create|}<T>(int arg, ReadOnlySpan<T> elements) => null;
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81767")>
        Public Async Function CollectionExpression_Builder4(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesNet9="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

MyCollection<int> mc1 = [|[]|];
MyCollection<int> mc2 = [with(1)];

[CollectionBuilder(typeof(MyCollection), "Create")]
public class MyCollection<T> : IEnumerable<T>
{
}

public class MyCollection
{
    public static MyCollection<T> {|Definition:$$Create|}<T>(ReadOnlySpan<T> elements) => null;
    public static MyCollection<T> Create<T>(int arg, ReadOnlySpan<T> elements) => null;
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

#End Region
    End Class
End Namespace
