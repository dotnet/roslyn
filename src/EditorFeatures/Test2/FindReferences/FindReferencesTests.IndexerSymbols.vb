' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529629")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_Indexer1(kind As TestKind, host As TestHost) As Task
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
    void Goo()
    {
        var q = new C();
        var b = q[||][4];
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529629")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestBasic_Indexer1(kind As TestKind, host As TestHost) As Task
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
    sub Goo()
        dim q = new C()
        dim b = q[||](4)
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545577")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestBasic_Indexer2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class A
    Default ReadOnly Property {|Definition:$$Goo|}(ByVal x As Integer) As Integer
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/650779")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestBasic_Indexer3(kind As TestKind, host As TestHost) As Task
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

    Public Sub Goo(c As C)
        c = c.[|Item|](2)
        c[||](1) = c
        c.[|Item|](1) = c
        c[||](1).[|Item|](1) = c
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/661362")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestBasic_Indexer4(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/39847")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_Indexer_Conditional(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
   private string[] arr = new T[100];

   public string {|Definition:$$this|}[int i]
   {
      get { return arr[i]; }
      set { arr[i] = value; }
   }
}
class B
{
    private A a;
    void M2()
    {
         var s = a?[||][0];
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/39847")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestBasic_Indexer_Conditional(kind As TestKind, host As TestHost) As Task
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

    Public Sub Goo(c As C)
        c = c?.[|Item|](2)
        c = c?[||](1)
        c = c?.[|Item|](1)
        c = c?[||](1)?.[|Item|](1)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/39847")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_Indexer_CRef(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
   private string[] arr = new T[100];
   
   /// &lt;see cref="[||]this[int]"/&gt;
   public string {|Definition:$$this|}[int i]
   {
      get { return arr[i]; }
      set { arr[i] = value; }
   }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/39847")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestBasic_Indexer_Cref(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class C
    ''' &lt;see cref="[|Item|]"/&gt;
    public default readonly property {|Definition:$$Item|}(y as Integer) as Integer
        get
            return 0
        end get
    end property
end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestIndexerReferenceInGlobalSuppression(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~P:C.[|Item|](System.Int32)")]

class C
{
    public int {|Definition:$$this|}[int y] { get { } }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestParameterizedPropertyReferenceInGlobalSuppression(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope:="member", Target:="~P:C.[|Goo|](System.Int32)")>

Class C
    ReadOnly Property {|Definition:$$Goo|}(x As Integer) As Integer
        Get
            Return 0
        End Get
    End Property
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharp_IndexerInSourceGeneratedDocument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    public int {|Definition:$$this|}[int y] { get { } }
}
        </Document>
        <DocumentFromSourceGenerator>

class D
{
    void Goo()
    {
        var q = new C();
        var b = q[||][4];
    }
}
        </DocumentFromSourceGenerator>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/40978")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitElementAccessExpression1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class Test
{
  int {|Definition:$$this|}[int index] { get => 0; set { } }

  Test Create() { return new Test() { [||][0] = 0, [||][1] = 1 }; }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/31819")>
        Public Async Function TestCSharp_Indexer_AtReferenceLocation(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    public int {|Definition:this|}[int y] { get { } }
}

class D
{
    void Goo()
    {
        var q = new C();
        var b = q[||]$$[4];
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
