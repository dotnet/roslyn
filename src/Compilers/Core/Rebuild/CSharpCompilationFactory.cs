// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Microsoft.Cci;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Text;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Rebuild
{
    public sealed class CSharpCompilationFactory : CompilationFactory
    {
        public new CSharpParseOptions ParseOptions { get; }
        public new CSharpCompilationOptions CompilationOptions { get; }

        protected override ParseOptions CommonParseOptions => ParseOptions;
        protected override CompilationOptions CommonCompilationOptions => CompilationOptions;

        private CSharpCompilationFactory(
            string assemblyFileName,
            CompilationOptionsReader optionsReader,
            CSharpParseOptions parseOptions,
            CSharpCompilationOptions compilationOptions)
            : base(assemblyFileName, optionsReader)
        {
            Debug.Assert(optionsReader.GetLanguageName() == LanguageNames.CSharp);
            ParseOptions = parseOptions;
            CompilationOptions = compilationOptions;
        }

        internal static new CSharpCompilationFactory Create(string assemblyFileName, CompilationOptionsReader optionsReader)
        {
            Debug.Assert(optionsReader.GetLanguageName() == LanguageNames.CSharp);
            var (compilationOptions, parseOptions) = CreateCSharpCompilationOptions(assemblyFileName, optionsReader);
            return new CSharpCompilationFactory(assemblyFileName, optionsReader, parseOptions, compilationOptions);
        }

        public override SyntaxTree CreateSyntaxTree(string filePath, SourceText sourceText)
            => CSharpSyntaxTree.ParseText(sourceText, ParseOptions, filePath);

        public override Compilation CreateCompilation(
            ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableArray<MetadataReference> metadataReferences)
            => CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(AssemblyFileName),
                syntaxTrees: syntaxTrees,
                references: metadataReferences,
                options: CompilationOptions);

        private static (CSharpCompilationOptions, CSharpParseOptions) CreateCSharpCompilationOptions(string assemblyFileName, CompilationOptionsReader optionsReader)
        {
            var pdbOptions = optionsReader.GetMetadataCompilationOptions();

            var langVersionString = pdbOptions.GetUniqueOption(CompilationOptionNames.LanguageVersion);
            pdbOptions.TryGetUniqueOption(CompilationOptionNames.Optimization, out var optimization);
            pdbOptions.TryGetUniqueOption(CompilationOptionNames.Platform, out var platform);

            // TODO: Check portability policy if needed
            // pdbCompilationOptions.TryGetValue("portability-policy", out var portabilityPolicyString);
            pdbOptions.TryGetUniqueOption(CompilationOptionNames.Define, out var define);
            pdbOptions.TryGetUniqueOption(CompilationOptionNames.Checked, out var checkedString);
            pdbOptions.TryGetUniqueOption(CompilationOptionNames.Nullable, out var nullable);
            pdbOptions.TryGetUniqueOption(CompilationOptionNames.Unsafe, out var unsafeString);

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
                pdbOptions.OptionToEnum<OutputKind>(CompilationOptionNames.OutputKind) ?? OutputKind.DynamicallyLinkedLibrary,
                reportSuppressedDiagnostics: false,

                moduleName: assemblyFileName,
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
                sourceReferenceResolver: RebuildSourceReferenceResolver.Instance,
                metadataReferenceResolver: null,

                assemblyIdentityComparer: null,
                strongNameProvider: null,
                publicSign: false,

                metadataImportOptions: MetadataImportOptions.Public,
                nullableContextOptions: nullableOptions);
            compilationOptions.DebugPlusMode = plus;

            return (compilationOptions, parseOptions);
        }
    }
}
