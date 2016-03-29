' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class GetSemanticInfoBrokenCodeTests
        Inherits SemanticModelTestBase

        <WorkItem(544328, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544328")>
        <Fact>
        Public Sub Bug12601()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Module M
    Sub M()
        Dim x As New {
    End Sub
End Module
]]></file>
</compilation>, {SystemCoreRef})
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            VisitAllExpressions(model, tree.GetCompilationUnitRoot())
        End Sub

        <WorkItem(544455, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544455")>
        <Fact>
        Public Sub EmptyDefaultPropertyName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Default Property
End Class
Module M
    Function F(o As C) As Object
        Return o()
    End Function
End Module
]]></file>
</compilation>, {SystemCoreRef})
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            VisitAllExpressions(model, tree.GetCompilationUnitRoot())
        End Sub

        <WorkItem(545233, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545233")>
        <Fact>
        Public Sub Bug13538()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Sub M()
        SyncLock
    End Sub
End Class
]]></file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            VisitAllExpressions(model, tree.GetCompilationUnitRoot())
        End Sub

        ''' <summary>
        ''' The BoundNode tree will contain a BoundPropertyGroup
        ''' if property overload resolution fails.
        ''' </summary>
        <Fact>
        Public Sub AnalyzePropertyGroup()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Module M
    Sub M(c As Char, s As String)
        If c <> s(
    End Sub
End Module
]]></file>
</compilation>, {SystemCoreRef})
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            For Each node In GetAllExpressions(tree.GetCompilationUnitRoot())
                model.AnalyzeDataFlow(node)
            Next
        End Sub

        <WorkItem(545667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545667")>
        <Fact()>
        Public Sub Bug14266()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Enum E
    A
End Enum
]]></file>
</compilation>)
            Dim oldTree = compilation.SyntaxTrees(0)
            Dim oldText = oldTree.GetText()
            Dim model = compilation.GetSemanticModel(oldTree)
            VisitAllDeclarations(model, oldTree.GetCompilationUnitRoot())

            ' Insert a single character at the beginning.
            Dim newText = oldText.Replace(start:=0, length:=0, newText:="B")
            Dim newTree = oldTree.WithChangedText(newText)
            compilation = compilation.ReplaceSyntaxTree(oldTree, newTree)
            model = compilation.GetSemanticModel(newTree)
            VisitAllDeclarations(model, newTree.GetCompilationUnitRoot())
        End Sub

        <WorkItem(546685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546685")>
        <Fact()>
        Public Sub Bug16557()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module M
    Sub M(b As Boolean)
        If b Then
        End If
    End Sub
End Module
]]></file>
</compilation>)
            compilation.AssertNoDiagnostics()

            ' Change "End Module" to "End module".
            Dim oldTree = compilation.SyntaxTrees(0)
            Dim oldText = oldTree.GetText()
            Dim position = oldText.ToString().LastIndexOf("Module", StringComparison.Ordinal)
            Dim newText = oldText.Replace(start:=position, length:=1, newText:="m")
            Dim newTree = oldTree.WithChangedText(newText)
            compilation = compilation.ReplaceSyntaxTree(oldTree, newTree)
            compilation.AssertNoDiagnostics()
        End Sub

        <Fact()>
        Public Sub ExpressionInStructuredTrivia()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
