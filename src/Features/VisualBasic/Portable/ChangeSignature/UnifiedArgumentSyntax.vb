' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ChangeSignature
    Friend Class UnifiedArgumentSyntax
        Implements IUnifiedArgumentSyntax

        Private ReadOnly _argument As ArgumentSyntax
        Private ReadOnly _index As Integer

        Private Sub New(argument As ArgumentSyntax, index As Integer)
            Debug.Assert(argument.IsKind(SyntaxKind.SimpleArgument))
            Me._argument = argument
            Me._index = index
        End Sub

        Public Shared Function Create(argument As ArgumentSyntax, index As Integer) As IUnifiedArgumentSyntax
            Return New UnifiedArgumentSyntax(argument, index)
        End Function

        Private ReadOnly Property IsDefault As Boolean Implements IUnifiedArgumentSyntax.IsDefault
            Get
                Return _argument Is Nothing
            End Get
        End Property

        Private ReadOnly Property IsNamed As Boolean Implements IUnifiedArgumentSyntax.IsNamed
            Get
                Return _argument.IsNamed
            End Get
        End Property

        Private ReadOnly Property Index As Integer Implements IUnifiedArgumentSyntax.Index
            Get
                Return _index
            End Get
        End Property

        Public Shared Widening Operator CType(ByVal unified As UnifiedArgumentSyntax) As ArgumentSyntax
            Return unified._argument
        End Operator

        Public Function GetName() As String Implements IUnifiedArgumentSyntax.GetName
            Return If(_argument.IsNamed,
                      DirectCast(_argument, SimpleArgumentSyntax).NameColonEquals.Name.Identifier.ToString(),
                      Nothing)
        End Function

        Private Function WithName(name As String) As IUnifiedArgumentSyntax Implements IUnifiedArgumentSyntax.WithName
            Return If(_argument.IsNamed,
                      Create(DirectCast(_argument, SimpleArgumentSyntax).WithNameColonEquals(DirectCast(_argument, SimpleArgumentSyntax).NameColonEquals.WithName(SyntaxFactory.IdentifierName(name))), _index),
                      Create(SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName(name)), _argument.GetExpression()), _index))
        End Function

        Public Function WithAdditionalAnnotations(annotation As SyntaxAnnotation) As IUnifiedArgumentSyntax Implements IUnifiedArgumentSyntax.WithAdditionalAnnotations
            Return New UnifiedArgumentSyntax(_argument.WithAdditionalAnnotations(annotation), _index)
        End Function
    End Class
End Namespace
