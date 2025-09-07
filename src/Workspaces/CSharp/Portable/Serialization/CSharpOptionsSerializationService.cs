// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Serialization;

[ExportLanguageService(typeof(IOptionsSerializationService), LanguageNames.CSharp), Shared]
internal sealed class CSharpOptionsSerializationService : AbstractOptionsSerializationService
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
        writer.WriteArray(csharpOptions.Usings, static (w, u) => w.WriteString(u));
        writer.WriteBoolean(csharpOptions.AllowUnsafe);
        writer.WriteByte((byte)csharpOptions.NullableContextOptions);
    }

    public override void WriteTo(ParseOptions options, ObjectWriter writer)
    {
        WriteParseOptionsTo(options, writer);

        var csharpOptions = (CSharpParseOptions)options;
        writer.WriteInt32((int)csharpOptions.SpecifiedLanguageVersion);
        writer.WriteArray(options.PreprocessorSymbolNames.ToImmutableArrayOrEmpty(), static (w, p) => w.WriteString(p));
    }

    public override CompilationOptions ReadCompilationOptionsFrom(ObjectReader reader, CancellationToken cancellationToken)
    {
        var (outputKind, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName,
            optimizationLevel, checkOverflow, cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey,
            delaySign, platform, generalDiagnosticOption, warningLevel, specificDiagnosticOptions,
            concurrentBuild, deterministic, publicSign, metadataImportOptions,
            xmlReferenceResolver, sourceReferenceResolver, metadataReferenceResolver, assemblyIdentityComparer,
            strongNameProvider) = ReadCompilationOptionsPieces(reader, cancellationToken);

        var usings = reader.ReadArray(static r => r.ReadString());
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
        var (kind, documentationMode, features) = ReadParseOptionsPieces(reader, cancellationToken);

        var languageVersion = (LanguageVersion)reader.ReadInt32();
        var preprocessorSymbolNames = reader.ReadArray(static r => r.ReadString());

        var options = new CSharpParseOptions(languageVersion, documentationMode, kind, preprocessorSymbolNames);
        return options.WithFeatures(features);
    }
}
