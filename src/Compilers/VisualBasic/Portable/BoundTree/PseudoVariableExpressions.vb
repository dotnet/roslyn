' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
