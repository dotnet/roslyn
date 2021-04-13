' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
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
