' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the DeclarationContext
'-----------------------------------------------------------------------------
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Class TypeBlockContext
        Inherits DeclarationContext

        Protected _inheritsDecls As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of InheritsStatementSyntax)
        Private _implementsDecls As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of ImplementsStatementSyntax)
        Protected _state As SyntaxKind

        Friend Sub New(contextKind As SyntaxKind, statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(contextKind, statement, prevContext)

            Debug.Assert(contextKind = SyntaxKind.ModuleBlock OrElse contextKind = SyntaxKind.ClassBlock OrElse
                              contextKind = SyntaxKind.StructureBlock OrElse contextKind = SyntaxKind.InterfaceBlock)

            Debug.Assert(BlockKind = SyntaxKind.ModuleBlock OrElse BlockKind = SyntaxKind.ClassBlock OrElse
                               BlockKind = SyntaxKind.StructureBlock OrElse BlockKind = SyntaxKind.InterfaceBlock)

            _state = SyntaxKind.None
        End Sub

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext

            Do
                Select Case _state
                    Case SyntaxKind.None
                        Select Case node.Kind
                            Case SyntaxKind.InheritsStatement
                                _state = SyntaxKind.InheritsStatement

                            Case SyntaxKind.ImplementsStatement
                                _state = SyntaxKind.ImplementsStatement

                            Case Else
                                _state = SyntaxKind.ClassStatement
                        End Select

                    Case SyntaxKind.InheritsStatement
                        Select Case node.Kind
                            Case SyntaxKind.InheritsStatement
                                Add(node)
                                Exit Do

                            Case Else
                                _inheritsDecls = BaseDeclarations(Of InheritsStatementSyntax)()
                                _state = SyntaxKind.ImplementsStatement
                        End Select

                    Case SyntaxKind.ImplementsStatement
                        Select Case node.Kind
                            Case SyntaxKind.ImplementsStatement
                                Add(node)
                                Exit Do

                            Case Else
                                _implementsDecls = BaseDeclarations(Of ImplementsStatementSyntax)()
                                _state = SyntaxKind.ClassStatement ' done with base decls
                        End Select

                    Case Else
                        Return MyBase.ProcessSyntax(node)

                End Select
            Loop

            Return Me
        End Function

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode

            Dim beginBlockStmt As TypeStatementSyntax = Nothing
            Dim endBlockStmt As EndBlockStatementSyntax = DirectCast(endStmt, EndBlockStatementSyntax)
            GetBeginEndStatements(beginBlockStmt, endBlockStmt)

            If _state <> SyntaxKind.ClassStatement Then
                Select Case _state
                    Case SyntaxKind.InheritsStatement
                        _inheritsDecls = BaseDeclarations(Of InheritsStatementSyntax)()

                    Case SyntaxKind.ImplementsStatement
                        _implementsDecls = BaseDeclarations(Of ImplementsStatementSyntax)()
                End Select
                _state = SyntaxKind.ClassStatement
            End If

            Dim result = InternalSyntaxFactory.TypeBlock(BlockKind, beginBlockStmt,
                                          _inheritsDecls,
                                          _implementsDecls,
                                          Body(),
                                          endBlockStmt)

            FreeStatements()

            Return result
        End Function

    End Class

End Namespace
