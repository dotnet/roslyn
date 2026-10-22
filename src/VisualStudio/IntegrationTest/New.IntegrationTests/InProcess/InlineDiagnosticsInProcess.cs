// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.InlineDiagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess
{
    [TestService]
    internal partial class InlineDiagnosticsInProcess
    {
        private const string AdornmentLayerName = "RoslynInlineDiagnostics";

        public async Task EnableOptionsAsync(string languageName, CancellationToken cancellationToken)
        {
            var optionService = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
            optionService.SetGlobalOption(InlineDiagnosticsOptions.EnableInlineDiagnostics, languageName, true);
        }

        public async Task<(int, List<InlineDiagnosticsTag>)> EnsureInlineDiagnosticsCountAndLocation(CancellationToken cancellationToken)
        {
            var adornmentLayer = await GetAdornmentLayer(cancellationToken);
            var list = new List<InlineDiagnosticsTag>();
            foreach (var item in adornmentLayer.Elements)
            {
                list.Add((InlineDiagnosticsTag)item.Tag);
            }

            return (adornmentLayer.Elements.Count, list);
        }

        private async Task<IAdornmentLayer> GetAdornmentLayer(CancellationToken cancellationToken)
        {
            await WaitForApplicationIdleAsync(cancellationToken);
            var vsTextManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>(cancellationToken);
            var vsTextView = await vsTextManager.GetActiveViewAsync(JoinableTaskFactory, cancellationToken);
            var textViewHost = await vsTextView.GetTextViewHostAsync(JoinableTaskFactory, cancellationToken);
            return textViewHost.TextView.GetAdornmentLayer(AdornmentLayerName);
        }
    }
}
