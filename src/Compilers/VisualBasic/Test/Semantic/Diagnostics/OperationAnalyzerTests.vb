' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics.SystemLanguage
Imports Roslyn.Test.Utilities
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
        Public Sub BadStuffVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub M1(z as Integer)
        Framitz()
        Dim x As Integer = Bexley()
        Dim y As Integer = 10
        Dim d As Double() = Nothing
        M1(d)
        Goto
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyAnalyzerDiagnostics({New BadStuffTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(BadStuffTestAnalyzer.InvalidExpressionDescriptor.Id, "Framitz()").WithLocation(3, 9),
                                           Diagnostic(BadStuffTestAnalyzer.IsInvalidDescriptor.Id, "Framitz()").WithLocation(3, 9),
                                           Diagnostic(BadStuffTestAnalyzer.InvalidExpressionDescriptor.Id, "Bexley()").WithLocation(4, 28),
                                           Diagnostic(BadStuffTestAnalyzer.IsInvalidDescriptor.Id, "Bexley()").WithLocation(4, 28),
                                           Diagnostic(BadStuffTestAnalyzer.InvalidExpressionDescriptor.Id, "M1(d)").WithLocation(7, 9),
                                           Diagnostic(BadStuffTestAnalyzer.IsInvalidDescriptor.Id, "M1(d)").WithLocation(7, 9),
                                           Diagnostic(BadStuffTestAnalyzer.InvalidStatementDescriptor.Id, "Goto").WithLocation(8, 9),
                                           Diagnostic(BadStuffTestAnalyzer.IsInvalidDescriptor.Id, "Goto").WithLocation(8, 9))
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
        Public Sub SwitchVisualBasic()
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

        Select Case x
            Case Else
                Exit Select
        End Select     

        Select Case x
        End Select

        Select Case x
            Case 1
                Exit Select
            Case
                Exit Select
        End Select   

        Select Case x
            Case 1
                Exit Select
            Case =
                Exit Select
        End Select  

        Select Case x
            Case 1
                Exit Select
            Case 2 to
                Exit Select
        End Select  
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(60, 17),
                                   Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(68, 1),
                                   Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(74, 22))
            comp.VerifyAnalyzerDiagnostics({New SwitchTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(SwitchTestAnalyzer.SparseSwitchDescriptor.Id, "x").WithLocation(12, 21),
                                           Diagnostic(SwitchTestAnalyzer.SparseSwitchDescriptor.Id, "x").WithLocation(30, 21),
                                           Diagnostic(SwitchTestAnalyzer.SparseSwitchDescriptor.Id, "x").WithLocation(37, 21),
                                           Diagnostic(SwitchTestAnalyzer.NoDefaultSwitchDescriptor.Id, "x").WithLocation(37, 21),
                                           Diagnostic(SwitchTestAnalyzer.NoDefaultSwitchDescriptor.Id, "x").WithLocation(42, 21),
                                           Diagnostic(SwitchTestAnalyzer.OnlyDefaultSwitchDescriptor.Id, "x").WithLocation(49, 21),
                                           Diagnostic(SwitchTestAnalyzer.SparseSwitchDescriptor.Id, "x").WithLocation(54, 21),
                                           Diagnostic(SwitchTestAnalyzer.NoDefaultSwitchDescriptor.Id, "x").WithLocation(54, 21))
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

    Public Sub M3(Optional a As Integer = Nothing, Optional b As Integer = 0)
    End Sub

    Public Sub M4()
        M3(Nothing, 0)
        M3(Nothing,)
        M3(,0)
        M3(,)
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
                                           Diagnostic(InvocationTestAnalyzer.BigParamArrayArgumentsDescriptor.Id, "M0(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)").WithLocation(14, 9),
                                           Diagnostic(InvocationTestAnalyzer.BigParamArrayArgumentsDescriptor.Id, "M0(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13)").WithLocation(15, 9),
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

        <Fact>
        Public Sub NullArgumentVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class Foo
    Public Sub New(X As String)

    End Sub
End Class

Class C
    Public Sub M0(x As String, y As String)
    End Sub

    Public Sub M1()
        M0("""", """")
        M0(Nothing, """")
        M0("""", Nothing)
        M0(Nothing, Nothing)
    End Sub

    Public Sub M2()
        Dim f1 = New Foo("""")
        Dim f2 = New Foo(Nothing)
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New NullArgumentTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(NullArgumentTestAnalyzer.NullArgumentsDescriptor.Id, "Nothing").WithLocation(13, 12),
                                           Diagnostic(NullArgumentTestAnalyzer.NullArgumentsDescriptor.Id, "Nothing").WithLocation(14, 18),
                                           Diagnostic(NullArgumentTestAnalyzer.NullArgumentsDescriptor.Id, "Nothing").WithLocation(15, 12),
                                           Diagnostic(NullArgumentTestAnalyzer.NullArgumentsDescriptor.Id, "Nothing").WithLocation(15, 21),
                                           Diagnostic(NullArgumentTestAnalyzer.NullArgumentsDescriptor.Id, "Nothing").WithLocation(20, 26))
        End Sub

        <Fact>
        Public Sub MemberInitializerVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class Bar
    Public Field As Boolean
End Class

Class Foo
    Public Field As Integer
    Public Property Prop1 As String
    Public Property Prop2 As Bar
End Class

Class C
    Public Sub M1()
        Dim f1 = New Foo()
        Dim f2 = New Foo() With {.Field = 10}
        Dim f3 = New Foo With {.Prop1 = Nothing}
        Dim f4 = New Foo With {.Field = 10, .Prop1 = Nothing}
        Dim f5 = New Foo With {.Prop2 = New Bar() With {.Field = True}}

        Dim e1 = New Foo() With {.Prop1 = 10}
        Dim e2 = New Foo With {10}
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedQualifiedNameInInit, "").WithLocation(20, 32))
            comp.VerifyAnalyzerDiagnostics({New MemberInitializerTestAnalyzer}, Nothing, Nothing, False,
                                            Diagnostic(MemberInitializerTestAnalyzer.DoNotUseFieldInitializerDescriptor.Id, ".Field = 10").WithLocation(14, 34),
                                            Diagnostic(MemberInitializerTestAnalyzer.DoNotUsePropertyInitializerDescriptor.Id, ".Prop1 = Nothing").WithLocation(15, 32),
                                            Diagnostic(MemberInitializerTestAnalyzer.DoNotUseFieldInitializerDescriptor.Id, ".Field = 10").WithLocation(16, 32),
                                            Diagnostic(MemberInitializerTestAnalyzer.DoNotUsePropertyInitializerDescriptor.Id, ".Prop1 = Nothing").WithLocation(16, 45),
                                            Diagnostic(MemberInitializerTestAnalyzer.DoNotUsePropertyInitializerDescriptor.Id, ".Prop2 = New Bar() With {.Field = True}").WithLocation(17, 32),
                                            Diagnostic(MemberInitializerTestAnalyzer.DoNotUseFieldInitializerDescriptor.Id, ".Field = True").WithLocation(17, 57),
                                            Diagnostic(MemberInitializerTestAnalyzer.DoNotUsePropertyInitializerDescriptor.Id, ".Prop1 = 10").WithLocation(19, 34))
        End Sub

        <Fact>
        Public Sub AssignmentVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class Bar
    Public Field As Boolean
End Class

Class Foo
    Public Field As Integer
    Public Property Prop1 As String
    Public Property Prop2 As Bar
End Class

Class C
    Public Sub M1()
        Dim f1 = New Foo()
        Dim f2 = New Foo() With {.Field = 10}
        Dim f3 = New Foo With {.Prop1 = Nothing}
        Dim f4 = New Foo With {.Field = 10, .Prop1 = Nothing}
        Dim f5 = New Foo With {.Prop2 = New Bar() With {.Field = True}}
    End Sub

    Public Sub M2()
        Dim f1 = New Foo With {.Prop2 = New Bar() With {.Field = True}}
        f1.Field = 0
        f1.Prop1 = Nothing

        Dim f2 = New Bar()
        f2.Field = True
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New AssignmentTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(AssignmentTestAnalyzer.DoNotUseMemberAssignmentDescriptor.Id, "f1.Field = 0").WithLocation(22, 9),
                Diagnostic(AssignmentTestAnalyzer.DoNotUseMemberAssignmentDescriptor.Id, "f1.Prop1 = Nothing").WithLocation(23, 9),
                Diagnostic(AssignmentTestAnalyzer.DoNotUseMemberAssignmentDescriptor.Id, "f2.Field = True").WithLocation(26, 9))
        End Sub

        <Fact>
        Public Sub ArrayInitializerVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub M1()
        Dim arr1 = New Integer() {}
        Dim arr2 As Object = {}
        Dim arr3 = {}

        Dim arr4 = New Integer() {1, 2, 3}
        Dim arr5 = {1, 2, 3}
        Dim arr6 As C() = {Nothing, Nothing, Nothing}

        Dim arr7 = New Integer() {1, 2, 3, 4, 5, 6}                                 ' LargeList
        Dim arr8 = {1, 2, 3, 4, 5, 6}                                               ' LargeList
        Dim arr9 As C() = {Nothing, Nothing, Nothing, Nothing, Nothing, Nothing}    ' LargeList

        Dim arr10 As Integer(,) = {{1, 2, 3, 4, 5, 6}}      ' LargeList
        Dim arr11 = New Integer(,) {{1, 2, 3, 4, 5, 6},     ' LargeList
                                    {7, 8, 9, 10, 11, 12}}  ' LargeList
        Dim arr12 As C(,) = {{Nothing, Nothing, Nothing, Nothing, Nothing, Nothing},    ' LargeList
                            {Nothing, Nothing, Nothing, Nothing, Nothing, Nothing}}     ' LargeList
        Dim arr13 = {{{1, 2}, {3, 4}}, {{5, 6}, {7, 8}}}

        ' jagged array
        Dim arr14 = {({1, 2, 3}), ({4, 5}), ({6}), ({7})}
        Dim arr15 = {({({1, 2, 3, 4, 5, 6})})}              ' LargeList
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New ArrayInitializerTestAnalyzer()}, Nothing, Nothing, False,
                Diagnostic(ArrayInitializerTestAnalyzer.DoNotUseLargeListOfArrayInitializersDescriptor.Id, "{1, 2, 3, 4, 5, 6}").WithLocation(11, 34),
                Diagnostic(ArrayInitializerTestAnalyzer.DoNotUseLargeListOfArrayInitializersDescriptor.Id, "{1, 2, 3, 4, 5, 6}").WithLocation(12, 20),
                Diagnostic(ArrayInitializerTestAnalyzer.DoNotUseLargeListOfArrayInitializersDescriptor.Id, "{Nothing, Nothing, Nothing, Nothing, Nothing, Nothing}").WithLocation(13, 27),
                Diagnostic(ArrayInitializerTestAnalyzer.DoNotUseLargeListOfArrayInitializersDescriptor.Id, "{1, 2, 3, 4, 5, 6}").WithLocation(15, 36),
                Diagnostic(ArrayInitializerTestAnalyzer.DoNotUseLargeListOfArrayInitializersDescriptor.Id, "{1, 2, 3, 4, 5, 6}").WithLocation(16, 37),
                Diagnostic(ArrayInitializerTestAnalyzer.DoNotUseLargeListOfArrayInitializersDescriptor.Id, "{7, 8, 9, 10, 11, 12}").WithLocation(17, 37),
                Diagnostic(ArrayInitializerTestAnalyzer.DoNotUseLargeListOfArrayInitializersDescriptor.Id, "{Nothing, Nothing, Nothing, Nothing, Nothing, Nothing}").WithLocation(18, 30),
                Diagnostic(ArrayInitializerTestAnalyzer.DoNotUseLargeListOfArrayInitializersDescriptor.Id, "{Nothing, Nothing, Nothing, Nothing, Nothing, Nothing}").WithLocation(19, 29),
                Diagnostic(ArrayInitializerTestAnalyzer.DoNotUseLargeListOfArrayInitializersDescriptor.Id, "{1, 2, 3, 4, 5, 6}").WithLocation(24, 25))
        End Sub

        <Fact>
        Public Sub VariableDeclarationVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
#Disable Warning BC42024
    Dim field1, field2, field3, field4 As Integer
    Public Sub M1()
        Dim a1 = 10
        Dim b1 As New Integer, b2, b3, b4 As New Foo(1)         'too many
        Dim c1, c2 As Integer, c3, c4 As Foo                    'too many
        Dim d1() As Foo
        Dim e1 As Integer = 10, e2 = {1, 2, 3}, e3, e4 As C     'too many
        Dim f1 = 10, f2 = 11, f3 As Integer
        Dim h1, h2, , h3 As Integer                             'too many
        Dim i1, i2, i3, i4 As New UndefType                     'too many
        Dim j1, j2, j3, j4 As UndefType                         'too many
        Dim k1 As Integer, k2, k3, k4 As New Foo(1)             'too many
    End Sub
#Enable Warning BC42024
End Class

Class Foo
    Public Sub New(X As Integer)
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ExpectedIdentifier, "").WithLocation(11, 21),
                Diagnostic(ERRID.ERR_UndefinedType1, "UndefType").WithArguments("UndefType").WithLocation(12, 35),
                Diagnostic(ERRID.ERR_UndefinedType1, "UndefType").WithArguments("UndefType").WithLocation(12, 35),
                Diagnostic(ERRID.ERR_UndefinedType1, "UndefType").WithArguments("UndefType").WithLocation(12, 35),
                Diagnostic(ERRID.ERR_UndefinedType1, "UndefType").WithArguments("UndefType").WithLocation(12, 35),
                Diagnostic(ERRID.ERR_UndefinedType1, "UndefType").WithArguments("UndefType").WithLocation(13, 31),
                Diagnostic(ERRID.ERR_UndefinedType1, "UndefType").WithArguments("UndefType").WithLocation(13, 31),
                Diagnostic(ERRID.ERR_UndefinedType1, "UndefType").WithArguments("UndefType").WithLocation(13, 31),
                Diagnostic(ERRID.ERR_UndefinedType1, "UndefType").WithArguments("UndefType").WithLocation(13, 31))
            comp.VerifyAnalyzerDiagnostics({New VariableDeclarationTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(VariableDeclarationTestAnalyzer.LocalVarInitializedDeclarationDescriptor.Id, "a1").WithLocation(5, 13),
                Diagnostic(VariableDeclarationTestAnalyzer.TooManyLocalVarDeclarationsDescriptor.Id, "Dim b1 As New Integer, b2, b3, b4 As New Foo(1)").WithLocation(6, 9),
                Diagnostic(VariableDeclarationTestAnalyzer.LocalVarInitializedDeclarationDescriptor.Id, "b1").WithLocation(6, 13),
                Diagnostic(VariableDeclarationTestAnalyzer.LocalVarInitializedDeclarationDescriptor.Id, "b2").WithLocation(6, 32),
                Diagnostic(VariableDeclarationTestAnalyzer.LocalVarInitializedDeclarationDescriptor.Id, "b3").WithLocation(6, 36),
                Diagnostic(VariableDeclarationTestAnalyzer.LocalVarInitializedDeclarationDescriptor.Id, "b4").WithLocation(6, 40),
                Diagnostic(VariableDeclarationTestAnalyzer.TooManyLocalVarDeclarationsDescriptor.Id, "Dim c1, c2 As Integer, c3, c4 As Foo").WithLocation(7, 9),
                Diagnostic(VariableDeclarationTestAnalyzer.TooManyLocalVarDeclarationsDescriptor.Id, "Dim e1 As Integer = 10, e2 = {1, 2, 3}, e3, e4 As C").WithLocation(9, 9),
                Diagnostic(VariableDeclarationTestAnalyzer.LocalVarInitializedDeclarationDescriptor.Id, "e1").WithLocation(9, 13),
                Diagnostic(VariableDeclarationTestAnalyzer.LocalVarInitializedDeclarationDescriptor.Id, "e2").WithLocation(9, 33),
                Diagnostic(VariableDeclarationTestAnalyzer.LocalVarInitializedDeclarationDescriptor.Id, "f1").WithLocation(10, 13),
                Diagnostic(VariableDeclarationTestAnalyzer.LocalVarInitializedDeclarationDescriptor.Id, "f2").WithLocation(10, 22),
                Diagnostic(VariableDeclarationTestAnalyzer.TooManyLocalVarDeclarationsDescriptor.Id, "Dim h1, h2, , h3 As Integer").WithLocation(11, 9),
                Diagnostic(VariableDeclarationTestAnalyzer.TooManyLocalVarDeclarationsDescriptor.Id, "Dim i1, i2, i3, i4 As New UndefType").WithLocation(12, 9),
                Diagnostic(VariableDeclarationTestAnalyzer.TooManyLocalVarDeclarationsDescriptor.Id, "Dim j1, j2, j3, j4 As UndefType").WithLocation(13, 9),
                Diagnostic(VariableDeclarationTestAnalyzer.TooManyLocalVarDeclarationsDescriptor.Id, "Dim k1 As Integer, k2, k3, k4 As New Foo(1)").WithLocation(14, 9),
                Diagnostic(VariableDeclarationTestAnalyzer.LocalVarInitializedDeclarationDescriptor.Id, "k2").WithLocation(14, 28),
                Diagnostic(VariableDeclarationTestAnalyzer.LocalVarInitializedDeclarationDescriptor.Id, "k3").WithLocation(14, 32),
                Diagnostic(VariableDeclarationTestAnalyzer.LocalVarInitializedDeclarationDescriptor.Id, "k4").WithLocation(14, 36))
        End Sub
        <Fact>
        Public Sub CaseVisualBasic()
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

        Select Case x
            Case Else
                Exit Select
        End Select     

        Select Case x
        End Select

        Select Case x
            Case 1
                Exit Select
            Case
                Exit Select
        End Select   

        Select Case x
            Case 1
                Exit Select
            Case =
                Exit Select
        End Select  

        Select Case x
            Case 1
                Exit Select
            Case 2 to
                Exit Select
        End Select  
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(60, 17),
                                   Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(68, 1),
                                   Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(74, 22))
            comp.VerifyAnalyzerDiagnostics({New CaseTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(CaseTestAnalyzer.MultipleCaseClausesDescriptor.Id,
"Case 1, 2
                Exit Select").WithLocation(4, 13),
                Diagnostic(CaseTestAnalyzer.HasDefaultCaseDescriptor.Id, "Case Else").WithLocation(8, 13),
                Diagnostic(CaseTestAnalyzer.HasDefaultCaseDescriptor.Id, "Case Else").WithLocation(17, 13),
                Diagnostic(CaseTestAnalyzer.HasDefaultCaseDescriptor.Id, "Case Else").WithLocation(26, 13),
                Diagnostic(CaseTestAnalyzer.MultipleCaseClausesDescriptor.Id,
"Case 1, 980 To 985
                Exit Select").WithLocation(31, 13),
                Diagnostic(CaseTestAnalyzer.HasDefaultCaseDescriptor.Id, "Case Else").WithLocation(33, 13),
                Diagnostic(CaseTestAnalyzer.MultipleCaseClausesDescriptor.Id,
"Case 1 to 3, 980 To 985
                Exit Select").WithLocation(38, 13),
                Diagnostic(CaseTestAnalyzer.HasDefaultCaseDescriptor.Id, "Case Else").WithLocation(50, 13))
        End Sub

        <Fact>
        Public Sub ExplicitVsImplicitInstancesVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Overridable Sub M1()
        Me.M1()
        M1()
    End Sub
    Public Sub M2()
    End Sub
End Class

Class D
    Inherits C
    Public Overrides Sub M1()
        MyBase.M1()
        M1()
        M2()
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New ExplicitVsImplicitInstanceAnalyzer}, Nothing, Nothing, False,
               Diagnostic(ExplicitVsImplicitInstanceAnalyzer.ExplicitInstanceDescriptor.Id, "Me").WithLocation(3, 9),
               Diagnostic(ExplicitVsImplicitInstanceAnalyzer.ImplicitInstanceDescriptor.Id, "M1").WithLocation(4, 9),
               Diagnostic(ExplicitVsImplicitInstanceAnalyzer.ExplicitInstanceDescriptor.Id, "MyBase").WithLocation(13, 9),
               Diagnostic(ExplicitVsImplicitInstanceAnalyzer.ImplicitInstanceDescriptor.Id, "M1").WithLocation(14, 9),
               Diagnostic(ExplicitVsImplicitInstanceAnalyzer.ImplicitInstanceDescriptor.Id, "M2").WithLocation(15, 9))
        End Sub

        <Fact>
        Public Sub EventAndMethodReferencesVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Delegate Sub MumbleEventHandler(sender As Object, args As System.EventArgs)

Class C
    Public Event Mumble As MumbleEventHandler

    Public Sub OnMumble(args As System.EventArgs)
        AddHandler Mumble, New MumbleEventHandler(AddressOf Mumbler)
        AddHandler Mumble, New MumbleEventHandler(Sub(s As Object, a As System.EventArgs)
                                                  End Sub)
        AddHandler Mumble, Sub(s As Object, a As System.EventArgs)
                           End Sub
        RaiseEvent Mumble(Me, args)
        ' Dim o As object = AddressOf Mumble
        Dim d As MumbleEventHandler = AddressOf Mumbler
        Mumbler(Me, Nothing)
        RemoveHandler Mumble, AddressOf Mumbler
    End Sub

    Private Sub Mumbler(sender As Object, args As System.EventArgs) 
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New MemberReferenceAnalyzer}, Nothing, Nothing, False,
                 Diagnostic(MemberReferenceAnalyzer.HandlerAddedDescriptor.Id, "AddHandler Mumble, New MumbleEventHandler(AddressOf Mumbler)").WithLocation(7, 9),  ' Bug: Missing a EventReferenceExpression here https://github.com/dotnet/roslyn/issues/8346
                 Diagnostic(MemberReferenceAnalyzer.MethodBindingDescriptor.Id, "AddressOf Mumbler").WithLocation(7, 51),
                 Diagnostic(MemberReferenceAnalyzer.HandlerAddedDescriptor.Id, "AddHandler Mumble, New MumbleEventHandler(Sub(s As Object, a As System.EventArgs)
                                                  End Sub)").WithLocation(8, 9),                                                                                    ' Bug: Missing a EventReferenceExpression here https://github.com/dotnet/roslyn/issues/8346
                 Diagnostic(MemberReferenceAnalyzer.HandlerAddedDescriptor.Id, "AddHandler Mumble, Sub(s As Object, a As System.EventArgs)
                           End Sub").WithLocation(10, 9),                                                                                                           ' Bug: Missing a EventReferenceExpression here https://github.com/dotnet/roslyn/issues/8346
                 Diagnostic(MemberReferenceAnalyzer.FieldReferenceDescriptor.Id, "Mumble").WithLocation(12, 20),   ' Bug: This should be an event reference. https://github.com/dotnet/roslyn/issues/8345
                 Diagnostic(MemberReferenceAnalyzer.MethodBindingDescriptor.Id, "AddressOf Mumbler").WithLocation(14, 39),
                 Diagnostic(MemberReferenceAnalyzer.HandlerRemovedDescriptor.Id, "RemoveHandler Mumble, AddressOf Mumbler").WithLocation(16, 9),                    ' Bug: Missing a EventReferenceExpression here https://github.com/dotnet/roslyn/issues/8346
                 Diagnostic(MemberReferenceAnalyzer.MethodBindingDescriptor.Id, "AddressOf Mumbler").WithLocation(16, 31))
        End Sub

        <Fact>
        Public Sub ParamArraysVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub M0(a As Integer, ParamArray b As Integer())
    End Sub

    Public Sub M1()
        M0(1)
        M0(1, 2)
        M0(1, 2, 3, 4)
        M0(1, 2, 3, 4, 5)
        M0(1, 2, 3, 4, 5, 6)
        M0(1, New Integer() { 2, 3, 4 })
        M0(1, New Integer() { 2, 3, 4, 5 })
        M0(1, New Integer() { 2, 3, 4, 5, 6 })
        Dim local As D = new D(1, 2, 3, 4, 5)
        local = new D(1, New Integer() { 2, 3, 4, 5 })
        local = new D(1, 2, 3, 4)
        local = new D(1, New Integer() { 2, 3, 4 })
    End Sub
End Class

Class D
    Public Sub New(a As Integer, ParamArray b As Integer())
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New ParamsArrayTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(ParamsArrayTestAnalyzer.LongParamsDescriptor.Id, "M0(1, 2, 3, 4, 5)").WithLocation(9, 9),
                                           Diagnostic(ParamsArrayTestAnalyzer.LongParamsDescriptor.Id, "M0(1, 2, 3, 4, 5)").WithLocation(9, 9),
                                           Diagnostic(ParamsArrayTestAnalyzer.LongParamsDescriptor.Id, "M0(1, 2, 3, 4, 5, 6)").WithLocation(10, 9),
                                           Diagnostic(ParamsArrayTestAnalyzer.LongParamsDescriptor.Id, "M0(1, 2, 3, 4, 5, 6)").WithLocation(10, 9),
                                           Diagnostic(ParamsArrayTestAnalyzer.LongParamsDescriptor.Id, "New Integer() { 2, 3, 4, 5 }").WithLocation(12, 15),
                                           Diagnostic(ParamsArrayTestAnalyzer.LongParamsDescriptor.Id, "New Integer() { 2, 3, 4, 5 }").WithLocation(12, 15),
                                           Diagnostic(ParamsArrayTestAnalyzer.LongParamsDescriptor.Id, "New Integer() { 2, 3, 4, 5, 6 }").WithLocation(13, 15),
                                           Diagnostic(ParamsArrayTestAnalyzer.LongParamsDescriptor.Id, "New Integer() { 2, 3, 4, 5, 6 }").WithLocation(13, 15),
                                           Diagnostic(ParamsArrayTestAnalyzer.LongParamsDescriptor.Id, "D").WithLocation(14, 30),
                                           Diagnostic(ParamsArrayTestAnalyzer.LongParamsDescriptor.Id, "New Integer() { 2, 3, 4, 5 }").WithLocation(15, 26))
        End Sub

        <Fact>
        Public Sub FieldInitializersVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public F1 As Integer = 44
    Public F2 As String = "Hello"
    Public F3 As Integer = Foo()

    Public Shared Function Foo()
        Return 10
    End Function

    Public Shared Function Bar(Optional P1 As Integer = 10, Optional F2 As Integer = 20)
        Return P1 + F2
    End Function
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New EqualsValueTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(EqualsValueTestAnalyzer.EqualsValueDescriptor.Id, "= 44").WithLocation(2, 26),
                                           Diagnostic(EqualsValueTestAnalyzer.EqualsValueDescriptor.Id, "= ""Hello""").WithLocation(3, 25),
                                           Diagnostic(EqualsValueTestAnalyzer.EqualsValueDescriptor.Id, "= Foo()").WithLocation(4, 26),
                                           Diagnostic(EqualsValueTestAnalyzer.EqualsValueDescriptor.Id, "= 20").WithLocation(10, 84))
        End Sub

        <Fact>
        Public Sub OwningSymbolVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub UnFunkyMethod()
        Dim x As Integer = 0
        Dim y As Integer = x
    End Sub

    Public Sub FunkyMethod()
        Dim x As Integer = 0
        Dim y As Integer = x
    End Sub

    Public FunkyField As Integer = 12
    Public UnFunkyField As Integer = 12
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New OwningSymbolTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(OwningSymbolTestAnalyzer.ExpressionDescriptor.Id, "0").WithLocation(8, 28),
                                           Diagnostic(OwningSymbolTestAnalyzer.ExpressionDescriptor.Id, "x").WithLocation(9, 28),
                                           Diagnostic(OwningSymbolTestAnalyzer.ExpressionDescriptor.Id, "12").WithLocation(12, 36))
        End Sub

        <Fact>
        Public Sub NoneOperationVisualBasic()
            ' BoundCaseStatement is OperationKind.None
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub M1(x as Integer)
        Select Case x
            Case 1, 2
                Exit Select
            Case = 10
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
     
    Public Property Fred As Integer  
        Set(value As Integer)  
            Exit Property  
        End Set  
        Get  
            Return 12  
        End Get  
    End Property  
  
    Public Sub Barney  
        Resume  
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New NoneOperationTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(NoneOperationTestAnalyzer.NoneOperationDescriptor.Id, "Resume").WithLocation(23, 9))
        End Sub

        <Fact>
        Public Sub LambdaExpressionVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Imports System

