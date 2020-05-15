// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.InternalUtilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public partial class DeterministicBuildCompilationTests
    {
        private class TestMetadataReferenceInfo
        {
            public readonly Compilation Compilation;
            public readonly TestMetadataReference MetadataReference;
            public readonly MetadataReferenceInfo MetadataReferenceInfo;

            public TestMetadataReferenceInfo(
                Compilation compilation,
                TestMetadataReference metadataReference,
                MetadataReferenceInfo metadataReferenceInfo)
            {
                Compilation = compilation;
                MetadataReference = metadataReference;
                MetadataReferenceInfo = metadataReferenceInfo;
            }

            public static TestMetadataReferenceInfo Create(string code, string fullPath, EmitOptions emitOptions)
            {
                var compilation = CreateCompilation(code, options: TestOptions.DebugDll);
                using var referenceStream = compilation.EmitToStream(emitOptions);

                var metadata = AssemblyMetadata.CreateFromStream(referenceStream);
                var metadataReference = new TestMetadataReference(metadata, fullPath: fullPath);

                using var peReader = new PEReader(referenceStream);

                var metadataReferenceInfo = new MetadataReferenceInfo(
                    peReader.GetTimestamp(),
                    peReader.GetSizeOfImage(),
                    PathUtilities.GetFileName(fullPath),
                    peReader.GetMvid(),
                    metadataReference.Properties.Aliases,
                    metadataReference.Properties.Kind,
                    metadataReference.Properties.EmbedInteropTypes);

                return new TestMetadataReferenceInfo(
                    compilation,
                    metadataReference,
                    metadataReferenceInfo);
            }
        }
    }
}
