' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' BoundExpressions to be used for emit. The expressions are assumed
    ''' to be lowered and will not be visited by <see cref="BoundTreeWalker"/>.
    ''' </summary>
    Friend MustInherit Class PseudoVariableExpressions
        Friend MustOverride Function GetAddress(variable As BoundPseudoVariable) As BoundExpression
        Friend MustOverride Function GetValue(variable As BoundPseudoVariable, diagnostics As DiagnosticBag) As BoundExpression
    End Class

End Namespace
