// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public abstract class DeterministicKeyBuilderTests
    {
        private static readonly char[] s_trimChars = { ' ', '\n', '\r' };

        public static SourceHashAlgorithm HashAlgorithm { get; } = SourceHashAlgorithm.Sha256;
        public static SourceHashAlgorithm[] HashAlgorithms { get; } = new[]
        {
            SourceHashAlgorithm.Sha1,
            SourceHashAlgorithm.Sha256
        };

        protected static void AssertJson(
            string expected,
            string actual,
            bool removeStandard = true) => AssertJson(expected, actual, "references", "extensions");

        protected static void AssertJson(
            string expected,
            string actual,
            params string[] ignoreSections)
        {
            var json = JObject.Parse(actual);
            if (ignoreSections.Length > 0)
            {
                json
                    .Descendants()
                    .OfType<JProperty>()
                    .Where(x => ignoreSections.Contains(x.Name))
                    .ToList()
                    .ForEach(x => x.Remove());
            }

            actual = json.ToString(Formatting.Indented);
            expected = JObject.Parse(expected).ToString(Formatting.Indented);
            AssertJsonCore(expected, actual);
        }

        protected static void AssertJsonSection(
            string expected,
            string actual,
            string sectionName,
            params string[] ignoreProperties)
        {
            var lastName = sectionName.Split('.').Last();
            AssertJsonCore(expected, getSection(actual));

            string getSection(string json)
            {
                var property = JObject.Parse(json)
                    .Descendants()
                    .OfType<JProperty>()
                    .Where(x => x.Name == lastName && getFullName(x) == sectionName)
                    .Single();

                if (ignoreProperties.Length > 0)
                {
                    if (property.Value is JObject value)
                    {
                        removeProperties(value);
                    }
                    else if (property.Value is JArray array)
                    {
                        foreach (var element in array.Values<JObject>())
                        {
                            removeProperties(element!);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    void removeProperties(JObject value)
                    {
                        foreach (var ignoreProperty in ignoreProperties)
                        {
                            value.Properties().Where(x => x.Name == ignoreProperty).Single().Remove();
                        }
                    }
                }

                return property.ToString(Formatting.Indented);
            }

            static string getFullName(JProperty property)
            {
                string name = property.Name;
                while (
                    property.Parent is JObject obj &&
                    obj.Parent is JProperty parent)
                {
                    name = $"{parent.Name}.{name}";
                    property = parent;
                }

                return name;
            }
        }

        protected static void AssertJsonCore(string expected, string actual)
        {
            expected = expected.Trim(s_trimChars);
            actual = actual.Trim(s_trimChars);
            Assert.Equal(expected, actual);
        }

        protected static string GetChecksum(SourceText text)
        {
            var checksum = text.GetChecksum();
            var builder = PooledStringBuilder.GetInstance();
            DeterministicKeyBuilder.EncodeByteArrayValue(checksum.AsSpan(), builder);
            return builder.ToStringAndFree();
        }

        protected abstract SyntaxTree ParseSyntaxTree(string content, string fileName, SourceHashAlgorithm hashAlgorithm);

        protected abstract Compilation CreateCompilation(
            SyntaxTree[] syntaxTrees,
            MetadataReference[]? references = null);

        private protected abstract DeterministicKeyBuilder GetDeterministicKeyBuilder();

        [Theory]
        [InlineData(@"hello world")]
        [InlineData(@"just need some text here")]
        [InlineData(@"yet another case")]
        public void SyntaxTreeContent(string content)
        {
            foreach (var hashAlgorithm in HashAlgorithms)
            {
                var syntaxTree = ParseSyntaxTree(content, fileName: "file.cs", hashAlgorithm);
                var contentChecksum = GetChecksum(syntaxTree.GetText());
                var compilation = CreateCompilation(new[] { syntaxTree });
                var key = compilation.GetDeterministicKey();
                var expected = @$"
""syntaxTrees"": [
  {{
    ""fileName"": ""file.cs"",
    ""text"": {{
      ""checksum"": ""{contentChecksum}"",
      ""checksumAlgorithm"": ""{hashAlgorithm}"",
      ""encoding"": ""Unicode (UTF-8)""
    }}
  }}
]";
                AssertJsonSection(expected, key, "compilation.syntaxTrees", "parseOptions");
            }
        }

        [Fact]
        public void EmitOptionsDefault()
        {
            var builder = GetDeterministicKeyBuilder();
            var key = builder.GetKey(EmitOptions.Default);
            AssertJson(@"
{
  ""emitMetadataOnly"": false,
  ""tolerateErrors"": false,
  ""includePrivateMembers"": true,
  ""instrumentationKinds"": [
  ],
  ""subsystemVersion"": {
    ""major"": 0,
    ""minor"": 0
  },
  ""fileAlignment"": 0,
  ""highEntropyVirtualAddressSpace"": false,
  ""baseAddress"": ""0"",
  ""debugInformationFormat"": ""Pdb"",
  ""outputNameOverride"": null,
  ""pdbFilePath"": null,
  ""pdbChecksumAlgorithm"": ""SHA256"",
  ""runtimeMetadataVersion"": null,
  ""defaultSourceFileEncoding"": null,
  ""fallbackSourceFileEncoding"": null
}
", key);
        }

        [Theory]
        [CombinatorialData]
        public void EmitOptionsCombo(
            DebugInformationFormat debugInformationFormat,
            InstrumentationKind kind)
        {
            var emitOptions = EmitOptions
                .Default
                .WithDebugInformationFormat(debugInformationFormat)
                .WithInstrumentationKinds(ImmutableArray.Create(kind));


            var builder = GetDeterministicKeyBuilder();
            var key = builder.GetKey(emitOptions);
            AssertJson(@$"
{{
  ""emitMetadataOnly"": false,
  ""tolerateErrors"": false,
  ""includePrivateMembers"": true,
  ""instrumentationKinds"": [
    ""{kind}"",
  ],
  ""subsystemVersion"": {{
    ""major"": 0,
    ""minor"": 0
  }},
  ""fileAlignment"": 0,
  ""highEntropyVirtualAddressSpace"": false,
  ""baseAddress"": ""0"",
  ""debugInformationFormat"": ""{debugInformationFormat}"",
  ""outputNameOverride"": null,
  ""pdbFilePath"": null,
  ""pdbChecksumAlgorithm"": ""SHA256"",
  ""runtimeMetadataVersion"": null,
  ""defaultSourceFileEncoding"": null,
  ""fallbackSourceFileEncoding"": null
}}
", key);
        }

    }
}
