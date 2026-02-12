' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class BinaryOperators
        Inherits BasicTestBase

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Test1()

            Dim currCulture = System.Threading.Thread.CurrentThread.CurrentCulture
            System.Threading.Thread.CurrentThread.CurrentCulture = New System.Globalization.CultureInfo("en-US", useUserOverride:=False)

            Try

                Dim compilationDef =
<compilation name="VBBinaryOperators1">
    <file name="lib.vb">
        <%= SemanticResourceUtil.PrintResultTestSource %>
    </file>
    <file name="a.vb">
        <%= SemanticResourceUtil.BinaryOperatorsTestSource1 %>
    </file>
</compilation>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

                Assert.True(compilation.Options.CheckOverflow)

                CompileAndVerify(compilation, expectedOutput:=SemanticResourceUtil.BinaryOperatorsTestBaseline1)

                compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOverflowChecks(False))

                Assert.False(compilation.Options.CheckOverflow)

                CompileAndVerify(compilation, expectedOutput:=SemanticResourceUtil.BinaryOperatorsTestBaseline1)

            Catch ex As Exception
                Assert.Null(ex)
            Finally
                System.Threading.Thread.CurrentThread.CurrentCulture = currCulture
            End Try

        End Sub

        <ConditionalFact(GetType(WindowsOrLinuxOnly), Reason:="https://github.com/dotnet/roslyn/issues/77861")>
        Public Sub Test1_Date()
            ' test binary operator between Date value and another type data
            ' call ToString() on it defeat the purpose of these scenarios
            Dim currCulture = System.Threading.Thread.CurrentThread.CurrentCulture
            System.Threading.Thread.CurrentThread.CurrentCulture = New System.Globalization.CultureInfo("en-US", useUserOverride:=False)

            Try

                Dim compilationDef =
<compilation name="VBBinaryOperators11">
    <file name="lib.vb">
        <%= SemanticResourceUtil.PrintResultTestSource %>
    </file>
    <file name="a.vb">
        <![CDATA[
Option Strict Off
Imports System

Module Module1

    Sub Main()
        Dim BoFalse As Boolean
        Dim BoTrue As Boolean
        Dim SB As SByte
        Dim By As Byte
        Dim Sh As Short
        Dim US As UShort
        Dim [In] As Integer
        Dim UI As UInteger
        Dim Lo As Long
        Dim UL As ULong
        Dim De As Decimal
        Dim Si As Single
        Dim [Do] As Double
        Dim St As String
        Dim Ob As Object
        Dim Tc As System.TypeCode
        Dim Da As Date
        Dim Ch As Char
        Dim ChArray() As Char

        BoFalse = False
        BoTrue = True
        SB = -1
        Sh = -3
        [In] = -5
        Lo = -7
        De = -9D
        Si = 10
        [Do] = -11
        St = "12"
        Ob = "-13"
        Da = #8:30:00 AM#
        Ch = "c"c
        Tc = TypeCode.Double
        ChArray = "14"
        By = 22
        US = 24
        UI = 26
        UL = 28

        PrintResult("Da + St", Da + St)
        PrintResult("Da + Ob", Da + Ob)
        PrintResult("Da + Da", Da + Da)
        PrintResult("Da + ChArray", Da + ChArray)
        PrintResult("ChArray + Da", ChArray + Da)
        PrintResult("St + Da", St + Da)
        PrintResult("Ob + Da", Ob + Da)

        PrintResult("Da & BoFalse", Da & BoFalse)
        PrintResult("Da & BoTrue", Da & BoTrue)
        PrintResult("Da & SB", Da & SB)
        PrintResult("Da & By", Da & By)
        PrintResult("Da & Sh", Da & Sh)
        PrintResult("Da & US", Da & US)
        PrintResult("Da & [In]", Da & [In])
        PrintResult("Da & UI", Da & UI)
        PrintResult("Da & Lo", Da & Lo)
        PrintResult("Da & UL", Da & UL)
        PrintResult("Da & De", Da & De)
        PrintResult("Da & Si", Da & Si)
        PrintResult("Da & [Do]", Da & [Do])
        PrintResult("Da & St", Da & St)
        PrintResult("Da & Ob", Da & Ob)
        PrintResult("Da & Tc", Da & Tc)
        PrintResult("Da & Da", Da & Da)
        PrintResult("Da & Ch", Da & Ch)
        PrintResult("Da & ChArray", Da & ChArray)

        PrintResult("Ch & Da", Ch & Da)
        PrintResult("ChArray & Da", ChArray & Da)
        PrintResult("BoFalse & Da", BoFalse & Da)
        PrintResult("BoTrue & Da", BoTrue & Da)
        PrintResult("SB & Da", SB & Da)
        PrintResult("By & Da", By & Da)
        PrintResult("Sh & Da", Sh & Da)
        PrintResult("US & Da", US & Da)
        PrintResult("[In] & Da", [In] & Da)
        PrintResult("UI & Da", UI & Da)
        PrintResult("Lo & Da", Lo & Da)
        PrintResult("UL & Da", UL & Da)
        PrintResult("De & Da", De & Da)
        PrintResult("Si & Da", Si & Da)
        PrintResult("[Do] & Da", [Do] & Da)
        PrintResult("St & Da", St & Da)
        PrintResult("Ob & Da", Ob & Da)
        PrintResult("Tc & Da", Tc & Da)
    End Sub

End Module
]]>
    </file>
