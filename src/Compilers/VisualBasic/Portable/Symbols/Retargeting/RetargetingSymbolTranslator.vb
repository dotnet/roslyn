' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting

    Friend Enum RetargetOptions As Byte
        RetargetPrimitiveTypesByName = 0
        RetargetPrimitiveTypesByTypeCode = 1
    End Enum

    Partial Friend Class RetargetingModuleSymbol
        ''' <summary>
        ''' Retargeting map from underlying module to this one.
        ''' </summary>
        Private ReadOnly _symbolMap As New ConcurrentDictionary(Of Symbol, Symbol)()

        Private ReadOnly _createRetargetingMethod As Func(Of Symbol, RetargetingMethodSymbol)
        Private ReadOnly _createRetargetingNamespace As Func(Of Symbol, RetargetingNamespaceSymbol)
        Private ReadOnly _createRetargetingTypeParameter As Func(Of Symbol, RetargetingTypeParameterSymbol)
        Private ReadOnly _createRetargetingNamedType As Func(Of Symbol, RetargetingNamedTypeSymbol)
        Private ReadOnly _createRetargetingField As Func(Of Symbol, RetargetingFieldSymbol)
        Private ReadOnly _createRetargetingProperty As Func(Of Symbol, RetargetingPropertySymbol)
        Private ReadOnly _createRetargetingEvent As Func(Of Symbol, RetargetingEventSymbol)

        Private Function CreateRetargetingMethod(symbol As Symbol) As RetargetingMethodSymbol
            Debug.Assert(symbol.ContainingModule Is Me.UnderlyingModule)
            Return New RetargetingMethodSymbol(Me, DirectCast(symbol, MethodSymbol))
        End Function

        Private Function CreateRetargetingNamespace(symbol As Symbol) As RetargetingNamespaceSymbol
            Debug.Assert(symbol.ContainingModule Is Me.UnderlyingModule)
            Return New RetargetingNamespaceSymbol(Me, DirectCast(symbol, NamespaceSymbol))
        End Function

        Private Function CreateRetargetingNamedType(symbol As Symbol) As RetargetingNamedTypeSymbol
            Debug.Assert(symbol.ContainingModule Is Me.UnderlyingModule)
            Return New RetargetingNamedTypeSymbol(Me, DirectCast(symbol, NamedTypeSymbol))
        End Function

        Private Function CreateRetargetingField(symbol As Symbol) As RetargetingFieldSymbol
            Debug.Assert(symbol.ContainingModule Is Me.UnderlyingModule)
            Return New RetargetingFieldSymbol(Me, DirectCast(symbol, FieldSymbol))
        End Function

        Private Function CreateRetargetingProperty(symbol As Symbol) As RetargetingPropertySymbol
            Debug.Assert(symbol.ContainingModule Is Me.UnderlyingModule)
            Return New RetargetingPropertySymbol(Me, DirectCast(symbol, PropertySymbol))
        End Function

        Private Function CreateRetargetingEvent(symbol As Symbol) As RetargetingEventSymbol
            Debug.Assert(symbol.ContainingModule Is Me.UnderlyingModule)
            Return New RetargetingEventSymbol(Me, DirectCast(symbol, EventSymbol))
        End Function

        Private Function CreateRetargetingTypeParameter(symbol As Symbol) As RetargetingTypeParameterSymbol
            Dim typeParameter = DirectCast(symbol, TypeParameterSymbol)
            Dim container = typeParameter.ContainingSymbol

            Dim containingType = If(container.Kind = SymbolKind.Method,
                                     container.ContainingType,
                                     DirectCast(container, NamedTypeSymbol))

            Debug.Assert(containingType.ContainingModule Is Me.UnderlyingModule)
            Return New RetargetingTypeParameterSymbol(Me, typeParameter)
        End Function

        Friend Class RetargetingSymbolTranslator
            Inherits VisualBasicSymbolVisitor(Of RetargetOptions, Symbol)

            Private ReadOnly _retargetingModule As RetargetingModuleSymbol

            Public Sub New(retargetingModule As RetargetingModuleSymbol)
                Debug.Assert(retargetingModule IsNot Nothing)
                _retargetingModule = retargetingModule
            End Sub

            ''' <summary>
            ''' Retargeting map from underlying module to the retargeting module.
            ''' </summary>
            Private ReadOnly Property SymbolMap As ConcurrentDictionary(Of Symbol, Symbol)
                Get
                    Return _retargetingModule._symbolMap
                End Get
            End Property

            ''' <summary>
            ''' RetargetingAssemblySymbol owning retargeting module.
            ''' </summary>
            Private ReadOnly Property RetargetingAssembly As RetargetingAssemblySymbol
                Get
                    Return _retargetingModule._retargetingAssembly
                End Get
            End Property

            ''' <summary>
            ''' The map that captures information about what assembly should be retargeted 
            ''' to what assembly. Key is the AssemblySymbol referenced by the underlying module,
            ''' value is the corresponding AssemblySymbol referenced by the retargeting module, 
            ''' and corresponding retargeting map for symbols.
            ''' </summary>
            Private ReadOnly Property RetargetingAssemblyMap As Dictionary(Of AssemblySymbol, DestinationData)
                Get
                    Return _retargetingModule._retargetingAssemblyMap
                End Get
            End Property

            ''' <summary>
            ''' The underlying ModuleSymbol for the retargeting module.
            ''' </summary>
            Private ReadOnly Property UnderlyingModule As SourceModuleSymbol
                Get
                    Return _retargetingModule._underlyingModule
                End Get
            End Property

            Public Function Retarget(symbol As Symbol) As Symbol
                Debug.Assert(symbol.Kind <> SymbolKind.NamedType OrElse DirectCast(symbol, NamedTypeSymbol).PrimitiveTypeCode = PrimitiveTypeCode.NotPrimitive)
                Return symbol.Accept(Me, RetargetOptions.RetargetPrimitiveTypesByName)
            End Function

            Public Function Retarget(marshallingInfo As MarshalPseudoCustomAttributeData) As MarshalPseudoCustomAttributeData
                If marshallingInfo Is Nothing Then
                    Return Nothing
                End If

                ' Retarget by type code - primitive types are encoded in short form in an attribute signature:
                Return marshallingInfo.WithTranslatedTypes(Of TypeSymbol, RetargetingSymbolTranslator)(
                    Function(type, translator) translator.Retarget(DirectCast(type, TypeSymbol), RetargetOptions.RetargetPrimitiveTypesByTypeCode), Me)
            End Function


            Public Function Retarget(symbol As TypeSymbol, options As RetargetOptions) As TypeSymbol
                Return DirectCast(symbol.Accept(Me, options), TypeSymbol)
            End Function

            Public Function Retarget(ns As NamespaceSymbol) As NamespaceSymbol
                Return DirectCast(SymbolMap.GetOrAdd(ns, _retargetingModule._createRetargetingNamespace), NamespaceSymbol)
            End Function

            Private Function RetargetNamedTypeDefinition(type As NamedTypeSymbol, options As RetargetOptions) As NamedTypeSymbol
                Debug.Assert(type Is type.OriginalDefinition)

                ' Before we do anything else, check if we need to do special retargeting
                ' for primitive type references encoded with enum values in metadata signatures.
                If (options = RetargetOptions.RetargetPrimitiveTypesByTypeCode) Then
                    Dim typeCode As PrimitiveTypeCode = type.PrimitiveTypeCode

                    If typeCode <> PrimitiveTypeCode.NotPrimitive Then
                        Return RetargetingAssembly.GetPrimitiveType(typeCode)
                    End If
                End If

                If type.Kind = SymbolKind.ErrorType Then
                    Return Retarget(DirectCast(type, ErrorTypeSymbol))
                End If

                Dim retargetFrom As AssemblySymbol = type.ContainingAssembly

                ' Deal with "to be local" NoPia types leaking through source module.
                ' These are the types that are coming from assemblies linked (/l-ed) 
                ' by the compilation that created the source module.
                Dim isLocalType As Boolean
                Dim useTypeIdentifierAttribute As Boolean = False

                If retargetFrom Is RetargetingAssembly.UnderlyingAssembly Then
                    Debug.Assert(Not retargetFrom.IsLinked)
                    isLocalType = type.IsExplicitDefinitionOfNoPiaLocalType
                Else
                    isLocalType = retargetFrom.IsLinked
                End If

                If isLocalType Then
                    Return RetargetNoPiaLocalType(type)
                End If

                ' Perform general retargeting.

                If retargetFrom Is RetargetingAssembly.UnderlyingAssembly Then
                    Return RetargetNamedTypeDefinitionFromUnderlyingAssembly(type)
                End If

                ' Does this type come from one of the retargeted assemblies?
                Dim destination As DestinationData = Nothing

                If Not RetargetingAssemblyMap.TryGetValue(retargetFrom, destination) Then
                    ' No need to retarget
                    Return type
                End If

                ' Retarget from one assembly to another
                Return PerformTypeRetargeting(destination, type)
            End Function

            Private Function RetargetNamedTypeDefinitionFromUnderlyingAssembly(type As NamedTypeSymbol) As NamedTypeSymbol
                ' The type is defined in the underlying assembly.
                Dim [module] = type.ContainingModule

                If [module] Is UnderlyingModule Then
                    Debug.Assert(Not type.IsExplicitDefinitionOfNoPiaLocalType)
                    Dim container = type.ContainingType
                    While container IsNot Nothing
                        If container.IsExplicitDefinitionOfNoPiaLocalType Then
                            ' Types nested into local types are not supported.
                            Return DirectCast(Me.SymbolMap.GetOrAdd(type, New UnsupportedMetadataTypeSymbol()), NamedTypeSymbol)
                        End If
                        container = container.ContainingType
                    End While
                    Return DirectCast(Me.SymbolMap.GetOrAdd(type, _retargetingModule._createRetargetingNamedType), NamedTypeSymbol)
                Else
                    ' The type is defined in one of the added modules
                    Debug.Assert([module].Ordinal > 0)
                    Dim addedModule = DirectCast(RetargetingAssembly.Modules([module].Ordinal), PEModuleSymbol)
                    Debug.Assert(DirectCast([module], PEModuleSymbol).Module Is addedModule.Module)
                    Return RetargetNamedTypeDefinition(DirectCast(type, PENamedTypeSymbol), addedModule)
                End If
            End Function

            Private Function RetargetNoPiaLocalType(type As NamedTypeSymbol) As NamedTypeSymbol
                Dim cached As NamedTypeSymbol = Nothing

                If RetargetingAssembly.m_NoPiaUnificationMap.TryGetValue(type, cached) Then
                    Return cached
                End If

                Dim result As NamedTypeSymbol

                If type.ContainingSymbol.Kind <> SymbolKind.NamedType AndAlso
                   type.Arity = 0 Then
                    ' Get type's identity

                    Dim isInterface As Boolean = (type.IsInterface)
                    Dim hasGuid = False
                    Dim interfaceGuid As String = Nothing
                    Dim scope As String = Nothing

                    If isInterface Then
                        ' Get type's Guid
                        hasGuid = type.GetGuidString(interfaceGuid)
                    End If

                    Dim name = MetadataTypeName.FromFullName(type.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat), forcedArity:=type.Arity)
                    Dim identifier As String = Nothing

                    If type.ContainingModule Is _retargetingModule.UnderlyingModule Then
                        ' This is a local type explicitly declared in source. Get information from TypeIdentifier attribute.
                        For Each attrData In type.GetAttributes()
                            Dim signatureIndex = attrData.GetTargetAttributeSignatureIndex(type, AttributeDescription.TypeIdentifierAttribute)

                            If signatureIndex <> -1 Then
                                Debug.Assert(signatureIndex = 0 OrElse signatureIndex = 1)

                                If signatureIndex = 1 AndAlso attrData.CommonConstructorArguments.Length = 2 Then
                                    scope = TryCast(attrData.CommonConstructorArguments(0).Value, String)
                                    identifier = TryCast(attrData.CommonConstructorArguments(1).Value, String)
                                End If

                                Exit For
                            End If
                        Next

                    Else
                        Debug.Assert(type.ContainingAssembly IsNot RetargetingAssembly.UnderlyingAssembly)

                        ' Note, this logic should match the one in EmbeddedType.Cci.IReference.GetAttributes.
                        ' Here we are trying to predict what attributes we will emit on embedded type, which corresponds the 
                        ' type we are retargeting. That function actually emits the attributes.

                        If Not (hasGuid OrElse isInterface) Then
                            type.ContainingAssembly.GetGuidString(scope)
                            identifier = name.FullName
                        End If

                    End If

                    result = MetadataDecoder.SubstituteNoPiaLocalType(
                        name,
                        isInterface,
                        type.BaseTypeNoUseSiteDiagnostics,
                        interfaceGuid,
                        scope,
                        identifier,
                        RetargetingAssembly)

                    Debug.Assert(result IsNot Nothing)
                Else
                    ' TODO: report better error?
                    result = New UnsupportedMetadataTypeSymbol()
                End If

                cached = RetargetingAssembly.m_NoPiaUnificationMap.GetOrAdd(type, result)

                Return cached
            End Function

            Private Shared Function RetargetNamedTypeDefinition(type As PENamedTypeSymbol, addedModule As PEModuleSymbol) As NamedTypeSymbol
                Debug.Assert(Not type.ContainingModule.Equals(addedModule) AndAlso
                             DirectCast(type.ContainingModule, PEModuleSymbol).Module Is addedModule.Module)

                Dim cached As TypeSymbol = Nothing

                If addedModule.TypeHandleToTypeMap.TryGetValue(type.Handle, cached) Then
                    Return DirectCast(cached, NamedTypeSymbol)
                End If

                Dim result As NamedTypeSymbol

                Dim containingType As NamedTypeSymbol = type.ContainingType
                Dim mdName As MetadataTypeName

                If containingType IsNot Nothing Then
                    ' Nested type.  We need to retarget 
                    ' the enclosing type and then go back and get the type we are interested in.

                    Dim scope As NamedTypeSymbol = RetargetNamedTypeDefinition(DirectCast(containingType, PENamedTypeSymbol), addedModule)

                    mdName = MetadataTypeName.FromTypeName(type.MetadataName, forcedArity:=type.Arity)
                    result = scope.LookupMetadataType(mdName)
                    Debug.Assert(result IsNot Nothing)
                    Debug.Assert(result.Arity = type.Arity)
                Else
                    Dim namespaceName As String = If(type.GetEmittedNamespaceName(), type.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat))
                    mdName = MetadataTypeName.FromNamespaceAndTypeName(namespaceName, type.MetadataName, forcedArity:=type.Arity)
                    result = addedModule.LookupTopLevelMetadataType(mdName)
                    Debug.Assert(result.Arity = type.Arity)
                End If

                Return result
            End Function

            Private Shared Function PerformTypeRetargeting(
                ByRef destination As DestinationData,
                type As NamedTypeSymbol) As NamedTypeSymbol
                Dim result As NamedTypeSymbol = Nothing

                If Not destination.SymbolMap.TryGetValue(type, result) Then
                    ' Lookup by name as a TypeRef.
                    Dim containingType As NamedTypeSymbol = type.ContainingType
                    Dim result1 As NamedTypeSymbol
                    Dim mdName As MetadataTypeName

                    If containingType IsNot Nothing Then
                        ' This happens if type is a nested class.  We need to retarget 
                        ' the enclosing class and then go back and get the type we are interested in.

                        Dim scope As NamedTypeSymbol = PerformTypeRetargeting(destination, containingType)
                        mdName = MetadataTypeName.FromTypeName(type.MetadataName, forcedArity:=type.Arity)
                        result1 = scope.LookupMetadataType(mdName)
                        Debug.Assert(result1 IsNot Nothing)
                        Debug.Assert(result1.Arity = type.Arity)
                    Else
                        Dim namespaceName As String = If(type.GetEmittedNamespaceName(), type.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat))
                        mdName = MetadataTypeName.FromNamespaceAndTypeName(namespaceName, type.MetadataName, forcedArity:=type.Arity)
                        result1 = destination.To.LookupTopLevelMetadataType(mdName, digThroughForwardedTypes:=True)
                        Debug.Assert(result1.Arity = type.Arity)
                    End If

                    result = destination.SymbolMap.GetOrAdd(type, result1)
                    Debug.Assert(result1.Equals(result))
                End If

                Return result
            End Function

            Public Function Retarget(type As NamedTypeSymbol, options As RetargetOptions) As NamedTypeSymbol
                Dim originalDefinition As NamedTypeSymbol = type.OriginalDefinition

                Dim newDefinition As NamedTypeSymbol = RetargetNamedTypeDefinition(originalDefinition, options)

                If type Is originalDefinition Then
                    Return newDefinition
                End If

                If newDefinition.Kind = SymbolKind.ErrorType AndAlso Not newDefinition.IsGenericType Then
                    Return newDefinition
                End If

                ' This must be a generic instantiation (i.e. constructed type).
                Debug.Assert(originalDefinition.Arity = 0 OrElse type.ConstructedFrom IsNot type)

                If type.IsUnboundGenericType Then
                    If newDefinition Is originalDefinition Then
                        Return type
                    End If

                    Return newDefinition.AsUnboundGenericType()
                End If

                Debug.Assert(type.ContainingType Is Nothing OrElse Not type.ContainingType.IsUnboundGenericType)

                Dim genericType As NamedTypeSymbol = type
                Dim oldArguments = ArrayBuilder(Of TypeWithModifiers).GetInstance()
                Dim startOfNonInterfaceArguments As Integer = Integer.MaxValue

                ' Collect generic arguments for the type and its containers.
                While genericType IsNot Nothing
                    If startOfNonInterfaceArguments = Integer.MaxValue AndAlso
                       Not genericType.IsInterface Then
                        startOfNonInterfaceArguments = oldArguments.Count
                    End If

                    Dim arity As Integer = genericType.Arity

                    If arity > 0 Then
                        Dim args = genericType.TypeArgumentsNoUseSiteDiagnostics

                        If genericType.HasTypeArgumentsCustomModifiers Then
                            Dim modifiers = genericType.TypeArgumentsCustomModifiers

                            For i As Integer = 0 To arity - 1
                                oldArguments.Add(New TypeWithModifiers(args(i), modifiers(i)))
                            Next
                        Else
                            For i As Integer = 0 To arity - 1
                                oldArguments.Add(New TypeWithModifiers(args(i)))
                            Next
                        End If
                    End If

                    genericType = genericType.ContainingType
                End While

                Dim anythingRetargeted As Boolean = Not originalDefinition.Equals(newDefinition)

                ' retarget the arguments
                Dim newArguments = ArrayBuilder(Of TypeWithModifiers).GetInstance(oldArguments.Count)

                For Each arg In oldArguments
                    Dim modifiersHaveChanged As Boolean = False

                    ' generic instantiation is a signature
                    Dim newArg = New TypeWithModifiers(DirectCast(arg.Type.Accept(Me, RetargetOptions.RetargetPrimitiveTypesByTypeCode), TypeSymbol),
                                                       RetargetModifiers(arg.CustomModifiers, modifiersHaveChanged))

                    If Not anythingRetargeted AndAlso (modifiersHaveChanged OrElse newArg.Type <> arg.Type) Then
                        anythingRetargeted = True
                    End If

                    newArguments.Add(newArg)
                Next

                ' See if it is or its enclosing type is a non-interface closed over NoPia local types.
                Dim noPiaIllegalGenericInstantiation As Boolean = IsNoPiaIllegalGenericInstantiation(oldArguments, newArguments, startOfNonInterfaceArguments)
                oldArguments.Free()
                Dim constructedType As NamedTypeSymbol

                If Not anythingRetargeted Then
                    ' Nothing was retargeted, return original type symbol.
                    constructedType = [type]
                Else
                    ' Create symbol for new constructed type and return it.

                    ' need to collect type parameters in the same order as we have arguments, 
                    ' but this should be done for the new definition.
                    genericType = newDefinition
                    Dim newParameters = ArrayBuilder(Of TypeParameterSymbol).GetInstance(newArguments.Count)

                    ' Collect generic arguments for the type and its containers.
                    While genericType IsNot Nothing
                        If genericType.Arity > 0 Then
                            newParameters.AddRange(genericType.TypeParameters)
                        End If

                        genericType = genericType.ContainingType
                    End While

                    Debug.Assert(newParameters.Count = newArguments.Count)

                    newParameters.ReverseContents()
                    newArguments.ReverseContents()
                    Dim substitution As TypeSubstitution = TypeSubstitution.Create(newDefinition, newParameters.ToImmutableAndFree(), newArguments.ToImmutable())

                    constructedType = newDefinition.Construct(substitution)
                End If

                newArguments.Free()

                If noPiaIllegalGenericInstantiation Then
                    constructedType = New NoPiaIllegalGenericInstantiationSymbol(constructedType)
                End If

                Return DirectCast(constructedType, NamedTypeSymbol)
            End Function

            Private Function IsNoPiaIllegalGenericInstantiation(oldArguments As ArrayBuilder(Of TypeWithModifiers), newArguments As ArrayBuilder(Of TypeWithModifiers), startOfNonInterfaceArguments As Integer) As Boolean
                ' TODO: Do we need to check constraints on type parameters as well?

                If UnderlyingModule.ContainsExplicitDefinitionOfNoPiaLocalTypes Then
                    For i As Integer = startOfNonInterfaceArguments To oldArguments.Count - 1 Step 1
                        If IsOrClosedOverAnExplicitLocalType(oldArguments(i).Type) Then
                            Return True
                        End If
                    Next
                End If

                Dim assembliesToEmbedTypesFrom As ImmutableArray(Of AssemblySymbol) = UnderlyingModule.GetAssembliesToEmbedTypesFrom()

                If assembliesToEmbedTypesFrom.Length > 0 Then
                    For i As Integer = startOfNonInterfaceArguments To oldArguments.Count - 1 Step 1
                        If MetadataDecoder.IsOrClosedOverATypeFromAssemblies(oldArguments(i).Type, assembliesToEmbedTypesFrom) Then
                            Return True
                        End If
                    Next
                End If

                Dim linkedAssemblies As ImmutableArray(Of AssemblySymbol) = RetargetingAssembly.GetLinkedReferencedAssemblies()

                If Not linkedAssemblies.IsDefaultOrEmpty Then
                    For i As Integer = startOfNonInterfaceArguments To newArguments.Count - 1 Step 1
                        If MetadataDecoder.IsOrClosedOverATypeFromAssemblies(newArguments(i).Type, linkedAssemblies) Then
                            Return True
                        End If
                    Next
                End If

                Return False
            End Function

            ''' <summary>
            ''' Perform a check whether the type or at least one of its generic arguments 
            ''' is an explicitly defined local type. The check is performed recursively. 
            ''' </summary>
            Private Function IsOrClosedOverAnExplicitLocalType(symbol As TypeSymbol) As Boolean

                Select Case symbol.Kind
                    Case SymbolKind.TypeParameter
                        Return False

                    Case SymbolKind.ArrayType
                        Return IsOrClosedOverAnExplicitLocalType(DirectCast(symbol, ArrayTypeSymbol).ElementType)

                    Case SymbolKind.ErrorType, SymbolKind.NamedType
                        Dim namedType = DirectCast(symbol, NamedTypeSymbol)

                        If symbol.OriginalDefinition.ContainingModule Is _retargetingModule.UnderlyingModule AndAlso
                            namedType.IsExplicitDefinitionOfNoPiaLocalType Then
                            Return True
                        End If

                        Do
                            For Each argument In namedType.TypeArgumentsNoUseSiteDiagnostics
                                If IsOrClosedOverAnExplicitLocalType(argument) Then
                                    Return True
                                End If
                            Next

                            namedType = namedType.ContainingType
                        Loop While namedType IsNot Nothing

                        Return False

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(symbol.Kind)

                End Select

            End Function

            Public Overridable Function Retarget(typeParameter As TypeParameterSymbol) As TypeParameterSymbol
                Return DirectCast(SymbolMap.GetOrAdd(typeParameter, _retargetingModule._createRetargetingTypeParameter), TypeParameterSymbol)
            End Function

            Public Function Retarget(type As ArrayTypeSymbol) As ArrayTypeSymbol
                Dim oldElement As TypeSymbol = type.ElementType
                Dim newElement As TypeSymbol = Retarget(oldElement, RetargetOptions.RetargetPrimitiveTypesByTypeCode)

                Dim modifiersHaveChanged As Boolean = False
                Dim newModifiers As ImmutableArray(Of CustomModifier) = RetargetModifiers(type.CustomModifiers, modifiersHaveChanged)

                If Not modifiersHaveChanged AndAlso oldElement.Equals(newElement) Then
                    Return type
                End If

                If type.IsSZArray Then
                    Return ArrayTypeSymbol.CreateSZArray(newElement, newModifiers, RetargetingAssembly)
                End If

                Return ArrayTypeSymbol.CreateMDArray(newElement, newModifiers, type.Rank, type.Sizes, type.LowerBounds, RetargetingAssembly)
            End Function

            Friend Function RetargetModifiers(oldModifiers As ImmutableArray(Of CustomModifier), ByRef modifiersHaveChanged As Boolean) As ImmutableArray(Of CustomModifier)
                Dim i As Integer
                Dim count As Integer = oldModifiers.Length
                modifiersHaveChanged = False

                If count <> 0 Then
                    Dim newModifiers As CustomModifier() = New CustomModifier(count - 1) {}

                    For i = 0 To count - 1 Step 1
                        Dim newModifier As NamedTypeSymbol = Retarget(DirectCast(oldModifiers(i).Modifier, NamedTypeSymbol), RetargetOptions.RetargetPrimitiveTypesByName) ' should be retargeted by name

                        If Not newModifier.Equals(oldModifiers(i).Modifier) Then
                            modifiersHaveChanged = True
                            newModifiers(i) = If(oldModifiers(i).IsOptional,
                                                VisualBasicCustomModifier.CreateOptional(newModifier),
                                                VisualBasicCustomModifier.CreateRequired(newModifier))
                        Else
                            newModifiers(i) = oldModifiers(i)
                        End If
                    Next

                    Return newModifiers.AsImmutableOrNull()
                End If

                Return oldModifiers
            End Function

            Friend Function RetargetModifiers(oldModifiers As ImmutableArray(Of CustomModifier), ByRef lazyCustomModifiers As ImmutableArray(Of CustomModifier)) As ImmutableArray(Of CustomModifier)
                If lazyCustomModifiers.IsDefault Then
                    Dim modifiersHaveChanged As Boolean
                    Dim newModifiers = RetargetModifiers(oldModifiers, modifiersHaveChanged)

                    If Not modifiersHaveChanged Then
                        newModifiers = oldModifiers
                    End If

                    ImmutableInterlocked.InterlockedCompareExchange(lazyCustomModifiers, newModifiers, Nothing)
                End If

                Return lazyCustomModifiers
            End Function

            Private Function RetargetAttributes(oldAttributes As ImmutableArray(Of VisualBasicAttributeData)) As ImmutableArray(Of VisualBasicAttributeData)
                Return oldAttributes.SelectAsArray(Function(a, t) t.RetargetAttributeData(a), Me)
            End Function

            Friend Iterator Function RetargetAttributes(attributes As IEnumerable(Of VisualBasicAttributeData)) As IEnumerable(Of VisualBasicAttributeData)
