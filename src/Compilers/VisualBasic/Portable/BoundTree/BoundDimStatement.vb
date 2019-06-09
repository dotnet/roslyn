' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class BoundDimStatement
        Implements IBoundLocalDeclarations

        Private ReadOnly Property IBoundLocalDeclarations_Declarations As ImmutableArray(Of BoundLocalDeclarationBase) Implements IBoundLocalDeclarations.Declarations
            Get
                Return Me.LocalDeclarations
            End Get
        End Property
    End Class
End Namespace
