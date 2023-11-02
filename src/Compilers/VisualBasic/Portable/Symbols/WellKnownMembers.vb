' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class VisualBasicCompilation

        Private ReadOnly _wellKnownMemberSignatureComparer As New WellKnownMembersSignatureComparer(Me)

        ''' <summary>
        ''' An array of cached well known types available for use in this Compilation.
        ''' Lazily filled by GetWellKnownType method.
        ''' </summary>
        ''' <remarks></remarks>
        Private _lazyWellKnownTypes() As NamedTypeSymbol

        ''' <summary>
        ''' Lazy cache of well known members.
        ''' Not yet known value is represented by ErrorTypeSymbol.UnknownResultType
        ''' </summary>
        Private _lazyWellKnownTypeMembers() As Symbol

        Private _lazyExtensionAttributeConstructor As Symbol = ErrorTypeSymbol.UnknownResultType ' Not yet known.
        Private _lazyExtensionAttributeConstructorErrorInfo As Object 'Actually, DiagnosticInfo

#Region "Synthesized Attributes"
        Friend Function GetExtensionAttributeConstructor(<Out> ByRef useSiteInfo As UseSiteInfo(Of AssemblySymbol)) As MethodSymbol

            Dim attributeCtor As MethodSymbol = Nothing
            Dim ctorError As DiagnosticInfo = Nothing

            If _lazyExtensionAttributeConstructor Is ErrorTypeSymbol.UnknownResultType Then

                Dim system_Runtime_CompilerServices = Me.GlobalNamespace.LookupNestedNamespace(ImmutableArray.Create(MetadataHelpers.SystemString, "Runtime", "CompilerServices"))
                Dim attributeType As NamedTypeSymbol = Nothing

                Dim sourceModuleSymbol = DirectCast(Me.SourceModule, SourceModuleSymbol)
                Dim sourceModuleBinder As Binder = BinderBuilder.CreateSourceModuleBinder(sourceModuleSymbol)

                If system_Runtime_CompilerServices IsNot Nothing Then
                    Dim candidates = system_Runtime_CompilerServices.GetTypeMembers(AttributeDescription.CaseInsensitiveExtensionAttribute.Name, 0)
                    Dim ambiguity As Boolean = False

                    For Each candidate As NamedTypeSymbol In candidates
                        If Not sourceModuleBinder.IsAccessible(candidate, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded) Then
                            Continue For
                        End If

                        If candidate.ContainingModule Is sourceModuleSymbol Then
                            ' Type from this module always better.
                            attributeType = candidate
                            ambiguity = False
                            Exit For
                        End If

                        If attributeType Is Nothing Then
                            Debug.Assert(Not ambiguity)
                            attributeType = candidate
                        ElseIf candidate.ContainingAssembly Is Me.Assembly Then
                            If attributeType.ContainingAssembly Is Me.Assembly Then
                                ambiguity = True
                            Else
                                attributeType = candidate
                                ambiguity = False
                            End If
                        ElseIf attributeType.ContainingAssembly IsNot Me.Assembly Then
                            Debug.Assert(candidate.ContainingAssembly IsNot Me.Assembly)
                            ambiguity = True
                        End If
                    Next

                    If ambiguity Then
                        Debug.Assert(attributeType IsNot Nothing)
                        attributeType = Nothing
                    End If
                End If

                If attributeType IsNot Nothing AndAlso
                   Not attributeType.IsStructureType AndAlso
                   Not attributeType.IsMustInherit AndAlso
                   GetWellKnownType(WellKnownType.System_Attribute).IsBaseTypeOf(attributeType, CompoundUseSiteInfo(Of AssemblySymbol).Discarded) AndAlso
                   sourceModuleBinder.IsAccessible(attributeType, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded) Then

                    For Each ctor In attributeType.InstanceConstructors
                        If ctor.ParameterCount = 0 Then
                            If sourceModuleBinder.IsAccessible(ctor, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded) Then
                                attributeCtor = ctor
                            End If

                            Exit For
                        End If
                    Next
                End If

                If attributeCtor Is Nothing Then
                    ' TODO (tomat): It is not clear under what circumstances is this error reported since the binder already reports errors when the conditions above are not satisfied.
                    ctorError = ErrorFactory.ErrorInfo(ERRID.ERR_MissingRuntimeHelper,
                                                       AttributeDescription.CaseInsensitiveExtensionAttribute.FullName & "." & WellKnownMemberNames.InstanceConstructorName)
                Else
                    Dim attributeUsage As AttributeUsageInfo = attributeCtor.ContainingType.GetAttributeUsageInfo()
                    Debug.Assert(Not attributeUsage.IsNull)

                    Const requiredTargets As AttributeTargets = AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method
                    If (attributeUsage.ValidTargets And requiredTargets) <> requiredTargets Then
                        ctorError = ErrorFactory.ErrorInfo(ERRID.ERR_ExtensionAttributeInvalid)
                    End If
                End If

                ' Storing m_LazyExtensionAttributeConstructorErrorInfo first.
                _lazyExtensionAttributeConstructorErrorInfo = ctorError
                Interlocked.CompareExchange(_lazyExtensionAttributeConstructor,
                                            DirectCast(attributeCtor, Symbol),
                                            DirectCast(ErrorTypeSymbol.UnknownResultType, Symbol))
            End If

            attributeCtor = DirectCast(_lazyExtensionAttributeConstructor, MethodSymbol)
            ctorError = DirectCast(Volatile.Read(_lazyExtensionAttributeConstructorErrorInfo), DiagnosticInfo)

            If ctorError IsNot Nothing Then
                useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(ctorError)
            Else
                useSiteInfo = Binder.GetUseSiteInfoForMemberAndContainingType(attributeCtor)
            End If

            Return attributeCtor
        End Function

        ''' <summary>
        ''' Synthesizes a custom attribute. 
        ''' Returns Nothing if the <paramref name="constructor"/> symbol is missing,
        ''' or any of the members in <paramref name="namedArguments" /> are missing.
        ''' The attribute is synthesized only if present.
        ''' </summary>
        ''' <param name="namedArguments">
        ''' Takes a list of pairs of well-known members and constants. The constants
        ''' will be passed to the field/property referenced by the well-known member.
        ''' If the well-known member does Not exist in the compilation then no attribute
        ''' will be synthesized.
        ''' </param>
        ''' <param name="isOptionalUse">
        ''' Indicates if this particular attribute application should be considered optional.
        ''' </param>
        Friend Function TrySynthesizeAttribute(
            constructor As WellKnownMember,
            Optional arguments As ImmutableArray(Of TypedConstant) = Nothing,
            Optional namedArguments As ImmutableArray(Of KeyValuePair(Of WellKnownMember, TypedConstant)) = Nothing,
            Optional isOptionalUse As Boolean = False
        ) As SynthesizedAttributeData

            Dim constructorSymbol = TryCast(GetWellKnownTypeMember(constructor), MethodSymbol)
            If constructorSymbol Is Nothing OrElse
               Binder.GetUseSiteInfoForWellKnownTypeMember(constructorSymbol, constructor, False).DiagnosticInfo IsNot Nothing Then
                Return ReturnNothingOrThrowIfAttributeNonOptional(constructor, isOptionalUse)
            End If

            If arguments.IsDefault Then
                arguments = ImmutableArray(Of TypedConstant).Empty
            End If

            Dim namedStringArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))
            If namedArguments.IsDefault Then
                namedStringArguments = ImmutableArray(Of KeyValuePair(Of String, TypedConstant)).Empty
            Else
                Dim builder = New ArrayBuilder(Of KeyValuePair(Of String, TypedConstant))(namedArguments.Length)
                For Each arg In namedArguments
                    Dim wellKnownMember = GetWellKnownTypeMember(arg.Key)
                    If wellKnownMember Is Nothing OrElse
                       TypeOf wellKnownMember Is ErrorTypeSymbol OrElse
                       Binder.GetUseSiteInfoForWellKnownTypeMember(wellKnownMember, arg.Key, False).DiagnosticInfo IsNot Nothing Then
                        Return ReturnNothingOrThrowIfAttributeNonOptional(constructor)
                    Else
                        builder.Add(New KeyValuePair(Of String, TypedConstant)(wellKnownMember.Name, arg.Value))
                    End If
                Next
                namedStringArguments = builder.ToImmutableAndFree()
            End If

            Return New SynthesizedAttributeData(Me, constructorSymbol, arguments, namedStringArguments)
        End Function

        Private Shared Function ReturnNothingOrThrowIfAttributeNonOptional(constructor As WellKnownMember, Optional isOptionalUse As Boolean = False) As SynthesizedAttributeData
            If isOptionalUse OrElse WellKnownMembers.IsSynthesizedAttributeOptional(constructor) Then
                Return Nothing
            Else
                Throw ExceptionUtilities.Unreachable
            End If
        End Function

        Friend Function SynthesizeExtensionAttribute() As SynthesizedAttributeData
            Dim constructor As MethodSymbol = GetExtensionAttributeConstructor(useSiteInfo:=Nothing)

            Debug.Assert(constructor IsNot Nothing AndAlso
                         constructor.GetUseSiteInfo().DiagnosticInfo Is Nothing AndAlso
                         constructor.ContainingType.GetUseSiteInfo().DiagnosticInfo Is Nothing)

            Return SynthesizedAttributeData.Create(Me, constructor, WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor)
        End Function

        Friend Function SynthesizeStateMachineAttribute(method As MethodSymbol, compilationState As ModuleCompilationState) As SynthesizedAttributeData
            Debug.Assert(method.IsAsync OrElse method.IsIterator)

            ' The async state machine type is not synthesized until the async method body is rewritten. If we are
            ' only emitting metadata the method body will not have been rewritten, and the async state machine
            ' type will not have been created. In this case, omit the attribute.
            Dim stateMachineType As NamedTypeSymbol = Nothing
            If compilationState.TryGetStateMachineType(method, stateMachineType) Then

                Dim ctor = If(method.IsAsync, WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor,
                                                WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor)

                Dim arg = New TypedConstant(GetWellKnownType(WellKnownType.System_Type),
                                            TypedConstantKind.Type,
                                            If(stateMachineType.IsGenericType, stateMachineType.ConstructUnboundGenericType(), stateMachineType))

                Return TrySynthesizeAttribute(ctor, ImmutableArray.Create(arg))
            End If

            Return Nothing
        End Function

        Friend Function SynthesizeDecimalConstantAttribute(value As Decimal) As SynthesizedAttributeData
            Dim isNegative As Boolean
            Dim scale As Byte
            Dim low, mid, high As UInteger
            value.GetBits(isNegative, scale, low, mid, high)

            Dim specialTypeByte = GetSpecialType(SpecialType.System_Byte)
            Debug.Assert(specialTypeByte.GetUseSiteInfo().DiagnosticInfo Is Nothing)

            Dim specialTypeUInt32 = GetSpecialType(SpecialType.System_UInt32)
            Debug.Assert(specialTypeUInt32.GetUseSiteInfo().DiagnosticInfo Is Nothing)

            Return TrySynthesizeAttribute(
                WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor,
                ImmutableArray.Create(
                    New TypedConstant(specialTypeByte, TypedConstantKind.Primitive, scale),
                    New TypedConstant(specialTypeByte, TypedConstantKind.Primitive, CByte(If(isNegative, 128, 0))),
                    New TypedConstant(specialTypeUInt32, TypedConstantKind.Primitive, high),
                    New TypedConstant(specialTypeUInt32, TypedConstantKind.Primitive, mid),
                    New TypedConstant(specialTypeUInt32, TypedConstantKind.Primitive, low)
                ))
        End Function

        Friend Function SynthesizeDebuggerBrowsableNeverAttribute() As SynthesizedAttributeData
            If Options.OptimizationLevel <> OptimizationLevel.Debug Then
                Return Nothing
            End If

            Return TrySynthesizeAttribute(
                WellKnownMember.System_Diagnostics_DebuggerBrowsableAttribute__ctor,
                ImmutableArray.Create(New TypedConstant(GetWellKnownType(WellKnownType.System_Diagnostics_DebuggerBrowsableState),
                                                                TypedConstantKind.Enum,
                                                                DebuggerBrowsableState.Never)))
        End Function

        Friend Function SynthesizeDebuggerHiddenAttribute() As SynthesizedAttributeData
            If Options.OptimizationLevel <> OptimizationLevel.Debug Then
                Return Nothing
            End If

            Return TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor)
        End Function

        Friend Function SynthesizeEditorBrowsableNeverAttribute() As SynthesizedAttributeData
            Return TrySynthesizeAttribute(
                WellKnownMember.System_ComponentModel_EditorBrowsableAttribute__ctor,
                ImmutableArray.Create(New TypedConstant(GetWellKnownType(WellKnownType.System_ComponentModel_EditorBrowsableState),
                                                             TypedConstantKind.Enum,
                                                             System.ComponentModel.EditorBrowsableState.Never)))
        End Function

        Friend Function SynthesizeDebuggerNonUserCodeAttribute() As SynthesizedAttributeData
            If Options.OptimizationLevel <> OptimizationLevel.Debug Then
                Return Nothing
            End If

            Return TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerNonUserCodeAttribute__ctor)
        End Function

        Friend Function SynthesizeOptionalDebuggerStepThroughAttribute() As SynthesizedAttributeData
            If Options.OptimizationLevel <> OptimizationLevel.Debug Then
                Return Nothing
            End If

            Debug.Assert(
                    WellKnownMembers.IsSynthesizedAttributeOptional(
                        WellKnownMember.System_Diagnostics_DebuggerStepThroughAttribute__ctor))

            Return TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerStepThroughAttribute__ctor)
        End Function

