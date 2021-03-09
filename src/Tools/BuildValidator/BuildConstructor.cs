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
using Microsoft.Cci;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;
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

        public (Compilation? compilation, bool isError) CreateCompilation(CompilationOptionsReader compilationOptionsReader, string fileName)
        {
            // We try to handle assemblies missing compilation options gracefully by skipping them.
            // However, if an assembly has some bad combination of data, for example if it contains
            // compilation options but not metadata references, then we throw an exception.
            if (!compilationOptionsReader.TryGetMetadataCompilationOptions(out var pdbCompilationOptions)
                || pdbCompilationOptions.Length == 0)
            {
                _logger.LogInformation($"{fileName} did not contain compilation options in its PDB");
                return (compilation: null, isError: false);
            }

            var metadataReferenceInfos = compilationOptionsReader.GetMetadataReferences();
            var encoding = compilationOptionsReader.GetEncoding();
            var sourceFileInfos = compilationOptionsReader.GetSourceFileInfos(encoding);

            _logger.LogInformation("Locating metadata references");
            if (!_referenceResolver.TryResolveReferences(metadataReferenceInfos, out var metadataReferences))
            {
                _logger.LogError($"Failed to rebuild {fileName} due to missing metadata references");
                return (compilation: null, isError: true);
            }
            logResolvedMetadataReferences();

            var sourceLinks = ResolveSourceLinks(compilationOptionsReader);
            var sources = ResolveSources(sourceFileInfos, sourceLinks, encoding);
            logResolvedSources();

            if (pdbCompilationOptions.TryGetUniqueOption("language", out var language))
            {
                var diagnosticBag = DiagnosticBag.GetInstance();
                var compilation = language switch
                {
                    LanguageNames.CSharp => CreateCSharpCompilation(fileName, compilationOptionsReader, sources, metadataReferences),
                    LanguageNames.VisualBasic => CreateVisualBasicCompilation(fileName, compilationOptionsReader, sources, metadataReferences, diagnosticBag),
                    _ => throw new InvalidDataException($"{language} is not a known language")
                };

                var diagnostics = diagnosticBag.ToReadOnlyAndFree();
                var hadError = false;
                foreach (var diagnostic in diagnostics)
                {
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        _logger.LogError(diagnostic.ToString());
                        hadError = true;
                    }
                    else
                    {
                        _logger.LogWarning(diagnostic.ToString());
                    }
                }

                compilation = hadError ? null : compilation;
                return (compilation, isError: compilation is null);
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
                    var embeddedCompressedHash = sourceFileInfo.EmbeddedCompressedHash is { } compressedHash
                        ? ("[uncompressed]" + BitConverter.ToString(compressedHash).Replace("-", ""))
                        : null;
                    _logger.LogInformation($@"""{resolvedSource.DisplayPath}"" - {sourceFileInfo.HashAlgorithm} - {hash} - {embeddedCompressedHash}");
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
                sourceLinks = ImmutableArray<SourceLink>.Empty;
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
            string fileName,
            CompilationOptionsReader optionsReader,
            ImmutableArray<ResolvedSource> sources,
            ImmutableArray<MetadataReference> metadataReferences)
        {
            var (compilationOptions, parseOptions) = CreateCSharpCompilationOptions(optionsReader, fileName);
            return CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(fileName),
                syntaxTrees: sources.Select(s => CSharpSyntaxTree.ParseText(s.SourceText, options: parseOptions, path: s.SourceFileInfo.SourceFilePath)).ToImmutableArray(),
                references: metadataReferences,
                options: compilationOptions);
        }

        private (CSharpCompilationOptions, CSharpParseOptions) CreateCSharpCompilationOptions(CompilationOptionsReader optionsReader, string fileName)
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

                moduleName: fileName,
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

        private static (OptimizationLevel, bool) GetOptimizationLevel(string? optimizationLevel)
            => optimizationLevel switch
            {
                null or "debug" => (OptimizationLevel.Debug, false),
                "debug-plus" => (OptimizationLevel.Debug, true),
                "release" => (OptimizationLevel.Release, false),
                _ => throw new InvalidDataException($"Optimization \"{optimizationLevel}\" level not recognized")
            };

        #endregion

        #region Visual Basic
        private Compilation CreateVisualBasicCompilation(
            string fileName,
            CompilationOptionsReader optionsReader,
            ImmutableArray<ResolvedSource> sources,
            ImmutableArray<MetadataReference> metadataReferences,
            DiagnosticBag diagnosticBag)
        {
            var compilationOptions = CreateVisualBasicCompilationOptions(optionsReader, fileName, diagnosticBag);
            return VisualBasicCompilation.Create(
                Path.GetFileNameWithoutExtension(fileName),
                syntaxTrees: sources.Select(s => VisualBasicSyntaxTree.ParseText(s.SourceText, options: compilationOptions.ParseOptions, path: s.SourceFileInfo.SourceFilePath)).ToImmutableArray(),
                references: metadataReferences,
                options: compilationOptions);
        }

        private static VisualBasicCompilationOptions CreateVisualBasicCompilationOptions(CompilationOptionsReader optionsReader, string fileName, DiagnosticBag diagnosticBag)
        {
            var pdbCompilationOptions = optionsReader.GetMetadataCompilationOptions();

            var langVersionString = pdbCompilationOptions.GetUniqueOption(CompilationOptionNames.LanguageVersion);
            pdbCompilationOptions.TryGetUniqueOption(CompilationOptionNames.Optimization, out var optimization);
            pdbCompilationOptions.TryGetUniqueOption(CompilationOptionNames.GlobalNamespaces, out var globalNamespacesString);

            IEnumerable<GlobalImport>? globalImports = null;
            if (!string.IsNullOrEmpty(globalNamespacesString))
            {
                globalImports = GlobalImport.Parse(globalNamespacesString.Split(';'));
            }

            VB.LanguageVersion langVersion = default;
            VB.LanguageVersionFacts.TryParse(langVersionString, ref langVersion);

            IReadOnlyDictionary<string, object>? preprocessorSymbols = null;
            if (OptionToString(CompilationOptionNames.Define) is string defineString)
            {
                preprocessorSymbols = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(defineString, out var diagnostics);
                if (diagnostics is object)
                {
                    diagnosticBag.AddRange(diagnostics);
                }
            }

            var parseOptions = VisualBasicParseOptions
                .Default
                .WithLanguageVersion(langVersion)
                .WithPreprocessorSymbols(preprocessorSymbols.ToImmutableArrayOrEmpty());

            var (optimizationLevel, plus) = GetOptimizationLevel(optimization);
            var isChecked = OptionToBool(CompilationOptionNames.Checked) ?? true;
            var embedVBRuntime = OptionToBool(CompilationOptionNames.EmbedRuntime) ?? false;
            var rootNamespace = OptionToString(CompilationOptionNames.RootNamespace);

            var compilationOptions = new VisualBasicCompilationOptions(
                optionsReader.GetOutputKind(),
                moduleName: fileName,
                mainTypeName: optionsReader.GetMainTypeName(),
                scriptClassName: "Script",
                globalImports: globalImports,
                rootNamespace: rootNamespace,
                optionStrict: OptionToEnum<OptionStrict>(CompilationOptionNames.OptionStrict) ?? OptionStrict.Off,
                optionInfer: OptionToBool(CompilationOptionNames.OptionInfer) ?? false,
                optionExplicit: OptionToBool(CompilationOptionNames.OptionExplicit) ?? false,
                optionCompareText: OptionToBool(CompilationOptionNames.OptionCompareText) ?? false,
                parseOptions: parseOptions,
                embedVbCoreRuntime: embedVBRuntime,
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
            compilationOptions.DebugPlusMode = plus;

            return compilationOptions;

            string? OptionToString(string option) => pdbCompilationOptions.TryGetUniqueOption(option, out var value) ? value : null;
            bool? OptionToBool(string option) => pdbCompilationOptions.TryGetUniqueOption(option, out var value) ? ToBool(value) : null;
            T? OptionToEnum<T>(string option) where T : struct => pdbCompilationOptions.TryGetUniqueOption(option, out var value) ? ToEnum<T>(value) : null;
            static bool? ToBool(string value) => bool.TryParse(value, out var boolValue) ? boolValue : null;
            static T? ToEnum<T>(string value) where T : struct => Enum.TryParse<T>(value, out var enumValue) ? enumValue : null;
        }
        #endregion
    }
}
