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

        protected void WriteEnum<T>(T value) where T : struct, Enum
        {
            Writer.Write(value.ToString());
        }

        protected void WriteEnum<T>(string name, T value) where T : struct, Enum
        {
            Writer.Write(name, value.ToString());
        }

        protected void WriteInt(string name, int? value)
        {
            if (value is { } i)
            {
                WriteInt(name, i);
            }
        }

        protected void WriteInt(string name, int value)
        {
            Writer.Write(name, value);
        }

        protected void WriteUlong(string name, ulong value)
        {
            Writer.Write(name, value.ToString());
        }

        protected void WriteBool(string name, bool? value)
        {
            if (value is bool b)
            {
                Writer.Write(name, b);
            }
        }

        protected void WriteString(string name, string? value)
        {
            // Skip null values for brevity. The lack of the value is just as significant in the 
            // key and overall makes it more readable
            if (value is object)
            {
                Writer.Write(name, value);
            }
        }

        protected void WriteFileName(string name, string? filePath)
        {
            if (0 != (Options & DeterministicKeyOptions.IgnorePaths))
            {
                filePath = Path.GetFileName(filePath);
            }

            WriteString(name, filePath);
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
                WriteString("fullName", type.FullName);
                // Note that the file path to the assembly is deliberately not included here. The file path
                // of the assembly does not contribute to the output of the program.
                WriteString("assemblyName", type.Assembly.FullName);
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
                WriteString("compilerVersion", compilerVersion);

                var runtimeVersion = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                WriteString("runtimeVersion", runtimeVersion);

                WriteString("framework", RuntimeInformation.FrameworkDescription);
                WriteString("os", RuntimeInformation.OSDescription);

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
            WriteEnum("checksumAlgorithm", sourceText.ChecksumAlgorithm);
            WriteString("encoding", sourceText.Encoding?.EncodingName);
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
                    WriteString("name", peReader.GetString(assemblyDef.Name));
                    WriteString("version", assemblyDef.Version.ToString());
                    WriteByteArray("publicKey", peReader.GetBlobBytes(assemblyDef.PublicKey).AsSpan());
                }

                WriteString("mvid", moduleMetadata.GetModuleVersionId().ToString());
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
            WriteBool("emitMetadataOnly", options.EmitMetadataOnly);
            WriteBool("tolerateErrors", options.TolerateErrors);
            WriteBool("includePrivateMembers", options.IncludePrivateMembers);
            if (options.InstrumentationKinds.Length > 0)
            {
                Writer.WriteArrayStart();
                foreach (var kind in options.InstrumentationKinds)
                {
                    WriteEnum(kind);
                }
                Writer.WriteArrayEnd();
            }

            WriteSubsystemVersion(Writer, options.SubsystemVersion);
            WriteInt("fileAlignment", options.FileAlignment);
            WriteBool("highEntropyVirtualAddressSpace", options.HighEntropyVirtualAddressSpace);
            WriteUlong("baseAddress", options.BaseAddress);
            WriteEnum("debugInformationFormat", options.DebugInformationFormat);
            WriteString("outputNameOverride", options.OutputNameOverride);
            WriteString("pdbFilePath", options.PdbFilePath);
            WriteString("pdbChecksumAlgorithm", options.PdbChecksumAlgorithm.Name);
            WriteString("runtimeMetadataVersion", options.RuntimeMetadataVersion);
            WriteInt("defaultSourceFileEncoding", options.DefaultSourceFileEncoding?.CodePage);
            WriteInt("fallbackSourceFileEncoding", options.FallbackSourceFileEncoding?.CodePage);

            Writer.WriteObjectEnd();

            static void WriteSubsystemVersion(JsonWriter writer, SubsystemVersion version)
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
            WriteEnum("kind", properties.Kind);
            WriteBool("embedInteropTypes", properties.EmbedInteropTypes);
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
            WriteEnum("outputKind", options.OutputKind);
            WriteString("moduleName", options.ModuleName);
            WriteString("scriptClassName", options.ScriptClassName);
            WriteString("mainTypeName", options.MainTypeName);
            WriteByteArray("cryptoPublicKey", options.CryptoPublicKey);
            WriteString("cryptoKeyFile", options.CryptoKeyFile);
            WriteBool("delaySign", options.DelaySign);
            WriteBool("publicSign", options.PublicSign);
            WriteBool("checkOverflow", options.CheckOverflow);
            WriteEnum("platform", options.Platform);
            WriteEnum("optimizationLevel", options.OptimizationLevel);
            WriteEnum("generalDiagnosticOption", options.GeneralDiagnosticOption);
            WriteInt("warningLevel", options.WarningLevel);
            WriteBool("deterministic", options.Deterministic);
            WriteBool("debugPlusMode", options.DebugPlusMode);
            WriteBool("referencesSupersedeLowerVersions", options.ReferencesSupersedeLowerVersions);
            WriteBool("reportSuppressedDiagnostics", options.ReportSuppressedDiagnostics);
            WriteEnum("nullableContextOptions", options.NullableContextOptions);

            if (options.SpecificDiagnosticOptions.Count > 0)
            {
                Writer.WriteKey("specificDiagnosticOptions");
                Writer.WriteArrayStart();
                foreach (var kvp in options.SpecificDiagnosticOptions)
                {
                    Writer.WriteObjectStart();
                    WriteEnum(kvp.Key, kvp.Value);
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
            WriteEnum("kind", parseOptions.Kind);
            WriteEnum("specifiedKind", parseOptions.SpecifiedKind);
            WriteEnum("documentationMode", parseOptions.DocumentationMode);
            WriteString("language", parseOptions.Language);

            var features = parseOptions.Features;
            if (features.Count > 0)
            {
                Writer.WriteKey("features");
                Writer.WriteArrayStart();
                foreach (var kvp in features)
                {
                    Writer.WriteObjectStart();
                    WriteString(kvp.Key, kvp.Value);
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
