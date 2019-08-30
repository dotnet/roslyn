// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor.Implementation.Debugging
{
    [Shared]
    [ExportLanguageService(typeof(ILanguageDebugInfoService), LanguageNames.FSharp)]
    internal class FSharpLanguageDebugInfoService : ILanguageDebugInfoService
    {
        private readonly IFSharpLanguageDebugInfoService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpLanguageDebugInfoService(IFSharpLanguageDebugInfoService service)
        {
            _service = service;
        }

        public async Task<CodeAnalysis.Editor.Implementation.Debugging.DebugDataTipInfo> GetDataTipInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var result = await _service.GetDataTipInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            return new CodeAnalysis.Editor.Implementation.Debugging.DebugDataTipInfo(result.Span, result.Text);
        }

        public async Task<CodeAnalysis.Editor.Implementation.Debugging.DebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var result = await _service.GetLocationInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (result.IsDefault)
            {
                return new CodeAnalysis.Editor.Implementation.Debugging.DebugLocationInfo();
            }
            else
            {
                return new CodeAnalysis.Editor.Implementation.Debugging.DebugLocationInfo(result.Name, result.LineOffset);
            }
        }
    }
}
