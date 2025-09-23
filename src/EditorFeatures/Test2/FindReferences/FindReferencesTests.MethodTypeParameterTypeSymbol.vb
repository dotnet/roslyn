' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
#Region "FAR on generic methods"

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/62744")>
        Public Async Function TestMethodTypeParameter_NewConstraint_CSharp1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C
        {
            void Goo<{|Definition:$$T|}>() where [|T|] : new()
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/78649")>
        Public Async Function TestMethodTypeParameter_NewConstraint_CSharp2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        class C
        {
            void Goo<{|Definition:T|}>() where [|T|] : new()
            {
                new [|$$T|]();
            }
        }]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/62744")>
        Public Async Function TestMethodTypeParameter_NewConstraint_VisualBasic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
        class C
            sub Goo(Of {|Definition:$$T|} As New)()
                dim x = new [|T|]()
            end sub
        end class]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/76650")>
        Public Async Function TestMethodTypeParameter_TopLevelLocalFunction(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
[|T|] TopLevelLocalFunction<{|Definition:$$T|}>() where [|T|] : new()
{
    return new [|T|]();
}]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/76650")>
        Public Async Function TestMethodTypeParameter_MethodLevelLocalFunction(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void Goo()
    {
        [|T|] LocalFunction<{|Definition:$$T|}>() where [|T|] : new()
        {
            return new [|T|]();
        }
    }
}]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/76650")>
        Public Async Function TestMethodTypeParameter_NormalMethod(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    [|T|] TopLevelMethod<{|Definition:$$T|}>() where [|T|] : new()
    {
        return new [|T|]();
    }
}
}]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

#End Region

#Region "FAR on generic partial methods"

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544436")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544436"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544475")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544435")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544435")>
        <WpfTheory, CombinatorialData>
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
