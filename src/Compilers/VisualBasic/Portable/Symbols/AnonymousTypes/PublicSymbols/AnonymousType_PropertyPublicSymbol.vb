' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Friend NotInheritable Class AnonymousTypePropertyPublicSymbol
            Inherits SynthesizedPropertyBase

            Private ReadOnly _container As AnonymousTypePublicSymbol
            Private ReadOnly _getMethod As MethodSymbol
            Private ReadOnly _setMethod As MethodSymbol

            ''' <summary> Index of the property in the containing anonymous type </summary>
            Friend ReadOnly PropertyIndex As Integer

            Public Sub New(container As AnonymousTypePublicSymbol, index As Integer)
                Me._container = container
                Me.PropertyIndex = index

                Me._getMethod = New AnonymousTypePropertyGetAccessorPublicSymbol(Me)
                If Not container.TypeDescriptor.Fields(index).IsKey Then
                    Me._setMethod = New AnonymousTypePropertySetAccessorPublicSymbol(Me, container.Manager.System_Void)
                End If
            End Sub

            Friend ReadOnly Property AnonymousType As AnonymousTypePublicSymbol
                Get
                    Return _container
                End Get
            End Property

            Public Overrides ReadOnly Property SetMethod As MethodSymbol
                Get
                    Return Me._setMethod
                End Get
            End Property

            Public Overrides ReadOnly Property GetMethod As MethodSymbol
                Get
                    Return Me._getMethod
                End Get
            End Property

            Public Overrides ReadOnly Property Type As TypeSymbol
                Get
                    Return Me._container.TypeDescriptor.Fields(Me.PropertyIndex).Type
                End Get
            End Property

            Public Overrides ReadOnly Property Name As String
                Get
                    Return Me._container.TypeDescriptor.Fields(Me.PropertyIndex).Name
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return _container
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
                Get
                    Return _container
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return ImmutableArray.Create(Me._container.TypeDescriptor.Fields(Me.PropertyIndex).Location)
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return GetDeclaringSyntaxReferenceHelper(Of FieldInitializerSyntax)(Me.Locations)
                End Get
            End Property

            Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
                Get
                    ' The same as owning type
                    Return Me.ContainingType.IsImplicitlyDeclared
                End Get
            End Property

            Public Overrides Function Equals(obj As Object) As Boolean
                If obj Is Nothing Then
                    Return False
                ElseIf obj Is Me Then
                    Return True
                End If

                Dim other = TryCast(obj, AnonymousTypePropertyPublicSymbol)
                If other Is Nothing Then
                    Return False
                End If

                '  consider properties the same is the owning types are the 
                '  same and the names are equal
                Return other IsNot Nothing AndAlso
                       IdentifierComparison.Equals(other.Name, Me.Name) AndAlso
                       other.ContainingType.Equals(Me.ContainingType)
            End Function

            Public Overrides Function GetHashCode() As Integer
                Return Hash.Combine(Me.ContainingType.GetHashCode(), IdentifierComparison.GetHashCode(Me.Name))
            End Function

        End Class

    End Class

End Namespace
