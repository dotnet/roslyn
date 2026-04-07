// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.DiaSymReader;
using Microsoft.DiaSymReader.PortablePdb;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

public sealed class EditAndContinueMethodDebugInfoReaderTests
{
    private sealed class DummyMetadataImportProvider : IMetadataImportProvider
    {
        public object GetMetadataImport() => throw new NotImplementedException();
    }

    [Fact]
    public void Create_Errors()
    {
        Assert.Throws<ArgumentNullException>(() => EditAndContinueDebugInfoReader.Create((ISymUnmanagedReader5)null));
        Assert.Throws<ArgumentNullException>(() => EditAndContinueDebugInfoReader.Create((MetadataReader)null));
        Assert.Throws<ArgumentNullException>(() => EditAndContinueDebugInfoReader.Create(null, 1));

        var mockSymReader = new Mock<ISymUnmanagedReader5>(MockBehavior.Strict).Object;
        Assert.Throws<ArgumentOutOfRangeException>(() => EditAndContinueDebugInfoReader.Create(mockSymReader, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => EditAndContinueDebugInfoReader.Create(mockSymReader, -1));
    }

    [Theory]
    [InlineData(DebugInformationFormat.PortablePdb, true)]
    [InlineData(DebugInformationFormat.PortablePdb, false)]
    [InlineData(DebugInformationFormat.Pdb, true)]
    public void DebugInfo(DebugInformationFormat format, bool useSymReader)
    {
        var source = """

            using System;
            delegate void D();
            class C
            {
                public static void Main()
                {
                    int x = 1;
                    D d = () => Console.Write(x);
                    d();
                }
            }

            """;
        var tree = CSharpTestSource.Parse(source, path: "/a/c.cs", options: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), checksumAlgorithm: SourceHashAlgorithm.Sha1);
        var compilation = CSharpTestBase.CreateCompilationWithMscorlib40AndSystemCore(tree, options: TestOptions.DebugDll);

        var pdbStream = new MemoryStream();
        compilation.EmitToArray(new EmitOptions(debugInformationFormat: format), pdbStream: pdbStream);
        pdbStream.Position = 0;

        DebugInformationReaderProvider provider;
        EditAndContinueDebugInfoReader reader;

        if (format == DebugInformationFormat.PortablePdb && useSymReader)
        {
            var pdbStreamCom = SymUnmanagedStreamFactory.CreateStream(pdbStream);
            var metadataImportProvider = new DummyMetadataImportProvider();
            Assert.Equal(0, new SymBinder().GetReaderFromPdbStream(metadataImportProvider, pdbStreamCom, out var symReader));
            reader = EditAndContinueDebugInfoReader.Create((ISymUnmanagedReader5)symReader, version: 1);
        }
        else
        {
            provider = DebugInformationReaderProvider.CreateFromStream(pdbStream);
            reader = provider.CreateEditAndContinueDebugInfoReader();
        }

        // Main method
        var debugInfo = reader.GetDebugInfo(MetadataTokens.MethodDefinitionHandle(5));
        Assert.Equal(0, debugInfo.GetMethodOrdinal());
        AssertEx.Equal(["Offset=0 Ordinal=0 Kind=LambdaDisplayClass", "Offset=33 Ordinal=0 Kind=UserDefined"], debugInfo.InspectLocalSlots());
        AssertEx.Equal(["Offset=43 Id=0#0 Closure=0"], debugInfo.InspectLambdas());
        AssertEx.Equal(["Offset=0 Id=0#0"], debugInfo.InspectClosures());

        var localSig = reader.GetLocalSignature(MetadataTokens.MethodDefinitionHandle(5));
        Assert.Equal(MetadataTokens.StandaloneSignatureHandle(1), localSig);

        // method without debug information:
        debugInfo = reader.GetDebugInfo(MetadataTokens.MethodDefinitionHandle(1));
        Assert.Equal(-1, debugInfo.GetMethodOrdinal());
        Assert.Null(debugInfo.InspectLocalSlots());
        Assert.Null(debugInfo.InspectLambdas());
        Assert.Null(debugInfo.InspectClosures());

        localSig = reader.GetLocalSignature(MetadataTokens.MethodDefinitionHandle(1));
        Assert.Equal(default, localSig);

        // document checksums:
        Assert.False(reader.TryGetDocumentChecksum("/b/c.cs", out _, out _));
        Assert.False(reader.TryGetDocumentChecksum("/a/d.cs", out _, out _));
        Assert.False(reader.TryGetDocumentChecksum("/A/C.cs", out _, out _));

        Assert.True(reader.TryGetDocumentChecksum("/a/c.cs", out var actualChecksum, out var actualAlgorithm));
        Assert.Equal("21-C8-B2-D7-A3-6B-49-C7-57-DF-67-B8-1F-75-DF-6A-64-FD-59-22", BitConverter.ToString([.. actualChecksum]));
        Assert.Equal(new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460"), actualAlgorithm);
    }
}
