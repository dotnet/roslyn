﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class OperationAnalyzerTests
        Inherits BasicTestBase

        <Fact>
        Public Sub EmptyArrayVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Sub M1()
        Dim arr1 As Integer() = New Integer(-1) { }               ' yes
        Dim arr2 As Byte() = { }                                  ' yes
        Dim arr3 As C() = New C(-1) { }                           ' yes
        Dim arr4 As String() = New String() { Nothing }           ' no
        Dim arr5 As Double() = New Double(1) { }                  ' no
        Dim arr6 As Integer() = { -1 }                            ' no
        Dim arr7 as Integer()() = New Integer(-1)() { }           ' yes
        Dim arr8 as Integer()()()() = New Integer(  -1)()()() { } ' yes
        Dim arr9 as Integer(,) = New Integer(-1,-1) { }           ' no
        Dim arr10 as Integer()(,) = New Integer(-1)(,) { }        ' yes
        Dim arr11 as Integer()(,) = New Integer(1)(,) { }         ' no
        Dim arr12 as Integer(,)() = New Integer(-1,-1)() { }      ' no
        Dim arr13 as Integer() = New Integer(0) { }               ' no
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New EmptyArrayOperationAnalyzer}, Nothing, Nothing, False,
               Diagnostic(EmptyArrayOperationAnalyzer.UseArrayEmptyDescriptor.Id, "New Integer(-1) { }").WithLocation(3, 33),
               Diagnostic(EmptyArrayOperationAnalyzer.UseArrayEmptyDescriptor.Id, "{ }").WithLocation(4, 30),
               Diagnostic(EmptyArrayOperationAnalyzer.UseArrayEmptyDescriptor.Id, "New C(-1) { }").WithLocation(5, 27),
               Diagnostic(EmptyArrayOperationAnalyzer.UseArrayEmptyDescriptor.Id, "New Integer(-1)() { }").WithLocation(9, 35),
               Diagnostic(EmptyArrayOperationAnalyzer.UseArrayEmptyDescriptor.Id, "New Integer(  -1)()()() { }").WithLocation(10, 39),
               Diagnostic(EmptyArrayOperationAnalyzer.UseArrayEmptyDescriptor.Id, "New Integer(-1)(,) { }").WithLocation(12, 37))
        End Sub

        <Fact>
        Public Sub BoxingVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Function M1(p1 As Object, p2 As Object, p3 As Object) As Object
         Dim v1 As New S
         Dim v2 As S = v1
         Dim v3 As S = v1.M1(v2)
         Dim v4 As Object = M1(3, Me, v1)
         Dim v5 As Object = v3
         If p1 Is Nothing
             return 3
         End If
         If p2 Is Nothing
             return v3
         End If
         If p3 Is Nothing
             Return v4
         End If
         Return v5
    End Function
End Class

Structure S
    Public X As Integer
    Public Y As Integer
    Public Z As Object

    Public Function M1(p1 As S) As S
        p1.GetType()
        Z = Me
        Return p1
    End Function
End Structure
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New BoxingOperationAnalyzer}, Nothing, Nothing, False,
               Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "3").WithLocation(6, 32),
               Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "v1").WithLocation(6, 39),
               Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "v3").WithLocation(7, 29),
               Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "3").WithLocation(9, 21),
               Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "v3").WithLocation(12, 21),
               Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "p1").WithLocation(27, 9),
               Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "Me").WithLocation(28, 13))
        End Sub

        <Fact>
        Public Sub BigForVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub M1()
        Dim x as Integer
        For x = 1 To 200000 : Next
        For x = 1 To 2000000 : Next
        For x = 1500000 To 0 Step -2 : Next
        For x = 3000000 To 0 Step -2 : Next
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New BigForTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(BigForTestAnalyzer.BigForDescriptor.Id, "For x = 1 To 2000000 : Next").WithLocation(5, 9),
                                           Diagnostic(BigForTestAnalyzer.BigForDescriptor.Id, "For x = 3000000 To 0 Step -2 : Next").WithLocation(7, 9))
        End Sub

        <Fact>
        Public Sub SparseSwitchVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub M1(x As Integer)
        Select Case x
            Case 1, 2
                Exit Select
            Case = 10
                Exit Select
            Case Else
                Exit Select
        End Select

        Select Case x
            Case 1
                Exit Select
            Case = 1000
                Exit Select
            Case Else
                Exit Select
        End Select

        Select Case x
            Case 10 To 500
                Exit Select
            Case = 1000
                Exit Select
            Case Else
                Exit Select
        End Select

        Select Case x
            Case 1, 980 To 985
                Exit Select
            Case Else
                Exit Select
        End Select

        Select Case x
            Case 1 to 3, 980 To 985
                Exit Select
        End Select

         Select Case x
            Case 1
                Exit Select
            Case > 100000
                Exit Select
        End Select
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New SparseSwitchTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(SparseSwitchTestAnalyzer.SparseSwitchDescriptor.Id, "x").WithLocation(12, 21),
                                           Diagnostic(SparseSwitchTestAnalyzer.SparseSwitchDescriptor.Id, "x").WithLocation(30, 21),
                                           Diagnostic(SparseSwitchTestAnalyzer.SparseSwitchDescriptor.Id, "x").WithLocation(37, 21))
        End Sub

        <Fact>
        Public Sub InvocationVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub M0(a As Integer, ParamArray b As Integer())
    End Sub

    Public Sub M1(a As Integer, b As Integer, c As Integer, x As Integer, y As Integer, z As Integer)
    End Sub

    Public Sub M2()
        M1(1, 2, 3, 4, 5, 6)
        M1(a:=1, b:=2, c:=3, x:=4, y:=5, z:=6)
        M1(a:=1, c:=2, b:=3, x:=4, y:=5, z:=6)
        M1(z:=1, x:=2, y:=3, c:=4, a:=5, b:=6)
        M0(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)
        M0(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)
        M0(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13)
        M0(1)
        M0(1, 2, 4, 3)
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New InvocationTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(InvocationTestAnalyzer.OutOfNumericalOrderArgumentsDescriptor.Id, "2").WithLocation(11, 21),
                                           Diagnostic(InvocationTestAnalyzer.OutOfNumericalOrderArgumentsDescriptor.Id, "1").WithLocation(12, 15),
                                           Diagnostic(InvocationTestAnalyzer.OutOfNumericalOrderArgumentsDescriptor.Id, "2").WithLocation(12, 21),
                                           Diagnostic(InvocationTestAnalyzer.OutOfNumericalOrderArgumentsDescriptor.Id, "4").WithLocation(12, 33),
                                           Diagnostic(InvocationTestAnalyzer.BigParamarrayArgumentsDescriptor.Id, "M0(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)").WithLocation(14, 9),
                                           Diagnostic(InvocationTestAnalyzer.BigParamarrayArgumentsDescriptor.Id, "M0(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13)").WithLocation(15, 9),
                                           Diagnostic(InvocationTestAnalyzer.OutOfNumericalOrderArgumentsDescriptor.Id, "3").WithLocation(17, 21))
        End Sub
    End Class
End Namespace
