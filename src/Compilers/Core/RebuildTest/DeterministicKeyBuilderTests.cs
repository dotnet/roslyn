// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;
using Newtonsoft;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public sealed class DeterministicKeyBuilderTests
    {
        private static readonly char[] s_trimChars = { ' ', '\n', '\r' };

        private void AssertJson(
            string expected,
            string actual,
            bool ignoreReferences = true)
        {
            var json = JObject.Parse(actual);
            if (ignoreReferences)
            {
                json
                    .Descendants()
                    .OfType<JProperty>()
                    .Where(x => x.Name == "references")
                    .ToList()
                    .ForEach(x => x.Remove());
            }

            actual = json.ToString(Formatting.Indented);
            expected = JObject.Parse(expected).ToString(Formatting.Indented);
            AssertJsonCore(expected, actual);
        }

        private void AssertJsonSection(
            string expected,
            string actual,
            string sectionName)
        {
            AssertJsonCore(getSection(expected), getSection(actual));

            string getSection(string json) =>
                JObject.Parse(json)
                    .Descendants()
                    .OfType<JProperty>()
                    .Where(x => x.Name == sectionName)
                    .Single()
                    .ToString(Formatting.Indented);
        }

        private void AssertJsonCore(string expected, string actual)
        {
            expected = expected.Trim(s_trimChars);
            actual = actual.Trim(s_trimChars);
            Assert.Equal(expected, actual);
        }

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
            verifyCount<VisualBasicParseOptions>(10);
            verifyCount<CompilationOptions>(62);
            verifyCount<CSharpCompilationOptions>(9);
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

        [Fact]
        public void Simple()
        {
            var compilation = CSharpTestBase.CreateCompilation(
                @"System.Console.WriteLine(""Hello World"");",
                targetFramework: TargetFramework.NetCoreApp);

            var builder = new CSharpDeterministicKeyBuilder();
            var key = builder.GetKey(compilation, options: DeterministicKeyOptions.IgnoreToolVersions);
            AssertJson(@"
{
  ""compilation"": {
    ""options"": {
      ""outputKind"": ""ConsoleApplication"",
      ""moduleName"": null,
      ""scriptClassName"": ""Script"",
      ""mainTypeName"": null,
      ""cryptoKeyFile"": null,
      ""delaySign"": null,
      ""publicSign"": false,
      ""checkOverflow"": false,
      ""platform"": ""AnyCpu"",
      ""optimizationLevel"": ""Release"",
      ""generalDiagnosticOption"": ""Default"",
      ""warningLevel"": 9999,
      ""deterministic"": false,
      ""debugPlusMode"": false,
      ""referencesSupersedeLowerVersions"": false,
      ""reportSuppressedDiagnostics"": false,
      ""nullableContextOptions"": ""Disable"",
      ""specificDiagnosticOptions"": [],
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
", key, ignoreReferences: true);
        }

        [Fact]
        public void EmitOptionsDefault()
        {
            var builder = new CSharpDeterministicKeyBuilder();
            var key = builder.GetKey(EmitOptions.Default);
            AssertJson(@"
{
  ""emitMetadataOnly"": false,
  ""tolerateErrors"": false,
  ""includePrivateMembers"": true,
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
        [InlineData(@"c:\code\file.cs", @"file.cs", DeterministicKeyOptions.IgnorePaths)]
        [InlineData(@"c:\code\file.cs", @"c:\code\file.cs", DeterministicKeyOptions.Default)]
        [InlineData(@"/code/file.cs", @"file.cs", DeterministicKeyOptions.IgnorePaths)]
        [InlineData(@"/code/file.cs", @"/code/file.cs", DeterministicKeyOptions.Default)]
        public void FilePathInSyntaxTree(string path, string expectedPath, DeterministicKeyOptions options)
        {
            var source = CSharpTestBase.Parse(
                @"System.Console.WriteLine(""Hello World"");",
                filename: path);
            var compilation = CSharpTestBase.CreateCompilation(source);
            var builder = new CSharpDeterministicKeyBuilder();
            var key = builder.GetKey(compilation, options: options);
            var expected = @$"{{
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
]
}}";
            AssertJsonSection(expected, key, "syntaxTrees");
        }
    }
}
