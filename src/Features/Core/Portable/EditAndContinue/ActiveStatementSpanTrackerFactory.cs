// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal sealed class ActiveStatementSpanTrackerFactory : IActiveStatementSpanTrackerFactory
    {
        [ExportWorkspaceServiceFactory(typeof(IActiveStatementSpanTrackerFactory)), Shared]
        private sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory() { }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService CreateService(HostWorkspaceServices services)
                => new ActiveStatementSpanTrackerFactory(services);
        }

        private readonly IActiveStatementSpanTracker _tracker;

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public ActiveStatementSpanTrackerFactory(HostWorkspaceServices services)
        {
            _tracker = new Tracker();
        }

        public IActiveStatementSpanTracker GetOrCreateActiveStatementSpanTracker()
        {
            return _tracker;
        }

        private sealed class Tracker : IActiveStatementSpanTracker
        {
            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public Tracker()
            {
            }

            public bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span)
            {
                // TODO:
                span = default;
                return false;
            }
        }
    }
}
