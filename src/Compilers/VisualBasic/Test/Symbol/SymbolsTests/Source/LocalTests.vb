' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class LocalTests
        Inherits BasicTestBase

        <WorkItem(59709, "https://github.com/dotnet/roslyn/issues/59709")>
        <Fact>
        Public Sub UsingBlock()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Module M1
    Class C
        Sub S() As Object
            Using writer As System.IO.TextWriter = System.IO.File.CreateText("log.txt")
                writer.WriteLine("")    'BIND:"writer"
            End Using
        End Sub
    End Class
End Module
    </file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 0)
            Dim local = DirectCast(model.GetSymbolInfo(expressionSyntax).Symbol, ILocalSymbol)
            Assert.False(local.IsConst)
            Assert.False(local.IsForEach)
            Assert.True(local.IsUsing)
        End Sub

        <WorkItem(59709, "https://github.com/dotnet/roslyn/issues/59709")>
        <Fact>
        Public Sub ForEach()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Module M1
    Class C
        Sub S() As Object
            Dim a() As Integer = {1, 2, 3}
            For Each x As Integer In a
                Dim y = x   'BIND:"x"
            Next
        End Sub
    End Class
End Module
    </file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 0)
            Dim local = DirectCast(model.GetSymbolInfo(expressionSyntax).Symbol, ILocalSymbol)
            Assert.False(local.IsConst)
            Assert.True(local.IsForEach)
            Assert.True(VisualBasicExtensions.IsForEach(local))
            Assert.True(local.IsForEach()) ' calls the property, not the extension method
            Assert.False(local.IsUsing)
        End Sub

    End Class
End Namespace
