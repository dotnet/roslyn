' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class BoundMyBaseReference
        Public NotOverridable Overrides ReadOnly Property SuppressVirtualCalls As Boolean
            Get
                Return True
            End Get
        End Property
    End Class
End Namespace