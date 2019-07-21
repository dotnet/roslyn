// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        [ImportingConstructor]
        public CSharpOptionsSerializationService()
        {
        }

        public override void WriteTo(CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken)
        {
            WriteCompilationOptionsTo(options, writer, cancellationToken);

            var csharpOptions = (CSharpCompilationOptions)options;
            writer.WriteValue(csharpOptions.Usings.ToArray());
            writer.WriteBoolean(csharpOptions.AllowUnsafe);
            writer.WriteByte((byte)csharpOptions.NullableContextOptions);
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

            foreach (var option in CSharpCodeStyleOptions.GetCodeStyleOptions())
            {
                WriteOptionTo(options, option, writer, cancellationToken);
            }

            foreach (var option in CSharpCodeStyleOptions.GetExpressionBodyOptions())
            {
                WriteOptionTo(options, option, writer, cancellationToken);
            }

            WriteOptionTo(options, CSharpCodeStyleOptions.PreferBraces, writer, cancellationToken);
            WriteOptionTo(options, CSharpCodeStyleOptions.PreferredModifierOrder, writer, cancellationToken);
            WriteOptionTo(options, CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, writer, cancellationToken);
            WriteOptionTo(options, CSharpCodeStyleOptions.PreferStaticLocalFunction, writer, cancellationToken);
        }

        public override OptionSet ReadOptionSetFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            OptionSet options = new SerializedPartialOptionSet();

            options = ReadOptionSetFrom(options, LanguageNames.CSharp, reader, cancellationToken);

            foreach (var option in CSharpCodeStyleOptions.GetCodeStyleOptions())
            {
                options = ReadOptionFrom(options, option, reader, cancellationToken);
            }

            foreach (var option in CSharpCodeStyleOptions.GetExpressionBodyOptions())
            {
                options = ReadOptionFrom(options, option, reader, cancellationToken);
            }

            options = ReadOptionFrom(options, CSharpCodeStyleOptions.PreferBraces, reader, cancellationToken);
            options = ReadOptionFrom(options, CSharpCodeStyleOptions.PreferredModifierOrder, reader, cancellationToken);
            options = ReadOptionFrom(options, CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, reader, cancellationToken);
            options = ReadOptionFrom(options, CSharpCodeStyleOptions.PreferStaticLocalFunction, reader, cancellationToken);

            return options;
        }

        public override CompilationOptions ReadCompilationOptionsFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            ReadCompilationOptionsFrom(
                reader,
                out var outputKind, out var reportSuppressedDiagnostics, out var moduleName, out var mainTypeName, out var scriptClassName,
                out var optimizationLevel, out var checkOverflow, out var cryptoKeyContainer, out var cryptoKeyFile, out var cryptoPublicKey,
                out var delaySign, out var platform, out var generalDiagnosticOption, out var warningLevel, out var specificDiagnosticOptions,
                out var concurrentBuild, out var deterministic, out var publicSign, out var metadataImportOptions,
                out var xmlReferenceResolver, out var sourceReferenceResolver, out var metadataReferenceResolver, out var assemblyIdentityComparer,
                out var strongNameProvider, cancellationToken);

            var usings = reader.ReadArray<string>();
            var allowUnsafe = reader.ReadBoolean();
            var nullableContextOptions = (NullableContextOptions)reader.ReadByte();

            return new CSharpCompilationOptions(
                outputKind, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName, usings, optimizationLevel, checkOverflow, allowUnsafe,
                cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign, platform, generalDiagnosticOption, warningLevel, specificDiagnosticOptions, concurrentBuild,
                deterministic, xmlReferenceResolver, sourceReferenceResolver, metadataReferenceResolver, assemblyIdentityComparer, strongNameProvider, publicSign,
                metadataImportOptions, nullableContextOptions);
        }

        public override ParseOptions ReadParseOptionsFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            ReadParseOptionsFrom(reader, out var kind, out var documentationMode, out var features, cancellationToken);

            var languageVersion = (LanguageVersion)reader.ReadInt32();
            var preprocessorSymbolNames = reader.ReadArray<string>();

            var options = new CSharpParseOptions(languageVersion, documentationMode, kind, preprocessorSymbolNames);
            return options.WithFeatures(features);
        }
    }
}
