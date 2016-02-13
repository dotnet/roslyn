' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WorkItem(529629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529629")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_Indexer1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    public int {|Definition:$$this|}[int y] { get { } }
}

class D
{
    void Foo()
    {
        var q = new C();
        var b = q[||][4];
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(529629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529629")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_Indexer1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    public default readonly property {|Definition:$$Item|}(y as Integer) as Integer
        get
            return 0
        end get
    end property
end class

class D
    sub Foo()
        dim q = new C()
        dim b = q[||](4)
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(545577, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545577")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_Indexer2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class A
    Default ReadOnly Property {|Definition:$$Foo|}(ByVal x As Integer) As Integer
        Get
        End Get
    End Property
    Shared Sub Main()
        Dim x As New A
        Dim y = x[||](1)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(650779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/650779")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_Indexer3() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Public Class C
    Default Public Property {|Definition:$$Item|}(index As Integer) As C
        Get
            Return Nothing
        End Get
        Set(value As C)

        End Set
    End Property

    Public Sub Foo(c As C)
        c = c.[|Item|](2)
        c[||](1) = c
        c.[|Item|](1) = c
        c[||](1).[|Item|](1) = c
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(661362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/661362")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBasic_Indexer4() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Default Public Property {|Definition:$$Hello|}(x As String) As String
        Get
            Return Nothing
        End Get
        Set(value As String)
        End Set
    End Property
 
End Class
Module Program
    Sub Main(args As String())
        Dim x As New C
        Dim y = x![||]HELLO
        Dim z = x![||]HI
        x[||]("HELLO") = ""
    End Sub
End Module

        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
    End Class
End Namespace