</compilation>

                Dim expected = <![CDATA[[Da + St] String: [8:30:00 AM12]
[Da + Ob] Object: 8:30:00 AM-13
[Da + Da] String: [8:30:00 AM8:30:00 AM]
[Da + ChArray] String: [8:30:00 AM14]
[ChArray + Da] String: [148:30:00 AM]
[St + Da] String: [128:30:00 AM]
[Ob + Da] Object: -138:30:00 AM
[Da & BoFalse] String: [8:30:00 AMFalse]
[Da & BoTrue] String: [8:30:00 AMTrue]
[Da & SB] String: [8:30:00 AM-1]
[Da & By] String: [8:30:00 AM22]
[Da & Sh] String: [8:30:00 AM-3]
[Da & US] String: [8:30:00 AM24]
[Da & [In]] String: [8:30:00 AM-5]
[Da & UI] String: [8:30:00 AM26]
[Da & Lo] String: [8:30:00 AM-7]
[Da & UL] String: [8:30:00 AM28]
[Da & De] String: [8:30:00 AM-9]
[Da & Si] String: [8:30:00 AM10]
[Da & [Do]] String: [8:30:00 AM-11]
[Da & St] String: [8:30:00 AM12]
[Da & Ob] Object: 8:30:00 AM-13
[Da & Tc] String: [8:30:00 AM14]
[Da & Da] String: [8:30:00 AM8:30:00 AM]
[Da & Ch] String: [8:30:00 AMc]
[Da & ChArray] String: [8:30:00 AM14]
[Ch & Da] String: [c8:30:00 AM]
[ChArray & Da] String: [148:30:00 AM]
[BoFalse & Da] String: [False8:30:00 AM]
[BoTrue & Da] String: [True8:30:00 AM]
[SB & Da] String: [-18:30:00 AM]
[By & Da] String: [228:30:00 AM]
[Sh & Da] String: [-38:30:00 AM]
[US & Da] String: [248:30:00 AM]
[[In] & Da] String: [-58:30:00 AM]
[UI & Da] String: [268:30:00 AM]
[Lo & Da] String: [-78:30:00 AM]
[UL & Da] String: [288:30:00 AM]
[De & Da] String: [-98:30:00 AM]
[Si & Da] String: [108:30:00 AM]
[[Do] & Da] String: [-118:30:00 AM]
[St & Da] String: [128:30:00 AM]
[Ob & Da] Object: -138:30:00 AM
[Tc & Da] String: [148:30:00 AM]]]>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)
                Assert.True(compilation.Options.CheckOverflow)
                CompileAndVerify(compilation, expectedOutput:=expected)

                compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOverflowChecks(False))
                Assert.False(compilation.Options.CheckOverflow)
                CompileAndVerify(compilation, expectedOutput:=expected)

            Catch ex As Exception
                Assert.Null(ex)
            Finally
                System.Threading.Thread.CurrentThread.CurrentCulture = currCulture
            End Try
        End Sub

        <Fact>
        Public Sub Test2()

            Dim compilationDef =
<compilation name="VBBinaryOperators2">
    <file name="a.vb">
        <%= SemanticResourceUtil.BinaryOperatorsTestSource2 %>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation, SemanticResourceUtil.BinaryOperatorsTestBaseline2)

        End Sub

        <Fact>
        Public Sub Test30()

            Dim compilationDef =
<compilation name="VBBinaryOperators30">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()

        Dim St1 As String
        Dim St2 As String
        Dim Ob1 As Object
        Dim Ob2 As Object

        St1 = "a"
        St2 = "a"
        Ob1 = "a"
        Ob2 = "a"

        Console.WriteLine(St1 = St2)
        Console.WriteLine(Ob1 = Ob2)

        St1 = "a"
        St2 = "A"
        Ob1 = "a"
        Ob2 = "A"

        Console.WriteLine(St1 = St2)
        Console.WriteLine(Ob1 = Ob2)

        St1 = "a"
        St2 = "b"
        Ob1 = "a"
        Ob2 = "b"

        Console.WriteLine(St1 = St2)
        Console.WriteLine(Ob1 = Ob2)
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.False(compilation.Options.OptionCompareText)

            CompileAndVerify(compilation, <![CDATA[
True
True
False
False
False
False    
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionCompareText(True))

            Assert.True(compilation.Options.OptionCompareText)

            CompileAndVerify(compilation, <![CDATA[
True
True
True
True
False
False    
]]>)

        End Sub

        <Fact>
        Public Sub Test3()

            Dim compilationDef =
<compilation name="VBBinaryOperators3">
    <file name="a.vb">
        <%= SemanticResourceUtil.BinaryOperatorsTestSource3 %>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation, SemanticResourceUtil.BinaryOperatorsTestBaseline3)

        End Sub

        <Fact>
        Public Sub Test4()

            Dim compilationDef =
<compilation name="VBBinaryOperators4">
    <file name="a.vb">
        <%= SemanticResourceUtil.BinaryOperatorsTestSource4 %>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=SemanticResourceUtil.BinaryOperatorsTestBaseline4)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Test5()

            Dim compilationDef =
