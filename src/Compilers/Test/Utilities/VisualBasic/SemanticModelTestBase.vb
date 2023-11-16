' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Xunit

Public MustInherit Class SemanticModelTestBase : Inherits BasicTestBase

    Protected Function GetNode(compilation As VisualBasicCompilation, treeName As String, textToFind As String) As VisualBasicSyntaxNode
        Dim tree = CompilationUtils.GetTree(compilation, treeName)
        Dim node = DirectCast(CompilationUtils.FindTokenFromText(tree, textToFind).Parent, VisualBasicSyntaxNode)
        Return node
    End Function

    Protected Function GetPosition(compilation As VisualBasicCompilation, treeName As String, textToFind As String) As Integer
        Dim tree = CompilationUtils.GetTree(compilation, treeName)
        Dim text As String = tree.GetText().ToString()
        Dim position As Integer = text.IndexOf(textToFind, StringComparison.Ordinal)
        Return position
    End Function

    Protected Function FindBindingText(Of TNode As SyntaxNode)(compilation As Compilation, fileName As String, Optional which As Integer = 0) As TNode
        Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()

        Dim bindText As String = Nothing
        Dim bindPoint = FindBindingTextPosition(compilation, fileName, bindText, which)
        Dim token As SyntaxToken = tree.GetRoot().FindToken(bindPoint)
        Dim node = token.Parent

        While (node IsNot Nothing AndAlso node.ToString <> bindText)
            node = node.Parent
        End While

        Assert.NotNull(node)  ' If this trips, then node  wasn't found
        Assert.IsAssignableFrom(GetType(TNode), node)
        Assert.Equal(bindText, node.ToString())

        Return DirectCast(node, TNode)
    End Function

    Private Function FindBindingTextPosition(compilation As Compilation, fileName As String, ByRef bindText As String, Optional which As Integer = 0) As Integer
        Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()

        Dim bindMarker As String
        If which > 0 Then
            bindMarker = "'BIND" & which.ToString() & ":"""
        Else
            bindMarker = "'BIND:"""
        End If

        Dim text As String = tree.GetRoot().ToFullString()
        Dim bindCommentIndex As Integer = text.IndexOf(bindMarker, StringComparison.Ordinal) + bindMarker.Length
        bindText = text.Substring(bindCommentIndex, text.IndexOf(""""c, bindCommentIndex) - bindCommentIndex)
        Dim bindPoint = text.LastIndexOf(bindText, bindCommentIndex - bindMarker.Length, StringComparison.Ordinal)
        Return bindPoint
    End Function

    Protected Function FindBindingTextPosition(compilation As Compilation, fileName As String) As Integer
        Dim bindText As String = Nothing
        Return FindBindingTextPosition(compilation, fileName, bindText)
    End Function

    Protected Function FindBindingStartText(Of TNode As SyntaxNode)(compilation As Compilation, fileName As String, Optional which As Integer = 0) As TNode
        Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()

        Dim bindText As String = Nothing
        Dim bindPoint = FindBindingTextPosition(compilation, fileName, bindText, which)
        Dim token As SyntaxToken = tree.GetRoot().FindToken(bindPoint)
        Dim node = token.Parent

        While (node IsNot Nothing AndAlso node.ToString.StartsWith(bindText, StringComparison.Ordinal) AndAlso Not (TypeOf node Is TNode))
            node = node.Parent
        End While

        Assert.NotNull(node)  ' If this trips, then node  wasn't found
        Assert.IsAssignableFrom(GetType(TNode), node)
        Assert.Contains(bindText, node.ToString(), StringComparison.Ordinal)

        Return DirectCast(node, TNode)
    End Function

    Protected Function GetStartSpanErrorMessage(syntax As SyntaxNode, tpSymbol As ISymbol) As String
        Return "    Syntax.SpanStart : " & syntax.SpanStart &
               "    Location1.SourceSpan.Start : " & tpSymbol.Locations.Item(0).SourceSpan.Start &
               "    Location2.SourceSpan.Start : " & tpSymbol.Locations.Item(0).SourceSpan.Start
    End Function

    Friend Function GetAliasInfoForTest(compilation As Compilation, fileName As String, Optional which As Integer = 0) As AliasSymbol
        Dim node As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, fileName, which)
        Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()
        Dim semanticModel = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)
        Return DirectCast(semanticModel.GetAliasInfo(node), AliasSymbol)
    End Function

    Protected Function GetBlockOrStatementInfoForTest(Of StmtSyntax As SyntaxNode)(compilation As Compilation, fileName As String, Optional which As Integer = 0, Optional useParent As Boolean = False) As Object
        Dim node As SyntaxNode = CompilationUtils.FindBindingText(Of StmtSyntax)(compilation, fileName, which)
        Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()
        Dim semanticModel = CType(compilation.GetSemanticModel(tree), VBSemanticModel)

        If useParent Then
            node = node.Parent
        End If

        If TypeOf node Is ForEachStatementSyntax Then
            Return semanticModel.GetForEachStatementInfo(DirectCast(node, ForEachStatementSyntax))
        ElseIf TypeOf node Is ForEachBlockSyntax Then
            Return semanticModel.GetForEachStatementInfo(DirectCast(node, ForEachBlockSyntax).ForEachStatement)
        End If

        Throw New NotSupportedException("Type of syntax node is not supported by GetExtendedSemanticInfoForTest")
    End Function

    Friend Function GetLookupNames(compilation As Compilation, filename As String, Optional container As NamespaceOrTypeSymbol = Nothing) As List(Of String)
        Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = filename).Single()
        Dim binding = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)

        Return binding.LookupNames(FindBindingTextPosition(compilation, "a.vb"), container).ToList()
    End Function

    Friend Function GetLookupSymbols(compilation As Compilation, filename As String, Optional container As NamespaceOrTypeSymbol = Nothing, Optional name As String = Nothing, Optional arity As Integer? = Nothing, Optional includeReducedExtensionMethods As Boolean = False, Optional mustBeStatic As Boolean = False) As List(Of ISymbol)
        Debug.Assert(Not includeReducedExtensionMethods OrElse Not mustBeStatic)

        Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = filename).Single()
        Dim binding = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)

        Dim anyArity = Not arity.HasValue
        Dim position = FindBindingTextPosition(compilation, "a.vb")
        Return If(
            mustBeStatic,
            binding.LookupStaticMembers(position, container, name),
            binding.LookupSymbols(position, container, name, includeReducedExtensionMethods)
            ).Where(Function(s) anyArity OrElse DirectCast(s, Symbol).GetArity() = arity.Value).ToList()
    End Function
End Class
