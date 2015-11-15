﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics.SystemLanguage
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
            comp.VerifyAnalyzerDiagnostics({New EmptyArrayAnalyzer}, Nothing, Nothing, False,
               Diagnostic(EmptyArrayAnalyzer.UseArrayEmptyDescriptor.Id, "New Integer(-1) { }").WithLocation(3, 33),
               Diagnostic(EmptyArrayAnalyzer.UseArrayEmptyDescriptor.Id, "{ }").WithLocation(4, 30),
               Diagnostic(EmptyArrayAnalyzer.UseArrayEmptyDescriptor.Id, "New C(-1) { }").WithLocation(5, 27),
               Diagnostic(EmptyArrayAnalyzer.UseArrayEmptyDescriptor.Id, "New Integer(-1)() { }").WithLocation(9, 35),
               Diagnostic(EmptyArrayAnalyzer.UseArrayEmptyDescriptor.Id, "New Integer(  -1)()()() { }").WithLocation(10, 39),
               Diagnostic(EmptyArrayAnalyzer.UseArrayEmptyDescriptor.Id, "New Integer(-1)(,) { }").WithLocation(12, 37))
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

        <Fact>
        Public Sub FieldCouldBeReadOnlyVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public F1 As Integer
    Public Const F2 As Integer = 2
    Public ReadOnly F3 As Integer
    Public F4 As Integer
    Public F5 As Integer
    Public F6 As Integer = 6
    Public F7 As Integer
    Public F9 As S
    Public F10 As New C1

    Public Sub New()
        F1 = 1
        F4 = 4
        F5 = 5
    End Sub

    Public Sub M0()
        Dim x As Integer = F1
        x = F2
        x = F3
        x = F4
        x = F5
        x = F6
        x = F7

        F4 = 4
        F7 = 7
        M1(F1, F5)
        F9.A = 10
        F9.B = 20
        F10.A = F9.A
        F10.B = F9.B
    End Sub

    Public Sub M1(ByRef X As Integer, Y As Integer)
        x = 10
    End Sub

    Structure S
        Public A As Integer
        Public B As Integer
    End Structure

    Class C1
        Public A As Integer
        Public B As Integer
    End Class
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New FieldCouldBeReadOnlyAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(FieldCouldBeReadOnlyAnalyzer.FieldCouldBeReadOnlyDescriptor.Id, "F5").WithLocation(6, 12),
                                           Diagnostic(FieldCouldBeReadOnlyAnalyzer.FieldCouldBeReadOnlyDescriptor.Id, "F6").WithLocation(7, 12),
                                           Diagnostic(FieldCouldBeReadOnlyAnalyzer.FieldCouldBeReadOnlyDescriptor.Id, "F10").WithLocation(10, 12))
        End Sub

        <Fact>
        Public Sub StaticFieldCouldBeReadOnlyVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Shared F1 As Integer
    Public Shared ReadOnly F2 As Integer = 2
    Public Shared Readonly F3 As Integer
    Public Shared F4 As Integer
    Public Shared F5 As Integer
    Public Shared F6 As Integer = 6
    Public Shared F7 As Integer
    Public Shared F9 As S
    Public Shared F10 As New C1

    Shared Sub New()
        F1 = 1
        F4 = 4
        F5 = 5
    End Sub

    Public Shared Sub M0()
        Dim x As Integer = F1
        x = F2
        x = F3
        x = F4
        x = F5
        x = F6
        x = F7

        F4 = 4
        F7 = 7
        M1(F1, F5)
        F9.A = 10
        F9.B = 20
        F10.A = F9.A
        F10.B = F9.B
    End Sub

    Public Shared Sub M1(ByRef X As Integer, Y As Integer)
        x = 10
    End Sub

    Structure S
        Public A As Integer
        Public B As Integer
    End Structure

    Class C1
        Public A As Integer
        Public B As Integer
    End Class
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New FieldCouldBeReadOnlyAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(FieldCouldBeReadOnlyAnalyzer.FieldCouldBeReadOnlyDescriptor.Id, "F5").WithLocation(6, 19),
                                           Diagnostic(FieldCouldBeReadOnlyAnalyzer.FieldCouldBeReadOnlyDescriptor.Id, "F6").WithLocation(7, 19),
                                           Diagnostic(FieldCouldBeReadOnlyAnalyzer.FieldCouldBeReadOnlyDescriptor.Id, "F10").WithLocation(10, 19))
        End Sub

        <Fact>
        Public Sub LocalCouldBeConstVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub M0(p as Integer)
        Dim x As Integer = p
        Dim y As Integer = x
        Const z As Integer = 1
        Dim a As Integer = 2
        Dim b As Integer = 3
        Dim c As Integer = 4
        Dim d As Integer = 5
        Dim e As Integer = 6
        Dim s As String = "ZZZ"
        b = 3
        c -= 12
        d += e + b
        M1(y, z, a, s)
        Dim n As S
        n.A = 10
        n.B = 20
        Dim o As New C1
        o.A = 10
        o.B = 20
    End Sub

    Public Sub M1(ByRef x As Integer, y As Integer, ByRef z as Integer, s as String)
        x = 10
    End Sub
