' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Binder used for speculatively binding.
    ''' </summary>
    Friend Class SpeculativeBinder
        Inherits SemanticModelBinder

        Private Sub New(containingBinder As Binder)
            MyBase.New(containingBinder)
        End Sub

        ' Create a new SpeculativeBinder inside the given containing binder. Also insert a ImplicitVariableBinder
        ' if one is required.
        Public Shared Function Create(containingBinder As Binder) As SpeculativeBinder
            If containingBinder.ImplicitVariableDeclarationAllowed Then
                ' We're in a location where implicit variable declaration is allowed. Because speculative binding
                ' shouldn't add variables to the containing (non-speculative) binder, we need to create a new ImplicitVariableBinder
                ' to hold newly declared variables from the speculative code.

                ' The containing binder shouldn't be accepting new variables any more.
                Debug.Assert(containingBinder.AllImplicitVariableDeclarationsAreHandled)

                containingBinder = New ImplicitVariableBinder(containingBinder, containingBinder.ContainingMember)
            End If

            Return New SpeculativeBinder(containingBinder)
        End Function

        Public Overrides Function GetSyntaxReference(node As VisualBasicSyntaxNode) As SyntaxReference
            Throw New NotSupportedException()   ' shouldn't happen within speculative binding.
        End Function

        ' TODO override SyntaxTree property to return correct tree. (after e.g. bugs 2174, 5848)

        Friend Overrides Function BindGroupAggregationExpression(group As GroupAggregationSyntax, diagnostics As DiagnosticBag) As BoundExpression
            ' Overridden method returns a BadExpression.
            Return Me.ContainingBinder.BindGroupAggregationExpression(group, diagnostics)
        End Function

        Friend Overrides Function BindFunctionAggregationExpression([function] As FunctionAggregationSyntax, diagnostics As DiagnosticBag) As BoundExpression
            ' Overridden method returns a BadExpression.
            Return Me.ContainingBinder.BindFunctionAggregationExpression([function], diagnostics)
        End Function
    End Class

End Namespace
