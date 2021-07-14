' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class WithBlockErrorTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub WithTestNotDeclared()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module WithTestNotDeclared
    Sub Main()
        With o1
        End With
        With o2
        End With
        Dim o2 As Object = Nothing
    End Sub
End Module
 </file>
</compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_NameNotDeclared1, "o1").WithArguments("o1"),
            Diagnostic(ERRID.ERR_UseOfLocalBeforeDeclaration1, "o2").WithArguments("o2"))
        End Sub

        <Fact()>
        Public Sub WithTestScoping()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module WithTestScoping
    Sub Main()
        Dim o1 As New Object()
        With o1
            Dim o1 = Nothing
            With o1
                Dim o2 = Nothing
            End With
            With New Object()
                Dim o1 As New Object()
                Dim o2 = Nothing
            End With
        End With
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_BlockLocalShadowing1, "o1").WithArguments("o1"),
            Diagnostic(ERRID.ERR_BlockLocalShadowing1, "o1").WithArguments("o1"))
        End Sub

        <Fact()>
        Public Sub WithTestNotAMember()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Class Class2
    Public Sub Method1()
    End Sub
End Class
Module WithTestNotAMember
    Sub Main()
        Dim c2 As New Class2()
        With c2
            .Method1("a")
            .Property1 = "a"
            .Method2()
            .Method3
        End With
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_TooManyArgs1, """a""").WithArguments("Public Sub Method1()"),
            Diagnostic(ERRID.ERR_NameNotMember2, ".Property1").WithArguments("Property1", "Class2"),
            Diagnostic(ERRID.ERR_NameNotMember2, ".Method2").WithArguments("Method2", "Class2"),
            Diagnostic(ERRID.ERR_NameNotMember2, ".Method3").WithArguments("Method3", "Class2"))
        End Sub

        <Fact()>
        Public Sub WithTestCannotLiftMeReference()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Public A As Integer
    Public B As Date
    Public C As String

    Public Sub New(i As Integer)
        With Me
            .A = 1
            Dim a As Action = Sub()
                                  .B = #1/2/2003#
                                  .C = "!"
                              End Sub
            a()
        End With
    End Sub
End Structure
    </file>
</compilation>)

            compilation.AssertTheseDiagnostics(<![CDATA[
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                                  .B = #1/2/2003#
                                  ~~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                                  .C = "!"
                                  ~~
]]>)
        End Sub

        <Fact()>
        Public Sub WithTestCannotLiftMeReference2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Public S As SSS
    Structure SSS
        Public A As Integer
        Public B As Date
    End Structure

    Public Sub New(i As Integer)
        Dim b As Action = Sub()
                              With Me.S
                                  .A = 1
                                  Dim a As Action = Sub()
                                                        .B = #1/2/2003#
                                                    End Sub
                              End With
                          End Sub
    End Sub
End Structure
    </file>
</compilation>)

            compilation.AssertTheseDiagnostics(<![CDATA[
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                              With Me.S
                                   ~~
]]>)
        End Sub

        <Fact()>
        Public Sub WithTestCannotLiftMeReference3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Public S As SSS
    Structure SSS
        Public A As Integer
        Public B As Date
    End Structure

    Public Sub New(i As Integer)
        With S
            Dim c As Action = Sub()
                                  .A = 1
                                  Dim a As Action = Sub()
                                                        .B = #1/2/2003#
                                                    End Sub
                              End Sub
        End With
    End Sub
End Structure
    </file>
</compilation>)

            compilation.AssertTheseDiagnostics(<![CDATA[
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                                  .A = 1
                                  ~~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                                                        .B = #1/2/2003#
                                                        ~~
]]>)

        End Sub

        <Fact()>
        Public Sub WithTestCannotLiftMeReference4()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Public S As SSS
    Structure SSS
        Public A As Integer
        Public B As Date
    End Structure

    Public Sub New(i As Integer)
        Dim b As Action = Sub()
                              With S
                                  .A = 1
                                  Dim a As Action = Sub()
                                                        .B = #1/2/2003#
                                                    End Sub
                              End With
                          End Sub
    End Sub
End Structure
    </file>
