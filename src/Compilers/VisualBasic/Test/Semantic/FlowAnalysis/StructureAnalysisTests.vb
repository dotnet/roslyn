' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class StructureAnalysisTests
        Inherits FlowTestBase

        <Fact>
        Public Sub UnassignedFieldsInParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="NestedStructs_NotAllFieldsReported">
          <file name="a.b">
Public Structure S
    Public Property P As Object
End Structure
Public Structure SS
    Public s As S
    Public s2 As S
    Public s3 As Object
    Public Sub F(p As SS)
        p.s.P = ""
    End Sub
End Structure
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub NestedStructs_NotAllFieldsReported()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="NestedStructs_NotAllFieldsReported">
          <file name="a.b">
Public Structure S
    Public Property P As Object
End Structure
Public Structure SS
    Public s As S
    Public s2 As S
    Public s3 As Object
    Public Function F() As SS
        Dim ret As SS
        ret.s.P = Nothing
        ret.s2.P = Nothing
        Return ret
    End Function
End Structure
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42109: Variable 's' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        ret.s.P = Nothing
        ~~~~~
BC42109: Variable 's2' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        ret.s2.P = Nothing
        ~~~~~~
BC42109: Variable 'ret' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Return ret
               ~~~
</errors>)
        End Sub

        <Fact>
        Public Sub NestedStructs_AllFieldsReported()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="NestedStructs_AllFieldsReported">
          <file name="a.b">
Public Structure S
    Public Property P As Object
End Structure
Public Structure SS
    Public s As S
    Public s2 As S
    Public s3 As Object
    Public Function F() As SS
        Dim ret As SS
        ret.s.P = Nothing
        ret.s2.P = Nothing
        Dim o As Object = ret.s3
        Return ret
    End Function
End Structure
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42109: Variable 's' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        ret.s.P = Nothing
        ~~~~~
BC42109: Variable 's2' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        ret.s2.P = Nothing
        ~~~~~~
BC42104: Variable 's3' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim o As Object = ret.s3
                          ~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub BigStruct()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="BigStruct">
          <file name="a.b">
Public Structure S(Of T)
    Public a As T
    Public b As T
    Public c As T
    Public d As T
    Public e As T
    Public f As T
    Public g As T
    Public h As T

    Shared Sub M()
        Dim x As New S(Of S(Of S(Of S(Of S(Of S(Of S(Of S(Of Integer))))))))
        x.a.a.a.a.a.a.a.a = 12
        x.a.a.a.a.a.a.a.b = x.a.a.a.a.a.a.a.a
    End Sub
End Structure
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub SimpleStructsWithGenerics()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="SimpleStructsWithGenerics">
          <file name="a.b">
Public Structure XXX
    Public x As S(Of Object)
    Public y As S(Of String)
End Structure

Public Structure S(Of T)
    Public a As String
    Public Property b As T
End Structure

Public Class Test
    Public Shared Sub Main(args As String())
        Dim s As XXX
        's.x.a = ""
        s.x.b = s.x.a
        Dim t As Object = s
    End Sub
    Public Shared Sub S1(ByRef arg As XXX)
        arg.x.a = ""
        arg.x.b = arg.x.a
    End Sub
End Class
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42109: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        s.x.b = s.x.a
        ~~~
BC42104: Variable 'a' is used before it has been assigned a value. A null reference exception could result at runtime.
        s.x.b = s.x.a
                ~~~~~
BC42109: Variable 's' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim t As Object = s
                          ~
</errors>)
        End Sub

        <Fact>
        Public Sub CallingMethodsOnUninitializedStructs()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="CallingMethodsOnUninitializedStructs">
          <file name="a.b">
Public Structure XXX
    Public x As S(Of Object)
    Public y As S(Of String)
End Structure

Public Structure S(Of T)
    Public x As String
    Public Property y As T
End Structure

Public Class Test
    Public Shared Sub Main(args As String())
        Dim s As XXX
        s.x.y.ToString()
        Dim t As Object = s
    End Sub
    Public Shared Sub S1(ByRef arg As XXX)
        arg.x.x = ""
        arg.x.y = arg.x.x
    End Sub
