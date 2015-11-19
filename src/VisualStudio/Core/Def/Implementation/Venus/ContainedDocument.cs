// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    /// <summary>
    /// An IVisualStudioDocument which represents the secondary buffer to the workspace API.
    /// </summary>
    internal sealed class ContainedDocument : ForegroundThreadAffinitizedObject, IVisualStudioHostDocument
    {
        private const string ReturnReplacementString = @"{|r|}";
        private const string NewLineReplacementString = @"{|n|}";

        private const string HTML = "HTML";
        private const string HTMLX = "HTMLX";
        private const string Razor = "Razor";
        private const string XOML = "XOML";

        private const char RazorExplicit = '@';

        private const string CSharpRazorBlock = "{";
        private const string VBRazorBlock = "code";

        private const string HelperRazor = "helper";
        private const string FunctionsRazor = "functions";

        private static readonly EditOptions s_venusEditOptions = new EditOptions(new StringDifferenceOptions
        {
            DifferenceType = StringDifferenceTypes.Character,
            IgnoreTrimWhiteSpace = false
        });

        private readonly AbstractContainedLanguage _containedLanguage;
        private readonly SourceCodeKind _sourceCodeKind;
        private readonly IComponentModel _componentModel;
        private readonly Workspace _workspace;
        private readonly ITextDifferencingSelectorService _differenceSelectorService;
        private readonly IOptionService _optionService;
        private readonly HostType _hostType;
        private readonly ReiteratedVersionSnapshotTracker _snapshotTracker;
        private readonly IFormattingRule _vbHelperFormattingRule;
        private readonly string _itemMoniker;

        public AbstractProject Project { get { return _containedLanguage.Project; } }
        public bool SupportsRename { get { return _hostType == HostType.Razor; } }

        public DocumentId Id { get; }
        public IReadOnlyList<string> Folders { get; }
        public TextLoader Loader { get; }
        public DocumentKey Key { get; }

        public ContainedDocument(
            AbstractContainedLanguage containedLanguage,
            SourceCodeKind sourceCodeKind,
            Workspace workspace,
            IVsHierarchy hierarchy,
            uint itemId,
            IComponentModel componentModel,
            IFormattingRule vbHelperFormattingRule)
        {
            Contract.ThrowIfNull(containedLanguage);

            _containedLanguage = containedLanguage;
            _sourceCodeKind = sourceCodeKind;
            _componentModel = componentModel;
            _workspace = workspace;
            _optionService = _workspace.Services.GetService<IOptionService>();
            _hostType = GetHostType();

            string filePath;
            if (!ErrorHandler.Succeeded(((IVsProject)hierarchy).GetMkDocument(itemId, out filePath)))
            {
                // we couldn't look up the document moniker from an hierarchy for an itemid.
                // Since we only use this moniker as a key, we could fall back to something else, like the document name.
                Debug.Assert(false, "Could not get the document moniker for an item from its hierarchy.");
                if (!hierarchy.TryGetItemName(itemId, out filePath))
                {
                    Environment.FailFast("Failed to get document moniker for a contained document");
                }
            }

            if (Project.Hierarchy != null)
            {
                string moniker;
                Project.Hierarchy.GetCanonicalName(itemId, out moniker);
                _itemMoniker = moniker;
            }

            this.Key = new DocumentKey(Project, filePath);
            this.Id = DocumentId.CreateNewId(Project.Id, filePath);
            this.Folders = containedLanguage.Project.GetFolderNames(itemId);
            this.Loader = TextLoader.From(containedLanguage.SubjectBuffer.AsTextContainer(), VersionStamp.Create(), filePath);
            _differenceSelectorService = componentModel.GetService<ITextDifferencingSelectorService>();
            _snapshotTracker = new ReiteratedVersionSnapshotTracker(_containedLanguage.SubjectBuffer);
            _vbHelperFormattingRule = vbHelperFormattingRule;
        }

        private HostType GetHostType()
        {
            var projectionBuffer = _containedLanguage.DataBuffer as IProjectionBuffer;
            if (projectionBuffer != null)
            {
                // For TypeScript hosted in HTML the source buffers will have type names
                // HTMLX and TypeScript. RazorCSharp has an HTMLX base type but should 
                // not be associated with the HTML host type. Use ContentType.TypeName 
                // instead of ContentType.IsOfType for HTMLX to ensure the Razor host 
                // type is identified correctly.
                if (projectionBuffer.SourceBuffers.Any(b => b.ContentType.IsOfType(HTML) ||
                    string.Compare(HTMLX, b.ContentType.TypeName, StringComparison.OrdinalIgnoreCase) == 0))
                {
                    return HostType.HTML;
                }

                if (projectionBuffer.SourceBuffers.Any(b => b.ContentType.IsOfType(Razor)))
                {
                    return HostType.Razor;
                }
            }
            else
            {
                // XOML is set up differently. For XOML, the secondary buffer (i.e. SubjectBuffer)
                // is a projection buffer, while the primary buffer (i.e. DataBuffer) is not. Instead,
                // the primary buffer is a regular unprojected ITextBuffer with the HTML content type.
                if (_containedLanguage.DataBuffer.CurrentSnapshot.ContentType.IsOfType(HTML))
                {
                    return HostType.XOML;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        public DocumentInfo GetInitialState()
        {
            return DocumentInfo.Create(
                this.Id,
                this.Name,
                folders: this.Folders,
                sourceCodeKind: _sourceCodeKind,
                loader: this.Loader,
                filePath: this.Key.Moniker);
        }

        public bool IsOpen
        {
            get
            {
                return true;
            }
        }

#pragma warning disable 67

        public event EventHandler UpdatedOnDisk;
        public event EventHandler<bool> Opened;
        public event EventHandler<bool> Closing;

#pragma warning restore 67

        IVisualStudioHostProject IVisualStudioHostDocument.Project { get { return this.Project; } }

        public ITextBuffer GetOpenTextBuffer()
        {
            return _containedLanguage.SubjectBuffer;
        }

        public SourceTextContainer GetOpenTextContainer()
        {
            return this.GetOpenTextBuffer().AsTextContainer();
        }

        public IContentType ContentType
        {
            get
            {
                return _containedLanguage.SubjectBuffer.ContentType;
            }
        }

        public string Name
        {
            get
            {
                try
                {
                    return Path.GetFileName(this.FilePath);
                }
                catch (ArgumentException)
                {
                    return this.FilePath;
                }
            }
        }

        public SourceCodeKind SourceCodeKind
        {
            get
            {
                return _sourceCodeKind;
            }
        }

        public string FilePath
        {
            get
            {
                return Key.Moniker;
            }
        }

        public AbstractContainedLanguage ContainedLanguage
        {
            get
            {
                return _containedLanguage;
            }
        }

        public void Dispose()
        {
            _snapshotTracker.StopTracking(_containedLanguage.SubjectBuffer);
            this.ContainedLanguage.Dispose();
        }

        public DocumentId FindProjectDocumentIdWithItemId(uint itemidInsertionPoint)
        {
            return Project.GetCurrentDocuments().SingleOrDefault(d => d.GetItemId() == itemidInsertionPoint).Id;
        }

        public uint FindItemIdOfDocument(Document document)
        {
            return Project.GetDocumentOrAdditionalDocument(document.Id).GetItemId();
        }

        public void UpdateText(SourceText newText)
        {
            var subjectBuffer = (IProjectionBuffer)this.GetOpenTextBuffer();
            var originalSnapshot = subjectBuffer.CurrentSnapshot;
            var originalText = originalSnapshot.AsText();

            var changes = newText.GetTextChanges(originalText);

            IEnumerable<int> affectedVisibleSpanIndices = null;
            var editorVisibleSpansInOriginal = SharedPools.Default<List<TextSpan>>().AllocateAndClear();

            try
            {
                var originalDocument = _workspace.CurrentSolution.GetDocument(this.Id);

                editorVisibleSpansInOriginal.AddRange(GetEditorVisibleSpans());
                var newChanges = FilterTextChanges(originalText, editorVisibleSpansInOriginal, changes).ToList();
                if (newChanges.Count == 0)
                {
                    // no change to apply
                    return;
                }

                ApplyChanges(subjectBuffer, newChanges, editorVisibleSpansInOriginal, out affectedVisibleSpanIndices);
                AdjustIndentation(subjectBuffer, affectedVisibleSpanIndices);
            }
            finally
            {
                SharedPools.Default<HashSet<int>>().ClearAndFree((HashSet<int>)affectedVisibleSpanIndices);
                SharedPools.Default<List<TextSpan>>().ClearAndFree(editorVisibleSpansInOriginal);
            }
        }

        private IEnumerable<TextChange> FilterTextChanges(SourceText originalText, List<TextSpan> editorVisibleSpansInOriginal, IReadOnlyList<TextChange> changes)
        {
            // no visible spans or changes
            if (editorVisibleSpansInOriginal.Count == 0 || changes.Count == 0)
            {
                // return empty one
                yield break;
            }

            using (var pooledObject = SharedPools.Default<List<TextChange>>().GetPooledObject())
            {
                var changeQueue = pooledObject.Object;
                changeQueue.AddRange(changes);

                var spanIndex = 0;
                var changeIndex = 0;
                for (; spanIndex < editorVisibleSpansInOriginal.Count; spanIndex++)
                {
                    var visibleSpan = editorVisibleSpansInOriginal[spanIndex];
                    var visibleTextSpan = GetVisibleTextSpan(originalText, visibleSpan, uptoFirstAndLastLine: true);

                    for (; changeIndex < changeQueue.Count; changeIndex++)
                    {
                        var change = changeQueue[changeIndex];

                        // easy case first
                        if (change.Span.End < visibleSpan.Start)
                        {
                            // move to next change
                            continue;
                        }

                        if (visibleSpan.End < change.Span.Start)
                        {
                            // move to next visible span
                            break;
                        }

                        // make sure we are not replacing whitespace around start and at the end of visible span
                        if (WhitespaceOnEdges(originalText, visibleTextSpan, change))
                        {
                            continue;
                        }

                        if (visibleSpan.Contains(change.Span))
                        {
                            yield return change;
                            continue;
                        }

                        // now it is complex case where things are intersecting each other
                        var subChanges = GetSubTextChanges(originalText, change, visibleSpan).ToList();
                        if (subChanges.Count > 0)
                        {
                            if (subChanges.Count == 1 && subChanges[0] == change)
                            {
                                // we can't break it. not much we can do here. just don't touch and ignore this change
                                continue;
                            }

                            changeQueue.InsertRange(changeIndex + 1, subChanges);
                            continue;
                        }
                    }
                }
            }
        }

        private bool WhitespaceOnEdges(SourceText text, TextSpan visibleTextSpan, TextChange change)
        {
            if (!string.IsNullOrWhiteSpace(change.NewText))
            {
                return false;
            }

            if (change.Span.End <= visibleTextSpan.Start)
            {
                return true;
            }

            if (visibleTextSpan.End <= change.Span.Start)
            {
                return true;
            }

            return false;
        }

        private IEnumerable<TextChange> GetSubTextChanges(SourceText originalText, TextChange changeInOriginalText, TextSpan visibleSpanInOriginalText)
        {
            using (var changes = SharedPools.Default<List<TextChange>>().GetPooledObject())
            {
                var leftText = originalText.ToString(changeInOriginalText.Span);
                var rightText = changeInOriginalText.NewText;
                var offsetInOriginalText = changeInOriginalText.Span.Start;

                if (TryGetSubTextChanges(originalText, visibleSpanInOriginalText, leftText, rightText, offsetInOriginalText, changes.Object))
                {
                    return changes.Object.ToList();
                }

                return GetSubTextChanges(originalText, visibleSpanInOriginalText, leftText, rightText, offsetInOriginalText);
            }
        }

        private bool TryGetSubTextChanges(
            SourceText originalText, TextSpan visibleSpanInOriginalText, string leftText, string rightText, int offsetInOriginalText, List<TextChange> changes)
        {
            // these are expensive. but hopefully we don't hit this as much except the boundary cases.
            using (var leftPool = SharedPools.Default<List<TextSpan>>().GetPooledObject())
            using (var rightPool = SharedPools.Default<List<TextSpan>>().GetPooledObject())
            {
                var spansInLeftText = leftPool.Object;
                var spansInRightText = rightPool.Object;

                if (TryGetWhitespaceOnlyChanges(leftText, rightText, spansInLeftText, spansInRightText))
                {
                    for (var i = 0; i < spansInLeftText.Count; i++)
                    {
                        var spanInLeftText = spansInLeftText[i];
                        var spanInRightText = spansInRightText[i];
                        if (spanInLeftText.IsEmpty && spanInRightText.IsEmpty)
                        {
                            continue;
                        }

                        var spanInOriginalText = new TextSpan(offsetInOriginalText + spanInLeftText.Start, spanInLeftText.Length);

                        TextChange textChange;
                        if (TryGetSubTextChange(originalText, visibleSpanInOriginalText, rightText, spanInOriginalText, spanInRightText, out textChange))
                        {
                            changes.Add(textChange);
                        }
                    }

                    return true;
                }

                return false;
            }
        }

        private IEnumerable<TextChange> GetSubTextChanges(
            SourceText originalText, TextSpan visibleSpanInOriginalText, string leftText, string rightText, int offsetInOriginalText)
        {
            // these are expensive. but hopefully we don't hit this as much except the boundary cases.
            using (var leftPool = SharedPools.Default<List<ValueTuple<int, int>>>().GetPooledObject())
            using (var rightPool = SharedPools.Default<List<ValueTuple<int, int>>>().GetPooledObject())
            {
                var leftReplacementMap = leftPool.Object;
                var rightReplacementMap = rightPool.Object;

                string leftTextWithReplacement, rightTextWithReplacement;
                GetTextWithReplacements(leftText, rightText, leftReplacementMap, rightReplacementMap, out leftTextWithReplacement, out rightTextWithReplacement);

                var diffResult = DiffStrings(leftTextWithReplacement, rightTextWithReplacement);

                foreach (var difference in diffResult)
                {
                    var spanInLeftText = AdjustSpan(diffResult.LeftDecomposition.GetSpanInOriginal(difference.Left), leftReplacementMap);
                    var spanInRightText = AdjustSpan(diffResult.RightDecomposition.GetSpanInOriginal(difference.Right), rightReplacementMap);

                    var spanInOriginalText = new TextSpan(offsetInOriginalText + spanInLeftText.Start, spanInLeftText.Length);

                    TextChange textChange;
                    if (TryGetSubTextChange(originalText, visibleSpanInOriginalText, rightText, spanInOriginalText, spanInRightText.ToTextSpan(), out textChange))
                    {
                        yield return textChange;
                    }
                }
            }
        }

        private bool TryGetWhitespaceOnlyChanges(string leftText, string rightText, List<TextSpan> spansInLeftText, List<TextSpan> spansInRightText)
        {
            return TryGetWhitespaceGroup(leftText, spansInLeftText) && TryGetWhitespaceGroup(rightText, spansInRightText) && spansInLeftText.Count == spansInRightText.Count;
        }

        private bool TryGetWhitespaceGroup(string text, List<TextSpan> groups)
        {
            if (text.Length == 0)
            {
                groups.Add(new TextSpan(0, 0));
                return true;
            }

            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                switch (ch)
                {
                    case ' ':
                        if (!TextAt(text, i - 1, ' '))
                        {
                            start = i;
                        }

                        break;

                    case '\r':
                    case '\n':
                        if (i == 0)
                        {
                            groups.Add(TextSpan.FromBounds(0, 0));
                        }
                        else if (TextAt(text, i - 1, ' '))
                        {
                            groups.Add(TextSpan.FromBounds(start, i));
                        }
                        else if (TextAt(text, i - 1, '\n'))
                        {
                            groups.Add(TextSpan.FromBounds(start, i));
                        }

                        start = i + 1;
                        break;

                    default:
                        return false;
                }
            }

            if (start <= text.Length)
            {
                groups.Add(TextSpan.FromBounds(start, text.Length));
            }

            return true;
        }

        private bool TextAt(string text, int index, char ch)
        {
            if (index < 0 || text.Length <= index)
            {
                return false;
            }

            return text[index] == ch;
        }

        private bool TryGetSubTextChange(
            SourceText originalText, TextSpan visibleSpanInOriginalText,
            string rightText, TextSpan spanInOriginalText, TextSpan spanInRightText, out TextChange textChange)
        {
            textChange = default(TextChange);

            var visibleFirstLineInOriginalText = originalText.Lines.GetLineFromPosition(visibleSpanInOriginalText.Start);
            var visibleLastLineInOriginalText = originalText.Lines.GetLineFromPosition(visibleSpanInOriginalText.End);

            // skip easy case
            // 1. things are out of visible span
            if (!visibleSpanInOriginalText.IntersectsWith(spanInOriginalText))
            {
                return false;
            }

            // 2. there are no intersects
            var snippetInRightText = rightText.Substring(spanInRightText.Start, spanInRightText.Length);
            if (visibleSpanInOriginalText.Contains(spanInOriginalText) && visibleSpanInOriginalText.End != spanInOriginalText.End)
            {
                textChange = new TextChange(spanInOriginalText, snippetInRightText);
                return true;
            }

            // okay, more complex case. things are intersecting boundaries.
            var firstLineOfRightTextSnippet = snippetInRightText.GetFirstLineText();
            var lastLineOfRightTextSnippet = snippetInRightText.GetLastLineText();

            // there are 4 complex cases - these are all heuristic. not sure what better way I have. and the heuristic is heavily based on
            // text differ's behavior.

            // 1. it is a single line
            if (visibleFirstLineInOriginalText.LineNumber == visibleLastLineInOriginalText.LineNumber)
            {
                // don't do anything
                return false;
            }

            // 2. replacement contains visible spans
            if (spanInOriginalText.Contains(visibleSpanInOriginalText))
            {
                // header
                // don't do anything

                // body
                textChange = new TextChange(
                    TextSpan.FromBounds(visibleFirstLineInOriginalText.EndIncludingLineBreak, visibleLastLineInOriginalText.Start),
                    snippetInRightText.Substring(firstLineOfRightTextSnippet.Length, snippetInRightText.Length - firstLineOfRightTextSnippet.Length - lastLineOfRightTextSnippet.Length));

                // footer
                // don't do anything

                return true;
            }

            // 3. replacement intersects with start
            if (spanInOriginalText.Start < visibleSpanInOriginalText.Start &&
                visibleSpanInOriginalText.Start <= spanInOriginalText.End &&
                spanInOriginalText.End < visibleSpanInOriginalText.End)
            {
                // header
                // don't do anything

                // body
                if (visibleFirstLineInOriginalText.EndIncludingLineBreak <= spanInOriginalText.End)
                {
                    textChange = new TextChange(
                        TextSpan.FromBounds(visibleFirstLineInOriginalText.EndIncludingLineBreak, spanInOriginalText.End),
                        snippetInRightText.Substring(firstLineOfRightTextSnippet.Length));
                    return true;
                }

                return false;
            }

            // 4. replacement intersects with end
            if (visibleSpanInOriginalText.Start < spanInOriginalText.Start &&
                spanInOriginalText.Start <= visibleSpanInOriginalText.End &&
                visibleSpanInOriginalText.End <= spanInOriginalText.End)
            {
                // body
                if (spanInOriginalText.Start <= visibleLastLineInOriginalText.Start)
                {
                    textChange = new TextChange(
                        TextSpan.FromBounds(spanInOriginalText.Start, visibleLastLineInOriginalText.Start),
                        snippetInRightText.Substring(0, snippetInRightText.Length - lastLineOfRightTextSnippet.Length));
                    return true;
                }

                // footer
                // don't do anything

                return false;
            }

            // if it got hit, then it means there is a missing case
            throw ExceptionUtilities.Unreachable;
        }

        private IHierarchicalDifferenceCollection DiffStrings(string leftTextWithReplacement, string rightTextWithReplacement)
        {
            var diffService = _differenceSelectorService.GetTextDifferencingService(
                _workspace.Services.GetLanguageServices(this.Project.Language).GetService<IContentTypeLanguageService>().GetDefaultContentType());

            diffService = diffService ?? _differenceSelectorService.DefaultTextDifferencingService;
            return diffService.DiffStrings(leftTextWithReplacement, rightTextWithReplacement, s_venusEditOptions.DifferenceOptions);
        }

        private void GetTextWithReplacements(
            string leftText, string rightText,
            List<ValueTuple<int, int>> leftReplacementMap, List<ValueTuple<int, int>> rightReplacementMap,
            out string leftTextWithReplacement, out string rightTextWithReplacement)
        {
            // to make diff works better, we choose replacement strings that don't appear in both texts.
            var returnReplacement = GetReplacementStrings(leftText, rightText, ReturnReplacementString);
            var newLineReplacement = GetReplacementStrings(leftText, rightText, NewLineReplacementString);

            leftTextWithReplacement = GetTextWithReplacementMap(leftText, returnReplacement, newLineReplacement, leftReplacementMap);
            rightTextWithReplacement = GetTextWithReplacementMap(rightText, returnReplacement, newLineReplacement, rightReplacementMap);
        }

        private static string GetReplacementStrings(string leftText, string rightText, string initialReplacement)
        {
            if (leftText.IndexOf(initialReplacement, StringComparison.Ordinal) < 0 && rightText.IndexOf(initialReplacement, StringComparison.Ordinal) < 0)
            {
                return initialReplacement;
            }

            // okay, there is already one in the given text.
            const string format = "{{|{0}|{1}|{0}|}}";
            for (var i = 0; true; i++)
            {
                var replacement = string.Format(format, i.ToString(), initialReplacement);
                if (leftText.IndexOf(replacement, StringComparison.Ordinal) < 0 && rightText.IndexOf(replacement, StringComparison.Ordinal) < 0)
                {
                    return replacement;
                }
            }
        }

        private string GetTextWithReplacementMap(string text, string returnReplacement, string newLineReplacement, List<ValueTuple<int, int>> replacementMap)
        {
            var delta = 0;
            var returnLength = returnReplacement.Length;
            var newLineLength = newLineReplacement.Length;

            var sb = StringBuilderPool.Allocate();
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch == '\r')
                {
                    sb.Append(returnReplacement);
                    delta += returnLength - 1;
                    replacementMap.Add(ValueTuple.Create(i + delta, delta));
                    continue;
                }
                else if (ch == '\n')
                {
                    sb.Append(newLineReplacement);
                    delta += newLineLength - 1;
                    replacementMap.Add(ValueTuple.Create(i + delta, delta));
                    continue;
                }

                sb.Append(ch);
            }

            return StringBuilderPool.ReturnAndFree(sb);
        }

        private Span AdjustSpan(Span span, List<ValueTuple<int, int>> replacementMap)
        {
            var start = span.Start;
            var end = span.End;

            for (var i = 0; i < replacementMap.Count; i++)
            {
                var positionAndDelta = replacementMap[i];
                if (positionAndDelta.Item1 <= span.Start)
                {
                    start = span.Start - positionAndDelta.Item2;
                }

                if (positionAndDelta.Item1 <= span.End)
                {
                    end = span.End - positionAndDelta.Item2;
                }

                if (positionAndDelta.Item1 > span.End)
                {
                    break;
                }
            }

            return Span.FromBounds(start, end);
        }

        public IEnumerable<TextSpan> GetEditorVisibleSpans()
        {
            var subjectBuffer = (IProjectionBuffer)this.GetOpenTextBuffer();

            var projectionDataBuffer = _containedLanguage.DataBuffer as IProjectionBuffer;
            if (projectionDataBuffer != null)
            {
                return projectionDataBuffer.CurrentSnapshot
                    .GetSourceSpans()
                    .Where(ss => ss.Snapshot.TextBuffer == subjectBuffer)
                    .Select(s => s.Span.ToTextSpan())
                    .OrderBy(s => s.Start);
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<TextSpan>();
            }
        }

        private static void ApplyChanges(
            IProjectionBuffer subjectBuffer,
            IEnumerable<TextChange> changes,
            IList<TextSpan> visibleSpansInOriginal,
            out IEnumerable<int> affectedVisibleSpansInNew)
        {
            using (var edit = subjectBuffer.CreateEdit(s_venusEditOptions, reiteratedVersionNumber: null, editTag: null))
            {
                var affectedSpans = SharedPools.Default<HashSet<int>>().AllocateAndClear();
                affectedVisibleSpansInNew = affectedSpans;

                var currentVisibleSpanIndex = 0;
                foreach (var change in changes)
                {
                    // Find the next visible span that either overlaps or intersects with 
                    while (currentVisibleSpanIndex < visibleSpansInOriginal.Count &&
                           visibleSpansInOriginal[currentVisibleSpanIndex].End < change.Span.Start)
                    {
                        currentVisibleSpanIndex++;
                    }

                    // no more place to apply text changes
                    if (currentVisibleSpanIndex >= visibleSpansInOriginal.Count)
                    {
                        break;
                    }

                    var newText = change.NewText;
                    var span = change.Span.ToSpan();

                    edit.Replace(span, newText);

                    affectedSpans.Add(currentVisibleSpanIndex);
                }

                edit.Apply();
            }
        }

        private void AdjustIndentation(IProjectionBuffer subjectBuffer, IEnumerable<int> visibleSpanIndex)
        {
            if (!visibleSpanIndex.Any())
            {
                return;
            }

            var snapshot = subjectBuffer.CurrentSnapshot;
            var document = _workspace.CurrentSolution.GetDocument(this.Id);
            if (!document.SupportsSyntaxTree)
            {
                return;
            }

            var originalText = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            Contract.Requires(object.ReferenceEquals(originalText, snapshot.AsText()));

            var root = document.GetSyntaxRootAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);

            var editorOptionsFactory = _componentModel.GetService<IEditorOptionsFactoryService>();
            var editorOptions = editorOptionsFactory.GetOptions(_containedLanguage.DataBuffer);
            var options = _workspace.Options
                                        .WithChangedOption(FormattingOptions.UseTabs, root.Language, !editorOptions.IsConvertTabsToSpacesEnabled())
                                        .WithChangedOption(FormattingOptions.TabSize, root.Language, editorOptions.GetTabSize())
                                        .WithChangedOption(FormattingOptions.IndentationSize, root.Language, editorOptions.GetIndentSize());

            using (var pooledObject = SharedPools.Default<List<TextSpan>>().GetPooledObject())
            {
                var spans = pooledObject.Object;

                spans.AddRange(this.GetEditorVisibleSpans());
                using (var edit = subjectBuffer.CreateEdit(s_venusEditOptions, reiteratedVersionNumber: null, editTag: null))
                {
                    foreach (var spanIndex in visibleSpanIndex)
                    {
                        var rule = GetBaseIndentationRule(root, originalText, spans, spanIndex);

                        var visibleSpan = spans[spanIndex];
                        AdjustIndentationForSpan(document, edit, visibleSpan, rule, options);
                    }

                    edit.Apply();
                }
            }
        }

        private void AdjustIndentationForSpan(
            Document document, ITextEdit edit, TextSpan visibleSpan, IFormattingRule baseIndentationRule, OptionSet options)
        {
            var root = document.GetSyntaxRootAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);

            using (var rulePool = SharedPools.Default<List<IFormattingRule>>().GetPooledObject())
            using (var spanPool = SharedPools.Default<List<TextSpan>>().GetPooledObject())
            {
                var venusFormattingRules = rulePool.Object;
                var visibleSpans = spanPool.Object;

                venusFormattingRules.Add(baseIndentationRule);
                venusFormattingRules.Add(ContainedDocumentPreserveFormattingRule.Instance);

                var formattingRules = venusFormattingRules.Concat(Formatter.GetDefaultFormattingRules(document));

                var workspace = document.Project.Solution.Workspace;
                var changes = Formatter.GetFormattedTextChanges(
                    root, new TextSpan[] { CommonFormattingHelpers.GetFormattingSpan(root, visibleSpan) },
                    workspace, options, formattingRules, CancellationToken.None);

                visibleSpans.Add(visibleSpan);
                var newChanges = FilterTextChanges(document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None), visibleSpans, changes.ToReadOnlyCollection()).Where(t => visibleSpan.Contains(t.Span));

                foreach (var change in newChanges)
                {
                    edit.Replace(change.Span.ToSpan(), change.NewText);
                }
            }
        }

        public BaseIndentationFormattingRule GetBaseIndentationRule(SyntaxNode root, SourceText text, List<TextSpan> spans, int spanIndex)
        {
            if (_hostType == HostType.Razor)
            {
                var currentSpanIndex = spanIndex;

                TextSpan visibleSpan;
                TextSpan visibleTextSpan;
                GetVisibleAndTextSpan(text, spans, currentSpanIndex, out visibleSpan, out visibleTextSpan);

                var end = visibleSpan.End;
                var current = root.FindToken(visibleTextSpan.Start).Parent;
                while (current != null)
                {
                    if (current.Span.Start == visibleTextSpan.Start)
                    {
                        var blockType = GetRazorCodeBlockType(visibleSpan.Start);
                        if (blockType == RazorCodeBlockType.Explicit)
                        {
                            var baseIndentation = GetBaseIndentation(root, text, visibleSpan);
                            return new BaseIndentationFormattingRule(root, TextSpan.FromBounds(visibleSpan.Start, end), baseIndentation, _vbHelperFormattingRule);
                        }
                    }

                    if (current.Span.Start < visibleSpan.Start)
                    {
                        var blockType = GetRazorCodeBlockType(visibleSpan.Start);
                        if (blockType == RazorCodeBlockType.Block || blockType == RazorCodeBlockType.Helper)
                        {
                            var baseIndentation = GetBaseIndentation(root, text, visibleSpan);
                            return new BaseIndentationFormattingRule(root, TextSpan.FromBounds(visibleSpan.Start, end), baseIndentation, _vbHelperFormattingRule);
                        }

                        if (currentSpanIndex == 0)
                        {
                            break;
                        }

                        GetVisibleAndTextSpan(text, spans, --currentSpanIndex, out visibleSpan, out visibleTextSpan);
                        continue;
                    }

                    current = current.Parent;
                }
            }

            var span = spans[spanIndex];
            var indentation = GetBaseIndentation(root, text, span);
            return new BaseIndentationFormattingRule(root, span, indentation, _vbHelperFormattingRule);
        }

        private void GetVisibleAndTextSpan(SourceText text, List<TextSpan> spans, int spanIndex, out TextSpan visibleSpan, out TextSpan visibleTextSpan)
        {
            visibleSpan = spans[spanIndex];

            visibleTextSpan = GetVisibleTextSpan(text, visibleSpan);
            if (visibleTextSpan.IsEmpty)
            {
                // span has no text in them
                visibleTextSpan = visibleSpan;
            }
        }

        private int GetBaseIndentation(SyntaxNode root, SourceText text, TextSpan span)
        {
            // Is this right?  We should probably get this from the IVsContainedLanguageHost instead.
            var editorOptionsFactory = _componentModel.GetService<IEditorOptionsFactoryService>();
            var editorOptions = editorOptionsFactory.GetOptions(_containedLanguage.DataBuffer);

            var additionalIndentation = GetAdditionalIndentation(root, text, span);

            string baseIndentationString;
            int parent, indentSize, useTabs = 0, tabSize = 0;

            // Skip over the first line, since it's in "Venus space" anyway.
            var startingLine = text.Lines.GetLineFromPosition(span.Start);
            for (var line = startingLine; line.Start < span.End; line = text.Lines[line.LineNumber + 1])
            {
                Marshal.ThrowExceptionForHR(
                    this.ContainedLanguage.ContainedLanguageHost.GetLineIndent(
                        line.LineNumber,
                        out baseIndentationString,
                        out parent,
                        out indentSize,
                        out useTabs,
                        out tabSize));

                if (!string.IsNullOrEmpty(baseIndentationString))
                {
                    return baseIndentationString.GetColumnFromLineOffset(baseIndentationString.Length, editorOptions.GetTabSize()) + additionalIndentation;
                }
            }

            return additionalIndentation;
        }

        private TextSpan GetVisibleTextSpan(SourceText text, TextSpan visibleSpan, bool uptoFirstAndLastLine = false)
        {
            var start = visibleSpan.Start;
            for (; start < visibleSpan.End; start++)
            {
                if (!char.IsWhiteSpace(text[start]))
                {
                    break;
                }
            }

            var end = visibleSpan.End - 1;
            if (start <= end)
            {
                for (; start <= end; end--)
                {
                    if (!char.IsWhiteSpace(text[end]))
                    {
                        break;
                    }
                }
            }

            if (uptoFirstAndLastLine)
            {
                var firstLine = text.Lines.GetLineFromPosition(visibleSpan.Start);
                var lastLine = text.Lines.GetLineFromPosition(visibleSpan.End);

                if (firstLine.LineNumber < lastLine.LineNumber)
                {
                    start = (start < firstLine.End) ? start : firstLine.End;
                    end = (lastLine.Start < end + 1) ? end : lastLine.Start - 1;
                }
            }

            return (start <= end) ? TextSpan.FromBounds(start, end + 1) : default(TextSpan);
        }

        private int GetAdditionalIndentation(SyntaxNode root, SourceText text, TextSpan span)
        {
            if (_hostType == HostType.HTML)
            {
                return _optionService.GetOption(FormattingOptions.IndentationSize, this.Project.Language);
            }

            if (_hostType == HostType.Razor)
            {
                var type = GetRazorCodeBlockType(span.Start);

                // razor block
                if (type == RazorCodeBlockType.Block)
                {
                    // more workaround for csharp razor case. when } for csharp razor code block is just typed, "}" exist
                    // in both subject and surface buffer and there is no easy way to figure out who owns } just typed.
                    // in this case, we let razor owns it. later razor will remove } from subject buffer if it is something
                    // razor owns.
                    if (this.Project.Language == LanguageNames.CSharp)
                    {
                        var textSpan = GetVisibleTextSpan(text, span);
                        var end = textSpan.End - 1;
                        if (end >= 0 && text[end] == '}')
                        {
                            var token = root.FindToken(end);
                            var syntaxFact = _workspace.Services.GetLanguageServices(Project.Language).GetService<ISyntaxFactsService>();
                            if (token.Span.Start == end && syntaxFact != null)
                            {
                                SyntaxToken openBrace;
                                if (syntaxFact.TryGetCorrespondingOpenBrace(token, out openBrace) && !textSpan.Contains(openBrace.Span))
                                {
                                    return 0;
                                }
                            }
                        }
                    }

                    // same as C#, but different text is in the buffer
                    if (this.Project.Language == LanguageNames.VisualBasic)
                    {
                        var textSpan = GetVisibleTextSpan(text, span);
                        var subjectSnapshot = _containedLanguage.SubjectBuffer.CurrentSnapshot;
                        var end = textSpan.End - 1;
                        if (end >= 0)
                        {
                            var ch = subjectSnapshot[end];
                            if (CheckCode(subjectSnapshot, textSpan.End, ch, VBRazorBlock, checkAt: false) ||
                                CheckCode(subjectSnapshot, textSpan.End, ch, FunctionsRazor, checkAt: false))
                            {
                                var token = root.FindToken(end, findInsideTrivia: true);
                                var syntaxFact = _workspace.Services.GetLanguageServices(Project.Language).GetService<ISyntaxFactsService>();
                                if (token.Span.End == textSpan.End && syntaxFact != null)
                                {
                                    if (syntaxFact.IsSkippedTokensTrivia(token.Parent))
                                    {
                                        return 0;
                                    }
                                }
                            }
                        }
                    }

                    return _optionService.GetOption(FormattingOptions.IndentationSize, this.Project.Language);
                }
            }

            return 0;
        }

        private RazorCodeBlockType GetRazorCodeBlockType(int position)
        {
            Debug.Assert(_hostType == HostType.Razor);

            var subjectBuffer = (IProjectionBuffer)this.GetOpenTextBuffer();
            var subjectSnapshot = subjectBuffer.CurrentSnapshot;
            var surfaceSnapshot = ((IProjectionBuffer)_containedLanguage.DataBuffer).CurrentSnapshot;

            var surfacePoint = surfaceSnapshot.MapFromSourceSnapshot(new SnapshotPoint(subjectSnapshot, position), PositionAffinity.Predecessor);
            if (!surfacePoint.HasValue)
            {
                // how this can happen?
                return RazorCodeBlockType.Implicit;
            }

            var ch = char.ToLower(surfaceSnapshot[Math.Max(surfacePoint.Value - 1, 0)]);

            // razor block
            if (IsCodeBlock(surfaceSnapshot, surfacePoint.Value, ch))
            {
                return RazorCodeBlockType.Block;
            }

            if (ch == RazorExplicit)
            {
                return RazorCodeBlockType.Explicit;
            }

            if (CheckCode(surfaceSnapshot, surfacePoint.Value, HelperRazor))
            {
                return RazorCodeBlockType.Helper;
            }

            return RazorCodeBlockType.Implicit;
        }

        private bool IsCodeBlock(ITextSnapshot surfaceSnapshot, int position, char ch)
        {
            if (this.Project.Language == LanguageNames.CSharp)
            {
                return CheckCode(surfaceSnapshot, position, ch, CSharpRazorBlock) ||
                       CheckCode(surfaceSnapshot, position, ch, FunctionsRazor, CSharpRazorBlock);
            }

            if (this.Project.Language == LanguageNames.VisualBasic)
            {
                return CheckCode(surfaceSnapshot, position, ch, VBRazorBlock) ||
                       CheckCode(surfaceSnapshot, position, ch, FunctionsRazor);
            }

            return false;
        }

        private bool CheckCode(ITextSnapshot snapshot, int position, char ch, string tag, bool checkAt = true)
        {
            if (ch != tag[tag.Length - 1] || position < tag.Length)
            {
                return false;
            }

            var start = position - tag.Length;
            var razorTag = snapshot.GetText(start, tag.Length);
            return string.Equals(razorTag, tag, StringComparison.OrdinalIgnoreCase) && (!checkAt || snapshot[start - 1] == RazorExplicit);
        }

        private bool CheckCode(ITextSnapshot snapshot, int position, string tag)
        {
            int i = position - 1;
            if (i < 0)
            {
                return false;
            }

            for (; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(snapshot[i]))
                {
                    break;
                }
            }

            var ch = snapshot[i];
            position = i + 1;

            return CheckCode(snapshot, position, ch, tag);
        }

        private bool CheckCode(ITextSnapshot snapshot, int position, char ch, string tag1, string tag2)
        {
            if (!CheckCode(snapshot, position, ch, tag2, checkAt: false))
            {
                return false;
            }

            return CheckCode(snapshot, position - tag2.Length, tag1);
        }

        public ITextUndoHistory GetTextUndoHistory()
        {
            // In Venus scenarios, the undo history is associated with the data buffer
            return _componentModel.GetService<ITextUndoHistoryRegistry>().GetHistory(_containedLanguage.DataBuffer);
        }

        public uint GetItemId()
        {
            AssertIsForeground();

            if (_itemMoniker == null)
            {
                return (uint)VSConstants.VSITEMID.Nil;
            }

            uint itemId;
            return Project.Hierarchy.ParseCanonicalName(_itemMoniker, out itemId) == VSConstants.S_OK
                ? itemId
                : (uint)VSConstants.VSITEMID.Nil;
        }

        private enum RazorCodeBlockType
        {
            Block,
            Explicit,
            Implicit,
            Helper
        }

        private enum HostType
        {
            HTML,
            Razor,
            XOML
        }
    }
}
