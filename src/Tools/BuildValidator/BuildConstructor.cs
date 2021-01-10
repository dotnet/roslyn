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

        public Compilation CreateCompilation(MetadataReader metadataReader, PEReader peReader, string name)
        {
            var pdbReader = new CompilationOptionsReader(metadataReader, peReader);
            var pdbCompilationOptions = pdbReader.GetMetadataCompilationOptions();

            if (pdbCompilationOptions.Length == 0)
            {
                throw new InvalidDataException("Did not find compilation options in pdb");
            }

            if (pdbCompilationOptions.TryGetUniqueOption("language", out var language))
            {
                var compilation = language switch
                {
                    LanguageNames.CSharp => CreateCSharpCompilation(pdbReader, name),
                    LanguageNames.VisualBasic => CreateVisualBasicCompilation(pdbReader, name),
                    _ => throw new InvalidDataException($"{language} is not a known language")
                };

                return compilation;
            }

            throw new InvalidDataException("Did not find language in compilation options");
        }

        private ImmutableArray<MetadataReferenceInfo> GetMetadataReferenceInfos(CompilationOptionsReader compilationOptionsReader)
        {
            using var _ = _logger.BeginScope("Metadata References");
            var referenceInfos = compilationOptionsReader.GetMetadataReferences();
            var count = 0;
            foreach (var refInfo in referenceInfos)
            {
                count++;
                if (count >= 10)
                {
                    _logger.LogInformation($"... {referenceInfos.Length - count} more");
                    break;
                }
                _logger.LogInformation($"{refInfo.Name} - {refInfo.Mvid}");
            }

            return referenceInfos;
        }

        private IEnumerable<SourceFileInfo> GetSourceFileInfos(CompilationOptionsReader compilationOptionsReader)
        {
            using var _ = _logger.BeginScope("Source Names");
            var sourceFileInfos = compilationOptionsReader.GetSourceFileInfos();
            foreach (var sourceFileInfo in sourceFileInfos)
            {
                var hash = BitConverter.ToString(sourceFileInfo.Hash).Replace("-", "");
                _logger.LogInformation($"{sourceFileInfo.SourceFileName} - {sourceFileInfo.HashAlgorithm} - {hash}");
            }

            return sourceFileInfos;
        }

        private ImmutableArray<MetadataReference> ResolveMetadataReferences(ImmutableArray<MetadataReferenceInfo> referenceInfos)
        {
            _logger.LogInformation("Locating metadata references");
            return _referenceResolver.ResolveReferences(referenceInfos);
        }

        private ImmutableArray<(string SourceFilePath, SourceText SourceText)> ResolveSources(IEnumerable<SourceFileInfo> sourceFileInfos, Encoding encoding)
        {
            _logger.LogInformation("Locating source files");

            var builder = ImmutableArray.CreateBuilder<(string filename, SourceText sourceText)>();
            foreach (var sourceFileInfo in sourceFileInfos)
            {
                var text = _sourceResolver.ResolveSource(sourceFileInfo, encoding);
                builder.Add((sourceFileInfo.SourceFilePath, text));
            }

            return builder.ToImmutable();
        }

        #region CSharp
        private Compilation CreateCSharpCompilation(CompilationOptionsReader compilationOptionsReader, string assemblyName)
        {
            var metadataReferenceInfos = GetMetadataReferenceInfos(compilationOptionsReader);
            var sourceFileInfos = GetSourceFileInfos(compilationOptionsReader);
            var (compilationOptions, parseOptions, encoding) = CreateCSharpCompilationOptions(compilationOptionsReader, assemblyName);

            var metadataReferences = ResolveMetadataReferences(metadataReferenceInfos);
            var sources = ResolveSources(sourceFileInfos, encoding);
            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: sources.Select(s => CSharpSyntaxTree.ParseText(s.SourceText, options: parseOptions, path: s.SourceFilePath)).ToImmutableArray(),
                references: metadataReferences,
                options: compilationOptions);
        }

        private (CSharpCompilationOptions, CSharpParseOptions, Encoding) CreateCSharpCompilationOptions(CompilationOptionsReader pdbReader, string assemblyName)
        {
            using var scope = _logger.BeginScope("Options");
            var pdbCompilationOptions = pdbReader.GetMetadataCompilationOptions();

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
                pdbReader.GetOutputKind(),
                reportSuppressedDiagnostics: false,

                // PROTOTYPE: can't rely on the implicity moduleName here. In the case of .NET Core EXE the output name will
                // end with .dll but the inferred name will be .exe
                moduleName: assemblyName + ".dll",
                mainTypeName: pdbReader.GetMainTypeName(),
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
        private Compilation CreateVisualBasicCompilation(CompilationOptionsReader compilationOptionsReader, string assemblyName)
        {
            var metadataReferenceInfos = GetMetadataReferenceInfos(compilationOptionsReader);
            var sourceFileInfos = GetSourceFileInfos(compilationOptionsReader);
            var compilationOptions = CreateVisualBasicCompilationOptions(compilationOptionsReader);
            var metadataReferences = ResolveMetadataReferences(metadataReferenceInfos);
            var sources = ResolveSources(sourceFileInfos, Encoding.UTF8);

            return VisualBasicCompilation.Create(
                assemblyName,
                syntaxTrees: sources.Select(s => VisualBasicSyntaxTree.ParseText(s.SourceText, options: compilationOptions.ParseOptions, path: s.SourceFilePath)).ToImmutableArray(),
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
