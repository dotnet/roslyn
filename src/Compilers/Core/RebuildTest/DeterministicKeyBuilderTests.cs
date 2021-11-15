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
    public abstract class DeterministicKeyBuilderTests<TCompilation, TCompilationOptions>
        where TCompilation : Compilation
        where TCompilationOptions : CompilationOptions
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

        protected static void AssertJsonCore(string expected, string? actual)
        {
            expected = expected.Trim(s_trimChars);
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

        protected static string GetChecksum(SourceText text)
        {
            var checksum = text.GetChecksum();
            var builder = PooledStringBuilder.GetInstance();
            DeterministicKeyBuilder.EncodeByteArrayValue(checksum.AsSpan(), builder);
            return builder.ToStringAndFree();
        }

        protected abstract SyntaxTree ParseSyntaxTree(string content, string fileName, SourceHashAlgorithm hashAlgorithm);

        protected abstract TCompilation CreateCompilation(
            SyntaxTree[] syntaxTrees,
            MetadataReference[]? references = null,
            TCompilationOptions? options = null);

        protected abstract TCompilationOptions GetCompilationOptions();

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

    }
}
