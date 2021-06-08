// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
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
    /// </summary>
    internal abstract class DeterministicKeyBuilder
    {
        internal StringBuilder Builder { get; } = new StringBuilder();

        public DeterministicKeyBuilder()
        {
        }

        internal string GetKey() => Builder.ToString();

        internal void Reset() => Builder.Length = 0;

        protected void AppendEnum<T>(string name, T value) where T : struct, Enum
        {
            Builder.Append(name);
            Builder.Append('=');
            Builder.Append(value.ToString());
            Builder.AppendLine();
        }

        protected void AppendInt(string name, int value)
        {
            Builder.Append(name);
            Builder.Append('=');
            Builder.Append(value.ToString());
            Builder.AppendLine();
        }

        protected void AppendBool(string name, bool? value)
        {
            if (value is bool b)
            {
                Builder.Append(name);
                Builder.Append('=');
                Builder.Append(b.ToString());
                Builder.AppendLine();
            }
        }

        protected void AppendString(string name, string? value)
        {
            // Skip null values for brevity. The lack of the value is just as significant in the 
            // key and overall makes it more readable
            if (value is object)
            {
                Builder.Append(name);
                Builder.Append('=');
                Builder.Append(value);
                Builder.AppendLine();
            }
        }

        protected void AppendLine(string? line)
        {
            Builder.AppendLine(line);
        }

        protected void AppendSpaces(int spaceCount)
        {
            for (int i = 0; i < spaceCount; i++)
            {
                Builder.Append(' ');
            }
        }

        protected void AppendByteArray(string name, ImmutableArray<byte> value)
        {
            if (!value.IsDefault)
            {
                Builder.Append(name);
                Builder.Append('=');
                foreach (var b in value)
                {
                    Builder.Append(b.ToString("x"));
                }
                Builder.AppendLine();
            }
        }

        internal virtual void AppendCompilation(Compilation compilation)
        {
            AppendLine("Options");
            AppendCompilationOptions(compilation.Options);
            AppendLine("=== Start Syntax Trees ===");
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                AppendSyntaxTree(syntaxTree);
            }
            AppendLine("=== End Syntax Trees ===");

            foreach (var reference in compilation.References)
            {
                AppendMetadataReference(reference);
            }
            AppendLine("=== End References ===");
        }

        internal void AppendSyntaxTree(SyntaxTree syntaxTree)
        {
            AppendString("File Name", Path.GetFileName(syntaxTree.FilePath));
            AppendString("Encoding", syntaxTree.Encoding?.EncodingName);
            AppendByteArray("Checksum", syntaxTree.GetDebugSourceInfo().Checksum);
            AppendLine("Parse Options");
            AppendParseOptions(syntaxTree.Options);
        }

        internal void AppendMetadataReference(MetadataReference reference)
        {
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

                AppendString("Name", Path.GetFileName(peReference.FilePath));
                AppendString("MVID", mvid.ToString());
                AppendMetadataReferenceProperties(reference.Properties);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        internal void AppendMetadataReferenceProperties(MetadataReferenceProperties properties)
        {
            AppendEnum(nameof(MetadataReferenceProperties.Kind), properties.Kind);
            AppendBool(nameof(MetadataReferenceProperties.EmbedInteropTypes), properties.EmbedInteropTypes);
            if (properties.Aliases is { Length: > 0 } aliases)
            {
                AppendLine(nameof(MetadataReferenceProperties.Aliases));
                foreach (var alias in aliases)
                {
                    AppendSpaces(spaceCount: 4);
                    AppendLine(alias);
                }
            }
        }

        internal virtual void AppendCompilationOptions(CompilationOptions options)
        {
            // CompilationOption values
            AppendEnum(nameof(CompilationOptions.OutputKind), options.OutputKind);
            AppendString(nameof(CompilationOptions.ModuleName), options.ModuleName);
            AppendString(nameof(CompilationOptions.ScriptClassName), options.ScriptClassName);
            AppendString(nameof(CompilationOptions.MainTypeName), options.MainTypeName);
            AppendByteArray(nameof(CompilationOptions.CryptoPublicKey), options.CryptoPublicKey);
            AppendString(nameof(CompilationOptions.CryptoKeyFile), options.CryptoKeyFile);
            AppendBool(nameof(CompilationOptions.DelaySign), options.DelaySign);
            AppendBool(nameof(CompilationOptions.PublicSign), options.PublicSign);
            AppendBool(nameof(CompilationOptions.CheckOverflow), options.CheckOverflow);
            AppendEnum(nameof(CompilationOptions.Platform), options.Platform);
            AppendEnum(nameof(CompilationOptions.OptimizationLevel), options.OptimizationLevel);
            AppendEnum(nameof(CompilationOptions.GeneralDiagnosticOption), options.GeneralDiagnosticOption);
            AppendInt(nameof(CompilationOptions.WarningLevel), options.WarningLevel);
            AppendBool(nameof(CompilationOptions.Deterministic), options.Deterministic);
            AppendBool(nameof(CompilationOptions.DebugPlusMode), options.DebugPlusMode);
            AppendBool(nameof(CompilationOptions.ReferencesSupersedeLowerVersions), options.ReferencesSupersedeLowerVersions);
            AppendBool(nameof(CompilationOptions.ReportSuppressedDiagnostics), options.ReportSuppressedDiagnostics);
            AppendEnum(nameof(CompilationOptions.NullableContextOptions), options.NullableContextOptions);

            if (options.SpecificDiagnosticOptions.Count > 0)
            {
                AppendLine(nameof(CompilationOptions.SpecificDiagnosticOptions));
                foreach (var kvp in options.SpecificDiagnosticOptions)
                {
                    AppendSpaces(spaceCount: 4);
                    AppendEnum(kvp.Key, kvp.Value);
                }
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

        internal virtual void AppendParseOptions(ParseOptions parseOptions)
        {
            AppendEnum(nameof(ParseOptions.Kind), parseOptions.Kind);
            AppendEnum(nameof(ParseOptions.SpecifiedKind), parseOptions.SpecifiedKind);
            AppendEnum(nameof(ParseOptions.DocumentationMode), parseOptions.DocumentationMode);
            AppendString(nameof(ParseOptions.Language), parseOptions.Language);

            var features = parseOptions.Features;
            if (features.Count > 0)
            {
                AppendLine("Features");
                foreach (var kvp in features)
                {
                    AppendSpaces(spaceCount: 4);
                    AppendString(kvp.Key, kvp.Value);
                }
            }

            // Skipped values
            // - Errors: not sure if we need that in the key file or not
            // - PreprocessorSymbolNames: handled at the language specific level
        }
    }
}
