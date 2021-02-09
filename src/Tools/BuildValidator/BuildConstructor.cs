// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
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
    /// <summary>
    /// An abstraction for building from a MetadataReaderProvider
    /// </summary>
    internal class BuildConstructor
    {
        private readonly LocalReferenceResolver _referenceResolver;
        private readonly LocalSourceResolver _sourceResolver;
        private readonly ILogger _logger;

        public BuildConstructor(LocalReferenceResolver referenceResolver, LocalSourceResolver sourceResolver, ILogger logger)
        {
            _referenceResolver = referenceResolver;
            _sourceResolver = sourceResolver;
            _logger = logger;
        }

        public Task<Compilation> CreateCompilationAsync(CompilationOptionsReader optionsReader, string name)
        {
            var pdbCompilationOptions = optionsReader.GetMetadataCompilationOptions();

            if (pdbCompilationOptions.Length == 0)
            {
                throw new InvalidDataException("Did not find compilation options in pdb");
            }

            if (pdbCompilationOptions.TryGetUniqueOption("language", out var language))
            {
                var compilation = language switch
                {
                    LanguageNames.CSharp => CreateCSharpCompilationAsync(optionsReader, name),
                    LanguageNames.VisualBasic => CreateVisualBasicCompilationAsync(optionsReader, name),
                    _ => Task.FromException<Compilation>(new InvalidDataException($"{language} is not a known language"))
                };

                return compilation;
            }

            return Task.FromException<Compilation>(new InvalidDataException("Did not find language in compilation options"));
        }

        private ImmutableArray<MetadataReferenceInfo> GetMetadataReferenceInfos(CompilationOptionsReader compilationOptionsReader)
        {
            return compilationOptionsReader.GetMetadataReferences();
        }

        private ImmutableArray<SourceFileInfo> GetSourceFileInfos(CompilationOptionsReader compilationOptionsReader, Encoding encoding)
        {
            return compilationOptionsReader.GetSourceFileInfos(encoding);
        }

        private ImmutableArray<MetadataReference> ResolveMetadataReferences(ImmutableArray<MetadataReferenceInfo> referenceInfos)
        {
            _logger.LogInformation("Locating metadata references");
            return _referenceResolver.ResolveReferences(referenceInfos);
        }

        private ImmutableArray<SourceLink> ResolveSourceLinks(CompilationOptionsReader compilationOptionsReader)
        {
            using var _ = _logger.BeginScope("Source Links");
            var sourceLinks = compilationOptionsReader.GetSourceLinksOpt();
            if (sourceLinks.IsDefault)
            {
                _logger.LogInformation("No source links found in pdb");
            }
            else
            {
                foreach (var link in sourceLinks)
                {
                    _logger.LogInformation($@"""{link.Prefix}"": ""{link.Replace}""");
                }
            }
            return sourceLinks;
        }

        private async Task<ImmutableArray<ResolvedSource>> ResolveSourcesAsync(
            ImmutableArray<SourceFileInfo> sourceFileInfos,
            ImmutableArray<SourceLink> sourceLinks,
            Encoding encoding)
        {
            _logger.LogInformation("Locating source files");

            var tasks = new Task<ResolvedSource>[sourceFileInfos.Length];
            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                var sourceFileInfo = sourceFileInfos[i];
                tasks[i] = _sourceResolver.ResolveSourceAsync(sourceFileInfo, sourceLinks, encoding);
            }
            var result = await Task.WhenAll(tasks).ConfigureAwait(false);

            return result.ToImmutableArray();
        }

        #region CSharp
        private async Task<Compilation> CreateCSharpCompilationAsync(CompilationOptionsReader compilationOptionsReader, string assemblyName)
        {
            var metadataReferenceInfos = GetMetadataReferenceInfos(compilationOptionsReader);
            var (compilationOptions, parseOptions, encoding) = CreateCSharpCompilationOptions(compilationOptionsReader, assemblyName);
            var sourceFileInfos = GetSourceFileInfos(compilationOptionsReader, encoding);

            var metadataReferences = ResolveMetadataReferences(metadataReferenceInfos);
            logResolvedMetadataReferences();

            var sourceLinks = compilationOptionsReader.GetSourceLinksOpt();
            var sources = await ResolveSourcesAsync(sourceFileInfos, sourceLinks, encoding).ConfigureAwait(false);
            logResolvedSources();

            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: sources.Select(s => CSharpSyntaxTree.ParseText(s.SourceText, options: parseOptions, path: s.SourceFileInfo.SourceFilePath)).ToImmutableArray(),
                references: metadataReferences,
                options: compilationOptions);

            void logResolvedMetadataReferences()
            {
                using var _ = _logger.BeginScope("Metadata References");
                for (var i = 0; i < metadataReferenceInfos.Length; i++)
                {
                    _logger.LogInformation($@"""{metadataReferences[i].Display}"" - {metadataReferenceInfos[i].Mvid}");
                }
            }

            void logResolvedSources()
            {
                using var _ = _logger.BeginScope("Source Names");
                foreach (var resolvedSource in sources)
                {
                    var sourceFileInfo = resolvedSource.SourceFileInfo;
                    var hash = BitConverter.ToString(sourceFileInfo.Hash).Replace("-", "");
                    _logger.LogInformation($@"""{resolvedSource.DisplayPath}"" - {sourceFileInfo.HashAlgorithm} - {hash}");
                }
            }
        }

        private (CSharpCompilationOptions, CSharpParseOptions, Encoding) CreateCSharpCompilationOptions(CompilationOptionsReader optionsReader, string assemblyName)
        {
            using var scope = _logger.BeginScope("Options");
            var pdbCompilationOptions = optionsReader.GetMetadataCompilationOptions();

            var langVersionString = GetUniqueOption("language-version");
            var optimization = GetUniqueOption("optimization");
            // TODO: Check portability policy if needed
            // pdbCompilationOptions.TryGetValue("portability-policy", out var portabilityPolicyString);
            TryGetUniqueOption("default-encoding", out var defaultEncoding);
            TryGetUniqueOption("fallback-encoding", out var fallbackEncoding);
            TryGetUniqueOption("define", out var define);
            TryGetUniqueOption("checked", out var checkedString);
            TryGetUniqueOption("nullable", out var nullable);
            TryGetUniqueOption("unsafe", out var unsafeString);

            var encodingString = defaultEncoding ?? fallbackEncoding;
            var encoding = encodingString is null
                ? Encoding.UTF8
                : Encoding.GetEncoding(encodingString);

            CS.LanguageVersionFacts.TryParse(langVersionString, out var langVersion);

            var preprocessorSymbols = define == null
                ? ImmutableArray<string>.Empty
                : define.Split(',').ToImmutableArray();

            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(langVersion)
                .WithPreprocessorSymbols(preprocessorSymbols);

            var (optimizationLevel, plus) = GetOptimizationLevel(optimization);

            var nullableOptions = nullable is null
                ? NullableContextOptions.Disable
                : (NullableContextOptions)Enum.Parse(typeof(NullableContextOptions), nullable);

            var compilationOptions = new CSharpCompilationOptions(
                optionsReader.GetOutputKind(),
                reportSuppressedDiagnostics: false,

                // PROTOTYPE: can't rely on the implicity moduleName here. In the case of .NET Core EXE the output name will
                // end with .dll but the inferred name will be .exe
                moduleName: assemblyName + ".dll",
                mainTypeName: optionsReader.GetMainTypeName(),
                scriptClassName: null,
                usings: null,
                optimizationLevel,
                !string.IsNullOrEmpty(checkedString) && bool.Parse(checkedString),
                !string.IsNullOrEmpty(unsafeString) && bool.Parse(unsafeString),
                cryptoKeyContainer: null,
                cryptoKeyFile: null,
                cryptoPublicKey: optionsReader.GetPublicKey()?.ToImmutableArray() ?? default,
                delaySign: null,
                Platform.AnyCpu,

                // presence of diagnostics is expected to not affect emit.
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
            compilationOptions.DebugPlusMode = plus;

            return (compilationOptions, parseOptions, encoding);

            bool TryGetUniqueOption(string name, [NotNullWhen(true)] out string? value)
            {
                var result = pdbCompilationOptions.TryGetUniqueOption(name, out value);
                _logger.LogInformation($"{name} - {value}");
                return result;
            }

            string GetUniqueOption(string name)
            {
                _ = TryGetUniqueOption(name, out var value);
                return value!;
            }
        }

        private static (OptimizationLevel, bool) GetOptimizationLevel(string optimizationLevel)
            => optimizationLevel switch
            {
                "debug" => (OptimizationLevel.Debug, false),
                "debug-plus" => (OptimizationLevel.Debug, true),
                "release" => (OptimizationLevel.Release, false),
                _ => throw new InvalidDataException($"Optimization \"{optimizationLevel}\" level not recognized")
            };

        #endregion

        #region Visual Basic
        // TODO: can we just make "get compilation options" and "create the compilation" virtual and share the rest?
        private async Task<Compilation> CreateVisualBasicCompilationAsync(CompilationOptionsReader compilationOptionsReader, string assemblyName)
        {
            var metadataReferenceInfos = GetMetadataReferenceInfos(compilationOptionsReader);
            var compilationOptions = CreateVisualBasicCompilationOptions(compilationOptionsReader);
            var sourceFileInfos = GetSourceFileInfos(compilationOptionsReader, Encoding.UTF8); // TODO: is this encoding right?
            var metadataReferences = ResolveMetadataReferences(metadataReferenceInfos);
            var sourceLinks = ResolveSourceLinks(compilationOptionsReader);
            var sources = await ResolveSourcesAsync(sourceFileInfos, sourceLinks, Encoding.UTF8).ConfigureAwait(false);

            return VisualBasicCompilation.Create(
                assemblyName,
                syntaxTrees: sources.Select(s => VisualBasicSyntaxTree.ParseText(s.SourceText, options: compilationOptions.ParseOptions, path: s.DisplayPath)).ToImmutableArray(),
                references: metadataReferences,
                options: compilationOptions);
        }

        private static VisualBasicCompilationOptions CreateVisualBasicCompilationOptions(CompilationOptionsReader pdbReader)
        {
            var pdbCompilationOptions = pdbReader.GetMetadataCompilationOptions();

            var langVersionString = pdbCompilationOptions.GetUniqueOption("language-version");
            var optimization = pdbCompilationOptions.GetUniqueOption("optimization");
            pdbCompilationOptions.TryGetUniqueOption("define", out var define);
            pdbCompilationOptions.TryGetUniqueOption("strict", out var strict);
            pdbCompilationOptions.TryGetUniqueOption("checked", out var checkedString);

            VB.LanguageVersion langVersion = default;
            VB.LanguageVersionFacts.TryParse(langVersionString, ref langVersion);

            var preprocessorSymbols = string.IsNullOrEmpty(define)
                ? Array.Empty<KeyValuePair<string, object>>()
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
