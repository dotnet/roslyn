' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.Threading
Imports System.Reflection
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.Cci

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' The class to represent all methods imported from a PE/module.
    ''' </summary>
    Friend NotInheritable Class PEMethodSymbol
        Inherits MethodSymbol

        Private ReadOnly _handle As MethodDefinitionHandle
        Private ReadOnly _name As String
        Private ReadOnly _implFlags As UShort
        Private ReadOnly _flags As UShort
        Private ReadOnly _containingType As PENamedTypeSymbol

        Private _associatedPropertyOrEventOpt As Symbol

        Private _packedFlags As PackedFlags

        Private _lazyTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Private _lazyExplicitMethodImplementations As ImmutableArray(Of MethodSymbol)

        ''' <summary>
        ''' A single field to hold optional auxiliary data.
        ''' In many scenarios it is possible to avoid allocating this, thus saving total space in <see cref="PEModuleSymbol"/>.
        ''' Even for lazily-computed values, it may be possible to avoid allocating <see cref="_uncommonFields"/> if
        ''' the computed value is a well-known "empty" value. In this case, bits in <see cref="_packedFlags"/> are used
        ''' to indicate that the lazy values have been computed and, if <see cref="_uncommonFields"/> is null, then
        ''' the "empty" value should be inferred.
        ''' </summary>
        Private _uncommonFields As UncommonFields

        Private Structure PackedFlags
            ' Flags are packed into a 32-bit int with the following layout:
            ' |           m|l|k|j|i|h|g|f|e|d|c|b|aaaaa|
            '
            ' a = method kind. 5 bits
            ' b = method kind populated. 1 bit
            ' c = isExtensionMethod. 1 bit.
            ' d = isExtensionMethod populated. 1 bit.
            ' e = obsolete attribute populated. 1 bit
            ' f = custom attributes populated. 1 bit
            ' g = use site diagnostic populated. 1 bit
            ' h = conditional attributes populated. 1 bit
            ' i = is init-only. 1 bit.
            ' j = is init-only populated. 1 bit.
            ' k = has SetsRequiredMembers. 1 bit.
            ' l = has SetsRequiredMembers populated. 1 bit.
            ' m = OverloadResolutionPriority populated. 1 bit.

            Private _bits As Integer

            Private Const s_methodKindOffset As Integer = 0
            Private Const s_methodKindMask As Integer = &H1F
            Private Const s_methodKindIsPopulatedBit As Integer = 1 << 5
            Private Const s_isExtensionMethodBit As Integer = 1 << 6
            Private Const s_isExtensionMethodIsPopulatedBit As Integer = 1 << 7
            Private Const s_isObsoleteAttributePopulatedBit As Integer = 1 << 8
            Private Const s_isCustomAttributesPopulatedBit As Integer = 1 << 9
            Private Const s_isUseSiteDiagnosticPopulatedBit As Integer = 1 << 10
            Private Const s_isConditionalAttributePopulatedBit As Integer = 1 << 11
            Private Const s_isInitOnlyBit = 1 << 12
            Private Const s_isInitOnlyPopulatedBit = 1 << 13
            Private Const s_hasSetsRequiredMembersBit = 1 << 14
            Private Const s_hasSetsRequiredMembersPopulatedBit = 1 << 15
            Private Const s_OverloadResolutionPriorityIsPopulatedBit As Integer = 1 << 16

            Public Property MethodKind As MethodKind
                Get
                    Return CType((_bits >> s_methodKindOffset) And s_methodKindMask, MethodKind)
                End Get
                Set(value As MethodKind)
                    Debug.Assert(value = (value And s_methodKindMask))
                    _bits = (_bits And Not (s_methodKindMask << s_methodKindOffset)) Or (value << s_methodKindOffset) Or s_methodKindIsPopulatedBit
                End Set
            End Property

            Public ReadOnly Property MethodKindIsPopulated As Boolean
                Get
                    Return (_bits And s_methodKindIsPopulatedBit) <> 0
                End Get
            End Property

            Public ReadOnly Property IsExtensionMethod As Boolean
                Get
                    Return (_bits And s_isExtensionMethodBit) <> 0
                End Get
            End Property

            Public ReadOnly Property IsExtensionMethodPopulated As Boolean
                Get
                    Return (_bits And s_isExtensionMethodIsPopulatedBit) <> 0
                End Get
            End Property

            Public ReadOnly Property IsObsoleteAttributePopulated As Boolean
                Get
                    Return (Volatile.Read(_bits) And s_isObsoleteAttributePopulatedBit) <> 0
                End Get
            End Property

            Public ReadOnly Property IsCustomAttributesPopulated As Boolean
                Get
                    Return (Volatile.Read(_bits) And s_isCustomAttributesPopulatedBit) <> 0
                End Get
            End Property

            Public ReadOnly Property IsUseSiteDiagnosticPopulated As Boolean
                Get
                    Return (Volatile.Read(_bits) And s_isUseSiteDiagnosticPopulatedBit) <> 0
                End Get
            End Property

            Public ReadOnly Property IsConditionalPopulated As Boolean
                Get
                    Return (Volatile.Read(_bits) And s_isConditionalAttributePopulatedBit) <> 0
                End Get
            End Property

            Public ReadOnly Property IsInitOnly As Boolean
                Get
                    Return (_bits And s_isInitOnlyBit) <> 0
                End Get
            End Property

            Public ReadOnly Property IsInitOnlyPopulated As Boolean
                Get
                    Return (_bits And s_isInitOnlyPopulatedBit) <> 0
                End Get
            End Property

            Public Function TryGetHasSetsRequiredMembers(ByRef hasSetsRequiredMembers As Boolean) As Boolean
                Dim bits = _bits
                hasSetsRequiredMembers = (bits And s_hasSetsRequiredMembersBit) <> 0
                Return (bits And s_hasSetsRequiredMembersPopulatedBit) <> 0
            End Function

            Private Shared Function BitsAreUnsetOrSame(bits As Integer, mask As Integer) As Boolean
                Return (bits And mask) = 0 OrElse (bits And mask) = mask
            End Function

            Public Sub InitializeMethodKind(methodKind As MethodKind)
                Debug.Assert(methodKind = (methodKind And s_methodKindMask))
                Dim bitsToSet = ((methodKind And s_methodKindMask) << s_methodKindOffset) Or s_methodKindIsPopulatedBit
                Debug.Assert(BitsAreUnsetOrSame(_bits, bitsToSet))
                ThreadSafeFlagOperations.Set(_bits, bitsToSet)
            End Sub

            Public Sub InitializeIsExtensionMethod(isExtensionMethod As Boolean)
                Dim bitsToSet = If(isExtensionMethod, s_isExtensionMethodBit, 0) Or s_isExtensionMethodIsPopulatedBit
                Debug.Assert(BitsAreUnsetOrSame(_bits, bitsToSet))
                ThreadSafeFlagOperations.Set(_bits, bitsToSet)
            End Sub

            Public Sub SetIsObsoleteAttributePopulated()
                ThreadSafeFlagOperations.Set(_bits, s_isObsoleteAttributePopulatedBit)
            End Sub

            Public Sub SetIsCustomAttributesPopulated()
                ThreadSafeFlagOperations.Set(_bits, s_isCustomAttributesPopulatedBit)
            End Sub

            Public Sub SetIsUseSiteDiagnosticPopulated()
                ThreadSafeFlagOperations.Set(_bits, s_isUseSiteDiagnosticPopulatedBit)
            End Sub

            Public Sub SetIsConditionalAttributePopulated()
                ThreadSafeFlagOperations.Set(_bits, s_isConditionalAttributePopulatedBit)
            End Sub

            Public Sub InitializeIsInitOnly(isInitOnly As Boolean)
                Dim bitsToSet = If(isInitOnly, s_isInitOnlyBit, 0) Or s_isInitOnlyPopulatedBit
                Debug.Assert(BitsAreUnsetOrSame(_bits, bitsToSet))
                ThreadSafeFlagOperations.Set(_bits, bitsToSet)
            End Sub

            Public Sub InitializeSetsRequiredMembers(hasSetsRequiredMembers As Boolean)
                Dim bitsToSet = If(hasSetsRequiredMembers, s_hasSetsRequiredMembersBit, 0) Or s_hasSetsRequiredMembersPopulatedBit
                Debug.Assert(BitsAreUnsetOrSame(_bits, bitsToSet))
                ThreadSafeFlagOperations.Set(_bits, bitsToSet)
            End Sub

            Public ReadOnly Property OverloadResolutionPriorityPopulated As Boolean
                Get
                    Return (Volatile.Read(_bits) And s_OverloadResolutionPriorityIsPopulatedBit) <> 0
                End Get
            End Property

            Public Sub SetOverloadResolutionPriorityPopulated()
                ThreadSafeFlagOperations.Set(_bits, s_OverloadResolutionPriorityIsPopulatedBit)
            End Sub
        End Structure

        ''' <summary>
        ''' Holds infrequently accessed fields. See <seealso cref="_uncommonFields"/> for an explanation.
        ''' </summary>
        Private NotInheritable Class UncommonFields
            Public _lazyMeParameter As ParameterSymbol
            Public _lazyDocComment As Tuple(Of CultureInfo, String)
            Public _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)
            Public _lazyConditionalAttributeSymbols As ImmutableArray(Of String)
            Public _lazyObsoleteAttributeData As ObsoleteAttributeData
            Public _lazyCachedUseSiteInfo As CachedUseSiteInfo(Of AssemblySymbol)
            Public _lazyOverloadResolutionPriority As Integer
        End Class

        Private Function CreateUncommonFields() As UncommonFields
            Dim retVal = New UncommonFields

            If Not _packedFlags.IsObsoleteAttributePopulated Then
                retVal._lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized
            End If

            '
            ' Do not set _lazyCachedUseSiteInfo !!!!
            '
            ' "null" Indicates "no errors" or "unknown state",
            ' and we know which one of the states we have from IsUseSiteDiagnosticPopulated
            '
            ' Setting _lazyCachedUseSiteInfo to a sentinel value here would introduce
            ' a number of extra states for various permutations of IsUseSiteDiagnosticPopulated, UncommonFields and _lazyUseSiteDiagnostic
            ' Some of them, in tight races, may lead to returning the sentinel as the diagnostics.
            '
            If _packedFlags.IsCustomAttributesPopulated Then
                retVal._lazyCustomAttributes = ImmutableArray(Of VisualBasicAttributeData).Empty
            End If

            If _packedFlags.IsConditionalPopulated Then
                retVal._lazyConditionalAttributeSymbols = ImmutableArray(Of String).Empty
            End If
            Return retVal
        End Function

        Private Function AccessUncommonFields() As UncommonFields
            Dim retVal = _uncommonFields
            Return If(retVal, InterlockedOperations.Initialize(_uncommonFields, CreateUncommonFields()))
        End Function

