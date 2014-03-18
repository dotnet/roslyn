// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class PEDeltaAssemblyBuilder : PEAssemblyBuilderBase
    {
        private readonly Microsoft.CodeAnalysis.Emit.EmitBaseline previousGeneration;
        private readonly CSharpCompilation.DefinitionMap previousDefinitions;
        private readonly Microsoft.CodeAnalysis.Emit.SymbolChanges changes;

        public PEDeltaAssemblyBuilder(
            SourceAssemblySymbol sourceAssembly,
            string outputName,
            OutputKind outputKind,
            ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources,
            Func<AssemblySymbol, AssemblyIdentity> assemblySymbolMapper,
            ImmutableArray<NamedTypeSymbol> additionalTypes,
            Microsoft.CodeAnalysis.Emit.EmitBaseline previousGeneration,
            IEnumerable<Microsoft.CodeAnalysis.Emit.SemanticEdit> edits)
            : base(sourceAssembly, outputName, outputKind, serializationProperties, manifestResources, assemblySymbolMapper, additionalTypes)
        {
            var context = new Microsoft.CodeAnalysis.Emit.Context(this, null, new DiagnosticBag());
            var module = previousGeneration.OriginalMetadata;
            var compilation = sourceAssembly.DeclaringCompilation;
            var metadataAssembly = compilation.GetBoundReferenceManager().CreatePEAssemblyForAssemblyMetadata(AssemblyMetadata.Create(module), MetadataImportOptions.All);
            var metadataDecoder = new Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.MetadataDecoder(metadataAssembly.PrimaryModule);

            previousGeneration = EnsureInitialized(previousGeneration, metadataDecoder);

            var matchToMetadata = new SymbolMatcher(previousGeneration.AnonymousTypeMap, sourceAssembly, context, metadataAssembly);

            SymbolMatcher matchToPrevious = null;
            if (previousGeneration.Ordinal > 0)
            {
                var previousAssembly = ((CSharpCompilation)previousGeneration.Compilation).SourceAssembly;
                var previousContext = new Microsoft.CodeAnalysis.Emit.Context((PEModuleBuilder)previousGeneration.PEModuleBuilder, null, new DiagnosticBag());
                matchToPrevious = new SymbolMatcher(previousGeneration.AnonymousTypeMap, sourceAssembly, context, previousAssembly, previousContext);
            }

            this.previousDefinitions = new CSharpCompilation.DefinitionMap(previousGeneration.OriginalMetadata.Module, metadataDecoder, matchToMetadata, matchToPrevious, GenerateMethodMap(edits));
            this.previousGeneration = previousGeneration;
            this.changes = new Microsoft.CodeAnalysis.Emit.SymbolChanges(this.previousDefinitions, edits);
        }

        private static IReadOnlyDictionary<Microsoft.CodeAnalysis.Emit.AnonymousTypeKey, Microsoft.CodeAnalysis.Emit.AnonymousTypeValue> GetAnonymousTypeMap(
            MetadataReader reader,
            Symbols.Metadata.PE.MetadataDecoder metadataDecoder)
        {
            var result = new Dictionary<Microsoft.CodeAnalysis.Emit.AnonymousTypeKey, Microsoft.CodeAnalysis.Emit.AnonymousTypeValue>();
            foreach (var handle in reader.TypeDefinitions)
            {
                var def = reader.GetTypeDefinition(handle);
                if (!def.Namespace.IsNil)
                {
                    continue;
                }
                if (!reader.StringStartsWith(def.Name, GeneratedNames.AnonymousNamePrefix))
                {
                    continue;
                }
                var metadataName = reader.GetString(def.Name);
                short arity;
                var name = MetadataHelpers.InferTypeArityAndUnmangleMetadataName(metadataName, out arity);
                int index;
                if (GeneratedNames.TryParseAnonymousTypeTemplateName(name, out index))
                {
                    var builder = ArrayBuilder<string>.GetInstance();
                    if (TryGetAnonymousTypeKey(reader, def, builder))
                    {
                        var type = (NamedTypeSymbol)metadataDecoder.GetTypeOfToken(handle);
                        var key = new Microsoft.CodeAnalysis.Emit.AnonymousTypeKey(builder.ToImmutable());
                        var value = new Microsoft.CodeAnalysis.Emit.AnonymousTypeValue(name, index, type);
                        result.Add(key, value);
                    }
                    builder.Free();
                }
            }
            return result;
        }

        private static bool TryGetAnonymousTypeKey(
            MetadataReader reader,
            TypeDefinition def,
            ArrayBuilder<string> builder)
        {
            foreach (var typeParameterHandle in def.GetGenericParameters())
            {
                var typeParameter = reader.GetGenericParameter(typeParameterHandle);
                string fieldName;
                if (!GeneratedNames.TryParseAnonymousTypeParameterName(reader.GetString(typeParameter.Name), out fieldName))
                {
                    return false;
                }
                builder.Add(fieldName);
            }
            return true;
        }

        private static Microsoft.CodeAnalysis.Emit.EmitBaseline EnsureInitialized(
            Microsoft.CodeAnalysis.Emit.EmitBaseline previousGeneration,
            Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.MetadataDecoder metadataDecoder)
        {
            if (previousGeneration.AnonymousTypeMap != null)
            {
                return previousGeneration;
            }

            var anonymousTypeMap = GetAnonymousTypeMap(previousGeneration.MetadataReader, metadataDecoder);
            return previousGeneration.With(
                previousGeneration.Compilation,
                previousGeneration.PEModuleBuilder,
                previousGeneration.Ordinal,
                previousGeneration.EncId,
                previousGeneration.TypesAdded,
                previousGeneration.EventsAdded,
                previousGeneration.FieldsAdded,
                previousGeneration.MethodsAdded,
                previousGeneration.PropertiesAdded,
                previousGeneration.EventMapAdded,
                previousGeneration.PropertyMapAdded,
                previousGeneration.TableEntriesAdded,
                blobStreamLengthAdded: previousGeneration.BlobStreamLengthAdded,
                stringStreamLengthAdded: previousGeneration.StringStreamLengthAdded,
                userStringStreamLengthAdded: previousGeneration.UserStringStreamLengthAdded,
                guidStreamLengthAdded: previousGeneration.GuidStreamLengthAdded,
                anonymousTypeMap: anonymousTypeMap,
                localsForMethodsAddedOrChanged: previousGeneration.LocalsForMethodsAddedOrChanged,
                localNames: previousGeneration.LocalNames);
        }

        internal override Microsoft.CodeAnalysis.Emit.EmitBaseline PreviousGeneration
        {
            get { return this.previousGeneration; }
        }

        internal override Microsoft.CodeAnalysis.Emit.DefinitionMap PreviousDefinitions
        {
            get { return this.previousDefinitions; }
        }

        internal override bool SupportsPrivateImplClass
        {
            get
            {
                // Disable <PrivateImplementationDetails> in ENC since the
                // CLR does not support adding non-private members.
                return false;
            }
        }

        internal override IReadOnlyDictionary<Microsoft.CodeAnalysis.Emit.AnonymousTypeKey, Microsoft.CodeAnalysis.Emit.AnonymousTypeValue> GetAnonymousTypeMap()
        {
            var anonymousTypes = this.Compilation.AnonymousTypeManager.GetAnonymousTypeMap();
            // Should contain all entries in previous generation.
            Debug.Assert(this.previousGeneration.AnonymousTypeMap.All(p => anonymousTypes.ContainsKey(p.Key)));
            return anonymousTypes;
        }

        internal override bool TryGetAnonymousTypeName(NamedTypeSymbol template, out string name, out int index)
        {
            Debug.Assert(this.Compilation == template.DeclaringCompilation);
            return this.previousDefinitions.TryGetAnonymousTypeName(template, out name, out index);
        }

        internal override Microsoft.CodeAnalysis.Emit.SymbolChanges Changes
        {
            get { return this.changes; }
        }

        internal override IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypesCore(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this.changes.GetTopLevelTypes(context);
        }

        internal override void OnCreatedIndices(DiagnosticBag diagnostics)
        {
            var embeddedTypesManager = this.EmbeddedTypesManagerOpt;
            if (embeddedTypesManager != null)
            {
                foreach (var embeddedType in embeddedTypesManager.EmbeddedTypesMap.Keys)
                {
                    diagnostics.Add(new CSDiagnosticInfo(ErrorCode.ERR_EnCNoPIAReference, embeddedType), Location.None);
                }
            }
        }

        internal override bool IsENCDelta
        {
            get { return true; }
        }

        private static IReadOnlyDictionary<MethodSymbol, CSharpCompilation.MethodDefinitionEntry> GenerateMethodMap(IEnumerable<Microsoft.CodeAnalysis.Emit.SemanticEdit> edits)
        {
            var methodMap = new Dictionary<MethodSymbol, CSharpCompilation.MethodDefinitionEntry>();
            foreach (var edit in edits)
            {
                if (edit.Kind == CodeAnalysis.Emit.SemanticEditKind.Update)
                {
                    var method = edit.NewSymbol as MethodSymbol;
                    if ((object)method != null)
                    {
                        methodMap.Add(method, new CSharpCompilation.MethodDefinitionEntry(
                            (MethodSymbol)edit.OldSymbol,
                            edit.PreserveLocalVariables,
                            edit.SyntaxMap));
                    }
                }
            }
            return methodMap;
        }
    }
}
