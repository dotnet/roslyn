// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Roslyn.ComponentDebugger
{
    [Export]
    [AppliesTo("(" + ProjectCapabilities.CSharp + " | " + ProjectCapabilities.VB + ") & !" + ProjectCapabilities.SharedAssetsProject)]
    public class CommandLineArgumentsDataSource : UnconfiguredProjectHostBridge<IProjectVersionedValue<IProjectSubscriptionUpdate>, IProjectVersionedValue<ImmutableArray<string>>, IProjectVersionedValue<ImmutableArray<string>>>
    {
        private readonly IActiveConfiguredProjectSubscriptionService _activeProjectSubscriptionService;

        [ImportingConstructor]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "MEF ensures not null")]
        public CommandLineArgumentsDataSource(IProjectThreadingService projectThreadingService, IActiveConfiguredProjectSubscriptionService activeProjectSubscriptionService)
            : base(projectThreadingService.JoinableTaskContext)
        {
            _activeProjectSubscriptionService = activeProjectSubscriptionService;
        }

        public async Task<ImmutableArray<string>> GetArgsAsync()
        {
            using (JoinableCollection.Join())
            {
                await this.InitializeAsync().ConfigureAwait(true);
                return this.AppliedValue?.Value ?? ImmutableArray<string>.Empty;
            }
        }

        protected override bool BlockInitializeOnFirstAppliedValue => true;

        protected override Task InitializeInnerCoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> targetBlock)
        {
            JoinUpstreamDataSources(_activeProjectSubscriptionService.ProjectBuildRuleSource);
            return _activeProjectSubscriptionService.ProjectBuildRuleSource.SourceBlock.LinkTo(target: targetBlock,
                                                                                              linkOptions: new DataflowLinkOptions { PropagateCompletion = true },
                                                                                              initialDataAsNew: true,
                                                                                              suppressVersionOnlyUpdates: true,
                                                                                              ruleNames: Constants.CommandLineArgsRuleName);
        }

        protected override Task<IProjectVersionedValue<ImmutableArray<string>>> PreprocessAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> input, IProjectVersionedValue<ImmutableArray<string>>? previousOutput)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var description = input.Value.ProjectChanges[Constants.CommandLineArgsRuleName];
            return Task.FromResult<IProjectVersionedValue<ImmutableArray<string>>>(new ProjectVersionedValue<ImmutableArray<string>>(description.After.Items.Keys.ToImmutableArray(), input.DataSourceVersions));
        }

        protected override Task ApplyAsync(IProjectVersionedValue<ImmutableArray<string>> value)
        {
            AppliedValue = value;
            return Task.CompletedTask;
        }
    }
}
