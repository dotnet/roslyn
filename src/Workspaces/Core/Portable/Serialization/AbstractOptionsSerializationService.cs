// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

internal abstract class AbstractOptionsSerializationService : IOptionsSerializationService
{
    public abstract void WriteTo(CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken);
    public abstract void WriteTo(ParseOptions options, ObjectWriter writer);

    public abstract CompilationOptions ReadCompilationOptionsFrom(ObjectReader reader, CancellationToken cancellationToken);
    public abstract ParseOptions ReadParseOptionsFrom(ObjectReader reader, CancellationToken cancellationToken);

    protected static void WriteCompilationOptionsTo(CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        writer.WriteInt32((int)options.OutputKind);
        writer.WriteBoolean(options.ReportSuppressedDiagnostics);
        writer.WriteString(options.ModuleName);
        writer.WriteString(options.MainTypeName);
        writer.WriteString(options.ScriptClassName);
        writer.WriteInt32((int)options.OptimizationLevel);
        writer.WriteBoolean(options.CheckOverflow);

        // REVIEW: is it okay this being not part of snapshot?
        writer.WriteString(options.CryptoKeyContainer);
        writer.WriteString(options.CryptoKeyFile);

        writer.WriteSpan(options.CryptoPublicKey.AsSpan());
        writer.WriteBoolean(options.DelaySign.HasValue);
        if (options.DelaySign.HasValue)
        {
            writer.WriteBoolean(options.DelaySign.Value);
        }

        writer.WriteInt32((int)options.Platform);
        writer.WriteInt32((int)options.GeneralDiagnosticOption);

        writer.WriteInt32(options.WarningLevel);

        // REVIEW: I don't think there is a guarantee on ordering of elements in the immutable dictionary.
        //         unfortunately, we need to sort them to make it deterministic
        writer.WriteInt32(options.SpecificDiagnosticOptions.Count);
        foreach (var kv in options.SpecificDiagnosticOptions.OrderBy(o => o.Key))
        {
            writer.WriteString(kv.Key);
            writer.WriteInt32((int)kv.Value);
        }

        writer.WriteBoolean(options.ConcurrentBuild);
        writer.WriteBoolean(options.Deterministic);
        writer.WriteBoolean(options.PublicSign);

        writer.WriteByte((byte)options.MetadataImportOptions);

        // REVIEW: What should I do with these. we probably need to implement either out own one
        //         or somehow share these as service....
        //
        // XmlReferenceResolver xmlReferenceResolver
        // SourceReferenceResolver sourceReferenceResolver
        // MetadataReferenceResolver metadataReferenceResolver
        // AssemblyIdentityComparer assemblyIdentityComparer
        // StrongNameProvider strongNameProvider
    }

