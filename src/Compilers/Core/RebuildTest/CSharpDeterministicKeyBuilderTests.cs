// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public sealed class CSharpDeterministicKeyBuilderTests : DeterministicKeyBuilderTests<CSharpCompilation, CSharpCompilationOptions, CSharpParseOptions>
    {
        public static CSharpCompilationOptions Options { get; } = new CSharpCompilationOptions(OutputKind.ConsoleApplication, deterministic: true);

        protected override SyntaxTree ParseSyntaxTree(string content, string fileName, SourceHashAlgorithm hashAlgorithm, CSharpParseOptions? parseOptions) =>
            CSharpTestBase.Parse(
                content,
                filename: fileName,
                checksumAlgorithm: hashAlgorithm,
                encoding: Encoding.UTF8,
                options: parseOptions);

        protected override CSharpCompilation CreateCompilation(SyntaxTree[] syntaxTrees, MetadataReference[]? references = null, CSharpCompilationOptions? options = null) =>
            CSharpCompilation.Create(
                "test",
                syntaxTrees,
                references ?? NetCoreApp.References.ToArray(),
                options ?? Options);

        private protected override DeterministicKeyBuilder GetDeterministicKeyBuilder() => CSharpDeterministicKeyBuilder.Instance;

        protected override CSharpCompilationOptions GetCompilationOptions() => Options;

        protected override CSharpParseOptions GetParseOptions() => CSharpParseOptions.Default;

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
            verifyCount<CSharpParseOptions>(13);
            verifyCount<CompilationOptions>(63);
            verifyCount<CSharpCompilationOptions>(9);

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

        [Fact]
        public void Simple()
        {
            var compilation = CSharpTestBase.CreateCompilation(
                CSharpTestSource.Parse(@"System.Console.WriteLine(""Hello World"");", checksumAlgorithm: SourceHashAlgorithm.Sha1),
                targetFramework: TargetFramework.NetCoreApp,
                options: Options);

            var key = compilation.GetDeterministicKey(options: DeterministicKeyOptions.IgnoreToolVersions);
            AssertJson(@"
{
  ""compilation"": {
    ""toolsVersions"": {},
    ""publicKey"": """",
    ""options"": {
      ""outputKind"": ""ConsoleApplication"",
      ""moduleName"": null,
      ""scriptClassName"": ""Script"",
      ""mainTypeName"": null,
      ""cryptoPublicKey"": """",
      ""cryptoKeyFile"": null,
      ""delaySign"": null,
      ""publicSign"": false,
      ""checkOverflow"": false,
      ""platform"": ""AnyCpu"",
      ""optimizationLevel"": ""Debug"",
      ""generalDiagnosticOption"": ""Default"",
      ""warningLevel"": 4,
      ""deterministic"": true,
      ""debugPlusMode"": false,
      ""referencesSupersedeLowerVersions"": false,
      ""reportSuppressedDiagnostics"": false,
      ""nullableContextOptions"": ""Disable"",
      ""specificDiagnosticOptions"": [],
      ""localtime"": null,
      ""unsafe"": false,
      ""topLevelBinderFlags"": ""None"",
      ""usings"": [],
      ""ruleSetPath"": null
    },
    ""syntaxTrees"": [
      {
        ""fileName"": """",
        ""text"": {
          ""checksum"": ""1b565cf6f2d814a4dc37ce578eda05fe0614f3d"",
          ""checksumAlgorithm"": ""Sha1"",
          ""encodingName"": ""Unicode (UTF-8)""
        },
        ""parseOptions"": {
          ""kind"": ""Regular"",
          ""specifiedKind"": ""Regular"",
          ""documentationMode"": ""Parse"",
          ""language"": ""C#"",
          ""features"": {},
          ""languageVersion"": ""Preview"",
          ""specifiedLanguageVersion"": ""Preview"",
          ""preprocessorSymbols"": []
        }
      }
    ]
  },
  ""additionalTexts"": [],
  ""analyzers"": [],
  ""generators"": [],
  ""emitOptions"": null,
  ""resources"": []
}
", key);
        }

        [Theory]
        [CombinatorialData]
        public void SyntaxTreeFilePath(bool ignoreFilePaths)
        {
            var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? @"c:\code\file.cs"
                : @"/code/file.cs";
            var (expectedPath, options) = ignoreFilePaths
                ? ("file.cs", DeterministicKeyOptions.IgnorePaths)
                : (path, DeterministicKeyOptions.Default);

            var source = CSharpTestBase.Parse(
                @"System.Console.WriteLine(""Hello World"");",
                filename: path,
                checksumAlgorithm: SourceHashAlgorithm.Sha1);
            var compilation = CSharpTestBase.CreateCompilation(source);
            var key = compilation.GetDeterministicKey(options: options);
            var expected = @$"
""syntaxTrees"": [
  {{
    ""fileName"": ""{Roslyn.Utilities.JsonWriter.EscapeString(expectedPath)}"",
    ""text"": {{
      ""checksum"": ""1b565cf6f2d814a4dc37ce578eda05fe0614f3d"",
      ""checksumAlgorithm"": ""Sha1"",
      ""encodingName"": ""Unicode (UTF-8)""
    }},
    ""parseOptions"": {{
      ""kind"": ""Regular"",
      ""specifiedKind"": ""Regular"",
      ""documentationMode"": ""Parse"",
      ""language"": ""C#"",
      ""features"": {{}},
      ""languageVersion"": ""Preview"",
      ""specifiedLanguageVersion"": ""Preview"",
      ""preprocessorSymbols"": []
    }}
  }}
]";
            AssertJsonSection(expected, key, "compilation.syntaxTrees");
        }

        [Theory]
        [InlineData(@"hello world")]
        [InlineData(@"just need some text here")]
        [InlineData(@"yet another case")]
        public void ContentInAdditionalText(string content)
        {
            var syntaxTree = CSharpTestBase.Parse(
                "",
                filename: "file.cs",
                checksumAlgorithm: HashAlgorithm);
            var additionalText = new TestAdditionalText(content, Encoding.UTF8, path: "file.txt", HashAlgorithm);
            var contentChecksum = GetChecksum(additionalText.GetText()!);

            var compilation = CSharpTestBase.CreateCompilation(syntaxTree);
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

        /// <summary>
        /// Generally tests omit the tools versions in the Json output for simplicity but need at least 
        /// one test that verifies we're actually encoding them.
        /// </summary>
        [Fact]
        public void ToolsVersion()
        {
            var compilation = CSharpTestBase.CreateCompilation(
                @"System.Console.WriteLine(""Hello World"");",
                targetFramework: TargetFramework.NetCoreApp,
                options: Options);

            var key = compilation.GetDeterministicKey(options: DeterministicKeyOptions.Default);

            var compilerVersion = typeof(Compilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var runtimeVersion = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            AssertJson($@"
{{
  ""compilation"": {{
    ""toolsVersions"": {{
      ""compilerVersion"": ""{compilerVersion}"",
      ""runtimeVersion"": ""{runtimeVersion}"",
      ""frameworkDescription"": ""{RuntimeInformation.FrameworkDescription}"",
      ""osDescription"": ""{RuntimeInformation.OSDescription}""
    }},
    ""publicKey"": """",
    ""options"": {{
      ""outputKind"": ""ConsoleApplication"",
      ""moduleName"": null,
      ""scriptClassName"": ""Script"",
      ""mainTypeName"": null,
      ""cryptoPublicKey"": """",
      ""cryptoKeyFile"": null,
      ""delaySign"": null,
      ""publicSign"": false,
      ""checkOverflow"": false,
      ""platform"": ""AnyCpu"",
      ""optimizationLevel"": ""Debug"",
      ""generalDiagnosticOption"": ""Default"",
      ""warningLevel"": 4,
      ""deterministic"": true,
      ""debugPlusMode"": false,
      ""referencesSupersedeLowerVersions"": false,
      ""reportSuppressedDiagnostics"": false,
      ""nullableContextOptions"": ""Disable"",
      ""specificDiagnosticOptions"": [],
      ""localtime"": null,
      ""unsafe"": false,
      ""topLevelBinderFlags"": ""None"",
      ""usings"": [],
      ""ruleSetPath"": null
    }}
  }},
  ""additionalTexts"": [],
  ""analyzers"": [],
  ""generators"": [],
  ""emitOptions"": null,
  ""resources"": []
}}
", key, "references", "syntaxTrees", "extensions");
        }

        [Theory]
        [CombinatorialData]
        public void CSharpCompilationOptionsCombination(bool @unsafe, NullableContextOptions nullableContextOptions)
        {
            foreach (BinderFlags binderFlags in Enum.GetValues(typeof(BinderFlags)))
            {
                var options = Options
                    .WithAllowUnsafe(@unsafe)
                    .WithTopLevelBinderFlags(binderFlags)
                    .WithNullableContextOptions(nullableContextOptions);

                var value = GetCompilationOptionsValue(options);
                Assert.Equal(@unsafe, value.Value<bool>("unsafe"));
                Assert.Equal(binderFlags.ToString(), value.Value<string>("topLevelBinderFlags"));
                Assert.Equal(nullableContextOptions.ToString(), value.Value<string>("nullableContextOptions"));
            }
        }

        [Fact]
        public void CSharpCompilationOptionsGlobalUsings()
        {
            assert(@"
[
  ""System"",
  ""System.Xml""
]
", "System", "System.Xml");

            assert(@"
[
  ""System.Xml"",
  ""System""
]
", "System.Xml", "System");

            assert(@"
[
  ""System.Xml""
]
", "System.Xml");

            void assert(string expected, params string[] usings)
            {
                var options = Options.WithUsings(usings);
                var value = GetCompilationOptionsValue(options);
                var actual = value["usings"]?.ToString(Formatting.Indented);
                AssertJsonCore(expected, actual);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CSharpParseOptionsLanguageVersion(LanguageVersion languageVersion)
        {
            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
            var obj = GetParseOptionsValue(parseOptions);
            var effective = languageVersion.MapSpecifiedToEffectiveVersion();

            Assert.Equal(effective.ToString(), obj.Value<string>("languageVersion"));
            Assert.Equal(languageVersion.ToString(), obj.Value<string>("specifiedLanguageVersion"));
        }

        [Fact]
        public void CSharpParseOptionsPreprocessorSymbols()
        {
            assert(@"[]");

            assert(@"
[
  ""DEBUG""
]", "DEBUG");

            assert(@"
[
  ""DEBUG"",
  ""TRACE""
]", "DEBUG", "TRACE");

            assert(@"
[
  ""DEBUG"",
  ""TRACE""
]", "TRACE", "DEBUG");

            void assert(string expected, params string[] values)
            {
                var parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(values);
                var obj = GetParseOptionsValue(parseOptions);
                AssertJsonCore(expected, obj.Value<JArray>("preprocessorSymbols")?.ToString(Formatting.Indented));
            }
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [InlineData(@"c:\src\code.cs", @"c:\src", null)]
        [InlineData(@"d:\src\code.cs", @"d:\src\", @"/pathmap:d:\=c:\")]
        [InlineData(@"e:\long\path\src\code.cs", @"e:\long\path\src\", @"/pathmap:e:\long\path\=c:\")]
        public void CSharpPathMapWindows(string filePath, string workingDirectory, string? pathMap)
        {
            var args = new List<string>(new[] { filePath, "/nostdlib", "/langversion:9", "/checksumalgorithm:sha256" });
            if (pathMap is not null)
            {
                args.Add(pathMap);
            }

            var compiler = new MockCSharpCompiler(
                null,
                workingDirectory: workingDirectory,
                args.ToArray());
            compiler.FileSystem = TestableFileSystem.CreateForFiles((filePath, new TestableFile("hello")));
            AssertSyntaxTreePathMap(@"
[
  {
    ""fileName"": ""c:\\src\\code.cs"",
    ""text"": {
      ""checksum"": ""2cf24dba5fb0a3e26e83b2ac5b9e29e1b161e5c1fa7425e7343362938b9824"",
      ""checksumAlgorithm"": ""Sha256"",
      ""encodingName"": ""Unicode (UTF-8)""
    },
    ""parseOptions"": {
      ""kind"": ""Regular"",
      ""specifiedKind"": ""Regular"",
      ""documentationMode"": ""None"",
      ""language"": ""C#"",
      ""features"": {},
      ""languageVersion"": ""CSharp9"",
      ""specifiedLanguageVersion"": ""CSharp9"",
      ""preprocessorSymbols"": []
    }
  }
]
", compiler);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void CSharpPublicKey()
        {
            using var temp = new TempRoot();
            var keyFilePath = @"c:\windows\key.snk";
            var publicKey = TestResources.General.snPublicKey;
            var publicKeyStr = DeterministicKeyBuilder.EncodeByteArrayValue(publicKey);
            var fileSystem = new TestStrongNameFileSystem(temp.CreateDirectory().Path);
            fileSystem.ReadAllBytesFunc = _ => publicKey;
            var options = Options
                .WithCryptoKeyFile(keyFilePath)
                .WithStrongNameProvider(new DesktopStrongNameProvider(default, fileSystem));
            var compilation = CreateCompilation(new SyntaxTree[] { }, options: options);
            var obj = GetCompilationValue(compilation);
            Assert.Equal(publicKeyStr, obj.Value<string>("publicKey"));
        }

        [Fact]
        public void MetadataReferenceCompilation()
        {
            var utilCompilation = CSharpCompilation.Create(
                assemblyName: "util",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(@"// this is a comment", CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.CSharp10)) },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true));
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
      ""checkOverflow"": false,
      ""platform"": ""AnyCpu"",
      ""optimizationLevel"": ""Debug"",
      ""generalDiagnosticOption"": ""Default"",
      ""warningLevel"": 4,
      ""deterministic"": true,
      ""debugPlusMode"": false,
      ""referencesSupersedeLowerVersions"": false,
      ""reportSuppressedDiagnostics"": false,
      ""nullableContextOptions"": ""Disable"",
      ""specificDiagnosticOptions"": [],
      ""localtime"": null,
      ""unsafe"": false,
      ""topLevelBinderFlags"": ""None"",
      ""usings"": [],
      ""ruleSetPath"": null
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
          ""language"": ""C#"",
          ""features"": {},
          ""languageVersion"": ""CSharp10"",
          ""specifiedLanguageVersion"": ""CSharp10"",
          ""preprocessorSymbols"": []
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
            var compiler = TestableCompiler.CreateCSharpNetCoreApp("test.cs", @"-t:library", "-nologo", "-features:debug-determinism", "-deterministic", "-debug:portable", "-checksumalgorithm:sha256");
            var sourceFile = compiler.AddSourceFile("test.cs", @"// this is a test file");
            compiler.AddOutputFile("test.dll");
            var pdbFile = compiler.AddOutputFile("test.pdb");
            var keyFile = compiler.AddOutputFile("test.dll.key");
            var (result, output) = compiler.Run();
            Assert.True(string.IsNullOrEmpty(output));
            Assert.Equal(0, result);

            var json = Encoding.UTF8.GetString(keyFile.Contents.ToArray());
            var expected = $$"""

{
  "compilation": {
    "publicKey": "",
    "options": {
      "outputKind": "DynamicallyLinkedLibrary",
      "moduleName": "test.dll",
      "scriptClassName": "Script",
      "mainTypeName": null,
      "cryptoPublicKey": "",
      "cryptoKeyFile": null,
      "delaySign": null,
      "publicSign": false,
      "checkOverflow": false,
      "platform": "AnyCpu",
      "optimizationLevel": "Debug",
      "generalDiagnosticOption": "Default",
      "warningLevel": 4,
      "deterministic": true,
      "debugPlusMode": false,
      "referencesSupersedeLowerVersions": false,
      "reportSuppressedDiagnostics": false,
      "nullableContextOptions": "Disable",
      "specificDiagnosticOptions": [],
      "localtime": null,
      "unsafe": false,
      "topLevelBinderFlags": "None",
      "usings": [],
      "ruleSetPath": null
    },
    "syntaxTrees": [
      {
        "fileName": "{{Roslyn.Utilities.JsonWriter.EscapeString(sourceFile.FilePath)}}",
        "text": {
          "checksum": "2326e849c5bb80ded5ef51743244896b812672aa03119ee8788cdc3b356f88",
          "checksumAlgorithm": "Sha256",
          "encodingName": "Unicode (UTF-8)"
        },
        "parseOptions": {
          "kind": "Regular",
          "specifiedKind": "Regular",
          "documentationMode": "None",
          "language": "C#",
          "features": {
            "debug-determinism": "true"
          },
          "languageVersion": "{{LanguageVersionFacts.CurrentVersion}}",
          "specifiedLanguageVersion": "Default",
          "preprocessorSymbols": []
        }
      }
    ]
  },
  "additionalTexts": [],
  "analyzers": [],
  "generators": [],
  "emitOptions": {
    "emitMetadataOnly": false,
    "tolerateErrors": false,
    "includePrivateMembers": true,
    "instrumentationKinds": [],
    "subsystemVersion": {
      "major": 0,
      "minor": 0
    },
    "fileAlignment": 0,
    "highEntropyVirtualAddressSpace": false,
    "baseAddress": "0",
    "debugInformationFormat": "PortablePdb",
    "outputNameOverride": "test.dll",
    "pdbFilePath": "{{Roslyn.Utilities.JsonWriter.EscapeString(pdbFile.FilePath)}}",
    "pdbChecksumAlgorithm": "SHA256",
    "runtimeMetadataVersion": null,
    "defaultSourceFileEncoding": null,
    "fallbackSourceFileEncoding": null,
    "sourceLink": null
  },
  "resources": []
}
""";
            AssertJson(expected, json, "toolsVersions", "references", "extensions");
        }

        [Fact]
        public void SourceLink()
        {
            var syntaxTree = CSharpTestBase.Parse(
                "",
                filename: "file.cs",
                checksumAlgorithm: HashAlgorithm);
            var compilation = CSharpTestBase.CreateCompilation(syntaxTree, options: Options);

            var sourceLinkContent = """
                {
                  "documents": {
                    "/*": "https://raw.githubusercontent.com/test/repo/*"
                  }
                }
                """;
            var sourceLinkText = SourceText.From(sourceLinkContent, Encoding.UTF8, SourceHashAlgorithm.Sha256);
            var sourceLinkChecksum = GetChecksum(sourceLinkText);

            var emitOptions = EmitOptions;
            var key = compilation.GetDeterministicKey(
                emitOptions: emitOptions,
                sourceLinkText: sourceLinkText,
                options: DeterministicKeyOptions.IgnoreToolVersions);

            var expected = $@"
""sourceLink"": {{
  ""checksum"": ""{sourceLinkChecksum}"",
  ""checksumAlgorithm"": ""Sha256"",
  ""encodingName"": ""Unicode (UTF-8)""
}}";
            AssertJsonSection(expected, key, "emitOptions.sourceLink");
        }

        [Fact]
        public void RuleSet()
        {
            var syntaxTree = CSharpTestBase.Parse(
                "",
                filename: "file.cs",
                checksumAlgorithm: HashAlgorithm);
            var compilation = CSharpTestBase.CreateCompilation(syntaxTree, options: Options);

            var ruleSetPath = "/path/to/ruleset.ruleset";
            var key = compilation.GetDeterministicKey(
                ruleSetFilePath: ruleSetPath,
                options: DeterministicKeyOptions.IgnoreToolVersions);

            var expected = @$"
""ruleSetPath"": ""{ruleSetPath}""";
            AssertJsonSection(expected, key, "compilation.options.ruleSetPath");
        }

        [Fact]
        public void Resources()
        {
            var syntaxTree = CSharpTestBase.Parse(
                "",
                filename: "file.cs",
                checksumAlgorithm: HashAlgorithm);
            var compilation = CSharpTestBase.CreateCompilation(syntaxTree, options: Options);

            var resourceContent = "Test resource content";
            var resourceBytes = Encoding.UTF8.GetBytes(resourceContent);
            string resourceChecksum;
            using (var hashAlgorithm = System.Security.Cryptography.SHA256.Create())
            {
                var hash = hashAlgorithm.ComputeHash(resourceBytes);
                resourceChecksum = DeterministicKeyBuilder.EncodeByteArrayValue(hash);
            }

            var resource = new ResourceDescription(
                "TestResource",
                () => new System.IO.MemoryStream(resourceBytes),
                isPublic: true);

            var key = compilation.GetDeterministicKey(
                resources: ImmutableArray.Create(resource),
                options: DeterministicKeyOptions.IgnoreToolVersions);

            var expected = $@"
""resources"": [
  {{
    ""resourceName"": ""TestResource"",
    ""fileName"": null,
    ""isPublic"": true,
    ""isEmbedded"": true,
    ""content"": {{
      ""checksum"": ""{resourceChecksum}""
    }}
  }}
]";
            AssertJsonSection(expected, key, "resources");
        }
    }
}
