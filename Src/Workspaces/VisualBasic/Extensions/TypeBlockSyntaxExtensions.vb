' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module TypeBlockSyntaxExtensions
        <Extension>
        Public Function WithInherits(node As TypeBlockSyntax, list As SyntaxList(Of InheritsStatementSyntax)) As TypeBlockSyntax
            Select Case node.VisualBasicKind
                Case SyntaxKind.ModuleBlock
                    Return DirectCast(node, ModuleBlockSyntax).WithInherits(list)
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).WithInherits(list)
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).WithInherits(list)
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).WithInherits(list)
            End Select

            Throw Contract.Unreachable
        End Function

        <Extension>
        Public Function WithImplements(node As TypeBlockSyntax, list As SyntaxList(Of ImplementsStatementSyntax)) As TypeBlockSyntax
            Select Case node.VisualBasicKind
                Case SyntaxKind.ModuleBlock
                    Return DirectCast(node, ModuleBlockSyntax).WithImplements(list)
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).WithImplements(list)
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).WithImplements(list)
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).WithImplements(list)
            End Select

            Throw Contract.Unreachable
        End Function

        <Extension>
        Public Function WithMembers(node As TypeBlockSyntax, members As SyntaxList(Of StatementSyntax)) As TypeBlockSyntax
            Select Case node.VisualBasicKind
                Case SyntaxKind.ModuleBlock
                    Return DirectCast(node, ModuleBlockSyntax).WithMembers(members)
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).WithMembers(members)
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).WithMembers(members)
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).WithMembers(members)
            End Select

            Throw Contract.Unreachable
        End Function

        <Extension>
        Public Function WithBegin(node As TypeBlockSyntax, [begin] As TypeStatementSyntax) As TypeBlockSyntax
            Select Case node.VisualBasicKind
                Case SyntaxKind.ModuleBlock
                    Return DirectCast(node, ModuleBlockSyntax).WithBegin(DirectCast([begin], ModuleStatementSyntax))
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).WithBegin(DirectCast([begin], InterfaceStatementSyntax))
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).WithBegin(DirectCast([begin], StructureStatementSyntax))
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).WithBegin(DirectCast([begin], ClassStatementSyntax))
            End Select

            Throw Contract.Unreachable
        End Function

        <Extension>
        Public Function WithEnd(node As TypeBlockSyntax, [end] As EndBlockStatementSyntax) As TypeBlockSyntax
            Select Case node.VisualBasicKind
                Case SyntaxKind.ModuleBlock
                    Return DirectCast(node, ModuleBlockSyntax).WithEnd([end])
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).WithEnd([end])
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).WithEnd([end])
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).WithEnd([end])
            End Select

            Throw Contract.Unreachable
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
    End Module
End Namespace