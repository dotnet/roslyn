// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal static class EmitHelpers
    {
        internal static EmitDifferenceResult EmitDifference(
            CSharpCompilation compilation,
            EmitBaseline baseline,
            IEnumerable<SemanticEdit> edits,
            Stream metadataStream,
            Stream ilStream,
            Stream pdbStream,
            ICollection<uint> updatedMethodTokens,
            CompilationTestData testData,
            CancellationToken cancellationToken)
        {
            Guid moduleVersionId;
            try
            {
                moduleVersionId = baseline.OriginalMetadata.GetModuleVersionId();
            }
            catch (BadImageFormatException)
            {
                // return MakeEmitResult(success: false, diagnostics: ..., baseline: null);
                throw;
            }

            var pdbName = PathUtilities.ChangeExtension(compilation.SourceModule.Name, "pdb");


            var diagnostics = DiagnosticBag.GetInstance();
            string runtimeMDVersion = compilation.GetRuntimeMetadataVersion(diagnostics);
            var serializationProperties = compilation.ConstructModuleSerializationProperties(runtimeMDVersion, moduleVersionId);
            var manifestResources = SpecializedCollections.EmptyEnumerable<ResourceDescription>();

            var moduleBeingBuilt = new PEDeltaAssemblyBuilder(
                compilation.SourceAssembly,
                outputName: null,
                outputKind: compilation.Options.OutputKind,
                serializationProperties: serializationProperties,
                manifestResources: manifestResources,
                assemblySymbolMapper: null,
                previousGeneration: baseline,
                edits: edits);

            if (testData != null)
            {
                moduleBeingBuilt.SetMethodTestData(testData.Methods);
                testData.Module = moduleBeingBuilt;
            }

            baseline = moduleBeingBuilt.PreviousGeneration;

            var definitionMap = moduleBeingBuilt.PreviousDefinitions;
            var changes = moduleBeingBuilt.Changes;

            if (compilation.Compile(
                moduleBeingBuilt,
                outputName: null,
                win32Resources: null,
                xmlDocStream: null,
                cancellationToken: cancellationToken,
                generateDebugInfo: true,
                diagnostics: diagnostics,
                filterOpt: changes.RequiresCompilation))
            {
                // Map the definitions from the previous compilation to the current compilation.
                // This must be done after compiling above since synthesized definitions
                // (generated when compiling method bodies) may be required.
                baseline = MapToCompilation(compilation, moduleBeingBuilt);

                using (var pdbWriter = new Cci.PdbWriter(pdbName, pdbStream, (testData != null) ? testData.SymWriterFactory : null))
                {
                    var context = new EmitContext(moduleBeingBuilt, null, diagnostics);
                    var encId = Guid.NewGuid();

                    try
                    {
                        var writer = new DeltaPeWriter(
                            context,
                            compilation.MessageProvider,
                            pdbWriter,
                            baseline,
                            encId,
                            definitionMap,
                            changes,
                            cancellationToken);
                    
                        writer.WriteMetadataAndIL(metadataStream, ilStream);
                        writer.GetMethodTokens(updatedMethodTokens);

                        return new EmitDifferenceResult(
                            success: true,
                            diagnostics: diagnostics.ToReadOnlyAndFree(),
                            baseline: writer.GetDelta(baseline, compilation, encId));
                    }
                    catch (Cci.PdbWritingException e)
                    {
                        diagnostics.Add(ErrorCode.FTL_DebugEmitFailure, Location.None, e.Message);
                    }
                    catch (PermissionSetFileReadException e)
                    {
                        diagnostics.Add(ErrorCode.ERR_PermissionSetAttributeFileReadError, Location.None, e.FileName, e.PropertyName, e.Message);
                    }
                }
            }

            return new EmitDifferenceResult(success: false, diagnostics: diagnostics.ToReadOnlyAndFree(), baseline: null);
        }

        /// <summary>
        /// Return a version of the baseline with all definitions mapped to this compilation.
        /// Definitions from the initial generation, from metadata, are not mapped since
        /// the initial generation is always included as metadata. That is, the symbols from
        /// types, methods, ... in the TypesAdded, MethodsAdded, ... collections are replaced
        /// by the corresponding symbols from the current compilation.
        /// </summary>
        private static EmitBaseline MapToCompilation(
            CSharpCompilation compilation,
            PEDeltaAssemblyBuilder moduleBeingBuilt)
        {
            var previousGeneration = moduleBeingBuilt.PreviousGeneration;
            Debug.Assert(previousGeneration.Compilation != compilation);

            if (previousGeneration.Ordinal == 0)
            {
                // Initial generation, nothing to map. (Since the initial generation
                // is always loaded from metadata in the context of the current
                // compilation, there's no separate mapping step.)
                return previousGeneration;
            }

            var map = new SymbolMatcher(
                moduleBeingBuilt.GetAnonymousTypeMap(),
                ((CSharpCompilation)previousGeneration.Compilation).SourceAssembly,
                new EmitContext((PEModuleBuilder)previousGeneration.PEModuleBuilder, null, new DiagnosticBag()),
                compilation.SourceAssembly,
                new EmitContext((Cci.IModule)moduleBeingBuilt, null, new DiagnosticBag()));

            // Map all definitions to this compilation.
            var typesAdded = MapDefinitions(map, previousGeneration.TypesAdded);
            var eventsAdded = MapDefinitions(map, previousGeneration.EventsAdded);
            var fieldsAdded = MapDefinitions(map, previousGeneration.FieldsAdded);
            var methodsAdded = MapDefinitions(map, previousGeneration.MethodsAdded);
            var propertiesAdded = MapDefinitions(map, previousGeneration.PropertiesAdded);

            // Map anonymous types to this compilation.
            var anonymousTypeMap = new Dictionary<AnonymousTypeKey, AnonymousTypeValue>();
            foreach (var pair in previousGeneration.AnonymousTypeMap)
            {
                var key = pair.Key;
                var value = pair.Value;
                var type = (Cci.ITypeDefinition)map.MapDefinition(value.Type);
                Debug.Assert(type != null);
                anonymousTypeMap.Add(key, new AnonymousTypeValue(value.Name, value.UniqueIndex, type));
            }

            // Map locals (specifically, local types) to this compilation.
            var locals = new Dictionary<uint, ImmutableArray<EncLocalInfo>>();
            foreach (var pair in previousGeneration.LocalsForMethodsAddedOrChanged)
            {
                locals.Add(pair.Key, pair.Value.SelectAsArray((l, m) => MapLocalInfo(m, l), map));
            }

            return previousGeneration.With(
                compilation,
                moduleBeingBuilt,
                previousGeneration.Ordinal,
                previousGeneration.EncId,
                typesAdded,
                eventsAdded,
                fieldsAdded,
                methodsAdded,
                propertiesAdded,
                eventMapAdded: previousGeneration.EventMapAdded,
                propertyMapAdded: previousGeneration.PropertyMapAdded,
                methodImplsAdded: previousGeneration.MethodImplsAdded,
                tableEntriesAdded: previousGeneration.TableEntriesAdded,
                blobStreamLengthAdded: previousGeneration.BlobStreamLengthAdded,
                stringStreamLengthAdded: previousGeneration.StringStreamLengthAdded,
                userStringStreamLengthAdded: previousGeneration.UserStringStreamLengthAdded,
                guidStreamLengthAdded: previousGeneration.GuidStreamLengthAdded,
                anonymousTypeMap: anonymousTypeMap,
                localsForMethodsAddedOrChanged: locals,
                localNames: previousGeneration.LocalNames);
        }

        private static IReadOnlyDictionary<K, V> MapDefinitions<K, V>(
            SymbolMatcher map,
            IReadOnlyDictionary<K, V> items)
            where K : Cci.IDefinition
        {
            var result = new Dictionary<K, V>();
            foreach (var pair in items)
            {
                var key = (K)map.MapDefinition(pair.Key);
                // Result may be null if the definition was deleted, or if the definition
                // was synthesized (e.g.: an iterator type) and the method that generated
                // the synthesized definition was unchanged and not recompiled.
                if (key != null)
                {
                    result.Add(key, pair.Value);
                }
            }
            return result;
        }

        private static EncLocalInfo MapLocalInfo(
            SymbolMatcher map,
            EncLocalInfo localInfo)
        {
            Debug.Assert(!localInfo.IsDefault);
            var type = map.MapReference(localInfo.Type);
            Debug.Assert(type != null);
            return new EncLocalInfo(localInfo.Offset, type, localInfo.Constraints, localInfo.TempKind);
        }
    }
}
