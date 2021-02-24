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

        public Compilation CreateCompilation(CompilationOptionsReader compilationOptionsReader, string name)
        {
            var pdbCompilationOptions = compilationOptionsReader.GetMetadataCompilationOptions();
            if (pdbCompilationOptions.Length == 0)
            {
                throw new InvalidDataException("Did not find compilation options in pdb");
            }

            var metadataReferenceInfos = compilationOptionsReader.GetMetadataReferences();
            var encoding = compilationOptionsReader.GetEncoding();
            var sourceFileInfos = compilationOptionsReader.GetSourceFileInfos(encoding);

            _logger.LogInformation("Locating metadata references");
            var metadataReferences = _referenceResolver.ResolveReferences(metadataReferenceInfos);
            logResolvedMetadataReferences();

            var sourceLinks = ResolveSourceLinks(compilationOptionsReader);
            var sources = ResolveSources(sourceFileInfos, sourceLinks, encoding);
            logResolvedSources();

            if (pdbCompilationOptions.TryGetUniqueOption("language", out var language))
            {
                var compilation = language switch
                {
                    LanguageNames.CSharp => CreateCSharpCompilation(name, compilationOptionsReader, sources, metadataReferences),
                    LanguageNames.VisualBasic => CreateVisualBasicCompilation(name, compilationOptionsReader, sources, metadataReferences),
                    _ => throw new InvalidDataException($"{language} is not a known language")
                };

                return compilation;
            }

            throw new InvalidDataException("Did not find language in compilation options");

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

        private ImmutableArray<ResolvedSource> ResolveSources(
            ImmutableArray<SourceFileInfo> sourceFileInfos,
            ImmutableArray<SourceLink> sourceLinks,
            Encoding encoding)
        {
            _logger.LogInformation("Locating source files");

            var sources = ImmutableArray.CreateBuilder<ResolvedSource>();
            foreach (var sourceFileInfo in sourceFileInfos)
            {
                sources.Add(_sourceResolver.ResolveSource(sourceFileInfo, sourceLinks, encoding));
            }

            return sources.ToImmutable();
        }

        #region CSharp
        private Compilation CreateCSharpCompilation(
            string assemblyName,
            CompilationOptionsReader optionsReader,
            ImmutableArray<ResolvedSource> sources,
            ImmutableArray<MetadataReference> metadataReferences)
        {
            var (compilationOptions, parseOptions) = CreateCSharpCompilationOptions(optionsReader, assemblyName);
            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: sources.Select(s => CSharpSyntaxTree.ParseText(s.SourceText, options: parseOptions, path: s.SourceFileInfo.SourceFilePath)).ToImmutableArray(),
                references: metadataReferences,
                options: compilationOptions);
        }

        private (CSharpCompilationOptions, CSharpParseOptions) CreateCSharpCompilationOptions(CompilationOptionsReader optionsReader, string assemblyName)
        {
            using var scope = _logger.BeginScope("Options");
            var pdbCompilationOptions = optionsReader.GetMetadataCompilationOptions();

            var langVersionString = pdbCompilationOptions.GetUniqueOption("language-version");
            var optimization = pdbCompilationOptions.GetUniqueOption("optimization");
            // TODO: Check portability policy if needed
            // pdbCompilationOptions.TryGetValue("portability-policy", out var portabilityPolicyString);
            pdbCompilationOptions.TryGetUniqueOption(_logger, "define", out var define);
            pdbCompilationOptions.TryGetUniqueOption(_logger, "checked", out var checkedString);
            pdbCompilationOptions.TryGetUniqueOption(_logger, "nullable", out var nullable);
            pdbCompilationOptions.TryGetUniqueOption(_logger, "unsafe", out var unsafeString);

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

                // TODO: can't rely on the implicity moduleName here. In the case of .NET Core EXE the output name will
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

            return (compilationOptions, parseOptions);
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
        private Compilation CreateVisualBasicCompilation(
            string assemblyName,
            CompilationOptionsReader optionsReader,
            ImmutableArray<ResolvedSource> sources,
            ImmutableArray<MetadataReference> metadataReferences)
        {
            var compilationOptions = CreateVisualBasicCompilationOptions(optionsReader);
            return VisualBasicCompilation.Create(
                assemblyName,
                syntaxTrees: sources.Select(s => VisualBasicSyntaxTree.ParseText(s.SourceText, options: compilationOptions.ParseOptions, path: s.DisplayPath)).ToImmutableArray(),
                references: metadataReferences,
                options: compilationOptions);
        }

        private static VisualBasicCompilationOptions CreateVisualBasicCompilationOptions(CompilationOptionsReader optionsReader)
        {
            var pdbCompilationOptions = optionsReader.GetMetadataCompilationOptions();

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

            // TODO: rebuilding VB projects fails due to reference issues
            // for example, core types like KeyValuePair are missing
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
                cryptoPublicKey: optionsReader.GetPublicKey()?.ToImmutableArray() ?? default,
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
