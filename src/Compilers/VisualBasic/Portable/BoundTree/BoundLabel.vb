' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundLabel

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return Label
            End Get
        End Property

    End Class
End Namespace
