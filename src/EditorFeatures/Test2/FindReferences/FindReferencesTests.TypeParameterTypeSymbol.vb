' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestTypeParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C<{|Definition:$$T|}>
        {
            void Goo([|T|] t)
            {
            }

            void Goo2(t t1)
            {
            }
        }]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/62744")>
        Public Async Function TestTypeParameter_NewConstraint_CSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C<{|Definition:$$T|}> where [|T|] : new()
        {
            void Goo()
            {
                new [|T|]();
            }
        }]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/62744")>
        Public Async Function TestTypeParameter_NewConstraint_VisualBasic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
        class C(Of {|Definition:$$T|} As New)
            sub Goo()
                dim x = new [|T|]()
            end sub
        end class]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/23699")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_LocalFunctionTypeParameter(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void M()
    {
        void local<{|Definition:TPar$$am|}>([|TParam|] parameter)
        {
        }
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/23699")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_LocalFunctionTypeParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void M()
    {
        void local<{|Definition:TParam|}>([|TPa$$ram|] parameter)
        {
        }
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/23699")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_LocalFunctionTypeParameter3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void M<{|Definition:TParam|}>()
    {
        void local([|TPa$$ram|] parameter)
        {
        }
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/23699")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_LocalFunctionTypeParameter4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void M<{|Definition:TPa$$ram|}>()
    {
        void local([|TParam|] parameter)
        {
        }
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTypeParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        partial class C<{|Definition:$$T|}>
        {
            void Goo([|T|] t)
            {
            }

            void Goo2(t t1)
            {
            }
        }]]></Document>
        <Document><![CDATA[
        partial class C<{|Definition:T|}>
        {
            void Goo2([|T|] t)
            {
            }
        }]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTypeParameter3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        partial class C<{|Definition:$$T|}>
        {
            void Goo(X<[|T|]> t)
            {
            }
        }

        class D<T>
        {
            C<T> c;
        }]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestGenericTypeArgsWithStaticCalls(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class {|Definition:G$$oo|} { void M() { Bar<[|Goo|]>.StaticDoSomething(); } }
class Bar<T> { public static void StaticDoSomething() { } }]]></Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTypeParameterCaseSensitivity(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        partial class C(of {|Definition:$$T|})
            sub Goo(x as [|T|])
            end sub
            sub Goo1(x as [|t|])
            end sub
        end class</Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542598")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestMethodTypeParameterExplicitImplementation1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I
{
    T Goo<T>();
}
 
class A : I
{
    [|$$T|] I.Goo<{|Definition:T|}>() { return default([|T|]); }
}
]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542598")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestMethodTypeParameterExplicitImplementation2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I
{
    T Goo<T>();
}
 
class A : I
{
    [|T|] I.Goo<{|Definition:$$T|}>() { return default([|T|]); }
}
]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542598")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestMethodTypeParameterExplicitImplementation3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I
{
    T Goo<T>();
}
 
class A : I
{
    [|T|] I.Goo<{|Definition:T|}>() { return default([|$$T|]); }
}
]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
