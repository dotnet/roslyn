' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
#Region "FAR on generic methods"
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_Parameter1() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_Parameter3() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_ParameterCaseSensitivity() As Task
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

            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_MethodCall() As Task
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
            Await TestAsync(input)
        End Function
#End Region

#Region "FAR on generic partial methods"

        <WorkItem(544436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544436")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_GenericPartialParameter_CSharp1() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(544436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544436"), WorkItem(544475, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544475")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_GenericPartialParameter_CSharp2() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(544435, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544435")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_GenericPartialParameter_VB1() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(544435, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544435")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_GenericPartialParameter_VB2() As Task
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
            Await TestAsync(input)
        End Function

#End Region
    End Class
End Namespace
