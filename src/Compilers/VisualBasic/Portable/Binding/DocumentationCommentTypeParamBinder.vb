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
    ''' value of 'typeparam' documentation comment tag
    ''' </summary>
    Friend Class DocumentationCommentTypeParamBinder
        Inherits DocumentationCommentBinder

        Public Sub New(containingBinder As Binder, commentedSymbol As Symbol)
            MyBase.New(containingBinder, commentedSymbol)
        End Sub

        Friend Overrides Function BindXmlNameAttributeValue(identifier As IdentifierNameSyntax, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of Symbol)
            If Me.CommentedSymbol Is Nothing Then
                Return ImmutableArray(Of Symbol).Empty
            End If

            Dim name As String = identifier.Identifier.ValueText
            If String.IsNullOrEmpty(name) Then
                Return ImmutableArray(Of Symbol).Empty
            End If

            Return FindSymbolInSymbolArray(name, TypeParameters)
        End Function

        Protected ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                If Me.CommentedSymbol IsNot Nothing Then
                    Select Case Me.CommentedSymbol.Kind
                        Case SymbolKind.NamedType
                            Return DirectCast(Me.CommentedSymbol, NamedTypeSymbol).TypeParameters

                        Case SymbolKind.Method
                            Return DirectCast(Me.CommentedSymbol, MethodSymbol).TypeParameters
                    End Select
                End If

                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                   options As LookupOptions,
                                                                   originalBinder As Binder)

            If (options And (LookupOptions.LabelsOnly Or LookupOptions.MustBeInstance Or LookupOptions.AttributeTypeOnly)) <> 0 Then
                Return
            End If

            Dim typeParameters As ImmutableArray(Of TypeParameterSymbol) = Me.TypeParameters

            If Not typeParameters.IsEmpty Then
                For Each typeParameter In typeParameters
                    If originalBinder.CanAddLookupSymbolInfo(typeParameter, options, nameSet, Nothing) Then
                        nameSet.AddSymbol(typeParameter, typeParameter.Name, 0)
                    End If
                Next
            End If
        End Sub

        Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult,
                                                      name As String,
                                                      arity As Integer,
                                                      options As LookupOptions,
                                                      originalBinder As Binder,
                                                      <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
            Debug.Assert(lookupResult.IsClear)

            If (options And (LookupOptions.LabelsOnly Or LookupOptions.MustBeInstance Or LookupOptions.AttributeTypeOnly)) <> 0 Then
                Return
            End If

            If Not TypeParameters.IsEmpty Then
                For Each typeParameter In TypeParameters
                    If IdentifierComparison.Equals(typeParameter.Name, name) Then
                        lookupResult.SetFrom(CheckViability(typeParameter, arity, options, Nothing, useSiteInfo))
                    End If
                Next
            End If
        End Sub

    End Class

End Namespace

