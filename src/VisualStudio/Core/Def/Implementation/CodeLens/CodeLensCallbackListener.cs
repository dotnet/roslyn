// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
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
            var builder = ImmutableDictionary.CreateBuilder<Guid, string>();
            foreach (var project in _workspace.CurrentSolution.Projects)
            {
                var projectGuid = _workspace.GetProjectGuid(project.Id);
                if (!projectGuids.Contains(projectGuid))
                    continue;

                var projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
                builder[projectGuid] = projectVersion.ToString();
            }

            return builder.ToImmutable();
        }

        public async Task<ReferenceCount?> GetReferenceCountAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
        {
            var solution = _workspace.CurrentSolution;
            var (documentId, node) = await GetDocumentIdAndNodeAsync(
                solution, descriptor, descriptorContext.ApplicableSpan, cancellationToken).ConfigureAwait(false);
            if (documentId == null)
            {
                return null;
            }

            var maxSearchResults = await GetMaxResultCapAsync(cancellationToken).ConfigureAwait(false);

            var service = _workspace.Services.GetRequiredService<ICodeLensReferencesService>();
            return await service.GetReferenceCountAsync(solution, documentId, node, maxSearchResults, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<ReferenceLocationDescriptor>?> FindReferenceLocationsAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
        {
            var solution = _workspace.CurrentSolution;
            var (documentId, node) = await GetDocumentIdAndNodeAsync(
                solution, descriptor, descriptorContext.ApplicableSpan, cancellationToken).ConfigureAwait(false);
            if (documentId == null)
            {
                return null;
            }

            var service = _workspace.Services.GetRequiredService<ICodeLensReferencesService>();
            return await service.FindReferenceLocationsAsync(solution, documentId, node, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<ReferenceMethodDescriptor>?> FindReferenceMethodsAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken)
        {
            var solution = _workspace.CurrentSolution;
            var (documentId, node) = await GetDocumentIdAndNodeAsync(
                solution, descriptor, descriptorContext.ApplicableSpan, cancellationToken).ConfigureAwait(false);
            if (documentId == null)
            {
                return null;
            }

            var service = _workspace.Services.GetRequiredService<ICodeLensReferencesService>();
            return await service.FindReferenceMethodsAsync(solution, documentId, node, cancellationToken).ConfigureAwait(false);
        }

        private async Task<(DocumentId?, SyntaxNode?)> GetDocumentIdAndNodeAsync(
            Solution solution, CodeLensDescriptor descriptor, Span? span, CancellationToken cancellationToken)
        {
            if (span is null)
            {
                return default;
            }

            if (!TryGetDocument(solution, descriptor.ProjectGuid, descriptor.FilePath, out var document))
            {
                return default;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = span.Value.ToTextSpan();

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

        private bool TryGetDocument(Solution solution, Guid projectGuid, string filePath, [NotNullWhen(true)] out Document? document)
        {
            document = null;

            if (projectGuid == VSConstants.CLSID.MiscellaneousFilesProject_guid)
            {
                return false;
            }

            foreach (var candidateId in solution.GetDocumentIdsWithFilePath(filePath))
            {
                if (_workspace.GetProjectGuid(candidateId.ProjectId) == projectGuid)
                {
                    var currentContextId = _workspace.GetDocumentIdInCurrentContext(candidateId);
                    document = solution.GetDocument(currentContextId);
                    break;
                }
            }

            return document != null;
        }
    }
}
