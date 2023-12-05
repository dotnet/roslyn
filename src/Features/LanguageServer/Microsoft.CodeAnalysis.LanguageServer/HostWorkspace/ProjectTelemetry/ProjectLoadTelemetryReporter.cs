// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;

[Export, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class ProjectLoadTelemetryReporter(ILoggerFactory loggerFactory, ServerConfiguration serverConfiguration)
{
    private static readonly string s_hashedSessionId = VsTfmAndFileExtHashingAlgorithm.HashInput(Guid.NewGuid().ToString());

    private readonly ILogger _logger = loggerFactory.CreateLogger<ProjectLoadTelemetryReporter>();

    /// <summary>
    /// This is designed to report project telemetry in an extremely similar way to O#
    /// so that we are able to compare data accurately.
    /// See https://github.com/OmniSharp/omnisharp-roslyn/blob/b2e64c6006beed49460f063117793f42ab2a8a5c/src/OmniSharp.MSBuild/ProjectLoadListener.cs#L36
    /// </summary>
    public async Task ReportProjectLoadTelemetryAsync(Dictionary<ProjectFileInfo, (ImmutableArray<CommandLineReference> MetadataReferences, OutputKind OutputKind, bool HasUnresolvedDependencies)> projectFileInfos, ProjectToLoad projectToLoad, CancellationToken cancellationToken)
    {
        try
        {
            if (serverConfiguration.TelemetryLevel is null or "off")
            {
                return;
            }

            if (!projectFileInfos.Any())
            {
                return;
            }

            // Arbitrarily pick the first.  This is an existing problem with the telemetry event where we report multiple target frameworks
            // but only the data from one of the sets of possible outputkinds / references / content / etc.
            var firstInfo = projectFileInfos.First();
            var projectFileInfo = firstInfo.Key;
            var (metadataReferences, outputKind, _) = firstInfo.Value;

            // Matches O# behavior to not report this event if no references found.
            if (!metadataReferences.Any())
            {
                return;
            }

            var projectId = await GetProjectIdAsync(projectToLoad);
            var targetFrameworks = GetTargetFrameworks(projectFileInfos.Keys);

            var projectCapabilities = projectFileInfo.ProjectCapabilities;

            var hashedReferences = GetHashedReferences(metadataReferences);
            var fileCounts = GetUniqueHashedFileExtensionsAndCounts(projectFileInfo);
            var isSdkStyleProject = projectFileInfo.IsSdkStyle;

            var projectEvent = new ProjectLoadTelemetryEvent(
                ProjectId: projectId,
                SessionId: s_hashedSessionId,
                OutputKind: (int)outputKind,
                ProjectCapabilities: projectCapabilities,
                TargetFrameworks: targetFrameworks,
                References: hashedReferences,
                FileExtensions: fileCounts.Keys,
                FileCounts: fileCounts.Values,
                SdkStyleProject: isSdkStyleProject);

            await ReportEventAsync(projectEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            // Don't fail project loading because we failed to report telemetry.  Just log a warning and move on.
            _logger.LogWarning($"Failed to get project telemetry data: {ex.ToString()}");
        }
    }

    private static async Task ReportEventAsync(ProjectLoadTelemetryEvent telemetryEvent, CancellationToken cancellationToken)
    {
        var instance = LanguageServerHost.Instance;
        Contract.ThrowIfNull(instance, nameof(instance));
        var clientLanguageServerManager = instance.GetRequiredLspService<IClientLanguageServerManager>();
        await clientLanguageServerManager.SendNotificationAsync("workspace/projectConfigurationTelemetry", telemetryEvent, cancellationToken);
    }

    private static ImmutableDictionary<string, int> GetUniqueHashedFileExtensionsAndCounts(ProjectFileInfo projectFileInfo)
    {
        // Similar to O#, we report the content files + any non-generated source files.
        var contentFiles = projectFileInfo.ContentFilePaths;
        var sourceFiles = projectFileInfo.Documents
            .Concat(projectFileInfo.AdditionalDocuments)
            .Concat(projectFileInfo.AnalyzerConfigDocuments)
            .Where(d => !d.IsGenerated)
            .SelectAsArray(d => d.FilePath);
        var allFiles = contentFiles.Concat(sourceFiles);
        var fileCounts = new Dictionary<string, int>();
        foreach (var file in allFiles)
        {
            var fileExtension = Path.GetExtension(file);
            fileCounts[fileExtension] = fileCounts.GetOrAdd(fileExtension, 0) + 1;
        }

        return fileCounts.ToImmutableDictionary(kvp => VsTfmAndFileExtHashingAlgorithm.HashInput(kvp.Key), kvp => kvp.Value);
    }

    private static ImmutableArray<string> GetHashedReferences(ImmutableArray<CommandLineReference> metadataReferences)
    {
        return metadataReferences.SelectAsArray(GetHashedReferenceName);

        static string GetHashedReferenceName(CommandLineReference reference)
        {
            var lowerCaseName = Path.GetFileNameWithoutExtension(reference.Reference).ToLower();
            return VsReferenceHashingAlgorithm.HashInput(lowerCaseName);
        }
    }

    /// <summary>
    /// This reads the solution file project id or hashes the contents+path
    /// Matches O# implementation - https://github.com/OmniSharp/omnisharp-roslyn/blob/master/src/OmniSharp.MSBuild/ProjectLoadListener.cs#L88
    /// </summary>
    private static async Task<string> GetProjectIdAsync(ProjectToLoad projectToLoad)
    {
        if (projectToLoad.ProjectGuid is not null)
        {
            // The projectId is formatted as {GUID}.
            // In order to match with O#, we need just the guid.
            var projectGuid = projectToLoad.ProjectGuid.Replace("{", string.Empty).Replace("}", string.Empty);

            // No need to actually hash the project guid.
            return projectGuid;
        }

        var content = await File.ReadAllTextAsync(projectToLoad.Path);
        // This should exactly match O# to ensure we get the same hashes.
        return VsReferenceHashingAlgorithm.HashInput($"Filename: {Path.GetFileName(projectToLoad.Path)}\n{content}");
    }

    private static ImmutableArray<string> GetTargetFrameworks(IEnumerable<ProjectFileInfo> projectFileInfos)
    {
        return projectFileInfos.Select(p => p.TargetFramework?.ToLower()).WhereNotNull().ToImmutableArray();
    }
}