</compilation>).VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_CannotLiftStructureMeLambda, "S"))
        End Sub

        <Fact()>
        Public Sub WithTestCannotLiftMeReference_NestedLambda()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Public A As Integer
    Public B As Date
    Public C As String
    Public Sub New(i As Integer)
        Dim b As Action = Sub()
                              Me.A = 1
                              With Me
                                  .A = 1
                                  Dim a As Action = Sub()
                                                        .B = #1/2/2003#
                                                        .C = "!"
                                                    End Sub
                                  a()
                              End With
                          End Sub
    End Sub
End Structure
    </file>
</compilation>)

            compilation.AssertTheseDiagnostics(<![CDATA[
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                              Me.A = 1
                              ~~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                              With Me
                                   ~~
]]>)
        End Sub

        <Fact()>
        Public Sub WithTestCannotLiftMeReference_NestedWith()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Public A As Integer
    Public B As Date
    Public C As String
    Public Sub New(i As Integer)
        Dim b As Action = Sub()
                              Me.A = 1
                              With Me
                                  .A = 1
                                  Dim a As Action = Sub()
                                                        .B = #1/2/2003#
                                                        .C = "!"
                                                    End Sub
                                  a()
                              End With
                          End Sub
    End Sub
End Structure
    </file>
</compilation>)

            compilation.AssertTheseDiagnostics(<![CDATA[
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                              Me.A = 1
                              ~~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                              With Me
                                   ~~
]]>)
        End Sub

        <Fact()>
        Public Sub WithTest_ValueTypeLValueInParentheses()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Structure SSS
        Public A As Integer
        Public B As Date
    End Structure

    Public Sub New(i As Integer)
        Dim a As New SSS
        With (a)
            .A = 1
            .B = #1/1/2001#
        End With
        Console.Write(a.ToString())
    End Sub
End Structure
    </file>
</compilation>).VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_LValueRequired, ".A"),
                    Diagnostic(ERRID.ERR_LValueRequired, ".B"))
        End Sub

        <Fact()>
        Public Sub WithTest_UnknownMember()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On
Class S
    Sub S()
        With New Object()
            Dim x = .Goo()
        End With
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_StrictDisallowsLateBinding, ".Goo"))
        End Sub

        <Fact()>
        Public Sub BC36549ERR_CannotLiftAnonymousType1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="BC36549ERR_CannotLiftAnonymousType1_2">
    <file name="a.vb">
Class Outer
    Sub New(ByVal value As Integer)
        Dim a = New With {.a = 1, .b = Sub()
                                           With .a
                                               .ToString()
                                           End With
                                       End Sub}
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36549: Anonymous type property 'a' cannot be used in the definition of a lambda expression within the same initialization list.
                                           With .a
                                                ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36549ERR_CannotLiftAnonymousType1_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="BC36549ERR_CannotLiftAnonymousType1_3">
    <file name="a.vb">
Structure Clazz
    Sub S()
        With New With {.a = 1}
            With .a
                Dim cnt = .ToString()
            End With
        End With
    End Sub
    Sub S2()
        Dim at = New With {
            .a = 1,
            .b = Sub()
                     With .a
                         Dim cnt = .ToString()
                     End With
                 End Sub
            }
    End Sub
End Structure
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36549: Anonymous type property 'a' cannot be used in the definition of a lambda expression within the same initialization list.
                     With .a
                          ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30044ERR_UseOfKeywordFromStructure1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="UseOfKeywordFromStructure1_2">
    <file name="a.vb">
Structure Clazz

    Public F_I As Integer
    Public F_S As String

    Sub New(ByVal value As Integer)
        With Me
            .F_I = 1
            .F_S = ""
        End With
        With MyClass.F_I
        End With
        With MyBase.F_I
        End With
    End Sub
End Structure
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30044: 'MyBase' is not valid within a structure.
        With MyBase.F_I
             ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30068ERR_LValueRequired_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="LValueRequired_3">
    <file name="a.vb">