<compilation name="VBBinaryOperators52">
    <file name="lib.vb">
        <%= SemanticResourceUtil.PrintResultTestSource %>
    </file>
    <file name="a.vb">
        <%= SemanticResourceUtil.BinaryOperatorsTestSource5 %>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            Assert.True(compilation.Options.CheckOverflow)
            CompileAndVerify(compilation, expectedOutput:=SemanticResourceUtil.BinaryOperatorsTestBaseline5)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOverflowChecks(False))
            Assert.False(compilation.Options.CheckOverflow)
            CompileAndVerify(compilation, expectedOutput:=SemanticResourceUtil.BinaryOperatorsTestBaseline5)

        End Sub

        <ConditionalFact(GetType(WindowsOrLinuxOnly), Reason:="https://github.com/dotnet/roslyn/issues/77861")>
        Public Sub Test5_DateConst()
            ' test binary operator between Date const and another type data
            ' call ToString() on it defeat the purpose of these scenarios
            Dim currCulture = System.Threading.Thread.CurrentThread.CurrentCulture
            System.Threading.Thread.CurrentThread.CurrentCulture = New System.Globalization.CultureInfo("en-US", useUserOverride:=False)
            Try

                Dim compilationDef =
    <compilation name="VBBinaryOperators52">
        <file name="lib.vb">
            <%= SemanticResourceUtil.PrintResultTestSource %>
        </file>
        <file name="a.vb">
            <![CDATA[
Option Strict Off
Imports System

Module Module1

    Sub Main()

        PrintResult("#8:30:00 AM# + ""12""", #8:30:00 AM# + "12")
        PrintResult("#8:30:00 AM# + #8:30:00 AM#", #8:30:00 AM# + #8:30:00 AM#)
        PrintResult("""12"" + #8:30:00 AM#", "12" + #8:30:00 AM#)
        PrintResult("#8:30:00 AM# & False", #8:30:00 AM# & False)
        PrintResult("#8:30:00 AM# & True", #8:30:00 AM# & True)
        PrintResult("#8:30:00 AM# & System.SByte.MinValue", #8:30:00 AM# & System.SByte.MinValue)
        PrintResult("#8:30:00 AM# & System.Byte.MaxValue", #8:30:00 AM# & System.Byte.MaxValue)
        PrintResult("#8:30:00 AM# & -3S", #8:30:00 AM# & -3S)
        PrintResult("#8:30:00 AM# & 24US", #8:30:00 AM# & 24US)
        PrintResult("#8:30:00 AM# & -5I", #8:30:00 AM# & -5I)
        PrintResult("#8:30:00 AM# & 26UI", #8:30:00 AM# & 26UI)
        PrintResult("#8:30:00 AM# & -7L", #8:30:00 AM# & -7L)
        PrintResult("#8:30:00 AM# & 28UL", #8:30:00 AM# & 28UL)
        PrintResult("#8:30:00 AM# & -9D", #8:30:00 AM# & -9D)
        PrintResult("#8:30:00 AM# & 10.0F", #8:30:00 AM# & 10.0F)
        PrintResult("#8:30:00 AM# & -11.0R", #8:30:00 AM# & -11.0R)
        PrintResult("#8:30:00 AM# & ""12""", #8:30:00 AM# & "12")
        PrintResult("#8:30:00 AM# & TypeCode.Double", #8:30:00 AM# & TypeCode.Double)
        PrintResult("#8:30:00 AM# & #8:30:00 AM#", #8:30:00 AM# & #8:30:00 AM#)
        PrintResult("#8:30:00 AM# & ""c""c", #8:30:00 AM# & "c"c)


        PrintResult("""c""c & #8:30:00 AM#", "c"c & #8:30:00 AM#)
        PrintResult("False & #8:30:00 AM#", False & #8:30:00 AM#)
        PrintResult("True & #8:30:00 AM#", True & #8:30:00 AM#)
        PrintResult("System.SByte.MinValue & #8:30:00 AM#", System.SByte.MinValue & #8:30:00 AM#)
        PrintResult("System.Byte.MaxValue & #8:30:00 AM#", System.Byte.MaxValue & #8:30:00 AM#)
        PrintResult("-3S & #8:30:00 AM#", -3S & #8:30:00 AM#)
        PrintResult("24US & #8:30:00 AM#", 24US & #8:30:00 AM#)
        PrintResult("-5I & #8:30:00 AM#", -5I & #8:30:00 AM#)
        PrintResult("26UI & #8:30:00 AM#", 26UI & #8:30:00 AM#)
        PrintResult("-7L & #8:30:00 AM#", -7L & #8:30:00 AM#)
        PrintResult("28UL & #8:30:00 AM#", 28UL & #8:30:00 AM#)
        PrintResult("-9D & #8:30:00 AM#", -9D & #8:30:00 AM#)
        PrintResult("10.0F & #8:30:00 AM#", 10.0F & #8:30:00 AM#)
        PrintResult("-11.0R & #8:30:00 AM#", -11.0R & #8:30:00 AM#)
        PrintResult("""12"" & #8:30:00 AM#", "12" & #8:30:00 AM#)
        PrintResult("TypeCode.Double & #8:30:00 AM#", TypeCode.Double & #8:30:00 AM#)

    End Sub

End Module
]]>
        </file>
    </compilation>

                Dim expected = <![CDATA[[#8:30:00 AM# + "12"] String: [8:30:00 AM12]
[#8:30:00 AM# + #8:30:00 AM#] String: [8:30:00 AM8:30:00 AM]
["12" + #8:30:00 AM#] String: [128:30:00 AM]
[#8:30:00 AM# & False] String: [8:30:00 AMFalse]
[#8:30:00 AM# & True] String: [8:30:00 AMTrue]
[#8:30:00 AM# & System.SByte.MinValue] String: [8:30:00 AM-128]
[#8:30:00 AM# & System.Byte.MaxValue] String: [8:30:00 AM255]
[#8:30:00 AM# & -3S] String: [8:30:00 AM-3]
[#8:30:00 AM# & 24US] String: [8:30:00 AM24]
[#8:30:00 AM# & -5I] String: [8:30:00 AM-5]
[#8:30:00 AM# & 26UI] String: [8:30:00 AM26]
[#8:30:00 AM# & -7L] String: [8:30:00 AM-7]
[#8:30:00 AM# & 28UL] String: [8:30:00 AM28]
[#8:30:00 AM# & -9D] String: [8:30:00 AM-9]
[#8:30:00 AM# & 10.0F] String: [8:30:00 AM10]
[#8:30:00 AM# & -11.0R] String: [8:30:00 AM-11]
[#8:30:00 AM# & "12"] String: [8:30:00 AM12]
[#8:30:00 AM# & TypeCode.Double] String: [8:30:00 AM14]
[#8:30:00 AM# & #8:30:00 AM#] String: [8:30:00 AM8:30:00 AM]
[#8:30:00 AM# & "c"c] String: [8:30:00 AMc]
["c"c & #8:30:00 AM#] String: [c8:30:00 AM]
[False & #8:30:00 AM#] String: [False8:30:00 AM]
[True & #8:30:00 AM#] String: [True8:30:00 AM]
[System.SByte.MinValue & #8:30:00 AM#] String: [-1288:30:00 AM]
[System.Byte.MaxValue & #8:30:00 AM#] String: [2558:30:00 AM]
[-3S & #8:30:00 AM#] String: [-38:30:00 AM]
[24US & #8:30:00 AM#] String: [248:30:00 AM]
[-5I & #8:30:00 AM#] String: [-58:30:00 AM]
[26UI & #8:30:00 AM#] String: [268:30:00 AM]
[-7L & #8:30:00 AM#] String: [-78:30:00 AM]
[28UL & #8:30:00 AM#] String: [288:30:00 AM]
[-9D & #8:30:00 AM#] String: [-98:30:00 AM]
[10.0F & #8:30:00 AM#] String: [108:30:00 AM]
[-11.0R & #8:30:00 AM#] String: [-118:30:00 AM]
["12" & #8:30:00 AM#] String: [128:30:00 AM]
[TypeCode.Double & #8:30:00 AM#] String: [148:30:00 AM]
]]>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)
                Assert.True(compilation.Options.CheckOverflow)
                CompileAndVerify(compilation, expectedOutput:=expected)

                compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOverflowChecks(False))
                Assert.False(compilation.Options.CheckOverflow)
                CompileAndVerify(compilation, expectedOutput:=expected)

            Catch ex As Exception
                Assert.Null(ex)
            Finally
                System.Threading.Thread.CurrentThread.CurrentCulture = currCulture
            End Try

        End Sub

        <Fact>
        Public Sub Test7()

            Dim compilationDef =
<compilation name="VBBinaryOperators7">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()

        Dim ob As Object

        ob = System.SByte.MinValue + (System.SByte.MaxValue + System.SByte.MinValue)
        ob = System.Byte.MinValue - (System.Byte.MaxValue \ System.Byte.MaxValue)
        ob = System.Int16.MinValue - 1S
        ob = System.UInt16.MinValue - 1US
        ob = System.Int32.MinValue - 1I
        ob = System.UInt32.MinValue - 1UI
        ob = System.Int64.MinValue - 1L
        ob = System.UInt64.MinValue - 1UL
        ob = -79228162514264337593543950335D - 1D
        ob = System.SByte.MaxValue - (System.SByte.MaxValue + System.SByte.MinValue)
        ob = System.Byte.MaxValue + (System.Byte.MaxValue \ System.Byte.MaxValue)
        ob = System.Int16.MaxValue + 1S
        ob = System.UInt16.MaxValue + 1US
        ob = System.Int32.MaxValue + 1I
        ob = System.UInt32.MaxValue + 1UI
        ob = System.Int64.MaxValue + 1L
        ob = System.UInt64.MaxValue + 1UL
        ob = 79228162514264337593543950335D + 1D


        ob = (2I \ 0)
        ob = (1.5F \ 0)
        ob = (2.5R \ 0)
        ob = (3.5D \ 0)
        ob = (2I Mod 0)
        ob = (3.5D Mod 0)
        ob = (3.5D / Nothing)
        ob = (2I \ Nothing)
        ob = (1.5F \ Nothing)
        ob = (2.5R \ Nothing)
        ob = (3.5D \ Nothing)
        ob = (2I Mod Nothing)
        ob = (3.5D Mod Nothing)
        ob = (3.5D / 0)

        ob = System.UInt64.MaxValue * 2UL
        ob = System.Int64.MaxValue * 2L
        ob = System.Int64.MinValue * (-2L)
        ob = 3L * (System.Int64.MinValue \ 2L)
        ob = (System.Int64.MinValue \ 2L) * 3L

        ob = System.Int64.MinValue \ (-1L)

    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30439: Constant expression not representable in type 'SByte'.
        ob = System.SByte.MinValue + (System.SByte.MaxValue + System.SByte.MinValue)
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Byte'.
        ob = System.Byte.MinValue - (System.Byte.MaxValue \ System.Byte.MaxValue)
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Short'.
        ob = System.Int16.MinValue - 1S
             ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'UShort'.
        ob = System.UInt16.MinValue - 1US
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Integer'.
        ob = System.Int32.MinValue - 1I
             ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'UInteger'.
        ob = System.UInt32.MinValue - 1UI
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Long'.
        ob = System.Int64.MinValue - 1L
             ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'ULong'.
        ob = System.UInt64.MinValue - 1UL
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Decimal'.
        ob = -79228162514264337593543950335D - 1D
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'SByte'.
        ob = System.SByte.MaxValue - (System.SByte.MaxValue + System.SByte.MinValue)
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Byte'.
        ob = System.Byte.MaxValue + (System.Byte.MaxValue \ System.Byte.MaxValue)
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Short'.
        ob = System.Int16.MaxValue + 1S
             ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'UShort'.
        ob = System.UInt16.MaxValue + 1US
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Integer'.
        ob = System.Int32.MaxValue + 1I
             ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'UInteger'.
        ob = System.UInt32.MaxValue + 1UI
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Long'.
        ob = System.Int64.MaxValue + 1L
             ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'ULong'.
        ob = System.UInt64.MaxValue + 1UL
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Decimal'.
        ob = 79228162514264337593543950335D + 1D
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (2I \ 0)
              ~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (1.5F \ 0)
              ~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (2.5R \ 0)
              ~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (3.5D \ 0)
              ~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (2I Mod 0)
              ~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (3.5D Mod 0)
              ~~~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (3.5D / Nothing)
              ~~~~~~~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (2I \ Nothing)
              ~~~~~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (1.5F \ Nothing)
              ~~~~~~~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (2.5R \ Nothing)
              ~~~~~~~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (3.5D \ Nothing)
              ~~~~~~~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (2I Mod Nothing)
              ~~~~~~~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (3.5D Mod Nothing)
              ~~~~~~~~~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
        ob = (3.5D / 0)
              ~~~~~~~~
BC30439: Constant expression not representable in type 'ULong'.
        ob = System.UInt64.MaxValue * 2UL
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Long'.
        ob = System.Int64.MaxValue * 2L
             ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Long'.
        ob = System.Int64.MinValue * (-2L)
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Long'.
        ob = 3L * (System.Int64.MinValue \ 2L)
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Long'.
        ob = (System.Int64.MinValue \ 2L) * 3L
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Long'.
        ob = System.Int64.MinValue \ (-1L)
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test8()

            Dim compilationDef =
<compilation name="VBBinaryOperators8">
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()

        Dim Ob As Object = Nothing

        Ob = Ob-Ob
        Ob = Ob = Ob
        Ob = Ob &lt;&gt; Ob
        Ob = Ob &gt; Ob

    End Sub

End Module
    </file>
</compilation>


            Dim expected =
<expected>
BC42019: Operands of type Object used for operator '-'; runtime errors could occur.
        Ob = Ob-Ob
             ~~
BC42019: Operands of type Object used for operator '-'; runtime errors could occur.
        Ob = Ob-Ob
                ~~
BC42018: Operands of type Object used for operator '='; use the 'Is' operator to test object identity.
        Ob = Ob = Ob
             ~~
BC42018: Operands of type Object used for operator '='; use the 'Is' operator to test object identity.
        Ob = Ob = Ob
                  ~~
BC42032: Operands of type Object used for operator '&lt;&gt;'; use the 'IsNot' operator to test object identity.
        Ob = Ob &lt;&gt; Ob
             ~~
BC42032: Operands of type Object used for operator '&lt;&gt;'; use the 'IsNot' operator to test object identity.
        Ob = Ob &lt;&gt; Ob
                   ~~
BC42019: Operands of type Object used for operator '&gt;'; runtime errors could occur.
        Ob = Ob &gt; Ob
             ~~
BC42019: Operands of type Object used for operator '&gt;'; runtime errors could occur.
        Ob = Ob &gt; Ob
                  ~~
</expected>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Custom))
            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)
            CompilationUtils.AssertTheseDiagnostics(compilation, expected)

        End Sub

        <WorkItem(543387, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543387")>
        <Fact()>
        Public Sub Test9()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        If Nothing = Function() 5 Then 'BIND1:"Nothing = Function() 5"
            System.Console.WriteLine("Failed")
        Else
            System.Console.WriteLine("Succeeded")
        End If
    End Sub
End Module
    ]]></file>
</compilation>, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Succeeded
]]>)

            AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            Dim node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            Dim symbolInfo = model.GetSymbolInfo(node)
            Assert.Equal("Public Shared Overloads Operator =(d1 As System.MulticastDelegate, d2 As System.MulticastDelegate) As Boolean", symbolInfo.Symbol.ToDisplayString())
        End Sub

        <WorkItem(544620, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544620")>
        <Fact()>
        Public Sub Bug13088()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Module Program

    Public Const Z As Integer = Integer.MaxValue + 1 'BIND:"Public Const Z As Integer = Integer.MaxValue + 1"

    Sub Main()
    End Sub
End Module
    ]]></file>
    </compilation>)

            VerifyDiagnostics(compilation, Diagnostic(ERRID.ERR_ExpressionOverflow1, "Integer.MaxValue + 1").WithArguments("Integer"))

            Dim symbol = compilation.GlobalNamespace.GetTypeMembers("Program").Single.GetMembers("Z").Single
            Assert.False(DirectCast(symbol, FieldSymbol).HasConstantValue)
        End Sub

        <Fact(), WorkItem(531531, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531531")>
        <CompilerTrait(CompilerFeature.IOperation)>
        Public Sub Bug18257()
            Dim source =
<compilation name="ErrorHandling">
    <file name="a.vb">
        <![CDATA[
Option Strict On

Public Class TestState
    Shared Function IsImmediateWindow1(line As Integer?) As String
        Dim s = "Expected: " & line & "."

        Return s
    End Function

    Shared Function IsImmediateWindow2(line As Integer) As String
        Dim s = "Expected: " & line & "."

        Return s
    End Function
End Class

Module Module1
    Sub Main()
        System.Console.WriteLine(TestState.IsImmediateWindow1(Nothing))
        System.Console.WriteLine(TestState.IsImmediateWindow2(Nothing))
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
Expected: .
Expected: 0.
]]>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of LocalDeclarationStatementSyntax)().First()

            Assert.Equal("Dim s = ""Expected: "" & line & "".""", node.ToString())

            compilation.VerifyOperationTree(node.Declarators.Last.Initializer.Value, expectedOperationTree:=
            <![CDATA[
IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.Binary, Type: System.String) (Syntax: '"Expected:  ...  line & "."')
  Left: 
    IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.Binary, Type: System.String) (Syntax: '"Expected: " & line')
      Left: 
        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Expected: ") (Syntax: '"Expected: "')
      Right: 
        ICoalesceOperation (OperationKind.Coalesce, Type: System.String, IsImplicit) (Syntax: 'line')
          Expression: 
            IParameterReferenceOperation: line (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'line')
          ValueConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            (NarrowingString)
          WhenNull: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: null, IsImplicit) (Syntax: 'line')
  Right: 
    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ".") (Syntax: '"."')
]]>.Value)
        End Sub

        <ConditionalFact(GetType(NoIOperationValidation))>
        Public Sub IntrinsicSymbols()
            Dim operators() As BinaryOperatorKind =
            {
            BinaryOperatorKind.Add,
            BinaryOperatorKind.Concatenate,
            BinaryOperatorKind.Like,
            BinaryOperatorKind.Equals,
            BinaryOperatorKind.NotEquals,
            BinaryOperatorKind.LessThanOrEqual,
            BinaryOperatorKind.GreaterThanOrEqual,
            BinaryOperatorKind.LessThan,
            BinaryOperatorKind.GreaterThan,
            BinaryOperatorKind.Subtract,
            BinaryOperatorKind.Multiply,
            BinaryOperatorKind.Power,
            BinaryOperatorKind.Divide,
            BinaryOperatorKind.Modulo,
            BinaryOperatorKind.IntegerDivide,
            BinaryOperatorKind.LeftShift,
            BinaryOperatorKind.RightShift,
            BinaryOperatorKind.Xor,
            BinaryOperatorKind.Or,
            BinaryOperatorKind.And,
            BinaryOperatorKind.OrElse,
            BinaryOperatorKind.AndAlso,
            BinaryOperatorKind.Is,
            BinaryOperatorKind.IsNot
            }

            Dim opTokens = (From op In operators Select SyntaxFacts.GetText(OverloadResolution.GetOperatorTokenKind(op))).ToArray()

            Dim typeNames() As String =
                {
                "System.Object",
                "System.String",
                "System.Double",
                "System.SByte",
                "System.Int16",
                "System.Int32",
                "System.Int64",
                "System.Decimal",
                "System.Single",
                "System.Byte",
                "System.UInt16",
                "System.UInt32",
                "System.UInt64",
                "System.Boolean",
                "System.Char",
                "System.DateTime",
                "System.TypeCode",
                "System.StringComparison",
                "System.Guid",
                "System.Char()"
                }

            Dim builder As New System.Text.StringBuilder
            Dim n As Integer = 0

            For Each arg1 In typeNames
                For Each arg2 In typeNames
                    n += 1
                    builder.AppendFormat(
"Sub Test{2}(x1 as {0}, y1 As {1}, x2 as System.Nullable(Of {0}), y2 As System.Nullable(Of {1}))" & vbCrLf, arg1, arg2, n)

                    Dim k As Integer = 0
                    For Each opToken In opTokens
                        builder.AppendFormat(
"    Dim z{0}_1 = x1 {1} y1" & vbCrLf &
"    Dim z{0}_2 = x2 {1} y2" & vbCrLf &
"    Dim z{0}_3 = x2 {1} y1" & vbCrLf &
"    Dim z{0}_4 = x1 {1} y2" & vbCrLf &
"    If x1 {1} y1" & vbCrLf &
"    End If" & vbCrLf &
"    If x2 {1} y2" & vbCrLf &
"    End If" & vbCrLf &
"    If x2 {1} y1" & vbCrLf &
"    End If" & vbCrLf &
"    If x1 {1} y2" & vbCrLf &
"    End If" & vbCrLf,
                                             k, opToken)
                        k += 1
                    Next

                    builder.Append(
"End Sub" & vbCrLf)
                Next
            Next

            Dim source =
<compilation>
    <file name="a.vb">
Class Module1
<%= New System.Xml.Linq.XCData(builder.ToString()) %>
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithOverflowChecks(True))

            Dim types(typeNames.Length - 1) As NamedTypeSymbol

            For i As Integer = 0 To typeNames.Length - 2
                types(i) = compilation.GetTypeByMetadataName(typeNames(i))
            Next

            Assert.Null(types(types.Length - 1))
            types(types.Length - 1) = compilation.GetSpecialType(SpecialType.System_String)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim nodes = (From node In tree.GetRoot().DescendantNodes()
                         Select node = TryCast(node, BinaryExpressionSyntax)
                         Where node IsNot Nothing).ToArray()

            n = 0
            For Each leftType In types
                For Each rightType In types
                    For Each op In operators
                        TestIntrinsicSymbol(
                            op,
                            leftType,
                            rightType,
                            compilation,
                            semanticModel,
                            nodes(n),
                            nodes(n + 1),
                            nodes(n + 2),
                            nodes(n + 3),
                            nodes(n + 4),
                            nodes(n + 5),
                            nodes(n + 6),
                            nodes(n + 7))
                        n += 8
                    Next
                Next
            Next

            Assert.Equal(n, nodes.Length)

        End Sub

        Private Sub TestIntrinsicSymbol(
            op As BinaryOperatorKind,
            leftType As TypeSymbol,
            rightType As TypeSymbol,
            compilation As VisualBasicCompilation,
            semanticModel As SemanticModel,
            node1 As BinaryExpressionSyntax,
            node2 As BinaryExpressionSyntax,
            node3 As BinaryExpressionSyntax,
            node4 As BinaryExpressionSyntax,
            node5 As BinaryExpressionSyntax,
            node6 As BinaryExpressionSyntax,
            node7 As BinaryExpressionSyntax,
            node8 As BinaryExpressionSyntax
        )

            Dim info1 As SymbolInfo = semanticModel.GetSymbolInfo(node1)

            If (leftType.SpecialType <> SpecialType.System_Object AndAlso
                   Not leftType.IsIntrinsicType()) OrElse
               (rightType.SpecialType <> SpecialType.System_Object AndAlso
                   Not rightType.IsIntrinsicType()) OrElse
               (leftType.IsDateTimeType() AndAlso rightType.IsDateTimeType() AndAlso
                   op = BinaryOperatorKind.Subtract) Then ' Let (Date - Date) use operator overloading.

                If info1.Symbol IsNot Nothing OrElse info1.CandidateSymbols.Length = 0 Then
                    Assert.Equal(CandidateReason.None, info1.CandidateReason)
                Else
                    Assert.Equal(CandidateReason.OverloadResolutionFailure, info1.CandidateReason)
                End If
            Else
                Assert.Equal(CandidateReason.None, info1.CandidateReason)
                Assert.Equal(0, info1.CandidateSymbols.Length)
            End If

            Dim symbol1 = DirectCast(info1.Symbol, MethodSymbol)
            Dim symbol2 = semanticModel.GetSymbolInfo(node2).Symbol
            Dim symbol3 = semanticModel.GetSymbolInfo(node3).Symbol
            Dim symbol4 = semanticModel.GetSymbolInfo(node4).Symbol
            Dim symbol5 = DirectCast(semanticModel.GetSymbolInfo(node5).Symbol, MethodSymbol)
            Dim symbol6 = semanticModel.GetSymbolInfo(node6).Symbol
            Dim symbol7 = semanticModel.GetSymbolInfo(node7).Symbol
            Dim symbol8 = semanticModel.GetSymbolInfo(node8).Symbol

            Assert.Equal(symbol1, symbol5)
            Assert.Equal(symbol2, symbol6)
            Assert.Equal(symbol3, symbol7)
            Assert.Equal(symbol4, symbol8)

            If symbol1 IsNot Nothing AndAlso symbol1.IsImplicitlyDeclared Then
                Assert.NotSame(symbol1, symbol5)
                Assert.Equal(symbol1.GetHashCode(), symbol5.GetHashCode())

                For i As Integer = 0 To 1
                    Assert.Equal(symbol1.Parameters(i), symbol5.Parameters(i))
                    Assert.Equal(symbol1.Parameters(i).GetHashCode(), symbol5.Parameters(i).GetHashCode())
                Next

                Assert.NotEqual(symbol1.Parameters(0), symbol5.Parameters(1))
            End If

            Select Case op
                Case BinaryOperatorKind.AndAlso, BinaryOperatorKind.OrElse, BinaryOperatorKind.Is, BinaryOperatorKind.IsNot
                    Assert.Null(symbol1)
                    Assert.Null(symbol2)
                    Assert.Null(symbol3)
                    Assert.Null(symbol4)
                    Return
            End Select

            Dim leftSpecial As SpecialType = leftType.GetEnumUnderlyingTypeOrSelf().SpecialType
            Dim rightSpecial As SpecialType = rightType.GetEnumUnderlyingTypeOrSelf().SpecialType

            Dim resultType As SpecialType = OverloadResolution.ResolveNotLiftedIntrinsicBinaryOperator(op, leftSpecial, rightSpecial)

            Dim userDefined As MethodSymbol = Nothing

            If resultType = SpecialType.None AndAlso
               (leftSpecial = SpecialType.None OrElse rightSpecial = SpecialType.None OrElse
                (op = BinaryOperatorKind.Subtract AndAlso leftSpecial = SpecialType.System_DateTime AndAlso rightSpecial = SpecialType.System_DateTime)) Then

                If leftSpecial = SpecialType.System_Object OrElse rightSpecial = SpecialType.System_Object OrElse TypeSymbol.Equals(leftType, rightType, TypeCompareKind.ConsiderEverything) Then
                    If leftSpecial = SpecialType.System_Object OrElse rightSpecial = SpecialType.System_Object Then
                        resultType = SpecialType.System_Object
                    End If

                    Dim nonSpecialType = If(leftSpecial = SpecialType.System_Object, rightType, leftType)

                    For Each m In nonSpecialType.GetMembers(OverloadResolution.TryGetOperatorName(op))
                        If m.Kind = SymbolKind.Method Then
                            Dim method = DirectCast(m, MethodSymbol)
                            If method.MethodKind = MethodKind.UserDefinedOperator AndAlso
                               method.ParameterCount = 2 AndAlso
                               TypeSymbol.Equals(method.Parameters(0).Type, nonSpecialType, TypeCompareKind.ConsiderEverything) AndAlso
                               TypeSymbol.Equals(method.Parameters(1).Type, nonSpecialType, TypeCompareKind.ConsiderEverything) Then
                                userDefined = method
                                resultType = SpecialType.None
                            End If
                        End If
                    Next

                Else
                    Assert.Null(symbol1)
                    Assert.Null(symbol2)
                    Assert.Null(symbol3)
                    Assert.Null(symbol4)
                    Return
                End If
            End If

            If resultType = SpecialType.None Then

                If userDefined IsNot Nothing Then
                    Assert.False(userDefined.IsImplicitlyDeclared)

                    Assert.Same(userDefined, symbol1)

                    If leftType.IsValueType Then
                        If rightType.IsValueType Then
                            Assert.Same(userDefined, symbol2)
                            Assert.Same(userDefined, symbol3)
                            Assert.Same(userDefined, symbol4)
                            Return
                        Else
                            Assert.Null(symbol2)
                            Assert.Same(userDefined, symbol3)
                            Assert.Null(symbol4)
                            Return
                        End If
                    ElseIf rightType.IsValueType Then
                        Assert.Null(symbol2)
                        Assert.Null(symbol3)
                        Assert.Same(userDefined, symbol4)
                        Return
                    Else
                        Assert.Null(symbol2)
                        Assert.Null(symbol3)
                        Assert.Null(symbol4)
                        Return
                    End If
                End If

                Assert.Null(symbol1)
                Assert.Null(symbol2)
                Assert.Null(symbol3)
                Assert.Null(symbol4)
                Return
            End If

            Assert.NotNull(symbol1)

            Dim containerName As String = compilation.GetSpecialType(resultType).ToTestDisplayString()
            Dim rightName As String = containerName
            Dim returnName As String = containerName

            Select Case op

                Case BinaryOperatorKind.Equals,
                     BinaryOperatorKind.NotEquals,
                     BinaryOperatorKind.LessThanOrEqual,
                     BinaryOperatorKind.GreaterThanOrEqual,
                     BinaryOperatorKind.LessThan,
                     BinaryOperatorKind.GreaterThan,
                     BinaryOperatorKind.Like

                    If resultType <> SpecialType.System_Object Then
                        returnName = compilation.GetSpecialType(SpecialType.System_Boolean).ToTestDisplayString()
                    End If

                Case BinaryOperatorKind.LeftShift, BinaryOperatorKind.RightShift
                    If resultType <> SpecialType.System_Object Then
                        rightName = compilation.GetSpecialType(SpecialType.System_Int32).ToTestDisplayString()
                    End If

                Case BinaryOperatorKind.Xor, BinaryOperatorKind.And, BinaryOperatorKind.Or
                    If leftType.IsEnumType() AndAlso TypeSymbol.Equals(leftType, rightType, TypeCompareKind.ConsiderEverything) Then
                        containerName = leftType.ToTestDisplayString()
                        rightName = containerName
                        returnName = containerName
                    End If
            End Select

            Assert.Equal(String.Format("Function {0}.{1}(left As {0}, right As {2}) As {3}",
                                       containerName,
                                       OverloadResolution.TryGetOperatorName(
                                           If(op = BinaryOperatorKind.Add AndAlso resultType = SpecialType.System_String,
                                              BinaryOperatorKind.Concatenate,
                                              op)),
                                       rightName,
                                       returnName),
                         symbol1.ToTestDisplayString())

            Assert.Equal(MethodKind.BuiltinOperator, symbol1.MethodKind)
            Assert.True(symbol1.IsImplicitlyDeclared)

            Assert.Equal((op = BinaryOperatorKind.Multiply OrElse
                          op = BinaryOperatorKind.Add OrElse
                          op = BinaryOperatorKind.Subtract OrElse
                          op = BinaryOperatorKind.IntegerDivide) AndAlso
                            symbol1.ContainingType.IsIntegralType(),
                         symbol1.IsCheckedBuiltin)
            Assert.False(symbol1.IsGenericMethod)
            Assert.False(symbol1.IsExtensionMethod)
            Assert.False(symbol1.IsExternalMethod)
            Assert.False(symbol1.CanBeReferencedByName)
            Assert.Null(symbol1.DeclaringCompilation)
            Assert.Equal(symbol1.Name, symbol1.MetadataName)

            Assert.Same(symbol1.ContainingSymbol, symbol1.Parameters(0).Type)

            Dim match As Integer = 0
            If TypeSymbol.Equals(symbol1.ContainingType, symbol1.ReturnType, TypeCompareKind.ConsiderEverything) Then
                match += 1
            End If

            If TypeSymbol.Equals(symbol1.ContainingType, symbol1.Parameters(0).Type, TypeCompareKind.ConsiderEverything) Then
                match += 1
            End If

            If TypeSymbol.Equals(symbol1.ContainingType, symbol1.Parameters(1).Type, TypeCompareKind.ConsiderEverything) Then
                match += 1
            End If

            Assert.True(match >= 2)

            Assert.Equal(0, symbol1.Locations.Length)
            Assert.Null(symbol1.GetDocumentationCommentId)
            Assert.Equal("", symbol1.GetDocumentationCommentXml)

            Assert.True(symbol1.HasSpecialName)
            Assert.True(symbol1.IsShared)
            Assert.Equal(Accessibility.Public, symbol1.DeclaredAccessibility)
            Assert.False(symbol1.IsOverloads)
            Assert.False(symbol1.IsOverrides)
            Assert.False(symbol1.IsOverridable)
            Assert.False(symbol1.IsMustOverride)
            Assert.False(symbol1.IsNotOverridable)
            Assert.Equal(2, symbol1.ParameterCount)
            Assert.Equal(0, symbol1.Parameters(0).Ordinal)
            Assert.Equal(1, symbol1.Parameters(1).Ordinal)

            Dim otherSymbol = DirectCast(semanticModel.GetSymbolInfo(node1).Symbol, MethodSymbol)
            Assert.Equal(symbol1, otherSymbol)

            If leftType.IsValueType Then
                If rightType.IsValueType Then
                    Assert.Equal(symbol1, symbol2)
                    Assert.Equal(symbol1, symbol3)
                    Assert.Equal(symbol1, symbol4)
                    Return
                Else
                    Assert.Null(symbol2)
                    Assert.Equal(symbol1, symbol3)
                    Assert.Null(symbol4)
                    Return
                End If
            ElseIf rightType.IsValueType Then
                Assert.Null(symbol2)
                Assert.Null(symbol3)
                Assert.Equal(symbol1, symbol4)
                Return
            End If

            Assert.Null(symbol2)
            Assert.Null(symbol3)
            Assert.Null(symbol4)
        End Sub

        <Fact()>
        Public Sub CheckedIntrinsicSymbols()


            Dim source =
<compilation>
    <file name="a.vb">
Class Module1
    Sub Test(x as Integer, y as Integer)
        Dim z1 = x + y
        Dim z2 = x - y
        Dim z3 = x * y
        Dim z4 = x \ y
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithOverflowChecks(False))

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim nodes = (From node In tree.GetRoot().DescendantNodes()
                         Select node = TryCast(node, BinaryExpressionSyntax)
                         Where node IsNot Nothing).ToArray()

            Dim builder1 = ArrayBuilder(Of MethodSymbol).GetInstance()
            For Each node In nodes
                Dim symbol = DirectCast(semanticModel.GetSymbolInfo(node).Symbol, MethodSymbol)
                Assert.False(symbol.IsCheckedBuiltin)
                builder1.Add(symbol)
            Next

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOverflowChecks(True))
            semanticModel = compilation.GetSemanticModel(tree)

            Dim builder2 = ArrayBuilder(Of MethodSymbol).GetInstance()
            For Each node In nodes
                Dim symbol = DirectCast(semanticModel.GetSymbolInfo(node).Symbol, MethodSymbol)
                Assert.True(symbol.IsCheckedBuiltin)
                builder2.Add(symbol)
            Next

            For i As Integer = 0 To builder1.Count - 1
                Assert.NotEqual(builder1(i), builder2(i))
            Next

            builder1.Free()
            builder2.Free()
        End Sub

        <Fact(), WorkItem(721565, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/721565")>
        Public Sub Bug721565()

            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Class Module1
    Sub Test(x as TestStr?, y as Integer?)
        Dim z1 = (x Is Nothing)
        Dim z2 = (Nothing Is x)
        Dim z3 = (x IsNot Nothing)
        Dim z4 = (Nothing IsNot x)
        Dim z5 = (x = Nothing)
        Dim z6 = (Nothing = x)
        Dim z7 = (x <> Nothing)
        Dim z8 = (Nothing <> x)

        Dim z11 = (y Is Nothing)
        Dim z12 = (Nothing Is y)
        Dim z13 = (y IsNot Nothing)
        Dim z14 = (Nothing IsNot y)
        Dim z15 = (y = Nothing)
        Dim z16 = (Nothing = y)
        Dim z17 = (y <> Nothing)
        Dim z18 = (Nothing <> y)
    End Sub
End Class

Structure TestStr
End Structure
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30452: Operator '=' is not defined for types 'TestStr?' and 'TestStr?'.
        Dim z5 = (x = Nothing)
                  ~~~~~~~~~~~
BC30452: Operator '=' is not defined for types 'TestStr?' and 'TestStr?'.
        Dim z6 = (Nothing = x)
                  ~~~~~~~~~~~
BC30452: Operator '<>' is not defined for types 'TestStr?' and 'TestStr?'.
        Dim z7 = (x <> Nothing)
                  ~~~~~~~~~~~~
BC30452: Operator '<>' is not defined for types 'TestStr?' and 'TestStr?'.
        Dim z8 = (Nothing <> x)
                  ~~~~~~~~~~~~
BC42037: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is null consider using 'Is Nothing'.
        Dim z15 = (y = Nothing)
                   ~~~~~~~~~~~
BC42037: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is null consider using 'Is Nothing'.
        Dim z16 = (Nothing = y)
                   ~~~~~~~~~~~
BC42038: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is not null consider using 'IsNot Nothing'.
        Dim z17 = (y <> Nothing)
                   ~~~~~~~~~~~~
BC42038: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is not null consider using 'IsNot Nothing'.
        Dim z18 = (Nothing <> y)
                   ~~~~~~~~~~~~
]]></expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim nodes = (From node In tree.GetRoot().DescendantNodes()
                         Select node = TryCast(node, BinaryExpressionSyntax)
                         Where node IsNot Nothing).ToArray()

            Assert.Equal(16, nodes.Length)

            For i As Integer = 0 To nodes.Length - 1
                Dim symbol = semanticModel.GetSymbolInfo(nodes(i)).Symbol

                Select Case i
                    Case 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11
                        Assert.Null(symbol)
                    Case 12, 13
                        Assert.Equal("Function System.Int32.op_Equality(left As System.Int32, right As System.Int32) As System.Boolean", symbol.ToTestDisplayString())
                    Case 14, 15
                        Assert.Equal("Function System.Int32.op_Inequality(left As System.Int32, right As System.Int32) As System.Boolean", symbol.ToTestDisplayString())
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(i)
                End Select
            Next
        End Sub

        <ConditionalFact(GetType(NoIOperationValidation))>
        <WorkItem(43019, "https://github.com/dotnet/roslyn/issues/43019"), WorkItem(529600, "DevDiv"), WorkItem(37572, "https://github.com/dotnet/roslyn/issues/37572")>
        Public Sub Bug529600()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module M
    Sub Main()
    End Sub

    Const c0 = "<%= New String("0"c, 65000) %>"

    Const C1=C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + C0 + 
             C0

    Const C2=C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + C1 + 
             C1

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilation(compilationDef)

            Dim err = compilation.GetDiagnostics().Single()

            Assert.Equal(ERRID.ERR_ConstantStringTooLong, err.Code)
            Assert.Equal("Length of String constant resulting from concatenation exceeds System.Int32.MaxValue.  Try splitting the string into multiple constants.", err.GetMessage(EnsureEnglishUICulture.PreferredOrNull))

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim fieldInitializerOperations = tree.GetRoot().DescendantNodes().OfType(Of VariableDeclaratorSyntax)().
                Select(Function(v) v.Initializer.Value).
                Select(Function(i) model.GetOperation(i))

            Dim numChildren = 0

            For Each iop in fieldInitializerOperations
                EnumerateChildren(iop, numChildren)
            Next

            Assert.Equal(1203, numChildren)
        End Sub

        Private Sub EnumerateChildren(iop As IOperation, ByRef numChildren as Integer)
            numChildren += 1
            Assert.NotNull(iop)
            For Each child in iop.Children
                EnumerateChildren(child, numChildren)
            Next
        End Sub

        <ConditionalFact(GetType(NoIOperationValidation)), WorkItem(43019, "https://github.com/dotnet/roslyn/issues/43019"), WorkItem(37572, "https://github.com/dotnet/roslyn/issues/37572")>
        Public Sub TestLargeStringConcatenation()

            Dim mid = New StringBuilder()
            For i As Integer = 0 To 4999
                mid.Append("""Lorem ipsum dolor sit amet"" + "", consectetur adipiscing elit, sed"" + "" do eiusmod tempor incididunt"" + "" ut labore et dolore magna aliqua. "" +" + vbCrLf)
            Next
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module M
    Sub Main()
        Dim s As String = "BEGIN "+
        <%= mid.ToString() %> "END"
        System.Console.WriteLine(System.Linq.Enumerable.Sum(s, Function(c As Char) System.Convert.ToInt32(c)))
    End Sub
End Module
    </file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilation(compilationDef, options:=TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics()
            CompileAndVerify(compilation, expectedOutput:="58430604")

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim initializer = tree.GetRoot().DescendantNodes().OfType(Of VariableDeclaratorSyntax).Single().Initializer.Value
            Dim literalOperation = model.GetOperation(initializer)

            Dim stringTextBuilder As New StringBuilder()
            stringTextBuilder.Append("BEGIN ")
            For i = 0 To 4999
                stringTextBuilder.Append("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. ")
            Next
            stringTextBuilder.Append("END")

            Assert.Equal(stringTextBuilder.ToString(), literalOperation.ConstantValue.Value)
        End Sub

    End Class

End Namespace
