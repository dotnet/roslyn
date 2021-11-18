// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public abstract class DeterministicKeyBuilderTests<TCompilation, TCompilationOptions, TParseOptions>
        where TCompilation : Compilation
        where TCompilationOptions : CompilationOptions
        where TParseOptions : ParseOptions
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
            var property = GetJsonProperty(actual, sectionName, ignoreProperties);
            AssertJsonCore(expected, property.ToString(Formatting.Indented));
        }

        protected static void AssertJsonCore(string? expected, string? actual)
        {
            expected = expected?.Trim(s_trimChars);
            actual = actual?.Trim(s_trimChars);
            Assert.Equal(expected, actual);
        }

        protected static JProperty GetJsonProperty(
            string json,
            string sectionName,
            params string[] ignoreProperties)
        {
            var lastName = sectionName.Split('.').Last();
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

            return property;

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

        protected JObject GetCompilationOptionsValue(CompilationOptions options)
        {
            var compilation = CreateCompilation(syntaxTrees: new SyntaxTree[] { }, options: (TCompilationOptions)options);
            var property = GetJsonProperty(compilation.GetDeterministicKey(), "compilation.options");
            return (JObject)property.Value;
        }

        protected JObject GetParseOptionsValue(ParseOptions parseOptions)
        {
            var syntaxTree = ParseSyntaxTree("", fileName: "test", SourceHashAlgorithm.Sha256, (TParseOptions)parseOptions);
            var compilation = CreateCompilation(syntaxTrees: new SyntaxTree[] { syntaxTree });
            var property = GetJsonProperty(compilation.GetDeterministicKey(), "compilation.syntaxTrees");
            var trees = (JArray)property.Value;
            var obj = (JObject)trees[0];
            return (JObject)(obj.Property("parseOptions")?.Value!);
        }

        protected JObject GetReferenceValue(MetadataReference reference)
        {
            var compilation = CreateCompilation(syntaxTrees: new SyntaxTree[] { }, references: new[] { reference });
            var property = GetJsonProperty(compilation.GetDeterministicKey(), "compilation.references");
            var expectedMvid = DeterministicKeyBuilder.GetGuidValue(reference.GetModuleVersionId());

            var array = (JArray)property.Value;
            foreach (var item in array!.Values<JObject>())
            {
                if (item?.Value<string>("mvid") == expectedMvid)
                {
                    return item;
                }
            }

            Assert.True(false, $"Could not find reference with MVID {expectedMvid}");
            throw null!;
        }

        protected static string GetChecksum(SourceText text)
        {
            var checksum = text.GetChecksum();
            var builder = PooledStringBuilder.GetInstance();
            DeterministicKeyBuilder.EncodeByteArrayValue(checksum.AsSpan(), builder);
            return builder.ToStringAndFree();
        }

        protected abstract SyntaxTree ParseSyntaxTree(string content, string fileName, SourceHashAlgorithm hashAlgorithm, TParseOptions? parseOptions = null);

        protected abstract TCompilation CreateCompilation(
            SyntaxTree[] syntaxTrees,
            MetadataReference[]? references = null,
            TCompilationOptions? options = null);

        protected abstract TCompilationOptions GetCompilationOptions();

        protected abstract TParseOptions GetParseOptions();

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

        [Theory]
        [CombinatorialData]
        public void CompilationOptionsCombination(
            OutputKind outputKind,
            bool delaySign,
            bool publicSign,
            bool deterministic)
        {
            var options = GetCompilationOptions()
                .WithOutputKind(outputKind)
                .WithDelaySign(delaySign)
                .WithPublicSign(publicSign)
                .WithDeterministic(deterministic);

            var obj = GetCompilationOptionsValue(options);
            Assert.Equal(outputKind.ToString(), obj.Value<string>("outputKind"));
            Assert.Equal(publicSign, obj.Value<bool>("publicSign"));
            Assert.Equal(delaySign, obj.Value<bool>("delaySign"));
            Assert.Equal(deterministic, obj.Value<bool>("deterministic"));
        }

        /// <summary>
        /// Makes sure that local time is not encoded for deterministic builds. Otherwise deterministic
        /// builds would not have deterministic keys
        /// </summary>
        [Fact]
        public void CompilationOptionsDeterministic()
        {
            var obj = getValue(deterministic: true);
            Assert.Null(obj.Value<string>("localtime"));

            obj = getValue(deterministic: false);
            Assert.NotNull(obj.Value<string>("localtime"));

            JObject getValue(bool deterministic)
            {
                var options = GetCompilationOptions()
                    .WithDeterministic(deterministic);

                return GetCompilationOptionsValue(options);
            }
        }

        /// <summary>
        /// Verify that options which don't impact determinism are excluded from the key
        /// </summary>
        [Theory]
        [CombinatorialData]
        public void CompilationOptionsExcluded(bool concurrentBuild, MetadataImportOptions metaImportOptions)
        {
            var options = GetCompilationOptions();
            var other = options
                .WithConcurrentBuild(concurrentBuild)
                .WithMetadataImportOptions(metaImportOptions);

            var expected = GetCompilationOptionsValue(options);
            var actual = GetCompilationOptionsValue(other);
            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void CompilationOptionsSpecificDiagnosticOptions()
        {
            assert(@"[]");
            assert(@"
[
  {
    ""CA109"": ""Error""
  }
]", ("CA109", ReportDiagnostic.Error));

            assert(@"
[
  {
    ""CA109"": ""Error""
  },
  {
    ""CA200"": ""Warn""
  }
]", ("CA109", ReportDiagnostic.Error), ("CA200", ReportDiagnostic.Warn));

            void assert(string expected, params (string Diagnostic, ReportDiagnostic ReportDiagnostic)[] values)
            {
                var map = values.ToImmutableDictionary(
                    x => x.Diagnostic,
                    x => x.ReportDiagnostic);

                var options = GetCompilationOptions()
                    .WithSpecificDiagnosticOptions(map);
                var value = GetCompilationOptionsValue(options);
                var actual = value["specificDiagnosticOptions"]?.ToString(Formatting.Indented);
                AssertJsonCore(expected, actual);
            }
        }

        [Theory]
        [CombinatorialData]
        public void ParseOptionsCombination(
            SourceCodeKind sourceCodeKind,
            DocumentationMode documentationMode)
        {
            var parseOptions = GetParseOptions()
                .WithKind(sourceCodeKind)
                .WithDocumentationMode(documentationMode);

#pragma warning disable 618
            if (sourceCodeKind == SourceCodeKind.Interactive)
            {
                sourceCodeKind = SourceCodeKind.Script;
            }
#pragma warning restore 618

            var obj = GetParseOptionsValue(parseOptions);
            Assert.Equal(sourceCodeKind.ToString(), obj.Value<string>("kind"));
            Assert.Equal(documentationMode.ToString(), obj.Value<string>("documentationMode"));
            Assert.Null(obj.Value<JObject>("features"));
        }

        [Fact]
        public void ParseOptionsFeatures()
        {
            var parseOptions = GetParseOptions();

            assert(null);
            assert(@"
{
  ""key"": ""value""
}", ("key", "value"));

            assert(@"
{
  ""k1"": ""v1"",
  ""k2"": ""v2""
}", ("k1", "v1"), ("k2", "v2"));

            // Same case but reverse the order the keys are added. That should not change the key
            assert(@"
{
  ""k1"": ""v1"",
  ""k2"": ""v2""
}", ("k2", "v2"), ("k1", "v1"));

            // Make sure that the keys are escaped properly
            assert(@"
{
  ""\\\""strange"": ""value""
}", (@"\""strange", "value"));

            void assert(string? expected, params (string Key, string Value)[] features)
            {
                var parseOptions = GetParseOptions()
                    .WithFeatures(features.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)));

                var obj = GetParseOptionsValue(parseOptions);
                var value = obj.Value<JObject>("features");
                AssertJsonCore(expected, value?.ToString(Formatting.Indented));
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

        [Theory]
        [InlineData(1, 2)]
        [InlineData(3, 4)]
        public void EmitOptionsSubsystemVersion(int major, int minor)
        {
            var emitOptions = EmitOptions.Default.WithSubsystemVersion(SubsystemVersion.Create(major, minor));
            var builder = GetDeterministicKeyBuilder();
            var key = builder.GetKey(emitOptions);
            var expected = @$"
""subsystemVersion"": {{
  ""major"": {major},
  ""minor"": {minor}
}}";
            AssertJsonSection(expected, key, "subsystemVersion");
        }

        [Fact]
        public void MetadataReferenceMscorlib()
        {
            var mscorlib = NetCoreApp.mscorlib;
            var obj = GetReferenceValue(mscorlib);

            var mvid = DeterministicKeyBuilder.GetGuidValue(mscorlib.GetModuleVersionId());
            var expected = $@"
{{
  ""name"": ""mscorlib"",
  ""version"": ""4.0.0.0"",
  ""publicKey"": ""0000000040000000"",
  ""mvid"": ""{mvid}"",
  ""properties"": {{
    ""kind"": ""Assembly"",
    ""embedInteropTypes"": false,
    ""aliases"": []
  }}
}}
";

            AssertJsonCore(expected, obj.ToString(Formatting.Indented));
        }
    }
}
