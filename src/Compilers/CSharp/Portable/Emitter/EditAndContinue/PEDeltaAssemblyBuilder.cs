// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class PEDeltaAssemblyBuilder : PEAssemblyBuilderBase, IPEDeltaAssemblyBuilder
    {
        private readonly SymbolChanges _changes;
        private readonly CSharpSymbolMatcher.DeepTranslator _deepTranslator;
        private readonly MethodSymbol? _predefinedHotReloadExceptionConstructor;
        private readonly EmitDifferenceOptions _options;

        /// <summary>
        /// HotReloadException type. May be created even if not used. We might find out
        /// we need it late in the emit phase only after all types and members have been compiled.
        /// <see cref="_isHotReloadExceptionTypeUsed"/> indicates if the type is actually used in the delta.
        /// </summary>
        private SynthesizedHotReloadExceptionSymbol? _lazyHotReloadExceptionType;

        /// <summary>
        /// True if usage of HotReloadException type symbol has been observed and shouldn't be changed anymore.
        /// </summary>
        private volatile bool _freezeHotReloadExceptionTypeUsage;

        /// <summary>
        /// True if HotReloadException type is actually used in the delta.
        /// </summary>
        private volatile bool _isHotReloadExceptionTypeUsed;

        public PEDeltaAssemblyBuilder(
            SourceAssemblySymbol sourceAssembly,
            CSharpSymbolChanges changes,
            EmitOptions emitOptions,
            EmitDifferenceOptions options,
            OutputKind outputKind,
            Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources,
            MethodSymbol? predefinedHotReloadExceptionConstructor)
            : base(sourceAssembly, emitOptions, outputKind, serializationProperties, manifestResources, additionalTypes: [])
        {
            _changes = changes;
            _options = options;

            // Workaround for https://github.com/dotnet/roslyn/issues/3192.
            // When compiling state machine we stash types of awaiters and state-machine hoisted variables,
            // so that next generation can look variables up and reuse their slots if possible.
            //
            // When we are about to allocate a slot for a lifted variable while compiling the next generation
            // we map its type to the previous generation and then check the slot types that we stashed earlier.
            // If the variable type matches we reuse it. In order to compare the previous variable type with the current one
            // both need to be completely lowered (translated). Standard translation only goes one level deep. 
            // Generic arguments are not translated until they are needed by metadata writer. 
            //
            // In order to get the fully lowered form we run the type symbols of stashed variables through a deep translator
            // that translates the symbol recursively.
            _deepTranslator = new CSharpSymbolMatcher.DeepTranslator(sourceAssembly.GetSpecialType(SpecialType.System_Object));

            _predefinedHotReloadExceptionConstructor = predefinedHotReloadExceptionConstructor;
        }

        public override SymbolChanges? EncSymbolChanges => _changes;
        public override EmitBaseline PreviousGeneration => _changes.DefinitionMap.Baseline;
        public override bool FieldRvaSupported => _options.EmitFieldRva;
        public override bool MethodImplSupported => _options.MethodImplEntriesSupported;

        internal override Cci.ITypeReference EncTranslateLocalVariableType(TypeSymbol type, DiagnosticBag diagnostics)
        {
            // Note: The translator is not aware of synthesized types. If type is a synthesized type it won't get mapped.
            // In such case use the type itself. This can only happen for variables storing lambda display classes.
            var visited = (TypeSymbol)_deepTranslator.Visit(type);
            Debug.Assert(visited is not null);
            //Debug.Assert(visited != null || type is LambdaFrame || ((NamedTypeSymbol)type).ConstructedFrom is LambdaFrame);
            return Translate(visited ?? type, null, diagnostics);
        }

        internal static EmitBaseline.MetadataSymbols GetOrCreateMetadataSymbols(EmitBaseline initialBaseline, CSharpCompilation compilation)
        {
            if (initialBaseline.LazyMetadataSymbols != null)
            {
                return initialBaseline.LazyMetadataSymbols;
            }

            var originalMetadata = initialBaseline.OriginalMetadata;

            // The purpose of this compilation is to provide PE symbols for original metadata.
            // We need to transfer the references from the current source compilation but don't need its syntax trees.
            var metadataCompilation = compilation.RemoveAllSyntaxTrees();

            ImmutableDictionary<AssemblyIdentity, AssemblyIdentity> assemblyReferenceIdentityMap;
            var metadataAssembly = metadataCompilation.GetBoundReferenceManager().CreatePEAssemblyForAssemblyMetadata(AssemblyMetadata.Create(originalMetadata), MetadataImportOptions.All, out assemblyReferenceIdentityMap);
            var metadataDecoder = new MetadataDecoder(metadataAssembly.PrimaryModule);

            var synthesizedTypes = GetSynthesizedTypesFromMetadata(originalMetadata.MetadataReader, metadataDecoder);
            var metadataSymbols = new EmitBaseline.MetadataSymbols(synthesizedTypes, metadataDecoder, assemblyReferenceIdentityMap);

            return InterlockedOperations.Initialize(ref initialBaseline.LazyMetadataSymbols, metadataSymbols);
        }

        // internal for testing
        internal static SynthesizedTypeMaps GetSynthesizedTypesFromMetadata(MetadataReader reader, MetadataDecoder metadataDecoder)
        {
            var anonymousTypes = ImmutableSegmentedDictionary.CreateBuilder<AnonymousTypeKey, AnonymousTypeValue>();
            var anonymousDelegatesWithIndexedNames = PooledDictionary<AnonymousDelegateWithIndexedNamePartialKey, ArrayBuilder<AnonymousTypeValue>>.GetInstance();
            var anonymousDelegates = ImmutableSegmentedDictionary.CreateBuilder<SynthesizedDelegateKey, SynthesizedDelegateValue>();

            foreach (var handle in reader.TypeDefinitions)
            {
                var def = reader.GetTypeDefinition(handle);
                if (!def.Namespace.IsNil)
                {
                    continue;
                }

                if (reader.StringComparer.StartsWith(def.Name, GeneratedNames.ActionDelegateNamePrefix) ||
                    reader.StringComparer.StartsWith(def.Name, GeneratedNames.FuncDelegateNamePrefix))
                {
                    // The name of a synthesized delegate neatly encodes everything we need to identify it, either
                    // in the prefix (return void or not) or the name (ref kinds and arity) so we don't need anything
                    // fancy for a key.
                    var key = new SynthesizedDelegateKey(reader.GetString(def.Name));
                    var type = (NamedTypeSymbol)metadataDecoder.GetTypeOfToken(handle);
                    var value = new SynthesizedDelegateValue(type.GetCciAdapter());
                    anonymousDelegates.Add(key, value);
                    continue;
                }

                // In general, the anonymous type name is "<{module-id}>f__AnonymousType{index}#{submission-index}",
                // but EnC is not supported for modules nor submissions. Hence we only look for type names with no module id and no submission index.
                if (reader.StringComparer.StartsWith(def.Name, GeneratedNames.AnonymousTypeNameWithoutModulePrefix))
                {
                    var name = MetadataHelpers.InferTypeArityAndUnmangleMetadataName(reader.GetString(def.Name), out _);
                    if (int.TryParse(name.Substring(GeneratedNames.AnonymousTypeNameWithoutModulePrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int index))
                    {
                        var builder = ArrayBuilder<AnonymousTypeKeyField>.GetInstance();
                        if (TryGetAnonymousTypeKey(reader, def, builder))
                        {
                            var type = (NamedTypeSymbol)metadataDecoder.GetTypeOfToken(handle);
                            var key = new AnonymousTypeKey(builder.ToImmutable());
                            var value = new AnonymousTypeValue(name, index, type.GetCciAdapter());
                            anonymousTypes.Add(key, value);
                        }
                        builder.Free();
                    }

                    continue;
                }

                // In general, the anonymous delegate name is "<{module-id}>f__AnonymousDelegate{index}#{submission-index}",
                // but EnC is not supported for modules nor submissions. Hence we only look for type names with no module id and no submission index.
                if (reader.StringComparer.StartsWith(def.Name, GeneratedNames.AnonymousDelegateNameWithoutModulePrefix))
                {
                    var name = MetadataHelpers.InferTypeArityAndUnmangleMetadataName(reader.GetString(def.Name), out _);
                    if (int.TryParse(name.Substring(GeneratedNames.AnonymousDelegateNameWithoutModulePrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int index))
                    {
                        var type = (NamedTypeSymbol)metadataDecoder.GetTypeOfToken(handle);
                        var value = new AnonymousTypeValue(name, index, type.GetCciAdapter());
                        int parameterCount = -1;

                        foreach (var methodHandle in def.GetMethods())
                        {
                            var methodDef = reader.GetMethodDefinition(methodHandle);
                            if (reader.StringComparer.Equals(methodDef.Name, "Invoke"))
                            {
                                try
                                {
                                    metadataDecoder.DecodeMethodSignatureParameterCountsOrThrow(methodHandle, out int invokeMethodParameterCount, out _);
                                    parameterCount = invokeMethodParameterCount;
                                    break;
                                }
                                catch (BadImageFormatException)
                                {
                                    continue;
                                }
                            }
                        }

                        if (parameterCount >= 0)
                        {
                            anonymousDelegatesWithIndexedNames.AddPooled(new AnonymousDelegateWithIndexedNamePartialKey(type.Arity, parameterCount), value);
                        }
                    }

                    continue;
                }
            }

            return new SynthesizedTypeMaps(anonymousTypes.ToImmutable(), anonymousDelegates.ToImmutable(), anonymousDelegatesWithIndexedNames.ToImmutableSegmentedDictionaryAndFree());
        }

        private static bool TryGetAnonymousTypeKey(
            MetadataReader reader,
            TypeDefinition def,
            ArrayBuilder<AnonymousTypeKeyField> builder)
        {
            foreach (var typeParameterHandle in def.GetGenericParameters())
            {
                var typeParameter = reader.GetGenericParameter(typeParameterHandle);
                if (!GeneratedNameParser.TryParseAnonymousTypeParameterName(reader.GetString(typeParameter.Name), out var fieldName))
                {
                    return false;
                }

                builder.Add(new AnonymousTypeKeyField(fieldName, isKey: false, ignoreCase: false));
            }
            return true;
        }

        internal CSharpDefinitionMap PreviousDefinitions
            => (CSharpDefinitionMap)_changes.DefinitionMap;

        public override IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypeDefinitions(EmitContext context)
            => GetTopLevelTypeDefinitionsExcludingNoPiaAndRootModule(context, includePrivateImplementationDetails: true);

        public override IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelSourceTypeDefinitions(EmitContext context)
        {
            return _changes.GetTopLevelSourceTypeDefinitions(context);
        }

        internal override VariableSlotAllocator? TryCreateVariableSlotAllocator(MethodSymbol method, MethodSymbol topLevelMethod, DiagnosticBag diagnostics)
        {
            return _changes.DefinitionMap.TryCreateVariableSlotAllocator(Compilation, method, topLevelMethod, diagnostics);
        }

        internal override MethodInstrumentation GetMethodBodyInstrumentations(MethodSymbol method)
        {
            // EmitDifference does not allow setting instrumentation kinds on EmitOptions:
            Debug.Assert(EmitOptions.InstrumentationKinds.IsEmpty);

            return _changes.DefinitionMap.GetMethodBodyInstrumentations(method);
        }

        internal override int GetNextAnonymousTypeIndex()
            => PreviousGeneration.GetNextAnonymousTypeIndex();

        internal override int GetNextAnonymousDelegateIndex()
            => PreviousGeneration.GetNextAnonymousDelegateIndex();

        internal override bool TryGetPreviousAnonymousTypeValue(AnonymousTypeManager.AnonymousTypeOrDelegateTemplateSymbol template, out AnonymousTypeValue typeValue)
        {
            Debug.Assert(Compilation == template.DeclaringCompilation);
            return PreviousDefinitions.TryGetAnonymousTypeValue(template, out typeValue);
        }

        public void OnCreatedIndices(DiagnosticBag diagnostics)
        {
            var embeddedTypesManager = this.EmbeddedTypesManagerOpt;
            if (embeddedTypesManager != null)
            {
                foreach (var embeddedType in embeddedTypesManager.EmbeddedTypesMap.Keys)
                {
                    diagnostics.Add(new CSDiagnosticInfo(ErrorCode.ERR_EncNoPIAReference, embeddedType.AdaptedSymbol), Location.None);
                }
            }
        }

        public override INamedTypeSymbolInternal? TryGetOrCreateSynthesizedHotReloadExceptionType()
            => _predefinedHotReloadExceptionConstructor is null
                ? GetOrCreateSynthesizedHotReloadExceptionType()
                : null;

        public override IMethodSymbolInternal GetOrCreateHotReloadExceptionConstructorDefinition()
        {
            if (_predefinedHotReloadExceptionConstructor is not null)
            {
                return _predefinedHotReloadExceptionConstructor;
            }

            if (_freezeHotReloadExceptionTypeUsage)
            {
                // the type shouldn't be used after usage has been frozen.
                throw ExceptionUtilities.Unreachable();
            }

            _isHotReloadExceptionTypeUsed = true;
            return GetOrCreateSynthesizedHotReloadExceptionType().Constructor;
        }

        public override INamedTypeSymbolInternal? GetUsedSynthesizedHotReloadExceptionType()
        {
            _freezeHotReloadExceptionTypeUsage = true;
            return _isHotReloadExceptionTypeUsed ? _lazyHotReloadExceptionType : null;
        }

        private SynthesizedHotReloadExceptionSymbol GetOrCreateSynthesizedHotReloadExceptionType()
        {
            var symbol = _lazyHotReloadExceptionType;
            if (symbol is not null)
            {
                return symbol;
            }

            var exceptionType = Compilation.GetWellKnownType(WellKnownType.System_Exception);
            var stringType = Compilation.GetSpecialType(SpecialType.System_String);
            var intType = Compilation.GetSpecialType(SpecialType.System_Int32);

            var containingNamespace = GetOrSynthesizeNamespace(SynthesizedHotReloadExceptionSymbol.NamespaceName);
            symbol = new SynthesizedHotReloadExceptionSymbol(containingNamespace, exceptionType, stringType, intType);

            Interlocked.CompareExchange(ref _lazyHotReloadExceptionType, symbol, comparand: null);
            return _lazyHotReloadExceptionType;
        }
    }
}
