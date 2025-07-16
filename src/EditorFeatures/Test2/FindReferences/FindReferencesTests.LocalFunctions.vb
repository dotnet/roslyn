' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WorkItem("https://github.com/dotnet/roslyn/issues/18761")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalFunction1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Test
{
    void Main()
    {
        {
            int x = 1;
            [|$$Print|](x);
            void {|Definition:Print|}(int y) { }
        }

        {
            int z = 1;
            Print(z);
            void Print(int y) { }
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/18761")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalFunction2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Test
{
    void Main()
    {
        {
            int x = 1;
            [|Print|](x);
            void {|Definition:$$Print|}(int y) { }
        }

        {
            int z = 1;
            Print(z);
            void Print(int y) { }
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/18761")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalFunction3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Test
{
    void Main()
    {
        {
            int x = 1;
            Print(x);
            void Print(int y) { }
        }

        {
            int z = 1;
            [|$$Print|](z);
            void {|Definition:Print|}(int y) { }
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/18761")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalFunction4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Test
{
    void Main()
    {
        {
            int x = 1;
            Print(x);
            void Print(int y) { }
        }

        {
            int z = 1;
            [|Print|](z);
            void {|Definition:$$Print|}(int y) { }
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/18761")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGenericLocalFunction1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Test
{
    void Main()
    {
        int x = 1;
        [|Print|](x);
        [|Print|]&lt;int&gt;(x);
        void {|Definition:$$Print|}&lt;T&gt;(T y) { }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/18761")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGenericLocalFunction2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Test
{
    void Main()
    {
        int x = 1;
        [|Print|](x);
        [|$$Print|]&lt;int&gt;(x);
        void {|Definition:Print|}&lt;T&gt;(T y) { }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/18761")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGenericLocalFunction3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Test
{
    void Main()
    {
        int x = 1;
        [|$$Print|](x);
        [|Print|]&lt;int&gt;(x);
        void {|Definition:Print|}&lt;T&gt;(T y) { }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalFunctionUsedInSourceGeneratedDocument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <DocumentFromSourceGenerator>
class Test
{
    void Main()
    {
        int x = 1;
        [|$$Print|](x);
        void {|Definition:Print|}(int y) { }
    }
}
        </DocumentFromSourceGenerator>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
