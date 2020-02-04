' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class BoundConversionOrCast
        Public MustOverride ReadOnly Property Operand As BoundExpression
        Public MustOverride ReadOnly Property ConversionKind As ConversionKind
        Public MustOverride ReadOnly Property ExplicitCastInCode As Boolean
    End Class
End Namespace