End Class
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42109: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        s.x.y.ToString()
        ~~~
BC42109: Variable 's' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim t As Object = s
                          ~
</errors>)
        End Sub

        <Fact>
        Public Sub CallingMethodsOnUninitializedStructs2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="CallingMethodsOnUninitializedStructs2">
          <file name="a.b">
Public Structure XXX
    Public x As S(Of Object)
    Public y As S(Of String)
End Structure

Public Structure S(Of T)
    Public x As String
    Public Property y As T
End Structure

Public Class Test
    Public Shared Sub Main(args As String())
        Dim s As XXX
        s.x = New S(Of Object)()
        s.x.y.ToString()
        Dim t As Object = s
    End Sub
    Public Shared Sub S1(ByRef arg As XXX)
        arg.x.x = ""
        arg.x.y = arg.x.x
    End Sub
End Class
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42109: Variable 's' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim t As Object = s
                          ~
</errors>)
        End Sub

        <Fact>
        Public Sub ReferencingCycledStructures()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="Compilation">
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim s1 As S1 = New S1()
        Dim s2 As S2 = New S2()
        s2.fld = New S3()
        s2.fld.fld.fld.fld = New S2()
    End Sub
End Class
    </file>
</compilation>, {TestReferences.SymbolsTests.CycledStructs})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)

        End Sub

        <WorkItem(530076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530076")>
        <Fact>
        Public Sub Bug530076a()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="Compilation">
    <file name="a.vb">
Imports System

Friend class RetVarWarn005mod
    Structure structTemp
        Public strTemp1 As String
        Public strTemp2 As String
    End Structure
    Function Sce1() As structTemp
        Sce1.strTemp1 = "Scenario1Temp1"
        Sce1.strTemp2 = "Scenario1Temp2"
    End Function
End class
    </file>
</compilation>, {})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)

        End Sub

        <WorkItem(530076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530076")>
        <Fact>
        Public Sub Bug530076b()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="Compilation">
    <file name="a.vb">
Imports System

Friend class RetVarWarn005mod
    Structure structTemp
        Public strTemp1 As String
        Public strTemp2 As String
    End Structure
    Function Sce1() As structTemp
        dim Sce0 as structTemp
        Sce0.strTemp1 = "Scenario1Temp1"
        Sce0.strTemp2 = "Scenario1Temp2"
        Return Sce0 
    End Function
End class
    </file>
</compilation>, {})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
        End Sub

        <WorkItem(807595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/807595")>
        <Fact>
        Public Sub Bug807595()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="Compilation">
    <file name="a.vb">
Imports System

Friend class RetVarWarn005mod
    Structure structTemp
        Public strTemp1 As String
        Public strTemp2 As String
    End Structure
    Function Sce1() As structTemp
        Dim Sce2 As structTemp
        Try
            Sce1.strTemp1 = "Scenario1Temp1InTry"
            Sce2.strTemp1 = "Scenario1Temp1InTry"
        Finally
            Sce1.strTemp2 = "Scenario1Temp2InFinally"
            Sce2.strTemp2 = "Scenario1Temp2InFinally"
        End Try
        Dim sce3 = Sce2
    End Function
End class
    </file>
</compilation>, {})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)

        End Sub

