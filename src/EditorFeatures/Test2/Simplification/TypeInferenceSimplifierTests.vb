' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    <Trait(Traits.Feature, Traits.Features.Simplification)>
    Public Class TypeInferenceSimplifierTests
        Inherits AbstractSimplificationTests
        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/734369")>
        Public Async Function TestDoNotSimplify1() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    Imports System
                    Class C
                    End Class
                    Class B
                        Inherits C
                        Public Shared Function Goo() As Integer
                        End Function
                    End Class
                    Module Program
                        Sub Main(args As String())
                            Dim {|SimplifyParent:AA|}
                            Dim {|SimplifyParent:A As Integer|}
                            Dim {|SimplifyParent:F(), G() As String|}
                            Dim {|SimplifyParent:M() As String|}, {|SimplifyParent:N() As String|}
                            Dim {|SimplifyParent:E As String = 5|}
                            Dim {|SimplifyParent:arr(,) As Double = {{1,2},{3,2}}|}
                            Dim {|SimplifyParent:arri() As Double = {1,2}|}
                            Dim {|SimplifyParent:x As IEnumerable(Of C) = New List(Of B)|}
                            Dim {|SimplifyParent:obj As C = New B()|}
                            Dim {|SimplifyParent:ret as Double = B.Goo()|}
                            Const {|SimplifyParent:con As Double = 1|}
                        End Sub
                    End Module
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    Imports System
                    Class C
                    End Class
                    Class B
                        Inherits C
                        Public Shared Function Goo() As Integer
                        End Function
                    End Class
                    Module Program
                        Sub Main(args As String())
                            Dim AA
                            Dim A As Integer
                            Dim F(), G() As String
                            Dim M() As String, N() As String
                            Dim E As String = 5
                            Dim arr(,) As Double = {{1,2},{3,2}}
                            Dim arri() As Double = {1,2}
                            Dim x As IEnumerable(Of C) = New List(Of B)
                            Dim obj As C = New B()
                            Dim ret as Double = B.Goo()
                            Const con As Double = 1
                        End Sub
                    End Module
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/734369")>
        Public Async Function TestSimplify_ArrayElementConversion() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    Imports System
                    Module Program
                        Sub Main(args As String())
                            Dim {|SimplifyParent:arr(,) As Double = {{1.9,2},{3,2}}|}
                        End Sub
                    End Module
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    Imports System
                    Module Program
                        Sub Main(args As String())
                            Dim arr = {{1.9,2},{3,2}}
                        End Sub
                    End Module
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestDoNotSimplify_Using() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    Imports System
                    Imports System.Collections.Generic
                    Imports System.Linq

                    Class B
                        Implements IDisposable

                        Public Sub Dispose() Implements IDisposable.Dispose
                            Throw New NotImplementedException()
                        End Sub
                    End Class

                    Class D
                        Inherits B

                    End Class
                    Class Program
                        Sub Main(args As String())
                            Using {|SimplifyParent:b As B|} = New D()

                            End Using
                        End Sub
                    End Class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    Imports System
                    Imports System.Collections.Generic
                    Imports System.Linq

                    Class B
                        Implements IDisposable

                        Public Sub Dispose() Implements IDisposable.Dispose
                            Throw New NotImplementedException()
                        End Sub
                    End Class

                    Class D
                        Inherits B

                    End Class
                    Class Program
                        Sub Main(args As String())
                            Using b As B = New D()

                            End Using
                        End Sub
                    End Class
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestDoNotSimplify_For_0() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    Imports System
                    Module Program
                        Sub Main(args As String())
                            For {|SimplifyParent:index As Long|} = 1 To 5
                            Next
                            For Each {|SimplifyParent:index As Long|} In New Integer() {1, 2, 3}
                            Next
                        End Sub
                    End Module
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    Imports System
                    Module Program
                        Sub Main(args As String())
                            For index As Long = 1 To 5
                            Next
                            For Each index As Long In New Integer() {1, 2, 3}
                            Next
                        End Sub
                    End Module
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestDoNotSimplify_For_1() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    Imports System
                    Imports System.Collections.Generic
                    Imports System.Linq

                    Class B
                    End Class
                    Class Program
                        Inherits B
                        Sub Main(args As String())
                        End Sub

                        Sub Madin(args As IEnumerable(Of Program))
                            For Each {|SimplifyParent:index As B|} In args
                            Next
                        End Sub
                    End Class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    Imports System
                    Imports System.Collections.Generic
                    Imports System.Linq

                    Class B
                    End Class
                    Class Program
                        Inherits B
                        Sub Main(args As String())
                        End Sub

                        Sub Madin(args As IEnumerable(Of Program))
                            For Each index As B In args
                            Next
                        End Sub
                    End Class
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/734377")>
        Public Async Function TestSimplify1() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    Imports System
                    Imports I = System.Int32
                    Module Program
                        Public Dim {|SimplifyParent:x As Integer = 5|}
                        Function Goo() As Integer
                        End Function
                        Sub Main(args As String())
                            Dim {|SimplifyParent:A As Integer = 5|}
                            Dim {|SimplifyParent:M() As String = New String(){}|}, {|SimplifyParent:N() As String|}
                            Dim {|SimplifyParent:B(,) As Integer = {{1,2},{2,3}}|}
                            Dim {|SimplifyParent:ret As Integer = Goo()|}
                            Const {|SimplifyParent:con As Integer = 1|}
                            Dim {|SimplifyParent:in As I = 1|}
                        End Sub
                    End Module
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    Imports System
                    Imports I = System.Int32
                    Module Program
                        Public Dim x As Integer = 5
                        Function Goo() As Integer
                        End Function
                        Sub Main(args As String())
                            Dim A = 5
                            Dim M = New String(){}, N() As String
                            Dim B = {{1,2},{2,3}}
                            Dim ret = Goo()
                            Const con = 1
                            Dim in = 1
                        End Sub
                    End Module
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestSimplify2() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    Imports System
                    Imports System.Collections.Generic
                    Imports System.Diagnostics
                    Imports System.Linq
                    Imports I = System.Int32
                    Module Program
                        Sub Main(args As String())
                            Using {|SimplifyParent:proc As Process|} = New Process
                            End Using
                            For {|SimplifyParent:index As Integer|} = 1 To 5
                            Next
                            For {|SimplifyParent:index As I|} = 1 to 5
                            Next
                            For Each {|SimplifyParent:index As Integer|} In New Integer() {1, 2, 3}
                            Next
                        End Sub
                    End Module
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    Imports System
                    Imports System.Collections.Generic
                    Imports System.Diagnostics
                    Imports System.Linq
                    Imports I = System.Int32
                    Module Program
                        Sub Main(args As String())
                            Using proc = New Process
                            End Using
                            For index = 1 To 5
                            Next
                            For index = 1 to 5
                            Next
                            For Each index In New Integer() {1, 2, 3}
                            Next
                        End Sub
                    End Module
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestSimplify_For_1() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    Imports System
                    Imports System.Collections.Generic
                    Imports System.Linq

                    Class B
                    End Class
                    Class Program
                        Inherits B
                        Sub Main(args As String())
                        End Sub

                        Sub Madin(args As IEnumerable(Of Program))
                            For Each {|SimplifyParent:index As Program|} In args
                            Next
                        End Sub
                    End Class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    Imports System
                    Imports System.Collections.Generic
                    Imports System.Linq

                    Class B
                    End Class
                    Class Program
                        Inherits B
                        Sub Main(args As String())
                        End Sub

                        Sub Madin(args As IEnumerable(Of Program))
                            For Each index In args
                            Next
                        End Sub
                    End Class
                </text>

            Await TestAsync(input, expected)
        End Function

