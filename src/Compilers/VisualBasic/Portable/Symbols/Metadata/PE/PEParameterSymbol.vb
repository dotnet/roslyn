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
    Friend NotInheritable Class PEParameterSymbol
        Inherits ParameterSymbol

        Private ReadOnly m_ContainingSymbol As Symbol
        Private ReadOnly m_Name As String
        Private ReadOnly m_Type As TypeSymbol
        Private ReadOnly m_Handle As ParameterHandle
        Private ReadOnly m_Flags As ParameterAttributes
        Private ReadOnly m_CustomModifiers As ImmutableArray(Of CustomModifier)
        Private ReadOnly m_Ordinal As UShort

        ' Layout
        ' |.....|c|h|r|
        '
        ' r = isByRef - 1 bit (bool)
        ' h = hasByRefBeforeCustomModifiers - 1 bit (bool)
        ' c = hasOptionCompare - 1 bit (bool)
        Private ReadOnly m_Packed As Byte
        Private Const isByRefMask As Integer = &H1
        Private Const hasByRefBeforeCustomModifiersMask As Integer = &H2
        Private Const hasOptionCompareMask As Integer = &H4

        Private m_lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)
        Private m_lazyDefaultValue As ConstantValue = ConstantValue.Unset

        ' TODO: We should consider merging these in a single bit field
        Private m_lazyHasIDispatchConstantAttribute As ThreeState = ThreeState.Unknown
        Private m_lazyHasIUnknownConstantAttribute As ThreeState = ThreeState.Unknown
        Private m_lazyHasCallerLineNumberAttribute As ThreeState = ThreeState.Unknown
        Private m_lazyHasCallerMemberNameAttribute As ThreeState = ThreeState.Unknown
        Private m_lazyHasCallerFilePathAttribute As ThreeState = ThreeState.Unknown

        Private m_lazyIsParamArray As ThreeState

        ''' <summary>
        ''' Attributes filtered out from m_lazyCustomAttributes, ParamArray, etc.
        ''' </summary>
        Private m_lazyHiddenAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Friend Sub New(
            moduleSymbol As PEModuleSymbol,
            containingSymbol As PEMethodSymbol,
            ordinal As Integer,
            ByRef parameter As ParamInfo(Of TypeSymbol),
            <Out> ByRef isBad As Boolean
        )
            Me.New(moduleSymbol, containingSymbol, ordinal, parameter.IsByRef, parameter.HasByRefBeforeCustomModifiers, parameter.Type, parameter.Handle, parameter.CustomModifiers, isBad)
        End Sub

        Friend Sub New(
            containingSymbol As Symbol,
            name As String,
            isByRef As Boolean,
            hasByRefBeforeCustomModifiers As Boolean,
            type As TypeSymbol,
            handle As ParameterHandle,
            flags As ParameterAttributes,
            isParamArray As Boolean,
            hasOptionCompare As Boolean,
            ordinal As Integer,
            defaultValue As ConstantValue,
            customModifiers As ImmutableArray(Of CustomModifier))

            Debug.Assert(containingSymbol IsNot Nothing)
            Debug.Assert(ordinal >= 0)
            Debug.Assert(type IsNot Nothing)

            m_ContainingSymbol = containingSymbol
            m_Name = EnsureParameterNameNotEmpty(name)
            m_Type = type
            m_Handle = handle
            m_Ordinal = CType(ordinal, UShort)
            m_Flags = flags
            m_lazyIsParamArray = isParamArray.ToThreeState()
            m_lazyDefaultValue = defaultValue
            m_CustomModifiers = customModifiers

            m_Packed = Pack(isByRef, hasByRefBeforeCustomModifiers, hasOptionCompare)
            Debug.Assert(ordinal = Me.Ordinal)
            Debug.Assert(isByRef = Me.IsByRef)
            Debug.Assert(hasByRefBeforeCustomModifiers = Me.HasByRefBeforeCustomModifiers)
            Debug.Assert(hasOptionCompare = Me.HasOptionCompare)
        End Sub

        Private Sub New(
            moduleSymbol As PEModuleSymbol,
            containingSymbol As Symbol,
            ordinal As Integer,
            isByRef As Boolean,
            hasByRefBeforeCustomModifiers As Boolean,
            type As TypeSymbol,
            handle As ParameterHandle,
            customModifiers As ImmutableArray(Of ModifierInfo(Of TypeSymbol)),
            <Out> ByRef isBad As Boolean
        )
            Debug.Assert(moduleSymbol IsNot Nothing)
            Debug.Assert(containingSymbol IsNot Nothing)
            Debug.Assert(ordinal >= 0)
            Debug.Assert(type IsNot Nothing)

            isBad = False
            m_ContainingSymbol = containingSymbol
            m_Type = type
            m_CustomModifiers = VisualBasicCustomModifier.Convert(customModifiers)
            m_Ordinal = CType(ordinal, UShort)
            m_Handle = handle

            Dim hasOptionCompare As Boolean = False

            If handle.IsNil Then
                m_lazyCustomAttributes = ImmutableArray(Of VisualBasicAttributeData).Empty
                m_lazyHiddenAttributes = ImmutableArray(Of VisualBasicAttributeData).Empty
                m_lazyHasIDispatchConstantAttribute = ThreeState.False
                m_lazyHasIUnknownConstantAttribute = ThreeState.False
                m_lazyDefaultValue = ConstantValue.NotAvailable
                m_lazyHasCallerLineNumberAttribute = ThreeState.False
                m_lazyHasCallerMemberNameAttribute = ThreeState.False
                m_lazyHasCallerFilePathAttribute = ThreeState.False
                m_lazyIsParamArray = ThreeState.False
            Else
                Try
                    moduleSymbol.Module.GetParamPropsOrThrow(handle, m_Name, m_Flags)
                Catch mrEx As BadImageFormatException
                    isBad = True
                End Try

                hasOptionCompare = moduleSymbol.Module.HasAttribute(handle, AttributeDescription.OptionCompareAttribute)
            End If

            m_Name = EnsureParameterNameNotEmpty(m_Name)

            m_Packed = Pack(isByRef, hasByRefBeforeCustomModifiers, hasOptionCompare)
            Debug.Assert(isByRef = Me.IsByRef)
            Debug.Assert(hasByRefBeforeCustomModifiers = Me.HasByRefBeforeCustomModifiers)
            Debug.Assert(hasOptionCompare = Me.HasOptionCompare)
        End Sub

        Private Shared Function Pack(isByRef As Boolean, hasByRefBeforeCustomModifiers As Boolean, hasOptionCompare As Boolean) As Byte
            Debug.Assert((Not hasByRefBeforeCustomModifiers) OrElse isByRef)
            Dim isByRefBits As Integer = If(isByRef, isByRefMask, 0)
            Dim hasByRefBeforeCustomModifiersBits As Integer = If(hasByRefBeforeCustomModifiers, hasByRefBeforeCustomModifiersMask, 0)
            Dim hasOptionCompareBits As Integer = If(hasOptionCompare, hasOptionCompareMask, 0)
            Return CType(isByRefBits Or hasByRefBeforeCustomModifiersBits Or hasOptionCompareBits, Byte)
        End Function

        Private Shared Function EnsureParameterNameNotEmpty(name As String) As String
            Return If(String.IsNullOrEmpty(name), "Param", name)
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_Name
            End Get
        End Property

        Friend ReadOnly Property ParamFlags As ParameterAttributes
            Get
                Return m_Flags
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return m_Ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_ContainingSymbol
            End Get
        End Property

        Friend Overrides ReadOnly Property HasMetadataConstantValue As Boolean
            Get
                Return (m_Flags And ParameterAttributes.HasDefault) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                If m_lazyDefaultValue Is ConstantValue.Unset Then
                    Debug.Assert(Not m_Handle.IsNil)

                    Dim defaultValue As ConstantValue = ConstantValue.NotAvailable

                    If IsOptional Then
                        Dim peModule = Me.PEModule
                        Dim handle = Me.m_Handle

                        If (m_Flags And ParameterAttributes.HasDefault) <> 0 Then
                            defaultValue = peModule.GetParamDefaultValue(handle)
                        Else
                            ' Dev10 behavior just checks for Decimal then DateTime.  If both are specified, DateTime wins
                            ' regardless of the parameter's type.

                            If Not peModule.HasDateTimeConstantAttribute(handle, defaultValue) Then
                                peModule.HasDecimalConstantAttribute(handle, defaultValue)
                            End If
                        End If
                    End If

                    Interlocked.CompareExchange(m_lazyDefaultValue, defaultValue, ConstantValue.Unset)
                End If

                Return m_lazyDefaultValue
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOptional As Boolean
            Get
                Return (m_Flags And ParameterAttributes.Optional) <> 0
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If m_lazyCustomAttributes.IsDefault Then
                Debug.Assert(Not m_Handle.IsNil)

                Dim containingPEModuleSymbol = DirectCast(m_ContainingSymbol.ContainingModule, PEModuleSymbol)

                ' Filter out ParamArrayAttributes if necessary and cache the attribute handle
                ' for GetCustomAttributesToEmit
                Dim filterOutParamArrayAttribute As Boolean = (Not m_lazyIsParamArray.HasValue() OrElse m_lazyIsParamArray.Value())

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
                        m_Handle,
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

                        ImmutableInterlocked.InterlockedInitialize(m_lazyHiddenAttributes, builder.ToImmutableAndFree())
                    Else
                        ImmutableInterlocked.InterlockedInitialize(m_lazyHiddenAttributes, ImmutableArray(Of VisualBasicAttributeData).Empty)
                    End If

                    If Not m_lazyIsParamArray.HasValue() Then
                        Debug.Assert(filterOutParamArrayAttribute)
                        m_lazyIsParamArray = (Not paramArrayAttribute.IsNil).ToThreeState()
                    End If

                    ImmutableInterlocked.InterlockedInitialize(m_lazyCustomAttributes, attributes)
                Else
                    ImmutableInterlocked.InterlockedInitialize(m_lazyHiddenAttributes, ImmutableArray(Of VisualBasicAttributeData).Empty)
                    containingPEModuleSymbol.LoadCustomAttributes(m_Handle, m_lazyCustomAttributes)
                End If
            End If

            Debug.Assert(Not m_lazyHiddenAttributes.IsDefault)
            Return m_lazyCustomAttributes
        End Function

        Friend Overrides Iterator Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            For Each attribute In GetAttributes()
                Yield attribute
            Next

            ' Yield hidden attributes last, order might be important.
            For Each attribute In m_lazyHiddenAttributes
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
                Return (m_Flags And ParameterAttributes.Optional) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                If Not m_lazyIsParamArray.HasValue() Then
                    m_lazyIsParamArray = Me.PEModule.HasParamsAttribute(m_Handle).ToThreeState()
                End If
                Return m_lazyIsParamArray.Value()
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_ContainingSymbol.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return m_Type
            End Get
        End Property

        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return (m_Packed And isByRefMask) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return IsByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return (m_Flags And ParameterAttributes.Out) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return (m_Flags And ParameterAttributes.In) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return (m_Packed And hasOptionCompareMask) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return m_CustomModifiers
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return (m_Flags And ParameterAttributes.HasFieldMarshal) <> 0
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
                If (m_Flags And ParameterAttributes.HasFieldMarshal) = 0 Then
                    Return Nothing
                End If

                Debug.Assert(Not m_Handle.IsNil)
                Return Me.PEModule.GetMarshallingDescriptor(m_Handle)
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingType As UnmanagedType
            Get
                If (m_Flags And ParameterAttributes.HasFieldMarshal) = 0 Then
                    Return Nothing
                End If

                Debug.Assert(Not m_Handle.IsNil)
                Return Me.PEModule.GetMarshallingType(m_Handle)
            End Get
        End Property

        ' May be Nil
        Friend ReadOnly Property Handle As ParameterHandle
            Get
                Return m_Handle
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                If m_lazyHasIDispatchConstantAttribute = ThreeState.Unknown Then
                    Debug.Assert(Not m_Handle.IsNil)

                    m_lazyHasIDispatchConstantAttribute = Me.PEModule.
                        HasAttribute(m_Handle, AttributeDescription.IDispatchConstantAttribute).ToThreeState()
                End If

                Return m_lazyHasIDispatchConstantAttribute.Value
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                If m_lazyHasIUnknownConstantAttribute = ThreeState.Unknown Then
                    Debug.Assert(Not m_Handle.IsNil)

                    m_lazyHasIUnknownConstantAttribute = Me.PEModule.
                        HasAttribute(m_Handle, AttributeDescription.IUnknownConstantAttribute).ToThreeState()
                End If

                Return m_lazyHasIUnknownConstantAttribute.Value
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                If m_lazyHasCallerLineNumberAttribute = ThreeState.Unknown Then
                    Debug.Assert(Not m_Handle.IsNil)

                    m_lazyHasCallerLineNumberAttribute = Me.PEModule.
                        HasAttribute(m_Handle, AttributeDescription.CallerLineNumberAttribute).ToThreeState()
                End If

                Return m_lazyHasCallerLineNumberAttribute.Value
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                If m_lazyHasCallerMemberNameAttribute = ThreeState.Unknown Then
                    Debug.Assert(Not m_Handle.IsNil)

                    m_lazyHasCallerMemberNameAttribute = Me.PEModule.
                        HasAttribute(m_Handle, AttributeDescription.CallerMemberNameAttribute).ToThreeState()
                End If

                Return m_lazyHasCallerMemberNameAttribute.Value
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                If m_lazyHasCallerFilePathAttribute = ThreeState.Unknown Then
                    Debug.Assert(Not m_Handle.IsNil)

                    m_lazyHasCallerFilePathAttribute = Me.PEModule.
                        HasAttribute(m_Handle, AttributeDescription.CallerFilePathAttribute).ToThreeState()
                End If

                Return m_lazyHasCallerFilePathAttribute.Value
            End Get
        End Property

        Friend Overrides ReadOnly Property HasByRefBeforeCustomModifiers As Boolean
            Get
                Return (m_Packed And hasByRefBeforeCustomModifiersMask) <> 0
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
                Return DirectCast(m_ContainingSymbol.ContainingModule, PEModuleSymbol).Module
            End Get
        End Property
    End Class
End Namespace
