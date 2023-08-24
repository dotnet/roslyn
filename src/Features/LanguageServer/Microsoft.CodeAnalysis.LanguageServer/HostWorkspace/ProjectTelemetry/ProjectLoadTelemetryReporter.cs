// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
internal class ProjectLoadTelemetryReporter
{
    private static readonly Guid s_sessionId = Guid.NewGuid();

    /// <summary>
    /// This is designed to report project telemetry in an extremely similar way to O#
    /// so that we are able to compare data accurately.
    /// See https://github.com/OmniSharp/omnisharp-roslyn/blob/master/src/OmniSharp.MSBuild/ProjectLoadListener.cs#L36
    /// </summary>
    public static async Task ReportProjectLoadTelemetryAsync(ImmutableArray<string> metadataReferences, OutputKind? outputKind, ProjectFileInfo projectFileInfo, ProjectToLoad projectToLoad, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            // Matches O# behavior to not report this event if no references found.
            if (!metadataReferences.Any())
            {
                return;
            }

            var projectId = GetProjectId(projectToLoad);
            var sessionId = VsTfmAndFileExtHashingAlgorithm.HashInput(s_sessionId.ToString());
            // We have 1 project per tfm, so we'll report a telemetry event for each tfm.
            var targetFramework = projectFileInfo.TargetFramework ?? string.Empty;

            var projectCapabilities = projectFileInfo.ProjectTelemetryMetadata.ProjectCapabilities;

            var hashedReferences = GetHashedReferences(metadataReferences);
            var fileCounts = GetUniqueHashedFileExtensionsAndCounts(projectFileInfo);
            var isSdkStyleProject = projectFileInfo.ProjectTelemetryMetadata.IsSdkStyle;

            var projectEvent = new ProjectLoadTelemetryEvent(
                ProjectId: projectId.HashedValue,
                SessionId: sessionId.HashedValue,
                // Matches how O# reports output kind - default is 0 (ConsoleApplication)
                OutputKind: (int?)outputKind ?? 0,
                ProjectCapabilities: projectCapabilities.ToArray(),
                TargetFrameworks: new string[] { targetFramework },
                References: hashedReferences.Select(h => h.HashedValue).ToArray(),
                FileExtensions: fileCounts.Keys.Select(k => k.HashedValue).ToArray(),
                FileCounts: fileCounts.Values.ToArray(),
                SdkStyleProject: isSdkStyleProject);

            await ReportEventAsync(projectEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            // Don't fail project loading because we failed to report telemetry.  Just log a warning and move on.
            logger.LogWarning($"Failed to get project telemetry data: {ex.ToString()}");
        }
    }

    private static async Task ReportEventAsync(ProjectLoadTelemetryEvent telemetryEvent, CancellationToken cancellationToken)
    {
        var instance = LanguageServerHost.Instance;
        Contract.ThrowIfNull(instance, nameof(instance));
        var clientLanguageServerManager = instance.GetRequiredLspService<IClientLanguageServerManager>();
        await clientLanguageServerManager.SendNotificationAsync("workspace/projectConfiguration", telemetryEvent, cancellationToken);
    }

    private static ImmutableDictionary<HashedString, int> GetUniqueHashedFileExtensionsAndCounts(ProjectFileInfo projectFileInfo)
    {
        // Similar to O#, we report the content files + any non-generated source files.
        var contentFiles = projectFileInfo.ProjectTelemetryMetadata.ContentFilePaths;
        var sourceFiles = projectFileInfo.Documents
            .Concat(projectFileInfo.AdditionalDocuments)
            .Concat(projectFileInfo.AnalyzerConfigDocuments)
            .Where(d => !d.IsGenerated)
            .SelectAsArray(d => d.FilePath);
        var allFiles = contentFiles.Concat(sourceFiles);
        var filesCounts = allFiles.GroupBy(file => Path.GetExtension(file)).ToImmutableDictionary(kvp => VsTfmAndFileExtHashingAlgorithm.HashInput(kvp.Key), kvp => kvp.Count());
        return filesCounts;
    }

    private static ImmutableArray<HashedString> GetHashedReferences(ImmutableArray<string> metadataReferences)
    {
        return metadataReferences.SelectAsArray(VsReferenceHashingAlgorithm.HashInput);
    }

    /// <summary>
    /// This reads the solution file project id or hashes the contents+path
    /// Matches O# implementation - https://github.com/OmniSharp/omnisharp-roslyn/blob/master/src/OmniSharp.MSBuild/ProjectLoadListener.cs#L88
    /// </summary>
    private static HashedString GetProjectId(ProjectToLoad projectToLoad)
    {
        if (projectToLoad.ProjectGuid is not null)
        {
            // The projectId is formatted as {GUID}.
            // In order to match with O#, we need just the guid.
            var projectGuid = projectToLoad.ProjectGuid.Replace("{", string.Empty).Replace("}", string.Empty);

            // No need to actually hash the project guid.
            return new HashedString(projectGuid);
        }

        var content = File.ReadAllText(projectToLoad.Path);
        // This should exactly match O# to ensure we get the same hashes.
        return VsReferenceHashingAlgorithm.HashInput($"Filename: {Path.GetFileName(projectToLoad.Path)}\n{content}");
    }
}
