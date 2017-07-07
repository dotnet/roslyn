﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WorkItem(18761, "https://github.com/dotnet/roslyn/issues/18761")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLocalFunction1() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(18761, "https://github.com/dotnet/roslyn/issues/18761")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLocalFunction2() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(18761, "https://github.com/dotnet/roslyn/issues/18761")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLocalFunction3() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(18761, "https://github.com/dotnet/roslyn/issues/18761")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLocalFunction4() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(18761, "https://github.com/dotnet/roslyn/issues/18761")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestGenericLocalFunction1() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(18761, "https://github.com/dotnet/roslyn/issues/18761")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestGenericLocalFunction2() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(18761, "https://github.com/dotnet/roslyn/issues/18761")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestGenericLocalFunction3() As Task
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
            Await TestAPIAndFeature(input)
        End Function
    End Class
End Namespace