Class B
    Public Sub M1(x As Integer)
        Dim action1 As Action = Sub()
                                End Sub
        Dim action2 As Action = Sub()
                                    Console.WriteLine(1)
                                End Sub
        Dim func1 As Func(Of Integer, Integer) = Function(value As Integer)
                                                     value = value + 1
                                                     value = value + 1
                                                     value = value + 1
                                                     Return value + 1
                                                 End Function
    End Sub
End Class

Delegate Sub MumbleEventHandler(sender As Object, args As EventArgs)

Class C
    Public Event Mumble As MumbleEventHandler

    Public Sub OnMumble(args As EventArgs)
        AddHandler Mumble, New MumbleEventHandler(Sub(s As Object, a As EventArgs)
                                                  End Sub)
        AddHandler Mumble, Sub(s As Object, a As EventArgs)
                               Dim value = 1
                               value = value + 1
                               value = value + 1
                               value = value + 1
                           End Sub
        RaiseEvent Mumble(Me, args)
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New LambdaTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(LambdaTestAnalyzer.LambdaExpressionDescriptor.Id, "Sub()
                                End Sub").WithLocation(5, 33),
                Diagnostic(LambdaTestAnalyzer.LambdaExpressionDescriptor.Id, "Sub(s As Object, a As EventArgs)
                                                  End Sub").WithLocation(25, 51),
                Diagnostic(LambdaTestAnalyzer.LambdaExpressionDescriptor.Id, "Sub()
                                    Console.WriteLine(1)
                                End Sub").WithLocation(7, 33),
                Diagnostic(LambdaTestAnalyzer.LambdaExpressionDescriptor.Id, "Sub(s As Object, a As EventArgs)
                               Dim value = 1
                               value = value + 1
                               value = value + 1
                               value = value + 1
                           End Sub").WithLocation(27, 28),
                Diagnostic(LambdaTestAnalyzer.TooManyStatementsInLambdaExpressionDescriptor.Id, "Sub(s As Object, a As EventArgs)
                               Dim value = 1
                               value = value + 1
                               value = value + 1
                               value = value + 1
                           End Sub").WithLocation(27, 28),
                Diagnostic(LambdaTestAnalyzer.LambdaExpressionDescriptor.Id, "Function(value As Integer)
                                                     value = value + 1
                                                     value = value + 1
                                                     value = value + 1
                                                     Return value + 1
                                                 End Function").WithLocation(10, 50),
                Diagnostic(LambdaTestAnalyzer.TooManyStatementsInLambdaExpressionDescriptor.Id, "Function(value As Integer)
                                                     value = value + 1
                                                     value = value + 1
                                                     value = value + 1
                                                     Return value + 1
                                                 End Function").WithLocation(10, 50))
        End Sub

        <WorkItem(8385, "https://github.com/dotnet/roslyn/issues/8385")>
        <Fact>
        Public Sub StaticMemberReferenceVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class D
    Public Shared Event E()

    Public Shared Field As Integer

    Public Shared Property P As Integer

    Public Shared Sub Method()
    End Sub
End Class

Class C
    Public Shared Event E()

    Public Shared Sub Bar()
    End Sub

    Public Sub Foo()
        AddHandler C.E, AddressOf D.Method
        RaiseEvent E()  ' Can't raise static event with type in VB
        C.Bar()

        AddHandler D.E, Sub()
                        End Sub
        D.Field = 1
        Dim x = D.P
        D.Method()
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New StaticMemberTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(StaticMemberTestAnalyzer.StaticMemberDescriptor.Id, "AddHandler C.E, AddressOf D.Method").WithLocation(19, 9),
                Diagnostic(StaticMemberTestAnalyzer.StaticMemberDescriptor.Id, "AddressOf D.Method").WithLocation(19, 25),
                Diagnostic(StaticMemberTestAnalyzer.StaticMemberDescriptor.Id, "E").WithLocation(20, 20),
                Diagnostic(StaticMemberTestAnalyzer.StaticMemberDescriptor.Id, "C.Bar()").WithLocation(21, 9),
                Diagnostic(StaticMemberTestAnalyzer.StaticMemberDescriptor.Id, "AddHandler D.E, Sub()
                        End Sub").WithLocation(23, 9),
                Diagnostic(StaticMemberTestAnalyzer.StaticMemberDescriptor.Id, "D.Field").WithLocation(25, 9),
                Diagnostic(StaticMemberTestAnalyzer.StaticMemberDescriptor.Id, "D.P").WithLocation(26, 17),
                Diagnostic(StaticMemberTestAnalyzer.StaticMemberDescriptor.Id, "D.Method()").WithLocation(27, 9))
        End Sub

        <Fact>
        Public Sub LabelOperatorsVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Public Class A
    Public Sub Fred()
        Wilma:
        GoTo Betty
        Betty:
        GoTo Wilma
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New LabelOperationsTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(LabelOperationsTestAnalyzer.LabelDescriptor.Id, "Wilma:").WithLocation(3, 9),
                Diagnostic(LabelOperationsTestAnalyzer.GotoDescriptor.Id, "GoTo Betty").WithLocation(4, 9),
                Diagnostic(LabelOperationsTestAnalyzer.LabelDescriptor.Id, "Betty:").WithLocation(5, 9),
                Diagnostic(LabelOperationsTestAnalyzer.GotoDescriptor.Id, "GoTo Wilma").WithLocation(6, 9))
        End Sub

        <Fact>
        Public Sub UnaryBinaryOperatorsVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Public Class A

    Private ReadOnly _value As Integer

    Public Sub New (value As Integer)
        _value = value
    End Sub

    Public Shared Operator +(x As A, Y As A) As A
        Return New A(x._value + y._value)
    End Operator

    Public Shared Operator *(x As A, y As A) As A
        Return New A(x._value * y._value)
    End Operator

    Public Shared Operator -(x  As A) As A
        Return New A(-x._value)
    End Operator

    Public Shared operator +(x As A) As A
        Return New A(+x._value)
    End Operator
End CLass

Class C
    Public Shared Sub Main()
        Dim B As Boolean = False
        Dim d As Double = 100
        Dim a1 As New A(0)
        Dim a2 As New A(100)

        b = Not b
        d = d * 100
        a1 = a1 + a2
        a1 = -a2
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New UnaryAndBinaryOperationsTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(UnaryAndBinaryOperationsTestAnalyzer.BooleanNotDescriptor.Id, "Not b").WithLocation(33, 13),
                Diagnostic(UnaryAndBinaryOperationsTestAnalyzer.DoubleMultiplyDescriptor.Id, "d * 100").WithLocation(34, 13),
                Diagnostic(UnaryAndBinaryOperationsTestAnalyzer.OperatorAddMethodDescriptor.Id, "a1 + a2").WithLocation(35, 14),
                Diagnostic(UnaryAndBinaryOperationsTestAnalyzer.OperatorMinusMethodDescriptor.Id, "-a2").WithLocation(36, 14))
        End Sub

        <Fact>
        Public Sub BinaryOperatorsVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Public Class B2

    Public Shared Operator +(x As B2, y As B2) As B2 
        System.Console.WriteLine("+")
        Return x
    End Operator

    Public Shared Operator -(x As B2, y As B2) As B2 
        System.Console.WriteLine("-")
        Return x
    End Operator

    Public Shared Operator *(x As B2, y As B2) As B2 
        System.Console.WriteLine("*")
        Return x
    End Operator

    Public Shared Operator /(x As B2, y As B2) As B2 
        System.Console.WriteLine("/")
        Return x
    End Operator

    Public Shared Operator \(x As B2, y As B2) As B2 
        System.Console.WriteLine("\")
        Return x
    End Operator

    Public Shared Operator Mod(x As B2, y As B2) As B2 
        System.Console.WriteLine("Mod")
        Return x
    End Operator

    Public Shared Operator ^(x As B2, y As B2) As B2 
        System.Console.WriteLine("^")
        Return x
    End Operator

    Public Shared Operator =(x As B2, y As B2) As B2 
        System.Console.WriteLine("=")
        Return x
    End Operator

    Public Shared Operator <>(x As B2, y As B2) As B2
        System.Console.WriteLine("<>")
        Return x
    End Operator

    Public Shared Operator <(x As B2, y As B2) As B2 
        System.Console.WriteLine("<")
        Return x
    End Operator

    Public Shared Operator >(x As B2, y As B2) As B2 
        System.Console.WriteLine(">")
        Return x
    End Operator

    Public Shared Operator <=(x As B2, y As B2) As B2
        System.Console.WriteLine("<=")
        Return x
    End Operator

    Public Shared Operator >=(x As B2, y As B2) As B2
        System.Console.WriteLine(">=")
        Return x
    End Operator

    Public Shared Operator Like(x As B2, y As B2) As B2
        System.Console.WriteLine("Like")
        Return x
    End Operator

    Public Shared Operator &(x As B2, y As B2) As B2 
        System.Console.WriteLine("&")
        Return x
    End Operator

    Public Shared Operator And(x As B2, y As B2) As B2
        System.Console.WriteLine("And")
        Return x
    End Operator

    Public Shared Operator Or(x As B2, y As B2) As B2 
        System.Console.WriteLine("Or")
        Return x
    End Operator

    Public Shared Operator Xor(x As B2, y As B2) As B2
        System.Console.WriteLine("Xor")
        Return x
    End Operator

    Public Shared Operator <<(x As B2, y As Integer) As B2
        System.Console.WriteLine("<<")
        Return x
    End Operator

    Public Shared Operator >>(x As B2, y As Integer) As B2
        System.Console.WriteLine(">>")
        Return x
    End Operator
End Class

Module Module1

    Sub Main() 
        Dim x, y As New B2()
        Dim r As B2
        r = x + y      
        r = x - y      
        r = x * y      
        r = x / y      
        r = x \ y      
        r = x Mod y    
        ' r = x ^ y  TODO: Bug https://github.com/dotnet/roslyn/issues/9174
        r = x = y      
        r = x <> y     
        r = x < y      
        r = x > y      
        r = x <= y     
        r = x >= y     
        ' r = x Like y   TODO: Bug https://github.com/dotnet/roslyn/issues/9174
        ' r = x & y      TODO: Bug https://github.com/dotnet/roslyn/issues/9174
        r = x And y    
        r = x Or y     
        r = x Xor y    
        r = x << 2     
        r = x >> 3       
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New BinaryOperatorVBTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x + y").WithArguments("OperatorMethodAdd").WithLocation(109, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x - y").WithArguments("OperatorMethodSubtract").WithLocation(110, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x * y").WithArguments("OperatorMethodMultiply").WithLocation(111, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x / y").WithArguments("OperatorMethodDivide").WithLocation(112, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x \ y").WithArguments("OperatorMethodIntegerDivide").WithLocation(113, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x Mod y").WithArguments("OperatorMethodRemainder").WithLocation(114, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x = y").WithArguments("OperatorMethodEquals").WithLocation(116, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x <> y").WithArguments("OperatorMethodNotEquals").WithLocation(117, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x < y").WithArguments("OperatorMethodLessThan").WithLocation(118, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x > y").WithArguments("OperatorMethodGreaterThan").WithLocation(119, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x <= y").WithArguments("OperatorMethodLessThanOrEqual").WithLocation(120, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x >= y").WithArguments("OperatorMethodGreaterThanOrEqual").WithLocation(121, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x And y").WithArguments("OperatorMethodAnd").WithLocation(124, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x Or y").WithArguments("OperatorMethodOr").WithLocation(125, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x Xor y").WithArguments("OperatorMethodExclusiveOr").WithLocation(126, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x << 2").WithArguments("OperatorMethodLeftShift").WithLocation(127, 13),
                Diagnostic(BinaryOperatorVBTestAnalyzer.BinaryUserDefinedOperatorDescriptor.Id, "x >> 3").WithArguments("OperatorMethodRightShift").WithLocation(128, 13))
        End Sub

        <Fact>
        Public Sub InvalidOperatorsVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Public Class B2

    Public Shared Operator +(x As B2, y As B2) As B2 
        System.Console.WriteLine("+")
        Return x
    End Operator

    Public Shared Operator -(x As B2, y As B2) As B2 
        System.Console.WriteLine("-")
        Return x
    End Operator
End Class

Module Module1

    Sub Main() 
        Dim x, y As New B2()
        x = x + 10
        x = x + y
        x = -x
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_TypeMismatch2, "10", New Object() {"Integer", "B2"}).WithLocation(18, 17),
                                   Diagnostic(ERRID.ERR_UnaryOperand2, "-x", New Object() {"-", "B2"}).WithLocation(20, 13))
            comp.VerifyAnalyzerDiagnostics({New OperatorPropertyPullerTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(OperatorPropertyPullerTestAnalyzer.BinaryOperatorDescriptor.Id, "x + 10").WithArguments("OperatorMethodAdd").WithLocation(18, 13),
                                           Diagnostic(OperatorPropertyPullerTestAnalyzer.UnaryOperatorDescriptor.Id, "-x").WithArguments("Invalid").WithLocation(20, 13))
        End Sub

        <Fact>
        Public Sub NullOperationSyntaxVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub M0(ParamArray b As Integer())
    End Sub

    Public Sub M1()
        M0()
        M0(1)
        M0(1, 2)
        M0(New Integer() {  })
        M0(New Integer() { 1 })
        M0(New Integer() { 1, 2 })
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            ' TODO: array should not be treated as ParamArray argument
            ' https://github.com/dotnet/roslyn/issues/8570
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New NullOperationSyntaxTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(NullOperationSyntaxTestAnalyzer.ParamsArrayOperationDescriptor.Id, "M0()").WithLocation(6, 9),
                Diagnostic(NullOperationSyntaxTestAnalyzer.ParamsArrayOperationDescriptor.Id, "M0(1)").WithLocation(7, 9),
                Diagnostic(NullOperationSyntaxTestAnalyzer.ParamsArrayOperationDescriptor.Id, "M0(1, 2)").WithLocation(8, 9),
                Diagnostic(NullOperationSyntaxTestAnalyzer.ParamsArrayOperationDescriptor.Id, "New Integer() {  }").WithLocation(9, 12),
                Diagnostic(NullOperationSyntaxTestAnalyzer.ParamsArrayOperationDescriptor.Id, "New Integer() { 1 }").WithLocation(10, 12),
                Diagnostic(NullOperationSyntaxTestAnalyzer.ParamsArrayOperationDescriptor.Id, "New Integer() { 1, 2 }").WithLocation(11, 12))
        End Sub

        <WorkItem(8114, "https://github.com/dotnet/roslyn/issues/8114")>
        <Fact>
        Public Sub InvalidOperatorVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Function M1(a As Double, b as C) as Double
        Return b + c
    End Sub

    Public Function M2(s As C) As C
        Return -s
    End Function
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_EndFunctionExpected, "Public Function M1(a As Double, b as C) as Double").WithLocation(2, 5),
                Diagnostic(ERRID.ERR_InvalidEndSub, "End Sub").WithLocation(4, 5),
                Diagnostic(ERRID.ERR_InvInsideEndsProc, "Public Function M2(s As C) As C").WithLocation(6, 5),
                Diagnostic(ERRID.ERR_ClassNotExpression1, "c").WithArguments("C").WithLocation(3, 20),
                Diagnostic(ERRID.ERR_UnaryOperand2, "-s").WithArguments("-", "C").WithLocation(7, 16))
            comp.VerifyAnalyzerDiagnostics({New InvalidOperatorExpressionTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(InvalidOperatorExpressionTestAnalyzer.InvalidBinaryDescriptor.Id, "b + c").WithLocation(3, 16),
                Diagnostic(InvalidOperatorExpressionTestAnalyzer.InvalidUnaryDescriptor.Id, "-s").WithLocation(7, 16))
        End Sub

        <WorkItem(9014, "https://github.com/dotnet/roslyn/issues/9014")>
        <Fact>
        Public Sub InvalidConstructorVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Protected Structure S
    End Structure
End Class
Class D
    Shared Sub M(o)
        M(New C.S())
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30389: 'C.S' is not accessible in this context because it is 'Protected'.
        M(New C.S())
              ~~~
]]></errors>)
            ' Reuse ParamsArrayTestAnalyzer for this test.
            comp.VerifyAnalyzerDiagnostics({New ParamsArrayTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(ParamsArrayTestAnalyzer.InvalidConstructorDescriptor.Id, "New C.S()").WithLocation(7, 11))
        End Sub

        <Fact>
        Public Sub ConditionalAccessOperationsVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Property Prop As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property

    Public Field As Integer
    
    Default Public Property Mumble(i As Integer)
        Get
            return Field
        End Get
        Set
            Field = Value
        End Set
    End Property
    
    Public Field1 As C = Nothing

    Public Sub M0(p As C)
        Dim x = p?.Prop
        x = p?.Field
        x = p?(0)
        p?.M0(Nothing)

        x = Field1?.Prop
        x = Field1?.Field
        x = Field1?(0)
        Field1?.M0(Nothing)
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New ConditionalAccessOperationTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessOperationDescriptor.Id, "p?.Prop").WithLocation(24, 17),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessInstanceOperationDescriptor.Id, "p?.Prop").WithLocation(24, 17),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessOperationDescriptor.Id, "p?.Field").WithLocation(25, 13),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessInstanceOperationDescriptor.Id, "p?.Field").WithLocation(25, 13),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessOperationDescriptor.Id, "p?(0)").WithLocation(26, 13),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessInstanceOperationDescriptor.Id, "p?(0)").WithLocation(26, 13),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessOperationDescriptor.Id, "p?.M0(Nothing)").WithLocation(27, 9),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessInstanceOperationDescriptor.Id, "p?.M0(Nothing)").WithLocation(27, 9),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessOperationDescriptor.Id, "Field1?.Prop").WithLocation(29, 13),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessInstanceOperationDescriptor.Id, "Field1?.Prop").WithLocation(29, 13),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessOperationDescriptor.Id, "Field1?.Field").WithLocation(30, 13),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessInstanceOperationDescriptor.Id, "Field1?.Field").WithLocation(30, 13),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessOperationDescriptor.Id, "Field1?(0)").WithLocation(31, 13),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessInstanceOperationDescriptor.Id, "Field1?(0)").WithLocation(31, 13),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessOperationDescriptor.Id, "Field1?.M0(Nothing)").WithLocation(32, 9),
                Diagnostic(ConditionalAccessOperationTestAnalyzer.ConditionalAccessInstanceOperationDescriptor.Id, "Field1?.M0(Nothing)").WithLocation(32, 9))
        End Sub

        <WorkItem(8955, "https://github.com/dotnet/roslyn/issues/8955")>
        <Fact>
        Public Sub ForToLoopConditionCrashVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Imports System   

