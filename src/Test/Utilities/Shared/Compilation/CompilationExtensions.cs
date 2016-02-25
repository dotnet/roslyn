// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class CompilationExtensions
    {
        internal static ImmutableArray<byte> EmitToArray(
            this Compilation compilation,
            EmitOptions options = null,
            CompilationTestData testData = null,
            DiagnosticDescription[] expectedWarnings = null,
            Stream pdbStream = null,
            IMethodSymbol debugEntryPoint = null)
        {
            var stream = new MemoryStream();

            if (pdbStream == null && compilation.Options.OptimizationLevel == OptimizationLevel.Debug)
            {
                if (MonoHelpers.IsRunningOnMono())
                {
                    options = (options ?? EmitOptions.Default).WithDebugInformationFormat(DebugInformationFormat.PortablePdb);
                }

                pdbStream = new MemoryStream();
            }

            var emitResult = compilation.Emit(
                peStream: stream,
                pdbStream: pdbStream,
                xmlDocumentationStream: null,
                win32Resources: null,
                manifestResources: null,
                options: options,
                debugEntryPoint: debugEntryPoint,
                testData: testData,
                cancellationToken: default(CancellationToken));

            Assert.True(emitResult.Success, "Diagnostics:\r\n" + string.Join("\r\n", emitResult.Diagnostics.Select(d => d.ToString())));

            if (expectedWarnings != null)
            {
                emitResult.Diagnostics.Verify(expectedWarnings);
            }

            return stream.ToImmutable();
        }

        public static MemoryStream EmitToStream(this Compilation compilation, EmitOptions options = null, DiagnosticDescription[] expectedWarnings = null)
        {
            var stream = new MemoryStream();
            var emitResult = compilation.Emit(stream, options: options);
            Assert.True(emitResult.Success, "Diagnostics: " + string.Join(", ", emitResult.Diagnostics.Select(d => d.ToString())));

            if (expectedWarnings != null)
            {
                emitResult.Diagnostics.Verify(expectedWarnings);
            }

            stream.Position = 0;
            return stream;
        }

        public static MetadataReference EmitToImageReference(
            this Compilation comp,
            EmitOptions options = null,
            bool embedInteropTypes = false,
            ImmutableArray<string> aliases = default(ImmutableArray<string>),
            DiagnosticDescription[] expectedWarnings = null)
        {
            var image = comp.EmitToArray(options, expectedWarnings: expectedWarnings);
            if (comp.Options.OutputKind == OutputKind.NetModule)
            {
                return ModuleMetadata.CreateFromImage(image).GetReference(display: comp.MakeSourceModuleName());
            }
            else
            {
                return AssemblyMetadata.CreateFromImage(image).GetReference(aliases: aliases, embedInteropTypes: embedInteropTypes, display: comp.MakeSourceAssemblySimpleName());
            }
        }

        internal static CompilationDifference EmitDifference(
            this Compilation compilation,
            EmitBaseline baseline,
            ImmutableArray<SemanticEdit> edits,
            IEnumerable<ISymbol> allAddedSymbols = null,
            CompilationTestData testData = null)
        {
            testData = testData ?? new CompilationTestData();
            var isAddedSymbol = new Func<ISymbol, bool>(s => allAddedSymbols?.Contains(s) ?? false);

            var pdbName = Path.ChangeExtension(compilation.SourceModule.Name, "pdb");

            // keep the stream open, it's passed to CompilationDifference
            var pdbStream = new MemoryStream();

            using (MemoryStream mdStream = new MemoryStream(), ilStream = new MemoryStream())
            {
                var updatedMethods = new List<MethodDefinitionHandle>();

                var result = compilation.EmitDifference(
                    baseline,
                    edits,
                    isAddedSymbol,
                    mdStream,
                    ilStream,
                    pdbStream,
                    updatedMethods,
                    testData,
                    default(CancellationToken));

                pdbStream.Seek(0, SeekOrigin.Begin);

                return new CompilationDifference(
                    mdStream.ToImmutable(),
                    ilStream.ToImmutable(),
                    pdbStream,
                    testData,
                    result,
                    updatedMethods.ToImmutableArray());
            }
        }

        internal static void VerifyAssemblyVersionsAndAliases(this Compilation compilation, params string[] expectedAssembliesAndAliases)
        {
            var actual = compilation.GetBoundReferenceManager().GetReferencedAssemblyAliases().
               Select(t => $"{t.Item1.Identity.Name}, Version={t.Item1.Identity.Version}{(t.Item2.IsEmpty ? "" : ": " + string.Join(",", t.Item2))}");

            AssertEx.Equal(expectedAssembliesAndAliases, actual, itemInspector: s => '"' + s + '"');
        }

        internal static void VerifyAssemblyAliases(this Compilation compilation, params string[] expectedAssembliesAndAliases)
        {
            var actual = compilation.GetBoundReferenceManager().GetReferencedAssemblyAliases().
               Select(t => $"{t.Item1.Identity.Name}{(t.Item2.IsEmpty ? "" : ": " + string.Join(",", t.Item2))}");

            AssertEx.Equal(expectedAssembliesAndAliases, actual, itemInspector: s => '"' + s + '"');
        }
    }
}
