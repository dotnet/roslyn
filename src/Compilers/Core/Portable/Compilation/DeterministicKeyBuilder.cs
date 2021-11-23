// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The string returned from this function represents the inputs to the compiler which impact determinism.  It is 
    /// meant to be inline with the specification here:
    /// 
    ///     - https://github.com/dotnet/roslyn/blob/main/docs/compilers/Deterministic%20Inputs.md
    /// 
    /// Issue #8193 tracks filling this out to the full specification. 
    /// 
    ///     https://github.com/dotnet/roslyn/issues/8193
    /// </summary>
    /// <remarks>
    /// Options which can cause compilation failure, but doesn't impact the result of a successful
    /// compilation should be included. That is because it is interesting to describe error states
    /// not just success states. Think about caching build failures as well as build successes.
    ///
    /// When an option is omitted, say if there is no value for a public crypto key, we should emit
    /// the property with a null value vs. omitting the property. Either approach would produce 
    /// correct results the preference is to be declarative that an option is omitted.
    /// </remarks>
    internal abstract class DeterministicKeyBuilder
    {
        protected DeterministicKeyBuilder()
        {
        }

        protected void WriteFileName(JsonWriter writer, string name, string? filePath, DeterministicKeyOptions options)
        {
            if ((options & DeterministicKeyOptions.IgnorePaths) != 0)
            {
                filePath = Path.GetFileName(filePath);
            }

            writer.Write(name, filePath);
        }

        protected void WriteByteArrayValue(JsonWriter writer, string name, ImmutableArray<byte> value)
        {
            if (!value.IsDefault)
            {
                WriteByteArrayValue(writer, name, value.AsSpan());
            }
        }

        internal static void EncodeByteArrayValue(ReadOnlySpan<byte> value, StringBuilder builder)
        {
            foreach (var b in value)
            {
                builder.Append(b.ToString("x"));
            }
        }

        protected void WriteByteArrayValue(JsonWriter writer, string name, ReadOnlySpan<byte> value)
        {
            var builder = PooledStringBuilder.GetInstance();
            EncodeByteArrayValue(value, builder.Builder);
            writer.Write(name, builder.ToStringAndFree());
        }

        protected void WriteType(JsonWriter writer, string key, Type? type)
        {
            writer.WriteKey(key);
            WriteType(writer, type);
        }

        protected void WriteType(JsonWriter writer, Type? type)
        {
            if (type is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteObjectStart();
            writer.Write("fullName", type.FullName);
            // Note that the file path to the assembly is deliberately not included here. The file path
            // of the assembly does not contribute to the output of the program.
            writer.Write("assemblyName", type.Assembly.FullName);
            writer.Write("mvid", type.Assembly.ManifestModule.ModuleVersionId.ToString());
            writer.WriteObjectEnd();
        }

        private (JsonWriter, PooledStringBuilder) CreateWriter()
        {
            var builder = PooledStringBuilder.GetInstance();
            var writer = new StringWriter(builder);
            return (new JsonWriter(writer), builder);
        }

        internal string GetKey(
            CompilationOptions compilationOptions,
            ImmutableArray<SyntaxTreeKey> syntaxTrees,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<AdditionalText> additionalTexts,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<ISourceGenerator> generators,
            EmitOptions? emitOptions,
            DeterministicKeyOptions options,
            CancellationToken cancellationToken)
        {
            additionalTexts = additionalTexts.NullToEmpty();
            analyzers = analyzers.NullToEmpty();
            generators = generators.NullToEmpty();

            var (writer, builder) = CreateWriter();

            writer.WriteObjectStart();

            writer.WriteKey("compilation");
            WriteCompilation(writer, compilationOptions, syntaxTrees, references, options, cancellationToken);
            writer.WriteKey("additionalTexts");
            writeAdditionalTexts();
            writer.WriteKey("analyzers");
            writeAnalyzers();
            writer.WriteKey("generators");
            writeGenerators();
            writer.WriteKey("emitOptions");
            WriteEmitOptions(writer, emitOptions);

            writer.WriteObjectEnd();

            return builder.ToStringAndFree();

            void writeAdditionalTexts()
            {
                writer.WriteArrayStart();
                foreach (var additionalText in additionalTexts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    writer.WriteObjectStart();
                    WriteFileName(writer, "fileName", additionalText.Path, options);
                    writer.WriteKey("text");
                    WriteSourceText(writer, additionalText.GetText(cancellationToken));
                    writer.WriteObjectEnd();
                }
                writer.WriteArrayEnd();
            }

            void writeAnalyzers()
            {
                writer.WriteArrayStart();
                foreach (var analyzer in analyzers)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WriteType(writer, analyzer.GetType());
                }
                writer.WriteArrayEnd();
            }

            void writeGenerators()
            {
                writer.WriteArrayStart();
                foreach (var generator in generators)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WriteType(writer, generator.GetType());
                }
                writer.WriteArrayEnd();
            }
        }

        internal static string GetGuidValue(in Guid guid) => guid.ToString("D");

        internal string GetKey(EmitOptions? emitOptions)
        {
            var (writer, builder) = CreateWriter();
            WriteEmitOptions(writer, emitOptions);
            return builder.ToStringAndFree();
        }

        private void WriteCompilation(
            JsonWriter writer,
            CompilationOptions compilationOptions,
            ImmutableArray<SyntaxTreeKey> syntaxTrees,
            ImmutableArray<MetadataReference> references,
            DeterministicKeyOptions options,
            CancellationToken cancellationToken)
        {
            writer.WriteObjectStart();
            writeToolsVersions();

            writer.WriteKey("options");
            WriteCompilationOptions(writer, compilationOptions);

            writer.WriteKey("syntaxTrees");
            writer.WriteArrayStart();
            foreach (var syntaxTree in syntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WriteSyntaxTree(writer, syntaxTree, options, cancellationToken);
            }
            writer.WriteArrayEnd();

            writer.WriteKey("references");
            writer.WriteArrayStart();
            foreach (var reference in references)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WriteMetadataReference(writer, reference, options, cancellationToken);
            }
            writer.WriteArrayEnd();
            writer.WriteObjectEnd();

            void writeToolsVersions()
            {
                writer.WriteKey("toolsVersions");
                writer.WriteObjectStart();
                if ((options & DeterministicKeyOptions.IgnoreToolVersions) != 0)
                {
                    writer.WriteObjectEnd();
                    return;
                }

                var compilerVersion = typeof(Compilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                writer.Write("compilerVersion", compilerVersion);

                var runtimeVersion = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                writer.Write("runtimeVersion", runtimeVersion);

                writer.Write("framework", RuntimeInformation.FrameworkDescription);
                writer.Write("os", RuntimeInformation.OSDescription);

                writer.WriteObjectEnd();
            }
        }

        private void WriteSyntaxTree(JsonWriter writer, SyntaxTreeKey syntaxTree, DeterministicKeyOptions options, CancellationToken cancellationToken)
        {
            writer.WriteObjectStart();
            WriteFileName(writer, "fileName", syntaxTree.FilePath, options);
            writer.WriteKey("text");
            WriteSourceText(writer, syntaxTree.GetText(cancellationToken));
            writer.WriteKey("parseOptions");
            WriteParseOptions(writer, syntaxTree.Options);
            writer.WriteObjectEnd();
        }

        private void WriteSourceText(JsonWriter writer, SourceText? sourceText)
        {
            if (sourceText is null)
            {
                return;
            }

            writer.WriteObjectStart();
            WriteByteArrayValue(writer, "checksum", sourceText.GetChecksum());
            writer.Write("checksumAlgorithm", sourceText.ChecksumAlgorithm);
            writer.Write("encoding", sourceText.Encoding?.EncodingName);
            writer.WriteObjectEnd();
        }

        internal void WriteMetadataReference(
            JsonWriter writer,
            MetadataReference reference,
            DeterministicKeyOptions deterministicKeyOptions,
            CancellationToken cancellationToken)
        {
            writer.WriteObjectStart();
            if (reference is PortableExecutableReference peReference)
            {
                switch (peReference.GetMetadata())
                {
                    case AssemblyMetadata assemblyMetadata:
                        {
                            var modules = assemblyMetadata.GetModules();
                            writeModuleMetadata(modules[0]);
                            writer.WriteKey("secondaryModules");
                            writer.WriteArrayStart();
                            for (var i = 1; i < modules.Length; i++)
                            {
                                writer.WriteObjectStart();
                                writeModuleMetadata(modules[i]);
                                writer.WriteObjectEnd();
                            }
                            writer.WriteArrayEnd();
                        }
                        break;
                    case ModuleMetadata m:
                        writeModuleMetadata(m);
                        break;
                    default:
                        throw new NotSupportedException();
                }

                writer.WriteKey("properties");
                writeMetadataReferenceProperties(writer, reference.Properties);

                void writeModuleMetadata(ModuleMetadata moduleMetadata)
                {
                    // The path of a reference, unlike the path of a file, does not contribute to the output
                    // of the compilation. Only the MVID, name and version contribute here hence the file path
                    // is deliberately omitted here.
                    var peReader = moduleMetadata.GetMetadataReader();
                    if (peReader.IsAssembly)
                    {
                        var assemblyDef = peReader.GetAssemblyDefinition();
                        writer.Write("name", peReader.GetString(assemblyDef.Name));
                        writer.Write("version", assemblyDef.Version.ToString());
                        WriteByteArrayValue(writer, "publicKey", peReader.GetBlobBytes(assemblyDef.PublicKey).AsSpan());
                    }
                    else
                    {
                        var moduleDef = peReader.GetModuleDefinition();
                        writer.Write("name", peReader.GetString(moduleDef.Name));
                    }

                    writer.Write("mvid", GetGuidValue(moduleMetadata.GetModuleVersionId()));
                }
            }
            else if (reference is CompilationReference compilationReference)
            {
                var compilation = compilationReference.Compilation;
                var builder = compilation.Options.CreateDeterministicKeyBuilder();
                builder.WriteCompilation(
                    writer,
                    compilation.Options,
                    compilation.SyntaxTrees.SelectAsArray(x => SyntaxTreeKey.Create(x)),
                    compilation.References.AsImmutable(),
                    deterministicKeyOptions,
                    cancellationToken);
            }
            else
            {
                throw new NotSupportedException();
            }
            writer.WriteObjectEnd();

            static void writeMetadataReferenceProperties(JsonWriter writer, MetadataReferenceProperties properties)
            {
                writer.WriteObjectStart();
                writer.Write("kind", properties.Kind);
                writer.Write("embedInteropTypes", properties.EmbedInteropTypes);
                writer.WriteKey("aliases");
                writer.WriteArrayStart();
                foreach (var alias in properties.Aliases)
                {
                    writer.Write(alias);
                }
                writer.WriteArrayEnd();
                writer.WriteObjectEnd();
            }
        }

        private void WriteEmitOptions(JsonWriter writer, EmitOptions? options)
        {
            writer.WriteObjectStart();
            if (options is null)
            {
                writer.WriteObjectEnd();
                return;
            }

            writer.Write("emitMetadataOnly", options.EmitMetadataOnly);
            writer.Write("tolerateErrors", options.TolerateErrors);
            writer.Write("includePrivateMembers", options.IncludePrivateMembers);
            writer.WriteKey("instrumentationKinds");
            writer.WriteArrayStart();
            foreach (var kind in options.InstrumentationKinds)
            {
                writer.Write(kind);
            }
            writer.WriteArrayEnd();

            writeSubsystemVersion(writer, options.SubsystemVersion);
            writer.Write("fileAlignment", options.FileAlignment);
            writer.Write("highEntropyVirtualAddressSpace", options.HighEntropyVirtualAddressSpace);
            writer.Write("baseAddress", options.BaseAddress.ToString());
            writer.Write("debugInformationFormat", options.DebugInformationFormat);
            writer.Write("outputNameOverride", options.OutputNameOverride);
            writer.Write("pdbFilePath", options.PdbFilePath);
            writer.Write("pdbChecksumAlgorithm", options.PdbChecksumAlgorithm.Name);
            writer.Write("runtimeMetadataVersion", options.RuntimeMetadataVersion);
            writer.Write("defaultSourceFileEncoding", options.DefaultSourceFileEncoding?.CodePage);
            writer.Write("fallbackSourceFileEncoding", options.FallbackSourceFileEncoding?.CodePage);

            writer.WriteObjectEnd();

            static void writeSubsystemVersion(JsonWriter writer, SubsystemVersion version)
            {
                writer.WriteKey("subsystemVersion");
                writer.WriteObjectStart();
                writer.Write("major", version.Major);
                writer.Write("minor", version.Minor);
                writer.WriteObjectEnd();
            }
        }

        private void WriteCompilationOptions(JsonWriter writer, CompilationOptions options)
        {
            writer.WriteObjectStart();
            WriteCompilationOptionsCore(writer, options);
            writer.WriteObjectEnd();
        }

        protected virtual void WriteCompilationOptionsCore(JsonWriter writer, CompilationOptions options)
        {
            // CompilationOption values
            writer.Write("outputKind", options.OutputKind);
            writer.Write("moduleName", options.ModuleName);
            writer.Write("scriptClassName", options.ScriptClassName);
            writer.Write("mainTypeName", options.MainTypeName);
            WriteByteArrayValue(writer, "cryptoPublicKey", options.CryptoPublicKey);
            writer.Write("cryptoKeyFile", options.CryptoKeyFile);
            writer.Write("delaySign", options.DelaySign);
            writer.Write("publicSign", options.PublicSign);
            writer.Write("checkOverflow", options.CheckOverflow);
            writer.Write("platform", options.Platform);
            writer.Write("optimizationLevel", options.OptimizationLevel);
            writer.Write("generalDiagnosticOption", options.GeneralDiagnosticOption);
            writer.Write("warningLevel", options.WarningLevel);
            writer.Write("deterministic", options.Deterministic);
            writer.Write("debugPlusMode", options.DebugPlusMode);
            writer.Write("referencesSupersedeLowerVersions", options.ReferencesSupersedeLowerVersions);
            writer.Write("reportSuppressedDiagnostics", options.ReportSuppressedDiagnostics);
            writer.Write("nullableContextOptions", options.NullableContextOptions);

            writer.WriteKey("specificDiagnosticOptions");
            writer.WriteArrayStart();
            foreach (var key in options.SpecificDiagnosticOptions.Keys.OrderBy(StringComparer.Ordinal))
            {
                writer.WriteObjectStart();
                writer.Write(key, options.SpecificDiagnosticOptions[key]);
                writer.WriteObjectEnd();
            }
            writer.WriteArrayEnd();

            if (options.Deterministic)
            {
                writer.Write("deterministic", true);
                writer.WriteNull("localtime");
            }
            else
            {
                writer.Write("deterministic", false);
                writer.Write("localtime", options.CurrentLocalTime.ToString(CultureInfo.InvariantCulture));
            }

            // Values which do not impact build success / failure
            // - ConcurrentBuild
            // - MetadataImportOptions:
            // - Options.Features: deprecated
            // 

            // Not really options but they can impact compilation so we record the types. For the majority
            // of compilations this is roughly the equivalent of recording the compiler version but it 
            // could differ when customers host the compiler via the API.
            writer.WriteKey("extensions");
            writer.WriteObjectStart();

            WriteType(writer, "syntaxTreeOptionsProvider", options.SyntaxTreeOptionsProvider?.GetType());
            WriteType(writer, "metadataReferenceResolver", options.MetadataReferenceResolver?.GetType());
            WriteType(writer, "xmlReferenceResolver", options.XmlReferenceResolver?.GetType());
            WriteType(writer, "sourceReferenceResolver", options.SourceReferenceResolver?.GetType());
            WriteType(writer, "strongNameProvider", options.StrongNameProvider?.GetType());
            WriteType(writer, "assemblyIdentityComparer", options.AssemblyIdentityComparer?.GetType());
            writer.WriteObjectEnd();
        }

        protected void WriteParseOptions(JsonWriter writer, ParseOptions parseOptions)
        {
            writer.WriteObjectStart();
            WriteParseOptionsCore(writer, parseOptions);
            writer.WriteObjectEnd();
        }

        protected virtual void WriteParseOptionsCore(JsonWriter writer, ParseOptions parseOptions)
        {
            writer.Write("kind", parseOptions.Kind);
            writer.Write("specifiedKind", parseOptions.SpecifiedKind);
            writer.Write("documentationMode", parseOptions.DocumentationMode);
            writer.Write("language", parseOptions.Language);

            writer.WriteKey("features");
            var features = parseOptions.Features;
            if (features.Count > 0)
            {
                writer.WriteObjectStart();
                foreach (var key in features.Keys.OrderBy(StringComparer.Ordinal))
                {
                    writer.Write(key, features[key]);
                }
                writer.WriteObjectEnd();
            }
            else
            {
                writer.WriteNull();
            }

            // Skipped values
            // - Errors: not sure if we need that in the key file or not
            // - PreprocessorSymbolNames: handled at the language specific level
        }
    }
}
