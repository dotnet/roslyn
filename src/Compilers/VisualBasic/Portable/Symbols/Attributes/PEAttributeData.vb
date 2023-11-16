' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Threading
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' Class to represent custom attributes attached to symbols.
    ''' </summary>
    Friend NotInheritable Class PEAttributeData
        Inherits VisualBasicAttributeData

        Private ReadOnly _decoder As MetadataDecoder
        Private ReadOnly _handle As CustomAttributeHandle
        Private _attributeClass As NamedTypeSymbol ' TODO - Remove class it is available from constructor. For now it is only used to know 
        ' that the attribute is fully loaded because there isn't an error method symbol.
        Private _attributeConstructor As MethodSymbol
        Private _lazyConstructorArguments As TypedConstant()
        Private _lazyNamedArguments As KeyValuePair(Of String, TypedConstant)()
        Private _lazyHasErrors As ThreeState = ThreeState.Unknown

        Friend Sub New(moduleSymbol As PEModuleSymbol, handle As CustomAttributeHandle)
            Debug.Assert(moduleSymbol IsNot Nothing)

            _decoder = New MetadataDecoder(moduleSymbol)
            _handle = handle
        End Sub

        ''' <summary>
        ''' The attribute class.
        ''' </summary>
        Public Overrides ReadOnly Property AttributeClass As NamedTypeSymbol
            Get
                If _attributeClass Is Nothing Then
                    EnsureClassAndConstructorSymbols()
                End If

                Return _attributeClass
            End Get
        End Property

        ''' <summary>
        ''' The constructor on the attribute class.
        ''' </summary>
        Public Overrides ReadOnly Property AttributeConstructor As MethodSymbol
            Get
                If _attributeConstructor Is Nothing Then
                    EnsureClassAndConstructorSymbols()
                End If

                Return _attributeConstructor
            End Get
        End Property

        Public Overrides ReadOnly Property ApplicationSyntaxReference As SyntaxReference
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Constructor arguments on the attribute.
        ''' </summary>
        Protected Overrides ReadOnly Property CommonConstructorArguments As ImmutableArray(Of TypedConstant)
            Get
                If _lazyConstructorArguments Is Nothing Then
                    EnsureLazyMembersAreLoaded()
                End If

                Return _lazyConstructorArguments.AsImmutableOrNull
            End Get
        End Property

        ''' <summary>
        ''' Named (property value) arguments on the attribute. 
        ''' </summary>
        Protected Overrides ReadOnly Property CommonNamedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))
            Get
                If _lazyNamedArguments Is Nothing Then
                    EnsureLazyMembersAreLoaded()
                End If

                Return _lazyNamedArguments.AsImmutableOrNull
            End Get
        End Property

        ''' <summary>
        ''' Matches an attribute by metadata namespace, metadata type name. Does not load the type symbol for
        ''' the attribute.
        ''' </summary>
        ''' <param name="namespaceName"></param>
        ''' <param name="typeName"></param>
        ''' <returns>True if the attribute data matches.</returns>
        Friend Overrides Function IsTargetAttribute(namespaceName As String, typeName As String, Optional ignoreCase As Boolean = False) As Boolean
            ' Matching an attribute by name should not load the attribute class.
            Return _decoder.IsTargetAttribute(_handle, namespaceName, typeName, ignoreCase)
        End Function

        ''' <summary>
        ''' Matches an attribute by metadata namespace, metadata type name and metadata signature. Does not load the
        ''' type symbol for the attribute.
        ''' </summary>
        ''' <param name="description">Attribute to match.</param>
        ''' <returns>
        ''' An index of the target constructor signature in signatures array,
        ''' -1 if this is not the target attribute.
        ''' </returns>
        ''' <remarks>Matching an attribute by name does not load the attribute class.</remarks>
        Friend Overrides Function GetTargetAttributeSignatureIndex(description As AttributeDescription) As Integer
            Return _decoder.GetTargetAttributeSignatureIndex(_handle, description)
        End Function

        Private Sub EnsureLazyMembersAreLoaded()

            If _lazyConstructorArguments Is Nothing Then

                Dim constructorArgs As TypedConstant() = Nothing
                Dim namedArgs As KeyValuePair(Of String, TypedConstant)() = Nothing

                If Not _decoder.GetCustomAttribute(_handle, AttributeConstructor, constructorArgs, namedArgs) Then
                    _lazyHasErrors = ThreeState.True
                End If

                Debug.Assert(constructorArgs IsNot Nothing AndAlso namedArgs IsNot Nothing)

                Interlocked.CompareExchange(Of KeyValuePair(Of String, TypedConstant)())(
                      _lazyNamedArguments,
                      namedArgs,
                      Nothing)

                Interlocked.CompareExchange(Of TypedConstant())(
                    _lazyConstructorArguments,
                    constructorArgs,
                    Nothing)
            End If
        End Sub

        Private Sub EnsureClassAndConstructorSymbols()
            If _attributeClass Is Nothing Then

                Dim attributeClass As TypeSymbol = Nothing
                Dim attributeCtor As MethodSymbol = Nothing

                If Not _decoder.GetCustomAttribute(_handle, attributeClass, attributeCtor) OrElse
                    attributeClass Is Nothing Then

                    Interlocked.CompareExchange(Of NamedTypeSymbol)(
                         _attributeClass,
                         ErrorTypeSymbol.UnknownResultType,
                         Nothing)

                    ' Method symbol is null when there is an error.

                    _lazyHasErrors = ThreeState.True
                    Return
                End If

                If attributeClass.IsErrorType() OrElse attributeCtor Is Nothing Then
                    _lazyHasErrors = ThreeState.True
                End If

                Interlocked.CompareExchange(Of MethodSymbol)(
                    _attributeConstructor,
                    attributeCtor,
                    Nothing)

                Interlocked.CompareExchange(Of NamedTypeSymbol)(
                      _attributeClass,
                      DirectCast(attributeClass, NamedTypeSymbol),
                      Nothing)
            End If
        End Sub

        Friend Overrides ReadOnly Property HasErrors As Boolean
            Get
                If _lazyHasErrors = ThreeState.Unknown Then
                    EnsureClassAndConstructorSymbols()
                    EnsureLazyMembersAreLoaded()

                    If _lazyHasErrors = ThreeState.Unknown Then
                        _lazyHasErrors = ThreeState.False
                    End If
                End If

                Return _lazyHasErrors.Value
            End Get
        End Property

        Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                If HasErrors Then

                    If AttributeConstructor IsNot Nothing Then
                        Return If(AttributeConstructor.GetUseSiteInfo().DiagnosticInfo,
                                  ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedType1, String.Empty))

                    ElseIf AttributeClass IsNot Nothing Then
                        Return If(AttributeClass.GetUseSiteInfo().DiagnosticInfo,
                                  ErrorFactory.ErrorInfo(ERRID.ERR_MissingRuntimeHelper, AttributeClass.MetadataName & "." & WellKnownMemberNames.InstanceConstructorName))
                    Else
                        Return ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedType1, String.Empty)
                    End If
                Else
                    Return Nothing
                End If
            End Get
        End Property

        Friend Overrides ReadOnly Property IsConditionallyOmitted As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace

