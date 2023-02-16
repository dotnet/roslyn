// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal class RazorDebugInfoService
    {
        public RazorDebugInfoService() { }
        internal async Task<RazorDebugDataTipInfo> GetDataTipInfoAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var languageDebugInfo = document.Project.Services.GetService<ILanguageDebugInfoService>();
            DebugDataTipInfo info = await languageDebugInfo!.GetDataTipInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            return new RazorDebugDataTipInfo(info.Span, info.Text);
        }
    }
}



