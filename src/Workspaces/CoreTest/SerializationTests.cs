﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public partial class SerializationTests : TestBase
    {
        private static Document CreateSolutionDocument(string sourceText)
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var solution = new AdhocWorkspace().CurrentSolution
                    .AddProject(pid, "test", "test", LanguageNames.CSharp)
                    .AddMetadataReference(pid, TestReferences.NetFx.v4_0_30319.mscorlib)
                    .AddDocument(did, "goo.cs", SourceText.From(sourceText));

            return solution.GetDocument(did);
        }

        [Fact]
        public void TestNameSimplificationAnnotationSerialization()
        {
            var text = @"public class C {}";
            var doc = CreateSolutionDocument(text);
            TestSymbolSerialization(doc, "C");
        }

        [Fact]
        public void VersionStamp_RoundTripText()
        {
            var versionStamp = VersionStamp.Create();

            using var writerStream = new MemoryStream();

            using (var writer = new ObjectWriter(writerStream, leaveOpen: true))
            {
                versionStamp.WriteTo(writer);
            }

            using var readerStream = new MemoryStream(writerStream.ToArray());
            using var reader = ObjectReader.TryGetReader(readerStream);
            var deserializedVersionStamp = VersionStamp.ReadFrom(reader);

            Assert.Equal(versionStamp, deserializedVersionStamp);
        }

        private static void TestSymbolSerialization(Document document, string symbolName)
        {
            var model = document.GetSemanticModelAsync().Result;
            var name = CS.SyntaxFactory.ParseName(symbolName);
            var symbol = model.GetSpeculativeSymbolInfo(0, name, SpeculativeBindingOption.BindAsExpression).Symbol;

            var root = (CS.Syntax.CompilationUnitSyntax)model.SyntaxTree.GetRoot();
            var annotation = SymbolAnnotation.Create(symbol);
            var rootWithAnnotation = root.WithAdditionalAnnotations(annotation);
            Assert.True(rootWithAnnotation.ContainsAnnotations);
            Assert.True(rootWithAnnotation.HasAnnotation(annotation));

            var stream = new MemoryStream();
            rootWithAnnotation.SerializeTo(stream);

            stream.Position = 0;
            var droot = CS.CSharpSyntaxNode.DeserializeFrom(stream);
            Assert.True(droot.ContainsAnnotations);
            Assert.True(droot.HasAnnotation(annotation));

            var dannotation = droot.GetAnnotations(SymbolAnnotation.Kind).SingleOrDefault();
            Assert.NotNull(dannotation);
            Assert.NotSame(annotation, dannotation);
            Assert.Equal(annotation, dannotation);
            var id = SymbolAnnotation.GetSymbol(annotation, model.Compilation);
            var did = SymbolAnnotation.GetSymbol(dannotation, model.Compilation);

            Assert.True(id.Equals(did));
        }

        private static void TextEncodingRoundrip(Encoding encoding)
        {
            using var stream = new MemoryStream();

            using (var writer = new ObjectWriter(stream, leaveOpen: true))
            {
                SerializerService.WriteTo(encoding, writer, CancellationToken.None);
            }

            stream.Position = 0;

            using var reader = ObjectReader.TryGetReader(stream);
            Assert.NotNull(reader);
            var actualEncoding = (Encoding)SerializerService.ReadEncodingFrom(reader, CancellationToken.None).Clone();
            var expectedEncoding = (Encoding)encoding.Clone();

            // set the fallbacks to the same instance so that equality comparison does not take them into account:
            actualEncoding.EncoderFallback = EncoderFallback.ExceptionFallback;
            actualEncoding.DecoderFallback = DecoderFallback.ExceptionFallback;
            expectedEncoding.EncoderFallback = EncoderFallback.ExceptionFallback;
            expectedEncoding.DecoderFallback = DecoderFallback.ExceptionFallback;

            Assert.Equal(expectedEncoding.GetPreamble(), actualEncoding.GetPreamble());
            Assert.Equal(expectedEncoding.CodePage, actualEncoding.CodePage);
            Assert.Equal(expectedEncoding.WebName, actualEncoding.WebName);
            Assert.Equal(expectedEncoding, actualEncoding);
        }

        [Theory]
        [CombinatorialData]
        public void EncodingSerialization_UTF8(bool byteOrderMark)
        {
            TextEncodingRoundrip(new UTF8Encoding(byteOrderMark));
        }

        [Theory]
        [CombinatorialData]
        public void EncodingSerialization_UTF32(bool bigEndian, bool byteOrderMark)
        {
            TextEncodingRoundrip(new UTF32Encoding(bigEndian, byteOrderMark));
        }

        [Theory]
        [CombinatorialData]
        public void EncodingSerialization_Unicode(bool bigEndian, bool byteOrderMark)
        {
            TextEncodingRoundrip(new UnicodeEncoding(bigEndian, byteOrderMark));
        }

        [Fact]
        public void EncodingSerialization_AllAvailable()
        {
            foreach (var info in Encoding.GetEncodings())
            {
                TextEncodingRoundrip(Encoding.GetEncoding(info.Name));
            }
        }
    }
}
