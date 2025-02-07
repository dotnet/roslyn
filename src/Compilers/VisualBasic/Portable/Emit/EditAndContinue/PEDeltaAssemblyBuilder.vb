' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class PEDeltaAssemblyBuilder
        Inherits PEAssemblyBuilderBase
        Implements IPEDeltaAssemblyBuilder

        Private ReadOnly _changes As SymbolChanges
        Private ReadOnly _deepTranslator As VisualBasicSymbolMatcher.DeepTranslator
        Private ReadOnly _predefinedHotReloadExceptionConstructor As MethodSymbol

        ''' <summary>
        ''' HotReloadException type. May be created even if not used. We might find out
        ''' we need it late in the emit phase only after all types and members have been compiled.
        ''' <see cref="_isHotReloadExceptionTypeUsed"/> indicates if the type is actually used in the delta.
        ''' </summary>
        Private _lazyHotReloadExceptionType As SynthesizedHotReloadExceptionSymbol

        ''' <summary>
        ''' True if usage of HotReloadException type symbol has been observed and shouldn't be changed anymore.
        ''' </summary>
        Private _freezeHotReloadExceptionTypeUsage As Boolean

        ''' <summary>
        ''' True if HotReloadException type is actually used in the delta.
        ''' </summary>
        Private _isHotReloadExceptionTypeUsed As Boolean

        Public Sub New(sourceAssembly As SourceAssemblySymbol,
                       changes As VisualBasicSymbolChanges,
                       emitOptions As EmitOptions,
                       outputKind As OutputKind,
                       serializationProperties As ModulePropertiesForSerialization,
                       manifestResources As IEnumerable(Of ResourceDescription),
                       predefinedHotReloadExceptionConstructor As MethodSymbol)

            MyBase.New(sourceAssembly, emitOptions, outputKind, serializationProperties, manifestResources, additionalTypes:=ImmutableArray(Of NamedTypeSymbol).Empty)

            _changes = changes

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

            _predefinedHotReloadExceptionConstructor = predefinedHotReloadExceptionConstructor
        End Sub

        Friend Overrides Function EncTranslateLocalVariableType(type As TypeSymbol, diagnostics As DiagnosticBag) As ITypeReference
            ' Note: The translator is Not aware of synthesized types. If type is a synthesized type it won't get mapped.
            ' In such case use the type itself. This can only happen for variables storing lambda display classes.
            Dim visited = DirectCast(_deepTranslator.Visit(type), TypeSymbol)
            Debug.Assert(visited IsNot Nothing OrElse TypeOf type Is LambdaFrame OrElse TypeOf DirectCast(type, NamedTypeSymbol).ConstructedFrom Is LambdaFrame)
            Return Translate(If(visited, type), Nothing, diagnostics)
        End Function

        Public Overrides ReadOnly Property EncSymbolChanges As SymbolChanges
            Get
                Return _changes
            End Get
        End Property

        Public Overrides ReadOnly Property PreviousGeneration As EmitBaseline
            Get
                Return _changes.DefinitionMap.Baseline
            End Get
        End Property

        Friend Shared Function GetOrCreateMetadataSymbols(initialBaseline As EmitBaseline, compilation As VisualBasicCompilation) As EmitBaseline.MetadataSymbols
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

            Dim synthesizedTypes = GetSynthesizedTypesFromMetadata(originalMetadata.MetadataReader, metadataDecoder)
            Dim metadataSymbols = New EmitBaseline.MetadataSymbols(synthesizedTypes, metadataDecoder, assemblyReferenceIdentityMap)

            Return InterlockedOperations.Initialize(initialBaseline.LazyMetadataSymbols, metadataSymbols)
        End Function

        ' friend for testing
        Friend Overloads Shared Function GetSynthesizedTypesFromMetadata(reader As MetadataReader, metadataDecoder As MetadataDecoder) As SynthesizedTypeMaps
            ' In general, the anonymous type name Is 'VB$Anonymous' ('Type'|'Delegate') '_' (submission-index '_')? index module-id
            ' but EnC Is not supported for modules nor submissions. Hence we only look for type names with no module id and no submission index:
            ' e.g. VB$AnonymousType_123, VB$AnonymousDelegate_123

            Dim anonymousTypes = ImmutableSegmentedDictionary.CreateBuilder(Of AnonymousTypeKey, AnonymousTypeValue)
            For Each handle In reader.TypeDefinitions
                Dim def = reader.GetTypeDefinition(handle)
                If Not def.Namespace.IsNil Then
                    Continue For
                End If

                If Not reader.StringComparer.StartsWith(def.Name, GeneratedNameConstants.AnonymousTypeOrDelegateCommonPrefix) Then
                    Continue For
                End If

                Dim metadataName = reader.GetString(def.Name)
                Dim arity As Short = 0
                Dim name = MetadataHelpers.InferTypeArityAndUnmangleMetadataName(metadataName, arity)

                Dim index As Integer = 0
                If TryParseAnonymousTypeTemplateName(GeneratedNameConstants.AnonymousTypeTemplateNamePrefix, name, index) Then
                    Dim type = DirectCast(metadataDecoder.GetTypeOfToken(handle), NamedTypeSymbol)
                    Dim key = GetAnonymousTypeKey(type)
                    Dim value = New AnonymousTypeValue(name, index, type.GetCciAdapter())
                    anonymousTypes.Add(key, value)
                ElseIf TryParseAnonymousTypeTemplateName(GeneratedNameConstants.AnonymousDelegateTemplateNamePrefix, name, index) Then
                    Dim type = DirectCast(metadataDecoder.GetTypeOfToken(handle), NamedTypeSymbol)
                    Dim key = GetAnonymousDelegateKey(type)
                    Dim value = New AnonymousTypeValue(name, index, type.GetCciAdapter())
                    anonymousTypes.Add(key, value)
                End If
            Next

            ' VB anonymous delegates are handled as anonymous types
            Return New SynthesizedTypeMaps(
                anonymousTypes.ToImmutable(),
                anonymousDelegates:=Nothing,
                anonymousDelegatesWithIndexedNames:=Nothing)
        End Function

        Friend Shared Function TryParseAnonymousTypeTemplateName(prefix As String, name As String, <Out()> ByRef index As Integer) As Boolean
            If name.StartsWith(prefix, StringComparison.Ordinal) AndAlso
                Integer.TryParse(name.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, index) Then
                Return True
            End If
            index = -1
            Return False
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
                    Debug.Assert(TypeSymbol.Equals(DirectCast(typeParameter.ContainingSymbol, TypeSymbol), type, TypeCompareKind.ConsiderEverything))
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
            parameters.AddRange(method.Parameters.SelectAsArray(Function(p) New AnonymousTypeKeyField(p.Name, isKey:=p.IsByRef, ignoreCase:=True)))
            parameters.Add(New AnonymousTypeKeyField(AnonymousTypeDescriptor.GetReturnParameterName(Not method.IsSub), isKey:=False, ignoreCase:=True))
            Return New AnonymousTypeKey(parameters.ToImmutableAndFree(), isDelegate:=True)
        End Function

        Friend ReadOnly Property PreviousDefinitions As VisualBasicDefinitionMap
            Get
                Return DirectCast(_changes.DefinitionMap, VisualBasicDefinitionMap)
            End Get
        End Property

        Friend Overloads Function GetSynthesizedTypes() As SynthesizedTypeMaps Implements IPEDeltaAssemblyBuilder.GetSynthesizedTypes
            ' VB anonymous delegates are handled as anonymous types
            Dim result = New SynthesizedTypeMaps(
                Compilation.AnonymousTypeManager.GetAnonymousTypeMap(),
                anonymousDelegates:=Nothing,
                anonymousDelegatesWithIndexedNames:=Nothing)

            ' Should contain all entries in previous generation.
            Debug.Assert(PreviousGeneration.SynthesizedTypes.IsSubsetOf(result))

            Return result
        End Function

        Friend Overrides Function TryCreateVariableSlotAllocator(method As MethodSymbol, topLevelMethod As MethodSymbol, diagnostics As DiagnosticBag) As VariableSlotAllocator
            Return _changes.DefinitionMap.TryCreateVariableSlotAllocator(Compilation, method, topLevelMethod, diagnostics)
        End Function

        Friend Overrides Function GetMethodBodyInstrumentations(method As MethodSymbol) As MethodInstrumentation
            Return _changes.DefinitionMap.GetMethodBodyInstrumentations(method)
        End Function

        Friend Overrides Function GetPreviousAnonymousTypes() As ImmutableArray(Of AnonymousTypeKey)
            Return ImmutableArray.CreateRange(PreviousGeneration.SynthesizedTypes.AnonymousTypes.Keys)
        End Function

        Friend Overrides Function GetNextAnonymousTypeIndex(fromDelegates As Boolean) As Integer
            Return PreviousGeneration.GetNextAnonymousTypeIndex(fromDelegates)
        End Function

        Friend Overrides Function TryGetAnonymousTypeName(template As AnonymousTypeManager.AnonymousTypeOrDelegateTemplateSymbol, <Out> ByRef name As String, <Out> ByRef index As Integer) As Boolean
            Debug.Assert(Compilation Is template.DeclaringCompilation)
            Return PreviousDefinitions.TryGetAnonymousTypeName(template, name, index)
        End Function

        Public Overrides Function GetTopLevelTypeDefinitions(context As EmitContext) As IEnumerable(Of Cci.INamespaceTypeDefinition)
            Return GetTopLevelTypeDefinitionsCore(context)
        End Function

        Public Overrides Function GetTopLevelSourceTypeDefinitions(context As EmitContext) As IEnumerable(Of Cci.INamespaceTypeDefinition)
            Return _changes.GetTopLevelSourceTypeDefinitions(context)
        End Function

        Friend Sub OnCreatedIndices(diagnostics As DiagnosticBag) Implements IPEDeltaAssemblyBuilder.OnCreatedIndices
            Dim embeddedTypesManager = Me.EmbeddedTypesManagerOpt
            If embeddedTypesManager IsNot Nothing Then
                For Each embeddedType In embeddedTypesManager.EmbeddedTypesMap.Keys
                    diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_EncNoPIAReference, embeddedType.AdaptedNamedTypeSymbol), Location.None)
                Next
            End If
        End Sub

        Friend Overrides ReadOnly Property AllowOmissionOfConditionalCalls As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property LinkedAssembliesDebugInfo As IEnumerable(Of String)
            Get
                ' This debug information is only emitted for the benefit of legacy EE.
                ' Since EnC requires Roslyn and Roslyn doesn't need this information we don't emit it during EnC.
                Return SpecializedCollections.EmptyEnumerable(Of String)()
            End Get
        End Property

        Public Overrides Function TryGetOrCreateSynthesizedHotReloadExceptionType() As INamedTypeSymbolInternal
            Return If(_predefinedHotReloadExceptionConstructor Is Nothing, GetOrCreateHotReloadExceptionType(), Nothing)
        End Function

        Public Overrides Function GetOrCreateHotReloadExceptionConstructorDefinition() As IMethodSymbolInternal
            If _predefinedHotReloadExceptionConstructor IsNot Nothing Then
                Return _predefinedHotReloadExceptionConstructor
            End If

            If _freezeHotReloadExceptionTypeUsage Then
                ' the type shouldn't be used after usage has been frozen.
                Throw ExceptionUtilities.Unreachable()
            End If

            _isHotReloadExceptionTypeUsed = True
            Return GetOrCreateHotReloadExceptionType().Constructor
        End Function

        Public Overrides Function GetUsedSynthesizedHotReloadExceptionType() As INamedTypeSymbolInternal
            _freezeHotReloadExceptionTypeUsage = True
            Return If(_isHotReloadExceptionTypeUsed, _lazyHotReloadExceptionType, Nothing)
        End Function

        Private Function GetOrCreateHotReloadExceptionType() As SynthesizedHotReloadExceptionSymbol
            Dim symbol = _lazyHotReloadExceptionType
            If symbol IsNot Nothing Then
                Return symbol
            End If

            Dim exceptionType = Compilation.GetWellKnownType(WellKnownType.System_Exception)
            Dim stringType = Compilation.GetSpecialType(SpecialType.System_String)
            Dim intType = Compilation.GetSpecialType(SpecialType.System_Int32)

            Dim containingNamespace = GetOrSynthesizeNamespace(SynthesizedHotReloadExceptionSymbol.NamespaceName)
            symbol = New SynthesizedHotReloadExceptionSymbol(containingNamespace, exceptionType, stringType, intType)

            Interlocked.CompareExchange(_lazyHotReloadExceptionType, symbol, comparand:=Nothing)
            Return _lazyHotReloadExceptionType
        End Function
    End Class
End Namespace
