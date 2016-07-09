' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class LocalRewriter

        Public Overrides Function VisitBlock(node As BoundBlock) As BoundNode

            Dim original As BoundBlock = node

            ' Static locals should be removed from the list of locals,
            ' they are replaced with fields.
            If Not node.Locals.IsEmpty Then
                Dim i As Integer
                Dim builder As ArrayBuilder(Of LocalSymbol) = Nothing

                For i = 0 To node.Locals.Length - 1
                    If node.Locals(i).IsStatic Then
                        builder = ArrayBuilder(Of LocalSymbol).GetInstance()
                        Exit For
                    End If
                Next

                If builder IsNot Nothing Then
                    builder.AddRange(node.Locals, i)

                    For i = i + 1 To node.Locals.Length - 1
                        If Not node.Locals(i).IsStatic Then
                            builder.Add(node.Locals(i))
                        End If
                    Next

                    Debug.Assert(builder.Count < node.Locals.Length)

                    node = node.Update(node.StatementListSyntax, builder.ToImmutableAndFree(), node.Statements)
                End If
            End If

            If Instrument Then
                Dim builder = ArrayBuilder(Of BoundStatement).GetInstance()

                For Each s In node.Statements
                    Dim rewrittenStatement = TryCast(Visit(s), BoundStatement)
                    If rewrittenStatement IsNot Nothing Then
                        builder.Add(rewrittenStatement)
                    End If
                Next

                Dim synthesizedLocal As LocalSymbol = Nothing
                Dim prologue As BoundStatement = _instrumenter.CreateBlockPrologue(original, node, synthesizedLocal)
                If prologue IsNot Nothing Then
                    builder.Insert(0, prologue)
                End If

                Return New BoundBlock(node.Syntax, node.StatementListSyntax, If(synthesizedLocal Is Nothing, node.Locals, node.Locals.Add(synthesizedLocal)), builder.ToImmutableAndFree())
            End If

            Return MyBase.VisitBlock(node)
        End Function
    End Class
End Namespace
