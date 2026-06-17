// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Xml;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Utilities;

namespace Microsoft.DotNet.FileBasedPrograms;

#if FILE_BASED_PROGRAMS_PUBLIC
public
#else
internal
#endif
sealed class VirtualProjectBuilder
{
    internal readonly record struct ExplicitProjectItem(string ItemType, string Include);

    internal const string FromIncludeDirectiveMetadataName = "FileBasedProgramsFromIncludeDirective";

    internal const string FromRefDirectiveMetadataName = "FileBasedProgramsFromRefDirective";

    /// <summary>
    /// Keeps a strong reference to the latest virtual <see cref="IProjectRootElement"/> created for each entry point in a <see cref="IProjectCollection"/>,
    /// preventing it from being garbage collected when MSBuild's <c>ProjectRootElementCache</c> demotes it to a weak reference.
    /// Without this, nested <c>&lt;MSBuild&gt;</c> tasks that re-evaluate virtual projects with different properties
    /// would fail to find the <see cref="IProjectRootElement"/> in the cache and try to load it from disk,
    /// resulting in MSB4025 because the virtual project file does not exist on disk.
    /// See <see href="https://github.com/dotnet/sdk/issues/52714"/>.
    /// </summary>
    private static readonly ConditionalWeakTable<IProjectCollection, Dictionary<string, IProjectRootElement>> s_projectRootsByProjectCollection = new();

    private readonly IBuildHost _buildHost;

    private readonly string _targetFramework;

    private readonly IEnumerable<(string name, string value)> _defaultProperties;

    private (ImmutableArray<CSharpDirective> Original, ImmutableArray<CSharpDirective> Evaluated)? _evaluatedDirectives;

    internal string EntryPointFileFullPath { get; }

    internal SourceFile EntryPointSourceFile
    {
        get
        {
            if (field == default)
            {
                field = SourceFile.Load(EntryPointFileFullPath);
            }

            return field;
        }
    }

    internal string ArtifactsPath
        => field ??= GetArtifactsPath(EntryPointFileFullPath);

    internal string[]? RequestedTargets { get; }

    internal VirtualProjectBuilder(
        IBuildHost buildHost,
        string entryPointFileFullPath,
        string targetFramework,
        string[]? requestedTargets = null,
        string? artifactsPath = null,
        SourceText? sourceText = null)
    {
        Debug.Assert(ExternalHelpers.IsPathFullyQualified(entryPointFileFullPath));

        _buildHost = buildHost;
        EntryPointFileFullPath = entryPointFileFullPath;
        RequestedTargets = requestedTargets;
        ArtifactsPath = artifactsPath;
        _targetFramework = targetFramework;
        _defaultProperties = GetDefaultProperties(targetFramework);

        if (sourceText != null)
        {
            EntryPointSourceFile = new SourceFile(entryPointFileFullPath, sourceText);
        }
    }

    /// <remarks>
    /// Kept in sync with the default <c>dotnet new console</c> project file (enforced by <c>DotnetProjectConvertTests.SameAsTemplate</c>).
    /// </remarks>
    internal static IEnumerable<(string name, string value)> GetDefaultProperties(string targetFramework) =>
    [
        ("OutputType", "Exe"),
        ("TargetFramework", targetFramework),
        ("ImplicitUsings", "enable"),
        ("Nullable", "enable"),
        ("PublishAot", "true"),
        ("PackAsTool", "true"),
    ];

    internal static string GetArtifactsPath(string entryPointFileFullPath)
    {
        // Include entry point file name so the directory name is not completely opaque.
        string fileName = Path.GetFileNameWithoutExtension(entryPointFileFullPath);
        string hash = Sha256Hasher.HashWithNormalizedCasing(entryPointFileFullPath);
        string directoryName = $"{fileName}-{hash}";

        return GetTempSubpath(directoryName);
    }

    private const string CsprojExtension = ".csproj";

    public static string GetVirtualProjectPath(string entryPointFilePath)
        => entryPointFilePath + CsprojExtension;

