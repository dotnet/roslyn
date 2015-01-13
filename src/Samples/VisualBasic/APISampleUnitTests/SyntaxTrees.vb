' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<TestClass()>
Public Class SyntaxTrees

    <TestMethod()>
    Public Sub FindNodeUsingMembers()
        Dim code =
<code>
Class C
    Sub M(i As Integer)
    End Sub
End Class
</code>.GetCode()

        Dim tree = SyntaxFactory.ParseSyntaxTree(code)
        Dim compilationUnit = CType(tree.GetRoot(), CompilationUnitSyntax)
        Dim typeBlock = CType(compilationUnit.Members(0), TypeBlockSyntax)
        Dim methodBlock = CType(typeBlock.Members(0), MethodBlockSyntax)
        Dim parameter = methodBlock.Begin.ParameterList.Parameters(0)
        Dim parameterName = parameter.Identifier.Identifier
        Assert.AreEqual("i", parameterName.ValueText)
    End Sub

    <TestMethod()>
    Public Sub FindNodeUsingQuery()
        Dim code =
<code>
Class C
    Sub M(i As Integer)
    End Sub
End Class
</code>.GetCode()

        Dim root As SyntaxNode = SyntaxFactory.ParseCompilationUnit(code)
        Dim parameter = root.DescendantNodes().OfType(Of ParameterSyntax)().First()
        Assert.AreEqual("i", parameter.Identifier.Identifier.ValueText)
    End Sub

    <TestMethod()>
    Public Sub UpdateNode()
        Dim code =
<code>
Class C
    Sub M()
    End Sub
End Class
</code>.GetCode()

        Dim tree = SyntaxFactory.ParseSyntaxTree(code)
        Dim root = CType(tree.GetRoot(), CompilationUnitSyntax)
        Dim method = CType(root.DescendantNodes().OfType(Of MethodBlockSyntax)().First().Begin, MethodStatementSyntax)

        Dim newMethod = method.Update(
            method.Kind,
            method.AttributeLists,
            method.Modifiers,
            method.Keyword,
            SyntaxFactory.Identifier("NewMethodName"),
            method.TypeParameterList,
            method.ParameterList,
            method.AsClause,
            method.HandlesClause,
            method.ImplementsClause)

        Dim newRoot = root.ReplaceNode(method, newMethod)
        Dim newTree = tree.WithRootAndOptions(newRoot, tree.Options)

        Dim newCode =
<code>
Class C
    Sub NewMethodName()
    End Sub
End Class
</code>.GetCode()

        Assert.AreEqual(newCode, newTree.GetText().ToString())
    End Sub

    Private Class FileContentsDumper
        Inherits VisualBasicSyntaxWalker

        Private ReadOnly sb As New StringBuilder()

        Public Overrides Sub VisitClassStatement(node As ClassStatementSyntax)
            sb.AppendLine(node.Keyword.ValueText & " " & node.Identifier.ValueText)
            MyBase.VisitClassStatement(node)
        End Sub

        Public Overrides Sub VisitStructureStatement(node As StructureStatementSyntax)
            sb.AppendLine(node.Keyword.ValueText & " " & node.Identifier.ValueText)
            MyBase.VisitStructureStatement(node)
        End Sub

        Public Overrides Sub VisitInterfaceStatement(node As InterfaceStatementSyntax)
            sb.AppendLine(node.Keyword.ValueText & " " & node.Identifier.ValueText)
            MyBase.VisitInterfaceStatement(node)
        End Sub

        Public Overrides Sub VisitMethodStatement(node As MethodStatementSyntax)
            sb.AppendLine("  " & node.Identifier.ToString())
            MyBase.VisitMethodStatement(node)
        End Sub

        Public Overrides Function ToString() As String
            Return sb.ToString()
        End Function
    End Class

    <TestMethod()>
    Public Sub WalkTreeUsingSyntaxWalker()
        Dim code =
<code>
Class C
    Sub M1()
    End Sub

    Structure S
    End Structure

    Sub M2()
    End Sub
End Class
</code>.GetCode()

        Dim node As SyntaxNode = SyntaxFactory.ParseCompilationUnit(code)
        Dim visitor As FileContentsDumper = New FileContentsDumper()
        visitor.Visit(node)

        Dim expectedText = "Class C" & vbCrLf &
                           "  M1" & vbCrLf &
                           "Structure S" & vbCrLf &
                           "  M2" & vbCrLf

        Assert.AreEqual(expectedText, visitor.ToString())
    End Sub

    Private Class RemoveMethodsRewriter
        Inherits VisualBasicSyntaxRewriter

        Public Overrides Function VisitMethodBlock(node As MethodBlockSyntax) As SyntaxNode
            ' Returning nothing removes the syntax node
            Return Nothing
        End Function
    End Class


    <TestMethod()>
    Public Sub TransformTreeUsingSyntaxRewriter()
        Dim code =
<code>
Class C
    Private field As Integer

    Sub M()
    End Sub
End Class
</code>.GetCode()

        Dim tree = SyntaxFactory.ParseSyntaxTree(code)
        Dim root = tree.GetRoot()
        Dim newRoot = root.RemoveNodes(root.DescendantNodes.OfType(Of MethodBlockSyntax), SyntaxRemoveOptions.KeepNoTrivia)

        Dim expectedCode =
<code>
Class C
    Private field As Integer
End Class
</code>.GetCode()

        Assert.AreEqual(expectedCode, newRoot.ToFullString())
    End Sub

End Class
