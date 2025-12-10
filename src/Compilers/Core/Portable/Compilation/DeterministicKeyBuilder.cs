// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The string returned from this function represents the inputs to the compiler which impact determinism.  It is 
    /// meant to be inline with the specification here:
    /// 
    ///     - https://github.com/dotnet/roslyn/blob/main/docs/compilers/Deterministic%20Inputs.md
    /// 
    /// Options which can cause compilation failure, but doesn't impact the result of a successful
    /// compilation should be included. That is because it is interesting to describe error states
    /// not just success states. Think about caching build failures as well as build successes.
    ///
    /// When an option is omitted, say if there is no value for a public crypto key, we should emit
    /// the property with a null value vs. omitting the property. Either approach would produce 
    /// correct results the preference is to be declarative that an option is omitted.
    /// </summary>
    internal abstract class DeterministicKeyBuilder
    {
        protected DeterministicKeyBuilder()
        {
        }

        protected void WriteFilePath(
            JsonWriter writer,
            string propertyName,
            string? filePath,
            ImmutableArray<KeyValuePair<string, string>> pathMap,
            DeterministicKeyOptions options)
        {
            if ((options & DeterministicKeyOptions.IgnorePaths) != 0)
            {
                filePath = Path.GetFileName(filePath);
            }
            else if (filePath is not null)
            {
                filePath = PathUtilities.NormalizePathPrefix(filePath, pathMap);
            }

            writer.Write(propertyName, filePath);
        }

        internal static string EncodeByteArrayValue(ReadOnlySpan<byte> value)
        {
            var builder = PooledStringBuilder.GetInstance();
            EncodeByteArrayValue(value, builder.Builder);
            return builder.ToStringAndFree();
        }

        internal static void EncodeByteArrayValue(ReadOnlySpan<byte> value, StringBuilder builder)
        {
            foreach (var b in value)
            {
                builder.Append(b.ToString("x"));
            }
        }

        protected static void WriteByteArrayValue(JsonWriter writer, string name, ReadOnlySpan<byte> value) =>
            writer.Write(name, EncodeByteArrayValue(value));

        protected void WriteResourceContent(JsonWriter writer, ResourceDescription resource, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (resource.IsEmbedded)
            {
                writer.WriteObjectStart();
                using var stream = resource.DataProvider();
                using var hashAlgorithm = System.Security.Cryptography.SHA256.Create();
                var hash = hashAlgorithm.ComputeHash(stream);
                WriteByteArrayValue(writer, "checksum", hash.AsSpan());
                writer.WriteObjectEnd();
            }
            else
            {
                writer.WriteNull();
            }
        }

        protected static void WriteVersion(JsonWriter writer, string key, Version version)
        {
            writer.WriteKey(key);
            writer.WriteObjectStart();
            writer.Write("major", version.Major);
            writer.Write("minor", version.Minor);
            writer.Write("build", version.Build);
            writer.Write("revision", version.Revision);
            writer.WriteObjectEnd();
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
            writer.Write("mvid", GetGuidValue(type.Assembly.ManifestModule.ModuleVersionId));
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
            ImmutableArray<byte> publicKey,
            ImmutableArray<AdditionalText> additionalTexts,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<ISourceGenerator> generators,
            ImmutableArray<KeyValuePair<string, string>> pathMap,
            EmitOptions? emitOptions,
            SourceText? sourceLinkText,
            string? ruleSetFilePath,
            ImmutableArray<ResourceDescription> resources,
            DeterministicKeyOptions options,
            CancellationToken cancellationToken)
        {
            Debug.Assert(!syntaxTrees.IsDefault);
            Debug.Assert(!references.IsDefault);
            Debug.Assert(!publicKey.IsDefault);

            additionalTexts = additionalTexts.NullToEmpty();
            analyzers = analyzers.NullToEmpty();
            generators = generators.NullToEmpty();
            resources = resources.NullToEmpty();

            var (writer, builder) = CreateWriter();

            writer.WriteObjectStart();

            writer.WriteKey("compilation");
            WriteCompilation(writer, compilationOptions, syntaxTrees, references, publicKey, pathMap, ruleSetFilePath, options, cancellationToken);
            writer.WriteKey("additionalTexts");
            writeAdditionalTexts();
            writer.WriteKey("analyzers");
            writeAnalyzers();
            writer.WriteKey("generators");
            writeGenerators();
            writer.WriteKey("emitOptions");
            WriteEmitOptions(writer, emitOptions, pathMap, sourceLinkText, options);
            writer.WriteKey("resources");
            writeResources();

            writer.WriteObjectEnd();

            return builder.ToStringAndFree();

            void writeAdditionalTexts()
            {
                writer.WriteArrayStart();
                foreach (var additionalText in additionalTexts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    writer.WriteObjectStart();
                    WriteFilePath(writer, "fileName", additionalText.Path, pathMap, options);
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
                    WriteType(writer, generator.GetGeneratorType());
                }
                writer.WriteArrayEnd();
            }

            void writeResources()
            {
                writer.WriteArrayStart();
                foreach (var resource in resources)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    writer.WriteObjectStart();
                    writer.Write("resourceName", resource.ResourceName);
                    writer.Write("fileName", resource.FileName);
                    writer.Write("isPublic", resource.IsPublic);
                    writer.Write("isEmbedded", resource.IsEmbedded);
                    writer.WriteKey("content");
                    WriteResourceContent(writer, resource, cancellationToken);
                    writer.WriteObjectEnd();
                }
                writer.WriteArrayEnd();
            }
        }

        internal static string GetGuidValue(in Guid guid) => guid.ToString("D");

        private void WriteCompilation(
            JsonWriter writer,
            CompilationOptions compilationOptions,
            ImmutableArray<SyntaxTreeKey> syntaxTrees,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<byte> publicKey,
            ImmutableArray<KeyValuePair<string, string>> pathMap,
            string? ruleSetFilePath,
            DeterministicKeyOptions options,
            CancellationToken cancellationToken)
        {
            writer.WriteObjectStart();
            writeToolsVersions();

            WriteByteArrayValue(writer, "publicKey", publicKey.AsSpan());
            writer.WriteKey("options");
            WriteCompilationOptions(writer, compilationOptions, ruleSetFilePath, pathMap, options);

            writer.WriteKey("syntaxTrees");
            writer.WriteArrayStart();
            foreach (var syntaxTree in syntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WriteSyntaxTree(writer, syntaxTree, pathMap, options, cancellationToken);
            }
            writer.WriteArrayEnd();

            writer.WriteKey("references");
            writer.WriteArrayStart();
            foreach (var reference in references)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WriteMetadataReference(writer, reference, pathMap, options, cancellationToken);
            }
            writer.WriteArrayEnd();
            writer.WriteObjectEnd();

            void writeToolsVersions()
            {
                writer.WriteKey("toolsVersions");
                writer.WriteObjectStart();
                if ((options & DeterministicKeyOptions.IgnoreToolVersions) == 0)
                {
                    var compilerVersion = typeof(Compilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                    writer.Write("compilerVersion", compilerVersion);

                    var runtimeVersion = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                    writer.Write("runtimeVersion", runtimeVersion);

                    writer.Write("frameworkDescription", RuntimeInformation.FrameworkDescription);
                    writer.Write("osDescription", RuntimeInformation.OSDescription);
                }

                writer.WriteObjectEnd();
            }
        }

        private void WriteSyntaxTree(
            JsonWriter writer,
            SyntaxTreeKey syntaxTree,
            ImmutableArray<KeyValuePair<string, string>> pathMap,
            DeterministicKeyOptions options,
            CancellationToken cancellationToken)
        {
            writer.WriteObjectStart();
            WriteFilePath(writer, "fileName", syntaxTree.FilePath, pathMap, options);
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
                writer.WriteNull();
                return;
            }

            writer.WriteObjectStart();
            WriteByteArrayValue(writer, "checksum", sourceText.GetChecksum().AsSpan());
            writer.Write("checksumAlgorithm", sourceText.ChecksumAlgorithm);
            writer.Write("encodingName", sourceText.Encoding?.EncodingName);
            writer.WriteObjectEnd();
        }

        internal void WriteMetadataReference(
            JsonWriter writer,
            MetadataReference reference,
            ImmutableArray<KeyValuePair<string, string>> pathMap,
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
                    case var m:
                        throw ExceptionUtilities.UnexpectedValue(m);
                }

                writer.WriteKey("properties");
                writeMetadataReferenceProperties(writer, reference.Properties);

            }
            else if (reference is CompilationReference compilationReference)
            {
                writer.WriteKey("compilation");
                var compilation = compilationReference.Compilation;
                var builder = compilation.Options.CreateDeterministicKeyBuilder();
                builder.WriteCompilation(
                    writer,
                    compilation.Options,
                    compilation.SyntaxTrees.SelectAsArray(static x => SyntaxTreeKey.Create(x)),
                    compilation.References.AsImmutable(),
                    compilation.Assembly.Identity.PublicKey,
                    pathMap,
                    ruleSetFilePath: null,
                    deterministicKeyOptions,
                    cancellationToken);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(reference);
            }

            writer.WriteObjectEnd();

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
                    WriteVersion(writer, "version", assemblyDef.Version);
                    WriteByteArrayValue(writer, "publicKey", peReader.GetBlobBytes(assemblyDef.PublicKey).AsSpan());
                }
                else
                {
                    var moduleDef = peReader.GetModuleDefinition();
                    writer.Write("name", peReader.GetString(moduleDef.Name));
                }

                writer.Write("mvid", GetGuidValue(moduleMetadata.GetModuleVersionId()));
            }

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

        private void WriteEmitOptions(
            JsonWriter writer,
            EmitOptions? options,
            ImmutableArray<KeyValuePair<string, string>> pathMap,
            SourceText? sourceLinkText,
            DeterministicKeyOptions deterministicKeyOptions)
        {
            if (options is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteObjectStart();
            writer.Write("emitMetadataOnly", options.EmitMetadataOnly);
            writer.Write("tolerateErrors", options.TolerateErrors);
            writer.Write("includePrivateMembers", options.IncludePrivateMembers);
            writer.WriteKey("instrumentationKinds");
            writer.WriteArrayStart();
            if (!options.InstrumentationKinds.IsDefault)
            {
                foreach (var kind in options.InstrumentationKinds)
                {
                    writer.Write(kind);
                }
            }
            writer.WriteArrayEnd();

            writeSubsystemVersion(writer, options.SubsystemVersion);
            writer.Write("fileAlignment", options.FileAlignment);
            writer.Write("highEntropyVirtualAddressSpace", options.HighEntropyVirtualAddressSpace);
            writer.WriteInvariant("baseAddress", options.BaseAddress);
            writer.Write("debugInformationFormat", options.DebugInformationFormat);
            writer.Write("outputNameOverride", options.OutputNameOverride);
            WriteFilePath(writer, "pdbFilePath", options.PdbFilePath, pathMap, deterministicKeyOptions);
            writer.Write("pdbChecksumAlgorithm", options.PdbChecksumAlgorithm.Name);
            writer.Write("runtimeMetadataVersion", options.RuntimeMetadataVersion);
            writer.Write("defaultSourceFileEncoding", options.DefaultSourceFileEncoding?.CodePage);
            writer.Write("fallbackSourceFileEncoding", options.FallbackSourceFileEncoding?.CodePage);
            writer.WriteKey("sourceLink");
            WriteSourceText(writer, sourceLinkText);
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

        private void WriteCompilationOptions(
            JsonWriter writer,
            CompilationOptions options,
            string? ruleSetFilePath,
            ImmutableArray<KeyValuePair<string, string>> pathMap,
            DeterministicKeyOptions deterministicKeyOptions)
        {
            writer.WriteObjectStart();
            WriteCompilationOptionsCore(writer, options);
            WriteFilePath(writer, "ruleSetPath", ruleSetFilePath, pathMap, deterministicKeyOptions);
            writer.WriteObjectEnd();
        }

        protected virtual void WriteCompilationOptionsCore(JsonWriter writer, CompilationOptions options)
        {
            // CompilationOption values
            writer.Write("outputKind", options.OutputKind);
            writer.Write("moduleName", options.ModuleName);
            writer.Write("scriptClassName", options.ScriptClassName);
            writer.Write("mainTypeName", options.MainTypeName);
            WriteByteArrayValue(writer, "cryptoPublicKey", options.CryptoPublicKey.AsSpan());
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
                writer.WriteInvariant("localtime", options.CurrentLocalTime);

                // When using /deterministic- the compiler will *always* emit different binaries hence the 
                // key we generate here also must be different. We cannot depend on the `localtime` property
                // to provide this as the same compilation can occur on different machines at the same 
                // time. Force the issue here.
                writer.Write("nondeterministicMvid", GetGuidValue(Guid.NewGuid()));
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
            writer.WriteObjectStart();
            foreach (var key in features.Keys.OrderBy(StringComparer.Ordinal))
            {
                writer.Write(key, features[key]);
            }
            writer.WriteObjectEnd();

            // Skipped values
            // - Errors: not sure if we need that in the key file or not
            // - PreprocessorSymbolNames: handled at the language specific level
        }
    }
}
