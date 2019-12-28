' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class Binder

        Friend Overridable Function BindInsideCrefAttributeValue(name As TypeSyntax, preserveAliases As Boolean, diagnosticBag As BindingDiagnosticBag, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of Symbol)
            Return Me.ContainingBinder.BindInsideCrefAttributeValue(name, preserveAliases, diagnosticBag, useSiteInfo)
        End Function

        Friend Overridable Function BindInsideCrefAttributeValue(reference As CrefReferenceSyntax, preserveAliases As Boolean, diagnosticBag As BindingDiagnosticBag, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of Symbol)
            Return Me.ContainingBinder.BindInsideCrefAttributeValue(reference, preserveAliases, diagnosticBag, useSiteInfo)
        End Function

        Friend Overridable Function BindXmlNameAttributeValue(identifier As IdentifierNameSyntax, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of Symbol)
            Return Me.ContainingBinder.BindXmlNameAttributeValue(identifier, useSiteInfo)
        End Function

    End Class
End Namespace
