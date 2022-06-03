// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
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

            writer.WriteValue(options.CryptoPublicKey.AsSpan());
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

        protected static void ReadCompilationOptionsFrom(
            ObjectReader reader,
            out OutputKind outputKind,
            out bool reportSuppressedDiagnostics,
            out string moduleName,
            out string mainTypeName,
            out string scriptClassName,
            out OptimizationLevel optimizationLevel,
            out bool checkOverflow,
            out string cryptoKeyContainer,
            out string cryptoKeyFile,
            out ImmutableArray<byte> cryptoPublicKey,
            out bool? delaySign,
            out Platform platform,
            out ReportDiagnostic generalDiagnosticOption,
            out int warningLevel,
            out IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions,
            out bool concurrentBuild,
            out bool deterministic,
            out bool publicSign,
            out MetadataImportOptions metadataImportOptions,
            out XmlReferenceResolver xmlReferenceResolver,
            out SourceReferenceResolver sourceReferenceResolver,
            out MetadataReferenceResolver metadataReferenceResolver,
            out AssemblyIdentityComparer assemblyIdentityComparer,
            out StrongNameProvider strongNameProvider,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            outputKind = (OutputKind)reader.ReadInt32();
            reportSuppressedDiagnostics = reader.ReadBoolean();
            moduleName = reader.ReadString();
            mainTypeName = reader.ReadString();

            scriptClassName = reader.ReadString();
            optimizationLevel = (OptimizationLevel)reader.ReadInt32();
            checkOverflow = reader.ReadBoolean();

            // REVIEW: is it okay this being not part of snapshot?
            cryptoKeyContainer = reader.ReadString();
            cryptoKeyFile = reader.ReadString();

            cryptoPublicKey = reader.ReadArray<byte>().ToImmutableArrayOrEmpty();

            delaySign = reader.ReadBoolean() ? reader.ReadBoolean() : null;

            platform = (Platform)reader.ReadInt32();
            generalDiagnosticOption = (ReportDiagnostic)reader.ReadInt32();

            warningLevel = reader.ReadInt32();

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

            specificDiagnosticOptions = specificDiagnosticOptionsList ?? SpecializedCollections.EmptyEnumerable<KeyValuePair<string, ReportDiagnostic>>();

            concurrentBuild = reader.ReadBoolean();
            deterministic = reader.ReadBoolean();
            publicSign = reader.ReadBoolean();

            metadataImportOptions = (MetadataImportOptions)reader.ReadByte();

            // REVIEW: What should I do with these. are these service required when compilation is built ourselves, not through
            //         compiler.
            xmlReferenceResolver = XmlFileResolver.Default;
            sourceReferenceResolver = SourceFileResolver.Default;
            metadataReferenceResolver = null;
            assemblyIdentityComparer = DesktopAssemblyIdentityComparer.Default;
            strongNameProvider = new DesktopStrongNameProvider();
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

        protected static void ReadParseOptionsFrom(
            ObjectReader reader,
            out SourceCodeKind kind,
            out DocumentationMode documentationMode,
            out IEnumerable<KeyValuePair<string, string>> features,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            kind = (SourceCodeKind)reader.ReadInt32();
            documentationMode = (DocumentationMode)reader.ReadInt32();

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

            features = featuresList ?? SpecializedCollections.EmptyEnumerable<KeyValuePair<string, string>>();
        }
    }
}
