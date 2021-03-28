// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Cci;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Utilities;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace BuildValidator
{
    public sealed class VisualBasicCompilationFactory : CompilationFactory
    {
        public new VisualBasicCompilationOptions CompilationOptions { get; }
        public new VisualBasicParseOptions ParseOptions => CompilationOptions.ParseOptions;

        protected override ParseOptions CommonParseOptions => ParseOptions;
        protected override CompilationOptions CommonCompilationOptions => CompilationOptions;

        private VisualBasicCompilationFactory(
            string assemblyFileName,
            CompilationOptionsReader optionsReader,
            VisualBasicCompilationOptions compilationOptions)
            : base(assemblyFileName, optionsReader)
        {
            CompilationOptions = compilationOptions;
        }

        internal static new VisualBasicCompilationFactory Create(string assemblyFileName, CompilationOptionsReader optionsReader)
        {
            Debug.Assert(optionsReader.GetLanguageName() == LanguageNames.VisualBasic);
            var compilationOptions = CreateVisualBasicCompilationOptions(assemblyFileName, optionsReader);
            return new VisualBasicCompilationFactory(assemblyFileName, optionsReader, compilationOptions);
        }

        public override SyntaxTree CreateSyntaxTree(string filePath, SourceText sourceText)
            => VisualBasicSyntaxTree.ParseText(sourceText, ParseOptions, filePath);

        public override Compilation CreateCompilation(
            ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableArray<MetadataReference> metadataReferences)
            => VisualBasicCompilation.Create(
                Path.GetFileNameWithoutExtension(AssemblyFileName),
                syntaxTrees: syntaxTrees,
                references: metadataReferences,
                options: CompilationOptions);

        private static VisualBasicCompilationOptions CreateVisualBasicCompilationOptions(string assemblyFileName, CompilationOptionsReader optionsReader)
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
                var diagnostic = diagnostics?.FirstOrDefault(x => x.IsUnsuppressedError);
                if (diagnostic is object)
                {
                    throw new Exception($"Cannot create compilation options: {diagnostic}");
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
                moduleName: assemblyFileName,
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
    }
}
