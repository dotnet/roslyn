// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// primary workspace for remote host. no one except solution service can update this workspace
    /// </summary>
    internal class RemoteWorkspace : Workspace
    {
        private readonly ISolutionCrawlerRegistrationService _registrationService;

        // guard to make sure host API doesn't run concurrently
        private readonly object _gate = new object();

        // this is used to make sure we never move remote workspace backward.
        // this version is the WorkspaceVersion of primary solution in client (VS) we are
        // currently caching
        private int _currentRemoteWorkspaceVersion = -1;

        public RemoteWorkspace()
            : base(RoslynServices.HostServices, workspaceKind: WorkspaceKind.RemoteWorkspace)
        {
            var exportProvider = (IMefHostExportProvider)Services.HostServices;
            var primaryWorkspace = exportProvider.GetExports<PrimaryWorkspace>().Single().Value;
            primaryWorkspace.Register(this);

            RegisterDocumentOptionProviders(exportProvider.GetExports<IDocumentOptionsProviderFactory, OrderableMetadata>());

            Options = Options.WithChangedOption(CacheOptions.RecoverableTreeLengthThreshold, 0);

            _registrationService = Services.GetService<ISolutionCrawlerRegistrationService>();
            _registrationService?.Register(this);
        }

        protected override void Dispose(bool finalize)
        {
            base.Dispose(finalize);

            _registrationService?.Unregister(this);
        }

        // constructor for testing
        public RemoteWorkspace(string workspaceKind)
            : base(RoslynServices.HostServices, workspaceKind: workspaceKind)
        {
        }

        // this workspace doesn't allow modification by calling TryApplyChanges.
        // consumer of solution is still free to fork solution as they want, they just can't apply those changes
        // back to primary workspace. only solution service can update primary workspace
        public override bool CanApplyChange(ApplyChangesKind feature) => false;

        // enables simulation of having documents open.
        public override bool CanOpenDocuments => true;

        /// <summary>
        /// Adds an entire solution to the workspace, replacing any existing solution.
        /// </summary>
        public bool TryAddSolutionIfPossible(SolutionInfo solutionInfo, int workspaceVersion, out Solution solution)
        {
            if (solutionInfo == null)
            {
                throw new ArgumentNullException(nameof(solutionInfo));
            }

            lock (_gate)
            {
                if (workspaceVersion <= _currentRemoteWorkspaceVersion)
                {
                    // we never move workspace backward
                    solution = null;
                    return false;
                }

                // set initial solution version
                _currentRemoteWorkspaceVersion = workspaceVersion;

                // clear previous solution data if there is one
                // it is required by OnSolutionAdded
                this.ClearSolutionData();

                this.OnSolutionAdded(solutionInfo);

                solution = this.CurrentSolution;
                return true;
            }
        }

        /// <summary>
        /// update primary solution
        /// </summary>
        public Solution UpdateSolutionIfPossible(Solution solution, int workspaceVersion)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            lock (_gate)
            {
                if (workspaceVersion <= _currentRemoteWorkspaceVersion)
                {
                    // we never move workspace backward
                    return solution;
                }

                // move version forward
                _currentRemoteWorkspaceVersion = workspaceVersion;

                var oldSolution = this.CurrentSolution;
                Contract.ThrowIfFalse(oldSolution.Id == solution.Id && oldSolution.FilePath == solution.FilePath);

                var newSolution = this.SetCurrentSolution(solution);
                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);

                return this.CurrentSolution;
            }
        }

        /// <summary>
        /// Puts the specified document into the open state.
        /// </summary>
        public override void OpenDocument(DocumentId documentId, bool activate = true)
        {
            lock (_gate)
            {
                var doc = this.CurrentSolution.GetDocument(documentId);
                if (doc != null)
                {
                    var text = doc.GetTextSynchronously(CancellationToken.None);
                    this.OnDocumentOpened(documentId, text.Container, activate);
                }
            }
        }

        /// <summary>
        /// Puts the specified document into the closed state.
        /// </summary>
        public override void CloseDocument(DocumentId documentId)
        {
            lock (_gate)
            {
                var doc = this.CurrentSolution.GetDocument(documentId);
                if (doc != null)
                {
                    var text = doc.GetTextSynchronously(CancellationToken.None);
                    var version = doc.GetTextVersionSynchronously(CancellationToken.None);
                    var loader = TextLoader.From(TextAndVersion.Create(text, version, doc.FilePath));
                    this.OnDocumentClosed(documentId, loader);
                }
            }
        }

        /// <summary>
        /// Puts the specified additional document into the open state.
        /// </summary>
        public override void OpenAdditionalDocument(DocumentId documentId, bool activate = true)
        {
            lock (_gate)
            {
                var doc = this.CurrentSolution.GetAdditionalDocument(documentId);
                if (doc != null)
                {
                    var text = doc.GetTextAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);
                    this.OnAdditionalDocumentOpened(documentId, text.Container, activate);
                }
            }
        }

        /// <summary>
        /// Puts the specified additional document into the closed state
        /// </summary>
        public override void CloseAdditionalDocument(DocumentId documentId)
        {
            lock (_gate)
            {
                var doc = this.CurrentSolution.GetAdditionalDocument(documentId);
                if (doc != null)
                {
                    var text = doc.GetTextAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);
                    var version = doc.GetTextVersionSynchronously(CancellationToken.None);
                    var loader = TextLoader.From(TextAndVersion.Create(text, version, doc.FilePath));
                    this.OnAdditionalDocumentClosed(documentId, loader);
                }
            }
        }
    }
}
