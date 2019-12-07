// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
    internal sealed class VSTypeScriptLanguageDebugInfoService : ILanguageDebugInfoService
    {
        private readonly IVSTypeScriptLanguageDebugInfoServiceImplementation _implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptLanguageDebugInfoService(IVSTypeScriptLanguageDebugInfoServiceImplementation implementation)
        {
            _implementation = implementation;
        }

        public async Task<DebugDataTipInfo> GetDataTipInfoAsync(Document document, int position, CancellationToken cancellationToken)
            => (await _implementation.GetDataTipInfoAsync(document, position, cancellationToken).ConfigureAwait(false)).UnderlyingObject;

        public async Task<DebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken)
            => (await _implementation.GetLocationInfoAsync(document, position, cancellationToken).ConfigureAwait(false)).UnderlyingObject;
    }
}