#Region "Test ported from C# + variations and derived tests"

        <Fact>
        Public Sub AllPiecesAssigned()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="AllPiecesAssigned">
          <file name="a.b">
        Public Structure S
            Public x As Integer
            Public y As Integer
        End Structure

        Public Class Test
            Public Shared Sub Main()
                Dim s As S
                s.x = 1
                s.y = 2
                Dim t As S = s
            End Sub
        End Class
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <WorkItem(542579, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542579")>
        <Fact>
        Public Sub AllPiecesAssigned2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
      <compilation name="AllPiecesAssigned2">
          <file name="a.b">
            Structure S
                Dim x As String
            End Structure
            Module Program
                Sub Main(args As String())
                    Dim S1 As S
                    S1.x = ""
                    Dim S2 = S1
                End Sub
            End Module
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <WorkItem(542579, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542579")>
        <Fact>
        Public Sub AllPiecesAssigned3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
      <compilation name="AllPiecesAssigned3">
          <file name="a.b">
            Structure S
                Dim x As String
            End Structure
            Module Program
                Sub Main(args As String())
                    Dim S1 As S
                    S1.x = ""
                    Dim S2 = S1
                End Sub
            End Module
        </file>
      </compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim diagnostics = model.GetDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(diagnostics, <errors></errors>)
        End Sub

        <Fact>
        Public Sub AllPiecesAssigned_Int()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="AllPiecesAssigned_Int">
          <file name="a.b">
Public Structure S
    Public x As Integer
    Public y As Integer
End Structure

Public Class Test
    Public Shared Sub Main()
        Dim s As S
        s.x = 1
        s.y = 2
        Dim t As S = s
    End Sub
End Class
</file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub AllPiecesAssigned_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="AllPiecesAssigned_Obj">
          <file name="a.b">
Public Structure S
    Public x As Object
    Public y As Object
End Structure

Public Class Test
    Public Shared Sub Main(args As String())
        Dim s As S
        s.x = args(0)
        s.y = New Object()
        Dim t As S = s
    End Sub
End Class
</file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub AllPiecesAssigned_Prop_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="AllPiecesAssigned_Prop_Obj">
          <file name="a.b">
Public Structure S
    Public Property x As Object
    Public Property y As Object
End Structure

Public Class Test
    Public Shared Sub Main()
        Dim s As S
        s.x = 1
        s.y = 2
        Dim t As S = s
    End Sub
End Class
</file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42109: Variable 's' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        s.x = 1
        ~
</errors>)
        End Sub

        <Fact>
        Public Sub OnePieceMissing_Int()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="OnePieceMissing_Int">
          <file name="a.b">
Public Structure S
    Public x As Integer
    Public y As Integer
End Structure

Public Class Test
    Public Shared Sub Main(args As String())
        Dim s As S
        s.x = args.Length
        Dim t As S = s
    End Sub
End Class
</file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub OnePieceMissing_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="OnePieceMissing_Obj">
          <file name="a.b">
Public Structure S
    Public x As Object
    Public y As Object
End Structure

Public Class Test
    Public Shared Sub Main(args As String())
        Dim s As S
        s.y = New Object()
        Dim t As S = s
    End Sub
End Class
</file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42109: Variable 's' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim t As S = s
                     ~
</errors>)
        End Sub

        <Fact>
        Public Sub OnePieceMissing_Prop_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="OnePieceMissing_Prop_Obj">
          <file name="a.b">
Public Structure S
    Public Property x As Object
    Public Property y As Object
End Structure

Public Class Test
    Public Shared Sub Main(args As String())
        Dim s As S
        s.y = New Object()
        Dim t As S = s
    End Sub
End Class
</file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42109: Variable 's' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        s.y = New Object()
        ~
</errors>)
        End Sub

        <Fact>
        Public Sub OnePieceOnOnePath_Int()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="OnePieceOnOnePath_Int">
          <file name="a.b">
Public Structure S
    Public x As Integer
    Public y As Integer
End Structure

Public Class Test
    Public Shared Sub Main(s As S)
        Dim s2 As S
        If s.x = 3 Then
            s2 = s
        Else
            s2.x = s.x
        End If
        Dim i As Integer = s2.x
    End Sub
End Class
</file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub OnePieceOnOnePath_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="OnePieceOnOnePath_Obj">
          <file name="a.b">
Public Structure S
    Public x As Object
    Public y As Object
End Structure

Public Class Test
    Public Shared Sub Main(s As S)
        Dim s2 As S
        If s.x Is New Object() Then
            s2 = s
        Else
            s2.x = s.x
        End If
        Dim i As Object = s2.x
    End Sub
End Class
</file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub OnePieceOnOnePath_Prop_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="OnePieceOnOnePath_Prop_Obj">
          <file name="a.b">
Public Structure S
    Public Property x As Object
    Public Property y As Object
End Structure

