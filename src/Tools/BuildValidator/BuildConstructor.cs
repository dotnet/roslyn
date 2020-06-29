// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;

using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace BuildValidator
{
    internal class BuildConstructor
    {
        private readonly IMetadataReferenceResolver _referenceResolver;
        private readonly ISourceResolver _sourceResolver;

        public BuildConstructor(ILoggerFactory _, IMetadataReferenceResolver referenceResolver, ISourceResolver sourceResolver)
        {
            _referenceResolver = referenceResolver;
            _sourceResolver = sourceResolver;
        }

        public async Task<Compilation> CreateCompilationAsync(MetadataReaderProvider metadataReaderProvider, string name)
        {
            var pdbReader = new CompilationOptionsReader(metadataReaderProvider);
            var pdbCompilationOptions = pdbReader.GetCompilationOptions();

            if (pdbCompilationOptions.TryGetValue("language", out var language))
            {
                var compilation = language switch
                {
                    LanguageNames.CSharp => await CreateCSharpCompilationAsync(pdbReader, name).ConfigureAwait(false),
                    LanguageNames.VisualBasic => await CreateVisualBasicCompilationAsync(pdbReader, name).ConfigureAwait(false),
                    _ => throw new InvalidOperationException($"{language} is not a known language")
                };

                return compilation;
            }

            throw new Exception("Did not find compilation options in pdb");
        }

        private async Task<ImmutableArray<MetadataReference>> CreateMetadataReferencesAsync(CompilationOptionsReader pdbReader)
        {
            var referenceInfos = pdbReader.GetMetadataReferences();
            return await _referenceResolver.ResolveReferencesAsync(referenceInfos).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<SourceText>> GetSourcesAsync(CompilationOptionsReader pdbReader)
        {
            var builder = ImmutableArray.CreateBuilder<SourceText>();

            foreach (var srcFile in pdbReader.GetSourceFileNames())
            {
                var text = await _sourceResolver.ResolveSourceAsync(srcFile).ConfigureAwait(false);
                builder.Add(text);
            }

            return builder.ToImmutable();
        }

        #region CSharp
        private async Task<Compilation> CreateCSharpCompilationAsync(CompilationOptionsReader pdbReader, string name)
        {
            var (compilationOptions, parseOptions) = CreateCSharpCompilationOptions(pdbReader);
            var metadataReferences = await CreateMetadataReferencesAsync(pdbReader).ConfigureAwait(false);
            var sources = await GetSourcesAsync(pdbReader).ConfigureAwait(false);

            return CSharpCompilation.Create(
                name,
                syntaxTrees: sources.Select(s => CSharpSyntaxTree.ParseText(s, options: parseOptions)).ToImmutableArray(),
                references: metadataReferences,
                options: compilationOptions);
        }

        private static (CSharpCompilationOptions, CSharpParseOptions) CreateCSharpCompilationOptions(CompilationOptionsReader pdbReader)
        {
            var pdbCompilationOptions = pdbReader.GetCompilationOptions();

            var langVersionString = pdbCompilationOptions["language-version"];
            var optimization = pdbCompilationOptions["optimization"];
            pdbCompilationOptions.TryGetValue("portability-policy", out var portabilityPolicyString);
            pdbCompilationOptions.TryGetValue("default-encoding", out var defaultEncoding);
            pdbCompilationOptions.TryGetValue("fallback-encoding", out var fallbackEncoding);
            pdbCompilationOptions.TryGetValue("define", out var define);
            pdbCompilationOptions.TryGetValue("checked", out var checkedString);
            pdbCompilationOptions.TryGetValue("nullable", out var nullable);
            pdbCompilationOptions.TryGetValue("unsafe", out var unsafeString);

            CS.LanguageVersionFacts.TryParse(langVersionString, out var langVersion);

            var preprocessorSymbols = define == null
                ? ImmutableArray<string>.Empty
                : define.Split(';').ToImmutableArray();

            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(langVersion)
                .WithPreprocessorSymbols(preprocessorSymbols);

            int.TryParse(portabilityPolicyString, out var portabilityPolicy);

            var (optimizationLevel, _) = GetOptimizationLevel(optimization);

            var nullableOptions = nullable is null
                ? NullableContextOptions.Disable
                : (NullableContextOptions)Enum.Parse(typeof(NullableContextOptions), nullable);

            var compilationOptions = new CSharpCompilationOptions(
                pdbReader.GetOutputKind(),
                reportSuppressedDiagnostics: false,
                moduleName: null,
                mainTypeName: null,
                scriptClassName: null,
                usings: null,
                optimizationLevel,
                !string.IsNullOrEmpty(checkedString) && bool.Parse(checkedString),
                !string.IsNullOrEmpty(unsafeString) && bool.Parse(unsafeString),
                cryptoKeyContainer: null,
                cryptoKeyFile: null,
                cryptoPublicKey: default,
                delaySign: null,
                Platform.AnyCpu,
                ReportDiagnostic.Suppress,
                warningLevel: 4,
                specificDiagnosticOptions: null,
                concurrentBuild: true,
                deterministic: true,
                xmlReferenceResolver: null,
                sourceReferenceResolver: null,
                metadataReferenceResolver: null,
                assemblyIdentityComparer: null,
                strongNameProvider: null,
                publicSign: false,
                metadataImportOptions: MetadataImportOptions.Public,
                nullableContextOptions: nullableOptions);

            return (compilationOptions, parseOptions);
        }

        private static (OptimizationLevel, bool) GetOptimizationLevel(string optimizationLevel)
            => optimizationLevel switch
            {
                "debug" => (OptimizationLevel.Debug, false),
                "debug-plus" => (OptimizationLevel.Debug, true),
                "release" => (OptimizationLevel.Release, false),
                _ => throw new InvalidOperationException()
            };

        #endregion

        #region Visual Basic
        private async Task<Compilation> CreateVisualBasicCompilationAsync(CompilationOptionsReader pdbReader, string name)
        {
            var compilationOptions = CreateVisualBasicCompilationOptions(pdbReader);
            var metadataReferences = await CreateMetadataReferencesAsync(pdbReader).ConfigureAwait(false);
            var sources = await GetSourcesAsync(pdbReader).ConfigureAwait(false);

            return VisualBasicCompilation.Create(
                name,
                syntaxTrees: sources.Select(s => VisualBasicSyntaxTree.ParseText(s, options: compilationOptions.ParseOptions)).ToImmutableArray(),
                references: metadataReferences,
                options: compilationOptions);
        }

        private static VisualBasicCompilationOptions CreateVisualBasicCompilationOptions(CompilationOptionsReader pdbReader)
        {
            var pdbCompilationOptions = pdbReader.GetCompilationOptions();

            var langVersionString = pdbCompilationOptions["language-version"];
            var optimization = pdbCompilationOptions["optimization"];
            pdbCompilationOptions.TryGetValue("define", out var define);
            pdbCompilationOptions.TryGetValue("strict", out var strict);
            pdbCompilationOptions.TryGetValue("checked", out var checkedString);

            VB.LanguageVersion langVersion = default;
            VB.LanguageVersionFacts.TryParse(langVersionString, ref langVersion);

            var preprocessorSymbols = string.IsNullOrEmpty(define)
                ? new KeyValuePair<string, object>[0]
                : define.Split(';')
                    .Select(s => s.Split('='))
                    .Select(a => new KeyValuePair<string, object>(a[0], a[1]))
                    .ToArray();

            var parseOptions = VisualBasicParseOptions.Default.WithLanguageVersion(langVersion)
                .WithPreprocessorSymbols(preprocessorSymbols);

            var (optimizationLevel, _) = GetOptimizationLevel(optimization);

            bool.TryParse(checkedString, out var isChecked);
            bool.TryParse(strict, out var isStrict);

            return new VisualBasicCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                moduleName: null,
                mainTypeName: null,
                scriptClassName: "Script",
                globalImports: null,
                rootNamespace: null,
                optionStrict: isStrict ? OptionStrict.On : OptionStrict.Off,
                optionInfer: true,
                optionExplicit: true,
                optionCompareText: false,
                parseOptions: parseOptions,
                embedVbCoreRuntime: false,
                optimizationLevel: optimizationLevel,
                checkOverflow: isChecked,
                cryptoKeyContainer: null,
                cryptoKeyFile: null,
                cryptoPublicKey: default,
                delaySign: null,
                platform: Platform.AnyCpu,
                generalDiagnosticOption: ReportDiagnostic.Default,
                specificDiagnosticOptions: null,
                concurrentBuild: true,
                deterministic: true,
                xmlReferenceResolver: null,
                sourceReferenceResolver: null,
                metadataReferenceResolver: null,
                assemblyIdentityComparer: null,
                strongNameProvider: null,
                publicSign: false,
                reportSuppressedDiagnostics: false,
                metadataImportOptions: MetadataImportOptions.Public);
        }
        #endregion
    }
}
