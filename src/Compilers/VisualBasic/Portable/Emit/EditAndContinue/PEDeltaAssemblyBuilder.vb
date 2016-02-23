' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class PEDeltaAssemblyBuilder
        Inherits PEAssemblyBuilderBase
        Implements IPEDeltaAssemblyBuilder

        Private ReadOnly _previousGeneration As EmitBaseline
        Private ReadOnly _previousDefinitions As VisualBasicDefinitionMap
        Private ReadOnly _changes As SymbolChanges
        Private ReadOnly _deepTranslator As VisualBasicSymbolMatcher.DeepTranslator

        Public Sub New(sourceAssembly As SourceAssemblySymbol,
                       emitOptions As EmitOptions,
                       outputKind As OutputKind,
                       serializationProperties As ModulePropertiesForSerialization,
                       manifestResources As IEnumerable(Of ResourceDescription),
                       previousGeneration As EmitBaseline,
                       edits As IEnumerable(Of SemanticEdit),
                       isAddedSymbol As Func(Of ISymbol, Boolean))

            MyBase.New(sourceAssembly, emitOptions, outputKind, serializationProperties, manifestResources, additionalTypes:=ImmutableArray(Of NamedTypeSymbol).Empty)

            Dim initialBaseline = previousGeneration.InitialBaseline
            Dim context = New EmitContext(Me, Nothing, New DiagnosticBag())

            ' Hydrate symbols from initial metadata. Once we do so it is important to reuse these symbols across all generations,
            ' in order for the symbol matcher to be able to use reference equality once it maps symbols to initial metadata.
            Dim metadataSymbols = GetOrCreateMetadataSymbols(initialBaseline, sourceAssembly.DeclaringCompilation)

            Dim metadataDecoder = DirectCast(metadataSymbols.MetadataDecoder, MetadataDecoder)
            Dim metadataAssembly = DirectCast(metadataDecoder.ModuleSymbol.ContainingAssembly, PEAssemblySymbol)
            Dim matchToMetadata = New VisualBasicSymbolMatcher(initialBaseline.LazyMetadataSymbols.AnonymousTypes, sourceAssembly, context, metadataAssembly)

            Dim matchToPrevious As VisualBasicSymbolMatcher = Nothing
            If previousGeneration.Ordinal > 0 Then
                Dim previousAssembly = DirectCast(previousGeneration.Compilation, VisualBasicCompilation).SourceAssembly
                Dim previousContext = New EmitContext(DirectCast(previousGeneration.PEModuleBuilder, PEModuleBuilder), Nothing, New DiagnosticBag())

                matchToPrevious = New VisualBasicSymbolMatcher(
                    previousGeneration.AnonymousTypeMap,
                    sourceAssembly:=sourceAssembly,
                    sourceContext:=context,
                    otherAssembly:=previousAssembly,
                    otherContext:=previousContext,
                    otherSynthesizedMembersOpt:=previousGeneration.SynthesizedMembers)
            End If

            _previousDefinitions = New VisualBasicDefinitionMap(previousGeneration.OriginalMetadata.Module, edits, metadataDecoder, matchToMetadata, matchToPrevious)
            _previousGeneration = previousGeneration
            _changes = New SymbolChanges(_previousDefinitions, edits, isAddedSymbol)

            ' Workaround for https://github.com/dotnet/roslyn/issues/3192. 
            ' When compiling state machine we stash types of awaiters and state-machine hoisted variables,
            ' so that next generation can look variables up and reuse their slots if possible.
            '
            ' When we are about to allocate a slot for a lifted variable while compiling the next generation
            ' we map its type to the previous generation and then check the slot types that we stashed earlier.
            ' If the variable type matches we reuse it. In order to compare the previous variable type with the current one
            ' both need to be completely lowered (translated). Standard translation only goes one level deep. 
            ' Generic arguments are not translated until they are needed by metadata writer. 
            '
            ' In order to get the fully lowered form we run the type symbols of stashed variables through a deep translator
            ' that translates the symbol recursively.
            _deepTranslator = New VisualBasicSymbolMatcher.DeepTranslator(sourceAssembly.GetSpecialType(SpecialType.System_Object))
        End Sub

        Friend Overrides Function EncTranslateLocalVariableType(type As TypeSymbol, diagnostics As DiagnosticBag) As ITypeReference
            ' Note: The translator is Not aware of synthesized types. If type is a synthesized type it won't get mapped.
            ' In such case use the type itself. This can only happen for variables storing lambda display classes.
            Dim visited = DirectCast(_deepTranslator.Visit(type), TypeSymbol)
            Debug.Assert(visited IsNot Nothing OrElse TypeOf type Is LambdaFrame OrElse TypeOf DirectCast(type, NamedTypeSymbol).ConstructedFrom Is LambdaFrame)
            Return Translate(If(visited, type), Nothing, diagnostics)
        End Function

        Public Overrides ReadOnly Property CurrentGenerationOrdinal As Integer
            Get
                Return _previousGeneration.Ordinal + 1
            End Get
        End Property

        Private Function GetOrCreateMetadataSymbols(initialBaseline As EmitBaseline, compilation As VisualBasicCompilation) As EmitBaseline.MetadataSymbols
            If initialBaseline.LazyMetadataSymbols IsNot Nothing Then
                Return initialBaseline.LazyMetadataSymbols
            End If

            Dim originalMetadata = initialBaseline.OriginalMetadata

            ' The purpose of this compilation is to provide PE symbols for original metadata.
            ' We need to transfer the references from the current source compilation but don't need its syntax trees.
            Dim metadataCompilation = compilation.RemoveAllSyntaxTrees()

            Dim assemblyReferenceIdentityMap As ImmutableDictionary(Of AssemblyIdentity, AssemblyIdentity) = Nothing
            Dim metadataAssembly = metadataCompilation.GetBoundReferenceManager().CreatePEAssemblyForAssemblyMetadata(AssemblyMetadata.Create(originalMetadata), MetadataImportOptions.All, assemblyReferenceIdentityMap)
            Dim metadataDecoder = New MetadataDecoder(metadataAssembly.PrimaryModule)
            Dim metadataAnonymousTypes = GetAnonymousTypeMapFromMetadata(originalMetadata.MetadataReader, metadataDecoder)
            Dim metadataSymbols = New EmitBaseline.MetadataSymbols(metadataAnonymousTypes, metadataDecoder, assemblyReferenceIdentityMap)

            Return InterlockedOperations.Initialize(initialBaseline.LazyMetadataSymbols, metadataSymbols)
        End Function

        ' friend for testing
        Friend Overloads Shared Function GetAnonymousTypeMapFromMetadata(reader As MetadataReader, metadataDecoder As MetadataDecoder) As IReadOnlyDictionary(Of AnonymousTypeKey, AnonymousTypeValue)
            Dim result = New Dictionary(Of AnonymousTypeKey, AnonymousTypeValue)
            For Each handle In reader.TypeDefinitions
                Dim def = reader.GetTypeDefinition(handle)
                If Not def.Namespace.IsNil Then
                    Continue For
                End If
                If Not reader.StringComparer.StartsWith(def.Name, GeneratedNames.AnonymousTypeOrDelegateCommonPrefix) Then
                    Continue For
                End If
                Dim metadataName = reader.GetString(def.Name)
                Dim arity As Short = 0
                Dim name = MetadataHelpers.InferTypeArityAndUnmangleMetadataName(metadataName, arity)
                Dim index As Integer = 0
                If GeneratedNames.TryParseAnonymousTypeTemplateName(GeneratedNames.AnonymousTypeTemplateNamePrefix, name, index) Then
                    Dim type = DirectCast(metadataDecoder.GetTypeOfToken(handle), NamedTypeSymbol)
                    Dim key = GetAnonymousTypeKey(type)
                    Dim value = New AnonymousTypeValue(name, index, type)
                    result.Add(key, value)
                ElseIf GeneratedNames.TryParseAnonymousTypeTemplateName(GeneratedNames.AnonymousDelegateTemplateNamePrefix, name, index) Then
                    Dim type = DirectCast(metadataDecoder.GetTypeOfToken(handle), NamedTypeSymbol)
                    Dim key = GetAnonymousDelegateKey(type)
                    Dim value = New AnonymousTypeValue(name, index, type)
                    result.Add(key, value)
                End If
            Next
            Return result
        End Function

        Private Shared Function GetAnonymousTypeKey(type As NamedTypeSymbol) As AnonymousTypeKey
            ' The key is the set of properties that correspond to type parameters.
            ' For each type parameter, get the name of the property of that type.
            Dim n = type.TypeParameters.Length
            If n = 0 Then
                Return New AnonymousTypeKey(ImmutableArray(Of AnonymousTypeKeyField).Empty)
            End If

            ' Properties indexed by type parameter ordinal.
            Dim properties = New AnonymousTypeKeyField(n - 1) {}
            For Each member In type.GetMembers()
                If member.Kind <> SymbolKind.Property Then
                    Continue For
                End If

                Dim [property] = DirectCast(member, PropertySymbol)
                Dim propertyType = [property].Type
                If propertyType.TypeKind = TypeKind.TypeParameter Then
                    Dim typeParameter = DirectCast(propertyType, TypeParameterSymbol)
                    Debug.Assert(typeParameter.ContainingSymbol = type)
                    Dim index = typeParameter.Ordinal
                    Debug.Assert(properties(index).Name Is Nothing)
                    ' ReadOnly anonymous type properties were 'Key' properties.
                    properties(index) = New AnonymousTypeKeyField([property].Name, isKey:=[property].IsReadOnly, ignoreCase:=True)
                End If
            Next

            Debug.Assert(properties.All(Function(f) Not String.IsNullOrEmpty(f.Name)))
            Return New AnonymousTypeKey(ImmutableArray.Create(properties))
        End Function

        Private Shared Function GetAnonymousDelegateKey(type As NamedTypeSymbol) As AnonymousTypeKey
            Debug.Assert(type.BaseTypeNoUseSiteDiagnostics.SpecialType = SpecialType.System_MulticastDelegate)

            ' The key is the set of parameter names to the Invoke method,
            ' where the parameters are of the type parameters.
            Dim members = type.GetMembers(WellKnownMemberNames.DelegateInvokeName)
            Debug.Assert(members.Length = 1 AndAlso members(0).Kind = SymbolKind.Method)
            Dim method = DirectCast(members(0), MethodSymbol)
            Debug.Assert(method.Parameters.Length + If(method.IsSub, 0, 1) = type.TypeParameters.Length)
            Dim parameters = ArrayBuilder(Of AnonymousTypeKeyField).GetInstance()
            parameters.AddRange(method.Parameters.SelectAsArray(Function(p) New AnonymousTypeKeyField(p.Name, isKey:=False, ignoreCase:=True)))
            parameters.Add(New AnonymousTypeKeyField(AnonymousTypeDescriptor.GetReturnParameterName(Not method.IsSub), isKey:=False, ignoreCase:=True))
            Return New AnonymousTypeKey(parameters.ToImmutableAndFree(), isDelegate:=True)
        End Function

        Friend ReadOnly Property PreviousGeneration As EmitBaseline
            Get
                Return _previousGeneration
            End Get
        End Property

        Friend ReadOnly Property PreviousDefinitions As VisualBasicDefinitionMap
            Get
                Return _previousDefinitions
            End Get
        End Property

        Friend Overrides ReadOnly Property SupportsPrivateImplClass As Boolean
            Get
                ' Disable <PrivateImplementationDetails> in ENC since the
                ' CLR does Not support adding non-private members.
                Return False
            End Get
        End Property

        Friend Overloads Function GetAnonymousTypeMap() As IReadOnlyDictionary(Of AnonymousTypeKey, AnonymousTypeValue) Implements IPEDeltaAssemblyBuilder.GetAnonymousTypeMap
            Dim anonymousTypes = Compilation.AnonymousTypeManager.GetAnonymousTypeMap()
            ' Should contain all entries in previous generation.
            Debug.Assert(_previousGeneration.AnonymousTypeMap.All(Function(p) anonymousTypes.ContainsKey(p.Key)))
            Return anonymousTypes
        End Function

        Friend Overrides Function TryCreateVariableSlotAllocator(method As MethodSymbol, topLevelMethod As MethodSymbol) As VariableSlotAllocator
            Return _previousDefinitions.TryCreateVariableSlotAllocator(_previousGeneration, method, topLevelMethod)
        End Function

        Friend Overrides Function GetPreviousAnonymousTypes() As ImmutableArray(Of AnonymousTypeKey)
            Return ImmutableArray.CreateRange(_previousGeneration.AnonymousTypeMap.Keys)
        End Function

        Friend Overrides Function GetNextAnonymousTypeIndex(fromDelegates As Boolean) As Integer
            Return _previousGeneration.GetNextAnonymousTypeIndex(fromDelegates)
        End Function

        Friend Overrides Function TryGetAnonymousTypeName(template As NamedTypeSymbol, <Out()> ByRef name As String, <Out()> ByRef index As Integer) As Boolean
            Debug.Assert(Compilation Is template.DeclaringCompilation)
            Return _previousDefinitions.TryGetAnonymousTypeName(template, name, index)
        End Function

        Friend ReadOnly Property Changes As SymbolChanges
            Get
                Return _changes
            End Get
        End Property

        Friend Overrides Function GetTopLevelTypesCore(context As EmitContext) As IEnumerable(Of Cci.INamespaceTypeDefinition)
            Return _changes.GetTopLevelTypes(context)
        End Function

        Friend Sub OnCreatedIndices(diagnostics As DiagnosticBag) Implements IPEDeltaAssemblyBuilder.OnCreatedIndices
            Dim embeddedTypesManager = Me.EmbeddedTypesManagerOpt
            If embeddedTypesManager IsNot Nothing Then
                For Each embeddedType In embeddedTypesManager.EmbeddedTypesMap.Keys
                    diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_EncNoPIAReference, embeddedType), Location.None)
                Next
            End If
        End Sub

        Friend Overrides ReadOnly Property AllowOmissionOfConditionalCalls As Boolean
            Get
                Return True
            End Get
        End Property

        Protected Overrides ReadOnly Property LinkedAssembliesDebugInfo As IEnumerable(Of String)
            Get
                ' This debug information is only emitted for the benefit of legacy EE.
                ' Since EnC requires Roslyn and Roslyn doesn't need this information we don't emit it during EnC.
                Return SpecializedCollections.EmptyEnumerable(Of String)()
            End Get
        End Property
    End Class
End Namespace