Class C2
    Structure S
        Public S() As Integer
    End Structure

    Public Shared Sub M()
        With New S
            .S = New Integer(2) {}
        End With
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
            .S = New Integer(2) {}
            ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30367ERR_NoDefaultNotExtend1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class S
    Sub S()
        With New Object()
            Dim a = New With {.a = 1, .b = !a}
        End With
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30367: Class '&lt;anonymous type&gt;' cannot be indexed because it has no default property.
            Dim a = New With {.a = 1, .b = !a}
                                           ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30491ERR_VoidValue_InWithStatement()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC30491ERR_VoidValue_InWithStatement">
        <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        With AddressOf Main
        End With
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
        With AddressOf Main
             ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30491ERR_VoidValue_InWithStatement2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC30491ERR_VoidValue_InWithStatement2">
        <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        With Main(args)
        End With
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
        With Main(args)
             ~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30524ERR_NoGetProperty1_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System
Module Program222
    Sub Main2(args As String())
        With IntProp
            Dim a = .GetType()
            Console.WriteLine(a.ToString())
        End With
    End Sub
    Public WriteOnly Property IntProp As Integer
        Set(value As Integer)
        End Set
    End Property
End Module
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30524: Property 'IntProp' is 'WriteOnly'.
        With IntProp
             ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36535ERR_CannotLiftStructureMeQuery()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="CannotLiftStructureMeQuery">
        <file name="a.vb">
Option Infer On
Imports System.Linq
Structure Clazz
    Structure SS
        Public FLD As String
    End Structure

    Public FLD As SS

    Sub TEST()
        With MyClass.FLD
            Dim q = From x In "" Select .FLD
        End With
    End Sub
End Structure
    </file>
    </compilation>, {SystemCoreRef})
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36535: Instance members and 'Me' cannot be used within query expressions in structures.
            Dim q = From x In "" Select .FLD
                                        ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub MultipleNestedWithWithNestedStructMe()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="MultipleNestedWithWithNestedStructMe">
        <file name="a.vb">
Imports System

Structure Base
    Public Structure S
        Public F As SS
    End Structure
    Public Structure SS
        Public F As SSS
    End Structure
    Public Structure SSS
        Public F As SSSS
    End Structure
    Public Structure SSSS
        Public F As String
    End Structure

    Public FLD As S

    Sub TEST()
        With Me.FLD
            With .F
                With .F
                    With .F
                        Dim _sub As Action = Sub()
                                                 .F = ""
                                             End Sub
                    End With
                End With
            End With
        End With
    End Sub
End Structure
    </file>
    </compilation>, {})
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                                                 .F = ""
                                                 ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub MultipleNestedWithWithNestedStructMe2()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="MultipleNestedWithWithNestedStructMe2">
        <file name="a.vb">
Imports System

Structure Base
    Public Structure S
        Public F As SS
    End Structure
    Public Structure SS
        Public F As SSS
    End Structure
    Public Structure SSS
        Public F As SSSS
    End Structure
    Public Structure SSSS
        Public F As String
    End Structure

    Public FLD As S

    Sub TEST()
        With Me.FLD
            With .F
                With .F
                    Dim _sub As Action = Sub()
                                             With .F
                                                 .F = ""
                                             End With
                                         End Sub
                End With
            End With
        End With
    End Sub
End Structure
    </file>
    </compilation>, {})
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                                             With .F
                                                  ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub MultipleNestedWithRValueStruct()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="MultipleNestedWithRValueStruct">
        <file name="a.vb">
Imports System

Structure Base
    Public Structure S
        Public F As SS
    End Structure
    Public Structure SS
        Public F As SSS
    End Structure
    Public Structure SSS
        Public F As SSSS
    End Structure
    Public Structure SSSS
        Public F As String
    End Structure

    Public FLD As S

    Sub TEST()
        Dim x As New S
        With (x)
            With .F
                With .F
                    With .F
                        .F = ""
                    End With
                End With
            End With
        End With
        With x
            With .F
                With .F
                    With .F
                        .F = ""
                    End With
                End With
            End With
        End With
        With (x).F
            With .F
                With .F
                    .F = ""
                End With
            End With
        End With
        With x.F
            With .F
                With .F
                    .F = ""
                End With
            End With
        End With
        With (x).F.F.F
            .F = ""
        End With
        With x.F.F.F
            .F = ""
        End With
    End Sub
