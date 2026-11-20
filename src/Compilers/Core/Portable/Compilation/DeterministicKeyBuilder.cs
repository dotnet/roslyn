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
            Stream? sourceLinkStream,
            ImmutableArray<ResourceDescription> resources,
            ImmutableArray<AnalyzerConfig> analyzerConfigFiles,
            AnalyzerConfigSet? analyzerConfigSet,
            string? generatedSourceBaseDirectory,
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
            analyzerConfigFiles = analyzerConfigFiles.NullToEmpty();

            Debug.Assert(analyzerConfigFiles.IsEmpty || analyzerConfigSet is not null);
            var configFilePathMap = analyzerConfigSet is null || pathMap.IsDefaultOrEmpty
                ? ImmutableArray<KeyValuePair<string, string>>.Empty
                : pathMap.SelectAsArray(static pair => KeyValuePair.Create(
                    PathUtilities.NormalizeDriveLetter(pair.Key),
                    pair.Value));
            var configPathMap = configFilePathMap.SelectAsArray(static pair => KeyValuePair.Create(
                PathUtilities.NormalizeWithForwardSlash(pair.Key),
                PathUtilities.NormalizeWithForwardSlash(pair.Value)));
            string? generatedDirectory = null;
            string? generatedPrefix = null;
            if (!generators.IsEmpty && !analyzerConfigFiles.IsEmpty)
            {
                Debug.Assert(generatedSourceBaseDirectory is not null);
                generatedDirectory = PathUtilities.NormalizeDriveLetter(
                    PathUtilities.ExpandAbsolutePathWithRelativeParts(PathUtilities.CollapseWithForwardSlash(generatedSourceBaseDirectory)));
                generatedPrefix = generatedDirectory.EndsWith("/", StringComparison.Ordinal)
                    ? generatedDirectory
                    : generatedDirectory + "/";
            }

            var (writer, builder) = CreateWriter();

            writer.WriteObjectStart();

            writer.WriteKey("compilation");
            WriteCompilation(writer, compilationOptions, syntaxTrees, references, publicKey, pathMap, options, cancellationToken);
            writer.WriteKey("additionalTexts");
            writeAdditionalTexts();
            writer.WriteKey("analyzerConfigs");
            writeAnalyzerConfigs();
            writer.WriteKey("analyzers");
            writeAnalyzers();
            writer.WriteKey("generators");
            writeGenerators();
            writer.WriteKey("emitOptions");
            WriteEmitOptions(writer, emitOptions, pathMap, sourceLinkStream, options);
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

                    // Generators can consume binary additional files by reading Path directly, so hash command-line files without decoding them.
                    if (additionalText is AdditionalTextFile additionalTextFile)
                    {
                        using var stream = additionalTextFile.OpenRead();
                        WriteStream(writer, stream);
                    }
                    else
                    {
                        WriteSourceText(writer, additionalText.GetText(cancellationToken));
                    }

                    writer.WriteObjectEnd();
                }
                writer.WriteArrayEnd();
            }

            void writeAnalyzerConfigs()
            {
                using var _ = PooledDictionary<AnalyzerConfig.Section, ArrayBuilder<(int InputIndex, int Order)>>.GetInstance(out var bindings);
                try
                {
                    if (analyzerConfigSet is not null)
                    {
                        AddAnalyzerConfigSectionBindings(analyzerConfigFiles, analyzerConfigSet, syntaxTrees, additionalTexts, bindings, cancellationToken);
                    }

                    writer.WriteArrayStart();
                    foreach (var config in analyzerConfigFiles)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var mayMatchGenerated = generatedDirectory is not null && !config.IsGlobal &&
                            (PathUtilities.IsSameDirectoryOrChildOf(generatedDirectory, config.NormalizedDirectory, StringComparison.Ordinal) ||
                             PathUtilities.IsSameDirectoryOrChildOf(config.NormalizedDirectory, generatedDirectory, StringComparison.Ordinal));

                        writer.WriteObjectStart();
                        WriteFilePath(writer, "fileName", config.PathToFile, configFilePathMap, options);
                        writer.Write("isGlobal", config.IsGlobal);
                        writer.Write("globalLevel", config.GlobalLevel);
                        writer.WriteKey("properties");
                        WriteAnalyzerConfigProperties(writer, config.GlobalSection.Properties, configPathMap, options, cancellationToken);
                        writer.WriteArrayStart("sections");
                        foreach (var section in config.NamedSections)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var absolutePath = config.IsGlobal && AnalyzerConfig.IsAbsoluteEditorConfigPath(section.Name)
                                ? AnalyzerConfig.UnescapeSectionName(section.Name)
                                : null;
                            var generatedFileName = absolutePath is not null && generatedPrefix is not null &&
                                absolutePath.StartsWith(generatedPrefix, StringComparison.Ordinal)
                                ? absolutePath.Substring(generatedPrefix.Length)
                                : null;
                            var mayMatchGeneratedSection = mayMatchGenerated || generatedFileName is not null;
                            bindings.TryGetValue(section, out var sectionBindings);
                            if (sectionBindings is null && !mayMatchGeneratedSection)
                            {
                                continue;
                            }

                            writer.WriteObjectStart();
                            writer.Write("name", absolutePath is null
                                ? section.Name
                                : NormalizeAnalyzerConfigPath(absolutePath, configPathMap, options));
                            writer.WriteKey("properties");
                            WriteAnalyzerConfigProperties(writer, section.Properties, configPathMap, options, cancellationToken);
                            WriteAnalyzerConfigSectionBindings(writer, "sourceFiles", sectionBindings, 0, syntaxTrees.Length, cancellationToken);
                            WriteAnalyzerConfigSectionBindings(writer, "additionalFiles", sectionBindings, syntaxTrees.Length, additionalTexts.Length, cancellationToken);

                            // This is the global section's target relative to the generated-source base, before PathMap.
                            // At cache lookup, generated filenames are unknown, so we cannot bind the section to them yet.
                            // The mapped section name alone cannot capture changes in matching when the generated-source base moves.
                            writer.Write("generatedFileName", generatedFileName);
                            writer.WriteObjectEnd();
                        }
                        writer.WriteArrayEnd();

                        // Ordinary editorconfig patterns are relative to the config directory.
                        // Record where generated files will be placed relative to that directory, since their names are unknown at cache lookup.
                        // Keep both directions: if the config moves from generated/a to generated/b, the path to the generated base is ".." in both cases;
                        // only the reverse path distinguishes "a" from "b".
                        writer.WriteKey("generatedSourceDirectory");
                        if (mayMatchGenerated && analyzerConfigSet?.HasNamedSections == true)
                        {
                            Debug.Assert(generatedDirectory is not null);
                            writer.WriteObjectStart();
                            writer.Write("relativeToConfig", GetAnalyzerConfigRelativeDirectory(config.NormalizedDirectory, generatedDirectory));
                            writer.Write("configRelativeToGenerated", GetAnalyzerConfigRelativeDirectory(generatedDirectory, config.NormalizedDirectory));
                            writer.WriteObjectEnd();
                        }
                        else
                        {
                            writer.WriteNull();
                        }
                        writer.WriteObjectEnd();
                    }
                    writer.WriteArrayEnd();
                }
                finally
                {
                    foreach (var binding in bindings.Values)
                    {
                        binding.Free();
                    }
                }
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
                foreach (ResourceDescription resource in resources)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    writer.WriteObjectStart();
                    writer.Write("resourceName", resource.ResourceName);
                    writer.Write("fileName", resource.FileName);
                    writer.Write("isPublic", resource.IsPublic);
                    writer.WriteKey("content");
                    writeResourceContent(writer, resource);
                    writer.WriteObjectEnd();
                }
                writer.WriteArrayEnd();
            }

            void writeResourceContent(JsonWriter writer, ResourceDescription resource)
            {
                if (resource.IsEmbedded)
                {
                    using Stream stream = resource.DataProvider();
                    WriteStream(writer, stream);
                }
                else
                {
                    writer.WriteNull();
                }
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
            DeterministicKeyOptions options,
            CancellationToken cancellationToken)
        {
            writer.WriteObjectStart();
            writeToolsVersions();

            WriteByteArrayValue(writer, "publicKey", publicKey.AsSpan());
            writer.WriteKey("options");
            WriteCompilationOptions(writer, compilationOptions);

            // Collect unique ParseOptions from all syntax trees and write them as a top-level array.
            // Each syntax tree will reference its ParseOptions by index into this array, avoiding
            // repetition when all (or most) trees share the same options.
            var parseOptionsList = new List<ParseOptions>();
            foreach (var syntaxTree in syntaxTrees)
            {
                var treeOptions = syntaxTree.Options;
                if (!parseOptionsList.Contains(treeOptions))
                {
                    parseOptionsList.Add(treeOptions);
                }
            }

            writer.WriteKey("parseOptions");
            writer.WriteArrayStart();
            foreach (var parseOptions in parseOptionsList)
            {
                WriteParseOptions(writer, parseOptions);
            }
            writer.WriteArrayEnd();

            writer.WriteKey("syntaxTrees");
            writer.WriteArrayStart();
            foreach (var syntaxTree in syntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WriteSyntaxTree(writer, syntaxTree, parseOptionsList.IndexOf(syntaxTree.Options), pathMap, options, cancellationToken);
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
            int parseOptionsIndex,
            ImmutableArray<KeyValuePair<string, string>> pathMap,
            DeterministicKeyOptions options,
            CancellationToken cancellationToken)
        {
            writer.WriteObjectStart();
            WriteFilePath(writer, "fileName", syntaxTree.FilePath, pathMap, options);
            writer.WriteKey("text");
            WriteSourceText(writer, syntaxTree.GetText(cancellationToken));
            writer.Write("parseOptionsIndex", parseOptionsIndex);
            writer.WriteObjectEnd();
        }

        private static string GetAnalyzerConfigRelativeDirectory(string generatedDirectory, string configDirectory)
        {
            var relativePath = PathUtilities.NormalizeWithForwardSlash(PathUtilities.GetRelativePath(generatedDirectory, configDirectory));
            var combinedPath = PathUtilities.ExpandAbsolutePathWithRelativeParts(
                PathUtilities.CollapseWithForwardSlash(PathUtilities.CombinePathsUnchecked(generatedDirectory, relativePath)));

            // Matching is case-sensitive, even on Windows. Do not lose a case difference when calculating a relative path.
            return string.Equals(
                PathUtilities.TrimTrailingSeparators(combinedPath),
                PathUtilities.TrimTrailingSeparators(configDirectory),
                StringComparison.Ordinal) ? relativePath : configDirectory;
        }

        private static string NormalizeAnalyzerConfigPath(
            string path, ImmutableArray<KeyValuePair<string, string>> pathMap, DeterministicKeyOptions options, bool directory = false)
        {
            path = PathUtilities.NormalizeWithForwardSlash(PathUtilities.NormalizeDriveLetter(path));
            if ((options & DeterministicKeyOptions.IgnorePaths) != 0)
            {
                return Path.GetFileName(PathUtilities.TrimTrailingSeparators(path));
            }

            // Prefix mappings include a trailing separator; directory-valued properties need not.
            var addSeparator = directory && path.Length > 0 && !path.EndsWith("/", StringComparison.Ordinal);
            var normalized = PathUtilities.NormalizePathPrefix(addSeparator ? path + "/" : path, pathMap);
            if (!addSeparator)
            {
                return normalized;
            }

            var trimmed = PathUtilities.TrimTrailingSeparators(normalized);
            var root = PathUtilities.GetPathRoot(normalized);
            return root is not null && trimmed.Length < root.Length ? root : trimmed;
        }

        private static void WriteAnalyzerConfigProperties(
            JsonWriter writer,
            ImmutableDictionary<string, string> properties,
            ImmutableArray<KeyValuePair<string, string>> pathMap,
            DeterministicKeyOptions options,
            CancellationToken cancellationToken)
        {
            writer.WriteObjectStart();
            foreach (var key in properties.Keys.OrderBy(StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = properties[key];
                if (key is "build_property.projectdir" or "build_property.msbuildprojectdirectory" or
                    "build_property.intermediateoutputpath" or "build_property.outdir")
                {
                    value = NormalizeAnalyzerConfigPath(value, pathMap, options, directory: true);
                }
                else if (key == "build_property.applicationmanifest")
                {
                    value = NormalizeAnalyzerConfigPath(value, pathMap, options);
                }

                writer.Write(key, value);
            }
            writer.WriteObjectEnd();
        }

        /// <summary>
        /// Populates <paramref name="bindings"/> with the inputs each original named analyzer-config section applies to, using the inputs' unmapped paths.
        /// </summary>
        /// <param name="bindings">
        /// <c>InputIndex</c> indexes <paramref name="syntaxTrees"/> followed by <paramref name="additionalTexts"/>,
        /// so additional-text indices are offset by the number of syntax trees.
        /// <c>Order</c> indexes that input's final list of matching named sections, across applicable config files.
        /// The list can contain multiple sections per config and excludes parent sections discarded by <c>root = true</c>.
        /// Higher indices apply later.
        /// Keys are the original section instances from <paramref name="configs"/>.
        /// Original sections contributing to the same merged global section receive the same order.
        /// </param>
        private static void AddAnalyzerConfigSectionBindings(
            ImmutableArray<AnalyzerConfig> configs,
            AnalyzerConfigSet configSet,
            ImmutableArray<SyntaxTreeKey> syntaxTrees,
            ImmutableArray<AdditionalText> additionalTexts,
            PooledDictionary<AnalyzerConfig.Section, ArrayBuilder<(int InputIndex, int Order)>> bindings,
            CancellationToken cancellationToken)
        {
            using var _ = PooledDictionary<string, ArrayBuilder<AnalyzerConfig.Section>>.GetInstance(out var globalSections);
            try
            {
                foreach (var config in configs)
                {
                    if (!config.IsGlobal)
                    {
                        continue;
                    }

                    foreach (var section in config.NamedSections)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!AnalyzerConfig.IsAbsoluteEditorConfigPath(section.Name))
                        {
                            continue;
                        }

                        var path = AnalyzerConfig.UnescapeSectionName(section.Name);
                        if (!globalSections.TryGetValue(path, out var sections))
                        {
                            sections = ArrayBuilder<AnalyzerConfig.Section>.GetInstance();
                            globalSections.Add(path, sections);
                        }
                        sections.Add(section);
                    }
                }

                var matchingSections = new List<AnalyzerConfig.Section>();
                for (int inputIndex = 0; inputIndex < syntaxTrees.Length + additionalTexts.Length; inputIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var path = inputIndex < syntaxTrees.Length
                        ? syntaxTrees[inputIndex].FilePath
                        : additionalTexts[inputIndex - syntaxTrees.Length].Path;
                    var globalSection = configSet.GetMatchingSections(path, matchingSections);
                    for (int order = 0; order < matchingSections.Count; order++)
                    {
                        var section = matchingSections[order];
                        if (section == globalSection)
                        {
                            // The evaluator merges global sections; bind all contributing definitions at the same precedence position.
                            foreach (var originalSection in globalSections[section.Name])
                            {
                                addBinding(originalSection, inputIndex, order);
                            }
                        }
                        else
                        {
                            addBinding(section, inputIndex, order);
                        }
                    }
                    matchingSections.Clear();
                }
            }
            finally
            {
                foreach (var sections in globalSections.Values)
                {
                    sections.Free();
                }
            }

            void addBinding(AnalyzerConfig.Section section, int inputIndex, int order)
            {
                if (!bindings.TryGetValue(section, out var entries))
                {
                    entries = ArrayBuilder<(int InputIndex, int Order)>.GetInstance();
                    bindings.Add(section, entries);
                }
                entries.Add((inputIndex, order));
            }
        }

        private static void WriteAnalyzerConfigSectionBindings(
            JsonWriter writer,
            string propertyName,
            ArrayBuilder<(int InputIndex, int Order)>? bindings,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            writer.WriteArrayStart(propertyName);
            if (bindings is not null)
            {
                foreach (var (inputIndex, order) in bindings)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (inputIndex >= offset && inputIndex - offset < count)
                    {
                        writer.WriteObjectStart();
                        writer.Write("index", inputIndex - offset);
                        writer.Write("order", order);
                        writer.WriteObjectEnd();
                    }
                }
            }
            writer.WriteArrayEnd();
        }

        private static void WriteSourceText(JsonWriter writer, SourceText? sourceText)
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

        private static void WriteStream(JsonWriter writer, Stream? stream)
        {
            if (stream is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteObjectStart();
            var checksumAlgorithm = SourceHashAlgorithms.Default;
            var checksum = SourceText.CalculateChecksum(stream, checksumAlgorithm);
            WriteByteArrayValue(writer, "checksum", checksum.AsSpan());
            writer.Write("checksumAlgorithm", checksumAlgorithm);
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
            Stream? sourceLinkStream,
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
            WriteStream(writer, sourceLinkStream);
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
            WriteByteArrayValue(writer, "cryptoPublicKey", options.CryptoPublicKey.AsSpan());

            writer.Write("cryptoKeyContainer", options.CryptoKeyContainer);

            // For NetModule the crypto key file value is serialized directly into an attribute
            // in the output so the full path is relevant. For all other output kinds only the
            // file name matters as the key content is consumed via the public key above.
            if (options.OutputKind == OutputKind.NetModule)
            {
                writer.Write("cryptoKeyFile", options.CryptoKeyFile);
            }
            else
            {
                writer.Write("cryptoKeyFile", options.CryptoKeyFile is { } keyFile ? Path.GetFileName(keyFile) : null);
            }

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
