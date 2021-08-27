// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Navigation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.CallstackExplorer;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CallstackExplorer
{
    internal class CallstackLineViewModel
    {
        private readonly ParsedLine _line;
        private readonly IThreadingContext _threadingContext;
        private readonly Workspace _workspace;

        public CallstackLineViewModel(ParsedLine line, IThreadingContext threadingContext, Workspace workspace)
        {
            _line = line;
            _threadingContext = threadingContext;
            _workspace = workspace;
        }

        public ImmutableArray<Inline> Inlines => CalculateInlines().ToImmutableArray();

        private IEnumerable<Inline> CalculateInlines()
        {
            var textUntilSymbol = _line.OriginalLine.Substring(0, _line.SymbolSpan.Start);
            yield return new Run(textUntilSymbol);

            var symbolText = _line.OriginalLine.Substring(_line.SymbolSpan.Start, _line.SymbolSpan.Length);
            var hyperlink = new Hyperlink();
            hyperlink.Inlines.Add(symbolText);
            hyperlink.RequestNavigate += (s, e) => NavigateToSymbol();
            hyperlink.Click += (s, e) => NavigateToSymbol();
            yield return hyperlink;

            var end = _line.SymbolSpan.End + 1;
            if (end < _line.OriginalLine.Length)
            {
                yield return new Run(_line.OriginalLine.Substring(end));
            }
        }

        private void NavigateToSymbol()
        {
            var cancellationToken = _threadingContext.DisposalToken;
            Task.Run(async () =>
                {
                    try
                    {
                        var symbol = await _line.ResolveSymbolAsync(_workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);

                        if (symbol is null)
                        {
                            // Show some dialog?
                            return;
                        }

                        var sourceLocation = symbol.Locations.FirstOrDefault(l => l.IsInSource);
                        if (sourceLocation is null || sourceLocation.SourceTree is null)
                        {
                            // Show some dialog?
                            return;
                        }

                        var navigationService = _workspace.Services.GetService<IDocumentNavigationService>();
                        if (navigationService is null)
                        {
                            return;
                        }

                        // While navigating do not activate the tab, which will change focus from the tool window
                        var options = _workspace.Options
                                .WithChangedOption(new OptionKey(NavigationOptions.PreferProvisionalTab), true)
                                .WithChangedOption(new OptionKey(NavigationOptions.ActivateTab), false);

                        var document = _workspace.CurrentSolution.GetRequiredDocument(sourceLocation.SourceTree);

                        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                        navigationService.TryNavigateToSpan(_workspace, document.Id, sourceLocation.SourceSpan, options, cancellationToken);
                    }
                    catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex))
                    {
                        Debug.Assert(false);
                    }
                }, cancellationToken);
        }
    }
}