End Structure
        </file>
    </compilation>, {})
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
                        .F = ""
                        ~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
                    .F = ""
                    ~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
            .F = ""
            ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWith_ValueTypeLValue_1()
            CompileAndVerify(
<compilation name="TestSimpleWith_ValueTypeLValue_1">
    <file name="a.vb">
Structure Struct
    Public A As String
    Public B As String
End Structure
Class Clazz
    Structure SS
        Public FLD As Struct
    End Structure

    Public FLD As SS

    Sub TEST_OK()
        Dim a As Struct
        With a
            .A = ""
            .B = ""
        End With
        a.ToString()
    End Sub

    Sub TEST_WARNING()
        Dim b As Struct
        With b
            .A = ""
        End With
        b.ToString()
    End Sub
End Class
    </file>
</compilation>).
            VerifyDiagnostics(
                Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "b").WithArguments("b"))
        End Sub

        <Fact()>
        Public Sub TestSimpleWith_ValueTypeLValue_2()
            CompileAndVerify(
<compilation name="TestSimpleWith_ValueTypeLValue_2">
    <file name="a.vb">
Structure Struct
    Public A As String
    Public B As String
End Structure
Class Clazz
    Structure SS
        Public FLD As Struct
    End Structure

    Public FLD As SS

    Sub TEST_OK()
        Dim a As Struct
        With a
            .A = ""
            .B = ""
            Dim x = .B
        End With
    End Sub

    Sub TEST_WARNING()
        Dim b As Struct
        With b
            .A = ""
            Dim x = .B
        End With
    End Sub
End Class
    </file>
</compilation>).
            VerifyDiagnostics(
                Diagnostic(ERRID.WRN_DefAsgUseNullRef, ".B").WithArguments("B"))
        End Sub

        <Fact()>
        Public Sub TestSimpleWith_ValueTypeLValue_3()
            CompileAndVerify(
<compilation name="TestSimpleWith_ValueTypeLValue_3">
    <file name="a.vb">
Structure Struct
    Public A As String
    Public B As String
End Structure
Class Clazz
    Structure SS
        Public FLD As Struct
    End Structure

    Public FLD As SS

    Sub TEST_OK()
        Dim a As Struct
        With a
            .A = ""
            .B = ""
            S_ByRef(.B)
        End With
    End Sub

    Sub TEST_WARNING()
        Dim b As Struct
        With b
            .A = ""
            S_ByRef(.B)
        End With
    End Sub

    Sub S_ByRef(ByRef s As String)
    End Sub
End Class
    </file>
</compilation>).
            VerifyDiagnostics(
                Diagnostic(ERRID.WRN_DefAsgUseNullRefByRef, ".B").WithArguments("B"))
        End Sub

        <Fact()>
        Public Sub TestSimpleWith_NotInitializedVariable()
            CompileAndVerify(
<compilation name="TestSimpleWith_NotInitializedVariable">
    <file name="a.vb">
Structure SS
    Public A As Integer
    Public B As String
End Structure

Class CC
    Public A As Integer
    Public B As String
End Class

Structure Clazz
    Sub Main(args As String())

        Dim s As SS
        With s
            Dim x = .A
        End With

        Dim c As CC
        With c
            Dim x = .A
        End With
    End Sub
End Structure 
    </file>
</compilation>).
            VerifyDiagnostics(
                Diagnostic(ERRID.WRN_DefAsgUseNullRef, "c").WithArguments("c"))
        End Sub

        <Fact()>
        Public Sub TestWithInsideUsing()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="TestWithInsideUsing">
    <file name="a.vb">
Imports System
Structure STRUCT
    Implements IDisposable
    Public C As String
    Public D As Integer
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Class Clazz
    Public Shared Sub Main(args() As String)
        Using s = New STRUCT()
            With s
                .C = "Success"
            End With
        End Using
    End Sub
End Class

    </file>
</compilation>, {})
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42351: Local variable 's' is read-only and its type is a structure. Invoking its members or passing it ByRef does not change its content and might lead to unexpected results. Consider declaring this variable outside of the 'Using' block.
        Using s = New STRUCT()
              ~~~~~~~~~~~~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
                .C = "Success"
                ~~
