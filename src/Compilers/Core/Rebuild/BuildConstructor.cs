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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Cci;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace BuildValidator
{
    public class BuildConstructor
    {
        private readonly ILogger _logger;

        public BuildConstructor(ILogger logger)
        {
            _logger = logger;
        }

        public Compilation CreateCompilation(
            CompilationOptionsReader compilationOptionsReader,
            string fileName,
            ImmutableArray<ResolvedSource> sources,
            ImmutableArray<MetadataReference> metadataReferences)
        {
            // We try to handle assemblies missing compilation options gracefully by skipping them.
            // However, if an assembly has some bad combination of data, for example if it contains
            // compilation options but not metadata references, then we throw an exception.
            if (!compilationOptionsReader.TryGetMetadataCompilationOptions(out var pdbCompilationOptions)
                || pdbCompilationOptions.Length == 0)
            {
                throw new Exception($"{fileName} did not contain compilation options in its PDB");
            }

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

                if (hadError)
                {
                    throw new Exception("Diagnostics creating the compilation");
                }

                return compilation;
            }

            throw new InvalidDataException("Did not find language in compilation options");
        }

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

            var langVersionString = pdbCompilationOptions.GetUniqueOption(CompilationOptionNames.LanguageVersion);
            pdbCompilationOptions.TryGetUniqueOption(CompilationOptionNames.Optimization, out var optimization);
            pdbCompilationOptions.TryGetUniqueOption(CompilationOptionNames.Platform, out var platform);

            // TODO: Check portability policy if needed
            // pdbCompilationOptions.TryGetValue("portability-policy", out var portabilityPolicyString);
            pdbCompilationOptions.TryGetUniqueOption(_logger, CompilationOptionNames.Define, out var define);
            pdbCompilationOptions.TryGetUniqueOption(_logger, CompilationOptionNames.Checked, out var checkedString);
            pdbCompilationOptions.TryGetUniqueOption(_logger, CompilationOptionNames.Nullable, out var nullable);
            pdbCompilationOptions.TryGetUniqueOption(_logger, CompilationOptionNames.Unsafe, out var unsafeString);

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
                GetPlatform(platform),

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

        private static Platform GetPlatform(string? platform)
            => platform is null
                ? Platform.AnyCpu
                : (Platform)Enum.Parse(typeof(Platform), platform);

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
            pdbCompilationOptions.TryGetUniqueOption(CompilationOptionNames.Platform, out var platform);
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
                platform: GetPlatform(platform),
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

        public static unsafe EmitResult Emit(
            Stream rebuildPeStream,
            CompilationOptionsReader optionsReader,
            Compilation producedCompilation,
            CancellationToken cancellationToken)
        {
            var peHeader = optionsReader.PeReader.PEHeaders.PEHeader!;
            var win32Resources = optionsReader.PeReader.GetSectionData(peHeader.ResourceTableDirectory.RelativeVirtualAddress);
            using var win32ResourceStream = new UnmanagedMemoryStream(win32Resources.Pointer, win32Resources.Length);

            var sourceLink = optionsReader.GetSourceLinkUTF8();

            var embeddedTexts = producedCompilation.SyntaxTrees
                    .Select(st => (path: st.FilePath, text: st.GetText()))
                    .Where(pair => pair.text.CanBeEmbedded)
                    .Select(pair => EmbeddedText.FromSource(pair.path, pair.text))
                    .ToImmutableArray();

            var debugEntryPoint = getDebugEntryPoint();

            var emitResult = producedCompilation.Emit(
                peStream: rebuildPeStream,
                pdbStream: null,
                xmlDocumentationStream: null,
                win32Resources: win32ResourceStream,
                useRawWin32Resources: true,
                manifestResources: optionsReader.GetManifestResources(),
                options: new EmitOptions(
                    debugInformationFormat: DebugInformationFormat.Embedded,
                    highEntropyVirtualAddressSpace: (peHeader.DllCharacteristics & DllCharacteristics.HighEntropyVirtualAddressSpace) != 0,
                    subsystemVersion: SubsystemVersion.Create(peHeader.MajorSubsystemVersion, peHeader.MinorSubsystemVersion)),
                debugEntryPoint: debugEntryPoint,
                metadataPEStream: null,
                pdbOptionsBlobReader: optionsReader.GetMetadataCompilationOptionsBlobReader(),
                sourceLinkStream: sourceLink != null ? new MemoryStream(sourceLink) : null,
                embeddedTexts: embeddedTexts,
                cancellationToken: cancellationToken);

            return emitResult;

            IMethodSymbol? getDebugEntryPoint()
            {
                if (optionsReader.GetMainTypeName() is { } mainTypeName &&
                    optionsReader.GetMainMethodName() is { } mainMethodName)
                {
                    var typeSymbol = producedCompilation.GetTypeByMetadataName(mainTypeName);
                    if (typeSymbol is object)
                    {
                        var methodSymbols = typeSymbol
                            .GetMembers(mainMethodName)
                            .OfType<IMethodSymbol>();
                        return methodSymbols.FirstOrDefault();
                    }
                }

                return null;
            }
        }
    }
}
