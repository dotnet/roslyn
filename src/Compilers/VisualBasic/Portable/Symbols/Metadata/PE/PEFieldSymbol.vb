' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' The class to represent all fields imported from a PE/module.
    ''' </summary>
    Friend NotInheritable Class PEFieldSymbol
        Inherits FieldSymbol

        ''' <summary>
        ''' This symbol is used as a type for a "fake" required custom modifier added for ByRef fields.
        ''' This allows us to report use site errors for ByRef fields, and, at the same time, allows us
        ''' to accurately match them by signature (since this instance is unique and is not used for anything else)
        ''' without adding full support for RefKind and RefCustomModifiers
        ''' </summary>
        Private Shared ReadOnly _byRefPlaceholder As New UnsupportedMetadataTypeSymbol()

        Private ReadOnly _handle As FieldDefinitionHandle
        Private ReadOnly _name As String
        Private ReadOnly _flags As FieldAttributes
        Private ReadOnly _containingType As PENamedTypeSymbol
        Private _lazyType As TypeSymbol
        Private _lazyCustomModifiers As ImmutableArray(Of CustomModifier)
        Private _lazyConstantValue As ConstantValue = Microsoft.CodeAnalysis.ConstantValue.Unset
        Private _lazyDocComment As Tuple(Of CultureInfo, String)
        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)
        Private _lazyCachedUseSiteInfo As CachedUseSiteInfo(Of AssemblySymbol) = CachedUseSiteInfo(Of AssemblySymbol).Uninitialized ' Indicates unknown state. 
        Private _lazyObsoleteAttributeData As ObsoleteAttributeData = ObsoleteAttributeData.Uninitialized
        Private _lazyIsRequired As ThreeState = ThreeState.Unknown

        Friend Sub New(
            moduleSymbol As PEModuleSymbol,
            containingType As PENamedTypeSymbol,
            handle As FieldDefinitionHandle
        )
            Debug.Assert(moduleSymbol IsNot Nothing)
            Debug.Assert(containingType IsNot Nothing)
            Debug.Assert(Not handle.IsNil)

            _handle = handle
            _containingType = containingType

            Try
                moduleSymbol.Module.GetFieldDefPropsOrThrow(handle, _name, _flags)
            Catch mrEx As BadImageFormatException
                If _name Is Nothing Then
                    _name = String.Empty
                End If

                _lazyCachedUseSiteInfo.Initialize(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedField1, Me))
            End Try
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataToken As Integer
            Get
                Return MetadataTokens.GetToken(_handle)
            End Get
        End Property

        Friend ReadOnly Property FieldFlags As FieldAttributes
            Get
                Return _flags
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Dim access As Accessibility = Accessibility.Private

                Select Case _flags And FieldAttributes.FieldAccessMask
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
                        access = Accessibility.Private
                End Select

                Return access
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If _lazyCustomAttributes.IsDefault Then
                Dim containingPEModuleSymbol = DirectCast(ContainingModule(), PEModuleSymbol)

                Dim filterOutConstantAttributeDescription As AttributeDescription = GetConstantAttributeDescription()

                If filterOutConstantAttributeDescription.Signatures IsNot Nothing Then
                    Dim constantAttribute As CustomAttributeHandle
                    Dim attributes = containingPEModuleSymbol.GetCustomAttributesForToken(
                        _handle,
                        constantAttribute,
                        filterOutConstantAttributeDescription)

                    ImmutableInterlocked.InterlockedInitialize(_lazyCustomAttributes, attributes)
                Else
                    containingPEModuleSymbol.LoadCustomAttributes(_handle, _lazyCustomAttributes)
                End If
            End If

            Return _lazyCustomAttributes
        End Function

        Private Function GetConstantAttributeDescription() As AttributeDescription
            Dim value As ConstantValue

            If Me.Type.SpecialType = SpecialType.System_DateTime Then
                value = GetConstantValue(ConstantFieldsInProgress.Empty)
                If value IsNot Nothing AndAlso value.Discriminator = ConstantValueTypeDiscriminator.DateTime Then
                    Return AttributeDescription.DateTimeConstantAttribute
                End If
            ElseIf Me.Type.SpecialType = SpecialType.System_Decimal Then
                value = GetConstantValue(ConstantFieldsInProgress.Empty)
                If value IsNot Nothing AndAlso value.Discriminator = ConstantValueTypeDiscriminator.Decimal Then
                    Return AttributeDescription.DecimalConstantAttribute
                End If
            End If

            Return Nothing
        End Function

        Friend Overrides Iterator Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            For Each attribute In GetAttributes()
                Yield attribute
            Next

            ' Yield hidden attributes last, order might be important.
            Dim filteredOutConstantAttributeDescription As AttributeDescription = GetConstantAttributeDescription()

            If filteredOutConstantAttributeDescription.Signatures IsNot Nothing Then
                Dim containingPEModuleSymbol = DirectCast(ContainingModule(), PEModuleSymbol)
                Yield New PEAttributeData(containingPEModuleSymbol,
                                          containingPEModuleSymbol.Module.FindLastTargetAttribute(Me._handle, filteredOutConstantAttributeDescription).Handle)
            End If
        End Function

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return (_flags And FieldAttributes.SpecialName) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return (_flags And FieldAttributes.RTSpecialName) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
#Disable Warning SYSLIB0050 ' 'TypeAttributes.Serializable' is obsolete
                Return (_flags And FieldAttributes.NotSerialized) <> 0
#Enable Warning SYSLIB0050
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return (_flags And FieldAttributes.InitOnly) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsConst As Boolean
            Get
                Return (_flags And FieldAttributes.Literal) <> 0 OrElse GetConstantValue(ConstantFieldsInProgress.Empty) IsNot Nothing
            End Get
        End Property

        Friend Overrides Function GetConstantValue(inProgress As ConstantFieldsInProgress) As ConstantValue
            If _lazyConstantValue Is Microsoft.CodeAnalysis.ConstantValue.Unset Then
                Dim value As ConstantValue = Nothing

                If (_flags And FieldAttributes.Literal) <> 0 Then
                    value = _containingType.ContainingPEModule.Module.GetConstantFieldValue(_handle)
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

                Interlocked.CompareExchange(_lazyConstantValue,
                                            value,
                                            Microsoft.CodeAnalysis.ConstantValue.Unset)
            End If

            Return _lazyConstantValue
        End Function

        Public Overrides ReadOnly Property IsRequired As Boolean
            Get
                If Not _lazyIsRequired.HasValue() Then
                    _lazyIsRequired = PEModule.HasAttribute(Handle, AttributeDescription.RequiredMemberAttribute).ToThreeState()
                End If

                Return _lazyIsRequired.Value()
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return (_flags And FieldAttributes.Static) <> 0
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
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(_lazyObsoleteAttributeData, _handle, DirectCast(ContainingModule, PEModuleSymbol))
                Return _lazyObsoleteAttributeData
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return (_flags And FieldAttributes.HasFieldMarshal) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingType As UnmanagedType
            Get
                If (_flags And FieldAttributes.HasFieldMarshal) = 0 Then
                    Return Nothing
                End If

                Return PEModule.GetMarshallingType(_handle)
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                If (_flags And FieldAttributes.HasFieldMarshal) = 0 Then
                    Return Nothing
                End If

                Return PEModule.GetMarshallingDescriptor(_handle)
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Return PEModule.GetFieldOffset(_handle)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return StaticCast(Of Location).From(_containingType.ContainingPEModule.MetadataLocation)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Private Sub EnsureSignatureIsLoaded()
            If _lazyType Is Nothing Then
                Dim moduleSymbol = _containingType.ContainingPEModule
                Dim fieldInfo As FieldInfo(Of TypeSymbol) = New MetadataDecoder(moduleSymbol, _containingType).DecodeFieldSignature(_handle)

                Dim type As TypeSymbol = Nothing
                Dim customModifiers As ImmutableArray(Of ModifierInfo(Of TypeSymbol)) = Nothing
                GetSignatureParts(fieldInfo, type, customModifiers)

                type = TupleTypeDecoder.DecodeTupleTypesIfApplicable(type, _handle, moduleSymbol)

                ImmutableInterlocked.InterlockedCompareExchange(_lazyCustomModifiers, VisualBasicCustomModifier.Convert(customModifiers), Nothing)
                Interlocked.CompareExchange(_lazyType, type, Nothing)
            End If
        End Sub

        Friend Shared Sub GetSignatureParts(fieldInfo As FieldInfo(Of TypeSymbol), ByRef type As TypeSymbol, ByRef customModifiers As ImmutableArray(Of ModifierInfo(Of TypeSymbol)))
            type = fieldInfo.Type
            customModifiers = fieldInfo.CustomModifiers.NullToEmpty

            If fieldInfo.IsByRef Then
                Dim refCustomModifiers = fieldInfo.RefCustomModifiers.NullToEmpty.Add(New ModifierInfo(Of TypeSymbol)(isOptional:=False, _byRefPlaceholder))
                customModifiers = refCustomModifiers.AddRange(customModifiers)
            ElseIf Not fieldInfo.RefCustomModifiers.IsDefaultOrEmpty Then
                Throw ExceptionUtilities.Unreachable
            End If
        End Sub

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                EnsureSignatureIsLoaded()
                Return _lazyType
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                EnsureSignatureIsLoaded()
                Return _lazyCustomModifiers
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            ' Note: m_LazyDocComment is passed ByRef
            Return PEDocumentationCommentUtils.GetDocumentationComment(
                Me, _containingType.ContainingPEModule, preferredCulture, cancellationToken, _lazyDocComment)
        End Function

        Friend Overrides Function GetUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            Dim primaryDependency As AssemblySymbol = Me.PrimaryDependency

            If Not _lazyCachedUseSiteInfo.IsInitialized Then
                Dim fieldUseSiteInfo = CalculateUseSiteInfo()

                If fieldUseSiteInfo.DiagnosticInfo Is Nothing Then
                    Dim errorInfo = DeriveCompilerFeatureRequiredDiagnostic(fieldUseSiteInfo)
                    If errorInfo IsNot Nothing Then
                        fieldUseSiteInfo = New UseSiteInfo(Of AssemblySymbol)(errorInfo)
                    End If
                End If

                ' if there was no previous use site error for this symbol, check the constant value
                If fieldUseSiteInfo.DiagnosticInfo Is Nothing Then

                    ' report use site errors for invalid constant values 
                    Dim constantValue = GetConstantValue(ConstantFieldsInProgress.Empty)
                    If constantValue IsNot Nothing AndAlso
                        constantValue.IsBad Then
                        fieldUseSiteInfo = New UseSiteInfo(Of AssemblySymbol)(New DiagnosticInfo(MessageProvider.Instance,
                                                                                                 ERRID.ERR_UnsupportedConstant2,
                                                                                                 Me.ContainingType,
                                                                                                 Me.Name))
                    End If
                End If

                _lazyCachedUseSiteInfo.Initialize(primaryDependency, fieldUseSiteInfo)
            End If

            Return _lazyCachedUseSiteInfo.ToUseSiteInfo(primaryDependency)
        End Function

        Private Function DeriveCompilerFeatureRequiredDiagnostic(ByRef result As UseSiteInfo(Of AssemblySymbol)) As DiagnosticInfo
            Dim containingModule = _containingType.ContainingPEModule
            Return If(DeriveCompilerFeatureRequiredAttributeDiagnostic(Me, containingModule, Handle, CompilerFeatureRequiredFeatures.None, New MetadataDecoder(containingModule, _containingType)),
                      _containingType.GetCompilerFeatureRequiredDiagnostic())
        End Function

        Friend ReadOnly Property Handle As FieldDefinitionHandle
            Get
                Return _handle
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
