// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        protected static async ValueTask<(
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
            StrongNameProvider strongNameProvider)> ReadCompilationOptionsPiecesAsync(
            ObjectReader reader,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var outputKind = (OutputKind)await reader.ReadInt32Async().ConfigureAwait(false);
            var reportSuppressedDiagnostics = await reader.ReadBooleanAsync().ConfigureAwait(false);
            var moduleName = await reader.ReadStringAsync().ConfigureAwait(false);
            var mainTypeName = await reader.ReadStringAsync().ConfigureAwait(false);

            var scriptClassName = await reader.ReadStringAsync().ConfigureAwait(false);
            var optimizationLevel = (OptimizationLevel)await reader.ReadInt32Async().ConfigureAwait(false);
            var checkOverflow = await reader.ReadBooleanAsync().ConfigureAwait(false);

            // REVIEW: is it okay this being not part of snapshot?
            var cryptoKeyContainer = await reader.ReadStringAsync().ConfigureAwait(false);
            var cryptoKeyFile = await reader.ReadStringAsync().ConfigureAwait(false);

            var cryptoPublicKey = (await reader.ReadArrayAsync<byte>().ConfigureAwait(false)).ToImmutableArrayOrEmpty();

            bool? delaySign = await reader.ReadBooleanAsync().ConfigureAwait(false) ? await reader.ReadBooleanAsync().ConfigureAwait(false) : null;

            var platform = (Platform)await reader.ReadInt32Async().ConfigureAwait(false);
            var generalDiagnosticOption = (ReportDiagnostic)await reader.ReadInt32Async().ConfigureAwait(false);

            var warningLevel = await reader.ReadInt32Async().ConfigureAwait(false);

            // REVIEW: I don't think there is a guarantee on ordering of elements in the immutable dictionary.
            //         unfortunately, we need to sort them to make it deterministic
            //         not sure why CompilationOptions uses SequencialEqual to check options equality
            //         when ordering can change result of it even if contents are same.
            var count = await reader.ReadInt32Async().ConfigureAwait(false);
            List<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptionsList = null;

            if (count > 0)
            {
                specificDiagnosticOptionsList = new List<KeyValuePair<string, ReportDiagnostic>>(count);

                for (var i = 0; i < count; i++)
                {
                    var key = await reader.ReadStringAsync().ConfigureAwait(false);
                    var value = (ReportDiagnostic)await reader.ReadInt32Async().ConfigureAwait(false);

                    specificDiagnosticOptionsList.Add(KeyValuePairUtil.Create(key, value));
                }
            }

            var specificDiagnosticOptions = specificDiagnosticOptionsList ?? SpecializedCollections.EmptyEnumerable<KeyValuePair<string, ReportDiagnostic>>();

            var concurrentBuild = await reader.ReadBooleanAsync().ConfigureAwait(false);
            var deterministic = await reader.ReadBooleanAsync().ConfigureAwait(false);
            var publicSign = await reader.ReadBooleanAsync().ConfigureAwait(false);

            var metadataImportOptions = (MetadataImportOptions)await reader.ReadByteAsync().ConfigureAwait(false);

            // REVIEW: What should I do with these. are these service required when compilation is built ourselves, not through
            //         compiler.
            var xmlReferenceResolver = XmlFileResolver.Default;
            var sourceReferenceResolver = SourceFileResolver.Default;
            var assemblyIdentityComparer = DesktopAssemblyIdentityComparer.Default;
            var strongNameProvider = new DesktopStrongNameProvider();

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

        protected static async ValueTask<(
            SourceCodeKind kind,
            DocumentationMode documentationMode,
            IEnumerable<KeyValuePair<string, string>> features)> ReadParseOptionsPiecesAsync(
            ObjectReader reader,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = (SourceCodeKind)await reader.ReadInt32Async().ConfigureAwait(false);
            var documentationMode = (DocumentationMode)await reader.ReadInt32Async().ConfigureAwait(false);

            // REVIEW: I don't think there is a guarantee on ordering of elements in the immutable dictionary.
            //         unfortunately, we need to sort them to make it deterministic
            //         not sure why ParseOptions uses SequencialEqual to check options equality
            //         when ordering can change result of it even if contents are same.
            var count = await reader.ReadInt32Async().ConfigureAwait(false);
            List<KeyValuePair<string, string>> featuresList = null;

            if (count > 0)
            {
                featuresList = new List<KeyValuePair<string, string>>(count);

                for (var i = 0; i < count; i++)
                {
                    var key = await reader.ReadStringAsync().ConfigureAwait(false);
                    var value = await reader.ReadStringAsync().ConfigureAwait(false);

                    featuresList.Add(KeyValuePairUtil.Create(key, value));
                }
            }

            var features = featuresList ?? SpecializedCollections.EmptyEnumerable<KeyValuePair<string, string>>();
            return (kind, documentationMode, features);
        }
    }
}
