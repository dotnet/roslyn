' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
#Region "FAR on generic methods"
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestMethodType_Parameter1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C
        {
            void Foo<{|Definition:$$T|}>([|T|] x1, t x2)
            {
            }
        }]]></Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestMethodType_Parameter3()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        partial class C
        {
            void Foo<{|Definition:$$T|}>(X<[|T|]> t)
            {
            }
    
            void Bar<T>(T t)
            {
            }
        }]]></Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestMethodType_ParameterCaseSensitivity()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        partial class C
            sub Foo(of {|Definition:$$T|})(x as [|T|], x1 as [|t|])
            end sub
        end class</Document>
    </Project>
</Workspace>

            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestMethodType_MethodCall()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
        interface GenericInterface<T>
        {
            void {|Definition:IntMethod|}<T>(T t);
        }
        class GenericClass<T> : GenericInterface<T>
        {
            public void {|Definition:IntMethod|}<T>(T t) { }
        }
        class M
        {
            public M()
            {
                GenericClass<string> GCObj = new GenericClass<string>();
                GCObj.[|$$IntMethod|]<string>("foo");
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub
#End Region

#Region "FAR on generic partial methods"

        <WorkItem(544436)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestMethodType_GenericPartialParameter_CSharp1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        partial class C
        {
            partial void Foo<{|Definition:$$T|}>([|T|] t)
            {
            }
        }]]></Document>
        <Document><![CDATA[
        partial class C
        {
            partial void Foo<{|Definition:T|}>([|T|] t);
        }]]></Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(544436), WorkItem(544475)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestMethodType_GenericPartialParameter_CSharp2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        partial class C
        {
            partial void Foo<{|Definition:T|}>([|T|] t)
            {
            }
        }]]></Document>
        <Document><![CDATA[
        partial class C
        {
            partial void Foo<{|Definition:$$T|}>([|T|] t);
        }]]></Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(544435)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestMethodType_GenericPartialParameter_VB1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            partial class C
                sub Foo(Of {|Definition:$$T|})(t as [|T|])
                end sub
            end class]]>
        </Document>
        <Document><![CDATA[
            partial class C
                partial sub Foo(Of {|Definition:T|})(t as [|T|])
                end sub
            end class]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(544435)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestMethodType_GenericPartialParameter_VB2()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            partial class C
                sub Foo(Of {|Definition:T|})(t as [|T|])
                end sub
            end class]]>
        </Document>
        <Document><![CDATA[
            partial class C
                partial sub Foo(Of {|Definition:$$T|})(t as [|T|])
                end sub
            end class]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

#End Region
    End Class
End Namespace
