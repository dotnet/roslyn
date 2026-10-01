// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Rebuild;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildValidator.UnitTests
{
    public class LocalReferenceResolverTests : CSharpTestBase
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResolvesOnlyCandidate(bool readyToRun)
        {
            var (ilPath, readyToRunPath, reference) = CreateCandidates();
            var path = readyToRun ? readyToRunPath : ilPath;
            var resolver = CreateResolver(reference.FileName, path);

            Assert.True(resolver.TryGetAssemblyInfo(reference, out var info));
            Assert.Equal(path, info.FilePath);
            Assert.True(resolver.TryResolveReferences(reference, out var metadataReference));
            Assert.Equal(path, metadataReference.Display);
            Assert.Equal(path, resolver.GetCachedReferencePath(reference));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PrefersILRegardlessOfCandidateOrder(bool readyToRunFirst)
        {
            var (ilPath, readyToRunPath, reference) = CreateCandidates();
            var resolver = readyToRunFirst
                ? CreateResolver(reference.FileName, readyToRunPath, ilPath)
                : CreateResolver(reference.FileName, ilPath, readyToRunPath);

            Assert.True(resolver.TryGetAssemblyInfo(reference, out var info));
            Assert.Equal(ilPath, info.FilePath);
            Assert.True(resolver.TryGetCachedAssemblyInfo(reference.ModuleVersionId, out var cached));
            Assert.Same(info, cached);
        }

        [Fact]
        public void ResolvesReadyToRunWhenILHasDifferentMvid()
        {
            var (_, readyToRunPath, reference) = CreateCandidates();
            var (otherILPath, _, otherReference) = CreateCandidates("public class Other { }");
            Assert.NotEqual(reference.ModuleVersionId, otherReference.ModuleVersionId);
            var resolver = CreateResolver(reference.FileName, otherILPath, readyToRunPath);

            Assert.True(resolver.TryGetAssemblyInfo(reference, out var info));
            Assert.Equal(readyToRunPath, info.FilePath);
            Assert.True(resolver.TryGetAssemblyInfo(otherReference, out var otherInfo));
            Assert.Equal(otherILPath, otherInfo.FilePath);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PrefersILAcrossFileNames(bool readyToRunFirst)
        {
            var (ilPath, readyToRunPath, reference) = CreateCandidates();
            var renamedILPath = Temp.CreateDirectory().CreateFile("Renamed.dll").WriteAllBytes(File.ReadAllBytes(ilPath)).Path;
            var ilReference = reference with { FileName = "Renamed.dll" };
            var resolver = new LocalReferenceResolver(
                new Dictionary<string, List<string>>
                {
                    [reference.FileName] = new List<string> { readyToRunPath },
                    [ilReference.FileName] = new List<string> { renamedILPath },
                }, NullLogger.Instance);

            Assert.True(resolver.TryGetAssemblyInfo(readyToRunFirst ? reference : ilReference, out _));
            Assert.True(resolver.TryGetAssemblyInfo(readyToRunFirst ? ilReference : reference, out var info));
            Assert.Equal(renamedILPath, info.FilePath);
            Assert.Equal(renamedILPath, resolver.GetCachedReferencePath(reference));
        }

        [Fact]
        public void DoesNotResolveUnknownMvid()
        {
            var (ilPath, readyToRunPath, reference) = CreateCandidates();
            var resolver = CreateResolver(reference.FileName, readyToRunPath, ilPath);

            Assert.False(resolver.TryGetAssemblyInfo(reference with { ModuleVersionId = Guid.NewGuid() }, out var info));
            Assert.Null(info);
        }

        private static LocalReferenceResolver CreateResolver(string fileName, params string[] paths)
            => new(new Dictionary<string, List<string>> { [fileName] = paths.ToList() }, NullLogger.Instance);

        private (string ilPath, string readyToRunPath, MetadataReferenceInfo reference) CreateCandidates(string source = "public class C { }")
        {
            using var stream = new MemoryStream();
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            var result = compilation.Emit(stream);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            var image = stream.ToArray();
            var ilPath = Temp.CreateDirectory().CreateFile("Reference.dll").WriteAllBytes(image).Path;
            using var reader = new PEReader(new MemoryStream(image));
            var mvid = Util.GetMvid(reader)!.Value;
            var reference = new MetadataReferenceInfo(
                "Reference.dll", mvid, ExternAlias: null, MetadataImageKind.Assembly, EmbedInteropTypes: false,
                reader.PEHeaders.CoffHeader.TimeDateStamp, reader.PEHeaders.PEHeader!.SizeOfImage);

            // The resolver only inspects metadata and the ReadyToRun marker. Model that marker
            // without requiring crossgen2 or executing native code; keep the IL image's MVID.
            using (var writer = new BinaryWriter(new MemoryStream(image, writable: true)))
            {
                writer.BaseStream.Position = reader.PEHeaders.CorHeaderStartOffset + 16;
                writer.Write((int)(reader.PEHeaders.CorHeader!.Flags | CorFlags.ILLibrary));
                writer.BaseStream.Position = reader.PEHeaders.CorHeaderStartOffset + 68;
                writer.Write(1);
            }

            var readyToRunPath = Temp.CreateDirectory().CreateFile("Reference.dll").WriteAllBytes(image).Path;
            Assert.False(Util.GetPortableExecutableInfo(ilPath)!.IsReadyToRun);
            var readyToRunInfo = Util.GetPortableExecutableInfo(readyToRunPath)!;
            Assert.True(readyToRunInfo.IsReadyToRun);
            Assert.Equal(mvid, readyToRunInfo.Mvid);
            return (ilPath, readyToRunPath, reference);
        }
    }
}
