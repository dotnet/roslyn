' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend NotInheritable Class ParametersAndLocalsBinder
        Inherits Binder

        Private ReadOnly _substitutedSourceMethod As MethodSymbol
        Private ReadOnly _nameToSymbolMap As Dictionary(Of String, Symbol)

        Public Sub New(containingBinder As Binder, targetMethod As EEMethodSymbol, substitutedSourceMethod As MethodSymbol)
            MyBase.New(containingBinder)

            _substitutedSourceMethod = substitutedSourceMethod
            _nameToSymbolMap = BuildNameToSymbolMap(targetMethod.Parameters, targetMethod.LocalsForBinding)
        End Sub

        ''' <remarks>
        ''' Currently, if there are duplicate names, the last one will win.
        ''' CONSIDER: We could create a multi-dictionary to let lookup fail "naturally".
        ''' CONSIDER: It would be nice to capture this behavior with a test.
        ''' </remarks>
        Private Shared Function BuildNameToSymbolMap(parameters As ImmutableArray(Of ParameterSymbol), locals As ImmutableArray(Of LocalSymbol)) As Dictionary(Of String, Symbol)
            Dim nameToSymbolMap As New Dictionary(Of String, Symbol)(CaseInsensitiveComparison.Comparer)

            For Each parameter In parameters
                Dim name As String = parameter.Name
                Dim kind As GeneratedNameKind = GeneratedNameParser.GetKind(name)
                If kind = GeneratedNameKind.None OrElse kind = GeneratedNameKind.HoistedMeField Then
                    nameToSymbolMap(name) = parameter
                Else
                    Debug.Assert(kind = GeneratedNameKind.TransparentIdentifier OrElse
                                 kind = GeneratedNameKind.AnonymousTransparentIdentifier)
                End If
            Next

            For Each local In locals
                nameToSymbolMap(local.Name) = local
            Next

            Return nameToSymbolMap
        End Function

        Public Overrides ReadOnly Property ContainingMember As Symbol
            Get
                Return _substitutedSourceMethod
            End Get
        End Property

        Public Overrides ReadOnly Property AdditionalContainingMembers As ImmutableArray(Of Symbol)
            Get
                Return ImmutableArray(Of Symbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingNamespaceOrType As NamespaceOrTypeSymbol
            Get
                Return _substitutedSourceMethod.ContainingNamespaceOrType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return _substitutedSourceMethod.ContainingType
            End Get
        End Property

        Public Overrides Function GetLocalForFunctionValue() As LocalSymbol
            ' If this is ever hit, we'll have to dig the function local out of the locals list.
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult,
                                              name As String,
                                              arity As Integer,
                                              options As LookupOptions,
                                              originalBinder As Binder,
                                              <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
            Debug.Assert(lookupResult.IsClear)

            ' Parameters and locals always have arity 0 and are not namespaces or types.
            If (options And (LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly Or LookupOptions.MustNotBeLocalOrParameter)) = 0 Then
                Dim symbol As Symbol = Nothing
                If _nameToSymbolMap.TryGetValue(name, symbol) Then
                    lookupResult.SetFrom(CheckViability(symbol, arity, options, Nothing, useSiteInfo))
                End If
            End If
        End Sub

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                    options As LookupOptions,
                                                                    originalBinder As Binder)
            ' UNDONE: additional filtering based on options?
            If (options And (LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly Or LookupOptions.MustNotBeLocalOrParameter)) = 0 Then
                For Each symbol In _nameToSymbolMap.Values
                    If originalBinder.CanAddLookupSymbolInfo(symbol, options, nameSet, Nothing) Then
                        nameSet.AddSymbol(symbol, symbol.Name, 0)
                    End If
                Next
            End If
        End Sub

    End Class
End Namespace
