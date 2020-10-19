// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ProjectTelemetry;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Remote
{
    internal class RemoteProjectTelemetryIncrementalAnalyzer : IncrementalAnalyzerBase
    {
        /// <summary>
        /// Channel back to VS to inform it of the designer attributes we discover.
        /// </summary>
        private readonly RemoteCallback<IProjectTelemetryListener> _callback;

        private readonly object _gate = new object();
        private readonly Dictionary<ProjectId, ProjectTelemetryData> _projectToData = new Dictionary<ProjectId, ProjectTelemetryData>();

        public RemoteProjectTelemetryIncrementalAnalyzer(RemoteCallback<IProjectTelemetryListener> callback)
            => _callback = callback;

        /// <summary>
        /// Collects data from <paramref name="project"/> and reports it to the telemetry service.
        /// </summary>
        /// <remarks>
        /// Only sends data to the telemetry service when one of the collected data points changes, 
        /// not necessarily every time this code is called.
        /// </remarks>
        public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            if (!semanticsChanged)
                return;

            var projectId = project.Id;
            var language = project.Language;
            var analyzerReferencesCount = project.AnalyzerReferences.Count;
            var projectReferencesCount = project.AllProjectReferences.Count;
            var metadataReferencesCount = project.MetadataReferences.Count;
            var documentsCount = project.DocumentIds.Count;
            var additionalDocumentsCount = project.AdditionalDocumentIds.Count;

            var info = new ProjectTelemetryData(
                projectId: projectId,
                language: language,
                analyzerReferencesCount: analyzerReferencesCount,
                projectReferencesCount: projectReferencesCount,
                metadataReferencesCount: metadataReferencesCount,
                documentsCount: documentsCount,
                additionalDocumentsCount: additionalDocumentsCount);

            lock (_gate)
            {
                if (_projectToData.TryGetValue(projectId, out var existingInfo) &&
                    existingInfo.Equals(info))
                {
                    // already have reported this.  No need to notify VS.
                    return;
                }

                _projectToData[projectId] = info;
            }

            await _callback.InvokeAsync(
                (callback, cancellationToken) => callback.ReportProjectTelemetryDataAsync(info, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        public override Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _projectToData.Remove(projectId);
            }

            return Task.CompletedTask;
        }
    }
}
