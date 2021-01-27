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

        public Task<Compilation> CreateCompilationAsync(MetadataReader pdbReader, PEReader peReader, string name)
        {
            var optionsReader = new CompilationOptionsReader(pdbReader, peReader);
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

        private ImmutableArray<SourceFileInfo> GetSourceFileInfos(CompilationOptionsReader compilationOptionsReader, Encoding encoding)
        {
            using var _ = _logger.BeginScope("Source Names");
            var sourceFileInfos = compilationOptionsReader.GetSourceFileInfos(encoding);
            var count = 0;
            foreach (var sourceFileInfo in sourceFileInfos)
            {
                count++;
                if (count >= 10)
                {
                    _logger.LogInformation($"... {sourceFileInfos.Length - count} more");
                    break;
                }
                var hash = BitConverter.ToString(sourceFileInfo.Hash).Replace("-", "");
                _logger.LogInformation($"{sourceFileInfo.SourceFileName} - {sourceFileInfo.HashAlgorithmDescription} - {hash}");
            }

            return sourceFileInfos;
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
                _logger.LogWarning("No source links found in pdb");
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

        private async Task<ImmutableArray<(string SourceFilePath, SourceText SourceText)>> ResolveSourcesAsync(
            ImmutableArray<SourceFileInfo> sourceFileInfos,
            ImmutableArray<SourceLink> sourceLinks,
            Encoding encoding)
        {
            _logger.LogInformation("Locating source files");

            var tasks = new Task<(string SourceFilePath, SourceText SourceText)>[sourceFileInfos.Length];
            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                var sourceFileInfo = sourceFileInfos[i];
                tasks[i] = _sourceResolver.ResolveSourceAsync(sourceFileInfo, sourceLinks, encoding);
            }
            var result = await Task.WhenAll(tasks);

            return result.ToImmutableArray();
        }

        #region CSharp
        private async Task<Compilation> CreateCSharpCompilationAsync(CompilationOptionsReader compilationOptionsReader, string assemblyName)
        {
            var metadataReferenceInfos = GetMetadataReferenceInfos(compilationOptionsReader);
            var (compilationOptions, parseOptions, encoding) = CreateCSharpCompilationOptions(compilationOptionsReader, assemblyName);
            var sourceFileInfos = GetSourceFileInfos(compilationOptionsReader, encoding);

            var metadataReferences = ResolveMetadataReferences(metadataReferenceInfos); // TODO: improve perf
            var sourceLinks = compilationOptionsReader.GetSourceLinksOpt();
            var sources = await ResolveSourcesAsync(sourceFileInfos, sourceLinks, encoding);
            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: sources.Select(s => CSharpSyntaxTree.ParseText(s.SourceText, options: parseOptions, path: s.SourceFilePath)).ToImmutableArray(),
                references: metadataReferences,
                options: compilationOptions);
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
        private async Task<Compilation> CreateVisualBasicCompilationAsync(CompilationOptionsReader compilationOptionsReader, string assemblyName)
        {
            var metadataReferenceInfos = GetMetadataReferenceInfos(compilationOptionsReader);
            var compilationOptions = CreateVisualBasicCompilationOptions(compilationOptionsReader);
            var sourceFileInfos = GetSourceFileInfos(compilationOptionsReader, Encoding.UTF8); // TODO: is this encoding right?
            var metadataReferences = ResolveMetadataReferences(metadataReferenceInfos);
            var sourceLinks = ResolveSourceLinks(compilationOptionsReader);
            var sources = await ResolveSourcesAsync(sourceFileInfos, sourceLinks, Encoding.UTF8);

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


#if !NETCOREAPP
// TODO: remove this by adding an IVT
namespace System.Diagnostics.CodeAnalysis
{
    //
    // Summary:
    //     Specifies that when a method returns System.Diagnostics.CodeAnalysis.NotNullWhenAttribute.ReturnValue,
    //     the parameter will not be null even if the corresponding type allows it.
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class NotNullWhenAttribute : Attribute
    {
        //
        // Summary:
        //     Initializes the attribute with the specified return value condition.
        //
        // Parameters:
        //   returnValue:
        //     The return value condition. If the method returns this value, the associated
        //     parameter will not be null.
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        //
        // Summary:
        //     Gets the return value condition.
        //
        // Returns:
        //     The return value condition. If the method returns this value, the associated
        //     parameter will not be null.
        public bool ReturnValue { get; }
    }
}
#endif