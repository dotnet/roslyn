// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SpellChecking
{
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(WorkspaceSpellCheckingHandler)), Shared]
    internal class WorkspaceSpellCheckingHandlerProvider : AbstractRequestHandlerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceSpellCheckingHandlerProvider()
        {
        }

        public override ImmutableArray<IRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind)
            => ImmutableArray.Create<IRequestHandler>(new WorkspaceSpellCheckingHandler());
    }
}