#If DEBUG Then
                Dim x As SynthesizedAttributeData = Nothing
                Dim y As SourceAttributeData = x ' Code below relies on the fact that SynthesizedAttributeData derives from SourceAttributeData.
                x = DirectCast(y, SynthesizedAttributeData)
#End If
                For Each attrData In attributes
                    Yield RetargetAttributeData(attrData)
                Next
            End Function

            Private Function RetargetAttributeData(oldAttribute As VisualBasicAttributeData) As VisualBasicAttributeData
                Dim oldAttributeCtor As MethodSymbol = oldAttribute.AttributeConstructor
                Dim newAttributeCtor As MethodSymbol = If(oldAttributeCtor Is Nothing,
                                                          Nothing,
                                                          Retarget(oldAttributeCtor, MethodSignatureComparer.RetargetedExplicitMethodImplementationComparer))

                Dim oldAttributeType As NamedTypeSymbol = oldAttribute.AttributeClass
                Dim newAttributeType As NamedTypeSymbol

                If newAttributeCtor IsNot Nothing Then
                    newAttributeType = newAttributeCtor.ContainingType
                ElseIf oldAttributeType IsNot Nothing Then
                    newAttributeType = Retarget(oldAttributeType, RetargetOptions.RetargetPrimitiveTypesByTypeCode)
                Else
                    newAttributeType = Nothing
                End If

                Dim oldCtorArguments = oldAttribute.CommonConstructorArguments
                Dim newCtorArguments = RetargetAttributeConstructorArguments(oldCtorArguments)

                Dim oldNamedArguments = oldAttribute.CommonNamedArguments
                Dim newNamedArguments = RetargetAttributeNamedArguments(oldNamedArguments)

                ' Must create a RetargetingAttributeData even if the types and
                ' arguments are unchanged since the AttributeData instance is
                ' used to resolve System.Type which may require retargeting.
                Return New RetargetingAttributeData(oldAttribute.ApplicationSyntaxReference,
                                                    newAttributeType,
                                                    newAttributeCtor,
                                                    newCtorArguments,
                                                    newNamedArguments,
                                                    oldAttribute.IsConditionallyOmitted,
                                                    oldAttribute.HasErrors)
            End Function

            Private Function RetargetAttributeConstructorArguments(constructorArguments As ImmutableArray(Of TypedConstant)) As ImmutableArray(Of TypedConstant)
                Dim retargetedArguments = constructorArguments
                Dim argumentsHaveChanged As Boolean = False

                If Not constructorArguments.IsDefault AndAlso constructorArguments.Any() Then
                    Dim newArguments = ArrayBuilder(Of TypedConstant).GetInstance(constructorArguments.Length)

                    For Each oldArgument As TypedConstant In constructorArguments
                        Dim retargetedArgument As TypedConstant = RetargetTypedConstant(oldArgument, argumentsHaveChanged)
                        newArguments.Add(retargetedArgument)
                    Next

                    If argumentsHaveChanged Then
                        retargetedArguments = newArguments.ToImmutable()
                    End If

                    newArguments.Free()
                End If

                Return retargetedArguments
            End Function

            Private Function RetargetTypedConstant(oldConstant As TypedConstant, ByRef typedConstantChanged As Boolean) As TypedConstant
                Dim oldConstantType As TypeSymbol = DirectCast(oldConstant.Type, TypeSymbol)
                Dim newConstantType As TypeSymbol = If(oldConstantType Is Nothing,
                                                       Nothing,
                                                       Retarget(oldConstantType, RetargetOptions.RetargetPrimitiveTypesByTypeCode))

                If oldConstant.Kind = TypedConstantKind.Array Then
                    Dim newArray = RetargetAttributeConstructorArguments(oldConstant.Values)
                    If newConstantType IsNot oldConstantType OrElse newArray <> oldConstant.Values Then
                        typedConstantChanged = True
                        Return New TypedConstant(newConstantType, newArray)
                    Else
                        Return oldConstant
                    End If
                End If

                Dim newConstantValue As Object
                Dim oldConstantValue = oldConstant.Value
                If (oldConstant.Kind = TypedConstantKind.Type) AndAlso (oldConstantValue IsNot Nothing) Then
                    newConstantValue = Retarget(DirectCast(oldConstantValue, TypeSymbol), RetargetOptions.RetargetPrimitiveTypesByTypeCode)
                Else
                    newConstantValue = oldConstantValue
                End If

                If newConstantType IsNot oldConstantType OrElse newConstantValue IsNot oldConstantValue Then
                    typedConstantChanged = True
                    Return New TypedConstant(newConstantType, oldConstant.Kind, newConstantValue)
                Else
                    Return oldConstant
                End If
            End Function


            Private Function RetargetAttributeNamedArguments(namedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))) As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))
                Dim retargetedArguments = namedArguments
                Dim argumentsHaveChanged As Boolean = False

                If namedArguments.Any() Then
                    Dim newArguments = ArrayBuilder(Of KeyValuePair(Of String, TypedConstant)).GetInstance(namedArguments.Length)

                    For Each oldArgument As KeyValuePair(Of String, TypedConstant) In namedArguments
                        Dim oldConstant As TypedConstant = oldArgument.Value
                        Dim typedConstantChanged As Boolean = False
                        Dim newConstant As TypedConstant = RetargetTypedConstant(oldConstant, typedConstantChanged)

                        If typedConstantChanged Then
                            newArguments.Add(New KeyValuePair(Of String, TypedConstant)(oldArgument.Key, newConstant))
                            argumentsHaveChanged = True
                        Else
                            newArguments.Add(oldArgument)
                        End If

                    Next

                    If argumentsHaveChanged Then
                        retargetedArguments = newArguments.ToImmutable()
                    End If

                    newArguments.Free()
                End If

                Return retargetedArguments
            End Function

            ' Get the retargeted attributes
            Friend Function GetRetargetedAttributes(underlyingSymbol As Symbol, ByRef lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData), Optional getReturnTypeAttributes As Boolean = False) As ImmutableArray(Of VisualBasicAttributeData)
                If lazyCustomAttributes.IsDefault Then
                    Dim oldAttributes As ImmutableArray(Of VisualBasicAttributeData)

                    If Not getReturnTypeAttributes Then
                        oldAttributes = underlyingSymbol.GetAttributes()

                        If underlyingSymbol.Kind = SymbolKind.Method Then
                            ' Also compute the return type attributes here because GetAttributes 
                            ' is called during ForceComplete on the symbol.
                            Dim unused = DirectCast(underlyingSymbol, MethodSymbol).GetReturnTypeAttributes()
                        End If
                    Else
                        Debug.Assert(underlyingSymbol.Kind = SymbolKind.Method)
                        oldAttributes = DirectCast(underlyingSymbol, MethodSymbol).GetReturnTypeAttributes()
                    End If

                    Dim retargetedAttributes As ImmutableArray(Of VisualBasicAttributeData) = RetargetAttributes(oldAttributes)

                    ImmutableInterlocked.InterlockedCompareExchange(lazyCustomAttributes, retargetedAttributes, Nothing)
                End If

                Return lazyCustomAttributes
            End Function

            Public Function Retarget(type As ErrorTypeSymbol) As ErrorTypeSymbol
                ' TODO: if it is no longer missing in the target assembly, then we can resolve it here.

                ' A retargeted error symbol must trigger an error on use so that a dependent compilation won't
                ' improperly succeed. We therefore ensure we have a use-site diagnostic.
                Dim useSiteDiagnostic = type.GetUseSiteErrorInfo
                If useSiteDiagnostic IsNot Nothing Then
                    Return type
                End If

                Dim errorInfo = If(type.ErrorInfo, ErrorFactory.ErrorInfo(ERRID.ERR_InReferencedAssembly, If(type.ContainingAssembly?.Identity.GetDisplayName, "")))
                Return New ExtendedErrorTypeSymbol(errorInfo, type.Name, type.Arity, type.CandidateSymbols, type.ResultKind, True)
            End Function

            Public Function Retarget(sequence As IEnumerable(Of NamedTypeSymbol)) As IEnumerable(Of NamedTypeSymbol)
                Return sequence.Select(Function(s)
                                           Debug.Assert(s.PrimitiveTypeCode = PrimitiveTypeCode.NotPrimitive)
                                           Return Retarget(s, RetargetOptions.RetargetPrimitiveTypesByName)
                                       End Function)
            End Function

            Public Function Retarget(arr As ImmutableArray(Of Symbol)) As ImmutableArray(Of Symbol)
                Dim symbols = ArrayBuilder(Of Symbol).GetInstance(arr.Length)

                For Each s As Symbol In arr
                    symbols.Add(Retarget(s))
                Next

                Return symbols.ToImmutableAndFree()
            End Function

            Public Function Retarget(sequence As ImmutableArray(Of NamedTypeSymbol)) As ImmutableArray(Of NamedTypeSymbol)
                Dim result = ArrayBuilder(Of NamedTypeSymbol).GetInstance(sequence.Length)

                For Each nts As NamedTypeSymbol In sequence
                    ' We want this to be true in non-error cases, but it is not true in general.
                    ' Debug.Assert(sequence(i).PrimitiveTypeCode = PrimitiveTypeCode.NotPrimitive)
                    result.Add(Retarget(nts, RetargetOptions.RetargetPrimitiveTypesByName))
                Next

                Return result.ToImmutableAndFree()
            End Function

            Public Function Retarget(sequence As ImmutableArray(Of TypeSymbol)) As ImmutableArray(Of TypeSymbol)
                Dim result = ArrayBuilder(Of TypeSymbol).GetInstance(sequence.Length)

                For Each ts As TypeSymbol In sequence
                    ' We want this to be true in non-error cases, but it is not true in general.
                    ' Debug.Assert(sequence(i).PrimitiveTypeCode = PrimitiveTypeCode.NotPrimitive)
                    result.Add(Retarget(ts, RetargetOptions.RetargetPrimitiveTypesByName))
                Next

                Return result.ToImmutableAndFree()
            End Function

            Public Function Retarget(list As ImmutableArray(Of TypeParameterSymbol)) As ImmutableArray(Of TypeParameterSymbol)
                Dim parameters = ArrayBuilder(Of TypeParameterSymbol).GetInstance(list.Length)

                For Each tps As TypeParameterSymbol In list
                    parameters.Add(Retarget(tps))
                Next

                Return parameters.ToImmutableAndFree()
            End Function

            Public Function Retarget(method As MethodSymbol) As RetargetingMethodSymbol
                Return DirectCast(SymbolMap.GetOrAdd(method, _retargetingModule._createRetargetingMethod), RetargetingMethodSymbol)
            End Function

            Public Function Retarget(method As MethodSymbol, retargetedMethodComparer As IEqualityComparer(Of MethodSymbol)) As MethodSymbol
                If method.ContainingModule Is Me.UnderlyingModule AndAlso method.IsDefinition Then
                    Return DirectCast(SymbolMap.GetOrAdd(method, _retargetingModule._createRetargetingMethod), RetargetingMethodSymbol)
                End If

                Dim containingType = method.ContainingType
                Dim retargetedType = Retarget(containingType, RetargetOptions.RetargetPrimitiveTypesByName)

                ' NB: may return null if the method cannot be found in the retargeted type (e.g. removed in a subsequent version)
                Return If(retargetedType Is containingType,
                          method,
                          FindMethodInRetargetedType(method, retargetedType, retargetedMethodComparer))
            End Function

            Private Function FindMethodInRetargetedType(method As MethodSymbol, retargetedType As NamedTypeSymbol, retargetedMethodComparer As IEqualityComparer(Of MethodSymbol)) As MethodSymbol
                Return RetargetedTypeMethodFinder.Find(Me, method, retargetedType, retargetedMethodComparer)
            End Function

            Private Class RetargetedTypeMethodFinder
                Inherits RetargetingSymbolTranslator

                Private Sub New(retargetingModule As RetargetingModuleSymbol)
                    MyBase.New(retargetingModule)
                End Sub

                Public Shared Function Find(
                    translator As RetargetingSymbolTranslator,
                    method As MethodSymbol,
                    retargetedType As NamedTypeSymbol,
                    retargetedMethodComparer As IEqualityComparer(Of MethodSymbol)
                ) As MethodSymbol
                    If retargetedType.IsErrorType() Then
                        Return Nothing
                    End If

                    If Not method.IsGenericMethod Then
                        Return FindWorker(translator, method, retargetedType, retargetedMethodComparer)
                    End If

                    ' We shouldn't run into a constructed method here because we are looking for a method
                    ' among members of a type, constructed methods are never returned through GetMembers API.
                    Debug.Assert(method Is method.ConstructedFrom)

                    ' A generic method needs special handling because its signature is very likely
                    ' to refer to method's type parameters.
                    Dim finder = New RetargetedTypeMethodFinder(translator._retargetingModule)
                    Return FindWorker(finder, method, retargetedType, retargetedMethodComparer)
                End Function

                Private Shared Function FindWorker(
                    translator As RetargetingSymbolTranslator,
                    method As MethodSymbol,
                    retargetedType As NamedTypeSymbol,
                    retargetedMethodComparer As IEqualityComparer(Of MethodSymbol)
                ) As MethodSymbol
                    Dim modifiersHaveChanged As Boolean

                    Dim targetParamsBuilder = ArrayBuilder(Of ParameterSymbol).GetInstance(method.Parameters.Length)
                    For Each param As ParameterSymbol In method.Parameters
                        targetParamsBuilder.Add(New SignatureOnlyParameterSymbol(
                                                translator.Retarget(param.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode),
                                                translator.RetargetModifiers(param.CustomModifiers, modifiersHaveChanged),
                                                param.ExplicitDefaultConstantValue, param.IsParamArray,
                                                param.IsByRef, param.IsOut, param.IsOptional))
                    Next

                    ' We will be using this symbol only for the purpose of method signature comparison,
                    ' IndexedTypeParameterSymbols should work just fine as the type parameters for the method.
                    ' We can't produce "real" TypeParameterSymbols without finding the method first and this
                    ' is what we are trying to do right now.
                    Dim targetMethod = New SignatureOnlyMethodSymbol(method.Name, retargetedType, method.MethodKind,
                                                                     method.CallingConvention,
                                                                     IndexedTypeParameterSymbol.Take(method.Arity),
                                                                     targetParamsBuilder.ToImmutableAndFree(),
                                                                     method.ReturnsByRef,
                                                                     translator.Retarget(method.ReturnType, RetargetOptions.RetargetPrimitiveTypesByTypeCode),
                                                                     translator.RetargetModifiers(method.ReturnTypeCustomModifiers, modifiersHaveChanged),
                                                                     ImmutableArray(Of MethodSymbol).Empty)

                    For Each retargetedMember As Symbol In retargetedType.GetMembers(method.Name)
                        If retargetedMember.Kind = SymbolKind.Method Then
                            Dim retargetedMethod = DirectCast(retargetedMember, MethodSymbol)
                            If retargetedMethodComparer.Equals(retargetedMethod, targetMethod) Then
                                Return retargetedMethod
                            End If
                        End If
                    Next

                    Return Nothing
                End Function

                Public Overrides Function Retarget(typeParameter As TypeParameterSymbol) As TypeParameterSymbol
                    If typeParameter.ContainingModule Is Me.UnderlyingModule Then
                        Return MyBase.Retarget(typeParameter)
                    End If

                    Debug.Assert(typeParameter.TypeParameterKind = TypeParameterKind.Method)

                    ' The method symbol we are building will be using IndexedTypeParameterSymbols as 
                    ' its type parameters, therefore, we should return them here as well.
                    Return IndexedTypeParameterSymbol.GetTypeParameter(typeParameter.Ordinal)
                End Function
            End Class

            Public Function Retarget(field As FieldSymbol) As RetargetingFieldSymbol
                Return DirectCast(SymbolMap.GetOrAdd(field, _retargetingModule._createRetargetingField), RetargetingFieldSymbol)
            End Function

            Public Function Retarget([property] As PropertySymbol) As RetargetingPropertySymbol
                Return DirectCast(SymbolMap.GetOrAdd([property], _retargetingModule._createRetargetingProperty), RetargetingPropertySymbol)
            End Function

            Public Function Retarget([event] As EventSymbol) As RetargetingEventSymbol
                Return DirectCast(SymbolMap.GetOrAdd([event], _retargetingModule._createRetargetingEvent), RetargetingEventSymbol)
            End Function

            Public Function RetargetImplementedEvent([event] As EventSymbol) As EventSymbol
                If ([event].ContainingModule Is Me.UnderlyingModule) AndAlso [event].IsDefinition Then
                    Return DirectCast(SymbolMap.GetOrAdd([event], _retargetingModule._createRetargetingEvent), RetargetingEventSymbol)
                End If

                Dim containingType = [event].ContainingType
                Dim retargetedType = Retarget(containingType, RetargetOptions.RetargetPrimitiveTypesByName)

                ' NB: may return Nothing if the [event] cannot be found in the retargeted type (e.g. removed in a subsequent version)
                Return If(retargetedType Is containingType,
                          [event],
                          FindEventInRetargetedType([event], retargetedType))
            End Function

            Private Function FindEventInRetargetedType([event] As EventSymbol,
                                                       retargetedType As NamedTypeSymbol) As EventSymbol

                Dim retargetedEventType = Retarget([event].Type, RetargetOptions.RetargetPrimitiveTypesByName)

                For Each retargetedMember As Symbol In retargetedType.GetMembers([event].Name)
                    If retargetedMember.Kind = SymbolKind.Event Then
                        Dim retargetedEvent = DirectCast(retargetedMember, EventSymbol)

                        If retargetedEvent.Type = retargetedEventType Then
                            Return retargetedEvent
                        End If
                    End If
                Next

                Return Nothing
            End Function

            Public Function Retarget([property] As PropertySymbol, retargetedPropertyComparer As IEqualityComparer(Of PropertySymbol)) As PropertySymbol
                If ([property].ContainingModule Is Me.UnderlyingModule) AndAlso [property].IsDefinition Then
                    Return DirectCast(SymbolMap.GetOrAdd([property], _retargetingModule._createRetargetingProperty), RetargetingPropertySymbol)
                End If

                Dim containingType = [property].ContainingType
                Dim retargetedType = Retarget(containingType, RetargetOptions.RetargetPrimitiveTypesByName)

                ' NB: may return Nothing if the [property] cannot be found in the retargeted type (e.g. removed in a subsequent version)
                Return If(retargetedType Is containingType,
                          [property],
                          FindPropertyInRetargetedType([property], retargetedType, retargetedPropertyComparer))
            End Function

            Private Function FindPropertyInRetargetedType([property] As PropertySymbol, retargetedType As NamedTypeSymbol, retargetedPropertyComparer As IEqualityComparer(Of PropertySymbol)) As PropertySymbol
                Dim modifiersHaveChanged As Boolean

                Dim targetParamsBuilder = ArrayBuilder(Of ParameterSymbol).GetInstance()
                For Each param As ParameterSymbol In [property].Parameters
                    targetParamsBuilder.Add(New SignatureOnlyParameterSymbol(
                                            Retarget(param.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode),
                                            RetargetModifiers(param.CustomModifiers, modifiersHaveChanged),
                                            If(param.HasExplicitDefaultValue, param.ExplicitDefaultConstantValue, Nothing), param.IsParamArray,
                                            param.IsByRef, param.IsOut, param.IsOptional))
                Next

                Dim targetProperty = New SignatureOnlyPropertySymbol([property].Name,
                                                                     retargetedType,
                                                                     [property].IsReadOnly,
                                                                     [property].IsWriteOnly,
                                                                     targetParamsBuilder.ToImmutableAndFree(),
                                                                     [property].ReturnsByRef,
                                                                     Retarget([property].Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode),
                                                                     RetargetModifiers([property].TypeCustomModifiers, modifiersHaveChanged))

                For Each retargetedMember As Symbol In retargetedType.GetMembers([property].Name)
                    If retargetedMember.Kind = SymbolKind.Property Then
                        Dim retargetedProperty = DirectCast(retargetedMember, PropertySymbol)
                        If retargetedPropertyComparer.Equals(retargetedProperty, targetProperty) Then
                            Return retargetedProperty
                        End If
                    End If
                Next

                Return Nothing
            End Function


            Public Overrides Function VisitModule(symbol As ModuleSymbol, options As RetargetOptions) As Symbol
                ' We shouldn't run into any other module, but the underlying module
                Debug.Assert(symbol Is _retargetingModule.UnderlyingModule)
                Return _retargetingModule
            End Function

            Public Overrides Function VisitNamespace(symbol As NamespaceSymbol, options As RetargetOptions) As Symbol
                Return Retarget(symbol)
            End Function

            Public Overrides Function VisitNamedType(symbol As NamedTypeSymbol, options As RetargetOptions) As Symbol
                Return Retarget(symbol, options)
            End Function

            Public Overrides Function VisitArrayType(symbol As ArrayTypeSymbol, arg As RetargetOptions) As Symbol
                Return Retarget(symbol)
            End Function

            Public Overrides Function VisitMethod(symbol As MethodSymbol, options As RetargetOptions) As Symbol
                Return Retarget(symbol)
            End Function

            Public Overrides Function VisitField(symbol As FieldSymbol, options As RetargetOptions) As Symbol
                Return Retarget(symbol)
            End Function

            Public Overrides Function VisitProperty(symbol As PropertySymbol, arg As RetargetOptions) As Symbol
                Return Retarget(symbol)
            End Function

            Public Overrides Function VisitEvent(symbol As EventSymbol, arg As RetargetOptions) As Symbol
                Return Retarget(symbol)
            End Function

            Public Overrides Function VisitTypeParameter(symbol As TypeParameterSymbol, options As RetargetOptions) As Symbol
                Return Retarget(symbol)
            End Function

            Public Overrides Function VisitErrorType(symbol As ErrorTypeSymbol, options As RetargetOptions) As Symbol
                Return Retarget(symbol)
            End Function

        End Class

    End Class

End Namespace

