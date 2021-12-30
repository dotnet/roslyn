' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A SubOrFunctionBodyBinder provides context for looking up parameters/labels in a body of an executable entity (a method, lambda, or top-level script code), 
    ''' and also for the implementation of ContainingMember, GetLocalForFunctionValue and GetBinder. 
    ''' </summary>
    Friend MustInherit Class SubOrFunctionBodyBinder
        Inherits ExecutableCodeBinder

        Private ReadOnly _methodSymbol As MethodSymbol
        Protected ReadOnly _parameterMap As Dictionary(Of String, Symbol)

        Public Sub New(methodOrLambdaSymbol As MethodSymbol, root As SyntaxNode, containingBinder As Binder)
            MyBase.New(root, containingBinder)

            _methodSymbol = methodOrLambdaSymbol

            Dim parameters As ImmutableArray(Of ParameterSymbol) = methodOrLambdaSymbol.Parameters
            Dim count As Integer = parameters.Length
            Dim mapSize As Integer = count

            If Not methodOrLambdaSymbol.IsSub Then
                mapSize += 1 ' account for possible function return value
            End If

            _parameterMap = New Dictionary(Of String, Symbol)(mapSize, CaseInsensitiveComparison.Comparer)

            For i = 0 To count - 1
                Dim parameterSymbol = parameters(i)
                ' If there are two parameters with the same name, the first takes precedence.
                ' This is an error condition anyway, but it seems more logical and
                ' it really doesn't matter which order we use.
                If Not _parameterMap.ContainsKey(parameterSymbol.Name) Then
                    _parameterMap(parameterSymbol.Name) = parameterSymbol
                End If
            Next
        End Sub

        Public Overrides ReadOnly Property ContainingMember As Symbol
            Get
                Return _methodSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property AdditionalContainingMembers As ImmutableArray(Of Symbol)
            Get
                Return ImmutableArray(Of Symbol).Empty
            End Get
        End Property

        Public MustOverride Overrides Function GetLocalForFunctionValue() As LocalSymbol

        Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult,
                                                      name As String,
                                                      arity As Integer,
                                                      options As LookupOptions,
                                                      originalBinder As Binder,
                                                      <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
            Debug.Assert(lookupResult.IsClear)

            ' Parameters always have arity 0 and are not namespaces or types.
            If (options And (LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly Or LookupOptions.MustNotBeLocalOrParameter)) = 0 Then
                Dim parameterSymbol As Symbol = Nothing
                If _parameterMap.TryGetValue(name, parameterSymbol) Then
                    lookupResult.SetFrom(CheckViability(parameterSymbol, arity, options, Nothing, useSiteInfo))
                End If
            Else
                MyBase.LookupInSingleBinder(lookupResult, name, arity, options, originalBinder, useSiteInfo)
            End If
        End Sub

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                    options As LookupOptions,
                                                                    originalBinder As Binder)
            ' UNDONE: additional filtering based on options?
            If (options And (LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly)) = 0 Then
                For Each param In _parameterMap.Values
                    If originalBinder.CanAddLookupSymbolInfo(param, options, nameSet, Nothing) Then
                        nameSet.AddSymbol(param, param.Name, 0)
                    End If
                Next
            Else
                MyBase.AddLookupSymbolsInfoInSingleBinder(nameSet, options, originalBinder)
            End If
        End Sub

    End Class

End Namespace
