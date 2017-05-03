// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttributes;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Versions;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    internal partial class DesignerAttributeIncrementalAnalyzer : ForegroundThreadAffinitizedObject, IIncrementalAnalyzer
    {
        private const string StreamName = "<DesignerAttribute>";
        private const string FormatVersion = "3";

        private readonly IForegroundNotificationService _notificationService;

        private readonly IServiceProvider _serviceProvider;
        private readonly IAsynchronousOperationListener _listener;

        /// <summary>
        /// cache designer from UI thread
        /// 
        /// access this field through <see cref="GetDesignerFromForegroundThread"/>
        /// </summary>
        private IVSMDDesignerService _dotNotAccessDirectlyDesigner;

        private readonly ConcurrentDictionary<ProjectId, ImmutableDictionary<string, DesignerAttributeResult>> _lastReportedProjectData =
            new ConcurrentDictionary<ProjectId, ImmutableDictionary<string, DesignerAttributeResult>>();

        public DesignerAttributeIncrementalAnalyzer(
            IServiceProvider serviceProvider,
            IForegroundNotificationService notificationService,
            IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _serviceProvider = serviceProvider;
            Contract.ThrowIfNull(_serviceProvider);

            _notificationService = notificationService;

            _listener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.DesignerAttribute);
        }

        public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
        {
            return false;
        }

        public async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(project.IsFromPrimaryBranch());
            cancellationToken.ThrowIfCancellationRequested();

            var vsWorkspace = project.Solution.Workspace as VisualStudioWorkspaceImpl;
            if (vsWorkspace == null)
            {
                return;
            }

            if (!vsWorkspace.Options.GetOption(InternalFeatureOnOffOptions.DesignerAttributes))
            {
                return;
            }

            // CPS projects do not support designer attributes.  So we just skip these projects entirely.
            var isCPSProject = await Task.Factory.StartNew(
                () => vsWorkspace.IsCPSProject(project),
                cancellationToken,
                TaskCreationOptions.None,
                this.ForegroundTaskScheduler).ConfigureAwait(false);

            if (isCPSProject)
            {
                return;
            }

            var pathToResult = await TryAnalyzeProjectInRemoteProcessAsync(project, cancellationToken).ConfigureAwait(false);
            if (pathToResult == null)
            {
                pathToResult = await TryAnalyzeProjectInCurrentProcessAsync(project, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            RegisterDesignerAttributes(project, pathToResult);
        }

        private async Task<ImmutableDictionary<string, DesignerAttributeResult>> TryAnalyzeProjectInCurrentProcessAsync(
            Project project, CancellationToken cancellationToken)
        {
            // use tree version so that things like compiler option changes are considered
            var projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
            var semanticVersion = await project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            var designerAttributeData = await ReadExistingDataAsync(project, cancellationToken).ConfigureAwait(false);

            if (designerAttributeData == null ||
                !project.CanReusePersistedDependentSemanticVersion(projectVersion, semanticVersion, designerAttributeData.SemanticVersion))
            {
                designerAttributeData = await ComputeAndPersistDesignerAttributeDataAsync(
                    project, semanticVersion, cancellationToken).ConfigureAwait(false);
            }

            return designerAttributeData.PathToResult;
        }

        private async Task<DesignerAttributeData> ComputeAndPersistDesignerAttributeDataAsync(
            Project project, VersionStamp semanticVersion, CancellationToken cancellationToken)
        {
            var service = project.LanguageServices.GetService<IDesignerAttributeService>();

            var builder = ImmutableDictionary.CreateBuilder<string, DesignerAttributeResult>();
            foreach (var document in project.Documents)
            {
                var result = await service.ScanDesignerAttributesAsync(document, cancellationToken).ConfigureAwait(false);
                builder[document.FilePath] = result;
            }

            var data = new DesignerAttributeData(semanticVersion, builder.ToImmutable());
            PersistData(project, data, cancellationToken);
            return data;
        }

        private async Task<DesignerAttributeData> ReadExistingDataAsync(
            Project project, CancellationToken cancellationToken)
        {
            try
            {
                var solution = project.Solution;
                var workspace = project.Solution.Workspace;

                var storageService = workspace.Services.GetService<IPersistentStorageService>();
                using (var persistenceService = storageService.GetStorage(solution))
                using (var stream = await persistenceService.ReadStreamAsync(project, StreamName, cancellationToken).ConfigureAwait(false))
                using (var reader = ObjectReader.TryGetReader(stream, cancellationToken))
                {
                    if (reader != null)
                    {
                        var version = reader.ReadString();
                        if (version == FormatVersion)
                        {
                            var semanticVersion = VersionStamp.ReadFrom(reader);

                            var resultCount = reader.ReadInt32();
                            var builder = ImmutableDictionary.CreateBuilder<string, DesignerAttributeResult>();

                            for (var i = 0; i < resultCount; i++)
                            {
                                var filePath = reader.ReadString();
                                var attribute = reader.ReadString();
                                var containsErrors = reader.ReadBoolean();
                                var notApplicable = reader.ReadBoolean();

                                builder[filePath] = new DesignerAttributeResult(filePath, attribute, containsErrors, notApplicable);
                            }

                            return new DesignerAttributeData(semanticVersion, builder.ToImmutable());
                        }
                    }
                }
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }

            return null;
        }

        private void PersistData(
            Project project, DesignerAttributeData data, CancellationToken cancellationToken)
        {
            try
            {
                var solution = project.Solution;
                var workspace = project.Solution.Workspace;

                var storageService = workspace.Services.GetService<IPersistentStorageService>();
                using (var persistenceService = storageService.GetStorage(solution))
                using (var stream = SerializableBytes.CreateWritableStream())
                using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
                {
                    writer.WriteString(FormatVersion);
                    data.SemanticVersion.WriteTo(writer);

                    writer.WriteInt32(data.PathToResult.Count);

                    foreach (var kvp in data.PathToResult)
                    {
                        var result = kvp.Value;
                        writer.WriteString(result.FilePath);
                        writer.WriteString(result.DesignerAttributeArgument);
                        writer.WriteBoolean(result.ContainsErrors);
                        writer.WriteBoolean(result.NotApplicable);
                    }
                }
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }
        }

        private async Task<ImmutableDictionary<string, DesignerAttributeResult>> TryAnalyzeProjectInRemoteProcessAsync(Project project, CancellationToken cancellationToken)
        {
            using (var session = await TryGetRemoteSessionAsync(project.Solution, cancellationToken).ConfigureAwait(false))
            {
                if (session == null)
                {
                    return null;
                }

                var serializedResults = await session.InvokeAsync<DesignerAttributeResult[]>(
                    nameof(IRemoteDesignerAttributeService.ScanDesignerAttributesAsync), project.Id).ConfigureAwait(false);

                var data = serializedResults.ToImmutableDictionary(kvp => kvp.FilePath);
                return data;
            }
        }

        private static async Task<RemoteHostClient.Session> TryGetRemoteSessionAsync(
            Solution solution, CancellationToken cancellationToken)
        {
            var client = await solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }

            return await client.TryCreateCodeAnalysisServiceSessionAsync(
                solution, cancellationToken).ConfigureAwait(false);
        }

        private void RegisterDesignerAttributes(
            Project project, ImmutableDictionary<string, DesignerAttributeResult> pathToResult)
        {
            // Diff this result against the last result we reported for this project.
            // If there are any changes report them all at once to VS.
            var lastPathToResult = _lastReportedProjectData.GetOrAdd(
                project.Id, ImmutableDictionary<string, DesignerAttributeResult>.Empty);

            _lastReportedProjectData[project.Id] = pathToResult;

            var difference = GetDifference(lastPathToResult, pathToResult);
            if (difference.Count == 0)
            {
                return;
            }

            _notificationService.RegisterNotification(() =>
            {
                foreach (var document in project.Documents)
                {
                    if (difference.TryGetValue(document.FilePath, out var result))
                    {
                        RegisterDesignerAttribute(document, result.DesignerAttributeArgument);
                    }
                }
            }, _listener.BeginAsyncOperation("RegisterDesignerAttribute"));
        }

        private ImmutableDictionary<string, DesignerAttributeResult> GetDifference(
            ImmutableDictionary<string, DesignerAttributeResult> oldFileToResult,
            ImmutableDictionary<string, DesignerAttributeResult> newFileToResult)
        {
            var difference = ImmutableDictionary.CreateBuilder<string, DesignerAttributeResult>();

            foreach (var newKvp in newFileToResult)
            {
                // 1) If this result is for a new document.  We always need to report it
                // 2) If both the old and new data have this result, then report it if it is different.
                var filePath = newKvp.Key;
                var newResult = newKvp.Value;

                if (!oldFileToResult.TryGetValue(filePath, out var oldResult) ||
                    !newResult.Equals(oldResult))
                {
                    difference.Add(filePath, newResult);
                }
            }

            return difference.ToImmutable();
        }

        private void RegisterDesignerAttribute(Document document, string designerAttributeArgument)
        {
            var workspace = (VisualStudioWorkspaceImpl)document.Project.Solution.Workspace;

            var vsDocument = workspace.GetHostDocument(document.Id);
            if (vsDocument == null)
            {
                return;
            }

            uint itemId = vsDocument.GetItemId();
            if (itemId == (uint)VSConstants.VSITEMID.Nil)
            {
                // it is no longer part of the solution
                return;
            }

            if (ErrorHandler.Succeeded(vsDocument.Project.Hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ItemSubType, out var currentValue)))
            {
                var currentStringValue = string.IsNullOrEmpty(currentValue as string) ? null : (string)currentValue;
                if (string.Equals(currentStringValue, designerAttributeArgument, StringComparison.OrdinalIgnoreCase))
                {
                    // PERF: Avoid sending the message if the project system already has the current value.
                    return;
                }
            }

            try
            {
                var designer = GetDesignerFromForegroundThread();
                if (designer != null)
                {
                    designer.RegisterDesignViewAttribute(vsDocument.Project.Hierarchy, (int)itemId, dwClass: 0, pwszAttributeValue: designerAttributeArgument);
                }
            }
            catch
            {
                // DevDiv # 933717
                // turns out RegisterDesignViewAttribute can throw in certain cases such as a file failed to be checked out by source control
                // or IVSHierarchy failed to set a property for this project
                //
                // just swallow it. don't crash VS.
            }
        }

        private IVSMDDesignerService GetDesignerFromForegroundThread()
        {
            if (_dotNotAccessDirectlyDesigner != null)
            {
                return _dotNotAccessDirectlyDesigner;
            }

            AssertIsForeground();
            _dotNotAccessDirectlyDesigner = _serviceProvider.GetService(typeof(SVSMDDesignerService)) as IVSMDDesignerService;

            return _dotNotAccessDirectlyDesigner;
        }

        #region unused

        public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyTask;

        public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyTask;

        public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyTask;

        public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyTask;

        public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyTask;

        public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyTask;

        public void RemoveDocument(DocumentId documentId)
        {
        }

        public void RemoveProject(ProjectId projectId)
        {
        }

        #endregion
    }
}