// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
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
        private readonly ISolutionCrawlerRegistrationService? _registrationService;

        // guard to make sure host API doesn't run concurrently
        private readonly object _gate = new object();

        // this is used to make sure we never move remote workspace backward.
        // this version is the WorkspaceVersion of primary solution in client (VS) we are
        // currently caching
        private int _currentRemoteWorkspaceVersion = -1;

        public RemoteWorkspace()
            : this(applyStartupOptions: true)
        {
        }

        // internal for testing purposes.
        internal RemoteWorkspace(bool applyStartupOptions)
            : base(RoslynServices.HostServices, workspaceKind: WorkspaceKind.RemoteWorkspace)
        {
            var exportProvider = (IMefHostExportProvider)Services.HostServices;
            var primaryWorkspace = exportProvider.GetExports<PrimaryWorkspace>().Single().Value;
            primaryWorkspace.Register(this);

            RegisterDocumentOptionProviders(exportProvider.GetExports<IDocumentOptionsProviderFactory, OrderableMetadata>());

            if (applyStartupOptions)
                SetOptions(Options.WithChangedOption(CacheOptions.RecoverableTreeLengthThreshold, 0));

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

        public override bool CanOpenDocuments => false;

        /// <summary>
        /// Adds an entire solution to the workspace, replacing any existing solution.
        /// </summary>
        public bool TryAddSolutionIfPossible(SolutionInfo solutionInfo, int workspaceVersion, SerializableOptionSet options, [NotNullWhen(true)] out Solution? solution)
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

                SetOptions(options);

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

                SetOptions(newSolution.Options);

                return this.CurrentSolution;
            }
        }
    }
}
