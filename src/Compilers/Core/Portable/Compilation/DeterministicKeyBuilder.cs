// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Debugging;
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

        public DeterministicKeyBuilder()
        {
            StringWriter = new StringWriter(Builder);
            Writer = new JsonWriter(StringWriter);
        }

        internal string GetKey() => Builder.ToString();

        internal void Reset() => Builder.Length = 0;

        protected void WriteEnum<T>(string name, T value) where T : struct, Enum
        {
            Writer.Write(name, value.ToString());
        }

        protected void WriteInt(string name, int value)
        {
            Writer.Write(name, value);
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

        protected void WriteByteArray(string name, ImmutableArray<byte> value)
        {
            if (!value.IsDefault)
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
            Writer.WriteObjectStart();
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
        }

        internal void WriteSyntaxTree(SyntaxTree syntaxTree)
        {
            Writer.WriteObjectStart();
            WriteString("fileName", Path.GetFileName(syntaxTree.FilePath));
            WriteString("encoding", syntaxTree.Encoding?.EncodingName);

            var debugSourceInfo = syntaxTree.GetDebugSourceInfo();
            WriteByteArray("checksum", debugSourceInfo.Checksum);
            WriteEnum("checksumAlgorithm", SourceHashAlgorithms.GetSourceHashAlgorithm(debugSourceInfo.ChecksumAlgorithmId));
            Writer.WriteKey("parseOptions");
            WriteParseOptions(syntaxTree.Options);
            Writer.WriteObjectEnd();
        }

        internal void WriteMetadataReference(MetadataReference reference)
        {
            Writer.WriteObjectStart();
            if (reference is PortableExecutableReference peReference)
            {
                Guid mvid;
                switch (peReference.GetMetadata())
                {
                    case AssemblyMetadata assemblyMetadata:
                        {
                            if (assemblyMetadata.GetModules() is { Length: 1 } modules)
                            {
                                mvid = modules[0].GetModuleVersionId();
                            }
                            else
                            {
                                throw new InvalidOperationException();
                            }
                        }
                        break;
                    case ModuleMetadata moduleMetadata:
                        mvid = moduleMetadata.GetModuleVersionId();
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                if (peReference.FilePath is null)
                {
                    throw new InvalidOperationException();
                }

                WriteString("name", Path.GetFileName(peReference.FilePath));
                WriteString("mvid", mvid.ToString());
                Writer.WriteKey("properties");
                WriteMetadataReferenceProperties(reference.Properties);
            }
            else
            {
                throw new InvalidOperationException();
            }
            Writer.WriteObjectEnd();
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