#End Region

        ''' <summary>
        ''' Lookup member declaration in well known type used by this Compilation.
        ''' </summary>
        Friend Function GetWellKnownTypeMember(member As WellKnownMember) As Symbol
            Debug.Assert(member >= 0 AndAlso member < WellKnownMember.Count)

            ' Test hook If a member Is marked missing, Then Return null.
            If IsMemberMissing(member) Then
                Return Nothing
            End If

            If _lazyWellKnownTypeMembers Is Nothing OrElse _lazyWellKnownTypeMembers(member) Is ErrorTypeSymbol.UnknownResultType Then
                If (_lazyWellKnownTypeMembers Is Nothing) Then
                    Dim wellKnownTypeMembers = New Symbol(WellKnownMember.Count - 1) {}

                    For i As Integer = 0 To wellKnownTypeMembers.Length - 1
                        wellKnownTypeMembers(i) = ErrorTypeSymbol.UnknownResultType
                    Next

                    Interlocked.CompareExchange(_lazyWellKnownTypeMembers, wellKnownTypeMembers, Nothing)
                End If

                Dim descriptor = WellKnownMembers.GetDescriptor(member)
                Dim type = If(descriptor.DeclaringTypeId <= SpecialType.Count,
                              GetSpecialType(CType(descriptor.DeclaringTypeId, SpecialType)),
                              GetWellKnownType(CType(descriptor.DeclaringTypeId, WellKnownType)))

                Dim result As Symbol = Nothing

                If Not type.IsErrorType() Then
                    result = VisualBasicCompilation.GetRuntimeMember(type, descriptor, _wellKnownMemberSignatureComparer, accessWithinOpt:=Me.Assembly)
                End If

                Interlocked.CompareExchange(_lazyWellKnownTypeMembers(member), result, DirectCast(ErrorTypeSymbol.UnknownResultType, Symbol))
            End If

            Return _lazyWellKnownTypeMembers(member)
        End Function

        Friend Overrides Function IsSystemTypeReference(type As ITypeSymbolInternal) As Boolean
            Return TypeSymbol.Equals(DirectCast(type, TypeSymbol), GetWellKnownType(WellKnownType.System_Type), TypeCompareKind.ConsiderEverything)
        End Function

        Friend Overrides Function CommonGetWellKnownTypeMember(member As WellKnownMember) As ISymbolInternal
            Return GetWellKnownTypeMember(member)
        End Function

        Friend Overrides Function CommonGetWellKnownType(wellknownType As WellKnownType) As ITypeSymbolInternal
            Return GetWellKnownType(wellknownType)
        End Function

        Friend Overrides Function IsAttributeType(type As ITypeSymbol) As Boolean
            If type.Kind <> SymbolKind.NamedType Then
                Return False
            End If

            Return DirectCast(type, NamedTypeSymbol).IsOrDerivedFromWellKnownClass(WellKnownType.System_Attribute, Me, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
        End Function

        ''' <summary>
        ''' In case duplicate types are encountered, returns an error type.
        ''' But if the IgnoreCorLibraryDuplicatedTypes compilation option is set, any duplicate type found in corlib is ignored and doesn't count as a duplicate.
        ''' </summary>
        Friend Function GetWellKnownType(type As WellKnownType) As NamedTypeSymbol
            Debug.Assert(type.IsWellKnownType())
            Dim index As Integer = type - WellKnownType.First

            If _lazyWellKnownTypes Is Nothing OrElse _lazyWellKnownTypes(index) Is Nothing Then
                If (_lazyWellKnownTypes Is Nothing) Then
                    Interlocked.CompareExchange(_lazyWellKnownTypes,
                        New NamedTypeSymbol(WellKnownTypes.Count - 1) {}, Nothing)
                End If

                Dim mdName As String = WellKnownTypes.GetMetadataName(type)
                Dim result As NamedTypeSymbol
                Dim conflicts As (AssemblySymbol, AssemblySymbol) = Nothing

                If IsTypeMissing(type) Then
                    result = Nothing
                Else
                    result = Me.Assembly.GetTypeByMetadataName(mdName, includeReferences:=True, isWellKnownType:=True, useCLSCompliantNameArityEncoding:=True, conflicts:=conflicts,
                                                               ignoreCorLibraryDuplicatedTypes:=Me.Options.IgnoreCorLibraryDuplicatedTypes)
                    Debug.Assert(If(Not result?.IsErrorType(), True))
                End If

                If result Is Nothing Then
                    Dim emittedName As MetadataTypeName = MetadataTypeName.FromFullName(mdName, useCLSCompliantNameArityEncoding:=True)

                    If type.IsValueTupleType() Then

                        Dim delayedErrorInfo As Func(Of MissingMetadataTypeSymbol.TopLevelWithCustomErrorInfo, DiagnosticInfo)
                        If conflicts.Item1 Is Nothing Then
                            Debug.Assert(conflicts.Item2 Is Nothing)

                            delayedErrorInfo = Function(t) ErrorFactory.ErrorInfo(ERRID.ERR_ValueTupleTypeRefResolutionError1, t)
                        Else
                            Dim capturedConflicts = conflicts
                            delayedErrorInfo = Function(t) ErrorFactory.ErrorInfo(ERRID.ERR_ValueTupleResolutionAmbiguous3, t, capturedConflicts.Item1, capturedConflicts.Item2)
                        End If

                        result = New MissingMetadataTypeSymbol.TopLevelWithCustomErrorInfo(Assembly.Modules(0), emittedName, delayedErrorInfo)
                    Else
                        result = New MissingMetadataTypeSymbol.TopLevel(Assembly.Modules(0), emittedName)
                    End If
                End If

                If (Interlocked.CompareExchange(_lazyWellKnownTypes(index), result, Nothing) IsNot Nothing) Then
                    Debug.Assert(result Is _lazyWellKnownTypes(index) OrElse
                                          (_lazyWellKnownTypes(index).IsErrorType() AndAlso result.IsErrorType()))
                End If

            End If

            Return _lazyWellKnownTypes(index)
        End Function

        Friend Shared Function GetRuntimeMember(
            ByVal declaringType As NamedTypeSymbol,
            ByRef descriptor As MemberDescriptor,
            ByVal comparer As SignatureComparer(Of MethodSymbol, FieldSymbol, PropertySymbol, TypeSymbol, ParameterSymbol),
            ByVal accessWithinOpt As AssemblySymbol
        ) As Symbol
            Dim result As Symbol = Nothing

            Dim targetSymbolKind As SymbolKind
            Dim targetMethodKind As MethodKind = MethodKind.Ordinary

            Dim isShared As Boolean = (descriptor.Flags And MemberFlags.Static) <> 0

            Select Case descriptor.Flags And MemberFlags.KindMask
                Case MemberFlags.Constructor
                    targetSymbolKind = SymbolKind.Method
                    targetMethodKind = MethodKind.Constructor
                    Debug.Assert(Not isShared) 'static constructors are never called explicitly

                Case MemberFlags.Method
                    targetSymbolKind = SymbolKind.Method

                Case MemberFlags.PropertyGet
                    targetSymbolKind = SymbolKind.Method
                    targetMethodKind = MethodKind.PropertyGet

                Case MemberFlags.Field
                    targetSymbolKind = SymbolKind.Field

                Case MemberFlags.Property
                    targetSymbolKind = SymbolKind.Property

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(descriptor.Flags)

            End Select

            For Each m In declaringType.GetMembers(descriptor.Name)
                If m.Kind <> targetSymbolKind OrElse m.IsShared <> isShared OrElse
                    Not (m.DeclaredAccessibility = Accessibility.Public OrElse (accessWithinOpt IsNot Nothing AndAlso Symbol.IsSymbolAccessible(m, accessWithinOpt))) Then
                    Continue For
                End If

                If Not String.Equals(m.Name, descriptor.Name, StringComparison.Ordinal) Then
                    Continue For
                End If

                Select Case targetSymbolKind
                    Case SymbolKind.Method
                        Dim method = DirectCast(m, MethodSymbol)
                        Dim methodKind = method.MethodKind

                        ' Treat user-defined conversions and operators as ordinary methods for the purpose
                        ' of matching them here.
                        If methodKind = MethodKind.Conversion OrElse methodKind = MethodKind.UserDefinedOperator Then
                            methodKind = MethodKind.Ordinary
                        End If

                        If method.Arity <> descriptor.Arity OrElse methodKind <> targetMethodKind OrElse
                            ((descriptor.Flags And MemberFlags.Virtual) <> 0) <>
                            (method.IsOverridable OrElse method.IsOverrides OrElse method.IsMustOverride) Then
                            Continue For
                        End If

                        If Not comparer.MatchMethodSignature(method, descriptor.Signature) Then
                            Continue For
                        End If

                    Case SymbolKind.Property
                        Dim [property] = DirectCast(m, PropertySymbol)

                        If ((descriptor.Flags And MemberFlags.Virtual) <> 0) <>
                            ([property].IsOverridable OrElse [property].IsOverrides OrElse [property].IsMustOverride) Then
                            Continue For
                        End If

                        If Not comparer.MatchPropertySignature([property], descriptor.Signature) Then
                            Continue For
                        End If

                    Case SymbolKind.Field
                        If Not comparer.MatchFieldSignature(DirectCast(m, FieldSymbol), descriptor.Signature) Then
                            Continue For
                        End If

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(targetSymbolKind)

                End Select

                If result IsNot Nothing Then
                    ' ambiguity
                    result = Nothing
                    Exit For
                Else
                    result = m
                End If
            Next

            Return result
        End Function

        Friend Class SpecialMembersSignatureComparer
            Inherits SignatureComparer(Of MethodSymbol, FieldSymbol, PropertySymbol, TypeSymbol, ParameterSymbol)

            Public Shared ReadOnly Instance As New SpecialMembersSignatureComparer()

            Protected Sub New()
            End Sub

            Protected Overrides Function GetMDArrayElementType(type As TypeSymbol) As TypeSymbol
                If type.Kind <> SymbolKind.ArrayType Then
                    Return Nothing
                End If

                Dim array = DirectCast(type, ArrayTypeSymbol)

                If array.IsSZArray Then
                    Return Nothing ' This is not a multidimensional array
                End If

                Return array.ElementType
            End Function

            Protected Overrides Function MatchArrayRank(type As TypeSymbol, countOfDimensions As Integer) As Boolean
                If countOfDimensions = 1 Then
                    Return Nothing ' This must be a multidimensional array
                End If

                If type.Kind <> SymbolKind.ArrayType Then
                    Return Nothing
                End If

                Dim array = DirectCast(type, ArrayTypeSymbol)

                Return array.Rank = countOfDimensions
            End Function

            Protected Overrides Function GetSZArrayElementType(type As TypeSymbol) As TypeSymbol
                If type.Kind <> SymbolKind.ArrayType Then
                    Return Nothing
                End If

                Dim array = DirectCast(type, ArrayTypeSymbol)

                If Not array.IsSZArray Then
                    Return Nothing ' This is a multidimensional array
                End If

                Return array.ElementType
            End Function

            Protected Overrides Function GetFieldType(field As FieldSymbol) As TypeSymbol
                Return field.Type
            End Function

            Protected Overrides Function GetPropertyType(prop As PropertySymbol) As TypeSymbol
                Return prop.Type
            End Function

            Protected Overrides Function GetGenericTypeArgument(ByVal type As TypeSymbol, ByVal argumentIndex As Integer) As TypeSymbol
                If type.Kind <> SymbolKind.NamedType Then
                    Return Nothing
                End If

                Dim named = DirectCast(type, NamedTypeSymbol)

                If named.Arity <= argumentIndex Then
                    Return Nothing
                End If

                'We don't support nested types at the moment
                If named.ContainingType IsNot Nothing Then
                    Return Nothing
                End If

                Return named.TypeArgumentsNoUseSiteDiagnostics(argumentIndex)
            End Function

            Protected Overrides Function GetGenericTypeDefinition(type As TypeSymbol) As TypeSymbol
                If type.Kind <> SymbolKind.NamedType Then
                    Return Nothing
                End If

                Dim named = DirectCast(type, NamedTypeSymbol)

                'We don't support nested types at the moment
                If named.ContainingType IsNot Nothing Then
                    Return Nothing
                End If

                If named.Arity = 0 Then
                    Return Nothing '  Not generic
                End If

                Return named.OriginalDefinition
            End Function

            Protected Overrides Function GetParameters(ByVal method As MethodSymbol) As ImmutableArray(Of ParameterSymbol)
                Return method.Parameters
            End Function

            Protected Overrides Function GetParameters(ByVal [property] As PropertySymbol) As ImmutableArray(Of ParameterSymbol)
                Return [property].Parameters
            End Function

            Protected Overrides Function GetParamType(ByVal parameter As ParameterSymbol) As TypeSymbol
                Return parameter.Type
            End Function

            Protected Overrides Function GetPointedToType(type As TypeSymbol) As TypeSymbol
                Return Nothing ' Do not support pointers
            End Function

            Protected Overrides Function GetReturnType(method As MethodSymbol) As TypeSymbol
                Return method.ReturnType
            End Function

            Protected Overrides Function IsByRefParam(ByVal parameter As ParameterSymbol) As Boolean
                Return parameter.IsByRef
            End Function

            Protected Overrides Function IsByRefMethod(ByVal method As MethodSymbol) As Boolean
                Return method.ReturnsByRef
            End Function

            Protected Overrides Function IsByRefProperty(ByVal [property] As PropertySymbol) As Boolean
                Return [property].ReturnsByRef
            End Function

            Protected Overrides Function IsGenericMethodTypeParam(type As TypeSymbol, paramPosition As Integer) As Boolean
                If type.Kind <> SymbolKind.TypeParameter Then
                    Return False
                End If

                Dim typeParam = DirectCast(type, TypeParameterSymbol)

                If typeParam.ContainingSymbol.Kind <> SymbolKind.Method Then
                    Return False
                End If

                Return typeParam.Ordinal = paramPosition
            End Function

            Protected Overrides Function IsGenericTypeParam(type As TypeSymbol, paramPosition As Integer) As Boolean
                If type.Kind <> SymbolKind.TypeParameter Then
                    Return False
                End If

                Dim typeParam = DirectCast(type, TypeParameterSymbol)

                If typeParam.ContainingSymbol.Kind <> SymbolKind.NamedType Then
                    Return False
                End If

                Return typeParam.Ordinal = paramPosition
            End Function

            Protected Overrides Function MatchTypeToTypeId(type As TypeSymbol, typeId As Integer) As Boolean
                Return type.SpecialType = typeId
            End Function
        End Class

        Private Class WellKnownMembersSignatureComparer
            Inherits SpecialMembersSignatureComparer

            Private ReadOnly _compilation As VisualBasicCompilation

            Public Sub New(compilation As VisualBasicCompilation)
                _compilation = compilation
            End Sub

            Protected Overrides Function MatchTypeToTypeId(type As TypeSymbol, typeId As Integer) As Boolean
                Dim wellKnownId = CType(typeId, WellKnownType)
                If wellKnownId.IsWellKnownType() Then
                    Return type Is _compilation.GetWellKnownType(wellKnownId)
                End If

                Return MyBase.MatchTypeToTypeId(type, typeId)
            End Function
        End Class

        Friend Function SynthesizeTupleNamesAttribute(type As TypeSymbol) As SynthesizedAttributeData
            Debug.Assert(type IsNot Nothing)
            Debug.Assert(type.ContainsTuple())

            Dim stringType = GetSpecialType(SpecialType.System_String)
            Debug.Assert(stringType IsNot Nothing)
            Dim names As ImmutableArray(Of TypedConstant) = TupleNamesEncoder.Encode(type, stringType)

            Debug.Assert(Not names.IsDefault, "should not need the attribute when no tuple names")

            Dim stringArray = ArrayTypeSymbol.CreateSZArray(stringType, ImmutableArray(Of CustomModifier).Empty, stringType.ContainingAssembly)
            Dim args = ImmutableArray.Create(New TypedConstant(stringArray, names))
            Return TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_TupleElementNamesAttribute__ctorTransformNames, args)
        End Function

        Friend Class TupleNamesEncoder
            Public Shared Function Encode(type As TypeSymbol) As ImmutableArray(Of String)
                Dim namesBuilder = ArrayBuilder(Of String).GetInstance()

                If Not TryGetNames(type, namesBuilder) Then
                    namesBuilder.Free()
                    Return Nothing
                End If

                Return namesBuilder.ToImmutableAndFree()
            End Function

            Public Shared Function Encode(type As TypeSymbol, stringType As TypeSymbol) As ImmutableArray(Of TypedConstant)
                Dim namesBuilder = ArrayBuilder(Of String).GetInstance()

                If Not TryGetNames(type, namesBuilder) Then
                    namesBuilder.Free()
                    Return Nothing
                End If

                Dim names = namesBuilder.SelectAsArray(Function(name, constantType)
                                                           Return New TypedConstant(constantType, TypedConstantKind.Primitive, name)
                                                       End Function,
                                                       stringType)

                namesBuilder.Free()
                Return names
            End Function

            Friend Shared Function TryGetNames(type As TypeSymbol, namesBuilder As ArrayBuilder(Of String)) As Boolean
                type.VisitType(Function(t As TypeSymbol, builder As ArrayBuilder(Of String)) AddNames(t, builder), namesBuilder)
                Return namesBuilder.Any(Function(name) name IsNot Nothing)
            End Function

            Private Shared Function AddNames(type As TypeSymbol, namesBuilder As ArrayBuilder(Of String)) As Boolean
                If type.IsTupleType Then
                    If type.TupleElementNames.IsDefaultOrEmpty Then
                        ' If none of the tuple elements have names, put
                        ' null placeholders in.
                        namesBuilder.AddMany(Nothing, type.TupleElementTypes.Length)
                    Else
                        namesBuilder.AddRange(type.TupleElementNames)
                    End If
                End If
                ' Always recur into nested types
                Return False
            End Function
        End Class
    End Class

End Namespace
