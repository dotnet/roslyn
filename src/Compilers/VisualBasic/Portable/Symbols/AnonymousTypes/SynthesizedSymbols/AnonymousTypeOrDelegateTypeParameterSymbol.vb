' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Private NotInheritable Class AnonymousTypeOrDelegateTypeParameterSymbol
            Inherits TypeParameterSymbol

            Private ReadOnly _container As AnonymousTypeOrDelegateTemplateSymbol
            Private ReadOnly _ordinal As Integer

            Public Sub New(container As AnonymousTypeOrDelegateTemplateSymbol, ordinal As Integer)
                _container = container
                _ordinal = ordinal
            End Sub

            Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
                Get
                    Return TypeParameterKind.Type
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return _container
                End Get
            End Property

            Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
                Get
                    Return ImmutableArray(Of TypeSymbol).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return ImmutableArray(Of Location).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return ImmutableArray(Of SyntaxReference).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property Name As String
                Get
                    If _container.TypeKind = TypeKind.Delegate Then
                        If _container.DelegateInvokeMethod.IsSub OrElse Ordinal < _container.Arity - 1 Then
                            Return "TArg" & Ordinal
                        Else
                            Return "TResult"
                        End If
                    Else
                        Debug.Assert(_container.TypeKind = TypeKind.Class)
                        Return "T" & Ordinal
                    End If
                End Get
            End Property

            Public Overrides ReadOnly Property Ordinal As Integer
                Get
                    Return _ordinal
                End Get
            End Property

            Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property AllowsRefLikeType As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property Variance As VarianceKind
                Get
                    Return VarianceKind.None
                End Get
            End Property

            Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
                Get
                    Return True
                End Get
            End Property

            Friend Overrides Sub EnsureAllConstraintsAreResolved()
            End Sub

            Public Overrides Function GetHashCode() As Integer
                Return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Me)
            End Function

            Public Overrides Function Equals(other As TypeSymbol, comparison As TypeCompareKind) As Boolean
                Return other Is Me
            End Function
        End Class

    End Class

End Namespace