#Region "Type Argument Expand/Reduce for Generic Method Calls - 639136"

        <Fact>
        Public Async Function TestSimplify_For_GenericMethods() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document><![CDATA[
interface I
{
    void Goo<T>(T x);
}
class C : I
{
    public void Goo<T>(T x) { }
}
class D : C
{
    public void Goo(int x)
    {

    }
    public void Sub()
    {
        {|SimplifyParent:base.Goo<int>(1)|};
    }
}
]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text><![CDATA[
interface I
{
    void Goo<T>(T x);
}
class C : I
{
    public void Goo<T>(T x) { }
}
class D : C
{
    public void Goo(int x)
    {

    }
    public void Sub()
    {
        base.Goo(1);
    }
}]]>
              </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestSimplify_For_GenericMethods_VB() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document><![CDATA[
Class C
    Public Sub Goo(Of T)(ByRef x As T)

    End Sub
End Class

Class D
    Inherits C

    Public Sub Goo(ByRef x As Integer)

    End Sub
    Public Sub Test()
        Dim x As String
        {|SimplifyParent:MyBase.Goo(Of String)(x)|}
    End Sub
End Class
]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text><![CDATA[
Class C
    Public Sub Goo(Of T)(ByRef x As T)

    End Sub
End Class

Class D
    Inherits C

    Public Sub Goo(ByRef x As Integer)

    End Sub
    Public Sub Test()
        Dim x As String
        MyBase.Goo(x)
    End Sub
End Class
]]>
              </text>

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/734377")>
        Public Async Function TestVisualBasic_ExplicitTypeDecl_FieldDecl() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Namespace X
    Module Program
        Public Dim {|SimplifyParent:t as Integer = {|SimplifyParent:X.A|}.getInt()|}
        Sub Main(args As String())
        End Sub
    End Module

    Class A
        Public Shared Function getInt() As Integer
            Return 0
        End Function
    End Class
End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Namespace X
    Module Program
        Public Dim t as Integer = A.getInt()
        Sub Main(args As String())
        End Sub
    End Module

    Class A
        Public Shared Function getInt() As Integer
            Return 0
        End Function
    End Class
End Namespace
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860111")>
        Public Async Function TestVisualBasic_ExplicitTypeDecl_MustGetNewSMForAnyReducer() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Namespace Y
    Namespace X
        Module Program
            Public Dim {|SimplifyParent:t as Integer = {|SimplifyParentParent:Y.X.A|}.getInt()|}
            Sub Main(args As String())
            End Function
        End Module

        Class A
            Public Shared Function getInt() As Integer
                Return 0
            End Function
        End Class
    End Namespace
End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Namespace Y
    Namespace X
        Module Program
            Public Dim t as Integer = A.getInt()
            Sub Main(args As String())
            End Function
        End Module

        Class A
            Public Shared Function getInt() As Integer
                Return 0
            End Function
        End Class
    End Namespace
End Namespace
                </text>

            Await TestAsync(input, expected)
        End Function
#End Region

    End Class
End Namespace

