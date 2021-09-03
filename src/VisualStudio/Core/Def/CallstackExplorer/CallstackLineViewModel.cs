// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

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

            if (_line is FileLineResult fileLineResult)
            {
                var textBetweenLength = fileLineResult.FileSpan.Start - _line.SymbolSpan.End;
                var textBetweenSpan = new TextSpan(_line.SymbolSpan.End, textBetweenLength);
                if (textBetweenSpan.Length > 0)
                {
                    var textBetween = _line.OriginalLine.Substring(textBetweenSpan.Start, textBetweenSpan.Length);
                    yield return new Run(textBetween);
                }

                var fileText = _line.OriginalLine.Substring(fileLineResult.FileSpan.Start, fileLineResult.FileSpan.Length);
                var fileHyperlink = new Hyperlink();
                fileHyperlink.Inlines.Add(fileText);
                fileHyperlink.RequestNavigate += (s, e) => NavigateToFile();
                fileHyperlink.Click += (s, e) => NavigateToFile();
                yield return fileHyperlink;

                var end = fileLineResult.FileSpan.End;
                if (end < _line.OriginalLine.Length)
                {
                    yield return new Run(_line.OriginalLine.Substring(end));
                }
            }
            else
            {
                var end = _line.SymbolSpan.End;
                if (end < _line.OriginalLine.Length)
                {
                    yield return new Run(_line.OriginalLine.Substring(end));
                }
            }
        }

        private void NavigateToFile()
        {
            var cancellationToken = _threadingContext.DisposalToken;
            Task.Run(async () =>
            {
                try
                {
                    var fileLineResult = _line as FileLineResult;
                    Contract.ThrowIfNull(fileLineResult);

                    var fileText = _line.OriginalLine.Substring(fileLineResult.FileSpan.Start, fileLineResult.FileSpan.Length);
                    Debug.Assert(fileText.Contains(':'));

                    var splitIndex = fileText.LastIndexOf(':');

                    var split = fileText.Split(':');
                    var fileName = fileText.Substring(0, splitIndex);
                    var lineNumberText = fileText.Substring(splitIndex + 1);

                    var numberRegex = new Regex("[0-9]+");
                    var match = numberRegex.Match(lineNumberText);
                    var lineNumber = int.Parse(match.Value);

                    var documentName = Path.GetFileName(fileName);
                    var potentialMatches = new HashSet<Document>();

                    var solution = _workspace.CurrentSolution;
                    foreach (var project in solution.Projects)
                    {
                        foreach (var document in project.Documents)
                        {
                            if (document.FilePath == fileName)
                            {
                                await NavigateToDocumentAsync(document, lineNumber).ConfigureAwait(false);
                                return;
                            }

                            else if (document.Name == documentName)
                            {
                                potentialMatches.Add(document);
                            }
                        }
                    }

                    // If the document didn't match exactly but we have potential matches, navigate
                    // to the first match available. This isn't great, but will work for now.
                    if (potentialMatches.Any())
                    {
                        var document = potentialMatches.First();
                        await NavigateToDocumentAsync(document, lineNumber).ConfigureAwait(false);
                    }


                    async Task NavigateToDocumentAsync(Document document, int lineNumber)
                    {
                        // While navigating do not activate the tab, which will change focus from the tool window
                        var options = _workspace.Options
                                .WithChangedOption(new OptionKey(NavigationOptions.PreferProvisionalTab), true)
                                .WithChangedOption(new OptionKey(NavigationOptions.ActivateTab), false);

                        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                        lineNumber = Math.Min(sourceText.Lines.Count, lineNumber);

                        var navigationService = _workspace.Services.GetService<IDocumentNavigationService>();
                        if (navigationService is null)
                        {
                            return;
                        }

                        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                        navigationService.TryNavigateToLineAndOffset(_workspace, document.Id, lineNumber - 1, 0, cancellationToken);
                    }
                }
                catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex))
                {
                    Debug.Assert(false);
                }
            }, cancellationToken);
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
