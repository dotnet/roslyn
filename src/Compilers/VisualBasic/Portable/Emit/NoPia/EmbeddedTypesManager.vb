' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports System.Collections.Concurrent
Imports System.Threading
Imports ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer

#If Not DEBUG Then
Imports SymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbol
Imports NamedTypeSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.NamedTypeSymbol
Imports FieldSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.FieldSymbol
Imports MethodSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSymbol
Imports EventSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.EventSymbol
Imports PropertySymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.PropertySymbol
Imports ParameterSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.ParameterSymbol
Imports TypeParameterSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeParameterSymbol
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedTypesManager
        Inherits Microsoft.CodeAnalysis.Emit.NoPia.EmbeddedTypesManager(Of PEModuleBuilder, ModuleCompilationState, EmbeddedTypesManager, SyntaxNode, VisualBasicAttributeData,
                                                                           SymbolAdapter, AssemblySymbol, NamedTypeSymbolAdapter, FieldSymbolAdapter, MethodSymbolAdapter, EventSymbolAdapter, PropertySymbolAdapter, ParameterSymbolAdapter, TypeParameterSymbolAdapter,
                                                                           EmbeddedType, EmbeddedField, EmbeddedMethod, EmbeddedEvent, EmbeddedProperty, EmbeddedParameter, EmbeddedTypeParameter)

        Private ReadOnly _assemblyGuidMap As New ConcurrentDictionary(Of AssemblySymbol, String)(ReferenceEqualityComparer.Instance)
        Private ReadOnly _reportedSymbolsMap As New ConcurrentDictionary(Of Symbol, Boolean)(ReferenceEqualityComparer.Instance)
        Private _lazySystemStringType As NamedTypeSymbol = ErrorTypeSymbol.UnknownResultType
        Private ReadOnly _lazyWellKnownTypeMethods As MethodSymbol()

        Public Sub New(moduleBeingBuilt As PEModuleBuilder)
            MyBase.New(moduleBeingBuilt)

            _lazyWellKnownTypeMethods = New MethodSymbol(WellKnownMember.Count - 1) {}
            For i = 0 To WellKnownMember.Count - 1
                _lazyWellKnownTypeMethods(i) = ErrorMethodSymbol.UnknownMethod
            Next
        End Sub

        Public Function GetSystemStringType(syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As NamedTypeSymbol
            If _lazySystemStringType Is ErrorTypeSymbol.UnknownResultType Then
                Dim type = ModuleBeingBuilt.Compilation.GetSpecialType(SpecialType.System_String)
                Dim info = type.GetUseSiteInfo()

                If type.IsErrorType() Then
                    type = Nothing
                End If

                If TypeSymbol.Equals(Interlocked.CompareExchange(Of NamedTypeSymbol)(_lazySystemStringType, type, ErrorTypeSymbol.UnknownResultType), ErrorTypeSymbol.UnknownResultType, TypeCompareKind.ConsiderEverything) Then
                    If info.DiagnosticInfo IsNot Nothing Then
                        ReportDiagnostic(diagnostics, syntaxNodeOpt, info.DiagnosticInfo)
                    End If
                End If
            End If

            Return _lazySystemStringType
        End Function

        Public Function GetWellKnownMethod(method As WellKnownMember, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As MethodSymbol
            Return LazyGetWellKnownTypeMethod(_lazyWellKnownTypeMethods(CInt(method)), method, syntaxNodeOpt, diagnostics)
        End Function

        Private Function LazyGetWellKnownTypeMethod(ByRef lazyMethod As MethodSymbol, method As WellKnownMember, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As MethodSymbol
            If lazyMethod Is ErrorMethodSymbol.UnknownMethod Then
                Dim info As UseSiteInfo(Of AssemblySymbol) = Nothing
                Dim symbol = DirectCast(Binder.GetWellKnownTypeMember(ModuleBeingBuilt.Compilation, method, info), MethodSymbol)

                Debug.Assert(info.DiagnosticInfo Is Nothing OrElse symbol Is Nothing)

                If Interlocked.CompareExchange(Of MethodSymbol)(lazyMethod, symbol, ErrorMethodSymbol.UnknownMethod) = ErrorMethodSymbol.UnknownMethod Then
                    If info.DiagnosticInfo IsNot Nothing Then
                        ReportDiagnostic(diagnostics, syntaxNodeOpt, info.DiagnosticInfo)
                    End If
                End If
            End If

            Return lazyMethod
        End Function

        Friend Overrides Function GetTargetAttributeSignatureIndex(attrData As VisualBasicAttributeData, description As AttributeDescription) As Integer
            Return attrData.GetTargetAttributeSignatureIndex(description)
        End Function

        Friend Overrides Function CreateSynthesizedAttribute(constructor As WellKnownMember, constructorArguments As ImmutableArray(Of TypedConstant), namedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant)), syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As VisualBasicAttributeData
            Dim ctor = GetWellKnownMethod(constructor, syntaxNodeOpt, diagnostics)
            If ctor Is Nothing Then
                Return Nothing
            End If

            Select Case constructor
                Case WellKnownMember.System_Runtime_InteropServices_ComEventInterfaceAttribute__ctor
                    ' When emitting a com event interface, we have to tweak the parameters: the spec requires that we use
                    ' the original source interface as both source interface and event provider. Otherwise, we'd have to embed
                    ' the event provider class too.
                    Return New SynthesizedAttributeData(ModuleBeingBuilt.Compilation, ctor,
                        ImmutableArray.Create(constructorArguments(0), constructorArguments(0)),
                        ImmutableArray(Of KeyValuePair(Of String, TypedConstant)).Empty)

                Case WellKnownMember.System_Runtime_InteropServices_CoClassAttribute__ctor
                    ' The interface needs to have a coclass attribute so that we can tell at runtime that it should be
                    ' instantiatable. The attribute cannot refer directly to the coclass, however, because we can't embed
                    ' classes, and we can't emit a reference to the PIA. We don't actually need
                    ' the class name at runtime: we will instead emit a reference to System.Object, as a placeholder.
                    Return New SynthesizedAttributeData(ModuleBeingBuilt.Compilation, ctor,
                        ImmutableArray.Create(New TypedConstant(ctor.Parameters(0).Type, TypedConstantKind.Type, ctor.ContainingAssembly.GetSpecialType(SpecialType.System_Object))),
                        ImmutableArray(Of KeyValuePair(Of String, TypedConstant)).Empty)

                Case Else
                    Return New SynthesizedAttributeData(ModuleBeingBuilt.Compilation, ctor, constructorArguments, namedArguments)

            End Select
        End Function

        Friend Overrides Function TryGetAttributeArguments(attrData As VisualBasicAttributeData, ByRef constructorArguments As ImmutableArray(Of TypedConstant), ByRef namedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant)), syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As Boolean
            Dim result As Boolean = Not attrData.HasErrors

            constructorArguments = attrData.CommonConstructorArguments
            namedArguments = attrData.CommonNamedArguments

            Dim errorInfo As DiagnosticInfo = attrData.ErrorInfo
            If errorInfo IsNot Nothing Then
                diagnostics.Add(errorInfo, If(syntaxNodeOpt?.Location, NoLocation.Singleton))
            End If

            Return result
        End Function

        Friend Function GetAssemblyGuidString(assembly As AssemblySymbol) As String
            Debug.Assert(Not IsFrozen) ' After we freeze the set of types, we might add additional assemblies into this map without actual guid values.

            Dim guidString As String = Nothing
            If _assemblyGuidMap.TryGetValue(assembly, guidString) Then
                Return guidString
            End If

            Debug.Assert(guidString Is Nothing)
            assembly.GetGuidString(guidString)
            Return _assemblyGuidMap.GetOrAdd(assembly, guidString)
        End Function

        Protected Overrides Sub OnGetTypesCompleted(types As ImmutableArray(Of EmbeddedType), diagnostics As DiagnosticBag)
            For Each t In types
                ' Note, once we reached this point we are no longer interested in guid values, using null.
                _assemblyGuidMap.TryAdd(t.UnderlyingNamedType.AdaptedNamedTypeSymbol.ContainingAssembly, Nothing)
            Next

            For Each a In ModuleBeingBuilt.GetReferencedAssembliesUsedSoFar()
                ReportIndirectReferencesToLinkedAssemblies(a, diagnostics)
            Next
        End Sub

        Protected Overrides Sub ReportNameCollisionBetweenEmbeddedTypes(typeA As EmbeddedType, typeB As EmbeddedType, diagnostics As DiagnosticBag)
            Dim underlyingTypeA = typeA.UnderlyingNamedType.AdaptedNamedTypeSymbol
            Dim underlyingTypeB = typeB.UnderlyingNamedType.AdaptedNamedTypeSymbol
            ReportDiagnostic(diagnostics,
                ERRID.ERR_DuplicateLocalTypes3,
                Nothing,
                underlyingTypeA,
                underlyingTypeA.ContainingAssembly,
                underlyingTypeB.ContainingAssembly)
        End Sub

        Protected Overrides Sub ReportNameCollisionWithAlreadyDeclaredType(type As EmbeddedType, diagnostics As DiagnosticBag)
            Dim underlyingType = type.UnderlyingNamedType.AdaptedNamedTypeSymbol
            ReportDiagnostic(diagnostics,
                ERRID.ERR_LocalTypeNameClash2,
                Nothing,
                underlyingType,
                underlyingType.ContainingAssembly)
        End Sub

        Friend Overrides Sub ReportIndirectReferencesToLinkedAssemblies(assembly As AssemblySymbol, diagnostics As DiagnosticBag)
            Debug.Assert(IsFrozen)

            ' We are emitting an assembly, A, which /references some assembly, B, and
            ' /links some other assembly, C, so that it can use C's types (by embedding them)
            ' without having an assemblyref to C itself.
            ' We can say that A has an indirect reference to each assembly that B references.
            ' In this function, we are looking for the situation where B has an assemblyref to C,
            ' thus giving A an indirect reference to C. If so, we will report a warning.

            For Each [module] In assembly.Modules
                For Each indirectRef In [module].GetReferencedAssemblySymbols()
                    If Not indirectRef.IsMissing AndAlso indirectRef.IsLinked AndAlso _assemblyGuidMap.ContainsKey(indirectRef) Then
                        ' WRNID_IndirectRefToLinkedAssembly2/WRN_ReferencedAssemblyReferencesLinkedPIA
                        ReportDiagnostic(diagnostics, ERRID.WRN_IndirectRefToLinkedAssembly2, Nothing, indirectRef, assembly)
                    End If
                Next
            Next
        End Sub

        ''' <summary>
        ''' Returns true if the type can be embedded. If the type is defined in a linked (/l-ed)
        ''' assembly, but doesn't meet embeddable type requirements, this function returns
        ''' False and reports appropriate diagnostics.
        ''' </summary>
        Friend Shared Function IsValidEmbeddableType(
            type As NamedTypeSymbol,
            syntaxNodeOpt As SyntaxNode,
            diagnostics As DiagnosticBag,
            Optional typeManagerOpt As EmbeddedTypesManager = Nothing
        ) As Boolean

            ' We do not embed SpecialTypes (they must be defined in Core assembly),
            ' error types and types from assemblies that aren't linked.
            If type.SpecialType <> SpecialType.None OrElse
                type.IsErrorType() OrElse
                Not type.ContainingAssembly.IsLinked Then

                ' Assuming that we already complained about an error type,
                ' no additional diagnostics necessary.
                Return False
            End If

            Dim id = ERRID.ERR_None

            Select Case type.TypeKind
                Case TypeKind.Interface
                    For Each member As Symbol In type.GetMembersUnordered()
                        If member.Kind <> SymbolKind.NamedType Then
                            If Not member.IsMustOverride Then
                                id = ERRID.ERR_DefaultInterfaceImplementationInNoPIAType
                            ElseIf member.IsNotOverridable Then
                                id = ERRID.ERR_ReAbstractionInNoPIAType
                            End If
                        End If
                    Next

                    If id = ERRID.ERR_None Then
                        GoTo checksForAllEmbedabbleTypes
                    End If

                Case TypeKind.Structure,
                    TypeKind.Enum,
                    TypeKind.Delegate
checksForAllEmbedabbleTypes:
                    If type.IsTupleType Then
                        type = type.TupleUnderlyingType
                    End If

                    If type.ContainingType IsNot Nothing Then
                        ' We do not support nesting for embedded types.
                        ' ERRID.ERR_InvalidInteropType/ERR_NoPIANestedType
                        id = ERRID.ERR_NestedInteropType
                    ElseIf type.IsGenericType Then
                        ' We do not support generic embedded types.
                        ' ERRID.ERR_CannotEmbedInterfaceWithGeneric/ERR_GenericsUsedInNoPIAType
                        id = ERRID.ERR_CannotEmbedInterfaceWithGeneric
                    End If

                Case Else
                    ' ERRID.ERR_CannotLinkClassWithNoPIA1/ERR_NewCoClassOnLink
                    Debug.Assert(type.TypeKind = TypeKind.Class OrElse type.TypeKind = TypeKind.Module)
                    id = ERRID.ERR_CannotLinkClassWithNoPIA1
            End Select

            If id <> ERRID.ERR_None Then
                ReportNotEmbeddableSymbol(id, type, syntaxNodeOpt, diagnostics, typeManagerOpt)
                Return False
            End If

            Return True

        End Function

        Private Sub VerifyNotFrozen()
            Debug.Assert(Not IsFrozen)
            If IsFrozen Then
                Throw ExceptionUtilities.UnexpectedValue(IsFrozen)
            End If
        End Sub

        Private Shared Sub ReportNotEmbeddableSymbol(id As ERRID, symbol As Symbol, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag, typeManagerOpt As EmbeddedTypesManager)
            ' Avoid reporting multiple errors for the symbol.
            If typeManagerOpt Is Nothing OrElse
                typeManagerOpt._reportedSymbolsMap.TryAdd(symbol.OriginalDefinition, True) Then

                ReportDiagnostic(diagnostics, id, syntaxNodeOpt, symbol.OriginalDefinition)
            End If
        End Sub

        Friend Shared Sub ReportDiagnostic(diagnostics As DiagnosticBag, id As ERRID, syntaxNodeOpt As SyntaxNode, ParamArray args As Object())
            ReportDiagnostic(diagnostics, syntaxNodeOpt, ErrorFactory.ErrorInfo(id, args))
        End Sub

        Private Shared Sub ReportDiagnostic(diagnostics As DiagnosticBag, syntaxNodeOpt As SyntaxNode, info As DiagnosticInfo)
            diagnostics.Add(New VBDiagnostic(info, If(syntaxNodeOpt Is Nothing, NoLocation.Singleton, syntaxNodeOpt.GetLocation())))
        End Sub

        Friend Function EmbedTypeIfNeedTo(namedType As NamedTypeSymbol, fromImplements As Boolean, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As Cci.INamedTypeReference
            Debug.Assert(namedType.IsDefinition)
            Debug.Assert(ModuleBeingBuilt.SourceModule.AnyReferencedAssembliesAreLinked)

            If IsValidEmbeddableType(namedType, syntaxNodeOpt, diagnostics, Me) Then
                Return EmbedType(namedType, fromImplements, syntaxNodeOpt, diagnostics)
            End If

            Return Nothing
        End Function

        Private Function EmbedType(namedType As NamedTypeSymbol, fromImplements As Boolean, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As EmbeddedType
            Debug.Assert(namedType.IsDefinition)

            Dim adapter = namedType.GetCciAdapter()
            Dim embedded = New EmbeddedType(Me, adapter)
            Dim cached = EmbeddedTypesMap.GetOrAdd(adapter, embedded)

            Dim isInterface = (namedType.IsInterface)

            If isInterface AndAlso fromImplements Then
                ' Note, we must use 'cached' here because we might drop 'embedded' below.
                cached.EmbedAllMembersOfImplementedInterface(syntaxNodeOpt, diagnostics)
            End If

            If embedded IsNot cached Then
                Return cached
            End If

            ' We do not expect this method to be called on a different thread once GetTypes is called.
            VerifyNotFrozen()

            Dim noPiaIndexer = New Cci.TypeReferenceIndexer(New EmitContext(ModuleBeingBuilt, syntaxNodeOpt, diagnostics, metadataOnly:=False, includePrivateMembers:=True))

            ' Make sure we embed all types referenced by the type declaration: implemented interfaces, etc.
            noPiaIndexer.VisitTypeDefinitionNoMembers(embedded)

            If Not isInterface Then
                Debug.Assert(namedType.TypeKind = TypeKind.Structure OrElse
                             namedType.TypeKind = TypeKind.Enum OrElse
                             namedType.TypeKind = TypeKind.Delegate)

                ' For structures, enums and delegates we embed all members.
                If namedType.TypeKind = TypeKind.Structure OrElse namedType.TypeKind = TypeKind.Enum Then
                    ' TODO: When building debug versions in the IDE, the compiler will insert some extra members
                    ' that support ENC. These make no sense in local types, so we will skip them. We have to
                    ' check for them explicitly or they will trip the member-validity check that follows.
                End If

                For Each f In namedType.GetFieldsToEmit()
                    EmbedField(embedded, f.GetCciAdapter(), syntaxNodeOpt, diagnostics)
                Next

                For Each m In namedType.GetMethodsToEmit()
                    EmbedMethod(embedded, m.GetCciAdapter(), syntaxNodeOpt, diagnostics)
                Next

                ' We also should embed properties and events, but we don't need to do this explicitly here
                ' because accessors embed them automatically.
            End If

            Return embedded
        End Function

        Friend Overrides Function EmbedField(
            type As EmbeddedType,
            field As FieldSymbolAdapter,
            syntaxNodeOpt As SyntaxNode,
            diagnostics As DiagnosticBag
        ) As EmbeddedField

            Debug.Assert(field.AdaptedFieldSymbol.IsDefinition)

            Dim embedded = New EmbeddedField(type, field)
            Dim cached = EmbeddedFieldsMap.GetOrAdd(field, embedded)

            If embedded IsNot cached Then
                Return cached
            End If

            ' We do not expect this method to be called on a different thread once GetTypes is called.
            VerifyNotFrozen()

            ' Embed types referenced by this field declaration.
            EmbedReferences(embedded, syntaxNodeOpt, diagnostics)

            Dim containerKind = field.AdaptedFieldSymbol.ContainingType.TypeKind

            ' Structures may contain only public instance fields.
            If containerKind = TypeKind.Interface OrElse
                containerKind = TypeKind.Delegate OrElse
                (containerKind = TypeKind.Structure AndAlso (field.AdaptedFieldSymbol.IsShared OrElse field.AdaptedFieldSymbol.DeclaredAccessibility <> Accessibility.Public)) Then
                ' ERRID.ERR_InvalidStructMemberNoPIA1/ERR_InteropStructContainsMethods
                ReportNotEmbeddableSymbol(ERRID.ERR_InvalidStructMemberNoPIA1, type.UnderlyingNamedType.AdaptedNamedTypeSymbol, syntaxNodeOpt, diagnostics, Me)
            End If

            Return embedded
        End Function

        Friend Overrides Function EmbedMethod(
            type As EmbeddedType,
            method As MethodSymbolAdapter,
            syntaxNodeOpt As SyntaxNode,
            diagnostics As DiagnosticBag
        ) As EmbeddedMethod

            Debug.Assert(method.AdaptedMethodSymbol.IsDefinition)
            Debug.Assert(Not method.AdaptedMethodSymbol.IsDefaultValueTypeConstructor())

            Dim embedded = New EmbeddedMethod(type, method)
            Dim cached = EmbeddedMethodsMap.GetOrAdd(method, embedded)

            If embedded IsNot cached Then
                Return cached
            End If

            ' We do not expect this method to be called on a different thread once GetTypes is called.
            VerifyNotFrozen()

            ' Embed types referenced by this method declaration.
            EmbedReferences(embedded, syntaxNodeOpt, diagnostics)

            Select Case type.UnderlyingNamedType.AdaptedNamedTypeSymbol.TypeKind
                Case TypeKind.Structure, TypeKind.Enum
                    ' ERRID.ERR_InvalidStructMemberNoPIA1/ERR_InteropStructContainsMethods
                    ReportNotEmbeddableSymbol(ERRID.ERR_InvalidStructMemberNoPIA1, type.UnderlyingNamedType.AdaptedNamedTypeSymbol, syntaxNodeOpt, diagnostics, Me)
                Case Else
                    If embedded.HasBody Then
                        ' ERRID.ERR_InteropMethodWithBody1/ERR_InteropMethodWithBody
                        ReportDiagnostic(diagnostics, ERRID.ERR_InteropMethodWithBody1, syntaxNodeOpt, method.AdaptedMethodSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))
                    End If
            End Select

            ' If this proc happens to belong to a property/event, we should include the property/event as well.
            Dim propertyOrEvent = method.AdaptedMethodSymbol.AssociatedSymbol
            If propertyOrEvent IsNot Nothing Then
                Select Case propertyOrEvent.Kind
                    Case SymbolKind.Property
                        EmbedProperty(type, DirectCast(propertyOrEvent, PropertySymbol).GetCciAdapter(), syntaxNodeOpt, diagnostics)
                    Case SymbolKind.Event
                        EmbedEvent(type, DirectCast(propertyOrEvent, EventSymbol).GetCciAdapter(), syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding:=False)
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(propertyOrEvent.Kind)
                End Select
            End If

            Return embedded
        End Function

        Friend Overrides Function EmbedProperty(
            type As EmbeddedType,
            [property] As PropertySymbolAdapter,
            syntaxNodeOpt As SyntaxNode,
            diagnostics As DiagnosticBag
        ) As EmbeddedProperty

            Debug.Assert([property].AdaptedPropertySymbol.IsDefinition)

            ' Make sure accessors are embedded.
            Dim getMethod = [property].AdaptedPropertySymbol.GetMethod
            Dim setMethod = [property].AdaptedPropertySymbol.SetMethod

            Dim embeddedGet = If(getMethod IsNot Nothing, EmbedMethod(type, getMethod.GetCciAdapter(), syntaxNodeOpt, diagnostics), Nothing)
            Dim embeddedSet = If(setMethod IsNot Nothing, EmbedMethod(type, setMethod.GetCciAdapter(), syntaxNodeOpt, diagnostics), Nothing)

            Dim embedded = New EmbeddedProperty([property], embeddedGet, embeddedSet)
            Dim cached = EmbeddedPropertiesMap.GetOrAdd([property], embedded)

            If embedded IsNot cached Then
                Return cached
            End If

            ' We do not expect this method to be called on a different thread once GetTypes is called.
            VerifyNotFrozen()

            ' Embed types referenced by this property declaration.
            ' This should also embed accessors.
            EmbedReferences(embedded, syntaxNodeOpt, diagnostics)

            Return embedded
        End Function

        Friend Overrides Function EmbedEvent(
            type As EmbeddedType,
            [event] As EventSymbolAdapter,
            syntaxNodeOpt As SyntaxNode,
            diagnostics As DiagnosticBag,
            isUsedForComAwareEventBinding As Boolean
        ) As EmbeddedEvent

            Debug.Assert([event].AdaptedEventSymbol.IsDefinition)

            ' Make sure accessors are embedded.
            Dim addMethod = [event].AdaptedEventSymbol.AddMethod
            Dim removeMethod = [event].AdaptedEventSymbol.RemoveMethod
            Dim callMethod = [event].AdaptedEventSymbol.RaiseMethod

            Dim embeddedAdd = If(addMethod IsNot Nothing, EmbedMethod(type, addMethod.GetCciAdapter(), syntaxNodeOpt, diagnostics), Nothing)
            Dim embeddedRemove = If(removeMethod IsNot Nothing, EmbedMethod(type, removeMethod.GetCciAdapter(), syntaxNodeOpt, diagnostics), Nothing)
            Dim embeddedCall = If(callMethod IsNot Nothing, EmbedMethod(type, callMethod.GetCciAdapter(), syntaxNodeOpt, diagnostics), Nothing)

            Dim embedded = New EmbeddedEvent([event], embeddedAdd, embeddedRemove, embeddedCall)
            Dim cached = EmbeddedEventsMap.GetOrAdd([event], embedded)

            If embedded IsNot cached Then
                If isUsedForComAwareEventBinding Then
                    cached.EmbedCorrespondingComEventInterfaceMethod(syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding)
                End If
                Return cached
            End If

            ' We do not expect this method to be called on a different thread once GetTypes is called.
            VerifyNotFrozen()

            ' Embed types referenced by this event declaration.
            ' This should also embed accessors.
            EmbedReferences(embedded, syntaxNodeOpt, diagnostics)

            embedded.EmbedCorrespondingComEventInterfaceMethod(syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding)

            Return embedded
        End Function

        Protected Overrides Function GetEmbeddedTypeForMember(member As SymbolAdapter, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As EmbeddedType
            Debug.Assert(member.AdaptedSymbol.IsDefinition)
            Debug.Assert(ModuleBeingBuilt.SourceModule.AnyReferencedAssembliesAreLinked)

            Dim namedType = member.AdaptedSymbol.ContainingType

            If IsValidEmbeddableType(namedType, syntaxNodeOpt, diagnostics, Me) Then
                ' It is possible that we have found a reference to a member before
                ' encountering a reference to its container; make sure the container gets included.
                Return EmbedType(namedType, fromImplements:=False, syntaxNodeOpt:=syntaxNodeOpt, diagnostics:=diagnostics)
            End If

            Return Nothing
        End Function

        Friend Shared Function EmbedParameters(containingPropertyOrMethod As CommonEmbeddedMember, underlyingParameters As ImmutableArray(Of ParameterSymbol)) As ImmutableArray(Of EmbeddedParameter)
            Return underlyingParameters.SelectAsArray(Function(parameter, container) New EmbeddedParameter(container, parameter.GetCciAdapter()), containingPropertyOrMethod)
        End Function

        Protected Overrides Function CreateCompilerGeneratedAttribute() As VisualBasicAttributeData
            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
            Dim compilation = ModuleBeingBuilt.Compilation
            Return compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor)
        End Function

    End Class

End Namespace
