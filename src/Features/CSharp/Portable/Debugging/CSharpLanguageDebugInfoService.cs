// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Debugging
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
