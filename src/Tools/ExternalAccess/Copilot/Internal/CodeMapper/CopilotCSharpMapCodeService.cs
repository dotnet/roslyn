// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.CodeMapper;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MapCode;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.CodeMapper
{
    [ExportLanguageService(typeof(IMapCodeService), language: LanguageNames.CSharp), Shared]
    internal sealed class CopilotCSharpMapCodeService : IMapCodeService
    {
        private readonly ICopilotCSharpMapCodeService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CopilotCSharpMapCodeService(ICopilotCSharpMapCodeService service)
        {
            _service = service;
        }

        public Task<Document?> MapCodeAsync(Document document, ImmutableArray<string> contents, ImmutableArray<(Document, TextSpan)> focusLocations, bool formatMappedCode, CancellationToken cancellationToken)
        {
            return _service.MapCodeAsync(document, contents, focusLocations, formatMappedCode, cancellationToken);
        }
    }
}
