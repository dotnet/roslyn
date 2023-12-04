' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Private MustInherit Class AnonymousTypePropertyAccessorPublicSymbol
            Inherits SynthesizedPropertyAccessorBase(Of PropertySymbol)

            Private ReadOnly _returnType As TypeSymbol

            Public Sub New([property] As PropertySymbol, returnType As TypeSymbol)
                MyBase.New([property].ContainingType, [property])
                _returnType = returnType
            End Sub

            Friend NotOverridable Overrides ReadOnly Property BackingFieldSymbol As FieldSymbol
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Public Overrides ReadOnly Property ReturnType As TypeSymbol
                Get
                    Return _returnType
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend NotOverridable Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
                Throw ExceptionUtilities.Unreachable
            End Function
        End Class

        Private NotInheritable Class AnonymousTypePropertyGetAccessorPublicSymbol
            Inherits AnonymousTypePropertyAccessorPublicSymbol

            Public Sub New([property] As PropertySymbol)
                MyBase.New([property], [property].Type)
            End Sub

            Public Overrides ReadOnly Property IsSub As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property MethodKind As MethodKind
                Get
                    Return MethodKind.PropertyGet
                End Get
            End Property

        End Class

        Private NotInheritable Class AnonymousTypePropertySetAccessorPublicSymbol
            Inherits AnonymousTypePropertyAccessorPublicSymbol

            Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

            Public Sub New([property] As PropertySymbol, voidTypeSymbol As TypeSymbol)
                MyBase.New([property], voidTypeSymbol)

                _parameters = ImmutableArray.Create(Of ParameterSymbol)(
                    New SynthesizedParameterSymbol(Me, m_propertyOrEvent.Type, 0, False, StringConstants.ValueParameterName))
            End Sub

            Public Overrides ReadOnly Property IsSub As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return _parameters
                End Get
            End Property

            Public Overrides ReadOnly Property MethodKind As MethodKind
                Get
                    Return MethodKind.PropertySet
                End Get
            End Property

        End Class

    End Class

End Namespace
