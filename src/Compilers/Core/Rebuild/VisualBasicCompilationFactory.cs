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

namespace Microsoft.CodeAnalysis.Rebuild
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
            var pdbOptions = optionsReader.GetMetadataCompilationOptions();

            var langVersionString = pdbOptions.GetUniqueOption(CompilationOptionNames.LanguageVersion);
            pdbOptions.TryGetUniqueOption(CompilationOptionNames.Optimization, out var optimization);
            pdbOptions.TryGetUniqueOption(CompilationOptionNames.Platform, out var platform);
            pdbOptions.TryGetUniqueOption(CompilationOptionNames.GlobalNamespaces, out var globalNamespacesString);

            IEnumerable<GlobalImport>? globalImports = null;
            if (!string.IsNullOrEmpty(globalNamespacesString))
            {
                globalImports = GlobalImport.Parse(globalNamespacesString.Split(';'));
            }

            VB.LanguageVersion langVersion = default;
            VB.LanguageVersionFacts.TryParse(langVersionString, ref langVersion);

            IReadOnlyDictionary<string, object>? preprocessorSymbols = null;
            if (pdbOptions.OptionToString(CompilationOptionNames.Define) is string defineString)
            {
                preprocessorSymbols = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(defineString, out var diagnostics);
                var diagnostic = diagnostics?.FirstOrDefault(x => x.IsUnsuppressedError);
                if (diagnostic is object)
                {
                    throw new Exception(string.Format(RebuildResources.Cannot_create_compilation_options_0, diagnostic));
                }
            }

            var parseOptions = VisualBasicParseOptions
                .Default
                .WithLanguageVersion(langVersion)
                .WithPreprocessorSymbols(preprocessorSymbols.ToImmutableArrayOrEmpty());

            var (optimizationLevel, plus) = GetOptimizationLevel(optimization);
            var isChecked = pdbOptions.OptionToBool(CompilationOptionNames.Checked) ?? true;
            var embedVBRuntime = pdbOptions.OptionToBool(CompilationOptionNames.EmbedRuntime) ?? false;
            var rootNamespace = pdbOptions.OptionToString(CompilationOptionNames.RootNamespace);

            var compilationOptions = new VisualBasicCompilationOptions(
                pdbOptions.OptionToEnum<OutputKind>(CompilationOptionNames.OutputKind) ?? OutputKind.DynamicallyLinkedLibrary,
                moduleName: assemblyFileName,
                mainTypeName: optionsReader.GetMainTypeName(),
                scriptClassName: "Script",
                globalImports: globalImports,
                rootNamespace: rootNamespace,
                optionStrict: pdbOptions.OptionToEnum<OptionStrict>(CompilationOptionNames.OptionStrict) ?? OptionStrict.Off,
                optionInfer: pdbOptions.OptionToBool(CompilationOptionNames.OptionInfer) ?? false,
                optionExplicit: pdbOptions.OptionToBool(CompilationOptionNames.OptionExplicit) ?? false,
                optionCompareText: pdbOptions.OptionToBool(CompilationOptionNames.OptionCompareText) ?? false,
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
                sourceReferenceResolver: RebuildSourceReferenceResolver.Instance,
                metadataReferenceResolver: null,
                assemblyIdentityComparer: null,
                strongNameProvider: null,
                publicSign: false,
                reportSuppressedDiagnostics: false,
                metadataImportOptions: MetadataImportOptions.Public);
            compilationOptions.DebugPlusMode = plus;

            return compilationOptions;
        }
    }
}