#If e=True
]]></file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            For Each expr In GetAllExpressions(tree.GetCompilationUnitRoot())
                model.GetTypeInfo(expr)
            Next
        End Sub

        ''' <summary>
        ''' Me references are not valid within a Module.
        ''' </summary>
        <WorkItem(546570, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546570")>
        <Fact()>
        Public Sub AnalyzeForEachMeInModule()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module M
    Sub M()
        For Each Me
        Next
    End Sub
End Module
]]></file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            For Each node In GetAllStatements(tree.GetCompilationUnitRoot())
                model.AnalyzeDataFlow(node)
            Next
        End Sub

        <WorkItem(546914, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546914")>
        <Fact()>
        Public Sub Bug17230_If()
            AnalyzeExpressionDataFlow(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        If True Then
            Dim x = Sub() If False : ElseIf
        End If
    End Sub
End Module
]]></file>
</compilation>))
            AnalyzeExpressionDataFlow(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        If True Then
            Dim x = Sub() If False : Else
        End If
    End Sub
End Module
]]></file>
</compilation>))
            AnalyzeExpressionDataFlow(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Dim x = Sub() If False
    End Sub
End Module
]]></file>
</compilation>))
            AnalyzeExpressionDataFlow(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Dim x = Sub() If False Then
    End Sub
End Module
]]></file>
</compilation>))
        End Sub

        <WorkItem(546914, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546914")>
        <Fact()>
        Public Sub Bug17230_Other()
            AnalyzeExpressionDataFlow(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Dim x = Sub() With False : End With
    End Sub
End Module
]]></file>
</compilation>))
            AnalyzeExpressionDataFlow(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Dim x = Sub() SyncLock False : End SyncLock
    End Sub
End Module
]]></file>
</compilation>))
            AnalyzeExpressionDataFlow(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Dim x = Sub() Select Case False : End Select
    End Sub
End Module
]]></file>
</compilation>))
            AnalyzeExpressionDataFlow(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Dim x = Sub() While False : End While
    End Sub
End Module
]]></file>
</compilation>))
            AnalyzeExpressionDataFlow(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Dim x = Sub() Do While False : Loop
    End Sub
End Module
]]></file>
</compilation>))
            AnalyzeExpressionDataFlow(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Dim x = Sub() For b = True To False : Next
    End Sub
End Module
]]></file>
</compilation>))
            AnalyzeExpressionDataFlow(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Dim x = Sub() For Each b in { False } : Next
    End Sub
End Module
]]></file>
</compilation>))
            AnalyzeExpressionDataFlow(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Dim x = Sub() Using False : End Using
    End Sub
End Module
]]></file>
</compilation>))
        End Sub

        <WorkItem(571062, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/571062")>
        <Fact()>
        Public Sub Bug571062()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
End Clas
Namespace N
    Class B
        Sub M(Optional o = Nothing)
        End Sub
        ReadOnly Property P(Optional o = Nothing)
            Get
                Return Nothing
            End Get
        End Property
        Event E(Optional o = Nothing)
        Private F = Function(Optional o = Nothing) Nothing
        Delegate Sub D(Optional o = Nothing)
    End Class
End Namespace
]]></file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            VisitAllExpressions(model, tree.GetCompilationUnitRoot())
        End Sub

        <WorkItem(578141, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578141")>
        <Fact()>
        Public Sub IsImplicitlyDeclared()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Dim F
MustOverride Property P
MustOverride Sub M()
]]></file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim diagnostics = model.GetDiagnostics().ToArray()
            Assert.NotEmpty(diagnostics)
            Dim type = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)(TypeSymbol.ImplicitTypeName)
            Assert.True(type.IsImplicitlyDeclared)
            Dim member As Symbol
            member = type.GetMember(Of FieldSymbol)("F")
            Assert.False(member.IsImplicitlyDeclared)
            member = type.GetMember(Of PropertySymbol)("P")
            Assert.False(member.IsImplicitlyDeclared)
            member = type.GetMember(Of MethodSymbol)("M")
            Assert.False(member.IsImplicitlyDeclared)
        End Sub

        <WorkItem(578141, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578141")>
        <Fact()>
        Public Sub MustOverrideMember()
            ' MustOverride method in script class.
            MustOverrideMemberCore(CompilationUtils.CreateCompilationWithMscorlib({VisualBasicSyntaxTree.ParseText(<![CDATA[
MustOverride Sub M()
]]>.Value,
                options:=TestOptions.Script)}))
            ' MustOverride method in invalid class.
            MustOverrideMemberCore(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
MClass C
    MustOverride Sub M()
End Class
]]></file>
</compilation>))
            ' MustOverride property in script class.
            MustOverrideMemberCore(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
MClass C
    MustOverride Property P
End Class
]]></file>
</compilation>))
            ' MustOverride constructor.
            MustOverrideMemberCore(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
MustInherit Class C
    MustOverride Sub New()
End Class
]]></file>
</compilation>))
            ' MustOverride method in class not MustInherit
            MustOverrideMemberCore(CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    MustOverride Sub M()
End Class
]]></file>
</compilation>))
        End Sub

        Private Sub MustOverrideMemberCore(compilation As VisualBasicCompilation)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim diagnostics = model.GetDiagnostics().ToArray()
            Assert.NotEmpty(diagnostics)
        End Sub

        <WorkItem(611707, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611707")>
        <Fact()>
        Public Sub UnexpectedVarianceKeyword()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Interface(Await
]]></file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim diagnostics = model.GetDiagnostics().ToArray()
            Assert.NotEmpty(diagnostics)
        End Sub

        <WorkItem(611707, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611707")>
        <Fact()>
        Public Sub UnexpectedVarianceKeyword_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Delegate Sub D(Of From
]]></file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim diagnostics = model.GetDiagnostics().ToArray()
            Assert.NotEmpty(diagnostics)
        End Sub

        <WorkItem(762034, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/762034")>
        <Fact()>
        Public Sub Bug762034()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Dim t = !Str
]]></file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            For Each expr In GetAllExpressions(tree.GetCompilationUnitRoot())
                Dim symbolInfo = model.GetSymbolInfo(expr)
                Assert.NotNull(symbolInfo)
                model.AnalyzeDataFlow(expr)
            Next
        End Sub

        Private Sub AnalyzeExpressionDataFlow(compilation As VisualBasicCompilation)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            For Each expr In GetAllExpressions(tree.GetCompilationUnitRoot())
                model.AnalyzeDataFlow(expr)
            Next
        End Sub

        Private Sub VisitAllExpressions(model As SemanticModel, node As VisualBasicSyntaxNode)
            For Each expr In GetAllExpressions(node)
                Dim symbolInfo = model.GetSymbolInfo(expr)
                Assert.NotNull(symbolInfo)
                Dim typeInfo = model.GetTypeInfo(expr)
                Assert.NotNull(typeInfo)
            Next
        End Sub

        Private Sub VisitAllDeclarations(model As SemanticModel, node As VisualBasicSyntaxNode)
            For Each node In node.DescendantNodesAndSelf()
                model.GetDeclaredSymbol(node)
            Next
        End Sub

        Private Shared Function GetAllExpressions(node As VisualBasicSyntaxNode) As IEnumerable(Of ExpressionSyntax)
            Return node.DescendantNodesAndSelf(descendIntoTrivia:=True).OfType(Of ExpressionSyntax)()
        End Function

        Private Shared Function GetAllStatements(node As VisualBasicSyntaxNode) As IEnumerable(Of ExecutableStatementSyntax)
            Return node.DescendantNodesAndSelf().OfType(Of ExecutableStatementSyntax)()
        End Function

    End Class

End Namespace
