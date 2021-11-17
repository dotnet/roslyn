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

        protected override VisualBasicCompilation CreateCompilation(SyntaxTree[] syntaxTrees, MetadataReference[]? references = null, VisualBasicCompilationOptions? options = null) =>
            VisualBasicCompilation.Create(
                "test",
                syntaxTrees,
                references ?? NetCoreApp.References.ToArray(),
                options: options ?? BasicOptions);

        protected override VisualBasicCompilationOptions GetCompilationOptions() => BasicOptions;

        protected override VisualBasicParseOptions GetParseOptions() => VisualBasicParseOptions.Default;

        private protected override DeterministicKeyBuilder GetDeterministicKeyBuilder() => new VisualBasicDeterministicKeyBuilder();

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
            verifyCount<CompilationOptions>(62);
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
            var contentChecksum = GetChecksum(additionalText.GetText());

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
      ""encoding"": ""Unicode (UTF-8)""
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
    ""isXml"": true
  },
  {
    ""name"": ""System.Xml"",
    ""isXml"": false
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
            assert(null);

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

            void assert(string? expected, params (string Key, object? Value)[] values)
            {
                var parseOptions = VisualBasicParseOptions.Default.WithPreprocessorSymbols(values.Select(x => new KeyValuePair<string, object>(x.Key, x.Value!)));
                var obj = GetParseOptionsValue(parseOptions);
                AssertJsonCore(expected, obj.Value<JObject>("preprocessorSymbols")?.ToString(Formatting.Indented));
            }
        }
    }
}
