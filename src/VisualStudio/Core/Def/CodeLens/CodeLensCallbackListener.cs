// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.CodeLens
{
    /// <summary>
    /// This is used by new codelens API to get extra data from VS if it is needed.
    /// </summary>
    [Export(typeof(ICodeLensCallbackListener))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal class CodeLensCallbackListener : ICodeLensCallbackListener, ICodeLensContext
    {
        private const int DefaultMaxSearchResultsValue = 99;

        private const string CodeLensUserSettingsConfigPath = @"Text Editor\Global Options";
        private const string CodeLensMaxSearchResults = nameof(CodeLensMaxSearchResults);

        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IServiceProvider _serviceProvider;
        private readonly IThreadingContext _threadingContext;

        private int _maxSearchResults = int.MinValue;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeLensCallbackListener(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl workspace)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
            _workspace = workspace;
        }

        public async Task<ImmutableDictionary<Guid, string>> GetProjectVersionsAsync(ImmutableArray<Guid> projectGuids, CancellationToken cancellationToken)
        {
            var service = _workspace.Services.GetRequiredService<ICodeLensReferencesService>();

            var builder = ImmutableDictionary.CreateBuilder<Guid, string>();
            var solution = _workspace.CurrentSolution;
            foreach (var project in solution.Projects)
            {
                var projectGuid = _workspace.GetProjectGuid(project.Id);
                if (!projectGuids.Contains(projectGuid))
                    continue;

                var projectVersion = await service.GetProjectCodeLensVersionAsync(solution, project.Id, cancellationToken).ConfigureAwait(false);
                builder[projectGuid] = projectVersion.ToString();
            }

            return builder.ToImmutable();
        }

        public async Task<ReferenceCount?> GetReferenceCountAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, ReferenceCount? previousCount, CancellationToken cancellationToken)
        {
            var solution = _workspace.CurrentSolution;
            var (documentId, node) = await GetDocumentIdAndNodeAsync(
                solution, descriptor, descriptorContext, cancellationToken).ConfigureAwait(false);
            if (documentId == null)
            {
                return null;
            }

            var service = _workspace.Services.GetRequiredService<ICodeLensReferencesService>();
            if (previousCount is not null)
            {
                // Avoid calculating results if we already have a result for the current project version
                var currentProjectVersion = await service.GetProjectCodeLensVersionAsync(solution, documentId.ProjectId, cancellationToken).ConfigureAwait(false);
                if (previousCount.Value.Version == currentProjectVersion.ToString())
                {
                    return previousCount;
                }
            }

            var maxSearchResults = await GetMaxResultCapAsync(cancellationToken).ConfigureAwait(false);
            return await service.GetReferenceCountAsync(solution, documentId, node, maxSearchResults, cancellationToken).ConfigureAwait(false);
        }

        public async Task<(string projectVersion, ImmutableArray<ReferenceLocationDescriptor> references)?> FindReferenceLocationsAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
        {
            var solution = _workspace.CurrentSolution;
            var (documentId, node) = await GetDocumentIdAndNodeAsync(
                solution, descriptor, descriptorContext, cancellationToken).ConfigureAwait(false);
            if (documentId == null)
            {
                return null;
            }

            var service = _workspace.Services.GetRequiredService<ICodeLensReferencesService>();
            var references = await service.FindReferenceLocationsAsync(solution, documentId, node, cancellationToken).ConfigureAwait(false);
            if (!references.HasValue)
            {
                return null;
            }

            var projectVersion = await service.GetProjectCodeLensVersionAsync(solution, documentId.ProjectId, cancellationToken).ConfigureAwait(false);
            return (projectVersion.ToString(), references.Value);
        }

        public async Task<ImmutableArray<ReferenceMethodDescriptor>?> FindReferenceMethodsAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
        {
            var solution = _workspace.CurrentSolution;
            var (documentId, node) = await GetDocumentIdAndNodeAsync(
                solution, descriptor, descriptorContext, cancellationToken).ConfigureAwait(false);
            if (documentId == null)
            {
                return null;
            }

            var service = _workspace.Services.GetRequiredService<ICodeLensReferencesService>();
            return await service.FindReferenceMethodsAsync(solution, documentId, node, cancellationToken).ConfigureAwait(false);
        }

        private async Task<(DocumentId?, SyntaxNode?)> GetDocumentIdAndNodeAsync(
            Solution solution, CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
        {
            if (descriptorContext.ApplicableSpan is null)
            {
                return default;
            }

            var document = await GetDocumentAsync(solution, descriptor.ProjectGuid, descriptor.FilePath, descriptorContext).ConfigureAwait(false);
            if (document == null)
            {
                return default;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = descriptorContext.ApplicableSpan.Value.ToTextSpan();

            // TODO: This check avoids ArgumentOutOfRangeException but it's not clear if this is the right solution
            // https://github.com/dotnet/roslyn/issues/44639
            if (!root.FullSpan.Contains(textSpan))
            {
                return default;
            }

            return (document.Id, root.FindNode(textSpan));
        }

        private async Task<int> GetMaxResultCapAsync(CancellationToken cancellationToken)
        {
            await EnsureMaxResultAsync(cancellationToken).ConfigureAwait(false);

            return _maxSearchResults;
        }

        private async Task EnsureMaxResultAsync(CancellationToken cancellationToken)
        {
            if (_maxSearchResults != int.MinValue)
            {
                return;
            }

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var settingsManager = new ShellSettingsManager(_serviceProvider);
            var settingsStore = settingsManager.GetReadOnlySettingsStore(Settings.SettingsScope.UserSettings);

            try
            {
                // If 14.0\Text Editor\Global Options\CodeLensMaxSearchResults
                //     exists
                //           as a value other than Int 32 - disable the capping feature.
                //     exists
                //           as Int32 with value <= 0 - disable the feature
                //           as Int32 with value > 0 - enable the feature, cap at given `value`.
                //     does not exist
                //           - feature is on by default, cap at 99
                _maxSearchResults = settingsStore.GetInt32(CodeLensUserSettingsConfigPath, CodeLensMaxSearchResults, defaultValue: DefaultMaxSearchResultsValue);
            }
            catch (ArgumentException)
            {
                // guard against users possibly creating a value with datatype other than Int32
                _maxSearchResults = DefaultMaxSearchResultsValue;
            }
        }

        private Task<Document?> GetDocumentAsync(Solution solution, Guid projectGuid, string filePath, CodeLensDescriptorContext descriptorContext)
        {
            if (projectGuid == VSConstants.CLSID.MiscellaneousFilesProject_guid)
            {
                return SpecializedTasks.Default<Document>();
            }

            foreach (var candidateId in solution.GetDocumentIdsWithFilePath(filePath))
            {
                if (_workspace.GetProjectGuid(candidateId.ProjectId) == projectGuid)
                {
                    var currentContextId = _workspace.GetDocumentIdInCurrentContext(candidateId);
                    return Task.FromResult(solution.GetDocument(currentContextId));
                }
            }

            // If we couldn't find the document the usual way we did so, then maybe it's source generated; let's try locating it
            // with the DocumentId we have directly
            if (TryGetGuid("RoslynDocumentIdGuid", out var documentIdGuid) &&
                TryGetGuid("RoslynProjectIdGuid", out var projectIdGuid))
            {
                var projectId = ProjectId.CreateFromSerialized(projectIdGuid);
                var documentId = DocumentId.CreateFromSerialized(projectId, documentIdGuid);
                return _workspace.CurrentSolution.GetDocumentAsync(documentId, includeSourceGenerated: true).AsTask();
            }

            return SpecializedTasks.Default<Document>();

            bool TryGetGuid(string key, out Guid guid)
            {
                guid = Guid.Empty;
                return descriptorContext.Properties.TryGetValue(key, out var guidStringUntyped) &&
                    guidStringUntyped is string guidString &&
                    Guid.TryParse(guidString, out guid);
            }
        }
    }
}
