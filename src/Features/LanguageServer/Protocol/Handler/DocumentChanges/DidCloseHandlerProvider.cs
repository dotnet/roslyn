// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges
{
    [ExportLspRequestHandlerProvider, Shared]
    internal class DidCloseHandlerProvider : AbstractRequestHandlerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DidCloseHandlerProvider()
        {
        }

        protected override IEnumerable<IRequestHandler> InitializeHandlers()
        {
            return ImmutableArray.Create(new DidCloseHandler());
        }
    }
}
