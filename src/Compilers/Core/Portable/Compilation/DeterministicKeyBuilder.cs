// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
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
    /// Issue #8193 tracks filling this out to the full specification. 
    /// 
    ///     https://github.com/dotnet/roslyn/issues/8193
    /// </summary>
    /// 
    /// <remarks>
    /// Options which can cause compilation failure, but doesn't impact the result of a successful
    /// compilation should be included. That is because it is interesting to describe error states
    /// not just success states. Think about caching build failures as well as build successes
    ///
    /// API considerations
    /// - Path dependent
    /// - throw when using a non-deterministic compilation
    /// </remarks>
    internal abstract class DeterministicKeyBuilder
    {
        internal StringBuilder Builder { get; } = new StringBuilder();
        internal StringWriter StringWriter { get; }
        internal JsonWriter Writer { get; }
        internal DeterministicKeyOptions Options { get; }

        public DeterministicKeyBuilder(DeterministicKeyOptions options)
        {
            StringWriter = new StringWriter(Builder);
            Writer = new JsonWriter(StringWriter);
            Options = options;
        }

        internal string GetKey() => Builder.ToString();

        internal void Reset() => Builder.Length = 0;

        protected void WriteFileName(string name, string? filePath)
        {
            if (0 != (Options & DeterministicKeyOptions.IgnorePaths))
            {
                filePath = Path.GetFileName(filePath);
            }

            Writer.Write(name, filePath);
        }

        protected void WriteByteArray(string name, ImmutableArray<byte> value)
        {
            if (!value.IsDefault)
            {
                WriteByteArray(name, value.AsSpan());
            }
        }

        protected void WriteByteArray(string name, ReadOnlySpan<byte> value)
        {
            if (value.Length > 0)
            {
                var builder = new StringBuilder();
                foreach (var b in value)
                {
                    builder.Append(b.ToString("x"));
                }
                Writer.Write(name, builder.ToString());
            }
        }

        internal void WriteCompilation(Compilation compilation)
        {
            WriteCompilationCore(compilation);
        }

        internal void WriteCompilation(
            Compilation compilation,
            ImmutableArray<AdditionalText> additionalTexts,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<ISourceGenerator> generators)
        {
            Writer.WriteObjectStart();
            Writer.WriteKey("compilation");
            WriteCompilationCore(compilation);
            Writer.WriteKey("additionalTexts");
            writeAdditionalTexts();
            Writer.WriteKey("analyzers");
            writeAnalyzers();
            Writer.WriteKey("generators");
            writeGenerators();
            Writer.WriteObjectEnd();

            void writeAdditionalTexts()
            {
                Writer.WriteArrayStart();
                foreach (var additionalText in additionalTexts)
                {
                    Writer.WriteObjectStart();
                    WriteFileName("fileName", additionalText.Path);
                    Writer.WriteKey("text");
                    WriteSourceText(additionalText.GetText());
                    Writer.WriteObjectEnd();
                }
                Writer.WriteArrayEnd();
            }

            void writeAnalyzers()
            {
                Writer.WriteArrayStart();
                foreach (var analyzer in analyzers)
                {
                    writeType(analyzer.GetType());
                }
                Writer.WriteArrayEnd();
            }

            void writeGenerators()
            {
                Writer.WriteArrayStart();
                foreach (var generator in generators)
                {
                    writeType(generator.GetType());
                }
                Writer.WriteArrayEnd();
            }

            void writeType(Type type)
            {
                Writer.WriteObjectStart();
                Writer.Write("fullName", type.FullName);
                // Note that the file path to the assembly is deliberately not included here. The file path
                // of the assembly does not contribute to the output of the program.
                Writer.Write("assemblyName", type.Assembly.FullName);
                Writer.WriteObjectEnd();
            }
        }

        private void WriteCompilationCore(Compilation compilation)
        {
            Writer.WriteObjectStart();
            if (0 == (Options & DeterministicKeyOptions.IgnoreToolVersions))
            {
                Writer.WriteKey("toolsVersions");
                writeToolsVersions();
            }

            Writer.WriteKey("options");
            WriteCompilationOptions(compilation.Options);

            Writer.WriteKey("syntaxTrees");
            Writer.WriteArrayStart();
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                WriteSyntaxTree(syntaxTree);
            }
            Writer.WriteArrayEnd();

            Writer.WriteKey("references");
            Writer.WriteArrayStart();
            foreach (var reference in compilation.References)
            {
                WriteMetadataReference(reference);
            }
            Writer.WriteArrayEnd();
            Writer.WriteObjectEnd();

            void writeToolsVersions()
            {
                Writer.WriteObjectStart();

                var compilerVersion = typeof(Compilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                Writer.Write("compilerVersion", compilerVersion);

                var runtimeVersion = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                Writer.Write("runtimeVersion", runtimeVersion);

                Writer.Write("framework", RuntimeInformation.FrameworkDescription);
                Writer.Write("os", RuntimeInformation.OSDescription);

                Writer.WriteObjectEnd();
            }
        }

        internal void WriteSyntaxTree(SyntaxTree syntaxTree)
        {
            Writer.WriteObjectStart();
            WriteFileName("fileName", syntaxTree.FilePath);
            Writer.WriteKey("text");
            WriteSourceText(syntaxTree.GetText());
            Writer.WriteKey("parseOptions");
            WriteParseOptions(syntaxTree.Options);
            Writer.WriteObjectEnd();
        }

        internal void WriteSourceText(SourceText? sourceText)
        {
            if (sourceText is null)
            {
                return;
            }

            Writer.WriteObjectStart();
            WriteByteArray("checksum", sourceText.GetChecksum());
            Writer.Write("checksumAlgorithm", sourceText.ChecksumAlgorithm);
            Writer.Write("encoding", sourceText.Encoding?.EncodingName);
            Writer.WriteObjectEnd();
        }

        internal void WriteMetadataReference(MetadataReference reference)
        {
            Writer.WriteObjectStart();
            if (reference is PortableExecutableReference peReference)
            {
                ModuleMetadata moduleMetadata;
                switch (peReference.GetMetadata())
                {
                    case AssemblyMetadata assemblyMetadata:
                        {
                            if (assemblyMetadata.GetModules() is { Length: 1 } modules)
                            {
                                moduleMetadata = modules[0];
                            }
                            else
                            {
                                throw new InvalidOperationException();
                            }
                        }
                        break;
                    case ModuleMetadata m:
                        moduleMetadata = m;
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                // The path of a reference, unlike the path of a file, does not contribute to the output
                // of the copmilation. Only the MVID, name and version contribute here hence the file path
                // is deliberately omitted here.
                if (moduleMetadata.GetMetadataReader() is { IsAssembly: true } peReader)
                {
                    var assemblyDef = peReader.GetAssemblyDefinition();
                    Writer.Write("name", peReader.GetString(assemblyDef.Name));
                    Writer.Write("version", assemblyDef.Version.ToString());
                    WriteByteArray("publicKey", peReader.GetBlobBytes(assemblyDef.PublicKey).AsSpan());
                }

                Writer.Write("mvid", moduleMetadata.GetModuleVersionId().ToString());
                Writer.WriteKey("properties");
                WriteMetadataReferenceProperties(reference.Properties);
            }
            else
            {
                throw new InvalidOperationException();
            }
            Writer.WriteObjectEnd();
        }

        internal void WriteEmitOptions(EmitOptions options)
        {
            Writer.WriteObjectStart();
            Writer.Write("emitMetadataOnly", options.EmitMetadataOnly);
            Writer.Write("tolerateErrors", options.TolerateErrors);
            Writer.Write("includePrivateMembers", options.IncludePrivateMembers);
            if (options.InstrumentationKinds.Length > 0)
            {
                Writer.WriteArrayStart();
                foreach (var kind in options.InstrumentationKinds)
                {
                    Writer.Write(kind);
                }
                Writer.WriteArrayEnd();
            }

            writeSubsystemVersion(Writer, options.SubsystemVersion);
            Writer.Write("fileAlignment", options.FileAlignment);
            Writer.Write("highEntropyVirtualAddressSpace", options.HighEntropyVirtualAddressSpace);
            Writer.Write("baseAddress", options.BaseAddress.ToString());
            Writer.Write("debugInformationFormat", options.DebugInformationFormat);
            Writer.Write("outputNameOverride", options.OutputNameOverride);
            Writer.Write("pdbFilePath", options.PdbFilePath);
            Writer.Write("pdbChecksumAlgorithm", options.PdbChecksumAlgorithm.Name);
            Writer.Write("runtimeMetadataVersion", options.RuntimeMetadataVersion);
            Writer.Write("defaultSourceFileEncoding", options.DefaultSourceFileEncoding?.CodePage);
            Writer.Write("fallbackSourceFileEncoding", options.FallbackSourceFileEncoding?.CodePage);

            Writer.WriteObjectEnd();

            static void writeSubsystemVersion(JsonWriter writer, SubsystemVersion version)
            {
                writer.WriteKey("subsystemVersion");
                writer.WriteObjectStart();
                writer.Write("major", version.Major);
                writer.Write("minor", version.Minor);
                writer.WriteObjectEnd();
            }
        }

        internal void WriteMetadataReferenceProperties(MetadataReferenceProperties properties)
        {
            Writer.WriteObjectStart();
            Writer.Write("kind", properties.Kind);
            Writer.Write("embedInteropTypes", properties.EmbedInteropTypes);
            if (properties.Aliases is { Length: > 0 } aliases)
            {
                Writer.WriteKey("aliases");
                Writer.WriteArrayStart();
                foreach (var alias in aliases)
                {
                    Writer.Write(alias);
                }
                Writer.WriteArrayEnd();
            }
            Writer.WriteObjectEnd();
        }

        internal void WriteCompilationOptions(CompilationOptions options)
        {
            Writer.WriteObjectStart();
            WriteCompilationOptionsCore(options);
            Writer.WriteObjectEnd();
        }

        protected virtual void WriteCompilationOptionsCore(CompilationOptions options)
        {
            // CompilationOption values
            Writer.Write("outputKind", options.OutputKind);
            Writer.Write("moduleName", options.ModuleName);
            Writer.Write("scriptClassName", options.ScriptClassName);
            Writer.Write("mainTypeName", options.MainTypeName);
            WriteByteArray("cryptoPublicKey", options.CryptoPublicKey);
            Writer.Write("cryptoKeyFile", options.CryptoKeyFile);
            Writer.Write("delaySign", options.DelaySign);
            Writer.Write("publicSign", options.PublicSign);
            Writer.Write("checkOverflow", options.CheckOverflow);
            Writer.Write("platform", options.Platform);
            Writer.Write("optimizationLevel", options.OptimizationLevel);
            Writer.Write("generalDiagnosticOption", options.GeneralDiagnosticOption);
            Writer.Write("warningLevel", options.WarningLevel);
            Writer.Write("deterministic", options.Deterministic);
            Writer.Write("debugPlusMode", options.DebugPlusMode);
            Writer.Write("referencesSupersedeLowerVersions", options.ReferencesSupersedeLowerVersions);
            Writer.Write("reportSuppressedDiagnostics", options.ReportSuppressedDiagnostics);
            Writer.Write("nullableContextOptions", options.NullableContextOptions);

            if (options.SpecificDiagnosticOptions.Count > 0)
            {
                Writer.WriteKey("specificDiagnosticOptions");
                Writer.WriteArrayStart();
                foreach (var kvp in options.SpecificDiagnosticOptions)
                {
                    Writer.WriteObjectStart();
                    Writer.Write(kvp.Key, kvp.Value);
                    Writer.WriteObjectEnd();
                }
                Writer.WriteArrayEnd();
            }

            // Skipped values
            // - ConcurrentBuild
            // - CurrentLocalTime: this is only valid when Determinism is false at which point the key isn't
            //   valid
            // - MetadataImportOptions: does not impact compilation success or failure
            // - Options.Features: deprecated
            // 
            // Not really options, implementation details that can't really be expressed in a key
            // - SyntaxTreeOptionsProvider 
            // - MetadataReferenceResolver 
            // - XmlReferenceResolver
            // - SourceReferenceResolver
            // - StrongNameProvider
            //
            // Think harder about 
            // - AssemblyIdentityComparer
        }

        internal void WriteParseOptions(ParseOptions parseOptions)
        {
            Writer.WriteObjectStart();
            WriteParseOptionsCore(parseOptions);
            Writer.WriteObjectEnd();
        }

        protected virtual void WriteParseOptionsCore(ParseOptions parseOptions)
        {
            Writer.Write("kind", parseOptions.Kind);
            Writer.Write("specifiedKind", parseOptions.SpecifiedKind);
            Writer.Write("documentationMode", parseOptions.DocumentationMode);
            Writer.Write("language", parseOptions.Language);

            var features = parseOptions.Features;
            if (features.Count > 0)
            {
                Writer.WriteKey("features");
                Writer.WriteArrayStart();
                foreach (var kvp in features)
                {
                    Writer.WriteObjectStart();
                    Writer.Write(kvp.Key, kvp.Value);
                    Writer.WriteObjectEnd();
                }
                Writer.WriteArrayEnd();
            }
            // Skipped values
            // - Errors: not sure if we need that in the key file or not
            // - PreprocessorSymbolNames: handled at the language specific level
        }
    }
}
