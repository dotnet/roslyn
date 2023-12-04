' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SyntaxFactoryTests

        <Fact>
        Public Sub SyntaxTree()
            Dim text = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit(), encoding:=Nothing).GetText()
            Assert.Null(text.Encoding)
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm)
        End Sub

        <Fact>
        Public Sub SyntaxTreeFromNode()
            Dim text = SyntaxFactory.CompilationUnit().SyntaxTree.GetText()
            Assert.Null(text.Encoding)
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm)
        End Sub

        <Fact>
        Public Sub TestSpacingOnNullableDatetimeType()
            Dim node =
                SyntaxFactory.CompilationUnit().WithMembers(
                    SyntaxFactory.List(Of Syntax.StatementSyntax)(
                    {
                        SyntaxFactory.ClassBlock(SyntaxFactory.ClassStatement("C")).WithMembers(
                            SyntaxFactory.List(Of Syntax.StatementSyntax)(
                            {
                                SyntaxFactory.PropertyStatement("P").WithAsClause(
                                    SyntaxFactory.SimpleAsClause(
                                        SyntaxFactory.NullableType(
                                            SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(SyntaxKind.IntegerKeyword)))))
                            }))
                    })).NormalizeWhitespace()

            Dim expected = "Class C" & vbCrLf & vbCrLf & "    Property P As Integer?" & vbCrLf & "End Class" & vbCrLf
            Assert.Equal(expected, node.ToFullString())
        End Sub

        <Fact>
        <WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")>
        <WorkItem(720708, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720708")>
        Public Sub TestLiteralDefaultStringValues()

            ' string
            CheckLiteralToString("A", """A""")
            CheckLiteralToString(ChrW(7).ToString(), "ChrW(7)")
            CheckLiteralToString(ChrW(10).ToString(), "vbLf")

            ' char
            CheckLiteralToString("A"c, """A""c")
            CheckLiteralToString(ChrW(7), "ChrW(7)")
            CheckLiteralToString(ChrW(10), "vbLf")

            '' Unsupported in VB: byte, sbyte, ushort, short

            ' uint
            CheckLiteralToString(UInteger.MinValue, "0UI")
            CheckLiteralToString(UInteger.MaxValue, "4294967295UI")

            ' int
            CheckLiteralToString(0, "0")
            CheckLiteralToString(Integer.MinValue, "-2147483648")
            CheckLiteralToString(Integer.MaxValue, "2147483647")

            ' ulong
            CheckLiteralToString(ULong.MinValue, "0UL")
            CheckLiteralToString(ULong.MaxValue, "18446744073709551615UL")

            ' long
            CheckLiteralToString(0L, "0L")
            CheckLiteralToString(Long.MinValue, "-9223372036854775808L")
            CheckLiteralToString(Long.MaxValue, "9223372036854775807L")

            ' float
            CheckLiteralToString(0.0F, "0F")
            CheckLiteralToString(0.012345F, "0.012345F")
#If NET472 Then
            CheckLiteralToString(Single.MaxValue, "3.40282347E+38F")
#Else
            CheckLiteralToString(Single.MaxValue, "3.4028235E+38F")
#End If

            ' double
            CheckLiteralToString(0.0, "0")
            CheckLiteralToString(0.012345, "0.012345")
            CheckLiteralToString(Double.MaxValue, "1.7976931348623157E+308")

            ' decimal
            CheckLiteralToString(0D, "0D")
            CheckLiteralToString(0.012345D, "0.012345D")
            CheckLiteralToString(Decimal.MaxValue, "79228162514264337593543950335D")
        End Sub

        Private Shared Sub CheckLiteralToString(value As Object, expected As String)
            Dim factoryType As System.Type = GetType(SyntaxFactory)
            Dim literalMethods = factoryType.GetMethods().Where(Function(m) m.Name = "Literal" AndAlso m.GetParameters().Count() = 1)
            Dim literalMethod = literalMethods.Single(Function(m) m.GetParameters().Single().ParameterType = value.GetType())

            Assert.Equal(expected, literalMethod.Invoke(Nothing, {value}).ToString())
        End Sub

        <Fact>
        Public Shared Sub TestParseTypeNameOptions()
            Dim options As VisualBasicParseOptions = TestOptions.Regular
            Dim code = "
#If Variable
String
#Else
Integer
#End If"

            Dim type1 = SyntaxFactory.ParseTypeName(code, options:=options.WithPreprocessorSymbols(New KeyValuePair(Of String, Object)("Variable", "True")))
            Assert.Equal("String", type1.ToString())

            Dim type2 = SyntaxFactory.ParseTypeName(code, options:=options)
            Assert.Equal("Integer", type2.ToString())
        End Sub
    End Class
End Namespace
