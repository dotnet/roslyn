// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Handler.Diagnostics
{
    [ExportLspRequestHandlerProvider(StringConstants.XamlLanguageName), Shared]
    internal class WorkspacePullDiagnosticHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly IXamlPullDiagnosticService _xamlPullDiagnosticService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspacePullDiagnosticHandlerProvider(
            IXamlPullDiagnosticService xamlPullDiagnosticService)
        {
            _xamlPullDiagnosticService = xamlPullDiagnosticService;
        }

        protected override IEnumerable<IRequestHandler> InitializeHandlers()
        {
            return ImmutableArray.Create(new WorkspacePullDiagnosticHandler(_xamlPullDiagnosticService));
        }
    }
}