</errors>)
        End Sub

        <Fact()>
        Public Sub TestWithAndQueryVariable()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="TestWithAndQueryVariable">
    <file name="a.vb">
Imports System
Imports System.Linq

Structure STRUCT
    Public C As String
    Public D As Integer
End Structure

Class Clazz
    Public Shared Sub Main(args() As String)
        Dim source(10) As STRUCT

        Dim result = From x In source Select DirectCast(Function()
                                                            With x
                                                                .D = 123
                                                            End With
                                                            Return x
                                                        End Function, Func(Of STRUCT))()
    End Sub
End Class

    </file>
</compilation>, {SystemCoreRef})
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
                                                                .D = 123
                                                                ~~
</errors>)
        End Sub

        <WorkItem(543921, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543921")>
        <Fact()>
        Public Sub WithNewT()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb">
Interface I
    Property P As Object
End Interface
Module M
    Sub M(Of T As {I, New})()
        With New T()
            .P = Nothing
        End With
    End Sub
End Module
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        <Fact(), WorkItem(544195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544195")>
        Public Sub WithMeMyClassMyBase()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class Class1
    Overridable Sub Method1()
        With Me
            .Method1()
        End With
        With MyClass
            .Method1()
        End With
        With MyBase
            .New()
        End With
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics(
           Diagnostic(ERRID.ERR_ExpectedDotAfterMyClass, "MyClass"),
           Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
           Diagnostic(ERRID.ERR_InvalidConstructorCall, ".New"))
        End Sub

        <Fact()>
        <WorkItem(49904, "https://github.com/dotnet/roslyn/issues/49904")>
        Public Sub ArrayAccessWithOmittedIndexAsWithTarget_01()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="c.vb">
Class Test

    Private MyArr(9, 3, 11) As MyStruct

    Sub Main()
        Dim n, p, r As Integer
        For n = 0 To 9
            For p = 0 To 3
                For r = 0 To 11
                    With MyArr(n, )
                        .A = n
                        .B = p
                        .C = r
                    End With
                Next
            Next
        Next
    End Sub

End Class

Structure MyStruct
    Dim A As Integer
    Dim B As Integer
    Dim C As Integer
End Structure
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()

            For Each node In tree.GetRoot().DescendantNodes()
                Dim model = compilation.GetSemanticModel(tree)
                model.GetMemberGroup(node)
                model = compilation.GetSemanticModel(tree)
                model.GetSymbolInfo(node)
                model = compilation.GetSemanticModel(tree)
                model.GetTypeInfo(node)
            Next

            compilation.AssertTheseEmitDiagnostics(
<expected>
BC30105: Number of indices is less than the number of dimensions of the indexed array.
                    With MyArr(n, )
                              ~~~~~
BC30491: Expression does not produce a value.
                    With MyArr(n, )
                                  ~
</expected>)
        End Sub

        <Fact()>
        <WorkItem(49904, "https://github.com/dotnet/roslyn/issues/49904")>
        Public Sub ArrayAccessWithOmittedIndexAsWithTarget_02()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="c.vb">
Class Test

    Private MyArr(9, 3, 11) As MyStruct

    Sub Main()
        Dim n, p, r As Integer
        For n = 0 To 9
            For p = 0 To 3
                For r = 0 To 11
                    With MyArr(n, , )
                        .A = n
                        .B = p
                        .C = r
                    End With
                Next
            Next
        Next
    End Sub

End Class

Structure MyStruct
    Dim A As Integer
    Dim B As Integer
    Dim C As Integer
End Structure
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()

            For Each node In tree.GetRoot().DescendantNodes()
                Dim model = compilation.GetSemanticModel(tree)
                model.GetMemberGroup(node)
                model = compilation.GetSemanticModel(tree)
                model.GetSymbolInfo(node)
                model = compilation.GetSemanticModel(tree)
                model.GetTypeInfo(node)
            Next

            compilation.AssertTheseEmitDiagnostics(
<expected>
BC30491: Expression does not produce a value.
                    With MyArr(n, , )
                                  ~
BC30491: Expression does not produce a value.
                    With MyArr(n, , )
                                    ~
</expected>)
        End Sub

    End Class

End Namespace