    public static bool TryGetEntryPointFilePathFromVirtualProjectPath(string projectPath, [NotNullWhen(returnValue: true)] out string? entryPointFilePath)
    {
        if (projectPath.EndsWith(CsprojExtension, StringComparison.OrdinalIgnoreCase))
        {
            entryPointFilePath = projectPath[..^CsprojExtension.Length];
            if (IsValidEntryPointPath(entryPointFilePath))
            {
                return true;
            }
        }

        entryPointFilePath = null;
        return false;
    }

    /// <summary>
    /// Parses a source file to extract property value from directives.
    /// </summary>
    /// <returns>Array of frameworks if TargetFrameworks is specified, or empty otherwise</returns>
    public static string? GetPropertyFromSourceFile(string sourceFilePath, string propertyName)
    {
        var sourceFile = SourceFile.Load(sourceFilePath);
        var directives = FileLevelDirectiveHelpers.FindDirectives(sourceFile, reportAllErrors: false, ErrorReporters.IgnoringReporter);

        // Return the first value. Conflicting duplicate directives are not supported.
        return directives.OfType<CSharpDirective.Property>()
            .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    /// <summary>
    /// Obtains a temporary subdirectory for file-based app artifacts, e.g., <c>/tmp/dotnet/runfile/</c>.
    /// </summary>
    internal static string GetTempSubdirectory()
    {
        // We want a location where permissions are expected to be restricted to the current user.
        string directory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.GetTempPath()
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException(FileBasedProgramsResources.EmptyTempPath);
        }

        return Path.Combine(directory, "dotnet", "runfile");
    }

    /// <summary>
    /// Obtains a specific temporary path in a subdirectory for file-based app artifacts, e.g., <c>/tmp/dotnet/runfile/{name}</c>.
    /// </summary>
    internal static string GetTempSubpath(string name)
    {
        return Path.Combine(GetTempSubdirectory(), name);
    }

