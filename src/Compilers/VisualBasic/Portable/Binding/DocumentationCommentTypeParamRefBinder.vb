' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Binder used for interiors of documentation comment for binding 'name' attribute 
    ''' value of 'typeparamref' documentation comment tag
    ''' </summary>
    Friend NotInheritable Class DocumentationCommentTypeParamRefBinder
        Inherits DocumentationCommentTypeParamBinder

        Public Sub New(containingBinder As Binder, commentedSymbol As Symbol)
            MyBase.New(containingBinder, commentedSymbol)
        End Sub

        Friend Overrides Function BindXmlNameAttributeValue(identifier As IdentifierNameSyntax, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of Symbol)
            Dim result As ImmutableArray(Of Symbol) = MyBase.BindXmlNameAttributeValue(identifier, useSiteInfo)
            If Not result.IsEmpty Then
                Return result
            End If

            Const options As LookupOptions =
                    LookupOptions.UseBaseReferenceAccessibility Or
                    LookupOptions.MustNotBeReturnValueVariable Or
                    LookupOptions.IgnoreExtensionMethods Or
                    LookupOptions.MustNotBeLocalOrParameter

            Dim lookupResult As LookupResult = lookupResult.GetInstance()
            Me.Lookup(lookupResult, identifier.Identifier.ValueText, 0, options, useSiteInfo)

            If Not lookupResult.HasSingleSymbol Then
                lookupResult.Free()
                Return Nothing
            End If

            Dim symbol As Symbol = lookupResult.SingleSymbol
            lookupResult.Free()

            If symbol.Kind = SymbolKind.TypeParameter Then
                Return ImmutableArray.Create(Of Symbol)(symbol)
            End If

            Return ImmutableArray(Of Symbol).Empty
        End Function

    End Class

End Namespace

