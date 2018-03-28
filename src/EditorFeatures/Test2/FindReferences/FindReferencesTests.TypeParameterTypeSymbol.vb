' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTypeParameter1() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(23699, "https://github.com/dotnet/roslyn/issues/23699")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_LocalFunctionTypeParameter() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(23699, "https://github.com/dotnet/roslyn/issues/23699")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_LocalFunctionTypeParameter2() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(23699, "https://github.com/dotnet/roslyn/issues/23699")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_LocalFunctionTypeParameter3() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(23699, "https://github.com/dotnet/roslyn/issues/23699")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_LocalFunctionTypeParameter4() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTypeParameter2() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTypeParameter3() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestGenericTypeArgsWithStaticCalls() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class {|Definition:G$$oo|} { void M() { Bar<[|Goo|]>.StaticDoSomething(); } }
class Bar<T> { public static void StaticDoSomething() { } }]]></Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTypeParameterCaseSensitivity() As Task
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

            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(542598, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542598")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodTypeParameterExplicitImplementation1() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(542598, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542598")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodTypeParameterExplicitImplementation2() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(542598, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542598")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodTypeParameterExplicitImplementation3() As Task
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
            Await TestAPIAndFeature(input)
        End Function
    End Class
End Namespace
