// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Serialization
{
    [ExportLanguageService(typeof(IOptionsSerializationService), LanguageNames.CSharp), Shared]
    internal class CSharpOptionsSerializationService : AbstractOptionsSerializationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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

        public override void WriteTo(ParseOptions options, ObjectWriter writer)
        {
            WriteParseOptionsTo(options, writer);

            var csharpOptions = (CSharpParseOptions)options;
            writer.WriteInt32((int)csharpOptions.SpecifiedLanguageVersion);
            writer.WriteValue(options.PreprocessorSymbolNames.ToArray());
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