#Region "Signature data"
        Private _lazySignature As SignatureData

        Private Class SignatureData
            Public ReadOnly Header As SignatureHeader
            Public ReadOnly Parameters As ImmutableArray(Of ParameterSymbol)
            Public ReadOnly ReturnParam As PEParameterSymbol

            Public Sub New(signatureHeader As SignatureHeader, parameters As ImmutableArray(Of ParameterSymbol), returnParam As PEParameterSymbol)
                Me.Header = signatureHeader
                Me.Parameters = parameters
                Me.ReturnParam = returnParam
            End Sub
        End Class
#End Region

        Friend Sub New(
            moduleSymbol As PEModuleSymbol,
            containingType As PENamedTypeSymbol,
            handle As MethodDefinitionHandle
        )
            Debug.Assert(moduleSymbol IsNot Nothing)
            Debug.Assert(containingType IsNot Nothing)
            Debug.Assert(Not handle.IsNil)

            _handle = handle
            _containingType = containingType

            Try
                Dim implFlags As MethodImplAttributes
                Dim flags As MethodAttributes
                Dim rva As Integer
                moduleSymbol.Module.GetMethodDefPropsOrThrow(handle, _name, implFlags, flags, rva)
                _implFlags = CType(implFlags, UShort)
                _flags = CType(flags, UShort)
            Catch mrEx As BadImageFormatException
                If _name Is Nothing Then
                    _name = String.Empty
                End If

                InitializeUseSiteInfo(New UseSiteInfo(Of AssemblySymbol)(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedMethod1, CustomSymbolDisplayFormatter.ShortErrorName(Me))))
            End Try
        End Sub

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

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return (_flags And MethodAttributes.SpecialName) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return (_flags And MethodAttributes.RTSpecialName) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataFinal As Boolean
            Get
                Return (_flags And MethodAttributes.Final) <> 0
            End Get
        End Property

        Friend ReadOnly Property MethodImplFlags As MethodImplAttributes
            Get
                Return CType(_implFlags, MethodImplAttributes)
            End Get
        End Property

        Friend ReadOnly Property MethodFlags As MethodAttributes
            Get
                Return CType(_flags, MethodAttributes)
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                If Not _packedFlags.MethodKindIsPopulated Then
                    _packedFlags.InitializeMethodKind(ComputeMethodKind())
                End If

                Return _packedFlags.MethodKind
            End Get

        End Property

        Friend Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
            Get
                Return False
            End Get
        End Property

        Private Function ComputeMethodKind() As MethodKind
            Dim name As String = Me.Name

            If HasSpecialName Then
                If name.StartsWith("."c, StringComparison.Ordinal) Then

                    ' 10.5.1 Instance constructor
                    ' An instance constructor shall be an instance (not static or virtual) method,
                    ' it shall be named .ctor, and marked instance, rtspecialname, and specialname (§15.4.2.6).
                    ' An instance constructor can have parameters, but shall not return a value.
                    ' An instance constructor cannot take generic type parameters.

                    ' 10.5.3 Type initializer
                    ' This method shall be static, take no parameters, return no value,
                    ' be marked with rtspecialname and specialname (§15.4.2.6), and be named .cctor.

                    If (_flags And (MethodAttributes.RTSpecialName Or MethodAttributes.Virtual)) = MethodAttributes.RTSpecialName AndAlso
                       String.Equals(name, If(IsShared, WellKnownMemberNames.StaticConstructorName, WellKnownMemberNames.InstanceConstructorName), StringComparison.Ordinal) AndAlso
                       IsSub AndAlso Arity = 0 Then

                        If IsShared Then
                            If Parameters.Length = 0 Then
                                Return MethodKind.SharedConstructor
                            End If
                        Else
                            Return MethodKind.Constructor
                        End If
                    End If

                    Return MethodKind.Ordinary

                ElseIf IsShared AndAlso DeclaredAccessibility = Accessibility.Public AndAlso Not IsSub AndAlso Arity = 0 Then
                    Dim opInfo As OverloadResolution.OperatorInfo = OverloadResolution.GetOperatorInfo(name)

                    If opInfo.ParamCount <> 0 Then
                        ' Combination of all conditions that should be met to get here must match implementation of 
                        ' IsPotentialOperatorOrConversion (with exception of ParameterCount matching).

                        If OverloadResolution.ValidateOverloadedOperator(Me, opInfo) Then
                            Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo)
                        End If
                    End If

                    Return MethodKind.Ordinary
                End If
            End If

            If Not IsShared AndAlso String.Equals(name, WellKnownMemberNames.DelegateInvokeName, StringComparison.Ordinal) AndAlso _containingType.TypeKind = TypeKind.Delegate Then
                Return MethodKind.DelegateInvoke
            End If

            Return MethodKind.Ordinary
        End Function

        Friend Overrides Function IsParameterlessConstructor() As Boolean
            If _packedFlags.MethodKindIsPopulated Then
                Return _packedFlags.MethodKind = MethodKind.Constructor AndAlso ParameterCount = 0
            End If

            ' 10.5.1 Instance constructor
            ' An instance constructor shall be an instance (not static or virtual) method,
            ' it shall be named .ctor, and marked instance, rtspecialname, and specialname (§15.4.2.6).
            ' An instance constructor can have parameters, but shall not return a value.
            ' An instance constructor cannot take generic type parameters.

            If (_flags And (MethodAttributes.SpecialName Or MethodAttributes.RTSpecialName Or MethodAttributes.Static Or MethodAttributes.Virtual)) =
                    (MethodAttributes.SpecialName Or MethodAttributes.RTSpecialName) AndAlso
               String.Equals(Me.Name, WellKnownMemberNames.InstanceConstructorName, StringComparison.Ordinal) AndAlso
               ParameterCount = 0 AndAlso
               IsSub AndAlso Arity = 0 Then

                _packedFlags.MethodKind = MethodKind.Constructor
                Return True
            End If

            Return False
        End Function

        Private Function ComputeMethodKindForPotentialOperatorOrConversion(opInfo As OverloadResolution.OperatorInfo) As MethodKind
            ' Don't mark methods involved in unsupported overloading as operators.

            If opInfo.IsUnary Then
                Select Case opInfo.UnaryOperatorKind
                    Case UnaryOperatorKind.Implicit
                        Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.Conversion, WellKnownMemberNames.ExplicitConversionName, True)
                    Case UnaryOperatorKind.Explicit
                        Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.Conversion, WellKnownMemberNames.ImplicitConversionName, True)
                    Case UnaryOperatorKind.IsFalse, UnaryOperatorKind.IsTrue, UnaryOperatorKind.Minus, UnaryOperatorKind.Plus
                        Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.UserDefinedOperator, Nothing, False)
                    Case UnaryOperatorKind.Not
                        If IdentifierComparison.Equals(Me.Name, WellKnownMemberNames.OnesComplementOperatorName) Then
                            Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.UserDefinedOperator, Nothing, False)
                        Else
                            Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.UserDefinedOperator, WellKnownMemberNames.OnesComplementOperatorName, False)
                        End If
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(opInfo.UnaryOperatorKind)
                End Select
            Else
                Debug.Assert(opInfo.IsBinary)
                Select Case opInfo.BinaryOperatorKind
                    Case BinaryOperatorKind.Add,
                         BinaryOperatorKind.Subtract,
                         BinaryOperatorKind.Multiply,
                         BinaryOperatorKind.Divide,
                         BinaryOperatorKind.IntegerDivide,
                         BinaryOperatorKind.Modulo,
                         BinaryOperatorKind.Power,
                         BinaryOperatorKind.Equals,
                         BinaryOperatorKind.NotEquals,
                         BinaryOperatorKind.LessThan,
                         BinaryOperatorKind.GreaterThan,
                         BinaryOperatorKind.LessThanOrEqual,
                         BinaryOperatorKind.GreaterThanOrEqual,
                         BinaryOperatorKind.Like,
                         BinaryOperatorKind.Concatenate,
                         BinaryOperatorKind.Xor
                        Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.UserDefinedOperator, Nothing, False)

                    Case BinaryOperatorKind.And
                        If IdentifierComparison.Equals(Me.Name, WellKnownMemberNames.BitwiseAndOperatorName) Then
                            Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.UserDefinedOperator, Nothing, False)
                        Else
                            Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.UserDefinedOperator, WellKnownMemberNames.BitwiseAndOperatorName, False)
                        End If
                    Case BinaryOperatorKind.Or
                        If IdentifierComparison.Equals(Me.Name, WellKnownMemberNames.BitwiseOrOperatorName) Then
                            Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.UserDefinedOperator, Nothing, False)
                        Else
                            Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.UserDefinedOperator, WellKnownMemberNames.BitwiseOrOperatorName, False)
                        End If
                    Case BinaryOperatorKind.LeftShift
                        If IdentifierComparison.Equals(Me.Name, WellKnownMemberNames.LeftShiftOperatorName) Then
                            Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.UserDefinedOperator, Nothing, False)
                        Else
                            Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.UserDefinedOperator, WellKnownMemberNames.LeftShiftOperatorName, False)
                        End If
                    Case BinaryOperatorKind.RightShift
                        If IdentifierComparison.Equals(Me.Name, WellKnownMemberNames.RightShiftOperatorName) Then
                            Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.UserDefinedOperator, Nothing, False)
                        Else
                            Return ComputeMethodKindForPotentialOperatorOrConversion(opInfo, MethodKind.UserDefinedOperator, WellKnownMemberNames.RightShiftOperatorName, False)
                        End If
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(opInfo.BinaryOperatorKind)
                End Select
            End If
        End Function

        Private Function IsPotentialOperatorOrConversion(opInfo As OverloadResolution.OperatorInfo) As Boolean
            Return HasSpecialName AndAlso
                   IsShared AndAlso DeclaredAccessibility = Accessibility.Public AndAlso
                   Not IsSub AndAlso Arity = 0 AndAlso
                   ParameterCount = opInfo.ParamCount
        End Function

        Private Function ComputeMethodKindForPotentialOperatorOrConversion(
            opInfo As OverloadResolution.OperatorInfo,
            potentialMethodKind As MethodKind,
            additionalNameOpt As String,
            adjustContendersOfAdditionalName As Boolean
        ) As MethodKind
            Debug.Assert(potentialMethodKind = MethodKind.Conversion OrElse potentialMethodKind = MethodKind.UserDefinedOperator)

            Dim result As MethodKind = potentialMethodKind
            Dim inputParams As ImmutableArray(Of ParameterSymbol) = Parameters
            Dim outputType As TypeSymbol = ReturnType

            For i As Integer = 0 To If(additionalNameOpt Is Nothing, 0, 1)
                For Each m In _containingType.GetMembers(If(i = 0, Me.Name, additionalNameOpt))
                    If m Is Me Then
                        Continue For
                    End If

                    If m.Kind <> SymbolKind.Method Then
                        Continue For
                    End If

                    Dim contender = TryCast(m, PEMethodSymbol)

                    If contender Is Nothing OrElse Not contender.IsPotentialOperatorOrConversion(opInfo) Then
                        Continue For
                    End If

                    If contender._packedFlags.MethodKindIsPopulated Then
                        Select Case contender._packedFlags.MethodKind
                            Case MethodKind.Ordinary
                                ' Need to check against our method
                            Case potentialMethodKind
                                If i = 0 OrElse adjustContendersOfAdditionalName Then
                                    ' Contender was already cleared, so it cannot conflict with this operator.
                                    Continue For
                                End If
                            Case Else
                                ' Cannot be an operator of the target kind.
                                Continue For
                        End Select
                    End If

                    If potentialMethodKind = MethodKind.Conversion AndAlso Not outputType.IsSameTypeIgnoringAll(contender.ReturnType) Then
                        Continue For
                    End If

                    Dim j As Integer
                    For j = 0 To inputParams.Length - 1
                        If Not inputParams(j).Type.IsSameTypeIgnoringAll(contender.Parameters(j).Type) Then
                            Exit For
                        End If
                    Next

                    If j < inputParams.Length Then
                        Continue For
                    End If

                    ' Unsupported overloading
                    result = MethodKind.Ordinary

                    ' Mark the contender too.
                    If i = 0 OrElse adjustContendersOfAdditionalName Then
                        contender._packedFlags.InitializeMethodKind(MethodKind.Ordinary)
                    End If
                Next
            Next

            Return result
        End Function

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return _associatedPropertyOrEventOpt
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Dim access As Accessibility = Accessibility.Private

                Select Case _flags And MethodAttributes.MemberAccessMask
                    Case MethodAttributes.Assembly
                        access = Accessibility.Friend

                    Case MethodAttributes.FamORAssem
                        access = Accessibility.ProtectedOrFriend

                    Case MethodAttributes.FamANDAssem
                        access = Accessibility.ProtectedAndFriend

                    Case MethodAttributes.Private,
                         MethodAttributes.PrivateScope
                        access = Accessibility.Private

                    Case MethodAttributes.Public
                        access = Accessibility.Public

                    Case MethodAttributes.Family
                        access = Accessibility.Protected

                    Case Else
                        access = Accessibility.Private
                End Select

                Return access

            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If Not _packedFlags.IsCustomAttributesPopulated Then
                Dim attributeData As ImmutableArray(Of VisualBasicAttributeData) = Nothing
                Dim containingPEModuleSymbol = DirectCast(ContainingModule(), PEModuleSymbol)
                Dim checkForRequiredMembers = MethodKind = MethodKind.Constructor AndAlso
                                              HasSetsRequiredMembers = False AndAlso
                                              (Me.ContainingType.HasAnyDeclaredRequiredMembers OrElse Not Me.ContainingType.AllRequiredMembers.IsEmpty)

                If checkForRequiredMembers Then
                    Dim compilerFeatureRequiredDiagnostic As DiagnosticInfo = Nothing
                    DeriveCompilerFeatureRequiredUseSiteInfo(compilerFeatureRequiredDiagnostic)

                    Dim discard1 As CustomAttributeHandle = Nothing
                    Dim discard2 As CustomAttributeHandle = Nothing
                    attributeData = containingPEModuleSymbol.GetCustomAttributesForToken(
                        Me.Handle,
                        filteredOutAttribute1:=discard1,
                        filterOut1:=If(compilerFeatureRequiredDiagnostic Is Nothing, AttributeDescription.CompilerFeatureRequiredAttribute, Nothing),
                        filteredOutAttribute2:=discard2,
                        filterOut2:=If(ObsoleteAttributeData Is Nothing, AttributeDescription.ObsoleteAttribute, Nothing))

                Else
                    containingPEModuleSymbol.LoadCustomAttributes(Me.Handle, attributeData)
                End If
                Debug.Assert(Not attributeData.IsDefault)
                If Not attributeData.IsEmpty Then
                    attributeData = InterlockedOperations.Initialize(AccessUncommonFields()._lazyCustomAttributes, attributeData)
                End If

                _packedFlags.SetIsCustomAttributesPopulated()
                Return attributeData
            End If

            Dim uncommonFields = _uncommonFields
            If uncommonFields Is Nothing Then
                Return ImmutableArray(Of VisualBasicAttributeData).Empty
            Else
                Dim attributeData = uncommonFields._lazyCustomAttributes
                Return If(attributeData.IsDefault,
                    InterlockedOperations.Initialize(uncommonFields._lazyCustomAttributes, ImmutableArray(Of VisualBasicAttributeData).Empty),
                    attributeData)
            End If
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return GetAttributes()
        End Function

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                If Not _packedFlags.IsExtensionMethodPopulated Then

                    Dim result As Boolean = False

                    If Me.IsShared AndAlso
                       Me.ParameterCount > 0 AndAlso
                       Me.MethodKind = MethodKind.Ordinary AndAlso
                       _containingType.MightContainExtensionMethods AndAlso
                       _containingType.ContainingPEModule.Module.HasExtensionAttribute(Me.Handle, ignoreCase:=True) AndAlso
                       ValidateGenericConstraintsOnExtensionMethodDefinition() Then

                        Dim firstParam As ParameterSymbol = Me.Parameters(0)

                        result = Not (firstParam.IsOptional OrElse firstParam.IsParamArray)
                    End If

                    _packedFlags.InitializeIsExtensionMethod(result)
                End If

                Return _packedFlags.IsExtensionMethod
            End Get
        End Property

        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Return (_flags And MethodAttributes.PinvokeImpl) <> 0 OrElse
                       (_implFlags And (MethodImplAttributes.InternalCall Or MethodImplAttributes.Runtime)) <> 0
            End Get
        End Property

        Public Overrides Function GetDllImportData() As DllImportData
            If (_flags And MethodAttributes.PinvokeImpl) = 0 Then
                Return Nothing
            End If

            ' do not cache the result, the compiler doesn't use this (it's only exposed through public API):
            Return _containingType.ContainingPEModule.Module.GetDllImportData(Me._handle)
        End Function

        Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Return (_flags And MethodAttributes.NewSlot) <> 0
        End Function

        Friend Overrides ReadOnly Property IsExternal As Boolean
            Get
                Return IsExternalMethod OrElse
                    (_implFlags And MethodImplAttributes.Runtime) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property IsAccessCheckedOnOverride As Boolean
            Get
                Return (_flags And MethodAttributes.CheckAccessOnOverride) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property ReturnValueIsMarshalledExplicitly As Boolean
            Get
                Return _lazySignature.ReturnParam.IsMarshalledExplicitly
            End Get
        End Property

        Friend Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return _lazySignature.ReturnParam.MarshallingInformation
            End Get
        End Property

        Friend Overrides ReadOnly Property ReturnValueMarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return _lazySignature.ReturnParam.MarshallingDescriptor
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                Return CType(_implFlags, Reflection.MethodImplAttributes)
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return (_flags And MethodAttributes.HasSecurity) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property RequiresSecurityObject As Boolean
            Get
                Return (_flags And MethodAttributes.RequireSecObject) <> 0
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Throw ExceptionUtilities.Unreachable
        End Function

        Private ReadOnly Property Signature As SignatureData
            Get
                Return If(_lazySignature, LoadSignature())
            End Get
        End Property

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return Signature.Header.CallingConvention = SignatureCallingConvention.VarArgs
            End Get
        End Property

        Public Overrides ReadOnly Property IsGenericMethod As Boolean
            Get
                Return Me.Arity > 0
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                If Me._lazyTypeParameters.IsDefault Then
                    Try
                        Dim paramCount As Integer = 0
                        Dim typeParamCount As Integer = 0
                        MetadataDecoder.GetSignatureCountsOrThrow(Me._containingType.ContainingPEModule.Module, Me._handle, paramCount, typeParamCount)
                        Return typeParamCount
                    Catch mrEx As BadImageFormatException
                        Return TypeParameters.Length
                    End Try
                Else
                    Return Me._lazyTypeParameters.Length
                End If
            End Get
        End Property

        Friend ReadOnly Property Handle As MethodDefinitionHandle
            Get
                Return _handle
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return (_flags And MethodAttributes.Virtual) <> 0 AndAlso
                    (_flags And MethodAttributes.Abstract) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return (_flags And
                            (MethodAttributes.Virtual Or
                             MethodAttributes.Final Or
                             MethodAttributes.Abstract Or
                             MethodAttributes.NewSlot)) =
                        If(_containingType.IsInterface,
                           MethodAttributes.Virtual Or MethodAttributes.Final Or MethodAttributes.Abstract,
                           MethodAttributes.Virtual Or MethodAttributes.Final)
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return (_flags And MethodAttributes.HideBySig) <> 0 OrElse
                    IsOverrides ' If overrides is present, then Overloads is implicit

                ' The check for IsOverrides is needed because of bug Dev10 #850631,
                ' VB compiler doesn't emit HideBySig flag for overriding methods that 
                ' aren't marked explicitly with Overrides modifier.
            End Get
        End Property

        Public Overrides Function GetOverloadResolutionPriority() As Integer
            If Not _packedFlags.OverloadResolutionPriorityPopulated Then

                Dim priority As Integer
                If _containingType.ContainingPEModule.Module.TryGetOverloadResolutionPriorityValue(_handle, priority) AndAlso
                   priority <> 0 Then
                    Interlocked.CompareExchange(AccessUncommonFields()._lazyOverloadResolutionPriority, priority, 0)
#If DEBUG Then
                Else
                    ' 0 is the default if nothing is present in metadata, and we don't care about preserving the difference between "not present" and "set to the default value".
                    Debug.Assert(_uncommonFields Is Nothing OrElse _uncommonFields._lazyOverloadResolutionPriority = 0)
#End If
                End If

                _packedFlags.SetOverloadResolutionPriorityPopulated()
            End If

            Return If(_uncommonFields?._lazyOverloadResolutionPriority, 0)
        End Function

        Friend Overrides ReadOnly Property IsHiddenBySignature As Boolean
            Get
                Return (_flags And MethodAttributes.HideBySig) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Dim flagsToCheck = (_flags And
                                    (MethodAttributes.Virtual Or
                                     MethodAttributes.Final Or
                                     MethodAttributes.Abstract Or
                                     MethodAttributes.NewSlot))

                Return flagsToCheck = (MethodAttributes.Virtual Or If(IsShared, 0, MethodAttributes.NewSlot)) OrElse
                       (Not _containingType.IsInterface AndAlso
                        flagsToCheck = MethodAttributes.Virtual AndAlso _containingType.BaseTypeNoUseSiteDiagnostics Is Nothing)
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                ' ECMA-335 
                ' 10.3.1 Introducing a virtual method
                ' If the definition is not marked newslot, the definition creates a new virtual method only 
                ' if there is not virtual method of the same name and signature inherited from a base class.
                '
                ' This means that a virtual method without NewSlot flag in a type that doesn't have a base
                ' is a new virtual method and doesn't override anything.
                Return Not _containingType.IsInterface AndAlso
                       (_flags And MethodAttributes.Virtual) <> 0 AndAlso
                       (_flags And MethodAttributes.NewSlot) = 0 AndAlso
                       _containingType.BaseTypeNoUseSiteDiagnostics IsNot Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return (_flags And MethodAttributes.Static) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return Me.ReturnType.SpecialType = SpecialType.System_Void
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsInitOnly As Boolean
            Get
                If Not _packedFlags.IsInitOnlyPopulated Then

                    Dim result As Boolean = Not Me.IsShared AndAlso
                                            Me.MethodKind = MethodKind.PropertySet AndAlso
                                            CustomModifierUtils.HasIsExternalInitModifier(ReturnTypeCustomModifiers)

                    _packedFlags.InitializeIsInitOnly(result)
                End If

                Return _packedFlags.IsInitOnly
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

        Friend Overrides ReadOnly Property ParameterCount As Integer
            Get
                If Me._lazySignature Is Nothing Then
                    Try
                        Dim paramCount As Integer = 0
                        Dim typeParamCount As Integer = 0
                        MetadataDecoder.GetSignatureCountsOrThrow(Me._containingType.ContainingPEModule.Module, Me._handle, paramCount, typeParamCount)
                        Return paramCount
                    Catch mrEx As BadImageFormatException
                        Return Parameters.Length
                    End Try
                Else
                    Return Me._lazySignature.Parameters.Length
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return Signature.Parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return Signature.ReturnParam.IsByRef
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return Signature.ReturnParam.Type
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Signature.ReturnParam.CustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Signature.ReturnParam.RefCustomModifiers
            End Get
        End Property

        Public Overrides Function GetReturnTypeAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Signature.ReturnParam.GetAttributes()
        End Function

        Friend ReadOnly Property ReturnParam As PEParameterSymbol
            Get
                Return Signature.ReturnParam
            End Get
        End Property

        ''' <summary>
        ''' Associate the method with a particular property. Returns
        ''' false if the method is already associated with a property or event.
        ''' </summary>
        Friend Function SetAssociatedProperty(propertySymbol As PEPropertySymbol, methodKind As MethodKind) As Boolean
            Debug.Assert((methodKind = MethodKind.PropertyGet) OrElse (methodKind = MethodKind.PropertySet))
            Return Me.SetAssociatedPropertyOrEvent(propertySymbol, methodKind)
        End Function

        ''' <summary>
        ''' Associate the method with a particular event. Returns
        ''' false if the method is already associated with a property or event.
        ''' </summary>
        Friend Function SetAssociatedEvent(eventSymbol As PEEventSymbol, methodKind As MethodKind) As Boolean
            Debug.Assert((methodKind = MethodKind.EventAdd) OrElse (methodKind = MethodKind.EventRemove) OrElse (methodKind = MethodKind.EventRaise))
            Return Me.SetAssociatedPropertyOrEvent(eventSymbol, methodKind)
        End Function

        Private Function SetAssociatedPropertyOrEvent(propertyOrEventSymbol As Symbol, methodKind As MethodKind) As Boolean
            If Me._associatedPropertyOrEventOpt Is Nothing Then
                Debug.Assert(TypeSymbol.Equals(propertyOrEventSymbol.ContainingType, Me.ContainingType, TypeCompareKind.ConsiderEverything))
                Me._associatedPropertyOrEventOpt = propertyOrEventSymbol
                _packedFlags.MethodKind = methodKind
                Return True
            End If

            Return False
        End Function

        Private Function LoadSignature() As SignatureData

            Dim moduleSymbol = _containingType.ContainingPEModule

            Dim signatureHeader As SignatureHeader
            Dim mrEx As BadImageFormatException = Nothing
            Dim paramInfo() As ParamInfo(Of TypeSymbol) =
                    (New MetadataDecoder(moduleSymbol, Me)).GetSignatureForMethod(_handle, signatureHeader, mrEx)

            ' If method is not generic, let's assign empty list for type parameters
            If Not signatureHeader.IsGeneric() AndAlso
                    _lazyTypeParameters.IsDefault Then
                ImmutableInterlocked.InterlockedInitialize(_lazyTypeParameters,
                                                           ImmutableArray(Of TypeParameterSymbol).Empty)
            End If

            Dim count As Integer = paramInfo.Length - 1
            Dim params As ImmutableArray(Of ParameterSymbol)
            Dim isBad As Boolean
            Dim hasBadParameter As Boolean = False

            If count > 0 Then
                Dim builder = ImmutableArray.CreateBuilder(Of ParameterSymbol)(count)
                For i As Integer = 0 To count - 1 Step 1
                    builder.Add(PEParameterSymbol.Create(moduleSymbol, Me, i, paramInfo(i + 1), isBad))

                    If isBad Then
                        hasBadParameter = True
                    End If
                Next

                params = builder.ToImmutable()
            Else
                params = ImmutableArray(Of ParameterSymbol).Empty
            End If

            ' paramInfo(0) contains information about return "parameter"
            Dim returnParam = PEParameterSymbol.Create(moduleSymbol, Me, 0, paramInfo(0), isBad)

            If mrEx IsNot Nothing OrElse hasBadParameter OrElse isBad Then
                InitializeUseSiteInfo(New UseSiteInfo(Of AssemblySymbol)(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedMethod1, CustomSymbolDisplayFormatter.ShortErrorName(Me))))
            End If

            Dim signature As New SignatureData(signatureHeader, params, returnParam)

            Return InterlockedOperations.Initialize(_lazySignature, signature)
        End Function

        Friend Overrides ReadOnly Property MetadataSignatureHandle As BlobHandle
            Get
                Try
                    Return _containingType.ContainingPEModule.Module.GetMethodSignatureOrThrow(_handle)
                Catch ex As BadImageFormatException
                    Return Nothing
                End Try
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Dim errorInfo As DiagnosticInfo = Nothing
                EnsureTypeParametersAreLoaded(errorInfo)
                Dim typeParams = EnsureTypeParametersAreLoaded(errorInfo)
                If errorInfo IsNot Nothing Then
                    InitializeUseSiteInfo(New UseSiteInfo(Of AssemblySymbol)(errorInfo))
                End If
                Return typeParams
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                If IsGenericMethod Then
                    Return StaticCast(Of TypeSymbol).From(Me.TypeParameters)
                Else
                    Return ImmutableArray(Of TypeSymbol).Empty
                End If
            End Get
        End Property

        Private Function EnsureTypeParametersAreLoaded(ByRef errorInfo As DiagnosticInfo) As ImmutableArray(Of TypeParameterSymbol)
            Dim typeParams = _lazyTypeParameters
            If Not typeParams.IsDefault Then
                Return typeParams
            End If

            Return InterlockedOperations.Initialize(_lazyTypeParameters, LoadTypeParameters(errorInfo))
        End Function

        Private Function LoadTypeParameters(ByRef errorInfo As DiagnosticInfo) As ImmutableArray(Of TypeParameterSymbol)

            Try
                Dim moduleSymbol = _containingType.ContainingPEModule
                Dim gpHandles = moduleSymbol.Module.GetGenericParametersForMethodOrThrow(_handle)

                If gpHandles.Count = 0 Then
                    Return ImmutableArray(Of TypeParameterSymbol).Empty
                Else
                    Dim ownedParams = ImmutableArray.CreateBuilder(Of TypeParameterSymbol)(gpHandles.Count)
                    For i = 0 To gpHandles.Count - 1
                        ownedParams.Add(New PETypeParameterSymbol(moduleSymbol, Me, CUShort(i), gpHandles(i)))
                    Next

                    Return ownedParams.ToImmutable()
                End If
            Catch mrEx As BadImageFormatException
                errorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedMethod1, CustomSymbolDisplayFormatter.ShortErrorName(Me))
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Try

        End Function

        Friend Overrides ReadOnly Property CallingConvention As Cci.CallingConvention
            Get
                Return CType(Signature.Header.RawValue, Cci.CallingConvention)
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                If Not _lazyExplicitMethodImplementations.IsDefault Then
                    Return _lazyExplicitMethodImplementations
                End If

                Dim moduleSymbol = _containingType.ContainingPEModule

                ' Context: we need the containing type of this method as context so that we can substitute appropriately into
                ' any generic interfaces that we might be explicitly implementing.  There is no reason to pass in the method
                ' context, however, because any method type parameters will belong to the implemented (i.e. interface) method,
                ' which we do not yet know.
                Dim explicitlyOverriddenMethods = New MetadataDecoder(
                        moduleSymbol,
                        _containingType).GetExplicitlyOverriddenMethods(_containingType.Handle, Me._handle, Me.ContainingType)

                'avoid allocating a builder in the common case
                Dim anyToRemove = False
                For Each method In explicitlyOverriddenMethods
                    If Not method.ContainingType.IsInterface Then
                        anyToRemove = True
                        Exit For
                    End If

                Next

                Dim explicitImplementations = explicitlyOverriddenMethods
                If anyToRemove Then
                    Dim explicitInterfaceImplementationsBuilder = ArrayBuilder(Of MethodSymbol).GetInstance()
                    For Each method In explicitlyOverriddenMethods
                        If method.ContainingType.IsInterface Then
                            explicitInterfaceImplementationsBuilder.Add(method)
                        End If

                    Next

                    explicitImplementations = explicitInterfaceImplementationsBuilder.ToImmutableAndFree()
                End If

                Return InterlockedOperations.Initialize(_lazyExplicitMethodImplementations, explicitImplementations)
            End Get

        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            ' Note: m_lazyDocComment is passed ByRef
            Return PEDocumentationCommentUtils.GetDocumentationComment(
                Me, _containingType.ContainingPEModule, preferredCulture, cancellationToken, AccessUncommonFields()._lazyDocComment)
        End Function

        Friend Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function GetUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            If Not _packedFlags.IsUseSiteDiagnosticPopulated Then
                Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = CalculateUseSiteInfo()
                Dim errorInfo As DiagnosticInfo = useSiteInfo.DiagnosticInfo
                DeriveCompilerFeatureRequiredUseSiteInfo(errorInfo)
                EnsureTypeParametersAreLoaded(errorInfo)
                CheckUnmanagedCallersOnly(errorInfo)
                CheckRequiredMembersError(errorInfo)
                Return InitializeUseSiteInfo(useSiteInfo.AdjustDiagnosticInfo(errorInfo))
            End If

            Return GetCachedUseSiteInfo()
        End Function

        Private Function GetCachedUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            Return If(_uncommonFields?._lazyCachedUseSiteInfo, New CachedUseSiteInfo(Of AssemblySymbol)()).ToUseSiteInfo(PrimaryDependency)
        End Function

        Private Sub CheckUnmanagedCallersOnly(ByRef errorInfo As DiagnosticInfo)
            If errorInfo Is Nothing OrElse errorInfo.Code <> ERRID.ERR_UnsupportedMethod1 Then
                Dim hasUnmanagedCallersOnly As Boolean =
                    DirectCast(ContainingModule, PEModuleSymbol).Module.FindTargetAttribute(_handle, AttributeDescription.UnmanagedCallersOnlyAttribute).HasValue

                If hasUnmanagedCallersOnly Then
                    errorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedMethod1, CustomSymbolDisplayFormatter.ShortErrorName(Me))
                End If
            End If
        End Sub

        Private Sub DeriveCompilerFeatureRequiredUseSiteInfo(ByRef errorInfo As DiagnosticInfo)
            If errorInfo IsNot Nothing Then
                Return
            End If

            Dim containingModule = _containingType.ContainingPEModule
            Dim decoder As New MetadataDecoder(containingModule, Me)

            errorInfo = DeriveCompilerFeatureRequiredAttributeDiagnostic(Me, DirectCast(containingModule, PEModuleSymbol), Handle, CompilerFeatureRequiredFeatures.RequiredMembers, decoder)
            If errorInfo IsNot Nothing Then
                Return
            End If

            errorInfo = Signature.ReturnParam.DeriveCompilerFeatureRequiredDiagnostic(decoder)
            If errorInfo IsNot Nothing Then
                Return
            End If

            For Each parameter In Parameters
                errorInfo = DirectCast(parameter, PEParameterSymbol).DeriveCompilerFeatureRequiredDiagnostic(decoder)
                If errorInfo IsNot Nothing Then
                    Return
                End If
            Next

            For Each typeParameter In TypeParameters
                errorInfo = DirectCast(typeParameter, PETypeParameterSymbol).DeriveCompilerFeatureRequiredDiagnostic(decoder)
                If errorInfo IsNot Nothing Then
                    Return
                End If
            Next

            errorInfo = _containingType.GetCompilerFeatureRequiredDiagnostic()
        End Sub

        Private Sub CheckRequiredMembersError(ByRef errorInfo As DiagnosticInfo)
            If errorInfo Is Nothing AndAlso
               MethodKind = MethodKind.Constructor AndAlso
               (Not HasSetsRequiredMembers) AndAlso
               ContainingType.HasRequiredMembersError Then

                errorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_RequiredMembersInvalid, ContainingType)
            End If

        End Sub

        Private Function InitializeUseSiteInfo(useSiteInfo As UseSiteInfo(Of AssemblySymbol)) As UseSiteInfo(Of AssemblySymbol)
            If _packedFlags.IsUseSiteDiagnosticPopulated Then
                Return GetCachedUseSiteInfo()
            End If

            If useSiteInfo.DiagnosticInfo IsNot Nothing OrElse Not useSiteInfo.SecondaryDependencies.IsNullOrEmpty() Then
                useSiteInfo = AccessUncommonFields()._lazyCachedUseSiteInfo.InterlockedInitializeFromDefault(PrimaryDependency, useSiteInfo)
            End If

            _packedFlags.SetIsUseSiteDiagnosticPopulated()
            Return useSiteInfo
        End Function

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                If Not _packedFlags.IsObsoleteAttributePopulated Then
                    Dim result = ObsoleteAttributeHelpers.GetObsoleteDataFromMetadata(_handle, DirectCast(ContainingModule, PEModuleSymbol))
                    If result IsNot Nothing Then
                        result = InterlockedOperations.Initialize(AccessUncommonFields()._lazyObsoleteAttributeData, result, ObsoleteAttributeData.Uninitialized)
                    End If

                    _packedFlags.SetIsObsoleteAttributePopulated()
                    Return result
                End If

                Dim uncommonFields = _uncommonFields
                If uncommonFields Is Nothing Then
                    Return Nothing
                Else
                    Dim result = uncommonFields._lazyObsoleteAttributeData
                    Return If(result Is ObsoleteAttributeData.Uninitialized,
                              InterlockedOperations.Initialize(uncommonFields._lazyObsoleteAttributeData, initializedValue:=Nothing, ObsoleteAttributeData.Uninitialized),
                              result)
                End If
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            If Not _packedFlags.IsConditionalPopulated Then
                Dim moduleSymbol As PEModuleSymbol = _containingType.ContainingPEModule
                Dim conditionalSymbols As ImmutableArray(Of String) = moduleSymbol.Module.GetConditionalAttributeValues(_handle)
                Debug.Assert(Not conditionalSymbols.IsDefault)
                If Not conditionalSymbols.IsEmpty Then
                    conditionalSymbols = InterlockedOperations.Initialize(AccessUncommonFields()._lazyConditionalAttributeSymbols, conditionalSymbols)
                End If

                _packedFlags.SetIsConditionalAttributePopulated()
                Return conditionalSymbols
            End If

            Dim uncommonFields = _uncommonFields
            If uncommonFields Is Nothing Then
                Return ImmutableArray(Of String).Empty
            Else
                Dim result = uncommonFields._lazyConditionalAttributeSymbols
                Return If(result.IsDefault,
                    InterlockedOperations.Initialize(uncommonFields._lazyConditionalAttributeSymbols, ImmutableArray(Of String).Empty),
                    result)
            End If
        End Function

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function TryGetMeParameter(<Out> ByRef meParameter As ParameterSymbol) As Boolean
            If IsShared Then
                meParameter = Nothing
            Else
                meParameter = If(_uncommonFields?._lazyMeParameter, InterlockedOperations.Initialize(AccessUncommonFields()._lazyMeParameter, New MeParameterSymbol(Me)))
            End If
            Return True
        End Function

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides ReadOnly Property HasSetsRequiredMembers As Boolean
            Get
                If MethodKind <> MethodKind.Constructor Then
                    Return False
                End If

                Dim hasSetsRequiredMembersValue As Boolean = False
                If _packedFlags.TryGetHasSetsRequiredMembers(hasSetsRequiredMembersValue) Then
                    Return hasSetsRequiredMembersValue
                End If

                hasSetsRequiredMembersValue = DirectCast(ContainingModule, PEModuleSymbol).Module.HasAttribute(Handle, AttributeDescription.SetsRequiredMembersAttribute)
                _packedFlags.InitializeSetsRequiredMembers(hasSetsRequiredMembersValue)

                Return hasSetsRequiredMembersValue
            End Get
        End Property
    End Class

End Namespace
