' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.Cci

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
#If DEBUG Then
    Partial Friend NotInheritable Class NamespaceSymbolAdapter
        Inherits SymbolAdapter
#Else
    Partial Friend MustInherit Class NamespaceSymbol
#End If
        Implements Cci.INamespace

        Private ReadOnly Property INamedEntity_Name As String Implements INamedEntity.Name
            Get
                Return AdaptedNamespaceSymbol.MetadataName
            End Get
        End Property

        Private ReadOnly Property INamespaceSymbol_ContainingNamespace As Cci.INamespace Implements Cci.INamespace.ContainingNamespace
            Get
                Return AdaptedNamespaceSymbol.ContainingNamespace?.GetCciAdapter()
            End Get
        End Property

        Private Function INamespaceSymbol_GetInternalSymbol() As CodeAnalysis.Symbols.INamespaceSymbolInternal Implements Cci.INamespace.GetInternalSymbol
            Return AdaptedNamespaceSymbol
        End Function
    End Class

    Partial Friend Class NamespaceSymbol
#If DEBUG Then
        Private _lazyAdapter As NamespaceSymbolAdapter

        Protected Overrides Function GetCciAdapterImpl() As SymbolAdapter
            Return GetCciAdapter()
        End Function

        Friend Shadows Function GetCciAdapter() As NamespaceSymbolAdapter
            If _lazyAdapter Is Nothing Then
                Return InterlockedOperations.Initialize(_lazyAdapter, New NamespaceSymbolAdapter(Me))
            End If

            Return _lazyAdapter
        End Function
#Else
        Friend ReadOnly Property AdaptedNamespaceSymbol As NamespaceSymbol
            Get
                Return Me
            End Get
        End Property

        Friend Shadows Function GetCciAdapter() As NamespaceSymbol
            Return Me
        End Function
#End If
    End Class

#If DEBUG Then
    Partial Friend Class NamespaceSymbolAdapter
        Friend ReadOnly Property AdaptedNamespaceSymbol As NamespaceSymbol

        Friend Sub New(underlyingNamespaceSymbol As NamespaceSymbol)
            AdaptedNamespaceSymbol = underlyingNamespaceSymbol
        End Sub

        Friend Overrides ReadOnly Property AdaptedSymbol As Symbol
            Get
                Return AdaptedNamespaceSymbol
            End Get
        End Property
    End Class
#End If
End Namespace
