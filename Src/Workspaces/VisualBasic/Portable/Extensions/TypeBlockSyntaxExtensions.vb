' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module TypeBlockSyntaxExtensions
        <Extension>
        Public Function WithInherits(node As TypeBlockSyntax, list As SyntaxList(Of InheritsStatementSyntax)) As TypeBlockSyntax
            Select Case node.VBKind
                Case SyntaxKind.ModuleBlock
                    Return DirectCast(node, ModuleBlockSyntax).WithInherits(list)
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).WithInherits(list)
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).WithInherits(list)
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).WithInherits(list)
            End Select

            Throw ExceptionUtilities.Unreachable
        End Function

        <Extension>
        Public Function WithImplements(node As TypeBlockSyntax, list As SyntaxList(Of ImplementsStatementSyntax)) As TypeBlockSyntax
            Select Case node.VBKind
                Case SyntaxKind.ModuleBlock
                    Return DirectCast(node, ModuleBlockSyntax).WithImplements(list)
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).WithImplements(list)
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).WithImplements(list)
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).WithImplements(list)
            End Select

            Throw ExceptionUtilities.Unreachable
        End Function

        <Extension>
        Public Function AddMembers(node As TypeBlockSyntax, ParamArray members As StatementSyntax()) As TypeBlockSyntax
            Select Case node.VBKind
                Case SyntaxKind.ModuleBlock
                    Return DirectCast(node, ModuleBlockSyntax).AddMembers(members)
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).AddMembers(members)
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).AddMembers(members)
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).AddMembers(members)
            End Select

            Throw ExceptionUtilities.Unreachable
        End Function

        <Extension>
        Public Function WithMembers(node As TypeBlockSyntax, members As SyntaxList(Of StatementSyntax)) As TypeBlockSyntax
            Select Case node.VBKind
                Case SyntaxKind.ModuleBlock
                    Return DirectCast(node, ModuleBlockSyntax).WithMembers(members)
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).WithMembers(members)
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).WithMembers(members)
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).WithMembers(members)
            End Select

            Throw ExceptionUtilities.Unreachable
        End Function

        <Extension>
        Public Function WithBegin(node As TypeBlockSyntax, [begin] As TypeStatementSyntax) As TypeBlockSyntax
            Select Case node.VBKind
                Case SyntaxKind.ModuleBlock
                    Return DirectCast(node, ModuleBlockSyntax).WithBegin(DirectCast([begin], ModuleStatementSyntax))
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).WithBegin(DirectCast([begin], InterfaceStatementSyntax))
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).WithBegin(DirectCast([begin], StructureStatementSyntax))
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).WithBegin(DirectCast([begin], ClassStatementSyntax))
            End Select

            Throw ExceptionUtilities.Unreachable
        End Function

        <Extension>
        Public Function WithEnd(node As TypeBlockSyntax, [end] As EndBlockStatementSyntax) As TypeBlockSyntax
            Select Case node.VBKind
                Case SyntaxKind.ModuleBlock
                    Return DirectCast(node, ModuleBlockSyntax).WithEnd([end])
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).WithEnd([end])
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).WithEnd([end])
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).WithEnd([end])
            End Select

            Throw ExceptionUtilities.Unreachable
        End Function

        <Extension>
        Public Function GetInsertionIndices(destination As TypeBlockSyntax,
                                            cancellationToken As CancellationToken) As IList(Of Boolean)
            Dim members = destination.Members

            Dim indices = New List(Of Boolean)
            If members.Count = 0 Then
                Dim start = destination.Begin.Span.End
                Dim [end] = destination.End.SpanStart

                indices.Add(Not destination.OverlapsHiddenPosition(destination.Begin, destination.End, cancellationToken))
            Else
                ' First, see if we can insert between the start of the typeblock, and it's first
                ' member.
                indices.Add(Not destination.OverlapsHiddenPosition(destination.Begin, destination.Members.First, cancellationToken))

                ' Now, walk between each member and see if something can be inserted between it and
                ' the next member
                For i = 0 To members.Count - 2
                    Dim member1 = members(i)
                    Dim member2 = members(i + 1)

                    indices.Add(Not destination.OverlapsHiddenPosition(member1, member2, cancellationToken))
                Next

                ' Last, see if we can insert between the last member and the end of the typeblock
                indices.Add(Not destination.OverlapsHiddenPosition(destination.Members.Last, destination.End, cancellationToken))
            End If

            Return indices
        End Function

        Private Function ReplaceTrailingColonToEndOfLineTrivia(Of TNode As SyntaxNode)(node As TNode) As TNode
            Return node.WithTrailingTrivia(node.GetTrailingTrivia().Select(Function(t) If(t.VBKind = SyntaxKind.ColonTrivia, SyntaxFactory.CarriageReturnLineFeed, t)))
        End Function

        Private Function EnsureProperList(Of TSyntax As SyntaxNode)(list As SyntaxList(Of TSyntax)) As SyntaxList(Of TSyntax)
            Dim allElements = list
            If Not allElements.Last().GetTrailingTrivia().Any(Function(t) t.VBKind = SyntaxKind.EndOfLineTrivia OrElse t.VBKind = SyntaxKind.ColonTrivia) Then
                Return SyntaxFactory.SingletonList(Of TSyntax)(
                    allElements.Last().WithAppendedTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))
            ElseIf allElements.Last().GetTrailingTrivia().Any(Function(t) t.VBKind = SyntaxKind.ColonTrivia) Then
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
               destinationType.Begin.GetTrailingTrivia().Any(Function(t) t.VBKind = SyntaxKind.ColonTrivia) Then
                Return ReplaceTrailingColonToEndOfLineTrivia(destinationType.Begin)
            End If

            Return destinationType.Begin
        End Function

        Private Function EnsureEndTokens(destinationType As TypeBlockSyntax) As EndBlockStatementSyntax
            If destinationType.End.IsMissing Then
                Select Case destinationType.VBKind
                    Case SyntaxKind.ClassBlock
                        Return SyntaxFactory.EndClassStatement().WithAdditionalAnnotations(Formatter.Annotation)
                    Case SyntaxKind.InterfaceBlock
                        Return SyntaxFactory.EndInterfaceStatement().WithAdditionalAnnotations(Formatter.Annotation)
                    Case SyntaxKind.StructureBlock
                        Return SyntaxFactory.EndStructureStatement().WithAdditionalAnnotations(Formatter.Annotation)
                End Select
            End If

            Return destinationType.End
        End Function

        <Extension>
        Public Function FixTerminators(destinationType As TypeBlockSyntax) As TypeBlockSyntax
            Return destinationType.WithInherits(EnsureProperInherits(destinationType)).
                                   WithImplements(EnsureProperImplements(destinationType)).
                                   WithBegin(EnsureProperBegin(destinationType)).
                                   WithEnd(EnsureEndTokens(destinationType))
        End Function
    End Module
End Namespace