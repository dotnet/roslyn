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
using Newtonsoft;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.VisualBasic.UnitTests;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public sealed class CSharpDeterministicKeyBuilderTests : DeterministicKeyBuilderTests
    {
        public static CSharpCompilationOptions Options { get; } = new CSharpCompilationOptions(OutputKind.ConsoleApplication, deterministic: true);
        protected override SyntaxTree ParseSyntaxTree(string content, string fileName, SourceHashAlgorithm hashAlgorithm) =>
            CSharpTestBase.Parse(
                content,
                filename: fileName,
                checksumAlgorithm: hashAlgorithm,
                encoding: Encoding.UTF8);

        protected override Compilation CreateCompilation(SyntaxTree[] syntaxTrees, MetadataReference[]? references = null) =>
            CSharpCompilation.Create(
                "test",
                syntaxTrees,
                references ?? NetCoreApp.References.ToArray(),
                Options);

        private protected override DeterministicKeyBuilder GetDeterministicKeyBuilder() => new CSharpDeterministicKeyBuilder();

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
            verifyCount<CSharpParseOptions>(10);
            verifyCount<CompilationOptions>(62);
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
                @"System.Console.WriteLine(""Hello World"");",
                targetFramework: TargetFramework.NetCoreApp,
                options: Options);

            var key = compilation.GetDeterministicKey(options: DeterministicKeyOptions.IgnoreToolVersions);
            AssertJson(@"
{
  ""compilation"": {
    ""toolsVersions"": {},
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
      ""topLevelBinderFlags"": ""None""
    },
    ""syntaxTrees"": [
      {
        ""fileName"": """",
        ""text"": {
          ""checksum"": ""1b565cf6f2d814a4dc37ce578eda05fe0614f3d"",
          ""checksumAlgorithm"": ""Sha1"",
          ""encoding"": ""Unicode (UTF-8)""
        },
        ""parseOptions"": {
          ""languageVersion"": ""Preview"",
          ""specifiedLanguageVersion"": ""Preview""
        }
      }
    ]
  },
  ""additionalTexts"": [],
  ""analyzers"": [],
  ""generators"": [],
  ""emitOptions"": {}
}
", key);
        }

        [Theory]
        [InlineData(@"c:\code\file.cs", @"file.cs", DeterministicKeyOptions.IgnorePaths)]
        [InlineData(@"c:\code\file.cs", @"c:\code\file.cs", DeterministicKeyOptions.Default)]
        [InlineData(@"/code/file.cs", @"file.cs", DeterministicKeyOptions.IgnorePaths)]
        [InlineData(@"/code/file.cs", @"/code/file.cs", DeterministicKeyOptions.Default)]
        public void SyntaxTreeFilePath(string path, string expectedPath, DeterministicKeyOptions options)
        {
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
      ""encoding"": ""Unicode (UTF-8)""
    }},
    ""parseOptions"": {{
      ""languageVersion"": ""Preview"",
      ""specifiedLanguageVersion"": ""Preview""
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
            var contentChecksum = GetChecksum(additionalText.GetText());

            var compilation = CSharpTestBase.CreateCompilation(syntaxTree);
            var key = compilation.GetDeterministicKey(additionalTexts: ImmutableArray.Create<AdditionalText>(additionalText));
            var expected = @$"
""additionalTexts"": [
  {{
    ""fileName"": ""file.txt"",
    ""text"": {{
      ""checksum"": ""{contentChecksum}"",
      ""checksumAlgorithm"": ""Sha256"",
      ""encoding"": ""Unicode (UTF-8)""
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
      ""framework"": ""{RuntimeInformation.FrameworkDescription}"",
      ""os"": ""{RuntimeInformation.OSDescription}""
    }},
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
      ""topLevelBinderFlags"": ""None""
    }}
  }},
  ""additionalTexts"": [],
  ""analyzers"": [],
  ""generators"": [],
  ""emitOptions"": {{}}
}}
", key, "references", "syntaxTrees", "extensions");
        }
    }
}
