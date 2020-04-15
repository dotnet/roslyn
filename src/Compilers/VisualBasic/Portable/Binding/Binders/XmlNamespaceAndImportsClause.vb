' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Structure XmlNamespaceAndImportsClausePosition
        Public ReadOnly XmlNamespace As String
        Public ReadOnly ImportsClausePosition As Integer

        Public Sub New(xmlNamespace As String, importsClausePosition As Integer)
            Me.XmlNamespace = xmlNamespace
            Me.ImportsClausePosition = importsClausePosition
        End Sub
    End Structure
End Namespace
