' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A MethodTypeParametersBinder provides the context for looking up type parameters on a method.
    ''' It is split out since binding of type in the parameters and return value need to happen with a context
    ''' that includes the type parameters, but we don't have a fully complete method symbol yet.
    ''' </summary>
    Friend NotInheritable Class MethodTypeParametersBinder
        Inherits Binder

        Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)

        Public Sub New(containingBinder As Binder, typeParameters As ImmutableArray(Of TypeParameterSymbol))
            MyBase.New(containingBinder)
            _typeParameters = typeParameters
        End Sub

        ''' <summary>
        ''' Looks up the name in the type parameters
        ''' a) type parameters in this type (but not outer or base types)
        ''' Returns all members of that name, or empty list if none.
        ''' </summary>
        Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult,
                                                      name As String,
                                                      arity As Integer,
                                                      options As LookupOptions,
                                                      originalBinder As Binder,
                                                      <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
            Debug.Assert(lookupResult.IsClear)

            ' type parameters can only be accessed with arity 0
            ' Since there are typically just one or two type parameters, using a dictionary/ILookup would be overkill.
            For i = 0 To _typeParameters.Length - 1
                Dim tp = _typeParameters(i)
                If IdentifierComparison.Equals(tp.Name, name) Then
                    lookupResult.SetFrom(CheckViability(tp, arity, options, Nothing, useSiteInfo))
                End If
            Next
        End Sub

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                    options As LookupOptions,
                                                                    originalBinder As Binder)
            ' UNDONE: check options to see if type parameters should be found.
            For Each typeParameter In _typeParameters
                If originalBinder.CanAddLookupSymbolInfo(typeParameter, options, nameSet, Nothing) Then
                    nameSet.AddSymbol(typeParameter, typeParameter.Name, 0)
                End If
            Next
        End Sub
    End Class

End Namespace

