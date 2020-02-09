' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Green = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Friend Module SyntaxEquivalence
        Friend Function AreEquivalent(before As SyntaxTree, after As SyntaxTree, ignoreChildNode As Func(Of SyntaxKind, Boolean), topLevel As Boolean) As Boolean
            If before Is after Then
                Return True
            End If

            If before Is Nothing OrElse after Is Nothing Then
                Return False
            End If

            Return AreEquivalent(before.GetRoot(), after.GetRoot(), ignoreChildNode, topLevel)
        End Function

        Public Function AreEquivalent(before As SyntaxNode, after As SyntaxNode, ignoreChildNode As Func(Of SyntaxKind, Boolean), topLevel As Boolean) As Boolean
            Debug.Assert(Not topLevel OrElse ignoreChildNode Is Nothing)

            If before Is Nothing OrElse after Is Nothing Then
                Return before Is after
            End If

            Return AreEquivalentRecursive(before.Green, after.Green, parentKind:=Nothing, ignoreChildNode:=ignoreChildNode, topLevel:=topLevel)
        End Function

        Public Function AreEquivalent(before As SyntaxTokenList, after As SyntaxTokenList) As Boolean
            Return AreEquivalentRecursive(before.Node, after.Node, parentKind:=Nothing, ignoreChildNode:=Nothing, topLevel:=False)
        End Function

        Public Function AreEquivalent(before As SyntaxToken, after As SyntaxToken) As Boolean
            Return before.RawKind = after.RawKind AndAlso (before.Node Is Nothing OrElse AreTokensEquivalent(before.Node, after.Node))
        End Function

        Private Function AreTokensEquivalent(before As GreenNode, after As GreenNode) As Boolean
            Debug.Assert(before.RawKind = after.RawKind)

            If before.IsMissing <> after.IsMissing Then
                Return False
            End If

            Select Case CType(before.RawKind, SyntaxKind)
                Case SyntaxKind.IdentifierToken,
                     SyntaxKind.CharacterLiteralToken,
                     SyntaxKind.DateLiteralToken,
                     SyntaxKind.DecimalLiteralToken,
                     SyntaxKind.FloatingLiteralToken,
                     SyntaxKind.IntegerLiteralToken,
                     SyntaxKind.InterpolatedStringTextToken,
                     SyntaxKind.StringLiteralToken
                    Return String.Equals(DirectCast(before, Green.SyntaxToken).Text,
                                         DirectCast(after, Green.SyntaxToken).Text,
                                         StringComparison.Ordinal)
            End Select

            Return True
        End Function

        Private Function AreEquivalentRecursive(before As GreenNode, after As GreenNode, parentKind As SyntaxKind, ignoreChildNode As Func(Of SyntaxKind, Boolean), topLevel As Boolean) As Boolean
            If before Is after Then
                Return True
            End If

            If before Is Nothing OrElse after Is Nothing Then
                Return False
            End If

            If before.RawKind <> after.RawKind Then
                Return False
            End If

            If before.IsToken Then
                Debug.Assert(after.IsToken)
                Return AreTokensEquivalent(before, after)
            End If

            Dim kind = CType(before.RawKind, SyntaxKind)

            If Not AreModifiersEquivalent(before, after, kind) Then
                Return False
            End If

            If topLevel Then
                Select Case kind
                    Case SyntaxKind.SubBlock,
                         SyntaxKind.FunctionBlock,
                         SyntaxKind.ConstructorBlock,
                         SyntaxKind.OperatorBlock,
                         SyntaxKind.GetAccessorBlock,
                         SyntaxKind.SetAccessorBlock,
                         SyntaxKind.AddHandlerAccessorBlock,
                         SyntaxKind.RemoveHandlerAccessorBlock,
                         SyntaxKind.RaiseEventAccessorBlock
                        ' Once we get down to the block level we need to only compare the header
                        Return AreEquivalentRecursive(DirectCast(before, Green.MethodBlockBaseSyntax).Begin,
                                                      DirectCast(after, Green.MethodBlockBaseSyntax).Begin,
                                                      kind,
                                                      ignoreChildNode:=Nothing,
                                                      topLevel:=True)

                    Case SyntaxKind.FieldDeclaration
                        ' If we're only checking top level equivalence, then we don't have to go down into
                        ' the initializer for a field. However, we can't put that optimization for all
                        ' fields. For example, fields that are 'const' do need their initializers checked as
                        ' changing them can affect binding results.
                        Dim fieldBefore = DirectCast(before, Green.FieldDeclarationSyntax)
                        Dim fieldAfter = DirectCast(after, Green.FieldDeclarationSyntax)
                        Dim isConstBefore = fieldBefore.Modifiers.Any(SyntaxKind.ConstKeyword)
                        Dim isConstAfter = fieldAfter.Modifiers.Any(SyntaxKind.ConstKeyword)
                        If Not isConstBefore AndAlso Not isConstAfter Then
                            ignoreChildNode = Function(childKind) childKind = SyntaxKind.EqualsValue OrElse childKind = SyntaxKind.AsNewClause
                        End If

                    Case SyntaxKind.EqualsValue
                        ' Don't recurse into an auto-property initializer.
                        If parentKind = SyntaxKind.PropertyStatement Then
                            Return True
                        End If
                End Select
            End If

            If ignoreChildNode IsNot Nothing Then
                Dim e1 = DirectCast(before, Green.VisualBasicSyntaxNode).ChildNodesAndTokens().GetEnumerator()
                Dim e2 = DirectCast(after, Green.VisualBasicSyntaxNode).ChildNodesAndTokens().GetEnumerator()

                While True
                    Dim child1 As GreenNode = Nothing
                    Dim child2 As GreenNode = Nothing

                    ' skip ignored children
                    While e1.MoveNext()
                        Dim c = e1.Current
                        If c IsNot Nothing AndAlso (c.IsToken OrElse Not ignoreChildNode(CType(c.RawKind, SyntaxKind))) Then
                            child1 = c
                            Exit While
                        End If
                    End While

                    While e2.MoveNext()
                        Dim c = e2.Current

                        If c IsNot Nothing AndAlso (c.IsToken OrElse Not ignoreChildNode(CType(c.RawKind, SyntaxKind))) Then
                            child2 = c
                            Exit While
                        End If
                    End While

                    If child1 Is Nothing OrElse child2 Is Nothing Then
                        ' false if some children remained
                        Return child1 Is child2
                    End If

                    If Not AreEquivalentRecursive(child1, child2, kind, ignoreChildNode, topLevel) Then
                        Return False
                    End If
                End While

                Throw ExceptionUtilities.Unreachable
            Else
                ' simple comparison - not ignoring children
                Dim slotCount1 = before.SlotCount

                If slotCount1 <> after.SlotCount Then
                    Return False
                End If

                For i = 0 To slotCount1 - 1
                    Dim child1 = before.GetSlot(i)
                    Dim child2 = after.GetSlot(i)
                    If Not AreEquivalentRecursive(child1, child2, kind, ignoreChildNode, topLevel) Then
                        Return False
                    End If
                Next

                Return True
            End If
        End Function

        Private Function AreModifiersEquivalent(before As GreenNode, after As GreenNode, kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock
                    Dim beforeModifiers = DirectCast(before, Green.MethodBlockBaseSyntax).Begin.Modifiers
                    Dim afterModifiers = DirectCast(after, Green.MethodBlockBaseSyntax).Begin.Modifiers

                    If beforeModifiers.Count <> afterModifiers.Count Then
                        Return False
                    End If

                    For i = 0 To beforeModifiers.Count - 1
                        If Not beforeModifiers.Any(afterModifiers(i).Kind) Then
                            Return False
                        End If
                    Next
            End Select

            Return True
        End Function

    End Module
End Namespace
