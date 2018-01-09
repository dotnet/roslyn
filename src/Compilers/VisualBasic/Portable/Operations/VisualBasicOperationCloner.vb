' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports Microsoft.CodeAnalysis.Operations

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Class VisualBasicOperationCloner
        Inherits OperationCloner

        Public Shared ReadOnly Property Instance As OperationCloner = New VisualBasicOperationCloner()
    End Class
End Namespace
