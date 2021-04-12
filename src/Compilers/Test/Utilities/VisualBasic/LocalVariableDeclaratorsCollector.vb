' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects

Friend NotInheritable Class LocalVariableDeclaratorsCollector
    Inherits VisualBasicSyntaxWalker

    Private ReadOnly _builder As ArrayBuilder(Of SyntaxNode)

    Public Sub New(builder As ArrayBuilder(Of SyntaxNode))
        Me._builder = builder
    End Sub

    Friend Shared Function GetDeclarators(method As SourceMethodSymbol) As ImmutableArray(Of SyntaxNode)
        Dim builder = ArrayBuilder(Of SyntaxNode).GetInstance()
        Dim visitor = New LocalVariableDeclaratorsCollector(builder)

        visitor.Visit(method.BlockSyntax)
        Return builder.ToImmutableAndFree()
    End Function

    Public Overrides Sub VisitForEachStatement(node As ForEachStatementSyntax)
        Me._builder.Add(node)
        MyBase.VisitForEachStatement(node)
    End Sub

    Public Overrides Sub VisitForStatement(node As ForStatementSyntax)
        Me._builder.Add(node)
        MyBase.VisitForStatement(node)
    End Sub

    Public Overrides Sub VisitSyncLockStatement(node As SyncLockStatementSyntax)
        Me._builder.Add(node)
        MyBase.VisitSyncLockStatement(node)
    End Sub

    Public Overrides Sub VisitWithStatement(node As WithStatementSyntax)
        Me._builder.Add(node)
        MyBase.VisitWithStatement(node)
    End Sub

    Public Overrides Sub VisitUsingStatement(node As UsingStatementSyntax)
        Me._builder.Add(node)
        MyBase.VisitUsingStatement(node)
    End Sub

    Public Overrides Sub VisitVariableDeclarator(node As VariableDeclaratorSyntax)
        For Each name In node.Names
            Me._builder.Add(name)
        Next
        MyBase.VisitVariableDeclarator(node)
    End Sub

    Public Overrides Sub VisitIdentifierName(node As IdentifierNameSyntax)
    End Sub

    Public Overrides Sub VisitGoToStatement(node As GoToStatementSyntax)
        ' goto syntax does not declare locals
        Return
    End Sub

    Public Overrides Sub VisitLabelStatement(node As LabelStatementSyntax)
        ' labels do not declare locals
        Return
    End Sub

    Public Overrides Sub VisitLabel(node As LabelSyntax)
        ' labels do not declare locals
        Return
    End Sub

    Public Overrides Sub VisitGetXmlNamespaceExpression(node As GetXmlNamespaceExpressionSyntax)
        ' GetXmlNamespace does not declare locals
        Return
    End Sub

    Public Overrides Sub VisitMemberAccessExpression(node As MemberAccessExpressionSyntax)
        MyBase.Visit(node.Expression)

        ' right side of the . does not declare locals
        Return
    End Sub

    Public Overrides Sub VisitQualifiedName(node As QualifiedNameSyntax)
        MyBase.Visit(node.Left)

        ' right side of the . does not declare locals
        Return
    End Sub

    Public Overrides Sub VisitSimpleArgument(node As SimpleArgumentSyntax)
        MyBase.Visit(node.Expression)

        ' argument name in "goo(argName := expr)" does not declare locals
        Return
    End Sub
End Class