    public static bool IsValidEntryPointPath(string entryPointFilePath)
    {
        if (!File.Exists(entryPointFilePath))
        {
            return false;
        }

        if (entryPointFilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if the first two characters are #!
        try
        {
            using var stream = File.OpenRead(entryPointFilePath);
            int first = stream.ReadByte();
            int second = stream.ReadByte();
            return first == '#' && second == '!';
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Evaluates <paramref name="directives"/> against a <paramref name="project"/> and the file system.
    /// </summary>
    /// <remarks>
    /// All directives that need some other evaluation (described below) are expanded as MSBuild expressions
    /// (i.e., <c>$()</c> and <c>@()</c> are substituted with property and item values, etc.).
    /// <para/>
    /// <c>#:project</c> directives are resolved to full project file paths
    /// (e.g., if the evaluated value is a directory, finds a project in that directory).
    /// <para/>
    /// <c>#:include</c>/<c>#:exclude</c> have their <see cref="CSharpDirective.IncludeOrExclude.ItemType"/> determined
    /// and relative paths resolved relative to their containing file.
    /// </remarks>
    private ImmutableArray<CSharpDirective> EvaluateDirectives(
        IProjectInstance project,
        ImmutableArray<CSharpDirective> directives,
        ErrorReporter reportError)
    {
        if (!directives.Any(static d => d is CSharpDirective.Project or CSharpDirective.IncludeOrExclude or CSharpDirective.Ref))
        {
            return directives;
        }

        var builder = ImmutableArray.CreateBuilder<CSharpDirective>(directives.Length);

        ImmutableArray<(string Extension, string ItemType)> mapping = default;

        foreach (var directive in directives)
        {
            switch (directive)
            {
                case CSharpDirective.Project projectDirective:
                    projectDirective = projectDirective.WithName(project.ExpandString(projectDirective.Name), CSharpDirective.Project.NameKind.Expanded);
                    projectDirective = projectDirective.EnsureProjectFilePath(reportError);

                    builder.Add(projectDirective);
                    break;

                case CSharpDirective.Ref refDirective:
                    refDirective = refDirective.WithName(project.ExpandString(refDirective.Name), CSharpDirective.Ref.NameKind.Expanded);
                    refDirective = refDirective.EnsureResolvedPath(reportError);

                    builder.Add(refDirective);
                    break;

                case CSharpDirective.IncludeOrExclude includeOrExcludeDirective:
                    var expandedPath = project.ExpandString(includeOrExcludeDirective.Name);
                    var fullPath = Path.Combine(Path.GetDirectoryName(includeOrExcludeDirective.Info.SourceFile.Path)!, expandedPath);
                    includeOrExcludeDirective = includeOrExcludeDirective.WithName(fullPath);

                    if (mapping.IsDefault)
                    {
                        mapping = GetItemMapping(project, reportError);
                    }

                    includeOrExcludeDirective = includeOrExcludeDirective.WithDeterminedItemType(reportError, mapping);

                    builder.Add(includeOrExcludeDirective);
                    break;

                default:
                    builder.Add(directive);
                    break;
            }
        }

        return builder.DrainToImmutable();
    }

    internal ImmutableArray<(string Extension, string ItemType)> GetItemMapping(IProjectInstance project, ErrorReporter reportError)
    {
        return CSharpDirective.IncludeOrExclude.ParseMapping(
            project.GetPropertyValue(CSharpDirective.IncludeOrExclude.MappingPropertyName),
            EntryPointSourceFile,
            reportError);
    }

    public static IProjectInstance CreateProjectInstance(
        IBuildHost buildHost,
        string entryPointFilePath,
        string targetFramework,
        IProjectCollection projectCollection,
        Action<string, int, string> errorReporter)
    {
        var builder = new VirtualProjectBuilder(buildHost, entryPointFilePath, targetFramework);

        builder.CreateProjectInstance(
            projectCollection,
            (text, path, textSpan, message, _) => errorReporter(path, text.Lines.GetLinePositionSpan(textSpan).Start.Line + 1, message),
            out var projectInstance,
            projectRootElement: out _,
            evaluatedDirectives: out _);

        return projectInstance;
    }

    internal void CreateProjectInstance(
        IProjectCollection projectCollection,
        ErrorReporter reportError,
        out IProjectInstance project,
        out IProjectRootElement projectRootElement,
        out ImmutableArray<CSharpDirective> evaluatedDirectives,
        ImmutableArray<CSharpDirective> directives = default,
        Action<IDictionary<string, string>>? addGlobalProperties = null,
        bool validateAllDirectives = false,
        HashSet<string>? processedRefFiles = null)
    {
        var directivesOriginal = directives;

        if (directives.IsDefault)
        {
            directives = FileLevelDirectiveHelpers.FindDirectives(EntryPointSourceFile, validateAllDirectives, reportError, checkDuplicates: false);
        }

        (string ProjectFileText, IProjectInstance ProjectInstance, IProjectRootElement ProjectRootElement)? lastProject = null;

        // If we evaluated directives previously (e.g., during restore), reuse them.
        // We don't use the additional properties from `addGlobalProperties`
        // during directive evaluation anyway, so the directives can be reused safely.
        if (_evaluatedDirectives is { } cached &&
            cached.Original == directivesOriginal)
        {
            evaluatedDirectives = cached.Evaluated;
            (project, projectRootElement) = CreateProjectInstanceNoEvaluation(
                projectCollection,
                evaluatedDirectives,
                addGlobalProperties);

            CheckDirectives(project, evaluatedDirectives, reportError);
            CreateReferencedVirtualProjects(projectCollection, evaluatedDirectives, reportError, validateAllDirectives, processedRefFiles);
            StoreProjectRootElement(projectCollection, EntryPointFileFullPath, projectRootElement);

            return;
        }

        var entryPointDirectory = Path.GetDirectoryName(EntryPointFileFullPath)!;
        var seenFiles = new HashSet<string>(StringComparer.Ordinal) { EntryPointFileFullPath };
        var filesToProcess = new Queue<string>();
        var evaluatedDirectiveBuilder = ImmutableArray.CreateBuilder<CSharpDirective>();
        var deduplicator = new DirectiveDeduplicator();

        do
        {
            var directivesForEvaluation = DeduplicateSdkDirectives(directives);

            // Create a project with properties from #:property directives so they can be expanded inside EvaluateDirectives.
            (project, projectRootElement) = CreateProjectInstanceNoEvaluation(
                projectCollection,
                [.. evaluatedDirectiveBuilder, .. directivesForEvaluation],
                addGlobalProperties);

            // Evaluate directives, e.g., determine item types for #:include/#:exclude from their file extension.
            var fileEvaluatedDirectives = EvaluateDirectives(project, directivesForEvaluation, reportError);

            // Detect duplicate directives across all files on evaluated directives. EvaluateDirectives only expands
            // #:project, #:ref, #:include, and #:exclude; #:property and #:package values are still unevaluated here.
            var deduplicatedFileEvaluatedDirectiveBuilder = ImmutableArray.CreateBuilder<CSharpDirective>(fileEvaluatedDirectives.Length);
            foreach (var directive in fileEvaluatedDirectives)
            {
                if (directive is CSharpDirective.Sdk)
                {
                    deduplicatedFileEvaluatedDirectiveBuilder.Add(directive);
                    continue;
                }

                if (directive is CSharpDirective.Named named)
                {
                    deduplicator.CheckDirective(named, reportError, out bool shouldKeep);
                    if (!shouldKeep)
                    {
                        continue;
                    }
                }

                deduplicatedFileEvaluatedDirectiveBuilder.Add(directive);
            }

            fileEvaluatedDirectives = deduplicatedFileEvaluatedDirectiveBuilder.DrainToImmutable();

            evaluatedDirectiveBuilder.AddRange(fileEvaluatedDirectives);

            if (fileEvaluatedDirectives != directives)
            {
                // This project will contain items from #:include/#:exclude directives which we will traverse recursively.
                (project, projectRootElement) = CreateProjectInstanceNoEvaluation(
                    projectCollection,
                    evaluatedDirectiveBuilder.ToImmutable(),
                    addGlobalProperties);
            }

            var compileItems = project.GetItems("Compile");
            foreach (var compileItem in compileItems)
            {
                var compilePath = Path.Combine(
                    entryPointDirectory,
                    compileItem.GetMetadataValue("FullPath"));
                if (seenFiles.Add(compilePath))
                {
                    filesToProcess.Enqueue(compilePath);
                }
            }
        }
        while (TryGetNextFileToProcess());

        evaluatedDirectives = evaluatedDirectiveBuilder.ToImmutable();
        _evaluatedDirectives = (directivesOriginal, evaluatedDirectives);

        CheckDirectives(project, evaluatedDirectives, reportError);
        CreateReferencedVirtualProjects(projectCollection, evaluatedDirectives, reportError, validateAllDirectives, processedRefFiles);
        StoreProjectRootElement(projectCollection, EntryPointFileFullPath, projectRootElement);

        bool TryGetNextFileToProcess()
        {
            while (filesToProcess.Count != 0)
            {
                var filePath = filesToProcess.Dequeue();
                if (!File.Exists(filePath))
                {
                    reportError(EntryPointSourceFile.Text, EntryPointSourceFile.Path, default, string.Format(FileBasedProgramsResources.IncludedFileNotFound, filePath));
                    continue;
                }

                var sourceFile = SourceFile.Load(filePath);
                directives = FileLevelDirectiveHelpers.FindDirectives(sourceFile, validateAllDirectives, reportError, checkDuplicates: false);
                return true;
            }

            return false;
        }

        // #:sdk directives become Sdk.props/Sdk.targets imports when creating the temporary project used for
        // directive evaluation, so identical duplicates must be removed before that project is created.
        ImmutableArray<CSharpDirective> DeduplicateSdkDirectives(ImmutableArray<CSharpDirective> directives)
        {
            if (!directives.Any(static directive => directive is CSharpDirective.Sdk))
            {
                return directives;
            }

            var builder = ImmutableArray.CreateBuilder<CSharpDirective>(directives.Length);
            var changed = false;

            foreach (var directive in directives)
            {
                if (directive is CSharpDirective.Sdk sdk)
                {
                    deduplicator.CheckDirective(sdk, reportError, out bool shouldKeep);
                    if (!shouldKeep)
                    {
                        changed = true;
                        continue;
                    }
                }

                builder.Add(directive);
            }

            return changed ? builder.DrainToImmutable() : directives;
        }

        (IProjectInstance, IProjectRootElement) CreateProjectInstanceNoEvaluation(
            IProjectCollection projectCollection,
            ImmutableArray<CSharpDirective> directives,
            Action<IDictionary<string, string>>? addGlobalProperties = null)
        {
            var projectFileWriter = new StringWriter();

            WriteProjectFile(
                projectFileWriter,
                directives,
                _defaultProperties,
                isVirtualProject: true,
                entryPointFilePath: EntryPointFileFullPath,
                artifactsPath: ArtifactsPath,
                includeRuntimeConfigInformation: RequestedTargets?.Any(static t => t is "Publish" or "Pack") != true);

            var projectFileText = projectFileWriter.ToString();

            // If nothing changed, reuse the previous project instance to avoid unnecessary re-evaluations.
            if (lastProject is { } cachedProject && cachedProject.ProjectFileText == projectFileText)
            {
                return (cachedProject.ProjectInstance, cachedProject.ProjectRootElement);
            }

            var projectRoot = CreateProjectRootElement(projectFileText, projectCollection);

            var globalProperties = projectCollection.GlobalProperties;
            if (addGlobalProperties is not null)
            {
                globalProperties = new Dictionary<string, string>(projectCollection.GlobalProperties, StringComparer.OrdinalIgnoreCase);
                addGlobalProperties(globalProperties);
            }

            var project = _buildHost.CreateProjectInstanceFromProjectRootElement(projectRoot, projectCollection, globalProperties);

            lastProject = (projectFileText, project, projectRoot);

            return (project, projectRoot);

            IProjectRootElement CreateProjectRootElement(string projectFileText, IProjectCollection projectCollection)
            {
                using var reader = new StringReader(projectFileText);
                using var xmlReader = XmlReader.Create(reader);
                var projectRoot = _buildHost.CreateProjectRootElement(xmlReader, projectCollection);
                projectRoot.FullPath = GetVirtualProjectPath(EntryPointFileFullPath);
                return projectRoot;
            }
        }
    }

    private static void StoreProjectRootElement(
        IProjectCollection projectCollection,
        string entryPointFileFullPath,
        IProjectRootElement projectRootElement)
    {
        var projectRoots = s_projectRootsByProjectCollection.GetValue(
            projectCollection,
            static _ => new Dictionary<string, IProjectRootElement>(StringComparer.OrdinalIgnoreCase));

        lock (projectRoots)
        {
            projectRoots[entryPointFileFullPath] = projectRootElement;
        }
    }

    /// <summary>
    /// Recursively creates virtual <see cref="IProjectRootElement"/>s for all <c>#:ref</c> directives
    /// so MSBuild can resolve <c>&lt;ProjectReference&gt;</c> items to them.
    /// </summary>
    private void CreateReferencedVirtualProjects(
        IProjectCollection projectCollection,
        ImmutableArray<CSharpDirective> directives,
        ErrorReporter reportError,
        bool validateAllDirectives,
        HashSet<string>? processedFiles)
    {
        if (!directives.Any(static d => d is CSharpDirective.Ref))
        {
            return;
        }

        processedFiles ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        processedFiles.Add(EntryPointFileFullPath);

        foreach (var refDirective in directives.OfType<CSharpDirective.Ref>())
        {
            Debug.Assert(refDirective.ResolvedPath is not null);

            if (refDirective.ResolvedPath is not { } resolvedPath)
            {
                continue;
            }

            if (!processedFiles.Add(resolvedPath))
            {
                continue;
            }

            var refBuilder = new VirtualProjectBuilder(_buildHost, resolvedPath, _targetFramework);
            refBuilder.CreateProjectInstance(
                projectCollection,
                reportError,
                project: out _,
                projectRootElement: out _,
                evaluatedDirectives: out _,
                validateAllDirectives: validateAllDirectives,
                processedRefFiles: processedFiles);
        }
    }

    private void CheckDirectives(
        IProjectInstance project,
        ImmutableArray<CSharpDirective> directives,
        ErrorReporter reportError)
    {
        bool? refEnabled = null;

        foreach (var directive in directives)
        {
            if (directive is CSharpDirective.Ref)
            {
                CheckFlagEnabled(ref refEnabled, CSharpDirective.Ref.ExperimentalFileBasedProgramEnableRefDirective, directive);
            }
        }

        void CheckFlagEnabled(ref bool? flag, string flagName, CSharpDirective directive)
        {
            bool value = flag ??= MSBuildUtilities.ConvertStringToBool(project.GetPropertyValue(flagName));

            if (!value)
            {
                reportError(
                    directive.Info.SourceFile.Text,
                    directive.Info.SourceFile.Path,
                    directive.Info.Span,
                    string.Format(FileBasedProgramsResources.ExperimentalFeatureDisabled, flagName));
            }
        }
    }

    internal static void WriteProjectFile(
        TextWriter writer,
        ImmutableArray<CSharpDirective> directives,
        IEnumerable<(string name, string value)> defaultProperties,
        bool isVirtualProject,
        string? entryPointFilePath = null,
        string? artifactsPath = null,
        bool includeRuntimeConfigInformation = true,
        string? userSecretsId = null,
        ImmutableArray<ExplicitProjectItem> explicitProjectItems = default)
    {
        Debug.Assert(userSecretsId == null || !isVirtualProject);

        int processedDirectives = 0;

        var sdkDirectives = directives.OfType<CSharpDirective.Sdk>();
        var propertyDirectives = directives.OfType<CSharpDirective.Property>();
        var packageDirectives = directives.OfType<CSharpDirective.Package>();
        var projectDirectives = directives.OfType<CSharpDirective.Project>();
        var refDirectives = directives.OfType<CSharpDirective.Ref>();
        var includeOrExcludeDirectives = directives.OfType<CSharpDirective.IncludeOrExclude>().ToArray();

        const string defaultSdkName = "Microsoft.NET.Sdk";
        string firstSdkName;
        string? firstSdkVersion;

        if (sdkDirectives.FirstOrDefault() is { } firstSdk)
        {
            firstSdkName = firstSdk.Name;
            firstSdkVersion = firstSdk.Version;
            processedDirectives++;
        }
        else
        {
            firstSdkName = defaultSdkName;
            firstSdkVersion = null;
        }

        if (isVirtualProject)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(artifactsPath));
            Debug.Assert(entryPointFilePath is not null);

            // Note that ArtifactsPath needs to be specified before Sdk.props
            // (usually it's recommended to specify it in Directory.Build.props
            // but importing Sdk.props manually afterwards also works).
            writer.WriteLine($"""
                <Project>

                  <PropertyGroup>
                    <IncludeProjectNameInArtifactsPaths>false</IncludeProjectNameInArtifactsPaths>
                    <ArtifactsPath>{EscapeValue(artifactsPath)}</ArtifactsPath>
                    <AssemblyName>{EscapeValue(Path.GetFileNameWithoutExtension(entryPointFilePath))}</AssemblyName>
                    <RootNamespace>$(AssemblyName)</RootNamespace>
                    <PublishDir>artifacts/$(AssemblyName)</PublishDir>
                    <PackageOutputPath>artifacts/$(AssemblyName)</PackageOutputPath>
                    <FileBasedProgram>true</FileBasedProgram>
                    <EntryPointFilePath>{EscapeValue(entryPointFilePath)}</EntryPointFilePath>
                    <FileBasedProgramsItemMapping>{CSharpDirective.IncludeOrExclude.DefaultMappingString}</FileBasedProgramsItemMapping>
                    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
                    <DisableDefaultItemsInProjectFolder>true</DisableDefaultItemsInProjectFolder>
                """);

            // Only set these to false when using the default SDK with no additional SDKs
            // to avoid including .resx and other files that are typically not expected in simple file-based apps.
            // When other SDKs are used (e.g., Microsoft.NET.Sdk.Web), keep the default behavior.
            bool usingOnlyDefaultSdk = firstSdkName == defaultSdkName && sdkDirectives.Count() <= 1;
            if (usingOnlyDefaultSdk)
            {
                writer.WriteLine("""
                        <EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>
                        <EnableDefaultNoneItems>false</EnableDefaultNoneItems>
                    """);
            }

            // Write default properties before importing SDKs so they can be overridden by SDKs
            // (and implicit build files which are imported by the default .NET SDK).
            foreach (var (name, value) in defaultProperties)
            {
                writer.WriteLine($"""
                        <{name}>{EscapeValue(value)}</{name}>
                    """);
            }

            writer.WriteLine($"""
                  </PropertyGroup>

                  <ItemGroup>
                    <Clean Include="{EscapeValue(artifactsPath)}/*" />
                  </ItemGroup>

                """);

            if (firstSdkVersion is null)
            {
                writer.WriteLine($"""
                      <Import Project="Sdk.props" Sdk="{EscapeValue(firstSdkName)}" />
                    """);
            }
            else
            {
                writer.WriteLine($"""
                      <Import Project="Sdk.props" Sdk="{EscapeValue(firstSdkName)}" Version="{EscapeValue(firstSdkVersion)}" />
                    """);
            }
        }
        else
        {
            string slashDelimited = firstSdkVersion is null
                ? firstSdkName
                : $"{firstSdkName}/{firstSdkVersion}";
            writer.WriteLine($"""
                <Project Sdk="{EscapeValue(slashDelimited)}">

                """);
        }

        foreach (var sdk in sdkDirectives.Skip(1))
        {
            if (isVirtualProject)
            {
                WriteImport(writer, "Sdk.props", sdk);
            }
            else if (sdk.Version is null)
            {
                writer.WriteLine($"""
                      <Sdk Name="{EscapeValue(sdk.Name)}" />
                    """);
            }
            else
            {
                writer.WriteLine($"""
                      <Sdk Name="{EscapeValue(sdk.Name)}" Version="{EscapeValue(sdk.Version)}" />
                    """);
            }

            processedDirectives++;
        }

        if (isVirtualProject || processedDirectives > 1)
        {
            writer.WriteLine();
        }

        // Write default and custom properties.
        {
            writer.WriteLine("""
                  <PropertyGroup>
                """);

            // First write the default properties except those specified by the user.
            if (!isVirtualProject)
            {
                var customPropertyNames = propertyDirectives
                    .Select(static d => d.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var (name, value) in defaultProperties)
                {
                    if (!customPropertyNames.Contains(name))
                    {
                        writer.WriteLine($"""
                                <{name}>{EscapeValue(value)}</{name}>
                            """);
                    }
                }

                if (userSecretsId != null && !customPropertyNames.Contains("UserSecretsId"))
                {
                    writer.WriteLine($"""
                            <UserSecretsId>{EscapeValue(userSecretsId)}</UserSecretsId>
                        """);
                }
            }

            // Write custom properties.
            foreach (var property in propertyDirectives)
            {
                writer.WriteLine($"""
                        <{property.Name}>{EscapeValue(property.Value)}</{property.Name}>
                    """);

                processedDirectives++;
            }

            // Write virtual-only properties which cannot be overridden.
            if (isVirtualProject)
            {
                writer.WriteLine("""
                        <RestoreUseStaticGraphEvaluation>false</RestoreUseStaticGraphEvaluation>
                        <Features>$(Features);FileBasedProgram</Features>
                    """);
            }

            writer.WriteLine("""
                  </PropertyGroup>

                """);
        }

        if (!isVirtualProject)
        {
            // In the real project, files are included by the conversion copying them to the output directory,
            // hence we don't need to transfer the #:include/#:exclude directives over by default.
            processedDirectives += includeOrExcludeDirectives.Length;
        }
        else if (includeOrExcludeDirectives.Length > 0)
        {
            writer.WriteLine("""
                  <ItemGroup>
                """);

            foreach (var includeOrExclude in includeOrExcludeDirectives)
            {
                processedDirectives++;

                var itemType = includeOrExclude.ItemType;

                if (itemType == null)
                {
                    // Before directives are evaluated, the item type is null.
                    // We still need to create the project (so that we can evaluate $() properties),
                    // but we can skip the items.
                    continue;
                }

                if (includeOrExclude.Kind == CSharpDirective.IncludeOrExcludeKind.Include)
                {
                    writer.WriteLine($"""
                        <{itemType} Include="{EscapeValue(includeOrExclude.Name)}" {FromIncludeDirectiveMetadataName}="true" />
                    """);
                }
                else
                {
                    writer.WriteLine($"""
                        <{itemType} Remove="{EscapeValue(includeOrExclude.Name)}" />
                    """);
                }
            }

            writer.WriteLine("""
                  </ItemGroup>

                """);
        }

        if (!explicitProjectItems.IsDefaultOrEmpty)
        {
            writer.WriteLine("""
                  <ItemGroup>
                """);

            foreach (var (itemType, include) in explicitProjectItems)
            {
                writer.WriteLine($"""
                        <{itemType} Include="{EscapeValue(include)}" />
                    """);
            }

            writer.WriteLine("""
                  </ItemGroup>

                """);
        }

        if (packageDirectives.Any())
        {
            writer.WriteLine("""
                  <ItemGroup>
                """);

            foreach (var package in packageDirectives)
            {
                if (package.Version is null)
                {
                    writer.WriteLine($"""
                            <PackageReference Include="{EscapeValue(package.Name)}" />
                        """);
                }
                else
                {
                    writer.WriteLine($"""
                            <PackageReference Include="{EscapeValue(package.Name)}" Version="{EscapeValue(package.Version)}" />
                        """);
                }

                processedDirectives++;
            }

            writer.WriteLine("""
                  </ItemGroup>

                """);
        }

        if (projectDirectives.Any() || refDirectives.Any())
        {
            writer.WriteLine("""
                  <ItemGroup>
                """);

            foreach (var projectReference in projectDirectives)
            {
                writer.WriteLine($"""
                        <ProjectReference Include="{EscapeValue(projectReference.Name)}" />
                    """);

                processedDirectives++;
            }

            foreach (var refDirective in refDirectives)
            {
                if (refDirective.ResolvedPath is not null)
                {
                    var virtualProjectPath = GetVirtualProjectPath(refDirective.ResolvedPath);
                    writer.WriteLine($"""
                            <ProjectReference Include="{EscapeValue(virtualProjectPath)}" {FromRefDirectiveMetadataName}="{EscapeValue(refDirective.ResolvedPath)}" />
                        """);
                }

                processedDirectives++;
            }

            writer.WriteLine("""
                  </ItemGroup>

                """);
        }

        Debug.Assert(processedDirectives + directives.OfType<CSharpDirective.Shebang>().Count() == directives.Length);

        if (isVirtualProject)
        {
            Debug.Assert(entryPointFilePath is not null);

            // We Exclude existing Compile items (which could be added e.g.
            // in Microsoft.NET.Sdk.DefaultItems.props when user sets EnableDefaultCompileItems=true,
            // or above via #:include/#:exclude directives).
            writer.WriteLine($"""
                  <ItemGroup>
                    <Compile Include="{EscapeValue(entryPointFilePath)}" Exclude="@(Compile)" />
                  </ItemGroup>

                """);

            if (includeRuntimeConfigInformation)
            {
                var entryPointDirectory = Path.GetDirectoryName(entryPointFilePath) ?? "";
                writer.WriteLine($"""
                      <ItemGroup>
                        <RuntimeHostConfigurationOption Include="EntryPointFilePath" Value="{EscapeValue(entryPointFilePath)}" />
                        <RuntimeHostConfigurationOption Include="EntryPointFileDirectoryPath" Value="{EscapeValue(entryPointDirectory)}" />
                      </ItemGroup>

                    """);
            }

            foreach (var sdk in sdkDirectives)
            {
                WriteImport(writer, "Sdk.targets", sdk);
            }

            if (!sdkDirectives.Any())
            {
                Debug.Assert(firstSdkName == defaultSdkName && firstSdkVersion == null);
                writer.WriteLine($"""
                      <Import Project="Sdk.targets" Sdk="{defaultSdkName}" />
                    """);
            }

            writer.WriteLine();
        }

        writer.WriteLine("""
            </Project>
            """);

        static string EscapeValue(string value) => SecurityElement.Escape(value);

        static void WriteImport(TextWriter writer, string project, CSharpDirective.Sdk sdk)
        {
            if (sdk.Version is null)
            {
                writer.WriteLine($"""
                      <Import Project="{EscapeValue(project)}" Sdk="{EscapeValue(sdk.Name)}" />
                    """);
            }
            else
            {
                writer.WriteLine($"""
                      <Import Project="{EscapeValue(project)}" Sdk="{EscapeValue(sdk.Name)}" Version="{EscapeValue(sdk.Version)}" />
                    """);
            }
        }
    }
}
