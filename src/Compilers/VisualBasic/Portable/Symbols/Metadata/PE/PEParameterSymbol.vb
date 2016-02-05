' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Reflection
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' The class to represent all types imported from a PE/module.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class PEParameterSymbol
        Inherits ParameterSymbol

        Private ReadOnly _containingSymbol As Symbol
        Private ReadOnly _name As String
        Private ReadOnly _type As TypeSymbol
        Private ReadOnly _handle As ParameterHandle
        Private ReadOnly _flags As ParameterAttributes
        Private ReadOnly _ordinal As UShort

        ' Layout
        ' |.....|c|n|r|
        '
        ' r = isByRef - 1 bit (bool)
        ' n = hasNameInMetadata - 1 bit (bool)
        ' c = hasOptionCompare - 1 bit (bool)
        Private ReadOnly _packed As Byte
        Private Const s_isByRefMask As Integer = &H1
        Private Const s_hasNameInMetadataMask As Integer = &H2
        Private Const s_hasOptionCompareMask As Integer = &H4

        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)
        Private _lazyDefaultValue As ConstantValue = ConstantValue.Unset

        ' TODO: We should consider merging these in a single bit field
        Private _lazyHasIDispatchConstantAttribute As ThreeState = ThreeState.Unknown
        Private _lazyHasIUnknownConstantAttribute As ThreeState = ThreeState.Unknown
        Private _lazyHasCallerLineNumberAttribute As ThreeState = ThreeState.Unknown
        Private _lazyHasCallerMemberNameAttribute As ThreeState = ThreeState.Unknown
        Private _lazyHasCallerFilePathAttribute As ThreeState = ThreeState.Unknown

        Private _lazyIsParamArray As ThreeState

        ''' <summary>
        ''' Attributes filtered out from m_lazyCustomAttributes, ParamArray, etc.
        ''' </summary>
        Private _lazyHiddenAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Friend Shared Function Create(
            moduleSymbol As PEModuleSymbol,
            containingSymbol As PEMethodSymbol,
            ordinal As Integer,
            ByRef parameter As ParamInfo(Of TypeSymbol),
            <Out> ByRef isBad As Boolean
        ) As PEParameterSymbol
            Return Create(moduleSymbol, containingSymbol, ordinal, parameter.IsByRef, parameter.CountOfCustomModifiersPrecedingByRef, parameter.Type, parameter.Handle, parameter.CustomModifiers, isBad)
        End Function

        Friend Shared Function Create(
            containingSymbol As Symbol,
            name As String,
            isByRef As Boolean,
            countOfCustomModifiersPrecedingByRef As UShort,
            type As TypeSymbol,
            handle As ParameterHandle,
            flags As ParameterAttributes,
            isParamArray As Boolean,
            hasOptionCompare As Boolean,
            ordinal As Integer,
            defaultValue As ConstantValue,
            customModifiers As ImmutableArray(Of CustomModifier)
        ) As PEParameterSymbol
            If customModifiers.IsDefaultOrEmpty Then
                Return New PEParameterSymbol(containingSymbol, name, isByRef, type, handle, flags, isParamArray, hasOptionCompare, ordinal, defaultValue)
            End If

            Return New PEParameterSymbolWithCustomModifiers(containingSymbol, name, isByRef, countOfCustomModifiersPrecedingByRef, type, handle, flags,
                                                            isParamArray, hasOptionCompare, ordinal, defaultValue, customModifiers)
        End Function

        Private Sub New(
            containingSymbol As Symbol,
            name As String,
            isByRef As Boolean,
            type As TypeSymbol,
            handle As ParameterHandle,
            flags As ParameterAttributes,
            isParamArray As Boolean,
            hasOptionCompare As Boolean,
            ordinal As Integer,
            defaultValue As ConstantValue
        )

            Debug.Assert(containingSymbol IsNot Nothing)
            Debug.Assert(ordinal >= 0)
            Debug.Assert(type IsNot Nothing)

            _containingSymbol = containingSymbol
            Dim hasNameInMetadata As Boolean
            _name = EnsureParameterNameNotEmpty(name, hasNameInMetadata)
            _type = type
            _handle = handle
            _ordinal = CType(ordinal, UShort)
            _flags = flags
            _lazyIsParamArray = isParamArray.ToThreeState()
            _lazyDefaultValue = defaultValue

            _packed = Pack(isByRef, hasNameInMetadata, hasOptionCompare)
            Debug.Assert(ordinal = Me.Ordinal)
            Debug.Assert(isByRef = Me.IsByRef)
            Debug.Assert(hasOptionCompare = Me.HasOptionCompare)
            Debug.Assert(hasNameInMetadata = Me.HasNameInMetadata)
        End Sub

        Private Shared Function Create(
            moduleSymbol As PEModuleSymbol,
            containingSymbol As Symbol,
            ordinal As Integer,
            isByRef As Boolean,
            countOfCustomModifiersPrecedingByRef As UShort,
            type As TypeSymbol,
            handle As ParameterHandle,
            customModifiers As ImmutableArray(Of ModifierInfo(Of TypeSymbol)),
            <Out> ByRef isBad As Boolean
        ) As PEParameterSymbol

            If customModifiers.IsDefaultOrEmpty Then
                Return New PEParameterSymbol(moduleSymbol, containingSymbol, ordinal, isByRef, type, handle, isBad)
            End If

            Return New PEParameterSymbolWithCustomModifiers(moduleSymbol, containingSymbol, ordinal, isByRef, countOfCustomModifiersPrecedingByRef, type, handle, customModifiers, isBad)
        End Function

        Private Sub New(
            moduleSymbol As PEModuleSymbol,
            containingSymbol As Symbol,
            ordinal As Integer,
            isByRef As Boolean,
            type As TypeSymbol,
            handle As ParameterHandle,
            <Out> ByRef isBad As Boolean
        )
            Debug.Assert(moduleSymbol IsNot Nothing)
            Debug.Assert(containingSymbol IsNot Nothing)
            Debug.Assert(ordinal >= 0)
            Debug.Assert(type IsNot Nothing)

            isBad = False
            _containingSymbol = containingSymbol
            _type = type
            _ordinal = CType(ordinal, UShort)
            _handle = handle

            Dim hasOptionCompare As Boolean = False

            If handle.IsNil Then
                _lazyCustomAttributes = ImmutableArray(Of VisualBasicAttributeData).Empty
                _lazyHiddenAttributes = ImmutableArray(Of VisualBasicAttributeData).Empty
                _lazyHasIDispatchConstantAttribute = ThreeState.False
                _lazyHasIUnknownConstantAttribute = ThreeState.False
                _lazyDefaultValue = ConstantValue.NotAvailable
                _lazyHasCallerLineNumberAttribute = ThreeState.False
                _lazyHasCallerMemberNameAttribute = ThreeState.False
                _lazyHasCallerFilePathAttribute = ThreeState.False
                _lazyIsParamArray = ThreeState.False
            Else
                Try
                    moduleSymbol.Module.GetParamPropsOrThrow(handle, _name, _flags)
                Catch mrEx As BadImageFormatException
                    isBad = True
                End Try

                hasOptionCompare = moduleSymbol.Module.HasAttribute(handle, AttributeDescription.OptionCompareAttribute)
            End If

            Dim hasNameInMetadata As Boolean
            _name = EnsureParameterNameNotEmpty(_name, hasNameInMetadata)

            _packed = Pack(isByRef, hasNameInMetadata, hasOptionCompare)
            Debug.Assert(isByRef = Me.IsByRef)
            Debug.Assert(hasOptionCompare = Me.HasOptionCompare)
            Debug.Assert(hasNameInMetadata = Me.HasNameInMetadata)
        End Sub

        Private NotInheritable Class PEParameterSymbolWithCustomModifiers
            Inherits PEParameterSymbol

            Private ReadOnly _customModifiers As ImmutableArray(Of CustomModifier)
            Private ReadOnly _countOfCustomModifiersPrecedingByRef As UShort

            Public Sub New(
                containingSymbol As Symbol,
                name As String,
                isByRef As Boolean,
                countOfCustomModifiersPrecedingByRef As UShort,
                type As TypeSymbol,
                handle As ParameterHandle,
                flags As ParameterAttributes,
                isParamArray As Boolean,
                hasOptionCompare As Boolean,
                ordinal As Integer,
                defaultValue As ConstantValue,
                customModifiers As ImmutableArray(Of CustomModifier)
            )
                MyBase.New(containingSymbol, name, isByRef, type, handle, flags, isParamArray, hasOptionCompare, ordinal, defaultValue)

                _customModifiers = customModifiers
                _countOfCustomModifiersPrecedingByRef = countOfCustomModifiersPrecedingByRef

                Debug.Assert(_countOfCustomModifiersPrecedingByRef = 0 OrElse isByRef)
                Debug.Assert(_countOfCustomModifiersPrecedingByRef <= _customModifiers.Length)
            End Sub

            Public Sub New(
                moduleSymbol As PEModuleSymbol,
                containingSymbol As Symbol,
                ordinal As Integer,
                isByRef As Boolean,
                countOfCustomModifiersPrecedingByRef As UShort,
                type As TypeSymbol,
                handle As ParameterHandle,
                customModifiers As ImmutableArray(Of ModifierInfo(Of TypeSymbol)),
                <Out> ByRef isBad As Boolean
            )
                MyBase.New(moduleSymbol, containingSymbol, ordinal, isByRef, type, handle, isBad)

                _customModifiers = VisualBasicCustomModifier.Convert(customModifiers)
                _countOfCustomModifiersPrecedingByRef = countOfCustomModifiersPrecedingByRef

                Debug.Assert(_countOfCustomModifiersPrecedingByRef = 0 OrElse isByRef)
                Debug.Assert(_countOfCustomModifiersPrecedingByRef <= _customModifiers.Length)
            End Sub

            Friend Overrides ReadOnly Property CountOfCustomModifiersPrecedingByRef As UShort
                Get
                    Return _countOfCustomModifiersPrecedingByRef
                End Get
            End Property

            Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _customModifiers
                End Get
            End Property
        End Class

        Private Shared Function Pack(isByRef As Boolean, hasNameInMetadata As Boolean, hasOptionCompare As Boolean) As Byte
            Dim isByRefBits As Integer = If(isByRef, s_isByRefMask, 0)
            Dim hasNoNameInMetadataBits As Integer = If(hasNameInMetadata, s_hasNameInMetadataMask, 0)
            Dim hasOptionCompareBits As Integer = If(hasOptionCompare, s_hasOptionCompareMask, 0)
            Return CType(isByRefBits Or hasNoNameInMetadataBits Or hasOptionCompareBits, Byte)
        End Function

        Private Shared Function EnsureParameterNameNotEmpty(name As String, <Out> ByRef hasNameInMetadata As Boolean) As String
            hasNameInMetadata = Not String.IsNullOrEmpty(name)
            Return If(hasNameInMetadata, name, "Param")
        End Function

        Private ReadOnly Property HasNameInMetadata As Boolean
            Get
                Return (_packed And s_hasNameInMetadataMask) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return If(HasNameInMetadata, _name, String.Empty)
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend ReadOnly Property ParamFlags As ParameterAttributes
            Get
                Return _flags
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingSymbol
            End Get
        End Property

        Friend Overrides ReadOnly Property HasMetadataConstantValue As Boolean
            Get
                Return (_flags And ParameterAttributes.HasDefault) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                If _lazyDefaultValue Is ConstantValue.Unset Then
                    Debug.Assert(Not _handle.IsNil)

                    Dim defaultValue As ConstantValue = ConstantValue.NotAvailable

                    Dim peModule = Me.PEModule
                    Dim handle = Me._handle

                    If (_flags And ParameterAttributes.HasDefault) <> 0 Then
                        defaultValue = peModule.GetParamDefaultValue(handle)
                    ElseIf IsOptional
                        ' Dev10 behavior just checks for Decimal then DateTime.  If both are specified, DateTime wins
                        ' regardless of the parameter's type.

                        If Not peModule.HasDateTimeConstantAttribute(handle, defaultValue) Then
                            peModule.HasDecimalConstantAttribute(handle, defaultValue)
                        End If
                    End If

                    Interlocked.CompareExchange(_lazyDefaultValue, defaultValue, ConstantValue.Unset)
                End If

                Return _lazyDefaultValue
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOptional As Boolean
            Get
                Return (_flags And ParameterAttributes.Optional) <> 0
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If _lazyCustomAttributes.IsDefault Then
                Debug.Assert(Not _handle.IsNil)

                Dim containingPEModuleSymbol = DirectCast(_containingSymbol.ContainingModule, PEModuleSymbol)

                ' Filter out ParamArrayAttributes if necessary and cache the attribute handle
                ' for GetCustomAttributesToEmit
                Dim filterOutParamArrayAttribute As Boolean = (Not _lazyIsParamArray.HasValue() OrElse _lazyIsParamArray.Value())

                Dim defaultValue As ConstantValue = Me.ExplicitDefaultConstantValue
                Dim filterOutConstantAttributeDescription As AttributeDescription = Nothing

                If defaultValue IsNot Nothing Then
                    If defaultValue.Discriminator = ConstantValueTypeDiscriminator.DateTime Then
                        filterOutConstantAttributeDescription = AttributeDescription.DateTimeConstantAttribute
                    ElseIf defaultValue.Discriminator = ConstantValueTypeDiscriminator.Decimal Then
                        filterOutConstantAttributeDescription = AttributeDescription.DecimalConstantAttribute
                    End If
                End If

                If filterOutParamArrayAttribute OrElse filterOutConstantAttributeDescription.Signatures IsNot Nothing Then
                    Dim paramArrayAttribute As CustomAttributeHandle
                    Dim constantAttribute As CustomAttributeHandle
                    Dim attributes = containingPEModuleSymbol.GetCustomAttributesForToken(
                        _handle,
                        paramArrayAttribute,
                        If(filterOutParamArrayAttribute, AttributeDescription.ParamArrayAttribute, Nothing),
                        constantAttribute,
                        filterOutConstantAttributeDescription)

                    If Not paramArrayAttribute.IsNil OrElse Not constantAttribute.IsNil Then
                        Dim builder = ArrayBuilder(Of VisualBasicAttributeData).GetInstance()

                        If Not paramArrayAttribute.IsNil Then
                            builder.Add(New PEAttributeData(containingPEModuleSymbol, paramArrayAttribute))
                        End If

                        If Not constantAttribute.IsNil Then
                            builder.Add(New PEAttributeData(containingPEModuleSymbol, constantAttribute))
                        End If

                        ImmutableInterlocked.InterlockedInitialize(_lazyHiddenAttributes, builder.ToImmutableAndFree())
                    Else
                        ImmutableInterlocked.InterlockedInitialize(_lazyHiddenAttributes, ImmutableArray(Of VisualBasicAttributeData).Empty)
                    End If

                    If Not _lazyIsParamArray.HasValue() Then
                        Debug.Assert(filterOutParamArrayAttribute)
                        _lazyIsParamArray = (Not paramArrayAttribute.IsNil).ToThreeState()
                    End If

                    ImmutableInterlocked.InterlockedInitialize(_lazyCustomAttributes, attributes)
                Else
                    ImmutableInterlocked.InterlockedInitialize(_lazyHiddenAttributes, ImmutableArray(Of VisualBasicAttributeData).Empty)
                    containingPEModuleSymbol.LoadCustomAttributes(_handle, _lazyCustomAttributes)
                End If
            End If

            Debug.Assert(Not _lazyHiddenAttributes.IsDefault)
            Return _lazyCustomAttributes
        End Function

        Friend Overrides Iterator Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            For Each attribute In GetAttributes()
                Yield attribute
            Next

            ' Yield hidden attributes last, order might be important.
            For Each attribute In _lazyHiddenAttributes
                Yield attribute
            Next

        End Function

        Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return IsOptional AndAlso ExplicitDefaultConstantValue IsNot Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return (_flags And ParameterAttributes.Optional) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                If Not _lazyIsParamArray.HasValue() Then
                    _lazyIsParamArray = Me.PEModule.HasParamsAttribute(_handle).ToThreeState()
                End If
                Return _lazyIsParamArray.Value()
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _containingSymbol.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return _type
            End Get
        End Property

        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return (_packed And s_isByRefMask) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return IsByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return (_flags And ParameterAttributes.Out) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return (_flags And ParameterAttributes.In) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return (_packed And s_hasOptionCompareMask) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return (_flags And ParameterAttributes.HasFieldMarshal) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                ' the compiler doesn't need full marshalling information, just the unmanaged type
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                If (_flags And ParameterAttributes.HasFieldMarshal) = 0 Then
                    Return Nothing
                End If

                Debug.Assert(Not _handle.IsNil)
                Return Me.PEModule.GetMarshallingDescriptor(_handle)
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingType As UnmanagedType
            Get
                If (_flags And ParameterAttributes.HasFieldMarshal) = 0 Then
                    Return Nothing
                End If

                Debug.Assert(Not _handle.IsNil)
                Return Me.PEModule.GetMarshallingType(_handle)
            End Get
        End Property

        ' May be Nil
        Friend ReadOnly Property Handle As ParameterHandle
            Get
                Return _handle
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                If _lazyHasIDispatchConstantAttribute = ThreeState.Unknown Then
                    Debug.Assert(Not _handle.IsNil)

                    _lazyHasIDispatchConstantAttribute = Me.PEModule.
                        HasAttribute(_handle, AttributeDescription.IDispatchConstantAttribute).ToThreeState()
                End If

                Return _lazyHasIDispatchConstantAttribute.Value
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                If _lazyHasIUnknownConstantAttribute = ThreeState.Unknown Then
                    Debug.Assert(Not _handle.IsNil)

                    _lazyHasIUnknownConstantAttribute = Me.PEModule.
                        HasAttribute(_handle, AttributeDescription.IUnknownConstantAttribute).ToThreeState()
                End If

                Return _lazyHasIUnknownConstantAttribute.Value
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                If _lazyHasCallerLineNumberAttribute = ThreeState.Unknown Then
                    Debug.Assert(Not _handle.IsNil)

                    _lazyHasCallerLineNumberAttribute = Me.PEModule.
                        HasAttribute(_handle, AttributeDescription.CallerLineNumberAttribute).ToThreeState()
                End If

                Return _lazyHasCallerLineNumberAttribute.Value
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                If _lazyHasCallerMemberNameAttribute = ThreeState.Unknown Then
                    Debug.Assert(Not _handle.IsNil)

                    _lazyHasCallerMemberNameAttribute = Me.PEModule.
                        HasAttribute(_handle, AttributeDescription.CallerMemberNameAttribute).ToThreeState()
                End If

                Return _lazyHasCallerMemberNameAttribute.Value
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                If _lazyHasCallerFilePathAttribute = ThreeState.Unknown Then
                    Debug.Assert(Not _handle.IsNil)

                    _lazyHasCallerFilePathAttribute = Me.PEModule.
                        HasAttribute(_handle, AttributeDescription.CallerFilePathAttribute).ToThreeState()
                End If

                Return _lazyHasCallerFilePathAttribute.Value
            End Get
        End Property

        Friend Overrides ReadOnly Property CountOfCustomModifiersPrecedingByRef As UShort
            Get
                Return 0
            End Get
        End Property

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property PEModule As PEModule
            Get
                Return DirectCast(_containingSymbol.ContainingModule, PEModuleSymbol).Module
            End Get
        End Property
    End Class
End Namespace
