' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class Binder

        Friend Overridable Function BindInsideCrefAttributeValue(name As TypeSyntax, preserveAliases As Boolean, diagnosticBag As DiagnosticBag, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ImmutableArray(Of Symbol)
            Return Me.ContainingBinder.BindInsideCrefAttributeValue(name, preserveAliases, diagnosticBag, useSiteDiagnostics)
        End Function

        Friend Overridable Function BindInsideCrefAttributeValue(reference As CrefReferenceSyntax, preserveAliases As Boolean, diagnosticBag As DiagnosticBag, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ImmutableArray(Of Symbol)
            Return Me.ContainingBinder.BindInsideCrefAttributeValue(reference, preserveAliases, diagnosticBag, useSiteDiagnostics)
        End Function

        Friend Overridable Function BindXmlNameAttributeValue(identifier As IdentifierNameSyntax, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ImmutableArray(Of Symbol)
            Return Me.ContainingBinder.BindXmlNameAttributeValue(identifier, useSiteDiagnostics)
        End Function

    End Class
End Namespace
