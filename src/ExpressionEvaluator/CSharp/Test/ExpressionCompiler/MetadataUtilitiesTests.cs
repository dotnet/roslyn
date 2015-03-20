// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class MetadataUtilitiesTests : CSharpTestBase
    {
        /// <summary>
        /// The same assembly loaded multiple times.
        /// </summary>
        [Fact]
        public void AssemblyDuplicateReferences()
        {
            var sourceA =
@"public class A
{
}";
            var sourceB =
@"public class B : A
{
}";
            var compilationA = CreateCompilationWithMscorlib(
                sourceA,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var referenceA = AssemblyMetadata.CreateFromImage(compilationA.EmitToArray()).GetReference();
            var compilationB = CreateCompilationWithMscorlib(
                sourceB,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName(),
                references: new MetadataReference[] { referenceA });
            var referenceB = AssemblyMetadata.CreateFromImage(compilationB.EmitToArray()).GetReference();
            var references = ImmutableArray.Create(
                    MscorlibRef,
                    referenceA,
                    referenceB,
                    referenceB,
                    referenceA,
                    referenceA);
            using (var runtime = CreateRuntime(references))
            {
                var blocks = GetMetadataBlocks(runtime);
                var actualReferences = blocks.MakeAssemblyReferences();
                var expectedReferences = ImmutableArray.Create(
                    MscorlibRef,
                    referenceA,
                    referenceB);
                AssertEx.Equal(expectedReferences, actualReferences);
            }
        }

        private static RuntimeInstance CreateRuntime(ImmutableArray<MetadataReference> references)
        {
            var modules = references.SelectAsArray(r => r.ToModuleInstance(fullImage: null, symReader: null, includeLocalSignatures: false));
            return new RuntimeInstance(modules);
        }

        private static ImmutableArray<MetadataBlock> GetMetadataBlocks(RuntimeInstance runtime)
        {
            return runtime.Modules.SelectAsArray(m => m.MetadataBlock);
        }

        private static ImmutableArray<string> GetNames(ImmutableArray<MetadataBlock> blocks)
        {
            return blocks.SelectAsArray(GetName);
        }

        private static string GetName(MetadataBlock block)
        {
            var metadata = ModuleMetadata.CreateFromMetadata(block.Pointer, block.Size, includeEmbeddedInteropTypes: true);
            var identity = metadata.MetadataReader.ReadAssemblyIdentityOrThrow();
            return identity.GetDisplayName();
        }
    }
}