Module M1
    Class C1(Of t)
        Shared Widening Operator CType(ByVal p1 As C1(Of t)) As Integer
            Return 1
        End Operator
        Shared Widening Operator CType(ByVal p1 As Integer) As C1(Of t)
            Return Nothing
        End Operator
        Shared Operator -(ByVal p1 As C1(Of t), ByVal p2 As C1(Of t)) As C1(Of Short)
            Return Nothing
        End Operator
        Shared Operator +(ByVal p1 As C1(Of t), ByVal p2 As C1(Of t)) As C1(Of Integer)
            Return Nothing
        End Operator
    End Class
    Sub foo()
        For i As C1(Of Integer) = 1 To 10
        Next
    End Sub
End Module
     
Module M2
    ReadOnly Property Moo As Integer
        Get
            Return 1
        End Get
    End Property

    WriteOnly Property Boo As integer
        Set(value As integer)

        End Set
    End Property

    Sub Main()
        For Moo = 1 to Moo step Moo
        Next

        For Boo = 1 to Boo step Boo
        Next
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_LoopControlMustNotBeProperty, "Moo").WithLocation(38, 13),
                Diagnostic(ERRID.ERR_LoopControlMustNotBeProperty, "Boo").WithLocation(41, 13),
                Diagnostic(ERRID.ERR_NoGetProperty1, "Boo").WithArguments("Boo").WithLocation(41, 24),
                Diagnostic(ERRID.ERR_NoGetProperty1, "Boo").WithArguments("Boo").WithLocation(41, 33),
                Diagnostic(ERRID.ERR_UnacceptableForLoopOperator2, "For i As C1(Of Integer) = 1 To 10").WithArguments("Public Shared Operator -(p1 As M1.C1(Of Integer), p2 As M1.C1(Of Integer)) As M1.C1(Of Short)", "M1.C1(Of Integer)").WithLocation(19, 9),
                Diagnostic(ERRID.ERR_ForLoopOperatorRequired2, "For i As C1(Of Integer) = 1 To 10").WithArguments("M1.C1(Of Integer)", "<=").WithLocation(19, 9),
                Diagnostic(ERRID.ERR_ForLoopOperatorRequired2, "For i As C1(Of Integer) = 1 To 10").WithArguments("M1.C1(Of Integer)", ">=").WithLocation(19, 9),
                Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports System").WithLocation(1, 1))
            comp.VerifyAnalyzerDiagnostics({New ForLoopConditionCrashVBTestAnalyzer}, Nothing, Nothing, False,
                Diagnostic(ForLoopConditionCrashVBTestAnalyzer.ForLoopConditionCrashDescriptor.Id, "Moo").WithLocation(38, 24),
                Diagnostic(ForLoopConditionCrashVBTestAnalyzer.ForLoopConditionCrashDescriptor.Id, "Boo").WithLocation(41, 24),
                Diagnostic(ForLoopConditionCrashVBTestAnalyzer.ForLoopConditionCrashDescriptor.Id, "10").WithLocation(19, 40))
        End Sub

        <WorkItem(9012, "https://github.com/dotnet/roslyn/issues/9012")>
        <Fact>
        Public Sub InvalidEventInstanceVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Imports System
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())
        AddHandler Function(ByVal x) x
    End Sub
