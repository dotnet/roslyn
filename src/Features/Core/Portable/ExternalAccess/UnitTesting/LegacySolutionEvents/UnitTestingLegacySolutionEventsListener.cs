// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LegacySolutionEvents;

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

        public ValueTask OnWorkspaceChangedEventAsync(WorkspaceChangeEventArgs args, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask OnTextDocumentOpenedAsync(TextDocumentEventArgs args, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask OnTextDocumentClosedAsync(TextDocumentEventArgs args, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
