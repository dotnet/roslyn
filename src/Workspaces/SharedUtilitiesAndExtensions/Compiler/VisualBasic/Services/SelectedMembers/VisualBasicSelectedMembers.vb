' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageServices
    Friend Class VisualBasicSelectedMembers
        Inherits AbstractSelectedMembers

        Public Shared ReadOnly Instance As New VisualBasicSelectedMembers()

        Private Sub New()
        End Sub

        Public Async Function GetSelectedFieldsAndPropertiesAsync(
                tree As SyntaxTree,
                textSpan As TextSpan,
                allowPartialSelection As Boolean,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SyntaxNode))

            Dim text = Await tree.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim root = Await tree.GetRootAsync(cancellationToken).ConfigureAwait(False)

            ' If there Is a selection, look for the token to the right of the selection That helps
            ' the user select Like so:
            '
            '          dim i as integer[|
            '          dim j as integer|]
            '
            ' In this case (which Is common with a mouse), we want to consider 'j' selected, and
            ' 'i' not involved in all.
            '
            ' However, if there Is no selection And the user has:
            '
            '          dim i as integer$$
            '          dim j as integer
            '
            ' Then we want to consider 'i' selected instead.  So we do a normal FindToken.

            Dim token = If(textSpan.IsEmpty,
                root.FindToken(textSpan.Start),
                root.FindTokenOnRightOfPosition(textSpan.Start))

            Dim firstMember = token.GetAncestors(Of StatementSyntax).
                                    Where(Function(s) TypeOf s.Parent Is TypeBlockSyntax).
                                    FirstOrDefault()
            If firstMember IsNot Nothing Then
                Dim containingType = DirectCast(firstMember.Parent, TypeBlockSyntax)
                If containingType IsNot Nothing AndAlso
                   firstMember IsNot containingType.BlockStatement AndAlso
                   firstMember IsNot containingType.EndBlockStatement Then
                    Return GetFieldsAndPropertiesInSpan(root, text, textSpan, containingType, firstMember, allowPartialSelection)
                End If
            End If

            Return ImmutableArray(Of SyntaxNode).Empty
        End Function

        Private Function GetFieldsAndPropertiesInSpan(
            root As SyntaxNode,
            text As SourceText,
            textSpan As TextSpan,
            containingType As TypeBlockSyntax,
            firstMember As StatementSyntax,
            allowPartialSelection As Boolean) As ImmutableArray(Of SyntaxNode)
            Dim selectedMembers = ArrayBuilder(Of SyntaxNode).GetInstance()

            Try
                Dim members = containingType.Members
                Dim fieldIndex = members.IndexOf(firstMember)
                If fieldIndex < 0 Then
                    Return ImmutableArray(Of SyntaxNode).Empty
                End If

                For i = fieldIndex To members.Count - 1
                    Dim member = members(i)
                    AddSelectedFieldOrPropertyDeclarations(root, text, textSpan, selectedMembers, member, allowPartialSelection)
                Next

                Return selectedMembers.ToImmutable()
            Finally
                selectedMembers.Free()
            End Try
        End Function

        Private Sub AddAllMembers(
                selectedMembers As ArrayBuilder(Of SyntaxNode),
                member As SyntaxNode)

            If member.IsKind(SyntaxKind.FieldDeclaration) Then
                Dim fieldDeclaration = DirectCast(member, FieldDeclarationSyntax)
                Dim fieldNames = fieldDeclaration.Declarators.SelectMany(Function(d) d.Names)
                selectedMembers.AddRange(fieldNames)
            ElseIf member.IsKind(SyntaxKind.PropertyStatement) Then
                selectedMembers.Add(member)
            End If
        End Sub

        Private Sub AddSelectedFieldOrPropertyDeclarations(
                root As SyntaxNode,
                text As SourceText,
                textSpan As TextSpan,
                selectedMembers As ArrayBuilder(Of SyntaxNode),
                member As StatementSyntax,
                allowPartialSelection As Boolean)
            If Not member.IsKind(SyntaxKind.FieldDeclaration, SyntaxKind.PropertyStatement) Then
                Return
            End If

            ' first, check if entire member is selected
            If textSpan.Contains(member.Span) Then
                AddAllMembers(selectedMembers, member)
                Return
            End If

            If textSpan.IsEmpty Then
                ' No selection.  We consider this member selected if a few cases are true:
                '
                '  1. Position precedes the first token of the member (on the same line).
                '  2. Position touches the name of the field/prop.
                '  3. Position Is after the last token of the member (on the same line).

                Dim position = textSpan.Start
                If IsBeforeOrAfterNodeOnSameLine(text, root, member, position) Then
                    AddAllMembers(selectedMembers, member)
                    Return
                Else
                    If member.IsKind(SyntaxKind.FieldDeclaration) Then
                        Dim fieldDeclaration = DirectCast(member, FieldDeclarationSyntax)
                        For Each declarator In fieldDeclaration.Declarators
                            For Each name In declarator.Names
                                If name.FullSpan.IntersectsWith(position) Then
                                    selectedMembers.Add(name)
                                End If
                            Next
                        Next
                    ElseIf member.IsKind(SyntaxKind.PropertyStatement) Then
                        If DirectCast(member, PropertyStatementSyntax).Identifier.FullSpan.IntersectsWith(position) Then
                            selectedMembers.Add(member)
                        End If
                    End If
                End If
            Else
                ' if the user has an actual selection, get the fields/props if the selection
                ' surrounds the names of in the case of allowPartialSelection.

                If Not allowPartialSelection Then
                    Return
                End If

                ' next, check if identifier is at lease partially selected
                If member.IsKind(SyntaxKind.FieldDeclaration) Then
                    Dim fieldDeclaration = DirectCast(member, FieldDeclarationSyntax)
                    For Each declarator In fieldDeclaration.Declarators
                        For Each name In declarator.Names
                            If textSpan.OverlapsWith(name.Span) Then
                                selectedMembers.Add(name)
                            End If
                        Next
                    Next
                ElseIf member.IsKind(SyntaxKind.PropertyStatement) Then
                    If textSpan.OverlapsWith(DirectCast(member, PropertyStatementSyntax).Identifier.Span) Then
                        selectedMembers.Add(member)
                    End If
                End If
            End If
        End Sub

    End Class
End Namespace