End Class

Structure S
    Public A As Integer
    Public B As Integer
End Structure

Class C1
    Public A As Integer
    Public B As Integer
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New LocalCouldBeConstAnalyzer}, Nothing, Nothing, False,
                                            Diagnostic(LocalCouldBeConstAnalyzer.LocalCouldBeConstDescriptor.Id, "e").WithLocation(10, 13),
                                            Diagnostic(LocalCouldBeConstAnalyzer.LocalCouldBeConstDescriptor.Id, "s").WithLocation(11, 13))
        End Sub

        <Fact>
        Public Sub SymbolCouldHaveMoreSpecificTypeVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub M0()
        Dim a As Object = New Middle()
        Dim b As Object = New Value(10)
        Dim c As Object = New Middle()
        c = New Base()
        Dim d As Base = New Derived()
        Dim e As Base = New Derived()
        e = New Middle()
        Dim f As Base = New Middle()
        f = New Base()
        Dim g As Object = New Derived()
        g = New Base()
        g = New Middle()
        Dim h As New Middle()
        h = New Derived()
        Dim i As Object = 3
        Dim j As Object
        j = 10
        j = 10.1
        Dim k As Middle = New Derived()
        Dim l As Middle = New Derived()
        Dim o As Object = New Middle()
        MM(l, o)

        Dim ibase1 As IBase1 = Nothing
        Dim ibase2 As IBase2 = Nothing
        Dim imiddle As IMiddle = Nothing
        Dim iderived As IDerived = Nothing

        Dim ia As Object = imiddle
        Dim ic As Object = imiddle
        ic = ibase1
        Dim id As IBase1 = iderived
        Dim ie As IBase1 = iderived
        ie = imiddle
        Dim iff As IBase1 = imiddle
        iff = ibase1
        Dim ig As Object = iderived
        ig = ibase1
        ig = imiddle
        Dim ih = imiddle
        ih = iderived
        Dim ik As IMiddle = iderived
        Dim il As IMiddle = iderived
        Dim io As Object = imiddle
        IMM(il, io)
        Dim im As IBase2 = iderived
        Dim isink As Object = ibase2
        isink = 3
    End Sub

    Private fa As Object = New Middle()
    Private fb As Object = New Value(10)
    Private fc As Object = New Middle()
    Private fd As Base = New Derived()
    Private fe As Base = New Derived()
    Private ff As Base = New Middle()
    Private fg As Object = New Derived()
    Private fh As New Middle()
    Private fi As Object = 3
    Private fj As Object
    Private fk As Middle = New Derived()
    Private fl As Middle = New Derived()
    Private fo As Object = New Middle()

    Private Shared fibase1 As IBase1 = Nothing
    Private Shared fibase2 As IBase2 = Nothing
    Private Shared fimiddle As IMiddle= Nothing
    Private Shared fiderived As IDerived = Nothing

    Private fia As Object = fimiddle
    Private fic As Object = fimiddle
    Private fid As IBase1 = fiderived
    Private fie As IBase1 = fiderived
    Private fiff As IBase1 = fimiddle
    Private fig As Object = fiderived
    Private fih As IMiddle = fimiddle
    Private fik As IMiddle = fiderived
    Private fil As IMiddle = fiderived
    Private fio As Object = fimiddle
    Private fisink As Object = fibase2
    Private fim As IBase2 = fiderived

    Sub M1()
        fc = New Base()
        fe = New Middle()
        ff = New Base()
        fg = New Base()
        fg = New Middle()
        fh = New Derived()
        fj = 10
        fj = 10.1
        MM(fl, fo)

        fic = fibase1
        fie = fimiddle
        fiff = fibase1
        fig = fibase1
        fig = fimiddle
        fih = fiderived
        IMM(fil, fio)
        fisink = 3
    End Sub

    Sub MM(ByRef p1 As  Middle, ByRef p2 As Object)
        p1 = New Middle()
        p2 = Nothing
    End Sub

    Sub IMM(ByRef p1 As IMiddle, ByRef p2 As object)
        p1 = Nothing
        p2 = Nothing
    End Sub
End Class

Class Base
End Class

Class Middle
    Inherits Base
End Class

Class Derived
    Inherits Middle
End Class

Structure Value
    Public Sub New(a As Integer)
        X = a
    End Sub

    Public X As Integer
End Structure