End Module

Class TestClass

    Event TestEvent As Action

    Shared Sub Test(receiver As TestClass)
        AddHandler receiver?.TestEvent, AddressOf Main
    End Sub

    Shared Sub Main()
    End Sub
End Class

Module Module1
    Sub Main()
        Dim x = {Iterator sub() yield, new object}
        Dim y = {Iterator sub() yield 1, Iterator sub() yield, new object}
        Dim z = {Sub() AddHandler, New Object}
        g0(Iterator sub() Yield)
        g1(Iterator Sub() Yield, 5)
    End Sub

    Sub g0(ByVal x As Func(Of IEnumerator))
    End Sub
    Sub g1(ByVal x As Func(Of IEnumerator), ByVal y As Integer)
    End Sub

    Iterator Function f() As IEnumerator
        Yield
    End Function
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ExpectedComma, "").WithLocation(6, 39),
                Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(6, 39),
                Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(24, 38),
                Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(25, 62),
                Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(26, 34),
                Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(27, 32),
                Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(28, 32),
                Diagnostic(ERRID.ERR_ExpectedExpression, "").WithLocation(37, 14),
                Diagnostic(ERRID.ERR_TooFewGenericArguments1, "IEnumerator").WithArguments("System.Collections.Generic.IEnumerator(Of Out T)").WithLocation(31, 31),
                Diagnostic(ERRID.ERR_TooFewGenericArguments1, "IEnumerator").WithArguments("System.Collections.Generic.IEnumerator(Of Out T)").WithLocation(33, 31),
                Diagnostic(ERRID.ERR_TooFewGenericArguments1, "IEnumerator").WithArguments("System.Collections.Generic.IEnumerator(Of Out T)").WithLocation(36, 30),
                Diagnostic(ERRID.ERR_AddOrRemoveHandlerEvent, "receiver?.TestEvent").WithLocation(15, 20),
                Diagnostic(ERRID.ERR_AddOrRemoveHandlerEvent, "Function(ByVal x) x").WithLocation(6, 20),
                Diagnostic(ERRID.ERR_BadIteratorReturn, "sub").WithLocation(24, 27),
                Diagnostic(ERRID.ERR_BadIteratorReturn, "sub").WithLocation(25, 27),
                Diagnostic(ERRID.ERR_BadIteratorReturn, "sub").WithLocation(25, 51),
                Diagnostic(ERRID.ERR_BadIteratorReturn, "sub").WithLocation(27, 21),
                Diagnostic(ERRID.ERR_BadIteratorReturn, "Sub").WithLocation(28, 21),
                Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports System.Collections.Generic").WithLocation(2, 1))
            comp.VerifyAnalyzerDiagnostics({New MemberReferenceAnalyzer}, Nothing, Nothing, False,
                Diagnostic(MemberReferenceAnalyzer.HandlerAddedDescriptor.Id, "AddHandler, New Object").WithLocation(26, 24),
                Diagnostic(MemberReferenceAnalyzer.InvalidEventDescriptor.Id, "AddHandler, New Object").WithLocation(26, 24),
                Diagnostic(MemberReferenceAnalyzer.HandlerAddedDescriptor.Id, "AddHandler receiver?.TestEvent, AddressOf Main").WithLocation(15, 9),
                Diagnostic(MemberReferenceAnalyzer.InvalidEventDescriptor.Id, "AddHandler receiver?.TestEvent, AddressOf Main").WithLocation(15, 9),
                Diagnostic(MemberReferenceAnalyzer.HandlerAddedDescriptor.Id, "AddHandler Function(ByVal x) x").WithLocation(6, 9),
                Diagnostic(MemberReferenceAnalyzer.InvalidEventDescriptor.Id, "AddHandler Function(ByVal x) x").WithLocation(6, 9))
        End Sub
    End Class
End Namespace
