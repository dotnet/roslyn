' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
#Region "FAR on generic methods"
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_Parameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C
        {
            void Goo<{|Definition:$$T|}>([|T|] x1, t x2)
            {
            }
        }]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_Parameter3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        partial class C
        {
            void Goo<{|Definition:$$T|}>(X<[|T|]> t)
            {
            }
    
            void Bar<T>(T t)
            {
            }
        }]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_ParameterCaseSensitivity(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        partial class C
            sub Goo(of {|Definition:$$T|})(x as [|T|], x1 as [|t|])
            end sub
        end class</Document>
    </Project>
</Workspace>

            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_MethodCall(kind As TestKind, host As TestHost) As Task
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
                GCObj.[|$$IntMethod|]<string>("goo");
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
#End Region

#Region "FAR on generic partial methods"

        <WorkItem(544436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544436")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_GenericPartialParameter_CSharp1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        partial class C
        {
            partial void Goo<{|Definition:$$T|}>([|T|] t)
            {
            }
        }]]></Document>
        <Document><![CDATA[
        partial class C
        {
            partial void Goo<{|Definition:T|}>([|T|] t);
        }]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(544436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544436"), WorkItem(544475, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544475")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_GenericPartialParameter_CSharp2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        partial class C
        {
            partial void Goo<{|Definition:T|}>([|T|] t)
            {
            }
        }]]></Document>
        <Document><![CDATA[
        partial class C
        {
            partial void Goo<{|Definition:$$T|}>([|T|] t);
        }]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(544435, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544435")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_GenericPartialParameter_VB1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            partial class C
                sub Goo(Of {|Definition:$$T|})(t as [|T|])
                end sub
            end class]]>
        </Document>
        <Document><![CDATA[
            partial class C
                partial sub Goo(Of {|Definition:T|})(t as [|T|])
                end sub
            end class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(544435, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544435")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodType_GenericPartialParameter_VB2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            partial class C
                sub Goo(Of {|Definition:T|})(t as [|T|])
                end sub
            end class]]>
        </Document>
        <Document><![CDATA[
            partial class C
                partial sub Goo(Of {|Definition:$$T|})(t as [|T|])
                end sub
            end class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

#End Region
    End Class
End Namespace
