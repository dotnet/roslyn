' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class DocumentationCommentCrefBinder
        Inherits DocumentationCommentBinder

        Private NotInheritable Class TypeParametersBinder
            Inherits Binder

            Friend ReadOnly _typeParameters As Dictionary(Of String, CrefTypeParameterSymbol)

            Public Sub New(containingBinder As Binder, typeParameters As Dictionary(Of String, CrefTypeParameterSymbol))
                MyBase.New(containingBinder)
                Me._typeParameters = typeParameters
            End Sub

            Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult,
                                                         name As String,
                                                         arity As Integer,
                                                         options As LookupOptions,
                                                         originalBinder As Binder,
                                                         <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
                Debug.Assert(lookupResult.IsClear)

                Dim typeParameter As CrefTypeParameterSymbol = Nothing
                If Me._typeParameters.TryGetValue(name, typeParameter) Then
                    lookupResult.SetFrom(CheckViability(typeParameter,
                                                        arity,
                                                        options Or LookupOptions.IgnoreAccessibility,
                                                        Nothing,
                                                        useSiteInfo))
                End If
            End Sub

            Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                       options As LookupOptions,
                                                                       originalBinder As Binder)

                For Each typeParameter In _typeParameters.Values
                    If originalBinder.CanAddLookupSymbolInfo(typeParameter, options, nameSet, Nothing) Then
                        nameSet.AddSymbol(typeParameter, typeParameter.Name, 0)
                    End If
                Next
            End Sub
        End Class

    End Class

End Namespace

