// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Roslyn.Test.Utilities.PDB
{
    internal static class DeterministicBuildCompilationTestHelpers
    {
        public static string GetCurrentCompilerVersion()
        {
            return typeof(Compilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        }

        public static void VerifyReferenceInfo(TestMetadataReferenceInfo[] references, BlobReader metadataReferenceReader)
        {
            foreach (var reference in references)
            {
                var info = ParseMetadataReferenceInfo(metadataReferenceReader);
                var originalInfo = reference.MetadataReferenceInfo;

                originalInfo.AssertEqual(info);
            }
        }

        public static BlobReader GetSingleBlob(Guid infoGuid, MetadataReader pdbReader)
        {
            return (from cdiHandle in pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                    let cdi = pdbReader.GetCustomDebugInformation(cdiHandle)
                    where pdbReader.GetGuid(cdi.Kind) == infoGuid
                    select pdbReader.GetBlobReader(cdi.Value)).Single();
        }

        public static MetadataReferenceInfo ParseMetadataReferenceInfo(BlobReader blobReader)
        {
            // Name is first. UTF8 encoded null-terminated string
            var terminatorIndex = blobReader.IndexOf(0);
            Assert.NotEqual(-1, terminatorIndex);

            var name = blobReader.ReadUTF8(terminatorIndex);

            // Skip the null terminator
            blobReader.ReadByte();

            // Extern aliases are second
            terminatorIndex = blobReader.IndexOf(0);
            Assert.NotEqual(-1, terminatorIndex);

            var externAliases = blobReader.ReadUTF8(terminatorIndex);

            // Skip the null terminator
            blobReader.ReadByte();

            var kind = (MetadataImageKind)blobReader.ReadByte();
            var embedInteropTypes = blobReader.ReadBoolean();
            var timestamp = blobReader.ReadInt32();
            var imageSize = blobReader.ReadInt32();
            var mvid = blobReader.ReadGuid();

            Assert.Equal(0, blobReader.RemainingBytes);

            return new MetadataReferenceInfo(
                timestamp,
                imageSize,
                name,
                mvid,
                string.IsNullOrEmpty(externAliases)
                    ? ImmutableArray<string>.Empty
                    : externAliases.Split(',').ToImmutableArray(),
                kind,
                embedInteropTypes);
        }

        public static ImmutableDictionary<string, string> ParseCompilationOptions(BlobReader blobReader)
        {

            // Compiler flag bytes are UTF-8 null-terminated key-value pairs
            string key = null;
            Dictionary<string, string> kvp = new Dictionary<string, string>();
            for (; ; )
            {
                var nullIndex = blobReader.IndexOf(0);
                if (nullIndex == -1)
                {
                    break;
                }

                var value = blobReader.ReadUTF8(nullIndex);

                // Skip the null terminator
                blobReader.ReadByte();

                if (key is null)
                {
                    key = value;
                }
                else
                {
                    kvp[key] = value;
                    key = null;
                }
            }

            Assert.Null(key);
            Assert.Equal(0, blobReader.RemainingBytes);
            return kvp.ToImmutableDictionary();
        }
    }
}
