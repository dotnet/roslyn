' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Globalization
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
    ''' The class to represent all fields imported from a PE/module.
    ''' </summary>
    Friend NotInheritable Class PEFieldSymbol
        Inherits FieldSymbol

        Private ReadOnly m_Handle As FieldDefinitionHandle
        Private ReadOnly m_Name As String
        Private ReadOnly m_Flags As FieldAttributes
        Private ReadOnly m_ContainingType As PENamedTypeSymbol
        Private m_LazyType As TypeSymbol
        Private m_LazyCustomModifiers As ImmutableArray(Of CustomModifier)
        Private m_LazyConstantValue As ConstantValue = Microsoft.CodeAnalysis.ConstantValue.Unset
        Private m_lazyDocComment As Tuple(Of CultureInfo, String)
        Private m_lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)
        Private m_lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 
        Private m_lazyObsoleteAttributeData As ObsoleteAttributeData = ObsoleteAttributeData.Uninitialized

        Friend Sub New(
            moduleSymbol As PEModuleSymbol,
            containingType As PENamedTypeSymbol,
            handle As FieldDefinitionHandle
        )
            Debug.Assert(moduleSymbol IsNot Nothing)
            Debug.Assert(containingType IsNot Nothing)
            Debug.Assert(Not handle.IsNil)

            m_Handle = handle
            m_ContainingType = containingType

            Try
                moduleSymbol.Module.GetFieldDefPropsOrThrow(handle, m_Name, m_Flags)
            Catch mrEx As BadImageFormatException
                If m_Name Is Nothing Then
                    m_Name = String.Empty
                End If

                m_lazyUseSiteErrorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedField1, Me)
            End Try
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_Name
            End Get
        End Property

        Friend ReadOnly Property FieldFlags As FieldAttributes
            Get
                Return m_Flags
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_ContainingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return m_ContainingType
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Dim access As Accessibility = Accessibility.Private

                Select Case m_Flags And FieldAttributes.FieldAccessMask
                    Case FieldAttributes.Assembly
                        access = Accessibility.Friend

                    Case FieldAttributes.FamORAssem
                        access = Accessibility.ProtectedOrFriend

                    Case FieldAttributes.FamANDAssem
                        access = Accessibility.ProtectedAndFriend

                    Case FieldAttributes.Private,
                         FieldAttributes.PrivateScope
                        access = Accessibility.Private

                    Case FieldAttributes.Public
                        access = Accessibility.Public

                    Case FieldAttributes.Family
                        access = Accessibility.Protected

                    Case Else
                        Debug.Assert(False, "Unexpected!!!")
                End Select

                Return access
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If m_lazyCustomAttributes.IsDefault Then
                Dim containingPEModuleSymbol = DirectCast(ContainingModule(), PEModuleSymbol)

                Dim filterOutConstantAttributeDescription As AttributeDescription = GetConstantAttributeDescription()

                If filterOutConstantAttributeDescription.Signatures IsNot Nothing Then
                    Dim constantAttribute As CustomAttributeHandle
                    Dim attributes = containingPEModuleSymbol.GetCustomAttributesForToken(
                        m_Handle,
                        constantAttribute,
                        filterOutConstantAttributeDescription)

                    ImmutableInterlocked.InterlockedInitialize(m_lazyCustomAttributes, attributes)
                Else
                    containingPEModuleSymbol.LoadCustomAttributes(m_Handle, m_lazyCustomAttributes)
                End If
            End If

            Return m_lazyCustomAttributes
        End Function

        Private Function GetConstantAttributeDescription() As AttributeDescription
            Dim value As ConstantValue

            If Me.Type.SpecialType = SpecialType.System_DateTime Then
                value = GetConstantValue(SymbolsInProgress(Of FieldSymbol).Empty)
                If value IsNot Nothing AndAlso value.Discriminator = ConstantValueTypeDiscriminator.DateTime Then
                    Return AttributeDescription.DateTimeConstantAttribute
                End If
            ElseIf Me.Type.SpecialType = SpecialType.System_Decimal Then
                value = GetConstantValue(SymbolsInProgress(Of FieldSymbol).Empty)
                If value IsNot Nothing AndAlso value.Discriminator = ConstantValueTypeDiscriminator.Decimal Then
                    Return AttributeDescription.DecimalConstantAttribute
                End If
            End If

            Return Nothing
        End Function

        Friend Overrides Iterator Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            For Each attribute In GetAttributes()
                Yield attribute
            Next

            ' Yield hidden attributes last, order might be important.
            Dim filteredOutConstantAttributeDescription As AttributeDescription = GetConstantAttributeDescription()

            If filteredOutConstantAttributeDescription.Signatures IsNot Nothing Then
                Dim containingPEModuleSymbol = DirectCast(ContainingModule(), PEModuleSymbol)
                Yield New PEAttributeData(containingPEModuleSymbol,
                                          containingPEModuleSymbol.Module.FindLastTargetAttribute(Me.m_Handle, filteredOutConstantAttributeDescription).Handle)
            End If
        End Function

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return (m_Flags And FieldAttributes.SpecialName) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return (m_Flags And FieldAttributes.RTSpecialName) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Return (m_Flags And FieldAttributes.NotSerialized) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return (m_Flags And FieldAttributes.InitOnly) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsConst As Boolean
            Get
                Return (m_Flags And FieldAttributes.Literal) <> 0 OrElse GetConstantValue(SymbolsInProgress(Of FieldSymbol).Empty) IsNot Nothing
            End Get
        End Property

        Friend Overrides Function GetConstantValue(inProgress As SymbolsInProgress(Of FieldSymbol)) As ConstantValue
            If m_LazyConstantValue Is Microsoft.CodeAnalysis.ConstantValue.Unset Then
                Dim value As ConstantValue = Nothing

                If (m_Flags And FieldAttributes.Literal) <> 0 Then
                    value = m_ContainingType.ContainingPEModule.Module.GetConstantFieldValue(m_Handle)
                    value = AdjustConstantValueFromMetadata(value, Me.Type, False)
                    Dim selfOrUnderlyingType = Me.Type.GetEnumUnderlyingTypeOrSelf
                    Dim selfOrUnderlyingSpecialType = selfOrUnderlyingType.SpecialType

                    ' handle nothing literal conversions
                    If value.IsNothing Then
                        ' assign a numerical 0 to numeric types
                        If Me.Type.GetEnumUnderlyingTypeOrSelf.IsNumericType Then
                            value = Microsoft.CodeAnalysis.ConstantValue.Default(
                            Microsoft.CodeAnalysis.ConstantValue.GetDiscriminator(selfOrUnderlyingSpecialType))
                        ElseIf selfOrUnderlyingSpecialType = SpecialType.System_DateTime Then
                            ' assign the default value for DateTime
                            value = Microsoft.CodeAnalysis.ConstantValue.Default(ConstantValueTypeDiscriminator.DateTime)
                        ElseIf selfOrUnderlyingSpecialType = SpecialType.System_Boolean Then
                            ' assign the default value for Boolean
                            value = Microsoft.CodeAnalysis.ConstantValue.Default(ConstantValueTypeDiscriminator.Boolean)
                        ElseIf selfOrUnderlyingSpecialType = SpecialType.System_Char Then
                            ' assign the default value for Char
                            value = Microsoft.CodeAnalysis.ConstantValue.Default(ConstantValueTypeDiscriminator.Char)
                        Else
                            ' This case handles System.Object (last remaining type of VB's primitives types) and
                            ' all type where const folding of a nothing reference would succeed (Dev10 allows importing constants 
                            ' of arbitrary reference types with a constant value of "nothing"). So we are allowing a bit more than
                            ' the spec defines.
                            If selfOrUnderlyingType.IsErrorType OrElse
                               Conversions.TryFoldNothingReferenceConversion(value,
                                                                             ConversionKind.WideningNothingLiteral,
                                                                             selfOrUnderlyingType) Is Nothing Then

                                ' type parameters and structures are not supported to be initialized with nothing
                                value = Microsoft.CodeAnalysis.ConstantValue.Bad
                            End If
                        End If

                    ElseIf value.SpecialType <> selfOrUnderlyingSpecialType Then
                        ' If the constant value from metadata is invalid (has a different type), mark the value as bad.
                        value = Microsoft.CodeAnalysis.ConstantValue.Bad
                    End If
                End If

                ' If this is a DateTime or Decimal, the constant value comes from an attributes (VB assumption).
                ' This also means that these values overwrite constant values that have been generated by non VB compilers.
                Dim defaultValue As ConstantValue = CodeAnalysis.ConstantValue.NotAvailable

                If Me.Type.SpecialType = SpecialType.System_DateTime Then
                    If PEModule.HasDateTimeConstantAttribute(Handle, defaultValue) Then
                        value = defaultValue
                    End If
                ElseIf Me.Type.SpecialType = SpecialType.System_Decimal Then
                    If PEModule.HasDecimalConstantAttribute(Handle, defaultValue) Then
                        value = defaultValue
                    End If
                End If

                Interlocked.CompareExchange(m_LazyConstantValue,
                                            value,
                                            Microsoft.CodeAnalysis.ConstantValue.Unset)
            End If

            Return m_LazyConstantValue
        End Function

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return (m_Flags And FieldAttributes.Static) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                ' the compiler doesn't need full marshalling information, just the unmanaged type
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(m_lazyObsoleteAttributeData, m_Handle, DirectCast(ContainingModule, PEModuleSymbol))
                Return m_lazyObsoleteAttributeData
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return (m_Flags And FieldAttributes.HasFieldMarshal) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingType As UnmanagedType
            Get
                If (m_Flags And FieldAttributes.HasFieldMarshal) = 0 Then
                    Return Nothing
                End If

                Return PEModule.GetMarshallingType(m_Handle)
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                If (m_Flags And FieldAttributes.HasFieldMarshal) = 0 Then
                    Return Nothing
                End If

                Return PEModule.GetMarshallingDescriptor(m_Handle)
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Return PEModule.GetFieldOffset(m_Handle)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return StaticCast(Of Location).From(m_ContainingType.ContainingPEModule.MetadataLocation)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Private Sub EnsureSignatureIsLoaded()
            If m_LazyType Is Nothing Then
                Dim moduleSymbol = m_ContainingType.ContainingPEModule
                Dim customModifiers As ImmutableArray(Of MetadataDecoder.ModifierInfo) = Nothing
                Dim type As TypeSymbol = New MetadataDecoder(moduleSymbol, m_ContainingType).DecodeFieldSignature(m_Handle, Nothing, customModifiers)

                ImmutableInterlocked.InterlockedCompareExchange(m_LazyCustomModifiers, VisualBasicCustomModifier.Convert(customModifiers), Nothing)
                Interlocked.CompareExchange(m_LazyType, type, Nothing)
            End If
        End Sub

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                EnsureSignatureIsLoaded()
                Return m_LazyType
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                EnsureSignatureIsLoaded()
                Return m_LazyCustomModifiers
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            ' Note: m_LazyDocComment is passed ByRef
            Return PEDocumentationCommentUtils.GetDocumentationComment(
                Me, m_ContainingType.ContainingPEModule, preferredCulture, cancellationToken, m_lazyDocComment)
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            If m_lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                Dim fieldUseSiteErrorInfo = CalculateUseSiteErrorInfo()

                ' if there was no previous use site error for this symbol, check the constant value
                If fieldUseSiteErrorInfo Is Nothing Then

                    ' report use site errors for invalid constant values 
                    Dim constantValue = GetConstantValue(SymbolsInProgress(Of FieldSymbol).Empty)
                    If constantValue IsNot Nothing AndAlso
                        constantValue.IsBad Then
                        fieldUseSiteErrorInfo = New DiagnosticInfo(MessageProvider.Instance,
                                                                   ERRID.ERR_UnsupportedConstant2,
                                                                   Me.ContainingType,
                                                                   Me.Name)
                    End If
                End If

                m_lazyUseSiteErrorInfo = fieldUseSiteErrorInfo
            End If

            Return m_lazyUseSiteErrorInfo
        End Function

        Friend ReadOnly Property Handle As FieldDefinitionHandle
            Get
                Return m_Handle
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
                Return DirectCast(ContainingModule, PEModuleSymbol).Module
            End Get
        End Property
    End Class

End Namespace
