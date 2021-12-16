' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundMyClassReference
        Public NotOverridable Overrides ReadOnly Property SuppressVirtualCalls As Boolean
            Get
                Return True
            End Get
        End Property
    End Class
End Namespace
