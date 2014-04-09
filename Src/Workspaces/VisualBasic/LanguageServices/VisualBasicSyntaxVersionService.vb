' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ISyntaxVersionLanguageService), LanguageNames.VisualBasic)>
    Friend Class VisualBasicSyntaxVersionLanguageService
        Implements ISyntaxVersionLanguageService

        Public Function ComputePublicHash(root As SyntaxNode, cancellationToken As CancellationToken) As Integer Implements ISyntaxVersionLanguageService.ComputePublicHash
            Dim result As Integer

            Dim computer = SharedPools.Default(Of PublicHashComputer)().Allocate()
            Try
                result = computer.ComputeHash(root, cancellationToken)
            Finally
                SharedPools.Default(Of PublicHashComputer)().Free(computer)
            End Try

            Return result
        End Function

        Private Class PublicHashComputer
            Inherits VisualBasicSyntaxWalker

            Private _hash As Integer
            Private _cancellationToken As CancellationToken

            Public Sub New()
                MyBase.New(SyntaxWalkerDepth.Token)
            End Sub

            Public Function ComputeHash(node As SyntaxNode, cancellationToken As CancellationToken) As Integer
                Me._hash = 0
                Me._cancellationToken = cancellationToken
                Visit(node)
                Return Me._hash
            End Function

            Public Overrides Sub Visit(node As SyntaxNode)

                Me._cancellationToken.ThrowIfCancellationRequested()

                If node.Parent IsNot Nothing Then

                    ' ignore statements of method bodies
                    Dim mb = TryCast(node, MethodBlockBaseSyntax)
                    If mb IsNot Nothing Then
                        Me.Visit(mb.Begin)
                        Me.Visit(mb.End)
                        Return
                    End If

                    ' non-const field initializers are not considered
                    If node.Parent.VisualBasicKind = SyntaxKind.EqualsValue AndAlso
                        node.Parent.Parent.VisualBasicKind = SyntaxKind.VariableDeclarator AndAlso
                        node.Parent.Parent.Parent.VisualBasicKind = SyntaxKind.FieldDeclaration Then
                        Dim fd = DirectCast(node.Parent.Parent.Parent, FieldDeclarationSyntax)
                        If Not fd.Modifiers.Any(SyntaxKind.ConstKeyword) Then
                            Return
                        End If
                    End If

                End If

                MyBase.Visit(node)
            End Sub

            Public Overrides Sub VisitToken(token As SyntaxToken)

                ' trivia is not considered, only the raw form of the token

                Me._hash = Hash.Combine(CType(token.VisualBasicKind, Integer), Me._hash)

                Select Case token.VisualBasicKind
                    Case SyntaxKind.IdentifierToken,
                         SyntaxKind.CharacterLiteralToken,
                         SyntaxKind.DateLiteralToken,
                         SyntaxKind.DecimalLiteralToken,
                         SyntaxKind.FloatingLiteralToken,
                         SyntaxKind.IntegerLiteralToken,
                         SyntaxKind.StringLiteralToken
                        Me._hash = Hash.Combine(token.ToString().GetHashCode(), Me._hash)
                End Select
            End Sub
        End Class
    End Class
End Namespace
