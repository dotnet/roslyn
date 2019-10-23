// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [Shared]
    [ExportLanguageService(typeof(ILanguageDebugInfoService), InternalLanguageNames.TypeScript)]
    internal class VSTypeScriptLanguageDebugInfoService : ILanguageDebugInfoService
    {
        private readonly IVSTypeScriptLanguageDebugInfoService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptLanguageDebugInfoService(IVSTypeScriptLanguageDebugInfoService service)
        {
            _service = service;
        }

        public async Task<DebugDataTipInfo> GetDataTipInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var result = await _service.GetDataTipInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            return new DebugDataTipInfo(result.Span, result.Text);
        }

        public async Task<DebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var result = await _service.GetLocationInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            return result.IsDefault ? default : new DebugLocationInfo(result.Name, result.LineOffset);
        }
    }
}
