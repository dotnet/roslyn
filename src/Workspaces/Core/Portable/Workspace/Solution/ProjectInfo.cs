// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// A class that represents all the arguments necessary to create a new project instance.
/// </summary>
[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
public sealed class ProjectInfo
{
    internal ProjectAttributes Attributes { get; }

    /// <summary>
    /// The unique Id of the project.
    /// </summary>
    public ProjectId Id => Attributes.Id;

    /// <summary>
    /// The version of the project.
    /// </summary>
    public VersionStamp Version => Attributes.Version;

    /// <summary>
    /// The name of the project. This may differ from the project's filename.
    /// </summary>
    public string Name => Attributes.Name;

    /// <inheritdoc cref="ProjectAttributes.NameAndFlavor"/>
    internal (string? name, string? flavor) NameAndFlavor => Attributes.NameAndFlavor;

    /// <summary>
    /// The name of the assembly that this project will create, without file extension.
    /// </summary>,
    public string AssemblyName => Attributes.AssemblyName;

    /// <summary>
    /// The language of the project.
    /// </summary>
    public string Language => Attributes.Language;

    /// <summary>
    /// The path to the project file or null if there is no project file.
    /// </summary>
    public string? FilePath => Attributes.FilePath;

    /// <summary>
    /// The path to the output file (module or assembly).
    /// </summary>
    public string? OutputFilePath => Attributes.OutputFilePath;

    /// <summary>
    /// The path to the reference assembly output file.
    /// </summary>
    public string? OutputRefFilePath => Attributes.OutputRefFilePath;

    /// <summary>
    /// The path to the compiler output file (module or assembly).
    /// </summary>
    public CompilationOutputInfo CompilationOutputInfo => Attributes.CompilationOutputInfo;

    /// <summary>
    /// The default namespace of the project ("" if not defined, which means global namespace),
    /// or null if it is unknown or not applicable.
    /// </summary>
    /// <remarks>
    /// Right now VB doesn't have the concept of "default namespace", but we conjure one in workspace
    /// by assigning the value of the project's root namespace to it. So various features can choose to
    /// use it for their own purpose.
    /// In the future, we might consider officially exposing "default namespace" for VB project
    /// (e.g. through a "defaultnamespace" msbuild property)
    /// </remarks>
    internal string? DefaultNamespace => Attributes.DefaultNamespace;

    /// <summary>
    /// Algorithm to calculate content checksum for debugging purposes.
    /// </summary>
    internal SourceHashAlgorithm ChecksumAlgorithm => Attributes.ChecksumAlgorithm;

    /// <summary>
    /// True if this is a submission project for interactive sessions.
    /// </summary>
    public bool IsSubmission => Attributes.IsSubmission;

    /// <summary>
    /// True if project information is complete. In some workspace hosts, it is possible
    /// a project only has partial information. In such cases, a project might not have all
    /// information on its files or references.
    /// </summary>
    internal bool HasAllInformation => Attributes.HasAllInformation;

    /// <summary>
    /// True if we should run analyzers for this project.
    /// </summary>
    internal bool RunAnalyzers => Attributes.RunAnalyzers;

    /// <summary>
    /// The initial compilation options for the project, or null if the default options should be used.
    /// </summary>
    public CompilationOptions? CompilationOptions { get; }

    /// <summary>
    /// The initial parse options for the source code documents in this project, or null if the default options should be used.
    /// </summary>
    public ParseOptions? ParseOptions { get; }

    /// <summary>
    /// The list of source documents initially associated with the project.
    /// </summary>
    public IReadOnlyList<DocumentInfo> Documents { get; }

    /// <summary>
    /// The project references initially defined for the project.
    /// </summary>
    public IReadOnlyList<ProjectReference> ProjectReferences { get; }

    /// <summary>
    /// The metadata references initially defined for the project.
    /// </summary>
    public IReadOnlyList<MetadataReference> MetadataReferences { get; }

    /// <summary>
    /// The analyzers initially associated with this project.
    /// </summary>
    public IReadOnlyList<AnalyzerReference> AnalyzerReferences { get; }

    /// <summary>
    /// The list of non-source documents associated with this project.
    /// </summary>
    public IReadOnlyList<DocumentInfo> AdditionalDocuments { get; }

    /// <summary>
    /// The list of analyzerconfig documents associated with this project.
    /// </summary>
    public IReadOnlyList<DocumentInfo> AnalyzerConfigDocuments { get; }

    /// <summary>
    /// Type of the host object.
    /// </summary>
    public Type? HostObjectType { get; }

    private ProjectInfo(
        ProjectAttributes attributes,
        CompilationOptions? compilationOptions,
        ParseOptions? parseOptions,
        IReadOnlyList<DocumentInfo> documents,
        IReadOnlyList<ProjectReference> projectReferences,
        IReadOnlyList<MetadataReference> metadataReferences,
        IReadOnlyList<AnalyzerReference> analyzerReferences,
        IReadOnlyList<DocumentInfo> additionalDocuments,
        IReadOnlyList<DocumentInfo> analyzerConfigDocuments,
        Type? hostObjectType)
    {
        Attributes = attributes;
        CompilationOptions = compilationOptions;
        ParseOptions = parseOptions;
        Documents = documents;
        ProjectReferences = projectReferences;
        MetadataReferences = metadataReferences;
        AnalyzerReferences = analyzerReferences;
        AdditionalDocuments = additionalDocuments;
        AnalyzerConfigDocuments = analyzerConfigDocuments;
        HostObjectType = hostObjectType;
    }

    // 2.7.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
    /// <summary>
    /// Create a new instance of a <see cref="ProjectInfo"/>.
    /// </summary>
    public static ProjectInfo Create(
        ProjectId id,
        VersionStamp version,
        string name,
        string assemblyName,
        string language,
        string? filePath,
        string? outputFilePath,
        CompilationOptions? compilationOptions,
        ParseOptions? parseOptions,
        IEnumerable<DocumentInfo>? documents,
        IEnumerable<ProjectReference>? projectReferences,
        IEnumerable<MetadataReference>? metadataReferences,
        IEnumerable<AnalyzerReference>? analyzerReferences,
        IEnumerable<DocumentInfo>? additionalDocuments,
        bool isSubmission,
        Type? hostObjectType)
    {
        return Create(
            id, version, name, assemblyName, language,
            filePath, outputFilePath, compilationOptions, parseOptions,
            documents, projectReferences, metadataReferences, analyzerReferences, additionalDocuments,
            isSubmission, hostObjectType, outputRefFilePath: null);
    }

    /// <summary>
    /// Create a new instance of a <see cref="ProjectInfo"/>.
    /// </summary>
    public static ProjectInfo Create(
        ProjectId id,
        VersionStamp version,
        string name,
        string assemblyName,
        string language,
        string? filePath = null,
        string? outputFilePath = null,
        CompilationOptions? compilationOptions = null,
        ParseOptions? parseOptions = null,
        IEnumerable<DocumentInfo>? documents = null,
        IEnumerable<ProjectReference>? projectReferences = null,
        IEnumerable<MetadataReference>? metadataReferences = null,
        IEnumerable<AnalyzerReference>? analyzerReferences = null,
        IEnumerable<DocumentInfo>? additionalDocuments = null,
        bool isSubmission = false,
        Type? hostObjectType = null,
        string? outputRefFilePath = null)
    {
        return Create(
            new ProjectAttributes(
                id ?? throw new ArgumentNullException(nameof(id)),
                version,
                name ?? throw new ArgumentNullException(nameof(name)),
                assemblyName ?? throw new ArgumentNullException(nameof(assemblyName)),
                language ?? throw new ArgumentNullException(nameof(language)),
                compilationOutputFilePaths: default,
                checksumAlgorithm: SourceHashAlgorithm.Sha1,
                defaultNamespace: null,
                filePath: filePath,
                outputFilePath: outputFilePath,
                outputRefFilePath: outputRefFilePath,
                telemetryId: default,
                isSubmission,
                hasAllInformation: true,
                runAnalyzers: true),
            compilationOptions,
            parseOptions,
            documents,
            projectReferences,
            metadataReferences,
            analyzerReferences,
            additionalDocuments,
            analyzerConfigDocuments: SpecializedCollections.EmptyBoxedImmutableArray<DocumentInfo>(),
            hostObjectType);
    }

    internal static ProjectInfo Create(
        ProjectAttributes attributes,
        CompilationOptions? compilationOptions = null,
        ParseOptions? parseOptions = null,
        IEnumerable<DocumentInfo>? documents = null,
        IEnumerable<ProjectReference>? projectReferences = null,
        IEnumerable<MetadataReference>? metadataReferences = null,
        IEnumerable<AnalyzerReference>? analyzerReferences = null,
        IEnumerable<DocumentInfo>? additionalDocuments = null,
        IEnumerable<DocumentInfo>? analyzerConfigDocuments = null,
        Type? hostObjectType = null)
    {
        return new ProjectInfo(
            attributes,
            compilationOptions,
            parseOptions,
            PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(documents, nameof(documents)),
            PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(projectReferences, nameof(projectReferences)),
            PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(metadataReferences, nameof(metadataReferences)),
            PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(analyzerReferences, nameof(analyzerReferences)),
            PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(additionalDocuments, nameof(additionalDocuments)),
            PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(analyzerConfigDocuments, nameof(analyzerConfigDocuments)),
            hostObjectType);
    }

    internal ProjectInfo With(
        ProjectAttributes? attributes = null,
        Optional<CompilationOptions?> compilationOptions = default,
        Optional<ParseOptions?> parseOptions = default,
        IReadOnlyList<DocumentInfo>? documents = null,
        IReadOnlyList<ProjectReference>? projectReferences = null,
        IReadOnlyList<MetadataReference>? metadataReferences = null,
        IReadOnlyList<AnalyzerReference>? analyzerReferences = null,
        IReadOnlyList<DocumentInfo>? additionalDocuments = null,
        IReadOnlyList<DocumentInfo>? analyzerConfigDocuments = null,
        Optional<Type?> hostObjectType = default)
    {
        var newAttributes = attributes ?? Attributes;
        var newCompilationOptions = compilationOptions.HasValue ? compilationOptions.Value : CompilationOptions;
        var newParseOptions = parseOptions.HasValue ? parseOptions.Value : ParseOptions;
        var newDocuments = documents ?? Documents;
        var newProjectReferences = projectReferences ?? ProjectReferences;
        var newMetadataReferences = metadataReferences ?? MetadataReferences;
        var newAnalyzerReferences = analyzerReferences ?? AnalyzerReferences;
        var newAdditionalDocuments = additionalDocuments ?? AdditionalDocuments;
        var newAnalyzerConfigDocuments = analyzerConfigDocuments ?? AnalyzerConfigDocuments;
        var newHostObjectType = hostObjectType.HasValue ? hostObjectType.Value : HostObjectType;

        if (newAttributes == Attributes &&
            newCompilationOptions == CompilationOptions &&
            newParseOptions == ParseOptions &&
            newDocuments == Documents &&
            newProjectReferences == ProjectReferences &&
            newMetadataReferences == MetadataReferences &&
            newAnalyzerReferences == AnalyzerReferences &&
            newAdditionalDocuments == AdditionalDocuments &&
            newAnalyzerConfigDocuments == AnalyzerConfigDocuments &&
            newHostObjectType == HostObjectType)
        {
            return this;
        }

        return new ProjectInfo(
            newAttributes,
            newCompilationOptions,
            newParseOptions,
            newDocuments,
            newProjectReferences,
            newMetadataReferences,
            newAnalyzerReferences,
            newAdditionalDocuments,
            newAnalyzerConfigDocuments,
            newHostObjectType);
    }

    public ProjectInfo WithVersion(VersionStamp version)
        => With(attributes: Attributes.With(version: version));

    public ProjectInfo WithName(string name)
        => With(attributes: Attributes.With(name: name ?? throw new ArgumentNullException(nameof(name))));

    public ProjectInfo WithAssemblyName(string assemblyName)
        => With(attributes: Attributes.With(assemblyName: assemblyName ?? throw new ArgumentNullException(nameof(assemblyName))));

    public ProjectInfo WithFilePath(string? filePath)
        => With(attributes: Attributes.With(filePath: filePath));

    public ProjectInfo WithOutputFilePath(string? outputFilePath)
        => With(attributes: Attributes.With(outputPath: outputFilePath));

    public ProjectInfo WithOutputRefFilePath(string? outputRefFilePath)
        => With(attributes: Attributes.With(outputRefPath: outputRefFilePath));

    public ProjectInfo WithCompilationOutputInfo(in CompilationOutputInfo info)
        => With(attributes: Attributes.With(compilationOutputInfo: info));

    public ProjectInfo WithDefaultNamespace(string? defaultNamespace)
        => With(attributes: Attributes.With(defaultNamespace: defaultNamespace));

    internal ProjectInfo WithChecksumAlgorithm(SourceHashAlgorithm checksumAlgorithm)
        => With(attributes: Attributes.With(checksumAlgorithm: checksumAlgorithm));

    internal ProjectInfo WithHasAllInformation(bool hasAllInformation)
        => With(attributes: Attributes.With(hasAllInformation: hasAllInformation));

    internal ProjectInfo WithRunAnalyzers(bool runAnalyzers)
        => With(attributes: Attributes.With(runAnalyzers: runAnalyzers));

    public ProjectInfo WithCompilationOptions(CompilationOptions? compilationOptions)
        => With(compilationOptions: compilationOptions);

    public ProjectInfo WithParseOptions(ParseOptions? parseOptions)
        => With(parseOptions: parseOptions);

    public ProjectInfo WithDocuments(IEnumerable<DocumentInfo>? documents)
        => With(documents: PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(documents, nameof(documents)));

    public ProjectInfo WithAdditionalDocuments(IEnumerable<DocumentInfo>? additionalDocuments)
        => With(additionalDocuments: PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(additionalDocuments, nameof(additionalDocuments)));

    public ProjectInfo WithAnalyzerConfigDocuments(IEnumerable<DocumentInfo>? analyzerConfigDocuments)
        => With(analyzerConfigDocuments: PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(analyzerConfigDocuments, nameof(analyzerConfigDocuments)));

    public ProjectInfo WithProjectReferences(IEnumerable<ProjectReference>? projectReferences)
        => With(projectReferences: PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(projectReferences, nameof(projectReferences)));

    public ProjectInfo WithMetadataReferences(IEnumerable<MetadataReference>? metadataReferences)
        => With(metadataReferences: PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(metadataReferences, nameof(metadataReferences)));

    public ProjectInfo WithAnalyzerReferences(IEnumerable<AnalyzerReference>? analyzerReferences)
        => With(analyzerReferences: PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(analyzerReferences, nameof(analyzerReferences)));

    internal ProjectInfo WithTelemetryId(Guid telemetryId)
    {
        return With(attributes: Attributes.With(telemetryId: telemetryId));
    }

    internal string GetDebuggerDisplay()
        => nameof(ProjectInfo) + " " + Name + (!string.IsNullOrWhiteSpace(FilePath) ? " " + FilePath : "");

    /// <summary>
    /// type that contains information regarding this project itself but
    /// no tree information such as document info
    /// </summary>
    internal sealed class ProjectAttributes(
        ProjectId id,
        VersionStamp version,
        string name,
        string assemblyName,
        string language,
        CompilationOutputInfo compilationOutputFilePaths,
        SourceHashAlgorithm checksumAlgorithm,
        string? defaultNamespace = null,
        string? filePath = null,
        string? outputFilePath = null,
        string? outputRefFilePath = null,
        Guid telemetryId = default,
        bool isSubmission = false,
        bool hasAllInformation = true,
        bool runAnalyzers = true)
    {
        /// <summary>
        /// Matches names like: Microsoft.CodeAnalysis.Features (netcoreapp3.1)
        /// </summary>
        private static readonly Regex s_projectNameAndFlavor = new(@"^(?<name>.*?)\s*\((?<flavor>.*?)\)$", RegexOptions.Compiled);

        /// <summary>
        /// The unique Id of the project.
        /// </summary>
        public ProjectId Id { get; } = id;

        /// <summary>
        /// The version of the project.
        /// </summary>
        public VersionStamp Version { get; } = version;

        /// <summary>
        /// The name of the project. This may differ from the project's filename.
        /// </summary>
        public string Name { get; } = name;

        /// <summary>
        /// The name of the assembly that this project will create, without file extension.
        /// </summary>,
        public string AssemblyName { get; } = assemblyName;

        /// <summary>
        /// The language of the project.
        /// </summary>
        public string Language { get; } = language;

        /// <summary>
        /// The path to the project file or null if there is no project file.
        /// </summary>
        public string? FilePath { get; } = filePath;

        /// <summary>
        /// The path to the output file (module or assembly).
        /// </summary>
        public string? OutputFilePath { get; } = outputFilePath;

        /// <summary>
        /// The path to the reference assembly output file.
        /// </summary>
        public string? OutputRefFilePath { get; } = outputRefFilePath;

        /// <summary>
        /// Paths to the compiler output files.
        /// </summary>
        public CompilationOutputInfo CompilationOutputInfo { get; } = compilationOutputFilePaths;

        /// <summary>
        /// The default namespace of the project.
        /// </summary>
        public string? DefaultNamespace { get; } = defaultNamespace;

        /// <summary>
        /// Algorithm to calculate content checksum for debugging purposes.
        /// </summary>
        public SourceHashAlgorithm ChecksumAlgorithm { get; } = checksumAlgorithm;

        /// <summary>
        /// True if this is a submission project for interactive sessions.
        /// </summary>
        public bool IsSubmission { get; } = isSubmission;

        /// <summary>
        /// True if project information is complete. In some workspace hosts, it is possible
        /// a project only has partial information. In such cases, a project might not have all
        /// information on its files or references.
        /// </summary>
        public bool HasAllInformation { get; } = hasAllInformation;

        /// <summary>
        /// True if we should run analyzers for this project.
        /// </summary>
        public bool RunAnalyzers { get; } = runAnalyzers;

        /// <summary>
        /// The id report during telemetry events.
        /// </summary>
        public Guid TelemetryId { get; } = telemetryId;

        private SingleInitNullable<(string? name, string? flavor)> _lazyNameAndFlavor;
        private SingleInitNullable<Checksum> _lazyChecksum;

        /// <summary>
        /// The name and flavor portions of the project broken out.  For example, the project
        /// <c>Microsoft.CodeAnalysis.Workspace (netcoreapp3.1)</c> would have the name
        /// <c>Microsoft.CodeAnalysis.Workspace</c> and the flavor <c>netcoreapp3.1</c>.  Values may be null <see
        /// langword="null"/> if the name does not contain a flavor.
        /// </summary>
        public (string? name, string? flavor) NameAndFlavor
            => _lazyNameAndFlavor.Initialize(static @this =>
            {
                var match = s_projectNameAndFlavor.Match(@this.Name);
                return match.Success ? (match.Groups["name"].Value, match.Groups["flavor"].Value) : default;
            }, this);

        public ProjectAttributes With(
            VersionStamp? version = null,
            string? name = null,
            string? assemblyName = null,
            string? language = null,
            Optional<string?> filePath = default,
            Optional<string?> outputPath = default,
            Optional<string?> outputRefPath = default,
            Optional<CompilationOutputInfo> compilationOutputInfo = default,
            Optional<string?> defaultNamespace = default,
            Optional<SourceHashAlgorithm> checksumAlgorithm = default,
            Optional<bool> isSubmission = default,
            Optional<bool> hasAllInformation = default,
            Optional<bool> runAnalyzers = default,
            Optional<Guid> telemetryId = default)
        {
            var newVersion = version ?? Version;
            var newName = name ?? Name;
            var newAssemblyName = assemblyName ?? AssemblyName;
            var newLanguage = language ?? Language;
            var newFilePath = filePath.HasValue ? filePath.Value : FilePath;
            var newOutputPath = outputPath.HasValue ? outputPath.Value : OutputFilePath;
            var newOutputRefPath = outputRefPath.HasValue ? outputRefPath.Value : OutputRefFilePath;
            var newCompilationOutputPaths = compilationOutputInfo.HasValue ? compilationOutputInfo.Value : CompilationOutputInfo;
            var newDefaultNamespace = defaultNamespace.HasValue ? defaultNamespace.Value : DefaultNamespace;
            var newChecksumAlgorithm = checksumAlgorithm.HasValue ? checksumAlgorithm.Value : ChecksumAlgorithm;
            var newIsSubmission = isSubmission.HasValue ? isSubmission.Value : IsSubmission;
            var newHasAllInformation = hasAllInformation.HasValue ? hasAllInformation.Value : HasAllInformation;
            var newRunAnalyzers = runAnalyzers.HasValue ? runAnalyzers.Value : RunAnalyzers;
            var newTelemetryId = telemetryId.HasValue ? telemetryId.Value : TelemetryId;

            if (newVersion == Version &&
                newName == Name &&
                newAssemblyName == AssemblyName &&
                newLanguage == Language &&
                newFilePath == FilePath &&
                newOutputPath == OutputFilePath &&
                newOutputRefPath == OutputRefFilePath &&
                newCompilationOutputPaths == CompilationOutputInfo &&
                newDefaultNamespace == DefaultNamespace &&
                newChecksumAlgorithm == ChecksumAlgorithm &&
                newIsSubmission == IsSubmission &&
                newHasAllInformation == HasAllInformation &&
                newRunAnalyzers == RunAnalyzers &&
                newTelemetryId == TelemetryId)
            {
                return this;
            }

            return new ProjectAttributes(
                Id,
                newVersion,
                newName,
                newAssemblyName,
                newLanguage,
                newCompilationOutputPaths,
                newChecksumAlgorithm,
                defaultNamespace: newDefaultNamespace,
                filePath: newFilePath,
                outputFilePath: newOutputPath,
                outputRefFilePath: newOutputRefPath,
                newTelemetryId,
                newIsSubmission,
                newHasAllInformation,
                newRunAnalyzers);
        }

        public void WriteTo(ObjectWriter writer)
        {
            Id.WriteTo(writer);

            // TODO: figure out a way to send version info over as well
            // info.Version.WriteTo(writer);

            writer.WriteString(Name);
            writer.WriteString(AssemblyName);
            writer.WriteString(Language);
            writer.WriteString(FilePath);
            writer.WriteString(OutputFilePath);
            writer.WriteString(OutputRefFilePath);
            CompilationOutputInfo.WriteTo(writer);
            writer.WriteString(DefaultNamespace);
            writer.WriteByte(checked((byte)ChecksumAlgorithm));
            writer.WriteBoolean(IsSubmission);
            writer.WriteBoolean(HasAllInformation);
            writer.WriteBoolean(RunAnalyzers);
            writer.WriteGuid(TelemetryId);

            // TODO: once CompilationOptions, ParseOptions, ProjectReference, MetadataReference, AnalyzerReference supports
            //       serialization, we should include those here as well.
        }

        public static ProjectAttributes ReadFrom(ObjectReader reader)
        {
            var projectId = ProjectId.ReadFrom(reader);

            // var version = VersionStamp.ReadFrom(reader);
            var name = reader.ReadRequiredString();
            var assemblyName = reader.ReadRequiredString();
            var language = reader.ReadRequiredString();
            var filePath = reader.ReadString();
            var outputFilePath = reader.ReadString();
            var outputRefFilePath = reader.ReadString();
            var compilationOutputFilePaths = CompilationOutputInfo.ReadFrom(reader);
            var defaultNamespace = reader.ReadString();
            var checksumAlgorithm = (SourceHashAlgorithm)reader.ReadByte();
            var isSubmission = reader.ReadBoolean();
            var hasAllInformation = reader.ReadBoolean();
            var runAnalyzers = reader.ReadBoolean();
            var telemetryId = reader.ReadGuid();

            return new ProjectAttributes(
                projectId,
                VersionStamp.Create(),
                name: name,
                assemblyName: assemblyName,
                language: language,
                compilationOutputFilePaths,
                checksumAlgorithm,
                defaultNamespace: defaultNamespace,
                filePath: filePath,
                outputFilePath: outputFilePath,
                outputRefFilePath: outputRefFilePath,
                telemetryId,
                isSubmission: isSubmission,
                hasAllInformation: hasAllInformation,
                runAnalyzers: runAnalyzers);
        }

        public Checksum Checksum
            => _lazyChecksum.Initialize(static @this => Checksum.Create(@this, static (@this, writer) => @this.WriteTo(writer)), this);
    }
}
