// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LegacySolutionEvents;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.LegacySolutionEvents
{
    [Export(typeof(ILegacySolutionEventsListener)), Shared]
    internal class UnitTestingLegacySolutionEventsListener : ILegacySolutionEventsListener
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnitTestingLegacySolutionEventsListener()
        {
        }

        private static IUnitTestingWorkCoordinator? GetCoordinator(ILegacyWorkspaceDescriptor descriptor)
        {
            var service = descriptor.SolutionServices.GetService<IUnitTestingSolutionCrawlerRegistrationService>();
            if (service == null)
                return null;

            return service.Register(descriptor);
        }

        public ValueTask OnWorkspaceChangedAsync(ILegacyWorkspaceDescriptor descriptor, WorkspaceChangeEventArgs args, CancellationToken cancellationToken)
        {
            var coordinator = GetCoordinator(descriptor);
            coordinator?.OnWorkspaceChanged(args);
            return ValueTaskFactory.CompletedTask;
        }

        public ValueTask OnTextDocumentOpenedAsync(ILegacyWorkspaceDescriptor descriptor, TextDocumentEventArgs args, CancellationToken cancellationToken)
        {
            var coordinator = GetCoordinator(descriptor);
            coordinator?.OnTextDocumentOpened(args);
            return ValueTaskFactory.CompletedTask;
        }

        public ValueTask OnTextDocumentClosedAsync(ILegacyWorkspaceDescriptor descriptor, TextDocumentEventArgs args, CancellationToken cancellationToken)
        {
            var coordinator = GetCoordinator(descriptor);
            coordinator?.OnTextDocumentClosed(args);
            return ValueTaskFactory.CompletedTask;
        }
    }
}
