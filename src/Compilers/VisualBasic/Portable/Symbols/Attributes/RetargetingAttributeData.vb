' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
    ''' <summary>
    ''' Represents a retargeting custom attribute
    ''' </summary>
    Friend NotInheritable Class RetargetingAttributeData
        Inherits VisualBasicAttributeData

        Private ReadOnly _underlying As VisualBasicAttributeData
        Private ReadOnly _attributeClass As NamedTypeSymbol
        Private ReadOnly _attributeConstructor As MethodSymbol
        Private ReadOnly _constructorArguments As ImmutableArray(Of TypedConstant)
        Private ReadOnly _namedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))

        Friend Sub New(ByVal underlying As VisualBasicAttributeData,
                       ByVal attributeClass As NamedTypeSymbol,
                       ByVal attributeConstructor As MethodSymbol,
                       ByVal constructorArguments As ImmutableArray(Of TypedConstant),
                       ByVal namedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant)))
            Debug.Assert(TypeOf underlying Is SourceAttributeData OrElse TypeOf underlying Is SynthesizedAttributeData)
            Debug.Assert(attributeClass IsNot Nothing OrElse underlying.HasErrors)

            _underlying = underlying
            Me._attributeClass = attributeClass
            Me._attributeConstructor = attributeConstructor
            Me._constructorArguments = constructorArguments.NullToEmpty()
            Me._namedArguments = namedArguments.NullToEmpty()
        End Sub

        Public Overrides ReadOnly Property AttributeClass As NamedTypeSymbol
            Get
                Return _attributeClass
            End Get
        End Property

        Public Overrides ReadOnly Property AttributeConstructor As MethodSymbol
            Get
                Return _attributeConstructor
            End Get
        End Property

        Public Overrides ReadOnly Property ApplicationSyntaxReference As SyntaxReference
            Get
                Return Nothing
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonConstructorArguments As ImmutableArray(Of TypedConstant)
            Get
                Return _constructorArguments
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonNamedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))
            Get
                Return _namedArguments
            End Get
        End Property

        Friend Overrides ReadOnly Property IsConditionallyOmitted As Boolean
            Get
                Return _underlying.IsConditionallyOmitted
            End Get
        End Property

        Friend Overrides ReadOnly Property HasErrors As Boolean
            Get
                Return _underlying.HasErrors OrElse _attributeConstructor Is Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                Debug.Assert(AttributeClass IsNot Nothing OrElse _underlying.HasErrors)

                If _underlying.HasErrors Then
                    Return _underlying.ErrorInfo
                ElseIf HasErrors Then
                    Debug.Assert(AttributeConstructor Is Nothing)

                    Return If(AttributeClass.GetUseSiteInfo().DiagnosticInfo,
                              ErrorFactory.ErrorInfo(ERRID.ERR_MissingRuntimeHelper, AttributeClass.MetadataName & "." & WellKnownMemberNames.InstanceConstructorName))
                Else
                    Return Nothing
                End If
            End Get
        End Property

        Friend Overrides Function IsTargetAttribute(namespaceName As String, typeName As String, Optional ignoreCase As Boolean = False) As Boolean
            Return _underlying.IsTargetAttribute(namespaceName, typeName, ignoreCase)
        End Function

        Friend Overrides Function GetTargetAttributeSignatureIndex(description As AttributeDescription) As Integer
            Return _underlying.GetTargetAttributeSignatureIndex(description)
        End Function
    End Class
End Namespace
