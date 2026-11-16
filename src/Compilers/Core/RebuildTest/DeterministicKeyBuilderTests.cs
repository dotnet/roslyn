// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public abstract partial class DeterministicKeyBuilderTests<TCompilation, TCompilationOptions, TParseOptions>
        where TCompilation : Compilation
        where TCompilationOptions : CompilationOptions
        where TParseOptions : ParseOptions
    {
        private static readonly char[] s_trimChars = { ' ', '\n', '\r' };

        public static EmitOptions EmitOptions { get; } = new();
        public static SourceHashAlgorithm HashAlgorithm { get; } = SourceHashAlgorithm.Sha256;
        public static SourceHashAlgorithm[] HashAlgorithms { get; } = new[]
        {
            SourceHashAlgorithm.Sha1,
            SourceHashAlgorithm.Sha256,
            SourceHashAlgorithm.Sha384,
            SourceHashAlgorithm.Sha512
        };

        protected static void AssertJson(
            string expected,
            string actual) => AssertJson(expected, actual, "references", "extensions");

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

        private protected static void AssertSyntaxTreePathMap(string? expected, CommonCompiler compiler)
        {
            Assert.Empty(compiler.Arguments.Errors);

            var writer = new StringWriter();
            var compilation = compiler.CreateCompilation(
                writer,
                touchedFilesLogger: null,
                errorLoggerOpt: null,
                analyzerConfigOptions: default,
                globalConfigOptions: default);
            Assert.NotNull(compilation);
            Assert.Empty(writer.GetStringBuilder().ToString());
            var obj = GetSyntaxTreeValues(compilation, compiler.Arguments.PathMap);
            AssertJsonCore(expected, obj.ToString(Formatting.Indented));
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
                while (property.Parent is JObject obj)
                {
                    if (obj.Parent is JProperty parent)
                    {
                        name = $"{parent.Name}.{name}";
                        property = parent;
                    }
                    else if (obj.Parent is JArray { Parent: JProperty arrayParent } array)
                    {
                        name = $"[].{name}";
                        property = arrayParent;
                    }
                    else
                    {
                        break;
                    }
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

        protected JObject GetCompilationValue(Compilation compilation)
        {
            var property = GetJsonProperty(compilation.GetDeterministicKey(), "compilation");
            return (JObject)property.Value;
        }

        protected JObject GetParseOptionsValue(ParseOptions parseOptions)
        {
            var syntaxTree = ParseSyntaxTree("", fileName: "test", SourceHashAlgorithm.Sha256, (TParseOptions)parseOptions);
            var compilation = CreateCompilation(syntaxTrees: new SyntaxTree[] { syntaxTree });
            var key = compilation.GetDeterministicKey();
            var treesProperty = GetJsonProperty(key, "compilation.syntaxTrees");
            var trees = (JArray)treesProperty.Value;
            var treeObj = (JObject)trees[0];
            var parseOptionsIndex = treeObj.Value<int>("parseOptionsIndex");
            var parseOptionsArray = (JArray)GetJsonProperty(key, "compilation.parseOptions").Value;
            return (JObject)parseOptionsArray[parseOptionsIndex];
        }

        protected JArray GetReferenceValues(Compilation compilation)
        {
            var property = GetJsonProperty(compilation.GetDeterministicKey(), "compilation.references");
            return (JArray)property.Value;
        }

        protected JObject GetReferenceValue(MetadataReference reference)
        {
            var expectedMvid = DeterministicKeyBuilder.GetGuidValue(reference.GetModuleVersionId());
            var compilation = CreateCompilation(syntaxTrees: new SyntaxTree[] { }, references: new[] { reference });
            var array = GetReferenceValues(compilation);

            foreach (var item in array!.Values<JObject>())
            {
                if (item?.Value<string>("mvid") == expectedMvid)
                {
                    return item;
                }
            }

            Assert.Fail($"Could not find reference with MVID {expectedMvid}");
            throw null!;
        }

        protected static JArray GetSyntaxTreeValues(Compilation compilation, ImmutableArray<KeyValuePair<string, string>> pathMap = default)
        {
            var property = GetJsonProperty(compilation.GetDeterministicKey(pathMap: pathMap), "compilation.syntaxTrees");
            return (JArray)property.Value; ;
        }

        protected static JArray GetAdditionalTextValues(Compilation compilation, ImmutableArray<AdditionalText> additionalTexts, ImmutableArray<KeyValuePair<string, string>> pathMap = default)
        {
            var property = GetJsonProperty(compilation.GetDeterministicKey(additionalTexts: additionalTexts, pathMap: pathMap), "additionalTexts");
            return (JArray)property.Value;
        }

        protected static JArray GetAnalyzerValues(Compilation compilation, params DiagnosticAnalyzer[] analyzers)
        {
            var property = GetJsonProperty(compilation.GetDeterministicKey(analyzers: analyzers.ToImmutableArray()), "analyzers");
            return (JArray)property.Value;
        }

        protected static JArray GetGeneratorValues(Compilation compilation, params ISourceGenerator[] generators)
        {
            var property = GetJsonProperty(compilation.GetDeterministicKey(generators: generators.ToImmutableArray()), "generators");
            return (JArray)property.Value;
        }

        private protected JObject GetEmitOptionsValue(
            EmitOptions emitOptions,
            ImmutableArray<KeyValuePair<string, string>> pathMap = default,
            DeterministicKeyOptions options = default)
        {
            var compilation = CreateCompilation(new SyntaxTree[] { });
            var key = compilation.GetDeterministicKey(
                emitOptions: emitOptions,
                pathMap: pathMap,
                options: options);
            var property = GetJsonProperty(key, "emitOptions");
            return (JObject)property.Value;
        }

        protected static string GetChecksum(SourceText text)
        {
            var checksum = text.GetChecksum();
            var builder = PooledStringBuilder.GetInstance();
            DeterministicKeyBuilder.EncodeByteArrayValue(checksum.AsSpan(), builder);
            return builder.ToStringAndFree();
        }

        protected static string GetChecksum(byte[] sourceLinkBytes)
        {
            using var hashAlgorithm = CryptographicHashProvider.TryGetAlgorithm(SourceHashAlgorithms.Default);
            var hash = hashAlgorithm!.ComputeHash(sourceLinkBytes);
            return DeterministicKeyBuilder.EncodeByteArrayValue(hash);
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
      ""encodingName"": ""Unicode (UTF-8)""
    }}
  }}
]";
                AssertJsonSection(expected, key, "compilation.syntaxTrees", "parseOptionsIndex");
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
        /// Disabling determinism should mean all calls to GetDeterministicKey return different values.
        /// </summary>
        [Fact]
        public void CompilationOptionsDeterministicOff()
        {
            var options = GetCompilationOptions();
            var compilation = CreateCompilation(syntaxTrees: new SyntaxTree[] { }, options: options);
            var key = compilation.GetDeterministicKey();

            Assert.Equal(key, compilation.GetDeterministicKey());

            options = (TCompilationOptions)options.WithDeterministic(false);
            compilation = (TCompilation)compilation.WithOptions(options);
            key = compilation.GetDeterministicKey();
            Assert.NotEqual(key, compilation.GetDeterministicKey());
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
            Assert.Empty(obj.Value<JObject>("features")!.Properties());
        }

        [Fact]
        public void ParseOptionsFeatures()
        {
            var parseOptions = GetParseOptions();

            assert("{}");
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
            var obj = GetEmitOptionsValue(EmitOptions);
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
  ""fallbackSourceFileEncoding"": null,
  ""sourceLink"": null
}
", obj.ToString(Formatting.Indented));
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

            var obj = GetEmitOptionsValue(emitOptions);
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
  ""fallbackSourceFileEncoding"": null,
  ""sourceLink"": null
}}
", obj.ToString(Formatting.Indented));
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(3, 4)]
        public void EmitOptionsSubsystemVersion(int major, int minor)
        {
            var emitOptions = EmitOptions.WithSubsystemVersion(SubsystemVersion.Create(major, minor));
            var obj = GetEmitOptionsValue(emitOptions);
            var expected = @$"
""subsystemVersion"": {{
  ""major"": {major},
  ""minor"": {minor}
}}";
            AssertJsonSection(expected, obj.ToString(Formatting.Indented), "subsystemVersion");
        }

        [Fact]
        public void EmitOptionsPdbFilePathRespectsOptions()
        {
            var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? @"c:\temp\util.pdb"
                : "/temp/util.pdb";
            var emitOptions = EmitOptions.WithPdbFilePath(path);
            var obj = GetEmitOptionsValue(emitOptions, options: DeterministicKeyOptions.IgnorePaths);
            Assert.Equal(@"util.pdb", obj.Value<string>("pdbFilePath"));
        }

        [Theory]
        [InlineData(@"c:\src\util.pdb", null, null)]
        [InlineData(@"d:\src\util.pdb", @"d:\", @"c:\")]
        [InlineData(@"d:\long\src\util.pdb", @"d:\long\", @"c:\")]
        public void EmitOptionsPdbFilePathRespectsPathMap(string filePath, string? pathMapFrom, string? pathMapTo)
        {
            var pathMap = (pathMapFrom, pathMapTo) switch
            {
                (null, null) => ImmutableArray<KeyValuePair<string, string>>.Empty,
                (string, string) => ImmutableArray.Create(KeyValuePair.Create(pathMapFrom, pathMapTo)),
                _ => throw new InvalidOperationException(),
            };
            var emitOptions = EmitOptions.WithPdbFilePath(filePath);
            var obj = GetEmitOptionsValue(emitOptions, pathMap);
            Assert.Equal(@"c:\src\util.pdb", obj.Value<string>("pdbFilePath"));
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
  ""version"": {{
    ""major"": 4,
    ""minor"": 0,
    ""build"": 0,
    ""revision"": 0
  }},
  ""publicKey"": ""0000000040000000"",
  ""mvid"": ""{mvid}"",
  ""secondaryModules"": [],
  ""properties"": {{
    ""kind"": ""Assembly"",
    ""embedInteropTypes"": false,
    ""aliases"": []
  }}
}}
";

            AssertJsonCore(expected, obj.ToString(Formatting.Indented));
        }

        [Fact]
        public void MetadataReferenceMscorlibWithAlias()
        {
            var mscorlib = NetCoreApp.mscorlib.WithAliases(new[] { "alias1", "alias2" });
            var obj = GetReferenceValue(mscorlib);

            var mvid = DeterministicKeyBuilder.GetGuidValue(mscorlib.GetModuleVersionId());
            var expected = $@"
{{
  ""name"": ""mscorlib"",
  ""version"": {{
    ""major"": 4,
    ""minor"": 0,
    ""build"": 0,
    ""revision"": 0
  }},
  ""publicKey"": ""0000000040000000"",
  ""mvid"": ""{mvid}"",
  ""secondaryModules"": [],
  ""properties"": {{
    ""kind"": ""Assembly"",
    ""embedInteropTypes"": false,
    ""aliases"": [
      ""alias1"",
      ""alias2""
    ]
  }}
}}
";

            AssertJsonCore(expected, obj.ToString(Formatting.Indented));
        }

        [Theory]
        [CombinatorialData]
        public void MetadataReferenceMscorlibEmbedInteropTypes(bool embedInteropTypes)
        {
            var mscorlib = NetCoreApp.mscorlib.WithEmbedInteropTypes(embedInteropTypes);
            var obj = GetReferenceValue(mscorlib);

            var mvid = DeterministicKeyBuilder.GetGuidValue(mscorlib.GetModuleVersionId());
            var expected = $@"
{{
  ""name"": ""mscorlib"",
  ""version"": {{
    ""major"": 4,
    ""minor"": 0,
    ""build"": 0,
    ""revision"": 0
  }},
  ""publicKey"": ""0000000040000000"",
  ""mvid"": ""{mvid}"",
  ""secondaryModules"": [],
  ""properties"": {{
    ""kind"": ""Assembly"",
    ""embedInteropTypes"": {embedInteropTypes.ToString().ToLowerInvariant()},
    ""aliases"": []
  }}
}}
";

            AssertJsonCore(expected, obj.ToString(Formatting.Indented));
        }

        [Fact]
        public void MetadataReferenceMultiModule()
        {
            var reference = TestReferences.SymbolsTests.MultiModule.Assembly;
            var obj = GetReferenceValue(reference);

            var expected = @"
{
  ""name"": ""MultiModule"",
  ""version"": {
    ""major"": 0,
    ""minor"": 0,
    ""build"": 0,
    ""revision"": 0
  },
  ""publicKey"": """",
  ""mvid"": ""0f07ef6c-4b63-421d-870e-1358db815764"",
  ""secondaryModules"": [
    {
      ""name"": ""mod2.netmodule"",
      ""mvid"": ""82f316ce-66f1-45b6-a2c7-b4476bda03fd""
    },
    {
      ""name"": ""mod3.netmodule"",
      ""mvid"": ""ff9f3a02-95e7-44b2-a278-e9149fa82ee4""
    }
  ],
  ""properties"": {
    ""kind"": ""Assembly"",
    ""embedInteropTypes"": false,
    ""aliases"": []
  }
}";

            AssertJsonCore(expected, obj.ToString(Formatting.Indented));
        }

        [Theory]
        [InlineData(@"c:\src\data.txt", null, null)]
        [InlineData(@"d:\src\data.txt", @"d:\", @"c:\")]
        [InlineData(@"d:\long\src\data.txt", @"d:\long\", @"c:\")]
        public void PathMapAdditionalFiles(string filePath, string? pathMapFrom, string? pathMapTo)
        {
            var pathMap = (pathMapFrom, pathMapTo) switch
            {
                (null, null) => ImmutableArray<KeyValuePair<string, string>>.Empty,
                (string, string) => ImmutableArray.Create(KeyValuePair.Create(pathMapFrom, pathMapTo)),
                _ => throw new InvalidOperationException(),
            };

            var additionalText = new InMemoryAdditionalText(filePath, "hello world");
            var array = GetAdditionalTextValues(
                CreateCompilation(new SyntaxTree[] { }),
                ImmutableArray.Create<AdditionalText>(additionalText),
                pathMap);

            var expected = @"
[
  {
    ""fileName"": ""c:\\src\\data.txt"",
    ""text"": {
      ""checksum"": ""8f8ceeeb5e1b799fe3c7dd9f059bf8852c57cb"",
      ""checksumAlgorithm"": ""Sha1"",
      ""encodingName"": ""Unicode (UTF-8)""
    }
  }
]
";

            AssertJsonCore(expected, array.ToString(Formatting.Indented));
        }

        private static AnalyzerConfig ParseAnalyzerConfig(string path, string content)
            => AnalyzerConfig.Parse(content, Path.GetFullPath(path));

        [Theory, CombinatorialData]
        public void AnalyzerConfigUnusedSections(bool globalConfig, bool withGenerator)
        {
            var directory = Path.GetFullPath("config-root");
            var sourcePath = Path.Combine(directory, "test.cs");
            var header = globalConfig ? Path.Combine(directory, "other.cs").Replace('\\', '/') : "*.vb";
            var configPath = Path.Combine(directory, globalConfig ? ".globalconfig" : ".editorconfig");
            ImmutableArray<AnalyzerConfig> firstConfigs = [ParseAnalyzerConfig(configPath, $"[{header}]\ncache_value = 1")];
            ImmutableArray<AnalyzerConfig> secondConfigs = [ParseAnalyzerConfig(configPath, $"[{header}]\ncache_value = 2")];
            ImmutableArray<ISourceGenerator> generators = withGenerator ? [new Generator()] : [];
            var compilation = CreateCompilation([ParseSyntaxTree("", sourcePath, HashAlgorithm)]);
            var key = compilation.GetDeterministicKey(
                generators: generators, analyzerConfigFiles: firstConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(firstConfigs),
                generatedSourceBaseDirectory: Path.Combine(directory, "generated"));
            var otherKey = compilation.GetDeterministicKey(
                generators: generators, analyzerConfigFiles: secondConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(secondConfigs),
                generatedSourceBaseDirectory: Path.Combine(directory, "generated"));

            if (withGenerator && !globalConfig)
            {
                Assert.NotEqual(key, otherKey);
            }
            else
            {
                Assert.Equal(key, otherKey);
                var config = Assert.Single((JArray)GetJsonProperty(key, "analyzerConfigs").Value);
                Assert.Empty(Assert.IsType<JArray>(config["sections"]));
            }
        }

        [Theory, CombinatorialData]
        public void AnalyzerConfigSectionBindingsRespectRoot(bool root)
        {
            var directory = Path.GetFullPath("config-root");
            var childDirectory = Path.Combine(directory, "child");
            var compilation = CreateCompilation([ParseSyntaxTree("", Path.Combine(childDirectory, "test.cs"), HashAlgorithm)]);
            var parent = ParseAnalyzerConfig(Path.Combine(directory, ".editorconfig"), "[*.cs]\ncache_value = parent");
            var changedParent = ParseAnalyzerConfig(Path.Combine(directory, ".editorconfig"), "[*.cs]\ncache_value = changed");
            var child = ParseAnalyzerConfig(Path.Combine(childDirectory, ".editorconfig"), $"root = {root.ToString().ToLowerInvariant()}\n[*.cs]\nchild_value = child");
            ImmutableArray<AnalyzerConfig> configs = [parent, child];
            ImmutableArray<AnalyzerConfig> changedConfigs = [changedParent, child];
            var key = compilation.GetDeterministicKey(analyzerConfigFiles: configs, analyzerConfigSet: AnalyzerConfigSet.Create(configs));
            var otherKey = compilation.GetDeterministicKey(analyzerConfigFiles: changedConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(changedConfigs));

            if (root)
            {
                Assert.Equal(key, otherKey);
            }
            else
            {
                Assert.NotEqual(key, otherKey);
            }

            var configEntries = (JArray)GetJsonProperty(key, "analyzerConfigs").Value;
            var parentSections = Assert.IsType<JArray>(configEntries[0]["sections"]);
            if (root)
            {
                Assert.Empty(parentSections);
            }
            else
            {
                var parentSection = Assert.Single(parentSections);
                var parentBinding = Assert.Single(Assert.IsType<JArray>(parentSection["sourceFiles"]));
                Assert.Equal(0, parentBinding.Value<int>("index"));
                Assert.Equal(0, parentBinding.Value<int>("order"));
            }
            var childSection = Assert.Single(Assert.IsType<JArray>(configEntries[1]["sections"]));
            var childBinding = Assert.Single(Assert.IsType<JArray>(childSection["sourceFiles"]));
            Assert.Equal(0, childBinding.Value<int>("index"));
            Assert.Equal(root ? 0 : 1, childBinding.Value<int>("order"));
        }

        [Fact]
        public void AnalyzerConfigSectionBindingsPreserveApplicationOrder()
        {
            var firstDirectory = Path.GetFullPath("config-root1") + Path.DirectorySeparatorChar;
            var secondDirectory = Path.GetFullPath("config-root2") + Path.DirectorySeparatorChar;
            var firstChild = Path.Combine(firstDirectory, "child") + Path.DirectorySeparatorChar;
            var secondChild = Path.Combine(secondDirectory, "child") + Path.DirectorySeparatorChar;
            ImmutableArray<AnalyzerConfig> firstConfigs =
            [
                ParseAnalyzerConfig(firstDirectory + ".editorconfig", "[*.cs]\ncache_value = first"),
                ParseAnalyzerConfig(firstChild + ".editorconfig", "[*.cs]\ncache_value = second"),
            ];
            ImmutableArray<AnalyzerConfig> secondConfigs =
            [
                ParseAnalyzerConfig(secondChild + ".editorconfig", "[*.cs]\ncache_value = first"),
                ParseAnalyzerConfig(secondDirectory + ".editorconfig", "[*.cs]\ncache_value = second"),
            ];
            ImmutableArray<KeyValuePair<string, string>> pathMap =
            [
                KeyValuePair.Create(firstChild, "/_/"),
                KeyValuePair.Create(secondChild, "/_/"),
                KeyValuePair.Create(firstDirectory, "/_/"),
                KeyValuePair.Create(secondDirectory, "/_/"),
            ];
            var key = CreateCompilation([ParseSyntaxTree("", firstChild + "test.cs", HashAlgorithm)]).GetDeterministicKey(
                pathMap: pathMap, analyzerConfigFiles: firstConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(firstConfigs));
            var otherKey = CreateCompilation([ParseSyntaxTree("", secondChild + "test.cs", HashAlgorithm)]).GetDeterministicKey(
                pathMap: pathMap, analyzerConfigFiles: secondConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(secondConfigs));

            Assert.NotEqual(key, otherKey);
            var first = (JArray)GetJsonProperty(key, "analyzerConfigs").Value;
            var second = (JArray)GetJsonProperty(otherKey, "analyzerConfigs").Value;
            Assert.Equal(0, first[0]["sections"]![0]!["sourceFiles"]![0]!.Value<int>("order"));
            Assert.Equal(1, first[1]["sections"]![0]!["sourceFiles"]![0]!.Value<int>("order"));
            Assert.Equal(1, second[0]["sections"]![0]!["sourceFiles"]![0]!.Value<int>("order"));
            Assert.Equal(0, second[1]["sections"]![0]!["sourceFiles"]![0]!.Value<int>("order"));
        }

        [Fact]
        public void AnalyzerConfigGlobalSectionBindings()
        {
            var directory = Path.GetFullPath("config-root");
            var sourcePath = Path.Combine(directory, "test.cs");
            var header = sourcePath.Replace('\\', '/');
            ImmutableArray<AnalyzerConfig> configs =
            [
                ParseAnalyzerConfig(Path.Combine(directory, "first.globalconfig"), $"is_global = true\nglobal_level = 1\n[{header}]\ncache_value = first"),
                ParseAnalyzerConfig(Path.Combine(directory, "second.globalconfig"), $"is_global = true\nglobal_level = 2\n[{header}]\ncache_value = second"),
            ];
            var key = CreateCompilation([ParseSyntaxTree("", sourcePath, HashAlgorithm)]).GetDeterministicKey(
                analyzerConfigFiles: configs, analyzerConfigSet: AnalyzerConfigSet.Create(configs));
            var configEntries = (JArray)GetJsonProperty(key, "analyzerConfigs").Value;

            for (int i = 0; i < configs.Length; i++)
            {
                var section = Assert.Single(Assert.IsType<JArray>(configEntries[i]["sections"]));
                var binding = Assert.Single(Assert.IsType<JArray>(section["sourceFiles"]));
                Assert.Equal(0, binding.Value<int>("index"));
                Assert.Equal(0, binding.Value<int>("order"));
                Assert.Equal(i + 1, configEntries[i].Value<int>("globalLevel"));
            }
        }

        [Fact]
        public void AnalyzerConfigDefaultAndEmpty()
        {
            var compilation = CreateCompilation([]);
            var key = compilation.GetDeterministicKey();

            Assert.Equal(key, compilation.GetDeterministicKey(analyzerConfigFiles: []));
            AssertJsonSection("""
                "analyzerConfigs": []
                """, key, "analyzerConfigs");
            ImmutableArray<AnalyzerConfig> configs = [ParseAnalyzerConfig(".editorconfig", "")];
            Assert.NotEqual(key, compilation.GetDeterministicKey(
                analyzerConfigFiles: configs, analyzerConfigSet: AnalyzerConfigSet.Create(configs)));
        }

        [Theory, CombinatorialData]
        public void AnalyzerConfigContent(bool globalConfig)
        {
            var content = globalConfig ? "is_global = true\ncache_value = 1" : "";
            var changedContent = globalConfig ? "is_global = true\ncache_value = 2" : "root = true";
            var compilation = CreateCompilation([]);
            ImmutableArray<AnalyzerConfig> configs = [ParseAnalyzerConfig(".globalconfig", content)];
            ImmutableArray<AnalyzerConfig> changedConfigs = [ParseAnalyzerConfig(".globalconfig", changedContent)];
            var configSet = AnalyzerConfigSet.Create(configs);
            var key = compilation.GetDeterministicKey(analyzerConfigFiles: configs, analyzerConfigSet: configSet);

            Assert.NotEqual(key, compilation.GetDeterministicKey(
                analyzerConfigFiles: changedConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(changedConfigs)));
            Assert.Equal(key, compilation.GetDeterministicKey(analyzerConfigFiles: configs, analyzerConfigSet: configSet));
            Assert.Equal(key, Compilation.GetDeterministicKey(
                compilation.Options, [], compilation.References.ToImmutableArray(), ImmutableArray<byte>.Empty,
                analyzerConfigFiles: configs, analyzerConfigSet: configSet));
            Assert.Equal(key, DeterministicKey.GetDeterministicKey(
                compilation.Options, [], compilation.References.ToImmutableArray(), ImmutableArray<byte>.Empty,
                analyzerConfigFiles: configs, analyzerConfigSet: configSet));
        }

        [Theory, CombinatorialData]
        public void AnalyzerConfigPaths(bool pathMapped, bool ignorePaths)
        {
            var compilation = CreateCompilation([]);
            var firstDirectory = Path.GetFullPath("config-root1") + Path.DirectorySeparatorChar;
            var secondDirectory = Path.GetFullPath("config-root2") + Path.DirectorySeparatorChar;
            ImmutableArray<KeyValuePair<string, string>> pathMap = pathMapped
                ? [KeyValuePair.Create(firstDirectory, "/_/"), KeyValuePair.Create(secondDirectory, "/_/")]
                : [];
            var options = ignorePaths ? DeterministicKeyOptions.IgnorePaths : DeterministicKeyOptions.Default;
            ImmutableArray<AnalyzerConfig> firstConfigs = [ParseAnalyzerConfig(firstDirectory + ".editorconfig", "root = true")];
            ImmutableArray<AnalyzerConfig> secondConfigs = [ParseAnalyzerConfig(secondDirectory + ".editorconfig", "root = true")];
            var key = compilation.GetDeterministicKey(
                analyzerConfigFiles: firstConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(firstConfigs), pathMap: pathMap, options: options);
            var otherKey = compilation.GetDeterministicKey(
                analyzerConfigFiles: secondConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(secondConfigs), pathMap: pathMap, options: options);

            if (pathMapped || ignorePaths)
            {
                Assert.Equal(key, otherKey);
            }
            else
            {
                Assert.NotEqual(key, otherKey);
            }

            var config = Assert.Single((JArray)GetJsonProperty(key, "analyzerConfigs").Value);
            Assert.Equal(ignorePaths ? ".editorconfig" : pathMapped ? "/_/.editorconfig" : firstDirectory + ".editorconfig", config.Value<string>("fileName"));
        }

        [Theory, CombinatorialData]
        public void AnalyzerConfigSectionBindings(bool additionalFile, bool differentOptions)
        {
            var firstDirectory = Path.GetFullPath("config-root1") + Path.DirectorySeparatorChar;
            var secondDirectory = Path.GetFullPath("config-root2") + Path.DirectorySeparatorChar;
            var firstPath = firstDirectory + "test.cs";
            var secondPath = secondDirectory + "test.cs";
            var configPath = Path.GetFullPath(".globalconfig");
            var content = $"""
                is_global = true
                [{firstPath.Replace('\\', '/')}]
                dotnet_diagnostic.TEST0001.severity = none
                cache_value = 1
                [{secondPath.Replace('\\', '/')}]
                dotnet_diagnostic.TEST0001.severity = {(differentOptions ? "warning" : "none")}
                cache_value = {(differentOptions ? "2" : "1")}
                """;
            ImmutableArray<AnalyzerConfig> configs = [ParseAnalyzerConfig(configPath, content)];
            var configSet = AnalyzerConfigSet.Create(configs);
            ImmutableArray<KeyValuePair<string, string>> pathMap =
                [KeyValuePair.Create(firstDirectory, "/_/"), KeyValuePair.Create(secondDirectory, "/_/")];
            var firstCompilation = CreateCompilation(additionalFile ? [] : [ParseSyntaxTree("", firstPath, HashAlgorithm)]);
            var secondCompilation = CreateCompilation(additionalFile ? [] : [ParseSyntaxTree("", secondPath, HashAlgorithm)]);
            ImmutableArray<AdditionalText> firstAdditional = additionalFile ? [new TestAdditionalText(firstPath, SourceText.From(""))] : [];
            ImmutableArray<AdditionalText> secondAdditional = additionalFile ? [new TestAdditionalText(secondPath, SourceText.From(""))] : [];
            var key = firstCompilation.GetDeterministicKey(
                additionalTexts: firstAdditional, pathMap: pathMap, analyzerConfigFiles: configs, analyzerConfigSet: configSet);
            var otherKey = secondCompilation.GetDeterministicKey(
                additionalTexts: secondAdditional, pathMap: pathMap, analyzerConfigFiles: configs, analyzerConfigSet: configSet);

            var section = additionalFile ? "additionalFiles" : "sourceFiles";
            var firstConfig = Assert.Single((JArray)GetJsonProperty(key, "analyzerConfigs").Value);
            var secondConfig = Assert.Single((JArray)GetJsonProperty(otherKey, "analyzerConfigs").Value);
            var firstSection = Assert.Single(Assert.IsType<JArray>(firstConfig["sections"]));
            var secondSection = Assert.Single(Assert.IsType<JArray>(secondConfig["sections"]));
            var firstBinding = Assert.Single(Assert.IsType<JArray>(firstSection[section]));
            var secondBinding = Assert.Single(Assert.IsType<JArray>(secondSection[section]));
            Assert.Equal(0, firstBinding.Value<int>("index"));
            Assert.Equal(0, secondBinding.Value<int>("index"));
            Assert.Equal("none", firstSection["properties"]!["dotnet_diagnostic.test0001.severity"]!.Value<string>());
            Assert.Equal("1", firstSection["properties"]!["cache_value"]!.Value<string>());
            Assert.Equal(differentOptions ? "warning" : "none", secondSection["properties"]!["dotnet_diagnostic.test0001.severity"]!.Value<string>());
            Assert.Equal(differentOptions ? "2" : "1", secondSection["properties"]!["cache_value"]!.Value<string>());
            Assert.Null(JObject.Parse(key)["analyzerConfigOptions"]);
            if (differentOptions)
            {
                Assert.NotEqual(key, otherKey);
            }
            else
            {
                Assert.Equal(key, otherKey);
            }
        }

        [Theory, CombinatorialData]
        public void AnalyzerConfigMappedProperties(
            [CombinatorialValues("build_property.ProjectDir", "build_property.MSBuildProjectDirectory", "custom_path")] string property,
            bool pathMapped,
            bool trailingSeparator)
        {
            var firstDirectory = Path.GetFullPath("config-root1") + Path.DirectorySeparatorChar;
            var secondDirectory = Path.GetFullPath("config-root2") + Path.DirectorySeparatorChar;
            var firstValue = trailingSeparator ? firstDirectory : PathUtilities.TrimTrailingSeparators(firstDirectory);
            var secondValue = trailingSeparator ? secondDirectory : PathUtilities.TrimTrailingSeparators(secondDirectory);
            ImmutableArray<AnalyzerConfig> firstConfigs =
                [ParseAnalyzerConfig(firstDirectory + ".globalconfig", $"is_global = true\n{property} = {firstValue}")];
            ImmutableArray<AnalyzerConfig> secondConfigs =
                [ParseAnalyzerConfig(secondDirectory + ".globalconfig", $"is_global = true\n{property} = {secondValue}")];
            ImmutableArray<KeyValuePair<string, string>> pathMap = pathMapped
                ? [KeyValuePair.Create(firstDirectory, "/_/"), KeyValuePair.Create(secondDirectory, "/_/")]
                : [];
            var compilation = CreateCompilation([]);
            var key = compilation.GetDeterministicKey(
                pathMap: pathMap, analyzerConfigFiles: firstConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(firstConfigs));
            var otherKey = compilation.GetDeterministicKey(
                pathMap: pathMap, analyzerConfigFiles: secondConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(secondConfigs));

            if (pathMapped && property != "custom_path")
            {
                Assert.Equal(key, otherKey);
                var config = Assert.Single((JArray)GetJsonProperty(key, "analyzerConfigs").Value);
                var options = Assert.IsType<JObject>(config["properties"]);
                Assert.Equal(trailingSeparator ? "/_/" : "/_", options.Value<string>(property.ToLowerInvariant()));
            }
            else
            {
                Assert.NotEqual(key, otherKey);
            }
        }

        [Theory, CombinatorialData]
        public void AnalyzerConfigSectionRelocation(
            [CombinatorialValues(null, "*.cs", "src/**/*.cs", "*.{cs,vb}")] string? pattern,
            bool withGenerator,
            bool additionalFile)
        {
            var firstDirectory = Path.GetFullPath("config-root1") + Path.DirectorySeparatorChar;
            var secondDirectory = Path.GetFullPath("config-root2") + Path.DirectorySeparatorChar;
            var firstPath = Path.Combine(firstDirectory, "src", "nested", "test.cs");
            var secondPath = Path.Combine(secondDirectory, "src", "nested", "test.cs");
            var firstHeader = pattern ?? firstPath.Replace('\\', '/');
            var secondHeader = pattern ?? secondPath.Replace('\\', '/');
            var configFileName = pattern is null ? ".globalconfig" : ".editorconfig";
            ImmutableArray<AnalyzerConfig> firstConfigs =
                [ParseAnalyzerConfig(firstDirectory + configFileName, $"[{firstHeader}]\ncache_value = 1")];
            ImmutableArray<AnalyzerConfig> secondConfigs =
                [ParseAnalyzerConfig(secondDirectory + configFileName, $"[{secondHeader}]\ncache_value = 1")];
            ImmutableArray<KeyValuePair<string, string>> pathMap =
                [KeyValuePair.Create(firstDirectory, "/_/"), KeyValuePair.Create(secondDirectory, "/_/")];
            ImmutableArray<ISourceGenerator> generators = withGenerator ? [new Generator()] : [];
            ImmutableArray<AdditionalText> firstAdditional = additionalFile ? [new TestAdditionalText(firstPath, SourceText.From(""))] : [];
            ImmutableArray<AdditionalText> secondAdditional = additionalFile ? [new TestAdditionalText(secondPath, SourceText.From(""))] : [];
            var key = CreateCompilation(additionalFile ? [] : [ParseSyntaxTree("", firstPath, HashAlgorithm)]).GetDeterministicKey(
                pathMap: pathMap, generators: generators, additionalTexts: firstAdditional,
                analyzerConfigFiles: firstConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(firstConfigs),
                generatedSourceBaseDirectory: Path.Combine(firstDirectory, "generated"));
            var otherKey = CreateCompilation(additionalFile ? [] : [ParseSyntaxTree("", secondPath, HashAlgorithm)]).GetDeterministicKey(
                pathMap: pathMap, generators: generators, additionalTexts: secondAdditional,
                analyzerConfigFiles: secondConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(secondConfigs),
                generatedSourceBaseDirectory: Path.Combine(secondDirectory, "generated"));

            Assert.Equal(key, otherKey);
            var section = additionalFile ? "additionalFiles" : "sourceFiles";
            var config = Assert.Single((JArray)GetJsonProperty(key, "analyzerConfigs").Value);
            var configSection = Assert.Single(Assert.IsType<JArray>(config["sections"]));
            var binding = Assert.Single(Assert.IsType<JArray>(configSection[section]));
            Assert.Equal(0, binding.Value<int>("index"));
            Assert.Equal("1", configSection["properties"]!["cache_value"]!.Value<string>());
        }

        [Fact]
        public void AnalyzerConfigMappedDirectoryRoot()
        {
            var directory = Path.GetFullPath("config-root");
            ImmutableArray<AnalyzerConfig> configs =
                [ParseAnalyzerConfig(Path.Combine(directory, ".globalconfig"), $"build_property.ProjectDir = {directory}")];
            var key = CreateCompilation([]).GetDeterministicKey(
                pathMap: [KeyValuePair.Create(directory + Path.DirectorySeparatorChar, "/")],
                analyzerConfigFiles: configs, analyzerConfigSet: AnalyzerConfigSet.Create(configs));

            var config = Assert.Single((JArray)GetJsonProperty(key, "analyzerConfigs").Value);
            var options = Assert.IsType<JObject>(config["properties"]);
            Assert.Equal("/", options.Value<string>("build_property.projectdir"));
        }

        [Theory, CombinatorialData]
        public void AnalyzerConfigGeneratedSectionMatching(bool relocateSection)
        {
            var firstDirectory = Path.GetFullPath("config-root1") + Path.DirectorySeparatorChar;
            var secondDirectory = Path.GetFullPath("config-root2") + Path.DirectorySeparatorChar;
            var firstGeneratedDirectory = Path.Combine(firstDirectory, "generated");
            var secondGeneratedDirectory = Path.Combine(secondDirectory, "generated");
            var firstPath = Path.Combine(firstGeneratedDirectory, "Generator[1].g.cs").Replace('\\', '/');
            var secondPath = relocateSection
                ? Path.Combine(secondGeneratedDirectory, "Generator[1].g.cs").Replace('\\', '/')
                : firstPath;
            var firstHeader = firstPath.Replace("[", "\\[").Replace("]", "\\]");
            var secondHeader = secondPath.Replace("[", "\\[").Replace("]", "\\]");
            ImmutableArray<AnalyzerConfig> firstConfigs =
                [ParseAnalyzerConfig(firstDirectory + ".globalconfig", $"[{firstHeader}]\ncache_value = 1")];
            ImmutableArray<AnalyzerConfig> secondConfigs =
                [ParseAnalyzerConfig(secondDirectory + ".globalconfig", $"[{secondHeader}]\ncache_value = 1")];
            ImmutableArray<KeyValuePair<string, string>> pathMap =
                [KeyValuePair.Create(firstDirectory, "/_/"), KeyValuePair.Create(secondDirectory, "/_/")];
            ImmutableArray<ISourceGenerator> generators = [new Generator()];
            var compilation = CreateCompilation([]);
            var key = compilation.GetDeterministicKey(
                pathMap: pathMap, generators: generators,
                analyzerConfigFiles: firstConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(firstConfigs),
                generatedSourceBaseDirectory: firstGeneratedDirectory);
            var otherKey = compilation.GetDeterministicKey(
                pathMap: pathMap, generators: generators,
                analyzerConfigFiles: secondConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(secondConfigs),
                generatedSourceBaseDirectory: secondGeneratedDirectory);

            var firstConfig = Assert.Single((JArray)GetJsonProperty(key, "analyzerConfigs").Value);
            var secondConfig = Assert.Single((JArray)GetJsonProperty(otherKey, "analyzerConfigs").Value);
            var firstSection = Assert.Single(Assert.IsType<JArray>(firstConfig["sections"]));
            Assert.Equal("/_/generated/Generator[1].g.cs", firstSection.Value<string>("name"));
            Assert.Equal("Generator[1].g.cs", firstSection.Value<string>("generatedFileName"));
            if (relocateSection)
            {
                var secondSection = Assert.Single(Assert.IsType<JArray>(secondConfig["sections"]));
                Assert.Equal(firstSection.Value<string>("name"), secondSection.Value<string>("name"));
                Assert.Equal("Generator[1].g.cs", secondSection.Value<string>("generatedFileName"));
                Assert.Equal(key, otherKey);
            }
            else
            {
                Assert.Empty(Assert.IsType<JArray>(secondConfig["sections"]));
                Assert.NotEqual(key, otherKey);
            }
        }

        [Theory, CombinatorialData]
        public void AnalyzerConfigGeneratedDirectoryRelationships(bool moveConfig)
        {
            var directory = Path.GetFullPath("config-root") + Path.DirectorySeparatorChar;
            var firstConfigDirectory = moveConfig ? Path.Combine(directory, "config1") + Path.DirectorySeparatorChar : directory;
            var secondConfigDirectory = moveConfig ? Path.Combine(directory, "config2") + Path.DirectorySeparatorChar : directory;
            var firstGeneratedDirectory = moveConfig ? directory : Path.Combine(directory, "generated1");
            var secondGeneratedDirectory = moveConfig ? directory : Path.Combine(directory, "generated2");
            var compilation = CreateCompilation([]);
            ImmutableArray<ISourceGenerator> generators = [new Generator()];
            ImmutableArray<AnalyzerConfig> firstConfigs = [ParseAnalyzerConfig(firstConfigDirectory + ".editorconfig", "[generated1/**/*.cs]\ncache_value = 1")];
            ImmutableArray<AnalyzerConfig> secondConfigs = [ParseAnalyzerConfig(secondConfigDirectory + ".editorconfig", "[generated1/**/*.cs]\ncache_value = 1")];
            ImmutableArray<KeyValuePair<string, string>> pathMap =
                [KeyValuePair.Create(firstConfigDirectory, "/_/"), KeyValuePair.Create(secondConfigDirectory, "/_/")];
            var key = compilation.GetDeterministicKey(
                pathMap: pathMap, generators: generators, analyzerConfigFiles: firstConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(firstConfigs),
                generatedSourceBaseDirectory: firstGeneratedDirectory);
            var otherKey = compilation.GetDeterministicKey(
                pathMap: pathMap, generators: generators, analyzerConfigFiles: secondConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(secondConfigs),
                generatedSourceBaseDirectory: secondGeneratedDirectory);

            Assert.NotEqual(key, otherKey);
            var config = Assert.Single((JArray)GetJsonProperty(key, "analyzerConfigs").Value);
            Assert.Equal(moveConfig ? ".." : "generated1", config["generatedSourceDirectory"]!["relativeToConfig"]!.Value<string>());
            Assert.Equal(moveConfig ? "config1" : "..", config["generatedSourceDirectory"]!["configRelativeToGenerated"]!.Value<string>());
        }

        [Fact]
        public void AnalyzerConfigMultipleFiles()
        {
            var compilation = CreateCompilation([]);
            ImmutableArray<AnalyzerConfig> configs =
            [
                ParseAnalyzerConfig(".globalconfig", "is_global = true"),
                ParseAnalyzerConfig(".editorconfig", "root = true"),
            ];
            var key = compilation.GetDeterministicKey(analyzerConfigFiles: configs, analyzerConfigSet: AnalyzerConfigSet.Create(configs));
            var array = (JArray)GetJsonProperty(key, "analyzerConfigs").Value;

            Assert.Equal(configs.Select(config => config.PathToFile), array.Select(config => config.Value<string>("fileName")));
            ImmutableArray<AnalyzerConfig> fewerConfigs = [configs[0]];
            ImmutableArray<AnalyzerConfig> reversedConfigs = [configs[1], configs[0]];
            ImmutableArray<AnalyzerConfig> changedConfigs = [configs[0], ParseAnalyzerConfig(configs[1].PathToFile, "root = false")];
            Assert.NotEqual(key, compilation.GetDeterministicKey(
                analyzerConfigFiles: fewerConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(fewerConfigs)));
            Assert.NotEqual(key, compilation.GetDeterministicKey(
                analyzerConfigFiles: reversedConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(reversedConfigs)));
            Assert.NotEqual(key, compilation.GetDeterministicKey(
                analyzerConfigFiles: changedConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(changedConfigs)));
        }

        [Fact]
        public void AnalyzerConfigProperties()
        {
            var compilation = CreateCompilation([]);
            ImmutableArray<AnalyzerConfig> configs = [ParseAnalyzerConfig(".editorconfig", "root = true\ncache_value = 1")];
            ImmutableArray<AnalyzerConfig> reorderedConfigs = [ParseAnalyzerConfig(".editorconfig", "# comment\ncache_value = 1\nroot = true")];
            var key = compilation.GetDeterministicKey(analyzerConfigFiles: configs, analyzerConfigSet: AnalyzerConfigSet.Create(configs));
            var otherKey = compilation.GetDeterministicKey(analyzerConfigFiles: reorderedConfigs, analyzerConfigSet: AnalyzerConfigSet.Create(reorderedConfigs));

            Assert.Equal(key, otherKey);
            var config = Assert.Single((JArray)GetJsonProperty(key, "analyzerConfigs").Value);
            var properties = Assert.IsType<JObject>(config["properties"]);
            Assert.Equal(new[] { "cache_value", "root" }, properties.Properties().Select(property => property.Name));
            Assert.Equal("1", properties.Value<string>("cache_value"));
            Assert.Equal("true", properties.Value<string>("root"));
        }

        [Fact]
        public void AdditionalTextError()
        {
            var additionalText = new TestAdditionalText(path: @"test.txt", text: null);
            var array = GetAdditionalTextValues(
                CreateCompilation(new SyntaxTree[] { }),
                ImmutableArray.Create<AdditionalText>(additionalText));

            var expected = @"
[
  {
    ""fileName"": ""test.txt"",
    ""text"": null
  }
]
";

            AssertJsonCore(expected, array.ToString(Formatting.Indented));
        }

        [Fact]
        public void Analyzers()
        {
            var array = GetAnalyzerValues(
                CreateCompilation(Array.Empty<SyntaxTree>()),
                new Analyzer(),
                new Analyzer2());

            var assembly = typeof(Analyzer).Assembly;
            var expected = @$"
[
  {{
    ""fullName"": ""{typeof(Analyzer).FullName}"",
    ""assemblyName"": ""{assembly.FullName}"",
    ""mvid"": ""{DeterministicKeyBuilder.GetGuidValue(assembly.ManifestModule.ModuleVersionId)}""
  }},
  {{
    ""fullName"": ""{typeof(Analyzer2).FullName}"",
    ""assemblyName"": ""{assembly.FullName}"",
    ""mvid"": ""{DeterministicKeyBuilder.GetGuidValue(assembly.ManifestModule.ModuleVersionId)}""
  }}
]
";
            AssertJsonCore(expected, array.ToString(Formatting.Indented));
        }

        [Fact]
        public void Generators()
        {
            var array = GetGeneratorValues(
                CreateCompilation(Array.Empty<SyntaxTree>()),
                new Generator(),
                new Generator2(),
                new Generator3().AsSourceGenerator());

            var assembly = typeof(Generator).Assembly;
            var expected = @$"
[
  {{
    ""fullName"": ""{typeof(Generator).FullName}"",
    ""assemblyName"": ""{assembly.FullName}"",
    ""mvid"": ""{DeterministicKeyBuilder.GetGuidValue(assembly.ManifestModule.ModuleVersionId)}""
  }},
  {{
    ""fullName"": ""{typeof(Generator2).FullName}"",
    ""assemblyName"": ""{assembly.FullName}"",
    ""mvid"": ""{DeterministicKeyBuilder.GetGuidValue(assembly.ManifestModule.ModuleVersionId)}""
  }},
  {{
    ""fullName"": ""{typeof(Generator3).FullName}"",
    ""assemblyName"": ""{assembly.FullName}"",
    ""mvid"": ""{DeterministicKeyBuilder.GetGuidValue(assembly.ManifestModule.ModuleVersionId)}""
  }}
]
";
            AssertJsonCore(expected, array.ToString(Formatting.Indented));
        }
    }
}
