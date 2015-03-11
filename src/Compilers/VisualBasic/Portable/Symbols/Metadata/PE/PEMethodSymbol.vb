' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.Threading
Imports System.Reflection
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' The class to represent all methods imported from a PE/module.
    ''' </summary>
    Friend NotInheritable Class PEMethodSymbol
        Inherits MethodSymbol

        Private Const s_uninitializedMethodKind As Integer = -1

        Private ReadOnly _handle As MethodDefinitionHandle
        Private ReadOnly _name As String
        Private ReadOnly _implFlags As MethodImplAttributes
        Private ReadOnly _flags As MethodAttributes
        Private ReadOnly _containingType As PENamedTypeSymbol

        Private _associatedPropertyOrEventOpt As Symbol

        Private _lazyMethodKind As Integer = s_uninitializedMethodKind ' really a MethodKind, but Interlocked.CompareExchange doesn't handle those

        Private _lazyTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Private _lazyDocComment As Tuple(Of CultureInfo, String)

        Private _lazyExplicitMethodImplementations As ImmutableArray(Of MethodSymbol)

        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)
        Private _lazyConditionalAttributeSymbols As ImmutableArray(Of String)

        Private _lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 

        Private _lazyIsExtensionMethod As Byte = ThreeState.Unknown
        Private _lazyObsoleteAttributeData As ObsoleteAttributeData = ObsoleteAttributeData.Uninitialized

        Private _lazyMeParameter As ParameterSymbol


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
                Dim rva As Integer
                moduleSymbol.Module.GetMethodDefPropsOrThrow(handle, _name, _implFlags, _flags, rva)
            Catch mrEx As BadImageFormatException
                If _name Is Nothing Then
                    _name = String.Empty
                End If

                _lazyUseSiteErrorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedMethod1, CustomSymbolDisplayFormatter.ShortErrorName(Me))
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
                Return _implFlags
            End Get
        End Property

        Friend ReadOnly Property MethodFlags As MethodAttributes
            Get
                Return _flags
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                If _lazyMethodKind = s_uninitializedMethodKind Then
                    Dim computed As MethodKind = ComputeMethodKind()
                    Dim oldValue As Integer = Interlocked.CompareExchange(_lazyMethodKind, CType(computed, Integer), s_uninitializedMethodKind)
                    Debug.Assert(oldValue = s_uninitializedMethodKind OrElse oldValue = computed)
                End If

                Return CType(_lazyMethodKind, MethodKind)
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
            If _lazyMethodKind <> s_uninitializedMethodKind Then
                Return _lazyMethodKind = MethodKind.Constructor AndAlso ParameterCount = 0
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

                Dim oldValue As Integer = Interlocked.CompareExchange(_lazyMethodKind, MethodKind.Constructor, s_uninitializedMethodKind)
                Debug.Assert(oldValue = s_uninitializedMethodKind OrElse oldValue = MethodKind.Constructor)

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

                    Select Case contender._lazyMethodKind
                        Case s_uninitializedMethodKind, MethodKind.Ordinary
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

                    If potentialMethodKind = MethodKind.Conversion AndAlso Not outputType.IsSameTypeIgnoringCustomModifiers(contender.ReturnType) Then
                        Continue For
                    End If

                    Dim j As Integer
                    For j = 0 To inputParams.Length - 1
                        If Not inputParams(j).Type.IsSameTypeIgnoringCustomModifiers(contender.Parameters(j).Type) Then
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
                        Dim oldValue As Integer = Interlocked.CompareExchange(contender._lazyMethodKind, MethodKind.Ordinary, s_uninitializedMethodKind)
                        Debug.Assert(oldValue = s_uninitializedMethodKind OrElse oldValue = MethodKind.Ordinary)
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
                        Debug.Assert(False, "Unexpected!!!")
                End Select

                Return access

            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If _lazyCustomAttributes.IsDefault Then
                Dim containingPEModuleSymbol = DirectCast(ContainingModule(), PEModuleSymbol)
                containingPEModuleSymbol.LoadCustomAttributes(Me.Handle, _lazyCustomAttributes)
            End If
            Return _lazyCustomAttributes
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            Return GetAttributes()
        End Function

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                If _lazyIsExtensionMethod = ThreeState.Unknown Then

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

                    If result Then
                        _lazyIsExtensionMethod = ThreeState.True
                    Else
                        _lazyIsExtensionMethod = ThreeState.False
                    End If
                End If

                Return _lazyIsExtensionMethod = ThreeState.True
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

            ' do not cache the result, the compiler doesn't use this (it's only exposed thru public API):
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
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                EnsureSignatureIsLoaded()
                Return _lazySignature.Header.CallingConvention = SignatureCallingConvention.VarArgs
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
                        (MethodAttributes.Virtual Or MethodAttributes.Final)
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

        Friend Overrides ReadOnly Property IsHiddenBySignature As Boolean
            Get
                Return (_flags And MethodAttributes.HideBySig) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Dim flagsToCheck As MethodAttributes = (_flags And
                                                        (MethodAttributes.Virtual Or
                                                         MethodAttributes.Final Or
                                                         MethodAttributes.Abstract Or
                                                         MethodAttributes.NewSlot))

                Return flagsToCheck = (MethodAttributes.Virtual Or MethodAttributes.NewSlot) OrElse
                       (flagsToCheck = MethodAttributes.Virtual AndAlso _containingType.BaseTypeNoUseSiteDiagnostics Is Nothing)
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
                Return (_flags And MethodAttributes.Virtual) <> 0 AndAlso
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
                EnsureSignatureIsLoaded()
                Return _lazySignature.Parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                EnsureSignatureIsLoaded()
                Return _lazySignature.ReturnParam.Type
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                EnsureSignatureIsLoaded()
                Return _lazySignature.ReturnParam.CustomModifiers
            End Get
        End Property

        Public Overrides Function GetReturnTypeAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            EnsureSignatureIsLoaded()
            Return _lazySignature.ReturnParam.GetAttributes()
        End Function

        Friend ReadOnly Property ReturnParam As PEParameterSymbol
            Get
                EnsureSignatureIsLoaded()
                Return _lazySignature.ReturnParam
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
                Debug.Assert(propertyOrEventSymbol.ContainingType = Me.ContainingType)
                Me._associatedPropertyOrEventOpt = propertyOrEventSymbol
                _lazyMethodKind = CType(methodKind, Integer)
                Return True
            End If

            Return False
        End Function

        Private Sub EnsureSignatureIsLoaded()
            If _lazySignature Is Nothing Then

                Dim moduleSymbol = _containingType.ContainingPEModule

                Dim signatureHeader As SignatureHeader
                Dim mrEx As BadImageFormatException = Nothing
                Dim paramInfo() As ParamInfo(Of TypeSymbol) =
                    (New MetadataDecoder(moduleSymbol, Me)).GetSignatureForMethod(_handle, signatureHeader, mrEx)

                ' If method is not generic, let's assign empty list for type parameters
                If Not signatureHeader.IsGeneric() AndAlso
                    _lazyTypeParameters.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(_lazyTypeParameters,
                                                ImmutableArray(Of TypeParameterSymbol).Empty, Nothing)
                End If

                Dim count As Integer = paramInfo.Length - 1
                Dim params As ImmutableArray(Of ParameterSymbol)
                Dim isBad As Boolean
                Dim hasBadParameter As Boolean = False

                If count > 0 Then
                    Dim parameterCreation(count - 1) As ParameterSymbol

                    For i As Integer = 0 To count - 1 Step 1
                        parameterCreation(i) = New PEParameterSymbol(moduleSymbol, Me, i, paramInfo(i + 1), isBad)

                        If isBad Then
                            hasBadParameter = True
                        End If
                    Next

                    params = parameterCreation.AsImmutableOrNull()
                Else
                    params = ImmutableArray(Of ParameterSymbol).Empty
                End If

                ' paramInfo(0) contains information about return "parameter"
                Debug.Assert(Not paramInfo(0).IsByRef)
                Dim returnParam = New PEParameterSymbol(moduleSymbol, Me, 0, paramInfo(0), isBad)

                If mrEx IsNot Nothing OrElse hasBadParameter OrElse isBad Then
                    Dim old = Interlocked.CompareExchange(_lazyUseSiteErrorInfo,
                                                          ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedMethod1, CustomSymbolDisplayFormatter.ShortErrorName(Me)),
                                                          ErrorFactory.EmptyErrorInfo)
                    Debug.Assert(old Is ErrorFactory.EmptyErrorInfo OrElse
                                 (old IsNot Nothing AndAlso old.Code = ERRID.ERR_UnsupportedMethod1))
                End If

                Dim signature As New SignatureData(signatureHeader, params, returnParam)
                Interlocked.CompareExchange(_lazySignature, signature, Nothing)
            End If
        End Sub

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                EnsureTypeParametersAreLoaded()
                Return _lazyTypeParameters
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

        Private Sub EnsureTypeParametersAreLoaded()

            If _lazyTypeParameters.IsDefault Then

                Dim typeParams As ImmutableArray(Of TypeParameterSymbol)

                Try
                    Dim moduleSymbol = _containingType.ContainingPEModule
                    Dim gpHandles = moduleSymbol.Module.GetGenericParametersForMethodOrThrow(_handle)


                    If gpHandles.Count = 0 Then
                        typeParams = ImmutableArray(Of TypeParameterSymbol).Empty
                    Else
                        Dim ownedParams(gpHandles.Count - 1) As PETypeParameterSymbol

                        For i = 0 To ownedParams.Length - 1
                            ownedParams(i) = New PETypeParameterSymbol(moduleSymbol, Me, CUShort(i), gpHandles(i))
                        Next

                        typeParams = StaticCast(Of TypeParameterSymbol).From(ownedParams.AsImmutableOrNull)
                    End If
                Catch mrEx As BadImageFormatException
                    Dim old = Interlocked.CompareExchange(_lazyUseSiteErrorInfo,
                                                          ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedMethod1, CustomSymbolDisplayFormatter.ShortErrorName(Me)),
                                                          ErrorFactory.EmptyErrorInfo)
                    Debug.Assert(old Is ErrorFactory.EmptyErrorInfo OrElse
                                 (old IsNot Nothing AndAlso old.Code = ERRID.ERR_UnsupportedMethod1))

                    typeParams = ImmutableArray(Of TypeParameterSymbol).Empty
                End Try

                ImmutableInterlocked.InterlockedCompareExchange(_lazyTypeParameters, typeParams, Nothing)
            End If

        End Sub

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                EnsureSignatureIsLoaded()
                Return CType(_lazySignature.Header.RawValue, Microsoft.Cci.CallingConvention)
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                If _lazyExplicitMethodImplementations.IsDefault Then
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

                    ImmutableInterlocked.InterlockedCompareExchange(_lazyExplicitMethodImplementations, explicitImplementations, Nothing)
                End If

                Return _lazyExplicitMethodImplementations
            End Get

        End Property


        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            ' Note: m_lazyDocComment is passed ByRef
            Return PEDocumentationCommentUtils.GetDocumentationComment(
                Me, _containingType.ContainingPEModule, preferredCulture, cancellationToken, _lazyDocComment)
        End Function

        Friend Overrides ReadOnly Property Syntax As VisualBasicSyntaxNode
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            If _lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                Dim errorInfo As DiagnosticInfo = CalculateUseSiteErrorInfo()
                EnsureTypeParametersAreLoaded()
                Interlocked.CompareExchange(_lazyUseSiteErrorInfo, errorInfo, ErrorFactory.EmptyErrorInfo)
            End If

            Return _lazyUseSiteErrorInfo
        End Function

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(_lazyObsoleteAttributeData, _handle, DirectCast(ContainingModule, PEModuleSymbol))
                Return _lazyObsoleteAttributeData
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            If Me._lazyConditionalAttributeSymbols.IsDefaultOrEmpty Then
                Dim moduleSymbol As PEModuleSymbol = _containingType.ContainingPEModule
                Dim conditionalSymbols As ImmutableArray(Of String) = moduleSymbol.Module.GetConditionalAttributeValues(_handle)
                Debug.Assert(Not conditionalSymbols.IsDefault)
                ImmutableInterlocked.InterlockedCompareExchange(_lazyConditionalAttributeSymbols, conditionalSymbols, Nothing)
            End If

            Return Me._lazyConditionalAttributeSymbols
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
            meParameter = _lazyMeParameter
            If meParameter IsNot Nothing OrElse Me.IsShared Then
                Return True
            End If

            Interlocked.CompareExchange(_lazyMeParameter, New MeParameterSymbol(Me), Nothing)
            meParameter = _lazyMeParameter
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
    End Class

End Namespace
