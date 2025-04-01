' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Global.Analyzer.Utilities

    Friend NotInheritable Class VisualBasicSyntaxKinds
        Implements ISyntaxKinds

        Public Shared ReadOnly Property Instance As New VisualBasicSyntaxKinds()

        Private Sub New()
        End Sub

        Public ReadOnly Property EndOfFileToken As Integer Implements ISyntaxKinds.EndOfFileToken
            Get
                Return SyntaxKind.EndOfFileToken
            End Get
        End Property

        Public ReadOnly Property ExpressionStatement As Integer Implements ISyntaxKinds.ExpressionStatement
            Get
                Return SyntaxKind.ExpressionStatement
            End Get
        End Property

        Public ReadOnly Property LocalDeclarationStatement As Integer Implements ISyntaxKinds.LocalDeclarationStatement
            Get
                Return SyntaxKind.LocalDeclarationStatement
            End Get
        End Property

        Public ReadOnly Property VariableDeclarator As Integer Implements ISyntaxKinds.VariableDeclarator
            Get
                Return SyntaxKind.VariableDeclarator
            End Get
        End Property
    End Class

End Namespace
