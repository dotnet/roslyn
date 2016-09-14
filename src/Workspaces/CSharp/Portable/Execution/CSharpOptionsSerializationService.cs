// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Execution
{
    [ExportLanguageService(typeof(IOptionsSerializationService), LanguageNames.CSharp), Shared]
    internal class CSharpOptionsSerializationService : AbstractOptionsSerializationService
    {
        public override void WriteTo(CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken)
        {
            WriteCompilationOptionsTo(options, writer, cancellationToken);

            var csharpOptions = (CSharpCompilationOptions)options;
            writer.WriteValue(csharpOptions.Usings.ToArray());
            writer.WriteBoolean(csharpOptions.AllowUnsafe);
        }

        public override void WriteTo(ParseOptions options, ObjectWriter writer, CancellationToken cancellationToken)
        {
            WriteParseOptionsTo(options, writer, cancellationToken);

            var csharpOptions = (CSharpParseOptions)options;
            writer.WriteInt32((int)csharpOptions.LanguageVersion);
            writer.WriteValue(options.PreprocessorSymbolNames.ToArray());
        }

        public override void WriteTo(OptionSet options, ObjectWriter writer, CancellationToken cancellationToken)
        {
            WriteOptionSetTo(options, LanguageNames.CSharp, writer, cancellationToken);

            WriteOptionTo(options, CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, writer, cancellationToken);
            WriteOptionTo(options, CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, writer, cancellationToken);
            WriteOptionTo(options, CSharpCodeStyleOptions.UseImplicitTypeWherePossible, writer, cancellationToken);
        }

        public override CompilationOptions ReadCompilationOptionsFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            OutputKind outputKind;
            bool reportSuppressedDiagnostics;
            string moduleName;
            string mainTypeName;
            string scriptClassName;
            OptimizationLevel optimizationLevel;
            bool checkOverflow;
            string cryptoKeyContainer;
            string cryptoKeyFile;
            ImmutableArray<byte> cryptoPublicKey;
            bool? delaySign;
            Platform platform;
            ReportDiagnostic generalDiagnosticOption;
            int warningLevel;
            IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions;
            bool concurrentBuild;
            bool deterministic;
            bool publicSign;
            XmlReferenceResolver xmlReferenceResolver;
            SourceReferenceResolver sourceReferenceResolver;
            MetadataReferenceResolver metadataReferenceResolver;
            AssemblyIdentityComparer assemblyIdentityComparer;
            StrongNameProvider strongNameProvider;

            ReadCompilationOptionsFrom(
                reader,
                out outputKind, out reportSuppressedDiagnostics, out moduleName, out mainTypeName, out scriptClassName,
                out optimizationLevel, out checkOverflow, out cryptoKeyContainer, out cryptoKeyFile, out cryptoPublicKey,
                out delaySign, out platform, out generalDiagnosticOption, out warningLevel, out specificDiagnosticOptions,
                out concurrentBuild, out deterministic, out publicSign, out xmlReferenceResolver, out sourceReferenceResolver,
                out metadataReferenceResolver, out assemblyIdentityComparer, out strongNameProvider, cancellationToken);

            var usings = reader.ReadArray<string>();
            var allowUnsafe = reader.ReadBoolean();

            return new CSharpCompilationOptions(
                outputKind, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName, usings, optimizationLevel, checkOverflow, allowUnsafe,
                cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign, platform, generalDiagnosticOption, warningLevel, specificDiagnosticOptions, concurrentBuild,
                deterministic, xmlReferenceResolver, sourceReferenceResolver, metadataReferenceResolver, assemblyIdentityComparer, strongNameProvider, publicSign);
        }

        public override ParseOptions ReadParseOptionsFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            SourceCodeKind kind;
            DocumentationMode documentationMode;
            IEnumerable<KeyValuePair<string, string>> features;
            ReadParseOptionsFrom(reader, out kind, out documentationMode, out features, cancellationToken);

            var languageVersion = (LanguageVersion)reader.ReadInt32();
            var preprocessorSymbolNames = reader.ReadArray<string>();

            var options = new CSharpParseOptions(languageVersion, documentationMode, kind, preprocessorSymbolNames);
            return options.WithFeatures(features);
        }

        public override OptionSet ReadOptionSetFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            OptionSet options = new SerializedPartialOptionSet();

            options = ReadOptionSetFrom(options, LanguageNames.CSharp, reader, cancellationToken);

            options = ReadOptionFrom(options, CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, reader, cancellationToken);
            options = ReadOptionFrom(options, CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, reader, cancellationToken);
            options = ReadOptionFrom(options, CSharpCodeStyleOptions.UseImplicitTypeWherePossible, reader, cancellationToken);

            return options;
        }
    }
}
