' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Private ReadOnly m_Decoder As MetadataDecoder
        Private ReadOnly m_Handle As CustomAttributeHandle
        Private m_AttributeClass As NamedTypeSymbol ' TODO - Remove class it is available from constructor. For now it is only used to know 
        ' that the attribute is fully loaded because there isn't an error method symbol.
        Private m_AttributeConstructor As MethodSymbol
        Private m_LazyConstructorArguments As TypedConstant()
        Private m_LazyNamedArguments As KeyValuePair(Of String, TypedConstant)()
        Private m_LazyHasErrors As ThreeState = ThreeState.Unknown

        Friend Sub New(moduleSymbol As PEModuleSymbol, handle As CustomAttributeHandle)
            Debug.Assert(moduleSymbol IsNot Nothing)

            m_Decoder = New MetadataDecoder(moduleSymbol)
            m_Handle = handle
        End Sub

        ''' <summary>
        ''' The attribute class.
        ''' </summary>
        Public Overrides ReadOnly Property AttributeClass As NamedTypeSymbol
            Get
                If m_AttributeClass Is Nothing Then
                    EnsureClassAndConstructorSymbols()
                End If

                Return m_AttributeClass
            End Get
        End Property

        ''' <summary>
        ''' The constructor on the attribute class.
        ''' </summary>
        Public Overrides ReadOnly Property AttributeConstructor As MethodSymbol
            Get
                If m_AttributeConstructor Is Nothing Then
                    EnsureClassAndConstructorSymbols()
                End If

                Return m_AttributeConstructor
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
                If m_LazyConstructorArguments Is Nothing Then
                    EnsureLazyMembersAreLoaded()
                End If

                Return m_LazyConstructorArguments.AsImmutableOrNull
            End Get
        End Property

        ''' <summary>
        ''' Named (property value) arguments on the attribute. 
        ''' </summary>
        Protected Overrides ReadOnly Property CommonNamedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))
            Get
                If m_LazyNamedArguments Is Nothing Then
                    EnsureLazyMembersAreLoaded()
                End If

                Return m_LazyNamedArguments.AsImmutableOrNull
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
            Return m_Decoder.IsTargetAttribute(m_Handle, namespaceName, typeName, ignoreCase)
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
        Friend Overrides Function GetTargetAttributeSignatureIndex(targetSymbol As Symbol, description As AttributeDescription) As Integer
            Return m_Decoder.GetTargetAttributeSignatureIndex(m_Handle, description)
        End Function

        Private Sub EnsureLazyMembersAreLoaded()

            If m_LazyConstructorArguments Is Nothing Then

                Dim constructorArgs As TypedConstant() = Nothing
                Dim namedArgs As KeyValuePair(Of String, TypedConstant)() = Nothing

                If Not m_Decoder.GetCustomAttribute(m_Handle, constructorArgs, namedArgs) Then
                    m_LazyHasErrors = ThreeState.True
                End If

                Debug.Assert(constructorArgs IsNot Nothing AndAlso namedArgs IsNot Nothing)

                Interlocked.CompareExchange(Of KeyValuePair(Of String, TypedConstant)())(
                      m_LazyNamedArguments,
                      namedArgs,
                      Nothing)

                Interlocked.CompareExchange(Of TypedConstant())(
                    m_LazyConstructorArguments,
                    constructorArgs,
                    Nothing)
            End If
        End Sub

        Private Sub EnsureClassAndConstructorSymbols()
            If m_AttributeClass Is Nothing Then

                Dim attributeClass As TypeSymbol = Nothing
                Dim attributeCtor As MethodSymbol = Nothing

                If Not m_Decoder.GetCustomAttribute(m_Handle, attributeClass, attributeCtor) OrElse
                    attributeClass Is Nothing Then

                    Interlocked.CompareExchange(Of NamedTypeSymbol)(
                         m_AttributeClass,
                         ErrorTypeSymbol.UnknownResultType,
                         Nothing)

                    ' Method symbol is null when there is an error.

                    m_LazyHasErrors = ThreeState.True
                    Return
                End If

                If attributeClass.IsErrorType() OrElse attributeCtor Is Nothing Then
                    m_LazyHasErrors = ThreeState.True
                End If

                Interlocked.CompareExchange(Of MethodSymbol)(
                    m_AttributeConstructor,
                    attributeCtor,
                    Nothing)

                Interlocked.CompareExchange(Of NamedTypeSymbol)(
                      m_AttributeClass,
                      DirectCast(attributeClass, NamedTypeSymbol),
                      Nothing)
            End If
        End Sub

        Friend Overrides ReadOnly Property HasErrors As Boolean
            Get
                If m_LazyHasErrors = ThreeState.Unknown Then
                    EnsureClassAndConstructorSymbols()
                    EnsureLazyMembersAreLoaded()

                    If m_LazyHasErrors = ThreeState.Unknown Then
                        m_LazyHasErrors = ThreeState.False
                    End If
                End If

                Return m_LazyHasErrors.Value
            End Get
        End Property
    End Class
End Namespace

