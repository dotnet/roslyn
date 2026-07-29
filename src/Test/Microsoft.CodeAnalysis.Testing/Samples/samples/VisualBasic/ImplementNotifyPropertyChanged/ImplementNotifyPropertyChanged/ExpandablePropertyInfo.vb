' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Friend Class ExpandablePropertyInfo
    Public Property BackingFieldName As String
    Public Property NeedsBackingField As Boolean
    Public Property PropertyDeclaration As DeclarationStatementSyntax
    Public Property Type As ITypeSymbol
End Class
