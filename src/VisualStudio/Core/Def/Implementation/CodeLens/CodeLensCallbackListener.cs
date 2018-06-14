// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeLens
{
    /// <summary>
    /// This is used by new codelens API to get extra data from VS if it is needed.
    /// </summary>
    [Export(typeof(ICodeLensCallbackListener))]
    internal class CodeLensCallbackListener : ICodeLensCallbackListener
    {
        private const int DefaultMaxSearchResultsValue = 99;

        private const string CodeLensUserSettingsConfigPath = @"Text Editor\Global Options";
        private const string CodeLensMaxSearchResults = nameof(CodeLensMaxSearchResults);

        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IServiceProvider _serviceProvider;
        private readonly JoinableTaskContext _joinableTaskContext;

        private int _maxSearchResults = int.MinValue;

        [ImportingConstructor]
        public CodeLensCallbackListener(
            SVsServiceProvider serviceProvider,
            JoinableTaskContext joinableTaskContext,
            VisualStudioWorkspaceImpl workspace)
        {
            _serviceProvider = serviceProvider;
            _joinableTaskContext = joinableTaskContext;
            _workspace = workspace;
        }

        public async System.Threading.Tasks.Task SynchronizePrimaryWorkspaceAsync(CancellationToken cancellationToken)
        {
            var client = await _workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                throw new InvalidOperationException("remote host doesn't exist");
            }

            await _workspace.SynchronizePrimaryWorkspaceAsync(_workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> GetHostGroupIdAsync(CancellationToken cancellationToken)
        {
            // in VS host, RemoteHostClient is always ServiceHubRemoteHostClient
            var client = (ServiceHubRemoteHostClient)await _workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                throw new InvalidOperationException("remote host doesn't exist");
            }

            return client.HostGroupId;
        }

        public async Task<int> GetMaxResultCapAsync(CancellationToken cancellationToken)
        {
            await EnsureMaxResultAsync(cancellationToken).ConfigureAwait(false);

            return _maxSearchResults;
        }

        public Guid GetProjectId(Guid projectGuid, CancellationToken cancellationToken)
        {
            if (TryGetProjectId(_workspace, projectGuid, out var projectId))
            {
                return projectId.Id;
            }

            return Guid.Empty;
        }

        private async System.Threading.Tasks.Task EnsureMaxResultAsync(CancellationToken cancellationToken)
        {
            if (_maxSearchResults != int.MinValue)
            {
                return;
            }

            await _joinableTaskContext.Factory.SwitchToMainThreadAsync(cancellationToken);

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
            }
        }

        private bool TryGetProjectId(VisualStudioWorkspaceImpl workspace, Guid projectGuid, out ProjectId projectId)
        {
            if (projectGuid != VSConstants.CLSID.MiscellaneousFilesProject_guid)
            {
                foreach (var project in workspace.ProjectTracker.ImmutableProjects)
                {
                    if (project.Guid == projectGuid)
                    {
                        projectId = project.Id;
                        return true;
                    }
                }
            }

            projectId = null;
            return false;
        }
    }
}
