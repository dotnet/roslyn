' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestTypeParameter1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C<{|Definition:$$T|}>
        {
            void Foo([|T|] t)
            {
            }

            void Foo2(t t1)
            {
            }
        }]]></Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestTypeParameter2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        partial class C<{|Definition:$$T|}>
        {
            void Foo([|T|] t)
            {
            }

            void Foo2(t t1)
            {
            }
        }]]></Document>
        <Document><![CDATA[
        partial class C<{|Definition:T|}>
        {
            void Foo2([|T|] t)
            {
            }
        }]]></Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestTypeParameter3()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        partial class C<{|Definition:$$T|}>
        {
            void Foo(X<[|T|]> t)
            {
            }
        }

        class D<T>
        {
            C<T> c;
        }]]></Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestGenericTypeArgsWithStaticCalls()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class {|Definition:F$$oo|} { void M() { Bar<[|Foo|]>.StaticDoSomething(); } }
class Bar<T> { public static void StaticDoSomething() { } }]]></Document>
    </Project>
</Workspace>

            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestTypeParameterCaseSensitivity()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        partial class C(of {|Definition:$$T|})
            sub Foo(x as [|T|])
            end sub
            sub Foo1(x as [|t|])
            end sub
        end class</Document>
    </Project>
</Workspace>

            Test(input)
        End Sub

        <WorkItem(542598)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestMethodTypeParameterExplicitImplementation1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I
{
    T Foo<T>();
}
 
class A : I
{
    [|$$T|] I.Foo<{|Definition:T|}>() { return default([|T|]); }
}
]]></Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(542598)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestMethodTypeParameterExplicitImplementation2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I
{
    T Foo<T>();
}
 
class A : I
{
    [|T|] I.Foo<{|Definition:$$T|}>() { return default([|T|]); }
}
]]></Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(542598)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestMethodTypeParameterExplicitImplementation3()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
interface I
{
    T Foo<T>();
}
 
class A : I
{
    [|T|] I.Foo<{|Definition:T|}>() { return default([|$$T|]); }
}
]]></Document>
    </Project>
</Workspace>
            Test(input)
        End Sub
    End Class
End Namespace
