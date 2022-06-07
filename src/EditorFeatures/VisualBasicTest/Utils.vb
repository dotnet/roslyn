' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests
    Friend Module Utils
        Friend Function ParseCode(code As String) As SyntaxTree
            Dim text = SourceText.From(code)
            Return SyntaxFactory.ParseSyntaxTree(text)
        End Function

        Friend Function StringFromLines(ParamArray lines As String()) As String
            Return String.Join(Environment.NewLine, lines)
        End Function

        Friend Function ParseLines(ParamArray lines As String()) As SyntaxTree
            Dim code = StringFromLines(lines)
            Return ParseCode(code)
        End Function

        Friend Function ParseExpression(expr As String) As SyntaxTree
            Dim format =
                "Class C1 " & vbCrLf &
                "  Sub S1()" & vbCrLf &
                "    Dim x = {0}" & vbCrLf &
                "  End Sub" & vbCrLf &
                "End Class"
            Dim code = String.Format(format, expr)
            Return ParseCode(code)
        End Function

        Friend Function ParseStatement(statement As String) As SyntaxTree
            Dim format = StringFromLines(
                "Class C1",
                "  Sub S1()",
                "    {0}",
                "  End Sub",
                "End Class")
            Dim code = String.Format(format, statement)
            Return ParseCode(code)
        End Function

        ''' <summary>
        ''' DFS search to find the first node of a given type.
        ''' </summary>
        <Extension()>
        Friend Function FindFirstNodeOfType(Of T As SyntaxNode)(node As SyntaxNode) As T
            If TypeOf (node) Is T Then
                Return CType(node, T)
            End If

            For Each child In node.ChildNodesAndTokens()
                If child.IsNode Then
                    Dim foundNode = child.AsNode().FindFirstNodeOfType(Of T)()
                    If foundNode IsNot Nothing Then
                        Return foundNode
                    End If
                End If
            Next

            Return Nothing
        End Function

        <Extension()>
        Friend Function DigToNthNodeOfType(Of T As SyntaxNode)(node As SyntaxNode, index As Integer) As T
            Return node.ChildNodesAndTokens().Where(Function(n) n.IsNode).
                                 Select(Function(n) n.AsNode()).
                                 OfType(Of T).ElementAt(index)
        End Function

        <Extension()>
        Friend Function DigToFirstNodeOfType(Of T As SyntaxNode)(node As SyntaxNode) As T
            Return node.ChildNodesAndTokens().Where(Function(n) n.IsNode).
                                 Select(Function(n) n.AsNode()).
                                 OfType(Of T).First()
        End Function

        <Extension()>
        Friend Function DigToFirstNodeOfType(Of T As SyntaxNode)(syntaxTree As SyntaxTree) As T
            Return syntaxTree.GetRoot().DigToFirstNodeOfType(Of T)()
        End Function

        <Extension()>
        Friend Function DigToLastNodeOfType(Of T As SyntaxNode)(node As SyntaxNode) As T
            Return node.ChildNodesAndTokens().Where(Function(n) n.IsNode).
                                 Select(Function(n) n.AsNode()).
                                 OfType(Of T).Last()
        End Function

        <Extension()>
        Friend Function DigToFirstTypeBlock(syntaxTree As SyntaxTree) As TypeBlockSyntax
            Return syntaxTree.GetRoot().ChildNodesAndTokens().Where(Function(n) n.IsNode).
                                      Select(Function(n) n.AsNode()).
                                      OfType(Of TypeBlockSyntax).First()
        End Function

        <Extension()>
        Friend Function DigToFirstNamespace(syntaxTree As SyntaxTree) As NamespaceBlockSyntax
            Return syntaxTree.GetRoot().ChildNodesAndTokens().Where(Function(n) n.IsNode).
                                      Select(Function(n) n.AsNode()).
                                      OfType(Of NamespaceBlockSyntax).First()
        End Function

        Friend Class TreeNodePair(Of T As SyntaxNode)
            Private ReadOnly _tree As SyntaxTree
            Private ReadOnly _node As T

            Public Sub New(syntaxTree As SyntaxTree, node As T)
#If Not CODE_STYLE Then
                Contract.ThrowIfNull(syntaxTree)
                Contract.ThrowIfNull(node)
#End If
                _tree = syntaxTree
                _node = node
            End Sub

            Public ReadOnly Property Tree As SyntaxTree
                Get
                    Return _tree
                End Get
            End Property

            Public ReadOnly Property Node As T
                Get
                    Return _node
                End Get
            End Property
        End Class

        Friend Class TreeNodePair
            Friend Shared Function Create(Of T As SyntaxNode)(syntaxTree As SyntaxTree, node As T) As TreeNodePair(Of T)
                Return New TreeNodePair(Of T)(syntaxTree, node)
            End Function
        End Class

        Private Function SplitIntoLines(
            text As String,
            separator As String,
            Optional removeLeadingLineBreaks As Boolean = True,
            Optional removeTrailingLineBreaks As Boolean = True) As IEnumerable(Of String)

            Dim lines = text.Split({separator}, StringSplitOptions.None).ToList()

            If removeLeadingLineBreaks Then
                While lines.Count > 0
                    If lines(0).Length = 0 Then
                        lines.RemoveAt(0)
                    Else
                        Exit While
                    End If
                End While
            End If

            If removeTrailingLineBreaks Then
                For i = lines.Count - 1 To 0 Step -1
                    If lines(i).Length = 0 Then
                        lines.RemoveAt(i)
                    Else
                        Exit For
                    End If
                Next
            End If

            Return lines
        End Function

        Private Function SurroundAndJoinLines(
            lines As IEnumerable(Of String),
            Optional leading As String = Nothing,
            Optional trailing As String = Nothing) As String

            Dim builder As New StringBuilder
            For Each line In lines
                If Not String.IsNullOrWhiteSpace(line) Then
                    builder.Append(leading)
                End If

                builder.Append(line)
                builder.Append(trailing)
            Next

            Return builder.ToString()
        End Function

        <Extension()>
        Friend Function ConvertTestSourceTag(testSource As XElement) As String
            ' Line breaks in XML values are represented as line feeds.
            Dim lines = SplitIntoLines(testSource.NormalizedValue, vbCrLf)

            Dim importStatements = "Imports System" & vbCrLf &
                                   "Imports System.Collections.Generic" & vbCrLf &
                                   "Imports System.Linq"

            Select Case testSource.Name
                Case "ClassDeclaration"
                    Return importStatements & vbCrLf &
                        "Class C1" & vbCrLf &
                        SurroundAndJoinLines(lines, "    ", vbCrLf) &
                        "End Class"

                Case "StructureDeclaration"
                    Return importStatements & vbCrLf &
                        "Structure S" & vbCrLf &
                        SurroundAndJoinLines(lines, "    ", vbCrLf) &
                        "End Structure"

                Case "NamespaceDeclaration"
                    Return importStatements & vbCrLf &
                        "Namespace Roslyn" & vbCrLf &
                        SurroundAndJoinLines(lines, "    ", vbCrLf) &
                        "End Namespace"

                Case "InterfaceDeclaration"
                    Return importStatements & vbCrLf &
                        "Interface IInterface" & vbCrLf &
                        SurroundAndJoinLines(lines, "    ", vbCrLf) &
                        "End Interface"

                Case "EnumDeclaration"
                    Return importStatements & vbCrLf &
                        "Enum Goo" & vbCrLf &
                        SurroundAndJoinLines(lines, "    ", vbCrLf) &
                        "End Enum"

                Case "ModuleDeclaration"
                    Return importStatements & vbCrLf &
                        "Module M1" & vbCrLf &
                        SurroundAndJoinLines(lines, "    ", vbCrLf) &
                        "End Module"

                Case "MethodBody"
                    Return importStatements & vbCrLf &
                        "Class C1" & vbCrLf &
                        "    Sub Method()" & vbCrLf &
                        SurroundAndJoinLines(lines, "        ", vbCrLf) &
                        "    End Sub" & vbCrLf &
                        "End Class"

                Case "SharedMethodBody"
                    Return importStatements & vbCrLf &
                        "Class C1" & vbCrLf &
                        "    Shared Sub Method()" & vbCrLf &
                        SurroundAndJoinLines(lines, "        ", vbCrLf) &
                        "    End Sub" & vbCrLf &
                        "End Class"

                Case "PropertyGetter"
                    Return importStatements & vbCrLf &
                        "Class C1" & vbCrLf &
                        "    ReadOnly Property P As Integer" & vbCrLf &
                        "        Get" & vbCrLf &
                        SurroundAndJoinLines(lines, "            ", vbCrLf) &
                        "        End Get" & vbCrLf &
                        "    End Property" & vbCrLf &
                        "End Class"

                Case "PropertyDeclaration"
                    Return importStatements & vbCrLf &
                        "Class C1" & vbCrLf &
                        "    Public Property P As String " & vbCrLf &
                        SurroundAndJoinLines(lines, "        ", vbCrLf) &
                        "    End Property" & vbCrLf &
                        "End Class"

                Case "SharedPropertyGetter"
                    Return importStatements & vbCrLf &
                        "Class C1" & vbCrLf &
                        "    Shared ReadOnly Property P As Integer" & vbCrLf &
                        "        Get" & vbCrLf &
                        SurroundAndJoinLines(lines, "            ", vbCrLf) &
                        "        End Get" & vbCrLf &
                        "    End Property" & vbCrLf &
                        "End Class"

                Case "CustomEventDeclaration"
                    Return importStatements & vbCrLf &
                        "Class C1" & vbCrLf &
                        "    Custom Event TestEvent As EventHandler" & vbCrLf &
                        SurroundAndJoinLines(lines, "        ", vbCrLf) &
                        "    End Event" & vbCrLf &
                        "End Class"

                Case "EventAddHandler"
                    Return importStatements & vbCrLf &
                        "Class C1" & vbCrLf &
                        "    Custom Event TestEvent As EventHandler" & vbCrLf &
                        "        AddHandler(value As EventHandler)" & vbCrLf &
                        SurroundAndJoinLines(lines, "            ", vbCrLf) &
                        "        End AddHandler" & vbCrLf &
                        "        RemoveHandler(value As EventHandler)" & vbCrLf &
                        "        End RemoveHandler" & vbCrLf &
                        "        RaiseEvent(sender As Object, e As EventArgs)" & vbCrLf &
                        "        End RaiseEvent" & vbCrLf &
                        "    End Event" & vbCrLf &
                        "End Class"

                Case "SharedEventAddHandler"
                    Return importStatements & vbCrLf &
                        "Class C1" & vbCrLf &
                        "    Shared Custom Event TestEvent As EventHandler" & vbCrLf &
                        "        AddHandler(value As EventHandler)" & vbCrLf &
                        SurroundAndJoinLines(lines, "            ", vbCrLf) &
                        "        End AddHandler" & vbCrLf &
                        "        RemoveHandler(value As EventHandler)" & vbCrLf &
                        "        End RemoveHandler" & vbCrLf &
                        "        RaiseEvent(sender As Object, e As EventArgs)" & vbCrLf &
                        "        End RaiseEvent" & vbCrLf &
                        "    End Event" & vbCrLf &
                        "End Class"

                Case "ModuleMethodBody"
                    Return importStatements & vbCrLf &
                        "Module M1" & vbCrLf &
                        "    Sub Method()" & vbCrLf &
                        SurroundAndJoinLines(lines, "        ", vbCrLf) &
                        "    End Sub" & vbCrLf &
                        "End Module"

                Case "StructureMethodBody"
                    Return importStatements & vbCrLf &
                        "Structure S" & vbCrLf &
                        "    Sub Method()" & vbCrLf &
                        SurroundAndJoinLines(lines, "    ", vbCrLf) &
                        "    End Sub" & vbCrLf &
                        "End Structure"

                Case "File"
                    Return testSource.NormalizedValue

                Case Else
                    Throw New ArgumentException("Unexpected testSource XML tag.", NameOf(testSource))
            End Select
        End Function
    End Module
End Namespace
