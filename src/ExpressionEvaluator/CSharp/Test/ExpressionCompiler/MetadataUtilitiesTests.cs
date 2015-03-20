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
        /// MakeAssemblyReferences should only drop duplicate
        /// assemblies if the assemblies are strong-named.
        /// </summary>
        [WorkItem(1141029)]
        [Fact]
        public void DuplicateAssemblyReferences()
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
            var identityA = referenceA.GetAssemblyIdentity();

            var compilationB = CreateCompilationWithMscorlib(
                sourceB,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName(),
                references: new MetadataReference[] { referenceA });
            var referenceB = AssemblyMetadata.CreateFromImage(compilationB.EmitToArray()).GetReference();
            var identityB = referenceB.GetAssemblyIdentity();

            var mscorlibIdentity = MscorlibRef.GetAssemblyIdentity();
            var systemRefIdentity = SystemRef.GetAssemblyIdentity();
            var systemRef20Identity = SystemRef_v20.GetAssemblyIdentity();

            // Non-strong-named duplicates.
            VerifyAssemblyReferences(
                ImmutableArray.Create(MscorlibRef, referenceA, referenceB, referenceB, referenceA, referenceA),
                ImmutableArray.Create(mscorlibIdentity, identityA, identityB, identityB, identityA, identityA));
            // Strong-named duplicate, same version.
            VerifyAssemblyReferences(
                ImmutableArray.Create(SystemRef, MscorlibRef, SystemRef),
                ImmutableArray.Create(systemRefIdentity, mscorlibIdentity));
            // Strong-named duplicate, lower version first.
            VerifyAssemblyReferences(
                ImmutableArray.Create(SystemRef_v20, SystemRef, SystemRef_v20),
                ImmutableArray.Create(systemRefIdentity));
            // Strong-named duplicate, higher version first.
            VerifyAssemblyReferences(
                ImmutableArray.Create(SystemRef, SystemRef_v20, SystemRef_v20),
                ImmutableArray.Create(systemRefIdentity));
            // Strong-named and non-strong named duplicates.
            VerifyAssemblyReferences(
                ImmutableArray.Create(referenceA, SystemRef, MscorlibRef, SystemRef, referenceA),
                ImmutableArray.Create(identityA, systemRefIdentity, mscorlibIdentity, identityA));
        }

        private static void VerifyAssemblyReferences(ImmutableArray<MetadataReference> references, ImmutableArray<AssemblyIdentity> expectedIdentities)
        {
            var modules = references.SelectAsArray(r => r.ToModuleInstance(fullImage: null, symReader: null, includeLocalSignatures: false));
            using (var runtime = new RuntimeInstance(modules))
            {
                var blocks = runtime.Modules.SelectAsArray(m => m.MetadataBlock);
                var actualReferences = blocks.MakeAssemblyReferences();
                var actualIdentities = actualReferences.SelectAsArray(r => r.GetAssemblyIdentity());
                AssertEx.Equal(expectedIdentities, actualIdentities);
            }
        }
    }
}
