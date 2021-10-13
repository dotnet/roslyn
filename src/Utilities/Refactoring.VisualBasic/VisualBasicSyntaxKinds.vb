' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
