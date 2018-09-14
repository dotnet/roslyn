' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Binder used for interiors of documentation comment for binding 'name' attribute 
    ''' value of 'param' and 'paramref' documentation comment tags
    ''' </summary>
    Friend NotInheritable Class DocumentationCommentParamBinder
        Inherits DocumentationCommentBinder

        Public Sub New(containingBinder As Binder, commentedSymbol As Symbol)
            MyBase.New(containingBinder, commentedSymbol)
        End Sub

        Private ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                If Me.CommentedSymbol IsNot Nothing Then
                    Select Case Me.CommentedSymbol.Kind
                        Case SymbolKind.NamedType
                            Dim namedType = DirectCast(Me.CommentedSymbol, NamedTypeSymbol)
                            If namedType.TypeKind = TypeKind.Delegate Then
                                Dim method As MethodSymbol = namedType.DelegateInvokeMethod
                                If method IsNot Nothing Then
                                    Return method.Parameters
                                End If
                            End If

                        Case SymbolKind.Method
                            Return DirectCast(Me.CommentedSymbol, MethodSymbol).Parameters

                        Case SymbolKind.Property
                            Return DirectCast(Me.CommentedSymbol, PropertySymbol).Parameters

                        Case SymbolKind.Event
                            Return DirectCast(Me.CommentedSymbol, EventSymbol).DelegateParameters

                    End Select
                End If

                Return ImmutableArray(Of ParameterSymbol).Empty
            End Get
        End Property

        Friend Overrides Function BindXmlNameAttributeValue(identifier As IdentifierNameSyntax, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ImmutableArray(Of Symbol)
            If Me.CommentedSymbol Is Nothing Then
                Return ImmutableArray(Of Symbol).Empty
            End If

            Dim name As String = identifier.Identifier.ValueText
            If String.IsNullOrEmpty(name) Then
                Return ImmutableArray(Of Symbol).Empty
            End If

            Return FindSymbolInSymbolArray(name, Me.Parameters)
        End Function

        Private Const s_invalidLookupOptions As LookupOptions =
                            LookupOptions.LabelsOnly Or
                            LookupOptions.MustNotBeInstance Or
                            LookupOptions.MustBeInstance Or
                            LookupOptions.AttributeTypeOnly Or
                            LookupOptions.NamespacesOrTypesOnly Or
                            LookupOptions.MustNotBeLocalOrParameter

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                   options As LookupOptions,
                                                                   originalBinder As Binder)

            If (options And s_invalidLookupOptions) <> 0 Then
                Return
            End If

            For Each parameter In Me.Parameters
                If originalBinder.CanAddLookupSymbolInfo(parameter, options, nameSet, Nothing) Then
                    nameSet.AddSymbol(parameter, parameter.Name, 0)
                End If
            Next
        End Sub

        Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult,
                                                     name As String,
                                                     arity As Integer,
                                                     options As LookupOptions,
                                                     originalBinder As Binder,
                                                     <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
            Debug.Assert(lookupResult.IsClear)

            If (options And s_invalidLookupOptions) <> 0 OrElse arity > 0 Then
                Return
            End If

            For Each parameter In Me.Parameters
                If IdentifierComparison.Equals(parameter.Name, name) Then
                    lookupResult.SetFrom(CheckViability(parameter, arity, options, Nothing, useSiteDiagnostics))
                End If
            Next
        End Sub

    End Class

End Namespace

