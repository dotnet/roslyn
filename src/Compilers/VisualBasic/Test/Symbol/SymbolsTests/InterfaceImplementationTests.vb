' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class InterfaceImplementationTests
        Inherits BasicTestBase

        <Theory>
        <CombinatorialData>
        <WorkItem(46494, "https://github.com/dotnet/roslyn/issues/46494")>
        Public Sub ExplicitImplementationInBaseType_01(useCompilationReference As Boolean)
            Dim source0 =
"Public Structure S(Of T)
End Structure
Public Interface I
    Function F() As S(Of (X As Integer, Y As Integer))
End Interface"
            Dim source1 =
"Public Class A(Of T)
    Implements I
    Public Function F() As S(Of T)
        Return Nothing
    End Function
    Private Function I_F() As S(Of (X As Integer, Y As Integer)) Implements I.F
        Return Nothing
    End Function
End Class"
            Dim source2 =
"Class B(Of T)
    Inherits A(Of T)
    Implements I
End Class"
            Dim source3 =
"Class Program
    Shared Sub Main()
        Dim i As I = New B(Of String)()
        Dim o = i.F()
        System.Console.WriteLine(o)
    End Sub
End Class"
            ExplicitImplementationInBaseType(useCompilationReference, source0, source1, source2, source3, "B", "I.F", "S`1[System.ValueTuple`2[System.Int32,System.Int32]]", "Function A(Of T).I_F() As S(Of (X As System.Int32, Y As System.Int32))")
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem(46494, "https://github.com/dotnet/roslyn/issues/46494")>
        Public Sub ExplicitImplementationInBaseType_02(useCompilationReference As Boolean)
            Dim source0 =
"Public Structure S(Of T)
End Structure
Public Interface I
    Sub F(s As S(Of (X As Integer, Y As Integer)))
End Interface"
            Dim source1 =
"Public Class A(Of T)
    Implements I
    Public Sub F(s As S(Of T))
    End Sub
    Private Sub I_F(s As S(Of (X As Integer, Y As Integer))) Implements I.F
    End Sub
End Class"
            Dim source2 =
"Class B(Of T)
    Inherits A(Of T)
    Implements I
End Class"
            Dim source3 =
"Class Program
    Shared Sub Main()
        Dim i As I = New B(Of String)()
        i.F(Nothing)
        System.Console.WriteLine(1)
    End Sub
End Class"
            ExplicitImplementationInBaseType(useCompilationReference, source0, source1, source2, source3, "B", "I.F", "1", "Sub A(Of T).I_F(s As S(Of (X As System.Int32, Y As System.Int32)))")
        End Sub

        Private Sub ExplicitImplementationInBaseType(
            useCompilationReference As Boolean,
            source0 As String,
            source1 As String,
            source2 As String,
            source3 As String,
            derivedTypeName As String,
            interfaceMemberName As String,
            expectedOutput As String,
            expectedImplementingMember As String)

            Dim comp = CreateCompilation(source0)
            Dim ref0 = AsReference(comp, useCompilationReference)

            comp = CreateCompilation(source1, references:={ref0})
            Dim ref1 = AsReference(comp, useCompilationReference)

            comp = CreateCompilation({source2, source3}, references:={ref0, ref1}, options:=TestOptions.ReleaseExe)
            CompileAndVerify(comp, expectedOutput:=expectedOutput)

            Dim derivedType = comp.GetMember(Of SourceNamedTypeSymbol)(derivedTypeName)
            Dim interfaceMember = comp.GetMember(Of MethodSymbol)(interfaceMemberName)
            Dim implementingMember = derivedType.FindImplementationForInterfaceMember(interfaceMember)
            Assert.Equal(expectedImplementingMember, implementingMember.ToTestDisplayString())
        End Sub

        <Fact()>
        <WorkItem(50713, "https://github.com/dotnet/roslyn/issues/50713")>
        Public Sub Issue50713_1()
            Dim vbSource1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface I1
    Sub M()
End Interface

Interface I2
    Inherits I1
    Overloads Sub M()
End Interface
]]>
                    </file>
                </compilation>

            Dim compilation1 = CreateCompilation(vbSource1, options:=TestOptions.ReleaseDll)
            compilation1.AssertNoDiagnostics()

            Dim i1M = compilation1.GetMember("I1.M")
            Dim i2 = compilation1.GetMember(Of NamedTypeSymbol)("I2")
            Assert.Null(i2.FindImplementationForInterfaceMember(i1M))
        End Sub

        <Fact()>
        <WorkItem(50713, "https://github.com/dotnet/roslyn/issues/50713")>
        Public Sub Issue50713_2()
            Dim vbSource0 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface I1
    Sub M()
End Interface

Interface I2
    Inherits I1
    Overloads Sub M()
End Interface
]]>
                    </file>
                </compilation>

            Dim compilation0 = CreateCompilation(vbSource0, options:=TestOptions.ReleaseDll)

            Dim vbSource1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
]]>
                    </file>
                </compilation>

            Dim compilation1 = CreateCompilation(vbSource1, options:=TestOptions.ReleaseDll, references:={compilation0.EmitToImageReference()})

            Dim i1M = compilation1.GetMember("I1.M")
            Dim i2 = compilation1.GetMember(Of NamedTypeSymbol)("I2")
            Assert.Null(i2.FindImplementationForInterfaceMember(i1M))
        End Sub

        <Fact()>
        <WorkItem(50713, "https://github.com/dotnet/roslyn/issues/50713")>
        Public Sub Issue50713_3()
            Dim vbSource1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface I1
    Sub M()
End Interface

Interface I2
    Inherits I1
    Shadows Sub M()
End Interface
]]>
                    </file>
                </compilation>

            Dim compilation1 = CreateCompilation(vbSource1, options:=TestOptions.ReleaseDll)
            compilation1.AssertNoDiagnostics()

            Dim i1M = compilation1.GetMember("I1.M")
            Dim i2 = compilation1.GetMember(Of NamedTypeSymbol)("I2")
            Assert.Null(i2.FindImplementationForInterfaceMember(i1M))
        End Sub

        <Fact()>
        <WorkItem(50713, "https://github.com/dotnet/roslyn/issues/50713")>
        Public Sub Issue50713_4()
            Dim vbSource0 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface I1
    Sub M()
End Interface

Interface I2
    Inherits I1
    Shadows Sub M()
End Interface
]]>
                    </file>
                </compilation>

            Dim compilation0 = CreateCompilation(vbSource0, options:=TestOptions.ReleaseDll)

            Dim vbSource1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
]]>
                    </file>
                </compilation>

            Dim compilation1 = CreateCompilation(vbSource1, options:=TestOptions.ReleaseDll, references:={compilation0.EmitToImageReference()})

            Dim i1M = compilation1.GetMember("I1.M")
            Dim i2 = compilation1.GetMember(Of NamedTypeSymbol)("I2")
            Assert.Null(i2.FindImplementationForInterfaceMember(i1M))
        End Sub

    End Class

End Namespace
