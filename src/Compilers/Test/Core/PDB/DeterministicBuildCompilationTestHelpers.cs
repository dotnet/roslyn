// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Cci;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Roslyn.Test.Utilities.PDB
{
    internal static partial class DeterministicBuildCompilationTestHelpers
    {
        public static void VerifyPdbOption<T>(this ImmutableDictionary<string, string> pdbOptions, string pdbName, T expectedValue, Func<T, bool> isDefault = null, Func<T, string> toString = null)
        {
            bool expectedIsDefault = (isDefault != null) ? isDefault(expectedValue) : EqualityComparer<T>.Default.Equals(expectedValue, default);
            var expectedValueString = expectedIsDefault ? null : (toString != null) ? toString(expectedValue) : expectedValue.ToString();

            pdbOptions.TryGetValue(pdbName, out var actualValueString);
            Assert.Equal(expectedValueString, actualValueString);
        }

        public static IEnumerable<EmitOptions> GetEmitOptions()
        {
            var emitOptions = new EmitOptions(
                debugInformationFormat: DebugInformationFormat.Embedded,
                pdbChecksumAlgorithm: HashAlgorithmName.SHA256,
                defaultSourceFileEncoding: Encoding.UTF32);

            yield return emitOptions;
            yield return emitOptions.WithDefaultSourceFileEncoding(Encoding.ASCII);
            yield return emitOptions.WithDefaultSourceFileEncoding(null).WithFallbackSourceFileEncoding(Encoding.Unicode);
            yield return emitOptions.WithFallbackSourceFileEncoding(Encoding.Unicode).WithDefaultSourceFileEncoding(Encoding.ASCII);
        }

        internal static void AssertCommonOptions(EmitOptions emitOptions, CompilationOptions compilationOptions, Compilation compilation, ImmutableDictionary<string, string> pdbOptions)
        {
            pdbOptions.VerifyPdbOption("version", 2);
            pdbOptions.VerifyPdbOption("fallback-encoding", emitOptions.FallbackSourceFileEncoding, toString: v => v.WebName);
            pdbOptions.VerifyPdbOption("default-encoding", emitOptions.DefaultSourceFileEncoding, toString: v => v.WebName);

            int portabilityPolicy = 0;
            if (compilationOptions.AssemblyIdentityComparer is DesktopAssemblyIdentityComparer identityComparer)
            {
                portabilityPolicy |= identityComparer.PortabilityPolicy.SuppressSilverlightLibraryAssembliesPortability ? 0b1 : 0;
                portabilityPolicy |= identityComparer.PortabilityPolicy.SuppressSilverlightPlatformAssembliesPortability ? 0b10 : 0;
            }

            pdbOptions.VerifyPdbOption("portability-policy", portabilityPolicy);

            var compilerVersion = typeof(Compilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            Assert.Equal(compilerVersion.ToString(), pdbOptions["compiler-version"]);

            var runtimeVersion = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            Assert.Equal(runtimeVersion, pdbOptions[CompilationOptionNames.RuntimeVersion]);

            pdbOptions.VerifyPdbOption(
                "optimization",
                (compilationOptions.OptimizationLevel, compilationOptions.DebugPlusMode),
                toString: v => v.OptimizationLevel.ToPdbSerializedString(v.DebugPlusMode));

            Assert.Equal(compilation.Language, pdbOptions["language"]);
        }

        public static void VerifyReferenceInfo(TestMetadataReferenceInfo[] references, TargetFramework targetFramework, BlobReader metadataReferenceReader)
        {
            var frameworkReferences = TargetFrameworkUtil.GetReferences(targetFramework);
            var count = 0;
            while (metadataReferenceReader.RemainingBytes > 0)
            {
                var info = ParseMetadataReferenceInfo(ref metadataReferenceReader);
                if (frameworkReferences.Any(x => x.GetModuleVersionId() == info.Mvid))
                {
                    count++;
                    continue;
                }

                var testReference = references.Single(x => x.MetadataReferenceInfo.Mvid == info.Mvid);
                testReference.MetadataReferenceInfo.AssertEqual(info);
                count++;
            }

            Assert.Equal(references.Length + frameworkReferences.Length, count);
        }

        public static BlobReader GetSingleBlob(Guid infoGuid, MetadataReader pdbReader)
        {
            return (from cdiHandle in pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                    let cdi = pdbReader.GetCustomDebugInformation(cdiHandle)
                    where pdbReader.GetGuid(cdi.Kind) == infoGuid
                    select pdbReader.GetBlobReader(cdi.Value)).Single();
        }

        public static MetadataReferenceInfo ParseMetadataReferenceInfo(ref BlobReader blobReader)
        {
            // Order of information
            // File name (null terminated string): A.exe
            // Extern Alias (null terminated string): a1,a2,a3
            // EmbedInteropTypes/MetadataImageKind (byte)
            // COFF header Timestamp field (4 byte int)
            // COFF header SizeOfImage field (4 byte int)
            // MVID (Guid, 24 bytes)

            var terminatorIndex = blobReader.IndexOf(0);
            Assert.NotEqual(-1, terminatorIndex);

            var name = blobReader.ReadUTF8(terminatorIndex);

            // Skip the null terminator
            blobReader.ReadByte();

            terminatorIndex = blobReader.IndexOf(0);
            Assert.NotEqual(-1, terminatorIndex);

            var externAliases = blobReader.ReadUTF8(terminatorIndex);

            blobReader.ReadByte();

            var embedInteropTypesAndKind = blobReader.ReadByte();
            var embedInteropTypes = (embedInteropTypesAndKind & 0b10) == 0b10;
            var kind = (embedInteropTypesAndKind & 0b1) == 0b1
                ? MetadataImageKind.Assembly
                : MetadataImageKind.Module;

            var timestamp = blobReader.ReadInt32();
            var imageSize = blobReader.ReadInt32();
            var mvid = blobReader.ReadGuid();

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
