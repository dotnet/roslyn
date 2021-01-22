// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportLspRequestHandlerProvider, Shared]
    internal class OnAutoInsertHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly ImmutableArray<IBraceCompletionService> _csharpBraceCompletionServices;
        private readonly ImmutableArray<IBraceCompletionService> _visualBasicBraceCompletionServices;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OnAutoInsertHandlerProvider(
            [ImportMany(LanguageNames.CSharp)] IEnumerable<IBraceCompletionService> csharpBraceCompletionServices,
            [ImportMany(LanguageNames.VisualBasic)] IEnumerable<IBraceCompletionService> visualBasicBraceCompletionServices)
        {
            _csharpBraceCompletionServices = csharpBraceCompletionServices.ToImmutableArray();
            _visualBasicBraceCompletionServices = _visualBasicBraceCompletionServices.ToImmutableArray();
        }

        protected override IEnumerable<IRequestHandler> InitializeHandlers()
        {
            return ImmutableArray.Create(new OnAutoInsertHandler(_csharpBraceCompletionServices, _visualBasicBraceCompletionServices));
        }
    }
}
