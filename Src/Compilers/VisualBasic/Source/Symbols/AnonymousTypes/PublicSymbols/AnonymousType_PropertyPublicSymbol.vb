' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Friend NotInheritable Class AnonymousTypePropertyPublicSymbol
            Inherits SynthesizedPropertyBase

            Private ReadOnly m_container As AnonymousTypePublicSymbol
            Private ReadOnly m_getMethod As MethodSymbol
            Private ReadOnly m_setMethod As MethodSymbol

            ''' <summary> Index of the property in the containing anonymous type </summary>
            Friend ReadOnly PropertyIndex As Integer

            Public Sub New(container As AnonymousTypePublicSymbol, index As Integer)
                Me.m_container = container
                Me.PropertyIndex = index

                Me.m_getMethod = New AnonymousTypePropertyGetAccessorPublicSymbol(Me)
                If Not container.TypeDescriptor.Fields(index).IsKey Then
                    Me.m_setMethod = New AnonymousTypePropertySetAccessorPublicSymbol(Me, container.Manager.System_Void)
                End If
            End Sub

            Friend ReadOnly Property AnonymousType As AnonymousTypePublicSymbol
                Get
                    Return m_container
                End Get
            End Property

            Public Overrides ReadOnly Property SetMethod As MethodSymbol
                Get
                    Return Me.m_setMethod
                End Get
            End Property

            Public Overrides ReadOnly Property GetMethod As MethodSymbol
                Get
                    Return Me.m_getMethod
                End Get
            End Property

            Public Overrides ReadOnly Property Type As TypeSymbol
                Get
                    Return Me.m_container.TypeDescriptor.Fields(Me.PropertyIndex).Type
                End Get
            End Property

            Public Overrides ReadOnly Property Name As String
                Get
                    Return Me.m_container.TypeDescriptor.Fields(Me.PropertyIndex).Name
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return m_container
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
                Get
                    Return m_container
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return ImmutableArray.Create(Of Location)(Me.m_container.TypeDescriptor.Fields(Me.PropertyIndex).Location)
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return GetDeclaringSyntaxReferenceHelper(Of FieldInitializerSyntax)(StaticCast(Of Location).From(Me.Locations))
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