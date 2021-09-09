// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBSourceLinkTests : CSharpPDBTestBase
    {
        [Theory]
        [MemberData(nameof(ExternalPdbFormats))]
        public void SourceLink(DebugInformationFormat format)
        {
            string source = @"
using System;

class C
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
";
            var sourceLinkBlob = Encoding.UTF8.GetBytes(@"
{
  ""documents"": {
     ""f:/build/*"" : ""https://raw.githubusercontent.com/my-org/my-project/1111111111111111111111111111111111111111/*""
  }
}
");

            var c = CreateCompilation(Parse(source, "f:/build/goo.cs"), options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(format), pdbStream: pdbStream, sourceLinkStream: new MemoryStream(sourceLinkBlob));

            var actualData = PdbValidation.GetSourceLinkData(pdbStream);
            AssertEx.Equal(sourceLinkBlob, actualData);
        }

        [Fact]
        public void SourceLink_Embedded()
        {
            string source = @"
using System;

class C
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
";
            var sourceLinkBlob = Encoding.UTF8.GetBytes(@"
{
  ""documents"": {
     ""f:/build/*"" : ""https://raw.githubusercontent.com/my-org/my-project/1111111111111111111111111111111111111111/*""
  }
}
");
            var c = CreateCompilation(Parse(source, "f:/build/goo.cs"), options: TestOptions.DebugDll);

            var peBlob = c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded), sourceLinkStream: new MemoryStream(sourceLinkBlob));

            using (var peReader = new PEReader(peBlob))
            {
                var embeddedEntry = peReader.ReadDebugDirectory().Single(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);

                using (var embeddedMetadataProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedEntry))
                {
                    var pdbReader = embeddedMetadataProvider.GetMetadataReader();

                    var actualBlob =
                        (from cdiHandle in pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                         let cdi = pdbReader.GetCustomDebugInformation(cdiHandle)
                         where pdbReader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.SourceLink
                         select pdbReader.GetBlobBytes(cdi.Value)).Single();

                    AssertEx.Equal(sourceLinkBlob, actualBlob);
                }
            }
        }

        [Theory]
        [MemberData(nameof(PdbFormats))]
        public void SourceLink_Errors(DebugInformationFormat format)
        {
            string source = @"
using System;

class C
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
";
            var sourceLinkStream = new TestStream(canRead: true, readFunc: (_, __, ___) => { throw new Exception("Error!"); });

            var c = CreateCompilation(Parse(source, "f:/build/goo.cs"), options: TestOptions.DebugDll);
            var pdbStream = format != DebugInformationFormat.Embedded ? new MemoryStream() : null;
            var result = c.Emit(new MemoryStream(), pdbStream, options: EmitOptions.Default.WithDebugInformationFormat(format), sourceLinkStream: sourceLinkStream);
            result.Diagnostics.Verify(
                // error CS0041: Unexpected error writing debug information -- 'Error!'
                Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("Error!").WithLocation(1, 1));
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SourceLink_Errors_NotSupportedByPdbWriter()
        {
            string source = @"
using System;

class C
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
";
            var c = CreateCompilation(Parse(source, "f:/build/goo.cs"), options: TestOptions.DebugDll);

            var result = c.Emit(
                peStream: new MemoryStream(),
                metadataPEStream: null,
                pdbStream: new MemoryStream(),
                xmlDocumentationStream: null,
                cancellationToken: default,
                win32Resources: null,
                manifestResources: null,
                options: EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Pdb),
                debugEntryPoint: null,
                sourceLinkStream: new MemoryStream(new byte[] { 1, 2, 3 }),
                embeddedTexts: null,
                rebuildData: null,
                testData: new CompilationTestData()
                {
                    SymWriterFactory = metadataProvider => new SymUnmanagedWriterWithoutSourceLinkSupport(metadataProvider)
                });

            result.Diagnostics.Verify(
                // error CS0041: Unexpected error writing debug information -- 'Windows PDB writer doesn't support SourceLink feature: '<lib name>''
                Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments(string.Format(CodeAnalysisResources.SymWriterDoesNotSupportSourceLink, "<lib name>")));
        }

        [Theory]
        [MemberData(nameof(ExternalPdbFormats))]
        public void SourceLink_Empty(DebugInformationFormat format)
        {
            string source = @"
using System;

class C
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
";
            var sourceLinkBlob = new byte[0];

            var c = CreateCompilation(Parse(source, "f:/build/goo.cs"), options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(format), pdbStream: pdbStream, sourceLinkStream: new MemoryStream(sourceLinkBlob));
            pdbStream.Position = 0;
            var bs = Roslyn.Utilities.StreamExtensions.ReadAllBytes(pdbStream);
            var actualData = PdbValidation.GetSourceLinkData(pdbStream);
            AssertEx.Equal(sourceLinkBlob, actualData);
        }
    }
}
