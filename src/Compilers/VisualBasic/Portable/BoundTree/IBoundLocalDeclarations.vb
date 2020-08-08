' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Interface IBoundLocalDeclarations
        ReadOnly Property Declarations As ImmutableArray(Of BoundLocalDeclarationBase)
    End Interface
End Namespace
