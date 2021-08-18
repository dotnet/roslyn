// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PdbTests : CSharpTestBase
    {
        [ClrOnlyFact(ClrOnlyReason.Pdb)]
        public void EmitToMemoryStreams()
        {
            var comp = CSharpCompilation.Create("Compilation", options: TestOptions.ReleaseDll);

            using (var output = new MemoryStream())
            {
                using (var outputPdb = new MemoryStream())
                {
                    using (var outputxml = new MemoryStream())
                    {
                        var result = comp.Emit(output, outputPdb, null);
                        Assert.True(result.Success);
                        result = comp.Emit(output, outputPdb);
                        Assert.True(result.Success);
                        result = comp.Emit(peStream: output, xmlDocumentationStream: null);
                        Assert.True(result.Success);
                        result = comp.Emit(peStream: output, pdbStream: outputPdb);
                        Assert.True(result.Success);
                        result = comp.Emit(output, outputPdb, null);
                        Assert.True(result.Success);
                        result = comp.Emit(output, outputPdb);
                        Assert.True(result.Success);
                        result = comp.Emit(output, outputPdb, outputxml);
                        Assert.True(result.Success);
                        result = comp.Emit(output, null, null, null);
                        Assert.True(result.Success);
                        result = comp.Emit(output);
                        Assert.True(result.Success);
                        result = comp.Emit(output, null, outputxml);
                        Assert.True(result.Success);
                        result = comp.Emit(output, xmlDocumentationStream: outputxml);
                        Assert.True(result.Success);
                        result = comp.Emit(output, xmlDocumentationStream: outputxml);
                        Assert.True(result.Success);
                        result = comp.Emit(output, options: null);
                        Assert.True(result.Success);
                        result = comp.Emit(output, options: EmitOptions.Default.WithHighEntropyVirtualAddressSpace(true));
                        Assert.True(result.Success);
                        result = comp.Emit(output, options: EmitOptions.Default.WithOutputNameOverride("goo"));
                        Assert.True(result.Success);
                        result = comp.Emit(output, options: EmitOptions.Default.WithPdbFilePath("goo.pdb"));
                        Assert.True(result.Success);
                    }
                }
            }
        }

        [ClrOnlyFact(ClrOnlyReason.Pdb)]
        public void EmitToBoundedStreams()
        {
            var pdbArray = new byte[100000];
            var pdbStream = new MemoryStream(pdbArray, 1, pdbArray.Length - 10);

            var peArray = new byte[100000];
            var peStream = new MemoryStream(peArray);

            var c = CSharpCompilation.Create("a",
                new[] { SyntaxFactory.ParseSyntaxTree("class C { static void Main() {} }") },
                new[] { MscorlibRef });

            var r = c.Emit(peStream, pdbStream);
            r.Diagnostics.Verify();
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void EmitToStreamWithNonZeroPosition()
        {
            var pdbStream = new MemoryStream();
            pdbStream.WriteByte(0x12);
            var peStream = new MemoryStream();
            peStream.WriteByte(0x12);

            var c = CSharpCompilation.Create("a",
                new[] { SyntaxFactory.ParseSyntaxTree("class C { static void Main() {} }") },
                new[] { MscorlibRef });

            var r = c.Emit(peStream, pdbStream);
            r.Diagnostics.Verify();

            AssertEx.Equal(new byte[] { 0x12, (byte)'M', (byte)'i', (byte)'c', (byte)'r', (byte)'o' }, pdbStream.GetBuffer().Take(6).ToArray());
            AssertEx.Equal(new byte[] { 0x12, (byte)'M', (byte)'Z' }, peStream.GetBuffer().Take(3).ToArray());
        }

        [ClrOnlyFact(ClrOnlyReason.Pdb)]
        public void EmitToNonSeekableStreams()
        {
            var peStream = new TestStream(canRead: false, canSeek: false, canWrite: true);
            var pdbStream = new TestStream(canRead: false, canSeek: false, canWrite: true);

            var c = CSharpCompilation.Create("a",
                new[] { SyntaxFactory.ParseSyntaxTree("class C { static void Main() {} }") },
                new[] { MscorlibRef });

            var r = c.Emit(peStream, pdbStream);
            r.Diagnostics.Verify();
        }

        [ClrOnlyFact(ClrOnlyReason.Pdb)]
        public void NegEmit()
        {
            var ops = TestOptions.ReleaseDll;
            var comp = CSharpCompilation.Create("Compilation", null, null, ops);
            using (MemoryStream output = new MemoryStream())
            {
                using (MemoryStream outputPdb = new MemoryStream())
                {
                    using (MemoryStream outputxml = new MemoryStream())
                    {
                        var result = comp.Emit(output, outputPdb, outputxml);
                        Assert.True(result.Success);
                    }
                }
            }
        }

    }
}
