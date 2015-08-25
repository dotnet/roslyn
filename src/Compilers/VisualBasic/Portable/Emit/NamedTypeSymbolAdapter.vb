' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Partial Class NamedTypeSymbol
        Implements ITypeReference
        Implements ITypeDefinition
        Implements INamedTypeReference
        Implements INamedTypeDefinition
        Implements INamespaceTypeReference
        Implements INamespaceTypeDefinition
        Implements INestedTypeReference
        Implements INestedTypeDefinition
        Implements IGenericTypeInstanceReference
        Implements ISpecializedNestedTypeReference

        Private ReadOnly Property ITypeReferenceIsEnum As Boolean Implements ITypeReference.IsEnum
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                Return Me.TypeKind = TYPEKIND.Enum
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceIsValueType As Boolean Implements ITypeReference.IsValueType
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                Return Me.IsValueType
            End Get
        End Property

        Private Function ITypeReferenceGetResolvedType(context As EmitContext) As ITypeDefinition Implements ITypeReference.GetResolvedType
            Debug.Assert(Not Me.IsAnonymousType)
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Return AsTypeDefinitionImpl(moduleBeingBuilt)
        End Function

        Private Function ITypeReferenceTypeCode(context As EmitContext) As Cci.PrimitiveTypeCode Implements ITypeReference.TypeCode
            Debug.Assert(Not Me.IsAnonymousType)
            Debug.Assert(Me.IsDefinitionOrDistinct())
            If Me.IsDefinition Then
                Return Me.PrimitiveTypeCode
            End If
            Return Cci.PrimitiveTypeCode.NotPrimitive
        End Function

        Private ReadOnly Property ITypeReferenceTypeDef As TypeDefinitionHandle Implements ITypeReference.TypeDef
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                Dim peNamedType As PENamedTypeSymbol = TryCast(Me, PENamedTypeSymbol)
                If peNamedType IsNot Nothing Then
                    Return peNamedType.Handle
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericMethodParameterReference As IGenericMethodParameterReference Implements ITypeReference.AsGenericMethodParameterReference
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericTypeInstanceReference As IGenericTypeInstanceReference Implements ITypeReference.AsGenericTypeInstanceReference
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                Debug.Assert(Me.IsDefinitionOrDistinct())
                If Not Me.IsDefinition AndAlso Me.Arity > 0 AndAlso Me.ConstructedFrom IsNot Me Then
                    Return Me
                End If
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericTypeParameterReference As IGenericTypeParameterReference Implements ITypeReference.AsGenericTypeParameterReference
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsNamespaceTypeReference As INamespaceTypeReference Implements ITypeReference.AsNamespaceTypeReference
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                Debug.Assert(Me.IsDefinitionOrDistinct())
                If Me.IsDefinition AndAlso Me.ContainingType Is Nothing Then
                    Return Me
                End If
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsNamespaceTypeDefinition(context As EmitContext) As INamespaceTypeDefinition Implements ITypeReference.AsNamespaceTypeDefinition
            Debug.Assert(Not Me.IsAnonymousType)
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Debug.Assert(Me.IsDefinitionOrDistinct())
            If Me.ContainingType Is Nothing AndAlso Me.IsDefinition AndAlso Me.ContainingModule.Equals(moduleBeingBuilt.SourceModule) Then
                Return Me
            End If
            Return Nothing
        End Function

        Private ReadOnly Property ITypeReferenceAsNestedTypeReference As INestedTypeReference Implements ITypeReference.AsNestedTypeReference
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                If Me.ContainingType IsNot Nothing Then
                    Return Me
                End If
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsNestedTypeDefinition(context As EmitContext) As INestedTypeDefinition Implements ITypeReference.AsNestedTypeDefinition
            Debug.Assert(Not Me.IsAnonymousType)
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Return AsNestedTypeDefinitionImpl(moduleBeingBuilt)
        End Function

        Private Function AsNestedTypeDefinitionImpl(moduleBeingBuilt As PEModuleBuilder) As INestedTypeDefinition
            Debug.Assert(Me.IsDefinitionOrDistinct())
            If Me.ContainingType IsNot Nothing AndAlso Me.IsDefinition AndAlso Me.ContainingModule.Equals(moduleBeingBuilt.SourceModule) Then
                Return Me
            End If
            Return Nothing
        End Function

        Private ReadOnly Property ITypeReferenceAsSpecializedNestedTypeReference As ISpecializedNestedTypeReference Implements ITypeReference.AsSpecializedNestedTypeReference
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                Debug.Assert(Me.IsDefinitionOrDistinct())
                If Not Me.IsDefinition AndAlso (Me.Arity = 0 OrElse Me.ConstructedFrom Is Me) Then
                    Debug.Assert(Me.ContainingType IsNot Nothing AndAlso Me.ContainingType.IsOrInGenericType())
                    Return Me
                End If
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsTypeDefinition(context As EmitContext) As ITypeDefinition Implements ITypeReference.AsTypeDefinition
            Debug.Assert(Not Me.IsAnonymousType)
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Return AsTypeDefinitionImpl(moduleBeingBuilt)
        End Function

        Private Function AsTypeDefinitionImpl(moduleBeingBuilt As PEModuleBuilder) As ITypeDefinition
            Debug.Assert(Me.IsDefinitionOrDistinct())

            ' Can't be generic instantiation
            ' must be declared in the module we are building
            If Me.IsDefinition AndAlso
                Me.ContainingModule.Equals(moduleBeingBuilt.SourceModule) Then
                Return Me
            End If
            Return Nothing
        End Function

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As MetadataVisitor) ' Implements IReference.Dispatch
            Debug.Assert(Me.IsDefinitionOrDistinct())

            If Not Me.IsDefinition Then
                If Me.Arity > 0 AndAlso Me.ConstructedFrom IsNot Me Then
                    Debug.Assert((DirectCast(Me, ITypeReference)).AsGenericTypeInstanceReference IsNot Nothing)
                    visitor.Visit(DirectCast(Me, IGenericTypeInstanceReference))
                Else
                    Debug.Assert((DirectCast(Me, ITypeReference)).AsSpecializedNestedTypeReference IsNot Nothing)
                    visitor.Visit(DirectCast(Me, ISpecializedNestedTypeReference))
                End If
            Else
                Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(visitor.Context.Module, PEModuleBuilder)
                Dim asDefinition As Boolean = (Me.ContainingModule.Equals(moduleBeingBuilt.SourceModule))
                If Me.ContainingType Is Nothing Then
                    If asDefinition Then
                        Debug.Assert((DirectCast(Me, ITypeReference)).AsNamespaceTypeDefinition(visitor.Context) IsNot Nothing)
                        visitor.Visit(DirectCast(Me, INamespaceTypeDefinition))
                    Else
                        Debug.Assert((DirectCast(Me, ITypeReference)).AsNamespaceTypeReference IsNot Nothing)
                        visitor.Visit(DirectCast(Me, INamespaceTypeReference))
                    End If
                Else
                    If asDefinition Then
                        Debug.Assert((DirectCast(Me, ITypeReference)).AsNestedTypeDefinition(visitor.Context) IsNot Nothing)
                        visitor.Visit(DirectCast(Me, INestedTypeDefinition))
                    Else
                        Debug.Assert((DirectCast(Me, ITypeReference)).AsNestedTypeReference IsNot Nothing)
                        visitor.Visit(DirectCast(Me, INestedTypeReference))
                    End If
                End If
            End If
        End Sub

        Friend NotOverridable Overrides Function IReferenceAsDefinition(context As EmitContext) As IDefinition ' Implements IReference.AsDefinition
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Return AsTypeDefinitionImpl(moduleBeingBuilt)
        End Function

        Private ReadOnly Property ITypeDefinitionAlignment As UShort Implements ITypeDefinition.Alignment
            Get
                CheckDefinitionInvariant()
                Dim layout = Me.Layout
                Return CUShort(layout.Alignment)
            End Get
        End Property

        Private Function ITypeDefinitionGetBaseClass(context As EmitContext) As ITypeReference Implements ITypeDefinition.GetBaseClass
            Debug.Assert(Not Me.IsAnonymousType)
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Debug.Assert((DirectCast(Me, ITypeReference)).AsTypeDefinition(context) IsNot Nothing)

            Dim baseType As NamedTypeSymbol = Me.BaseTypeNoUseSiteDiagnostics

            If Me.TypeKind = TypeKind.Submission Then
                ' although submission semantically doesn't have a base we need to emit one into metadata:
                Debug.Assert(baseType Is Nothing)
                baseType = Me.ContainingAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Object)
            End If

            If baseType IsNot Nothing Then
                Return moduleBeingBuilt.Translate(baseType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
            End If

            Return Nothing
        End Function

        Private ReadOnly Property ITypeDefinitionEvents As IEnumerable(Of IEventDefinition) Implements ITypeDefinition.Events
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()

                Return GetEventsToEmit()
            End Get
        End Property

        Friend Overridable Iterator Function GetEventsToEmit() As IEnumerable(Of EventSymbol)
            CheckDefinitionInvariant()

            For Each member In Me.GetMembersForCci()
                If member.Kind = SymbolKind.Event Then
                    Yield DirectCast(member, EventSymbol)
                End If
            Next
        End Function

        Private Function ITypeDefinitionGetExplicitImplementationOverrides(context As EmitContext) As IEnumerable(Of Cci.MethodImplementation) Implements ITypeDefinition.GetExplicitImplementationOverrides
            Debug.Assert(Not Me.IsAnonymousType)
            'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

            ' can't be generic instantiation
            ' must be declared in the module we are building
            CheckDefinitionInvariant()

            If Me.IsInterface Then
                Return SpecializedCollections.EmptyEnumerable(Of Cci.MethodImplementation)()
            End If

            Dim moduleBeingBuilt = DirectCast(context.Module, PEModuleBuilder)
            Dim sourceNamedType = TryCast(Me, SourceNamedTypeSymbol)
            Dim explicitImplements As ArrayBuilder(Of Cci.MethodImplementation) = ArrayBuilder(Of Cci.MethodImplementation).GetInstance()

            For Each member In Me.GetMembersForCci()
                If member.Kind = SymbolKind.Method Then
                    AddExplicitImplementations(context, DirectCast(member, MethodSymbol), explicitImplements, sourceNamedType, moduleBeingBuilt)
                End If
            Next

            Dim syntheticMethods = moduleBeingBuilt.GetSynthesizedMethods(Me)
            If syntheticMethods IsNot Nothing Then
                For Each synthetic In syntheticMethods
                    Dim method = TryCast(synthetic, MethodSymbol)
                    If method IsNot Nothing Then
                        AddExplicitImplementations(context, method, explicitImplements, sourceNamedType, moduleBeingBuilt)
                    End If
                Next
            End If

            Return explicitImplements.ToImmutableAndFree()
        End Function

        Private Sub AddExplicitImplementations(context As EmitContext,
                                               implementingMethod As MethodSymbol,
                                               explicitImplements As ArrayBuilder(Of Cci.MethodImplementation),
                                               sourceNamedType As SourceNamedTypeSymbol,
                                               moduleBeingBuilt As PEModuleBuilder)

            Debug.Assert(implementingMethod.PartialDefinitionPart Is Nothing) ' must be definition

            For Each implemented In implementingMethod.ExplicitInterfaceImplementations
                ' If signature doesn't match, we have created a stub with matching signature that delegates to the implementingMethod.
                ' The stub will implement the implemented method in metadata.
                If MethodSignatureComparer.CustomModifiersAndParametersAndReturnTypeSignatureComparer.Equals(implementingMethod, implemented) Then
                    explicitImplements.Add(New Cci.MethodImplementation(implementingMethod,
                        moduleBeingBuilt.TranslateOverriddenMethodReference(implemented, DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), context.Diagnostics)))
                End If
            Next

            ' Explicit overrides needed in some overriding situations.
            If OverrideHidingHelper.RequiresExplicitOverride(implementingMethod) Then
                explicitImplements.Add(New Cci.MethodImplementation(implementingMethod, implementingMethod.OverriddenMethod))
            End If

            If sourceNamedType IsNot Nothing Then
                Dim comMethod As MethodSymbol = sourceNamedType.GetCorrespondingComClassInterfaceMethod(implementingMethod)

                If comMethod IsNot Nothing Then
                    explicitImplements.Add(New Cci.MethodImplementation(implementingMethod, comMethod))
                End If
            End If
        End Sub

        Private Iterator Function ITypeDefinitionGetFields(context As EmitContext) As IEnumerable(Of IFieldDefinition) Implements ITypeDefinition.GetFields
            Debug.Assert(Not Me.IsAnonymousType)
            'Debug.Assert(((ITypeReference)this).AsTypeDefinition(moduleBeingBuilt) != null);

            ' can't be generic instantiation
            ' must be declared in the module we are building
            CheckDefinitionInvariant()

            For Each field In Me.GetFieldsToEmit()
                Yield field
            Next

            Dim syntheticFields = DirectCast(context.Module, PEModuleBuilder).GetSynthesizedFields(Me)
            If syntheticFields IsNot Nothing Then
                For Each field In syntheticFields
                    Yield field
                Next
            End If
        End Function

        Friend MustOverride Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)

        Private ReadOnly Property ITypeDefinitionGenericParameters As IEnumerable(Of IGenericTypeParameter) Implements ITypeDefinition.GenericParameters
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition(moduleBeingBuilt) != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()

                Return Me.TypeParameters
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionGenericParameterCount As UShort Implements ITypeDefinition.GenericParameterCount
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()

                Return GenericParameterCountImpl
            End Get
        End Property

        Private ReadOnly Property GenericParameterCountImpl As UShort
            Get
                Return CType(Me.Arity, UShort)
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionHasDeclarativeSecurity As Boolean Implements ITypeDefinition.HasDeclarativeSecurity
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()
                Return Me.HasDeclarativeSecurity
            End Get
        End Property

        ''' <summary>
        ''' Should return Nothing if there are none.
        ''' </summary>
        Friend Overridable Function GetSynthesizedImplements() As IEnumerable(Of NamedTypeSymbol)
            Return Nothing
        End Function

        Private Iterator Function ITypeDefinitionInterfaces(context As EmitContext) As IEnumerable(Of ITypeReference) Implements ITypeDefinition.Interfaces
            Debug.Assert(Not Me.IsAnonymousType)
            Debug.Assert((DirectCast(Me, ITypeReference)).AsTypeDefinition(context) IsNot Nothing)

            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            For Each [interface] In GetInterfacesToEmit()
                Yield moduleBeingBuilt.Translate([interface],
                                                 syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode),
                                                 diagnostics:=context.Diagnostics,
                                                 fromImplements:=True)
            Next
        End Function

        Friend Overridable Function GetInterfacesToEmit() As IEnumerable(Of NamedTypeSymbol)
            Debug.Assert(IsDefinition)
            Debug.Assert(TypeOf ContainingModule Is SourceModuleSymbol)

            ' Synthesized implements should go first. Currently they are used only by
            ' ComClass feature, which depends on the order of implemented interfaces.
            Dim synthesized As IEnumerable(Of NamedTypeSymbol) = GetSynthesizedImplements()

            ' If this type implements I, and the base class also implements interface I, and this class
            ' does not implement all the members of I, then do not emit I as an interface. This prevents
            ' the CLR from using implicit interface implementation.
            Dim interfaces = Me.InterfacesNoUseSiteDiagnostics
            If interfaces.IsEmpty Then
                Return If(synthesized, SpecializedCollections.EmptyEnumerable(Of NamedTypeSymbol)())
            End If

            Dim base = Me.BaseTypeNoUseSiteDiagnostics
            Dim result As IEnumerable(Of NamedTypeSymbol) =
                interfaces.Where(Function(sym As NamedTypeSymbol) As Boolean
                                     Return Not (base IsNot Nothing AndAlso
                                                 base.ImplementsInterface(sym, Nothing) AndAlso
                                                 Not Me.ImplementsAllMembersOfInterface(sym))
                                 End Function)

            Return If(synthesized Is Nothing, result, synthesized.Concat(result))
        End Function

        Private ReadOnly Property ITypeDefinitionIsAbstract As Boolean Implements ITypeDefinition.IsAbstract
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()
                Return IsMetadataAbstract
            End Get
        End Property

        Friend Overridable ReadOnly Property IsMetadataAbstract As Boolean
            Get
                CheckDefinitionInvariant()
                Return Me.IsMustInherit OrElse Me.IsInterface
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionIsBeforeFieldInit As Boolean Implements ITypeDefinition.IsBeforeFieldInit
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()

                ' enums or interfaces or delegates are not BeforeFieldInit
                Select Case Me.TypeKind
                    Case TypeKind.Enum, TypeKind.Interface, TypeKind.Delegate
                        Return False
                End Select

                ' apply the beforefieldinit attribute only if there is an implicitly specified static constructor (e.g. caused by
                ' a Decimal or DateTime field with an initialization).
                Dim cctor = Me.SharedConstructors.FirstOrDefault
                If cctor IsNot Nothing Then

                    Debug.Assert(Me.SharedConstructors.Length = 1)

                    ' NOTE: Partial methods without implementation should be skipped in the 
                    '       analysis above, see native compiler: PRBuilder.cpp, 'DWORD PEBuilder::GetTypeDefFlags'
                    If Not cctor.IsImplicitlyDeclared Then
                        ' If there is an explicitly implemented shared constructor, do not look further
                        Return False
                    End If

                    ' if the found constructor contains a generated AddHandler for a method,
                    '       beforefieldinit should not be applied.
                    For Each member In GetMembers()
                        If member.Kind = SymbolKind.Method Then
                            Dim methodSym = DirectCast(member, MethodSymbol)
                            Dim handledEvents = methodSym.HandledEvents
                            If Not handledEvents.IsEmpty Then
                                For Each handledEvent In handledEvents
                                    If handledEvent.hookupMethod.MethodKind = MethodKind.SharedConstructor Then
                                        Return False
                                    End If
                                Next
                            End If
                        End If
                    Next

                    ' cctor is implicit and there are no handles, so the sole purpose of 
                    ' the cctor is to initialize some fields. 
                    ' Therefore it can be deferred until fields are accessed via "beforefieldinit"
                    Return True
                End If

                ' if there is a const field of type Decimal or DateTime, the synthesized shared constructor does not
                ' appear in the member list. We need to check this separately.
                Dim sourceNamedType = TryCast(Me, SourceMemberContainerTypeSymbol)
                If sourceNamedType IsNot Nothing AndAlso Not sourceNamedType.StaticInitializers.IsDefaultOrEmpty Then
                    Return sourceNamedType.AnyInitializerToBeInjectedIntoConstructor(sourceNamedType.StaticInitializers,
                                                                                     includingNonMetadataConstants:=True)
                End If

                Return False
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionIsComObject As Boolean Implements ITypeDefinition.IsComObject
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()

                Return IsComImport
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionIsGeneric As Boolean Implements ITypeDefinition.IsGeneric
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()

                Return Me.Arity <> 0
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionIsInterface As Boolean Implements ITypeDefinition.IsInterface
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()

                Return Me.IsInterface
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionIsRuntimeSpecial As Boolean Implements ITypeDefinition.IsRuntimeSpecial
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()

                Return False
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionIsSerializable As Boolean Implements ITypeDefinition.IsSerializable
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()

                Return Me.IsSerializable
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionIsSpecialName As Boolean Implements ITypeDefinition.IsSpecialName
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()

                Return HasSpecialName
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionIsWindowsRuntimeImport As Boolean Implements ITypeDefinition.IsWindowsRuntimeImport
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()

                Return Me.IsWindowsRuntimeImport
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionIsSealed As Boolean Implements ITypeDefinition.IsSealed
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()
                Return IsMetadataSealed
            End Get
        End Property

        Friend Overridable ReadOnly Property IsMetadataSealed As Boolean
            Get
                CheckDefinitionInvariant()

                If Me.IsNotInheritable Then
                    Return True
                End If

                Select Case Me.TypeKind
                    Case TypeKind.Module, TypeKind.Enum, TypeKind.Structure
                        Return True
                    Case Else
                        Return False
                End Select
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionLayout As LayoutKind Implements ITypeDefinition.Layout
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                CheckDefinitionInvariant()

                Return Me.Layout.Kind
            End Get
        End Property

        Friend Overridable Function GetMembersForCci() As ImmutableArray(Of Symbol)
            Return Me.GetMembers()
        End Function

        Private Iterator Function ITypeDefinitionGetMethods(context As EmitContext) As IEnumerable(Of IMethodDefinition) Implements ITypeDefinition.GetMethods
            Debug.Assert(Not Me.IsAnonymousType)
            ' Debug.Assert(((ITypeReference)this).AsTypeDefinition(moduleBeingBuilt) != null);

            ' can't be generic instantiation
            ' must be declared in the module we are building
            CheckDefinitionInvariant()

            For Each method In Me.GetMethodsToEmit()
                Yield method
            Next

            Dim syntheticMethods = DirectCast(context.Module, PEModuleBuilder).GetSynthesizedMethods(Me)
            If syntheticMethods IsNot Nothing Then
                For Each method In syntheticMethods
                    Yield method
                Next
            End If
        End Function

        Friend Overridable Iterator Function GetMethodsToEmit() As IEnumerable(Of MethodSymbol)
            For Each member In Me.GetMembersForCci()
                If member.Kind = SymbolKind.Method Then
                    Dim method As MethodSymbol = DirectCast(member, MethodSymbol)

                    ' Don't emit:
                    '  (a) Partial methods without an implementation part
                    '  (b) The default value type constructor - the runtime handles that
                    If Not method.IsPartialWithoutImplementation() AndAlso Not method.IsDefaultValueTypeConstructor() Then
                        Yield method
                    End If
                End If
            Next
        End Function

        Private Function ITypeDefinitionGetNestedTypes(context As EmitContext) As IEnumerable(Of INestedTypeDefinition) Implements ITypeDefinition.GetNestedTypes
            Debug.Assert(Not Me.IsAnonymousType)
            'Debug.Assert(((ITypeReference)this).AsTypeDefinition(moduleBeingBuilt) != null);

            ' can't be generic instantiation
            ' must be declared in the module we are building
            CheckDefinitionInvariant()

            Dim containingModule = DirectCast(context.Module, PEModuleBuilder)

            Dim result As IEnumerable(Of INestedTypeDefinition)
            Dim nestedTypes = Me.GetTypeMembers() ' Ordered.
            If nestedTypes.Length = 0 Then
                result = SpecializedCollections.EmptyEnumerable(Of INestedTypeDefinition)()
            ElseIf Me.IsEmbedded Then
                ' filter out embedded nested types that are not referenced
                result = nestedTypes.Where(containingModule.SourceModule.ContainingSourceAssembly.DeclaringCompilation.EmbeddedSymbolManager.IsReferencedPredicate)
            Else
                result = nestedTypes
            End If

            Dim syntheticNested = containingModule.GetSynthesizedTypes(Me)
            If syntheticNested IsNot Nothing Then
                result = result.Concat(syntheticNested)
            End If

            Return result
        End Function

        Private Iterator Function ITypeDefinitionGetProperties(context As EmitContext) As IEnumerable(Of IPropertyDefinition) Implements ITypeDefinition.GetProperties
            Debug.Assert(Not Me.IsAnonymousType)
            'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

            ' can't be generic instantiation
            ' must be declared in the module we are building
            CheckDefinitionInvariant()

            For Each [property] In Me.GetPropertiesToEmit()
                Debug.Assert([property] IsNot Nothing)
                Yield [property]
            Next

            Dim syntheticProperties = DirectCast(context.Module, PEModuleBuilder).GetSynthesizedProperties(Me)
            If syntheticProperties IsNot Nothing Then
                For Each prop In syntheticProperties
                    Yield prop
                Next
            End If
        End Function

        Friend Overridable Iterator Function GetPropertiesToEmit() As IEnumerable(Of PropertySymbol)
            CheckDefinitionInvariant()

            For Each member In Me.GetMembersForCci()
                If member.Kind = SymbolKind.Property Then
                    Yield DirectCast(member, PropertySymbol)
                End If
            Next
        End Function

        Private ReadOnly Property ITypeDefinitionSecurityAttributes As IEnumerable(Of SecurityAttribute) Implements ITypeDefinition.SecurityAttributes
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                'Debug.Assert(((ITypeReference)this).AsTypeDefinition != null);

                ' can't be generic instantiation
                ' must be declared in the module we are building
                CheckDefinitionInvariant()

                Debug.Assert(Me.HasDeclarativeSecurity)
                Dim securityAttributes As IEnumerable(Of SecurityAttribute) = Me.GetSecurityInformation()
                Debug.Assert(securityAttributes IsNot Nothing)
                Return securityAttributes
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionSizeOf As UInteger Implements ITypeDefinition.SizeOf
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                CheckDefinitionInvariant()

                Return CUInt(Me.Layout.Size)
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionStringFormat As CharSet Implements ITypeDefinition.StringFormat
            Get
                Debug.Assert(Not Me.IsAnonymousType)
                CheckDefinitionInvariant()

                Return Me.MarshallingCharSet
            End Get
        End Property

        Private ReadOnly Property INamedTypeReferenceGenericParameterCount As UShort Implements INamedTypeReference.GenericParameterCount
            Get
                Return GenericParameterCountImpl
            End Get
        End Property

        Private ReadOnly Property INamedTypeReferenceMangleName As Boolean Implements INamedTypeReference.MangleName
            Get
                Return MangleName
            End Get
        End Property

        Private ReadOnly Property INamedEntityName As String Implements INamedEntity.Name
            Get
                ' CCI automatically adds the arity suffix, so we return Name, not MetadataName here.
                Return Me.Name
            End Get
        End Property

        Private Function INamespaceTypeReferenceGetUnit(context As EmitContext) As IUnitReference Implements INamespaceTypeReference.GetUnit
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Debug.Assert((DirectCast(Me, ITypeReference)).AsNamespaceTypeReference IsNot Nothing)
            Return moduleBeingBuilt.Translate(Me.ContainingModule, context.Diagnostics)
        End Function

        Private ReadOnly Property INamespaceTypeReferenceNamespaceName As String Implements INamespaceTypeReference.NamespaceName
            Get
                Debug.Assert((DirectCast(Me, ITypeReference)).AsNamespaceTypeReference IsNot Nothing)
                Return If(Me.GetEmittedNamespaceName(), Me.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat))
            End Get
        End Property

        Private ReadOnly Property INamespaceTypeDefinitionIsPublic As Boolean Implements INamespaceTypeDefinition.IsPublic
            Get
                'Debug.Assert(((ITypeReference)this).AsNamespaceTypeDefinition != null);
                CheckDefinitionInvariant()
                Return PEModuleBuilder.MemberVisibility(Me) = Cci.TypeMemberVisibility.Public
            End Get
        End Property

        Private Function ITypeMemberReferenceGetContainingType(context As EmitContext) As ITypeReference Implements ITypeMemberReference.GetContainingType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Debug.Assert((DirectCast(Me, ITypeReference)).AsNestedTypeReference IsNot Nothing)
            Debug.Assert(Me.IsDefinitionOrDistinct())

            If Not Me.IsDefinition Then
                Return moduleBeingBuilt.Translate(Me.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
            End If

            Return Me.ContainingType
        End Function

        Private ReadOnly Property ITypeDefinitionMemberContainingTypeDefinition As ITypeDefinition Implements ITypeDefinitionMember.ContainingTypeDefinition
            Get
                'Debug.Assert(((ITypeReference)this).AsNestedTypeDefinition != null);
                'return (ITypeDefinition)moduleBeingBuilt.Translate(this.ContainingType, true);
                Debug.Assert(Me.ContainingType IsNot Nothing)
                CheckDefinitionInvariant()

                Return Me.ContainingType
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberVisibility As TypeMemberVisibility Implements ITypeDefinitionMember.Visibility
            Get
                'Debug.Assert(((ITypeReference)this).AsNestedTypeDefinition != null);
                Debug.Assert(Me.ContainingType IsNot Nothing)
                CheckDefinitionInvariant()

                Return PEModuleBuilder.MemberVisibility(Me)
            End Get
        End Property

        Private Function IGenericTypeInstanceReferenceGetGenericArguments(context As EmitContext) As ImmutableArray(Of ITypeReference) Implements IGenericTypeInstanceReference.GetGenericArguments
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Debug.Assert((DirectCast(Me, ITypeReference)).AsGenericTypeInstanceReference IsNot Nothing)

            Dim modifiers As ImmutableArray(Of ImmutableArray(Of CustomModifier)) = Nothing

            If Me.HasTypeArgumentsCustomModifiers Then
                modifiers = Me.TypeArgumentsCustomModifiers
            End If

            Dim builder = ArrayBuilder(Of ITypeReference).GetInstance()
            Dim arguments = Me.TypeArgumentsNoUseSiteDiagnostics
            For i As Integer = 0 To arguments.Length - 1
                Dim arg = moduleBeingBuilt.Translate(arguments(i), syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)

                If Not modifiers.IsDefault AndAlso Not modifiers(i).IsDefaultOrEmpty Then
                    arg = New Cci.ModifiedTypeReference(arg, modifiers(i).As(Of Cci.ICustomModifier))
                End If

                builder.Add(arg)
            Next

            Return builder.ToImmutableAndFree
        End Function

        Private ReadOnly Property IGenericTypeInstanceReferenceGenericType As INamedTypeReference Implements IGenericTypeInstanceReference.GenericType
            Get
                Debug.Assert((DirectCast(Me, ITypeReference)).AsGenericTypeInstanceReference IsNot Nothing)
                Return GenericTypeImpl
            End Get
        End Property

        Private ReadOnly Property GenericTypeImpl As INamedTypeReference
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        Private ReadOnly Property ISpecializedNestedTypeReferenceUnspecializedVersion As INestedTypeReference Implements ISpecializedNestedTypeReference.UnspecializedVersion
            Get
                Debug.Assert((DirectCast(Me, ITypeReference)).AsSpecializedNestedTypeReference IsNot Nothing)

                Dim result = GenericTypeImpl.AsNestedTypeReference
                Debug.Assert(result IsNot Nothing)
                Return result
            End Get
        End Property
    End Class
End Namespace