Interface IBase1
End Interface

Interface IBase2
End Interface

Interface IMiddle
    Inherits IBase1
End Interface

Interface IDerived
    Inherits IMiddle
    Inherits IBase2
End Interface
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New SymbolCouldHaveMoreSpecificTypeAnalyzer}, Nothing, Nothing, False,
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "a").WithArguments("a", "Middle").WithLocation(3, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "b").WithArguments("b", "Value").WithLocation(4, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "c").WithArguments("c", "Base").WithLocation(5, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "d").WithArguments("d", "Derived").WithLocation(7, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "e").WithArguments("e", "Middle").WithLocation(8, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "g").WithArguments("g", "Base").WithLocation(12, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "i").WithArguments("i", "Integer").WithLocation(17, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "k").WithArguments("k", "Derived").WithLocation(21, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "ia").WithArguments("ia", "IMiddle").WithLocation(31, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "ic").WithArguments("ic", "IBase1").WithLocation(32, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "id").WithArguments("id", "IDerived").WithLocation(34, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "ie").WithArguments("ie", "IMiddle").WithLocation(35, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "ig").WithArguments("ig", "IBase1").WithLocation(39, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "ik").WithArguments("ik", "IDerived").WithLocation(44, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "im").WithArguments("im", "IDerived").WithLocation(48, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fa").WithArguments("Private fa As Object", "Middle").WithLocation(53, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fb").WithArguments("Private fb As Object", "Value").WithLocation(54, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fc").WithArguments("Private fc As Object", "Base").WithLocation(55, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fd").WithArguments("Private fd As Base", "Derived").WithLocation(56, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fe").WithArguments("Private fe As Base", "Middle").WithLocation(57, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fg").WithArguments("Private fg As Object", "Base").WithLocation(59, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fi").WithArguments("Private fi As Object", "Integer").WithLocation(61, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fk").WithArguments("Private fk As Middle", "Derived").WithLocation(63, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fia").WithArguments("Private fia As Object", "IMiddle").WithLocation(72, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fic").WithArguments("Private fic As Object", "IBase1").WithLocation(73, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fid").WithArguments("Private fid As IBase1", "IDerived").WithLocation(74, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fie").WithArguments("Private fie As IBase1", "IMiddle").WithLocation(75, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fig").WithArguments("Private fig As Object", "IBase1").WithLocation(77, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fik").WithArguments("Private fik As IMiddle", "IDerived").WithLocation(79, 13),
                                            Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fim").WithArguments("Private fim As IBase2", "IDerived").WithLocation(83, 13))
        End Sub

        <Fact>
        Public Sub ValueContextsVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub M0(Optional a As Integer = 16, Optional b As Integer = 17, Optional c As Integer = 18)
    End Sub

    Public F1 As Integer = 16
    Public F2 As Integer = 17
    Public F3 As Integer = 18

    Public Sub M1()
        M0(16, 17, 18)
        M0(f1, f2, f3)
        M0()
    End Sub
End Class

Enum E
    A = 16
    B
    C = 17
    D = 18
End Enum

Class C1
    Public Sub New (a As Integer, b As Integer, c As Integer)
    End Sub

    Public F1 As C1 = New C1(c:=16, a:=17, b:=18)
    Public F2 As New C1(16, 17, 18)
    Public F3(16) As Integer
    Public F4(17) As Integer                          ' The upper bound specification is not presently treated as a code block. This is suspect.
    Public F5(18) As Integer
    Public F6 As Integer() = New Integer(16) {}
    Public F7 As Integer() = New Integer(17) {}
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New SeventeenTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(SeventeenTestAnalyzer.SeventeenDescriptor.Id, "17").WithLocation(2, 71),
                                           Diagnostic(SeventeenTestAnalyzer.SeventeenDescriptor.Id, "17").WithLocation(6, 28),
                                           Diagnostic(SeventeenTestAnalyzer.SeventeenDescriptor.Id, "17").WithLocation(10, 16),
                                           Diagnostic(SeventeenTestAnalyzer.SeventeenDescriptor.Id, "17").WithLocation(19, 9),
                                           Diagnostic(SeventeenTestAnalyzer.SeventeenDescriptor.Id, "17").WithLocation(27, 40),
                                           Diagnostic(SeventeenTestAnalyzer.SeventeenDescriptor.Id, "17").WithLocation(28, 29),
                                           Diagnostic(SeventeenTestAnalyzer.SeventeenDescriptor.Id, "17").WithLocation(33, 42),
                                           Diagnostic(SeventeenTestAnalyzer.SeventeenDescriptor.Id, "M0").WithLocation(12, 9)) ' The M0 diagnostic is an artifact of the VB compiler filling in default values in the high-level bound tree, and is questionable.
        End Sub
    End Class
End Namespace
