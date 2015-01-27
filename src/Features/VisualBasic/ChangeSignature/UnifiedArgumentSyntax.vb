' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ChangeSignature
    Friend Class UnifiedArgumentSyntax
        Implements IUnifiedArgumentSyntax

        Private ReadOnly argument As ArgumentSyntax

        Private Sub New(argument As ArgumentSyntax)
            Debug.Assert(argument.IsKind(SyntaxKind.SimpleArgument))
            Me.argument = argument
        End Sub

        Public Shared Function Create(argument As ArgumentSyntax) As IUnifiedArgumentSyntax
            Return New UnifiedArgumentSyntax(argument)
        End Function

        Private ReadOnly Property IsDefault As Boolean Implements IUnifiedArgumentSyntax.IsDefault
            Get
                Return argument Is Nothing
            End Get
        End Property

        Private ReadOnly Property IsNamed As Boolean Implements IUnifiedArgumentSyntax.IsNamed
            Get
                Return argument.IsNamed
            End Get
        End Property

        Public Shared Widening Operator CType(ByVal unified As UnifiedArgumentSyntax) As ArgumentSyntax
            Return unified.argument
        End Operator

        Public Function GetName() As String Implements IUnifiedArgumentSyntax.GetName
            Return If(argument.IsNamed,
                      DirectCast(argument, SimpleArgumentSyntax).NameColonEquals.Name.Identifier.ToString(),
                      Nothing)
        End Function

        Private Function WithName(name As String) As IUnifiedArgumentSyntax Implements IUnifiedArgumentSyntax.WithName
            Return If(argument.IsNamed,
                      Create(DirectCast(argument, SimpleArgumentSyntax).WithNameColonEquals(DirectCast(argument, SimpleArgumentSyntax).NameColonEquals.WithName(SyntaxFactory.IdentifierName(name)))),
                      Create(SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName(name)), argument.GetExpression())))
        End Function

        Public Function WithAdditionalAnnotations(annotation As SyntaxAnnotation) As IUnifiedArgumentSyntax Implements IUnifiedArgumentSyntax.WithAdditionalAnnotations
            Return New UnifiedArgumentSyntax(argument.WithAdditionalAnnotations(annotation))
        End Function
    End Class
End Namespace
