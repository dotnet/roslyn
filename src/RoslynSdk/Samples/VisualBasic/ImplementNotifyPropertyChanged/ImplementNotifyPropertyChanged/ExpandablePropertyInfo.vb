' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Friend Class ExpandablePropertyInfo
    Public Property BackingFieldName As String
    Public Property NeedsBackingField As Boolean
    Public Property PropertyDeclaration As DeclarationStatementSyntax
    Public Property Type As ITypeSymbol
End Class
