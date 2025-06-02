' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class OptionApis
        Inherits SemanticModelTestBase

        <Fact>
        Public Sub Options1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="allon.vb">
Option Strict On
Option Infer On
Option Explicit On
Option Compare Text
    </file>
    <file name="alloff.vb">
Option Strict Off
Option Infer Off
Option Explicit Off
Option Compare Binary
    </file>
    <file name="empty.vb"></file>
</compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom).WithOptionInfer(False).WithOptionExplicit(True).WithOptionCompareText(False))

            Dim semanticModelAllOn = CompilationUtils.GetSemanticModel(compilation, "allon.vb")
            Assert.Equal(OptionStrict.On, semanticModelAllOn.OptionStrict)
            Assert.Equal(True, semanticModelAllOn.OptionInfer)
            Assert.Equal(True, semanticModelAllOn.OptionExplicit)
            Assert.Equal(True, semanticModelAllOn.OptionCompareText)

            Dim semanticModelAllOff = CompilationUtils.GetSemanticModel(compilation, "alloff.vb")
            Assert.Equal(OptionStrict.Off, semanticModelAllOff.OptionStrict)
            Assert.Equal(False, semanticModelAllOff.OptionInfer)
            Assert.Equal(False, semanticModelAllOff.OptionExplicit)
            Assert.Equal(False, semanticModelAllOff.OptionCompareText)

            Dim semanticModelEmpty = CompilationUtils.GetSemanticModel(compilation, "empty.vb")
            Assert.Equal(OptionStrict.Custom, semanticModelEmpty.OptionStrict)
            Assert.Equal(False, semanticModelEmpty.OptionInfer)
            Assert.Equal(True, semanticModelEmpty.OptionExplicit)
            Assert.Equal(False, semanticModelEmpty.OptionCompareText)
        End Sub

        <Fact>
        <WorkItem(50610, "https://github.com/dotnet/roslyn/issues/50610")>
        Public Sub MissingParts_01()
            Dim compilation = CreateCompilation(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Option Text
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim root = tree.GetRoot()

            Dim stmt = root.DescendantNodes().OfType(Of OptionStatementSyntax)().Single()

            Assert.Equal(SyntaxKind.CompareKeyword, stmt.NameKeyword.Kind)
            Assert.Equal(SyntaxKind.None, stmt.ValueKeyword.Kind)
            Assert.True(stmt.NameKeyword.IsMissing)
            Assert.False(stmt.ValueKeyword.IsMissing)

            compilation.AssertTheseDiagnostics(
<expected>
BC30208: 'Compare' expected.
Option Text
       ~~~~
</expected>)

            Dim model = compilation.GetSemanticModel(tree)
            For i As Integer = 0 To root.Span.End
                model.GetEnclosingSymbol(i)
            Next
        End Sub

        <Fact>
        Public Sub MissingParts_02()
            Dim compilation = CreateCompilation(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Option Compare
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim root = tree.GetRoot()

            Dim stmt = root.DescendantNodes().OfType(Of OptionStatementSyntax)().Single()

            Assert.Equal(SyntaxKind.CompareKeyword, stmt.NameKeyword.Kind)
            Assert.Equal(SyntaxKind.BinaryKeyword, stmt.ValueKeyword.Kind)
            Assert.False(stmt.NameKeyword.IsMissing)
            Assert.True(stmt.ValueKeyword.IsMissing)

            compilation.AssertTheseDiagnostics(
<expected>
BC30207: 'Option Compare' must be followed by 'Text' or 'Binary'.
Option Compare
~~~~~~~~~~~~~~
</expected>)

            Dim model = compilation.GetSemanticModel(tree)
            For i As Integer = 0 To root.Span.End
                model.GetEnclosingSymbol(i)
            Next
        End Sub

        <Fact>
        Public Sub MissingParts_03()
            Dim compilation = CreateCompilation(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Option Strict
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim root = tree.GetRoot()

            Dim stmt = root.DescendantNodes().OfType(Of OptionStatementSyntax)().Single()

            Assert.Equal(SyntaxKind.StrictKeyword, stmt.NameKeyword.Kind)
            Assert.Equal(SyntaxKind.None, stmt.ValueKeyword.Kind)
            Assert.False(stmt.NameKeyword.IsMissing)
            Assert.False(stmt.ValueKeyword.IsMissing)

            compilation.AssertNoDiagnostics()

            Dim model = compilation.GetSemanticModel(tree)
            For i As Integer = 0 To root.Span.End
                model.GetEnclosingSymbol(i)
            Next
        End Sub

        <Fact>
        Public Sub MissingParts_04()
            Dim compilation = CreateCompilation(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Option Infer
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim root = tree.GetRoot()

            Dim stmt = root.DescendantNodes().OfType(Of OptionStatementSyntax)().Single()

            Assert.Equal(SyntaxKind.InferKeyword, stmt.NameKeyword.Kind)
            Assert.Equal(SyntaxKind.None, stmt.ValueKeyword.Kind)
            Assert.False(stmt.NameKeyword.IsMissing)
            Assert.False(stmt.ValueKeyword.IsMissing)

            compilation.AssertNoDiagnostics()

            Dim model = compilation.GetSemanticModel(tree)
            For i As Integer = 0 To root.Span.End
                model.GetEnclosingSymbol(i)
            Next
        End Sub

        <Fact>
        Public Sub MissingParts_05()
            Dim compilation = CreateCompilation(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Option Explicit
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim root = tree.GetRoot()

            Dim stmt = root.DescendantNodes().OfType(Of OptionStatementSyntax)().Single()

            Assert.Equal(SyntaxKind.ExplicitKeyword, stmt.NameKeyword.Kind)
            Assert.Equal(SyntaxKind.None, stmt.ValueKeyword.Kind)
            Assert.False(stmt.NameKeyword.IsMissing)
            Assert.False(stmt.ValueKeyword.IsMissing)

            compilation.AssertNoDiagnostics()

            Dim model = compilation.GetSemanticModel(tree)
            For i As Integer = 0 To root.Span.End
                model.GetEnclosingSymbol(i)
            Next
        End Sub

        <Fact>
        Public Sub MissingParts_06()
            Dim compilation = CreateCompilation(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Option On
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim root = tree.GetRoot()

            Dim stmt = root.DescendantNodes().OfType(Of OptionStatementSyntax)().Single()

            Assert.Equal(SyntaxKind.StrictKeyword, stmt.NameKeyword.Kind)
            Assert.Equal(SyntaxKind.None, stmt.ValueKeyword.Kind)
            Assert.True(stmt.NameKeyword.IsMissing)
            Assert.False(stmt.ValueKeyword.IsMissing)

            compilation.AssertTheseDiagnostics(
<expected>
BC30206: 'Option' must be followed by 'Compare', 'Explicit', 'Infer', or 'Strict'.
Option On
       ~~
</expected>)

            Dim model = compilation.GetSemanticModel(tree)
            For i As Integer = 0 To root.Span.End
                model.GetEnclosingSymbol(i)
            Next
        End Sub

        <Fact>
        Public Sub MissingParts_07()
            Dim compilation = CreateCompilation(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Option Off
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim root = tree.GetRoot()

            Dim stmt = root.DescendantNodes().OfType(Of OptionStatementSyntax)().Single()

            Assert.Equal(SyntaxKind.StrictKeyword, stmt.NameKeyword.Kind)
            Assert.Equal(SyntaxKind.None, stmt.ValueKeyword.Kind)
            Assert.True(stmt.NameKeyword.IsMissing)
            Assert.False(stmt.ValueKeyword.IsMissing)

            compilation.AssertTheseDiagnostics(
<expected>
BC30206: 'Option' must be followed by 'Compare', 'Explicit', 'Infer', or 'Strict'.
Option Off
       ~~~
</expected>)

            Dim model = compilation.GetSemanticModel(tree)
            For i As Integer = 0 To root.Span.End
                model.GetEnclosingSymbol(i)
            Next
        End Sub

        <Fact>
        Public Sub MissingParts_08()
            Dim compilation = CreateCompilation(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Option
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim root = tree.GetRoot()

            Dim stmt = root.DescendantNodes().OfType(Of OptionStatementSyntax)().Single()

            Assert.Equal(SyntaxKind.StrictKeyword, stmt.NameKeyword.Kind)
            Assert.Equal(SyntaxKind.None, stmt.ValueKeyword.Kind)
            Assert.True(stmt.NameKeyword.IsMissing)
            Assert.False(stmt.ValueKeyword.IsMissing)

            compilation.AssertTheseDiagnostics(
<expected>
BC30206: 'Option' must be followed by 'Compare', 'Explicit', 'Infer', or 'Strict'.
Option
~~~~~~
</expected>)

            Dim model = compilation.GetSemanticModel(tree)
            For i As Integer = 0 To root.Span.End
                model.GetEnclosingSymbol(i)
            Next
        End Sub

        <Fact>
        <WorkItem(50610, "https://github.com/dotnet/roslyn/issues/50610")>
        Public Sub MissingParts_09()
            Dim compilation = CreateCompilation(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Option Binary
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim root = tree.GetRoot()

            Dim stmt = root.DescendantNodes().OfType(Of OptionStatementSyntax)().Single()

            Assert.Equal(SyntaxKind.CompareKeyword, stmt.NameKeyword.Kind)
            Assert.Equal(SyntaxKind.None, stmt.ValueKeyword.Kind)
            Assert.True(stmt.NameKeyword.IsMissing)
            Assert.False(stmt.ValueKeyword.IsMissing)

            compilation.AssertTheseDiagnostics(
<expected>
BC30208: 'Compare' expected.
Option Binary
       ~~~~~~
</expected>)

            Dim model = compilation.GetSemanticModel(tree)
            For i As Integer = 0 To root.Span.End
                model.GetEnclosingSymbol(i)
            Next
        End Sub
    End Class
End Namespace
