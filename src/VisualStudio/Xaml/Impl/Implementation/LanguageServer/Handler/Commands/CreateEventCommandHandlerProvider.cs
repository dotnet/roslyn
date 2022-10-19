// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Handler.Commands
{
    [ExportXamlLspRequestHandlerProvider(typeof(CreateEventCommandHandler)), Shared]
    internal class CreateEventCommandHandlerProvider : IRequestHandlerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CreateEventCommandHandlerProvider()
        {
        }

        public ImmutableArray<IRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind)
        {
            return ImmutableArray.Create<IRequestHandler>(new CreateEventCommandHandler());
        }
    }
}
