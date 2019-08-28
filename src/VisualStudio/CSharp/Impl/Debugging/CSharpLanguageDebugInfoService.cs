// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.Debugging;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Debugging
{
    [ExportLanguageService(typeof(ILanguageDebugInfoService), LanguageNames.CSharp), Shared]
    internal partial class CSharpLanguageDebugInfoService : ILanguageDebugInfoService
    {
        [ImportingConstructor]
        public CSharpLanguageDebugInfoService()
        {
        }

        public Task<DebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            return LocationInfoGetter.GetInfoAsync(document, position, cancellationToken);
        }

        public Task<DebugDataTipInfo> GetDataTipInfoAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            return DataTipInfoGetter.GetInfoAsync(document, position, cancellationToken);
        }
    }
}
