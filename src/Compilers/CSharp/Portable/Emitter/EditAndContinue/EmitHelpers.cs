// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal static class EmitHelpers
    {
        internal static EmitDifferenceResult EmitDifference(
            CSharpCompilation compilation,
            EmitBaseline baseline,
            IEnumerable<SemanticEdit> edits,
            Func<ISymbol, bool> isAddedSymbol,
            Stream metadataStream,
            Stream ilStream,
            Stream pdbStream,
            ICollection<MethodDefinitionHandle> updatedMethods,
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
                // TODO:
                // return MakeEmitResult(success: false, diagnostics: ..., baseline: null);
                throw;
            }

            var pdbName = FileNameUtilities.ChangeExtension(compilation.SourceModule.Name, "pdb");
            var diagnostics = DiagnosticBag.GetInstance();

            var emitOptions = EmitOptions.Default;
            string runtimeMDVersion = compilation.GetRuntimeMetadataVersion(emitOptions, diagnostics);
            var serializationProperties = compilation.ConstructModuleSerializationProperties(emitOptions, runtimeMDVersion, moduleVersionId);
            var manifestResources = SpecializedCollections.EmptyEnumerable<ResourceDescription>();

            var moduleBeingBuilt = new PEDeltaAssemblyBuilder(
                compilation.SourceAssembly,
                emitOptions: emitOptions,
                outputKind: compilation.Options.OutputKind,
                serializationProperties: serializationProperties,
                manifestResources: manifestResources,
                previousGeneration: baseline,
                edits: edits,
                isAddedSymbol: isAddedSymbol);

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
                win32Resources: null,
                xmlDocStream: null,
                generateDebugInfo: true,
                diagnostics: diagnostics,
                filterOpt: changes.RequiresCompilation,
                cancellationToken: cancellationToken))
            {
                // Map the definitions from the previous compilation to the current compilation.
                // This must be done after compiling above since synthesized definitions
                // (generated when compiling method bodies) may be required.
                baseline = MapToCompilation(compilation, moduleBeingBuilt);

                var pdbOutputInfo = new Cci.PdbOutputInfo(pdbName, pdbStream);
                using (var pdbWriter = new Cci.PdbWriter(pdbOutputInfo, (testData != null) ? testData.SymWriterFactory : null))
                {
                    var context = new EmitContext(moduleBeingBuilt, null, diagnostics);
                    var encId = Guid.NewGuid();

                    try
                    {
                        var writer = new DeltaMetadataWriter(
                            context,
                            compilation.MessageProvider,
                            baseline,
                            encId,
                            definitionMap,
                            changes,
                            cancellationToken);

                        Cci.MetadataSizes metadataSizes;
                        writer.WriteMetadataAndIL(pdbWriter, metadataStream, ilStream, out metadataSizes);
                        writer.GetMethodTokens(updatedMethods);

                        bool hasErrors = diagnostics.HasAnyErrors();

                        return new EmitDifferenceResult(
                            success: !hasErrors,
                            diagnostics: diagnostics.ToReadOnlyAndFree(),
                            baseline: hasErrors ? null : writer.GetDelta(baseline, compilation, encId, metadataSizes));
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

            var currentSynthesizedMembers = moduleBeingBuilt.GetSynthesizedMembers();

            // Mapping from previous compilation to the current.
            var anonymousTypeMap = moduleBeingBuilt.GetAnonymousTypeMap();
            var sourceAssembly = ((CSharpCompilation)previousGeneration.Compilation).SourceAssembly;
            var sourceContext = new EmitContext((PEModuleBuilder)previousGeneration.PEModuleBuilder, null, new DiagnosticBag());
            var otherContext = new EmitContext(moduleBeingBuilt, null, new DiagnosticBag());

            var matcher = new CSharpSymbolMatcher(
                anonymousTypeMap,
                sourceAssembly,
                sourceContext,
                compilation.SourceAssembly,
                otherContext,
                currentSynthesizedMembers);

            var mappedSynthesizedMembers = matcher.MapSynthesizedMembers(previousGeneration.SynthesizedMembers, currentSynthesizedMembers);

            // TODO: can we reuse some data from the previos matcher?
            var matcherWithAllSynthesizedMembers = new CSharpSymbolMatcher(
                anonymousTypeMap,
                sourceAssembly,
                sourceContext,
                compilation.SourceAssembly,
                otherContext,
                mappedSynthesizedMembers);

            return matcherWithAllSynthesizedMembers.MapBaselineToCompilation(
                previousGeneration,
                compilation,
                moduleBeingBuilt,
                mappedSynthesizedMembers);
        }
    }
}
