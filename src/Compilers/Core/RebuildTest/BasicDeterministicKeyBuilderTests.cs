// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Roslyn.Test.Utilities;
using Xunit;
using Newtonsoft;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.VisualBasic.UnitTests;
using System.Collections.Generic;
using System;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public sealed class BasicDeterministicKeyBuilderTests : DeterministicKeyBuilderTests<VisualBasicCompilation, VisualBasicCompilationOptions, VisualBasicParseOptions>
    {
        public static VisualBasicCompilationOptions BasicOptions { get; } = new VisualBasicCompilationOptions(OutputKind.ConsoleApplication, deterministic: true);

        protected override SyntaxTree ParseSyntaxTree(string content, string fileName, SourceHashAlgorithm hashAlgorithm, VisualBasicParseOptions? parseOptions) =>
            VisualBasicSyntaxTree.ParseText(
                SourceText.From(content, checksumAlgorithm: hashAlgorithm, encoding: Encoding.UTF8),
                path: fileName,
                options: parseOptions);

        protected override VisualBasicCompilation CreateCompilation(SyntaxTree[] syntaxTrees, MetadataReference[]? references = null, VisualBasicCompilationOptions? options = null)
            => VisualBasicCompilation.Create(
                "test",
                syntaxTrees,
                references ?? NetCoreApp.References.ToArray(),
                options: options ?? BasicOptions);

        protected override VisualBasicCompilationOptions GetCompilationOptions() => BasicOptions;

        protected override VisualBasicParseOptions GetParseOptions() => VisualBasicParseOptions.Default;

        private protected override DeterministicKeyBuilder GetDeterministicKeyBuilder() => VisualBasicDeterministicKeyBuilder.Instance;

        /// <summary>
        /// This check monitors the set of properties and fields on the various option types
        /// that contribute to the deterministic checksum of a <see cref="Compilation"/>. When
        /// any of these tests change that means the new property or field needs to be evaluated
        /// for inclusion into the checksum
        /// </summary>
        [Fact]
        public void VerifyUpToDate()
        {
            verifyCount<ParseOptions>(11);
            verifyCount<VisualBasicParseOptions>(10);
            verifyCount<CompilationOptions>(63);
            verifyCount<VisualBasicCompilationOptions>(22);

            static void verifyCount<T>(int expected)
            {
                var type = typeof(T);
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance;
                var fields = type.GetFields(flags);
                var properties = type.GetProperties(flags);
                var count = fields.Length + properties.Length;
                Assert.Equal(expected, count);
            }
        }

        [Theory]
        [InlineData(@"hello world")]
        [InlineData(@"just need some text here")]
        [InlineData(@"yet another case")]
        public void ContentInAdditionalText(string content)
        {
            var syntaxTree = VisualBasicSyntaxTree.ParseText(
                "",
                path: "file.vb");
            var additionalText = new TestAdditionalText(content, Encoding.UTF8, path: "file.txt", HashAlgorithm);
            var contentChecksum = GetChecksum(additionalText.GetText()!);

            var compilation = VisualBasicCompilation.Create(
                "test",
                new[] { syntaxTree },
                NetCoreApp.References,
                options: BasicOptions);
            var key = compilation.GetDeterministicKey(additionalTexts: ImmutableArray.Create<AdditionalText>(additionalText));
            var expected = @$"
""additionalTexts"": [
  {{
    ""fileName"": ""file.txt"",
    ""text"": {{
      ""checksum"": ""{contentChecksum}"",
      ""checksumAlgorithm"": ""Sha256"",
      ""encodingName"": ""Unicode (UTF-8)""
    }}
  }}
]";
            AssertJsonSection(expected, key, "additionalTexts");
        }

        [Fact]
        public void GlobalImports()
        {
            var syntaxTree = VisualBasicSyntaxTree.ParseText(
                "",
                path: "file.vb");

            var options = BasicOptions
                .WithGlobalImports(new[]
                {
                    GlobalImport.Parse(@"<xmlns:xmlNamespacePrefix = ""xmlNamespaceName"">"),
                    GlobalImport.Parse("System.Xml")
                });
            var compilation = VisualBasicCompilation.Create(
                "test",
                new[] { syntaxTree },
                NetCoreApp.References,
                options: options);
            var key = compilation.GetDeterministicKey();
            var expected = @"
""globalImports"": [
  {
    ""name"": ""<xmlns:xmlNamespacePrefix = \""xmlNamespaceName\"">"",
    ""isXmlClause"": true
  },
  {
    ""name"": ""System.Xml"",
    ""isXmlClause"": false
  }
]";

            AssertJsonSection(expected, key, "compilation.options.globalImports");
        }

        [Theory]
        [CombinatorialData]
        public void BasicParseOptionsLanguageVersion(LanguageVersion languageVersion)
        {
            var parseOptions = VisualBasicParseOptions.Default.WithLanguageVersion(languageVersion);
            var obj = GetParseOptionsValue(parseOptions);
            var effective = languageVersion.MapSpecifiedToEffectiveVersion();

            Assert.Equal(effective.ToString(), obj.Value<string>("languageVersion"));
            Assert.Equal(languageVersion.ToString(), obj.Value<string>("specifiedLanguageVersion"));
        }

        [Fact]
        public void BasicPreprocessorSymbols()
        {
            assert(@"{}");

            assert(@"
{
  ""DEBUG"": null
}", ("DEBUG", null));

            assert(@"
{
  ""DEBUG"": null,
  ""TRACE"": null
}", ("TRACE", null), ("DEBUG", null));

            assert(@"
{
  ""DEBUG"": ""13"",
  ""TRACE"": ""42""
}", ("TRACE", 42), ("DEBUG", 13));

            assert(@"
{
  ""DEBUG"": ""4.2"",
  ""TRACE"": true
}", ("TRACE", true), ("DEBUG", 4.2));

            void assert(string? expected, params (string Key, object? Value)[] values)
            {
                var parseOptions = VisualBasicParseOptions.Default.WithPreprocessorSymbols(values.Select(x => new KeyValuePair<string, object>(x.Key, x.Value!)));
                var obj = GetParseOptionsValue(parseOptions);
                AssertJsonCore(expected, obj.Value<JObject>("preprocessorSymbols")?.ToString(Formatting.Indented));
            }
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [InlineData(@"c:\src\code.vb", @"c:\src", null)]
        [InlineData(@"d:\src\code.vb", @"d:\src\", @"/pathmap:d:\=c:\")]
        [InlineData(@"e:\long\path\src\code.vb", @"e:\long\path\src\", @"/pathmap:e:\long\path\=c:\")]
        public void BasicPathMapWindows(string filePath, string workingDirectory, string? pathMap)
        {
            var args = new List<string>(new[] { filePath, "/nostdlib", "/vbruntime-", "/langversion:15" });
            if (pathMap is not null)
            {
                args.Add(pathMap);
            }

            var compiler = new MockVisualBasicCompiler(
                baseDirectory: workingDirectory,
                args.ToArray());
            compiler.FileSystem = TestableFileSystem.CreateForFiles((filePath, new TestableFile("hello")));
            AssertSyntaxTreePathMap(@"
[
  {
    ""fileName"": ""c:\\src\\code.vb"",
    ""text"": {
      ""checksum"": ""2cf24dba5fb0a3e26e83b2ac5b9e29e1b161e5c1fa7425e7343362938b9824"",
      ""checksumAlgorithm"": ""Sha256"",
      ""encodingName"": ""Unicode (UTF-8)""
    },
    ""parseOptions"": {
      ""kind"": ""Regular"",
      ""specifiedKind"": ""Regular"",
      ""documentationMode"": ""None"",
      ""language"": ""Visual Basic"",
      ""features"": {},
      ""languageVersion"": ""VisualBasic15"",
      ""specifiedLanguageVersion"": ""VisualBasic15"",
      ""preprocessorSymbols"": {
        ""TARGET"": ""exe"",
        ""VBC_VER"": ""17.13""
      }
    }
  }
]
", compiler);
        }

        [Fact]
        public void MetadataReferenceCompilation()
        {
            var utilCompilation = VisualBasicCompilation.Create(
                assemblyName: "util",
                syntaxTrees: new[] { VisualBasicSyntaxTree.ParseText(@"// this is a comment", VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic15)) },
                options: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true));
            var compilation = CreateCompilation(
                Array.Empty<SyntaxTree>(),
                references: new[] { utilCompilation.ToMetadataReference() });
            var references = GetReferenceValues(compilation);
            var compilationValue = references.Values<JObject>().Single()!;
            var expected = @"
{
  ""compilation"": {
    ""publicKey"": """",
    ""options"": {
      ""outputKind"": ""DynamicallyLinkedLibrary"",
      ""moduleName"": null,
      ""scriptClassName"": ""Script"",
      ""mainTypeName"": null,
      ""cryptoPublicKey"": """",
      ""cryptoKeyFile"": null,
      ""delaySign"": null,
      ""publicSign"": false,
      ""checkOverflow"": true,
      ""platform"": ""AnyCpu"",
      ""optimizationLevel"": ""Debug"",
      ""generalDiagnosticOption"": ""Default"",
      ""warningLevel"": 1,
      ""deterministic"": true,
      ""debugPlusMode"": false,
      ""referencesSupersedeLowerVersions"": false,
      ""reportSuppressedDiagnostics"": false,
      ""nullableContextOptions"": ""Disable"",
      ""specificDiagnosticOptions"": [],
      ""localtime"": null,
      ""rootNamespace"": """",
      ""optionStrict"": ""Off"",
      ""optionInfer"": true,
      ""optionExplicit"": true,
      ""optionCompareText"": false,
      ""embedVbCoreRuntime"": false,
      ""globalImports"": [],
      ""parseOptions"": null
    },
    ""syntaxTrees"": [
      {
        ""fileName"": """",
        ""text"": {
          ""checksum"": ""053e2a4aa83f63193c1069d651b63bedca1e97"",
          ""checksumAlgorithm"": ""Sha1"",
          ""encodingName"": null
        },
        ""parseOptions"": {
          ""kind"": ""Regular"",
          ""specifiedKind"": ""Regular"",
          ""documentationMode"": ""Parse"",
          ""language"": ""Visual Basic"",
          ""features"": {},
          ""languageVersion"": ""VisualBasic15"",
          ""specifiedLanguageVersion"": ""VisualBasic15"",
          ""preprocessorSymbols"": {
            ""_MYTYPE"": ""Empty""
          }
        }
      }
    ]
  }
}
";

            AssertJson(expected, compilationValue.ToString(Formatting.Indented), "toolsVersions", "references", "extensions");
        }

        [Fact]
        public void FeatureFlag()
        {
            Assert.Equal("debug-determinism", CodeAnalysis.Feature.DebugDeterminism);
            var compiler = TestableCompiler.CreateBasicNetCoreApp("test.vb", @"-t:library", "-nologo", "-features:debug-determinism", "-deterministic", @"-define:_MYTYPE=""Empty""", "-debug:portable");
            var sourceFile = compiler.AddSourceFile("test.vb", @"' this is a test file");
            compiler.AddOutputFile("test.dll");
            var pdbFile = compiler.AddOutputFile("test.pdb");
            var keyFile = compiler.AddOutputFile("test.dll.key");
            var (result, output) = compiler.Run();
            Assert.True(string.IsNullOrEmpty(output));
            Assert.Equal(0, result);

            var json = Encoding.UTF8.GetString(keyFile.Contents.ToArray());
            var expected = @$"
{{
  ""compilation"": {{
    ""publicKey"": """",
    ""options"": {{
      ""outputKind"": ""DynamicallyLinkedLibrary"",
      ""moduleName"": ""test.dll"",
      ""scriptClassName"": ""Script"",
      ""mainTypeName"": null,
      ""cryptoPublicKey"": """",
      ""cryptoKeyFile"": null,
      ""delaySign"": null,
      ""publicSign"": false,
      ""checkOverflow"": true,
      ""platform"": ""AnyCpu"",
      ""optimizationLevel"": ""Debug"",
      ""generalDiagnosticOption"": ""Default"",
      ""warningLevel"": 1,
      ""deterministic"": true,
      ""debugPlusMode"": false,
      ""referencesSupersedeLowerVersions"": false,
      ""reportSuppressedDiagnostics"": false,
      ""nullableContextOptions"": ""Disable"",
      ""specificDiagnosticOptions"": [],
      ""localtime"": null,
      ""rootNamespace"": """",
      ""optionStrict"": ""Off"",
      ""optionInfer"": false,
      ""optionExplicit"": true,
      ""optionCompareText"": false,
      ""embedVbCoreRuntime"": false,
      ""globalImports"": [],
      ""parseOptions"": {{
        ""kind"": ""Regular"",
        ""specifiedKind"": ""Regular"",
        ""documentationMode"": ""None"",
        ""language"": ""Visual Basic"",
        ""features"": {{
          ""debug-determinism"": ""true""
        }},
        ""languageVersion"": ""VisualBasic17_13"",
        ""specifiedLanguageVersion"": ""Default"",
        ""preprocessorSymbols"": {{
          ""TARGET"": ""library"",
          ""VBC_VER"": ""17.13"",
          ""_MYTYPE"": ""Empty""
        }}
      }}
    }},
    ""syntaxTrees"": [
      {{
        ""fileName"": ""{Roslyn.Utilities.JsonWriter.EscapeString(sourceFile.FilePath)}"",
        ""text"": {{
          ""checksum"": ""8f9cdc9e727da9f8f0569be3dc606bfc6c1a1b13444e18c95eefb73810bbf1"",
          ""checksumAlgorithm"": ""Sha256"",
          ""encodingName"": ""Unicode (UTF-8)""
        }},
        ""parseOptions"": {{
          ""kind"": ""Regular"",
          ""specifiedKind"": ""Regular"",
          ""documentationMode"": ""None"",
          ""language"": ""Visual Basic"",
          ""features"": {{
            ""debug-determinism"": ""true""
          }},
          ""languageVersion"": ""VisualBasic17_13"",
          ""specifiedLanguageVersion"": ""Default"",
          ""preprocessorSymbols"": {{
            ""TARGET"": ""library"",
            ""VBC_VER"": ""17.13"",
            ""_MYTYPE"": ""Empty""
          }}
        }}
      }}
    ]
  }},
  ""additionalTexts"": [],
  ""analyzers"": [],
  ""generators"": [],
  ""emitOptions"": {{
    ""emitMetadataOnly"": false,
    ""tolerateErrors"": false,
    ""includePrivateMembers"": true,
    ""instrumentationKinds"": [],
    ""subsystemVersion"": {{
      ""major"": 0,
      ""minor"": 0
    }},
    ""fileAlignment"": 0,
    ""highEntropyVirtualAddressSpace"": false,
    ""baseAddress"": ""0"",
    ""debugInformationFormat"": ""PortablePdb"",
    ""outputNameOverride"": ""test.dll"",
    ""pdbFilePath"": ""{Roslyn.Utilities.JsonWriter.EscapeString(pdbFile.FilePath)}"",
    ""pdbChecksumAlgorithm"": ""SHA256"",
    ""runtimeMetadataVersion"": null,
    ""defaultSourceFileEncoding"": null,
    ""fallbackSourceFileEncoding"": null
  }}
}}
";
            AssertJson(expected, json, "toolsVersions", "references", "extensions");
        }
    }
}
