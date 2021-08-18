' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.SimplifyInterpolation

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyInterpolation
    Friend NotInheritable Class VisualBasicSimplifyInterpolationHelpers
        Inherits AbstractSimplifyInterpolationHelpers

        Public Shared ReadOnly Property Instance As New VisualBasicSimplifyInterpolationHelpers

        Private Sub New()
        End Sub

        Protected Overrides ReadOnly Property PermitNonLiteralAlignmentComponents As Boolean = False

        Protected Overrides Function GetPreservedInterpolationExpressionSyntax(operation As IOperation) As SyntaxNode
            Return operation.Syntax
        End Function
    End Class
End Namespace
