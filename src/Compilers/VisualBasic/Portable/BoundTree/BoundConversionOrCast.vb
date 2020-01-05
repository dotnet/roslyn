' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class BoundConversionOrCast
        Public MustOverride ReadOnly Property Operand As BoundExpression
        Public MustOverride ReadOnly Property ConversionKind As ConversionKind
        Public MustOverride ReadOnly Property ExplicitCastInCode As Boolean
    End Class
End Namespace
