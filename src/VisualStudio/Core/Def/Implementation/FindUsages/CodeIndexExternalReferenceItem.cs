// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindUsages
{
    internal partial class VisualStudioFindSymbolMonikerUsagesService
    {
        private class CodeIndexExternalReferenceItem : ExternalReferenceItem
        {
            private readonly VisualStudioFindSymbolMonikerUsagesService _service;
            private readonly Uri _documentUri;

            public CodeIndexExternalReferenceItem(
                VisualStudioFindSymbolMonikerUsagesService service,
                DefinitionItem definition,
                Uri documentUri,
                string projectName,
                string displayPath,
                LinePositionSpan span,
                string text) : base(definition, projectName, displayPath, span, text)
            {
                _service = service;
                _documentUri = documentUri;
            }

            public override bool TryNavigateTo(bool isPreview)
            {
                // Cancel the navigation to any previous item the user was trying to navigate to.
                // Then try to navigate to this. Because it's async, and we're not, just assume it
                // will succeed.
                var cancellationToken = _service.CancelLastNavigationAndGetNavigationToken();
                _ = NavigateToAsync(isPreview: false, cancellationToken);
                return true;
            }

            private async Task NavigateToAsync(bool isPreview, CancellationToken cancellationToken)
            {
                // No way to report any errors thrown by OpenNavigationResultInEditorAsync.
                // So just catch and report through our watson system.
                try
                {
                    await _service._codeIndexProvider!.OpenNavigationResultInEditorAsync(
                        _documentUri, this.Span.Start.Line, this.Span.Start.Character, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e) when (FatalError.ReportWithoutCrash(e))
                {
                }
            }
        }
    }
}
