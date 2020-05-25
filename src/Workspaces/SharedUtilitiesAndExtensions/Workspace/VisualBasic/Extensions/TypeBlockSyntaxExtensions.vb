' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module TypeBlockSyntaxExtensions
        <Extension>
        Public Function GetInsertionIndices(destination As TypeBlockSyntax,
                                            cancellationToken As CancellationToken) As IList(Of Boolean)
            Dim members = destination.Members

            Dim indices = New List(Of Boolean)
            If members.Count = 0 Then
                Dim start = destination.BlockStatement.Span.End
                Dim [end] = destination.EndBlockStatement.SpanStart

                indices.Add(Not destination.OverlapsHiddenPosition(destination.BlockStatement, destination.EndBlockStatement, cancellationToken))
            Else
                ' First, see if we can insert between the start of the typeblock, and it's first
                ' member.
                indices.Add(Not destination.OverlapsHiddenPosition(destination.BlockStatement, destination.Members.First, cancellationToken))

                ' Now, walk between each member and see if something can be inserted between it and
                ' the next member
                For i = 0 To members.Count - 2
                    Dim member1 = members(i)
                    Dim member2 = members(i + 1)

                    indices.Add(Not destination.OverlapsHiddenPosition(member1, member2, cancellationToken))
                Next

                ' Last, see if we can insert between the last member and the end of the typeblock
                indices.Add(Not destination.OverlapsHiddenPosition(destination.Members.Last, destination.EndBlockStatement, cancellationToken))
            End If

            Return indices
        End Function

        Private Function ReplaceTrailingColonToEndOfLineTrivia(Of TNode As SyntaxNode)(node As TNode) As TNode
            Return node.WithTrailingTrivia(node.GetTrailingTrivia().Select(Function(t) If(t.Kind = SyntaxKind.ColonTrivia, SyntaxFactory.ElasticCarriageReturnLineFeed, t)))
        End Function

        Private Function EnsureProperList(Of TSyntax As SyntaxNode)(list As SyntaxList(Of TSyntax)) As SyntaxList(Of TSyntax)
            Dim allElements = list
            If Not allElements.Last().GetTrailingTrivia().Any(Function(t) t.Kind = SyntaxKind.EndOfLineTrivia OrElse t.Kind = SyntaxKind.ColonTrivia) Then
                Return SyntaxFactory.SingletonList(Of TSyntax)(
                    allElements.Last().WithAppendedTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed))
            ElseIf allElements.Last().GetTrailingTrivia().Any(Function(t) t.Kind = SyntaxKind.ColonTrivia) Then
                Return SyntaxFactory.List(Of TSyntax)(
                    allElements.Take(allElements.Count - 1).Concat(ReplaceTrailingColonToEndOfLineTrivia(allElements.Last())))
            End If

            Return list
        End Function

        Private Function EnsureProperInherits(destinationType As TypeBlockSyntax) As SyntaxList(Of InheritsStatementSyntax)
            Dim allElements = destinationType.Inherits
            If allElements.Count > 0 AndAlso
               destinationType.Implements.Count = 0 Then
                Return EnsureProperList(destinationType.Inherits)
            End If

            Return destinationType.Inherits
        End Function

        Private Function EnsureProperImplements(destinationType As TypeBlockSyntax) As SyntaxList(Of ImplementsStatementSyntax)
            Dim allElements = destinationType.Implements
            If allElements.Count > 0 Then
                Return EnsureProperList(destinationType.Implements)
            End If

            Return destinationType.Implements
        End Function

        Private Function EnsureProperBegin(destinationType As TypeBlockSyntax) As TypeStatementSyntax
            If destinationType.Inherits.Count = 0 AndAlso
               destinationType.Implements.Count = 0 AndAlso
               destinationType.BlockStatement.GetTrailingTrivia().Any(Function(t) t.Kind = SyntaxKind.ColonTrivia) Then
                Return ReplaceTrailingColonToEndOfLineTrivia(destinationType.BlockStatement)
            End If

            Return destinationType.BlockStatement
        End Function

        Private Function EnsureEndTokens(destinationType As TypeBlockSyntax) As EndBlockStatementSyntax
            If destinationType.EndBlockStatement.IsMissing Then
                Select Case destinationType.Kind
                    Case SyntaxKind.ClassBlock
                        Return SyntaxFactory.EndClassStatement().WithAdditionalAnnotations(Formatter.Annotation)
                    Case SyntaxKind.InterfaceBlock
                        Return SyntaxFactory.EndInterfaceStatement().WithAdditionalAnnotations(Formatter.Annotation)
                    Case SyntaxKind.StructureBlock
                        Return SyntaxFactory.EndStructureStatement().WithAdditionalAnnotations(Formatter.Annotation)
                End Select
            End If

            Return destinationType.EndBlockStatement
        End Function

        <Extension>
        Public Function FixTerminators(destinationType As TypeBlockSyntax) As TypeBlockSyntax
            Return destinationType.WithInherits(EnsureProperInherits(destinationType)).
                                   WithImplements(EnsureProperImplements(destinationType)).
                                   WithBlockStatement(EnsureProperBegin(destinationType)).
                                   WithEndBlockStatement(EnsureEndTokens(destinationType))
        End Function
    End Module
End Namespace