    protected static (
        OutputKind outputKind,
        bool reportSuppressedDiagnostics,
        string moduleName,
        string mainTypeName,
        string scriptClassName,
        OptimizationLevel optimizationLevel,
        bool checkOverflow,
        string cryptoKeyContainer,
        string cryptoKeyFile,
        ImmutableArray<byte> cryptoPublicKey,
        bool? delaySign,
        Platform platform,
        ReportDiagnostic generalDiagnosticOption,
        int warningLevel,
        IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions,
        bool concurrentBuild,
        bool deterministic,
        bool publicSign,
        MetadataImportOptions metadataImportOptions,
        XmlReferenceResolver xmlReferenceResolver,
        SourceReferenceResolver sourceReferenceResolver,
        MetadataReferenceResolver metadataReferenceResolver,
        AssemblyIdentityComparer assemblyIdentityComparer,
        StrongNameProvider strongNameProvider) ReadCompilationOptionsPieces(
        ObjectReader reader,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var outputKind = (OutputKind)reader.ReadInt32();
        var reportSuppressedDiagnostics = reader.ReadBoolean();
        var moduleName = reader.ReadString();
        var mainTypeName = reader.ReadString();

        var scriptClassName = reader.ReadString();
        var optimizationLevel = (OptimizationLevel)reader.ReadInt32();
        var checkOverflow = reader.ReadBoolean();

        // REVIEW: is it okay this being not part of snapshot?
        var cryptoKeyContainer = reader.ReadString();
        var cryptoKeyFile = reader.ReadString();

        var cryptoPublicKey = reader.ReadByteArray().ToImmutableArrayOrEmpty();

        bool? delaySign = reader.ReadBoolean() ? reader.ReadBoolean() : null;

        var platform = (Platform)reader.ReadInt32();
        var generalDiagnosticOption = (ReportDiagnostic)reader.ReadInt32();

        var warningLevel = reader.ReadInt32();

        // REVIEW: I don't think there is a guarantee on ordering of elements in the immutable dictionary.
        //         unfortunately, we need to sort them to make it deterministic
        //         not sure why CompilationOptions uses SequencialEqual to check options equality
        //         when ordering can change result of it even if contents are same.
        var count = reader.ReadInt32();
        List<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptionsList = null;

        if (count > 0)
        {
            specificDiagnosticOptionsList = new List<KeyValuePair<string, ReportDiagnostic>>(count);

            for (var i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var value = (ReportDiagnostic)reader.ReadInt32();

                specificDiagnosticOptionsList.Add(KeyValuePairUtil.Create(key, value));
            }
        }

        var specificDiagnosticOptions = specificDiagnosticOptionsList ?? SpecializedCollections.EmptyEnumerable<KeyValuePair<string, ReportDiagnostic>>();

        var concurrentBuild = reader.ReadBoolean();
        var deterministic = reader.ReadBoolean();
        var publicSign = reader.ReadBoolean();

        var metadataImportOptions = (MetadataImportOptions)reader.ReadByte();

        // REVIEW: What should I do with these. are these service required when compilation is built ourselves, not through
        //         compiler.
        var xmlReferenceResolver = XmlFileResolver.Default;
        var sourceReferenceResolver = SourceFileResolver.Default;
        var assemblyIdentityComparer = DesktopAssemblyIdentityComparer.Default;
        var strongNameProvider = new DesktopStrongNameProvider(ImmutableArray<string>.Empty, Path.GetTempPath());

        return (
            outputKind,
            reportSuppressedDiagnostics,
            moduleName,
            mainTypeName,
            scriptClassName,
            optimizationLevel,
            checkOverflow,
            cryptoKeyContainer,
            cryptoKeyFile,
            cryptoPublicKey,
            delaySign,
            platform,
            generalDiagnosticOption,
            warningLevel,
            specificDiagnosticOptions,
            concurrentBuild,
            deterministic,
            publicSign,
            metadataImportOptions,
            xmlReferenceResolver,
            sourceReferenceResolver,
            metadataReferenceResolver: null,
            assemblyIdentityComparer,
            strongNameProvider);
    }

    protected static void WriteParseOptionsTo(ParseOptions options, ObjectWriter writer)
    {
        writer.WriteInt32((int)options.Kind);
        writer.WriteInt32((int)options.DocumentationMode);

        // REVIEW: I don't think there is a guarantee on ordering of elements in the readonly dictionary.
        //         unfortunately, we need to sort them to make it deterministic
        writer.WriteInt32(options.Features.Count);
        foreach (var kv in options.Features.OrderBy(o => o.Key))
        {
            writer.WriteString(kv.Key);
            writer.WriteString(kv.Value);
        }
    }

    protected static (SourceCodeKind kind, DocumentationMode documentationMode, IEnumerable<KeyValuePair<string, string>> features) ReadParseOptionsPieces(
        ObjectReader reader,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var kind = (SourceCodeKind)reader.ReadInt32();
        var documentationMode = (DocumentationMode)reader.ReadInt32();

        // REVIEW: I don't think there is a guarantee on ordering of elements in the immutable dictionary.
        //         unfortunately, we need to sort them to make it deterministic
        //         not sure why ParseOptions uses SequencialEqual to check options equality
        //         when ordering can change result of it even if contents are same.
        var count = reader.ReadInt32();
        List<KeyValuePair<string, string>> featuresList = null;

        if (count > 0)
        {
            featuresList = new List<KeyValuePair<string, string>>(count);

            for (var i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var value = reader.ReadString();

                featuresList.Add(KeyValuePairUtil.Create(key, value));
            }
        }

        var features = featuresList ?? SpecializedCollections.EmptyEnumerable<KeyValuePair<string, string>>();
        return (kind, documentationMode, features);
    }
}
