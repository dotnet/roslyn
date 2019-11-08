// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    /// <summary>
    /// Creates an <see cref="IIncrementalAnalyzer"/> that collects basic information on <see cref="Project"/> inputs
    /// and reports it to the <see cref="IVsTelemetryService"/>.
    /// </summary>
    /// <remarks>
    /// This includes data such an source file counts, project, metadata, and analyzer reference counts, and so on.
    /// </remarks>
    [ExportIncrementalAnalyzerProvider(nameof(ProjectTelemetryIncrementalAnalyzerProvider), new[] { WorkspaceKind.Host }), Shared]
    internal sealed class ProjectTelemetryIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        [ImportingConstructor]
        public ProjectTelemetryIncrementalAnalyzerProvider()
        {
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Microsoft.CodeAnalysis.Workspace workspace)
        {
            return new Analyzer();
        }

        private sealed class Analyzer : IIncrementalAnalyzer
        {
            /// <summary>
            /// For a given <see cref="ProjectId"/>, stores the most recent set of data reported to the
            /// telemetry service.
            /// </summary>
            private sealed class Cache
            {
                private class Inputs
                {
                    public string Language;
                    public int AnalyzerReferencesCount;
                    public int ProjectReferencesCount;
                    public int MetadataReferencesCount;
                    public int DocumentsCount;
                    public int AdditionalDocumentsCount;

                    public Inputs(string language, int analyzerReferencesCount, int projectReferencesCount, int metadataReferencesCount, int documentsCount, int additionalDocumentsCount)
                    {
                        this.Language = language;
                        this.AnalyzerReferencesCount = analyzerReferencesCount;
                        this.ProjectReferencesCount = projectReferencesCount;
                        this.MetadataReferencesCount = metadataReferencesCount;
                        this.DocumentsCount = documentsCount;
                        this.AdditionalDocumentsCount = additionalDocumentsCount;
                    }

                    public bool Equals(Inputs other)
                    {
                        return this.Language.Equals(other.Language) && this is
                        {
                            AnalyzerReferencesCount: other.AnalyzerReferencesCount,
                            ProjectReferencesCount: other.ProjectReferencesCount,
                            MetadataReferencesCount: other.MetadataReferencesCount,
                            DocumentsCount: other.DocumentsCount,
                            AdditionalDocumentsCount: other.AdditionalDocumentsCount
                        };
                    }
                }

                private readonly object _lockObject = new object();

                private readonly Dictionary<ProjectId, Inputs> _items = new Dictionary<ProjectId, Inputs>();

                /// <summary>
                /// Adds or updates the data for the <see cref="Project"/> indicated by <paramref name="projectId"/>.
                /// </summary>
                /// <returns>
                /// True if the data was added or updated, false if the data matches what is already in the cache.
                /// </returns>
                public bool TryAddOrUpdate(ProjectId projectId, string language, int analyzerReferenceCount, int projectReferencesCount, int metadataReferencesCount, int documentsCount, int additionalDocumentsCount)
                {
                    lock (_lockObject)
                    {
                        var newInputs = new Inputs(
                            language,
                            analyzerReferenceCount,
                            projectReferencesCount,
                            metadataReferencesCount,
                            documentsCount,
                            additionalDocumentsCount);
                        if (!_items.TryGetValue(projectId, out var existingInputs) ||
                            !existingInputs.Equals(newInputs))
                        {
                            _items[projectId] = newInputs;
                            return true;
                        }

                        return false;
                    }
                }

                /// <summary>
                /// Removes all data associated with <paramref name="projectId"/>.
                /// </summary>
                public void Remove(ProjectId projectId)
                {
                    lock (_lockObject)
                    {
                        _items.Remove(projectId);
                    }
                }
            }

            private const string EventPrefix = "VS/Compilers/Compilation/";
            private const string PropertyPrefix = "VS.Compilers.Compilation.Inputs.";

            private const string TelemetryEventPath = EventPrefix + "Inputs";
            private const string TelemetryExceptionEventPath = EventPrefix + "TelemetryUnhandledException";

            private const string TelemetryProjectIdName = PropertyPrefix + "ProjectId";
            private const string TelemetryProjectGuidName = PropertyPrefix + "ProjectGuid";
            private const string TelemetryLanguageName = PropertyPrefix + "Language";
            private const string TelemetryAnalyzerReferencesCountName = PropertyPrefix + "AnalyzerReferences.Count";
            private const string TelemetryProjectReferencesCountName = PropertyPrefix + "ProjectReferences.Count";
            private const string TelemetryMetadataReferencesCountName = PropertyPrefix + "MetadataReferences.Count";
            private const string TelemetryDocumentsCountName = PropertyPrefix + "Documents.Count";
            private const string TelemetryAdditionalDocumentsCountName = PropertyPrefix + "AdditionalDocuments.Count";

            private readonly Cache _cache = new Cache();

            public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            /// <summary>
            /// Collects data from <paramref name="project"/> and reports it to the telemetry service.
            /// </summary>
            /// <remarks>
            /// Only sends data to the telemetry service when one of the collected data points changes, 
            /// not necessarily every time this code is called.
            /// </remarks>
            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                if (!semanticsChanged)
                {
                    return Task.CompletedTask;
                }

                var projectId = project.Id;
                var language = project.Language;
                var analyzerReferencesCount = project.AnalyzerReferences.Count;
                var projectReferencesCount = project.AllProjectReferences.Count;
                var metadataReferencesCount = project.MetadataReferences.Count;
                var documentsCount = project.DocumentIds.Count;
                var additionalDocumentsCount = project.AdditionalDocumentIds.Count;

                if (_cache.TryAddOrUpdate(projectId, language, analyzerReferencesCount, projectReferencesCount, metadataReferencesCount, documentsCount, additionalDocumentsCount))
                {
                    try
                    {
                        var workspace = (VisualStudioWorkspaceImpl)project.Solution.Workspace;

                        var telemetryEvent = TelemetryHelper.TelemetryService.CreateEvent(TelemetryEventPath);
                        telemetryEvent.SetStringProperty(TelemetryProjectIdName, projectId.Id.ToString());
                        // TODO: reconnect project GUID
                        telemetryEvent.SetStringProperty(TelemetryProjectGuidName, Guid.Empty.ToString());
                        telemetryEvent.SetStringProperty(TelemetryLanguageName, language);
                        telemetryEvent.SetIntProperty(TelemetryAnalyzerReferencesCountName, analyzerReferencesCount);
                        telemetryEvent.SetIntProperty(TelemetryProjectReferencesCountName, projectReferencesCount);
                        telemetryEvent.SetIntProperty(TelemetryMetadataReferencesCountName, metadataReferencesCount);
                        telemetryEvent.SetIntProperty(TelemetryDocumentsCountName, documentsCount);
                        telemetryEvent.SetIntProperty(TelemetryAdditionalDocumentsCountName, additionalDocumentsCount);

                        TelemetryHelper.DefaultTelemetrySession.PostEvent(telemetryEvent);
                    }
                    catch (Exception e)
                    {
                        // The telemetry service itself can throw.
                        // So, to be very careful, put this in a try/catch too.
                        try
                        {
                            var exceptionEvent = TelemetryHelper.TelemetryService.CreateEvent(TelemetryExceptionEventPath);
                            exceptionEvent.SetStringProperty("Type", e.GetTypeDisplayName());
                            exceptionEvent.SetStringProperty("Message", e.Message);
                            exceptionEvent.SetStringProperty("StackTrace", e.StackTrace);
                            TelemetryHelper.DefaultTelemetrySession.PostEvent(exceptionEvent);
                        }
                        catch
                        {
                        }
                    }
                }

                return Task.CompletedTask;
            }

            public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                return false;
            }

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public void RemoveDocument(DocumentId documentId)
            {
            }

            public void RemoveProject(ProjectId projectId)
            {
                _cache.Remove(projectId);
            }
        }
    }
}