Public Class Test
    Public Shared Sub Main(s As S)
        Dim s2 As S
        If s.x Is New Object() Then
            s2 = s
        Else
            s2.x = s.x
        End If
        Dim i As Object = s2.x
    End Sub
End Class
</file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42109: Variable 's2' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
            s2.x = s.x
            ~~
</errors>)
        End Sub

        <Fact>
        Public Sub FullInitializationInConstructor_Int()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="FullInitializationInConstructor_Int">
          <file name="a.b">
Public Structure S
    Public x As Integer
    Public y As Integer
    Public Sub New(p As Integer)
        Me.x = p
        Me.y = p
    End Sub
End Structure
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub FullInitializationInConstructor_Prop_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="FullInitializationInConstructor_Prop_Obj">
          <file name="a.b">
Public Structure S
    Public Property x As Object
    Public Property y As Object
    Public Sub New(p As Object)
        Me.x = p
        Me.y = p
    End Sub
End Structure
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub PartialInitializationInConstructor_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="PartialInitializationInConstructor_Obj">
          <file name="a.b">
Public Structure S
    Public x As Object
    Public y As Object
    Public Sub New(p As Object)
        Me.x = p
    End Sub
End Structure
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub PartialInitializationInConstructor_Prop_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="PartialInitializationInConstructor_Prop_Obj">
          <file name="a.b">
Public Structure S
    Public Property x As Object
    Public Property y As Object
    Public Sub New(p As Object)
        Me.x = p
    End Sub
End Structure
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub DefaultConstructor_Int()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="DefaultConstructor_Int">
          <file name="a.b">
Public Structure S
    Public x As Integer
    Public y As Integer
End Structure
Public Class Test
    Public Shared Sub Main()
        Dim s As S = New S()
        s.x = s.y
        s.y = s.x
    End Sub
End Class
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub DefaultConstructor_Prop_And_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="DefaultConstructor_Prop_And_Obj">
          <file name="a.b">
Public Structure S
    Public x As Object
    Public Property y As Object
End Structure
Public Class Test
    Public Shared Sub Main()
        Dim s As S = New S()
        s.x = s.y
        s.y = s.x
    End Sub
End Class
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub AutoPropInitialization_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="AutoPropInitialization_Obj">
          <file name="a.b">
Public Structure S
    Public Property x As Object
    Public Sub New(y As Object)
    End Sub
End Structure
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub EmptyStructAlwaysAssigned_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="EmptyStructAlwaysAssigned_Obj">
          <file name="a.b">
Public Structure S
End Structure
Public Structure SS
    Public s1 As S
    Public s2 As S
    Public Function F() As SS
        Dim ret As SS
        Return ret
    End Function
End Structure
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub Struct_WithStructProperty_Auto_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="Struct_WithStructProperty_Auto_Obj">
          <file name="a.b">
Public Structure S
    Public Property P As Object
End Structure
Public Structure SS
    Public s As S
    Public Function F() As SS
        Dim ret As SS
        ret.s.P = Nothing
        Return ret
    End Function
End Structure
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42109: Variable 's' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        ret.s.P = Nothing
        ~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub Struct_WithStructProperty_Obj()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation name="Struct_WithStructProperty_Obj">
          <file name="a.b">
Public Structure S
    Public Property P As Object
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
End Structure
Public Structure SS
    Public s As S
    Public Function F() As SS
        Dim ret As SS
        ret.s.P = 0
        Return ret
    End Function
End Structure
        </file>
      </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

#End Region

        <WorkItem(874526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/874526")>
        <Fact()>
        Public Sub GenericStructWithPropertyUsingStruct()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
      <compilation>
          <file name="a.b">
Structure S(Of T)
    Property P As S(Of T())?
End Structure
        </file>
      </compilation>)
            comp.AssertTheseDiagnostics(
<errors>
BC30294: Structure 'S' cannot contain an instance of itself: 
    'S(Of T)' contains 'S(Of T())?' (variable '_P').
    'S(Of T())?' contains 'S(Of T())' (variable 'value').
    Property P As S(Of T())?
             ~
</errors>)
        End Sub

    End Class

End Namespace
