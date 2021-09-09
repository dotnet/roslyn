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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.CallstackExplorer;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CallstackExplorer
{
    internal class CallstackLineViewModel
    {
        private readonly ParsedLine _line;
        private readonly IThreadingContext _threadingContext;
        private readonly Workspace _workspace;
        private readonly IClassificationFormatMap _formatMap;
        private readonly ClassificationTypeMap _classificationTypeMap;

        private ISymbol? _cachedSymbol;
        private Document? _cachedDocument;
        private int _cachedLineNumber;

        public CallstackLineViewModel(
            ParsedLine line,
            IThreadingContext threadingContext,
            Workspace workspace,
            IClassificationFormatMap formatMap,
            ClassificationTypeMap classificationTypeMap)
        {
            _line = line;
            _threadingContext = threadingContext;
            _workspace = workspace;
            _formatMap = formatMap;
            _classificationTypeMap = classificationTypeMap;
        }

        public ImmutableArray<Inline> Inlines => CalculateInlines().ToImmutableArray();

        private IEnumerable<Inline> CalculateInlines()
        {
            var textUntilSymbol = _line.OriginalLine[.._line.ClassSpan.Start];
            yield return MakeClassifiedRun(ClassificationTypeNames.Text, textUntilSymbol);

            var classText = _line.OriginalLine[_line.ClassSpan.Start.._line.ClassSpan.End];

            var classLink = new Hyperlink();
            classLink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.ClassName, classText));
            classLink.Click += (s, a) => NavigateToClass();
            classLink.RequestNavigate += (s, a) => NavigateToClass();
            yield return classLink;

            // +1 to the argspan end because we want to include the closing paren
            var methodText = _line.OriginalLine[_line.MethodSpan.Start..(_line.ArgsSpan.End + 1)];
            var methodLink = new Hyperlink();
            var methodClassifiedText = new ClassifiedText(ClassificationTypeNames.MethodName, methodText);
            methodLink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.MethodName, methodText));
            methodLink.Click += (s, a) => NavigateToSymbol();
            methodLink.RequestNavigate += (s, a) => NavigateToSymbol();
            yield return methodLink;

            if (_line is FileLineResult fileLineResult)
            {
                var textBetweenLength = fileLineResult.FileSpan.Start - _line.ArgsSpan.End;
                var textBetweenSpan = new TextSpan(_line.ArgsSpan.End, textBetweenLength);
                if (textBetweenSpan.Length > 0)
                {
                    var textBetween = _line.OriginalLine.Substring(textBetweenSpan.Start, textBetweenSpan.Length);
                    yield return new Run(textBetween);
                }

                var fileText = _line.OriginalLine[fileLineResult.FileSpan.Start..fileLineResult.FileSpan.End];
                var fileHyperlink = new Hyperlink();
                fileHyperlink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.Text, fileText));
                fileHyperlink.RequestNavigate += (s, e) => NavigateToFile();
                fileHyperlink.Click += (s, e) => NavigateToFile();
                yield return fileHyperlink;

                var end = fileLineResult.FileSpan.End;
                if (end < _line.OriginalLine.Length)
                {
                    yield return MakeClassifiedRun(ClassificationTypeNames.Text, _line.OriginalLine[..end]);
                }
            }
            else
            {
                var end = _line.ArgsSpan.End;
                if (end < _line.OriginalLine.Length)
                {
                    yield return MakeClassifiedRun(ClassificationTypeNames.Text, _line.OriginalLine[..end]);
                }
            }
        }

        private Run MakeClassifiedRun(string classificationName, string text)
        {
            var classifiedText = new ClassifiedText(classificationName, text);
            return classifiedText.ToRun(_formatMap, _classificationTypeMap);
        }

        public void NavigateToClass()
        {
            var cancellationToken = _threadingContext.DisposalToken;
            Task.Run(() => NavigateToClassAsync(cancellationToken), cancellationToken);
        }

        public async Task NavigateToClassAsync(CancellationToken cancellationToken)
        {
            try
            {
                var symbol = await GetSymbolAsync(cancellationToken).ConfigureAwait(false);

                if (symbol is not { ContainingSymbol: not null })
                {
                    // Show some dialog?
                    return;
                }

                // Use the parent class instead of the method to navigate to
                symbol = symbol.ContainingSymbol;

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

                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                navigationService.TryNavigateToSpan(_workspace, document.Id, sourceLocation.SourceSpan, options, cancellationToken);
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
            {
            }
        }

        public void NavigateToSymbol()
        {
            var cancellationToken = _threadingContext.DisposalToken;
            Task.Run(() => NavigateToMethodAsync(cancellationToken), cancellationToken);
        }

        public async Task NavigateToMethodAsync(CancellationToken cancellationToken)
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

                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                navigationService.TryNavigateToSpan(_workspace, document.Id, sourceLocation.SourceSpan, options, cancellationToken);
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
            {
            }
        }

        public void NavigateToFile()
        {
            var cancellationToken = _threadingContext.DisposalToken;
            Task.Run(() => NavigateToFileAsync(cancellationToken), cancellationToken);
        }

        public async Task NavigateToFileAsync(CancellationToken cancellationToken)
        {
            try
            {
                var document = GetDocument(out var lineNumber);

                if (document is not null)
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

                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    navigationService.TryNavigateToLineAndOffset(_workspace, document.Id, lineNumber - 1, 0, cancellationToken);
                }
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
            {
            }
        }

        private Document? GetDocument(out int lineNumber)
        {
            if (_cachedDocument is not null)
            {
                lineNumber = _cachedLineNumber;
                return _cachedDocument;
            }

            var potentialMatches = GetFileMatches(out _cachedLineNumber);
            if (potentialMatches.Any())
            {
                _cachedDocument = potentialMatches.First();
            }

            lineNumber = _cachedLineNumber;
            return _cachedDocument;
        }

        private async Task<ISymbol?> GetSymbolAsync(CancellationToken cancellationToken)
        {
            if (_cachedSymbol is not null)
            {
                return _cachedSymbol;
            }

            _cachedSymbol = await _line.ResolveSymbolAsync(_workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);
            return _cachedSymbol;
        }

        private ImmutableArray<Document> GetFileMatches(out int lineNumber)
        {
            var fileLineResult = _line as FileLineResult;
            Contract.ThrowIfNull(fileLineResult);

            var fileText = _line.OriginalLine.Substring(fileLineResult.FileSpan.Start, fileLineResult.FileSpan.Length);
            Debug.Assert(fileText.Contains(':'));

            var splitIndex = fileText.LastIndexOf(':');

            var fileName = fileText[..splitIndex];
            var lineNumberText = fileText[(splitIndex + 1)..];

            var numberRegex = new Regex("[0-9]+");
            var match = numberRegex.Match(lineNumberText);
            lineNumber = int.Parse(match.Value);

            var documentName = Path.GetFileName(fileName);
            var potentialMatches = new HashSet<Document>();

            var solution = _workspace.CurrentSolution;
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (document.FilePath == fileName)
                    {
                        return ImmutableArray.Create(document);
                    }

                    else if (document.Name == documentName)
                    {
                        potentialMatches.Add(document);
                    }
                }
            }

            return potentialMatches.ToImmutableArray();
        }
    }
}
