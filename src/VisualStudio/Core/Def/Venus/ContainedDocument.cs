﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Projection;
using Roslyn.Utilities;
using static Microsoft.VisualStudio.VSConstants;
using IVsContainedLanguageHost = Microsoft.VisualStudio.TextManager.Interop.IVsContainedLanguageHost;
using IVsTextBufferCoordinator = Microsoft.VisualStudio.TextManager.Interop.IVsTextBufferCoordinator;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal sealed partial class ContainedDocument : ForegroundThreadAffinitizedObject, IContainedDocument
    {
        private const string ReturnReplacementString = @"{|r|}";
        private const string NewLineReplacementString = @"{|n|}";

        private const string HTML = nameof(HTML);
        private const string HTMLX = nameof(HTMLX);
        private const string LegacyRazor = nameof(LegacyRazor);
        private const string Razor = nameof(Razor);
        private const string XOML = nameof(XOML);
        private const string WebForms = nameof(WebForms);

        private const char RazorExplicit = '@';

        private const string CSharpRazorBlock = "{";
        private const string VBRazorBlock = "code";

        private const string HelperRazor = "helper";
        private const string FunctionsRazor = "functions";
        private const string CodeRazor = "code";

        private static readonly EditOptions s_venusEditOptions = new(new StringDifferenceOptions
        {
            DifferenceType = StringDifferenceTypes.Character,
            IgnoreTrimWhiteSpace = false
        });

        private static readonly ConcurrentDictionary<DocumentId, ContainedDocument> s_containedDocuments = new();

        public static ContainedDocument TryGetContainedDocument(DocumentId id)
        {
            if (id == null)
            {
                return null;
            }

            s_containedDocuments.TryGetValue(id, out var document);

            return document;
        }

        private readonly IComponentModel _componentModel;
        private readonly Workspace _workspace;
        private readonly EditorOptionsService _editorOptionsService;
        private readonly ITextDifferencingSelectorService _differenceSelectorService;
        private readonly HostType _hostType;
        private readonly ReiteratedVersionSnapshotTracker _snapshotTracker;
        private readonly AbstractFormattingRule _vbHelperFormattingRule;
        private readonly ProjectSystemProject _project;

        public bool SupportsRename { get { return _hostType == HostType.Razor; } }
        public bool SupportsSemanticSnippets { get { return false; } }

        public DocumentId Id { get; }
        public ITextBuffer SubjectBuffer { get; }
        public ITextBuffer DataBuffer { get; }
        public IVsTextBufferCoordinator BufferCoordinator { get; }
        public IVsContainedLanguageHost ContainedLanguageHost { get; set; }

        public ContainedDocument(
            IThreadingContext threadingContext,
            DocumentId documentId,
            ITextBuffer subjectBuffer,
            ITextBuffer dataBuffer,
            IVsTextBufferCoordinator bufferCoordinator,
            Workspace workspace,
            ProjectSystemProject project,
            IComponentModel componentModel,
            AbstractFormattingRule vbHelperFormattingRule)
            : base(threadingContext)
        {
            _componentModel = componentModel;
            _workspace = workspace;
            _project = project;

            Id = documentId;
            SubjectBuffer = subjectBuffer;
            DataBuffer = dataBuffer;
            BufferCoordinator = bufferCoordinator;

            _differenceSelectorService = componentModel.GetService<ITextDifferencingSelectorService>();
            _editorOptionsService = componentModel.GetService<EditorOptionsService>();
            _snapshotTracker = new ReiteratedVersionSnapshotTracker(SubjectBuffer);
            _vbHelperFormattingRule = vbHelperFormattingRule;

            _hostType = GetHostType();
            s_containedDocuments.TryAdd(documentId, this);
        }

        private HostType GetHostType()
        {
            if (DataBuffer is IProjectionBuffer projectionBuffer)
            {
                // RazorCSharp has an HTMLX base type but should not be associated with
                // the HTML host type, so we check for it first.
                if (projectionBuffer.SourceBuffers.Any(b => b.ContentType.IsOfType(Razor) ||
                    b.ContentType.IsOfType(LegacyRazor)))
                {
                    return HostType.Razor;
                }

                // For TypeScript hosted in HTML the source buffers will have type names
                // HTMLX and TypeScript.
                if (projectionBuffer.SourceBuffers.Any(b => b.ContentType.IsOfType(HTML) ||
                    b.ContentType.IsOfType(WebForms) ||
                    b.ContentType.IsOfType(HTMLX)))
                {
                    return HostType.HTML;
                }
            }
            else
            {
                // XOML is set up differently. For XOML, the secondary buffer (i.e. SubjectBuffer)
                // is a projection buffer, while the primary buffer (i.e. DataBuffer) is not. Instead,
                // the primary buffer is a regular unprojected ITextBuffer with the HTML content type.
                if (DataBuffer.CurrentSnapshot.ContentType.IsOfType(HTML))
                {
                    return HostType.XOML;
                }
            }

            throw ExceptionUtilities.Unreachable();
        }

        public SourceTextContainer GetOpenTextContainer()
            => this.SubjectBuffer.AsTextContainer();

        public void Dispose()
        {
            _snapshotTracker.StopTracking(SubjectBuffer);
            s_containedDocuments.TryRemove(Id, out _);
        }

        public DocumentId FindProjectDocumentIdWithItemId(uint itemidInsertionPoint)
        {
            // We cast to VisualStudioWorkspace because the expectation is this isn't being used in Live Share workspaces
            var hierarchy = ((VisualStudioWorkspace)_workspace).GetHierarchy(_project.Id);

            var containingDocumentItemid = GetContainingDocumentItemId(hierarchy);
            if (containingDocumentItemid == itemidInsertionPoint)
            {
                // No need to walk project documents if requesting containing document's id
                return this.Id;
            }

            foreach (var document in _workspace.CurrentSolution.GetProject(_project.Id).Documents)
            {
                if (document.FilePath != null && hierarchy.TryGetItemId(document.FilePath) == itemidInsertionPoint)
                {
                    return document.Id;
                }
            }

            return null;
        }

        public uint FindItemIdOfDocument(Document document)
        {
            // We cast to VisualStudioWorkspace because the expectation is this isn't being used in Live Share workspaces
            var hierarchy = ((VisualStudioWorkspace)_workspace).GetHierarchy(_project.Id);

            if (document.Id == this.Id)
            {
                return GetContainingDocumentItemId(hierarchy);
            }

            return hierarchy.TryGetItemId(_workspace.CurrentSolution.GetDocument(document.Id).FilePath);
        }

        public void UpdateText(SourceText newText)
        {
            var originalText = SubjectBuffer.CurrentSnapshot.AsText();
            ApplyChanges(originalText, newText.GetTextChanges(originalText));
        }

        public ITextSnapshot ApplyChanges(IEnumerable<TextChange> changes)
            => ApplyChanges(SubjectBuffer.CurrentSnapshot.AsText(), changes);

        private uint GetContainingDocumentItemId(IVsHierarchy hierarchy)
        {
            var editorAdaptersFactoryService = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
            Marshal.ThrowExceptionForHR(BufferCoordinator.GetPrimaryBuffer(out var primaryTextLines));

            var primaryBufferDocumentBuffer = editorAdaptersFactoryService.GetDocumentBuffer(primaryTextLines)!;

            var textDocumentFactoryService = _componentModel.GetService<ITextDocumentFactoryService>();
            textDocumentFactoryService.TryGetTextDocument(primaryBufferDocumentBuffer, out var textDocument);
            var documentPath = textDocument.FilePath;

            var itemId = hierarchy.TryGetItemId(documentPath);

            return itemId;
        }

        private ITextSnapshot ApplyChanges(SourceText originalText, IEnumerable<TextChange> changes)
        {
            var subjectBuffer = (IProjectionBuffer)SubjectBuffer;

            IEnumerable<int> affectedVisibleSpanIndices = null;
            var editorVisibleSpansInOriginal = SharedPools.Default<List<TextSpan>>().AllocateAndClear();

            try
            {
                var originalDocument = _workspace.CurrentSolution.GetDocument(this.Id);

                editorVisibleSpansInOriginal.AddRange(GetEditorVisibleSpans());
                var newChanges = FilterTextChanges(originalText, editorVisibleSpansInOriginal, changes).ToList();
                if (newChanges.Count > 0)
                {
                    ApplyChanges(subjectBuffer, newChanges, editorVisibleSpansInOriginal, out affectedVisibleSpanIndices);
                    AdjustIndentation(subjectBuffer, affectedVisibleSpanIndices);
                }

                return subjectBuffer.CurrentSnapshot;
            }
            finally
            {
                SharedPools.Default<HashSet<int>>().ClearAndFree((HashSet<int>)affectedVisibleSpanIndices);
                SharedPools.Default<List<TextSpan>>().ClearAndFree(editorVisibleSpansInOriginal);
            }
        }

        private IEnumerable<TextChange> FilterTextChanges(SourceText originalText, List<TextSpan> editorVisibleSpansInOriginal, IEnumerable<TextChange> changes)
        {
            // no visible spans or changes
            if (editorVisibleSpansInOriginal.Count == 0 || !changes.Any())
            {
                // return empty one
                yield break;
            }

            using var pooledObject = SharedPools.Default<List<TextChange>>().GetPooledObject();

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
                    if (WhitespaceOnEdges(visibleTextSpan, change))
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

        private static bool WhitespaceOnEdges(TextSpan visibleTextSpan, TextChange change)
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
            using var changes = SharedPools.Default<List<TextChange>>().GetPooledObject();

            var leftText = originalText.ToString(changeInOriginalText.Span);
            var rightText = changeInOriginalText.NewText;
            var offsetInOriginalText = changeInOriginalText.Span.Start;

            if (TryGetSubTextChanges(originalText, visibleSpanInOriginalText, leftText, rightText, offsetInOriginalText, changes.Object))
            {
                return changes.Object.ToList();
            }

            return GetSubTextChanges(originalText, visibleSpanInOriginalText, leftText, rightText, offsetInOriginalText);
        }

        private static bool TryGetSubTextChanges(
            SourceText originalText, TextSpan visibleSpanInOriginalText, string leftText, string rightText, int offsetInOriginalText, List<TextChange> changes)
        {
            // these are expensive. but hopefully we don't hit this as much except the boundary cases.
            using var leftPool = SharedPools.Default<List<TextSpan>>().GetPooledObject();
            using var rightPool = SharedPools.Default<List<TextSpan>>().GetPooledObject();

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
                    if (TryGetSubTextChange(originalText, visibleSpanInOriginalText, rightText, spanInOriginalText, spanInRightText, out var textChange))
                    {
                        changes.Add(textChange);
                    }
                }

                return true;
            }

            return false;
        }

        private IEnumerable<TextChange> GetSubTextChanges(
            SourceText originalText, TextSpan visibleSpanInOriginalText, string leftText, string rightText, int offsetInOriginalText)
        {
            // these are expensive. but hopefully we don't hit this as much except the boundary cases.
            using var leftPool = SharedPools.Default<List<ValueTuple<int, int>>>().GetPooledObject();
            using var rightPool = SharedPools.Default<List<ValueTuple<int, int>>>().GetPooledObject();

            var leftReplacementMap = leftPool.Object;
            var rightReplacementMap = rightPool.Object;
            GetTextWithReplacements(leftText, rightText, leftReplacementMap, rightReplacementMap, out var leftTextWithReplacement, out var rightTextWithReplacement);

            var diffResult = DiffStrings(leftTextWithReplacement, rightTextWithReplacement);

            foreach (var difference in diffResult)
            {
                var spanInLeftText = AdjustSpan(diffResult.LeftDecomposition.GetSpanInOriginal(difference.Left), leftReplacementMap);
                var spanInRightText = AdjustSpan(diffResult.RightDecomposition.GetSpanInOriginal(difference.Right), rightReplacementMap);

                var spanInOriginalText = new TextSpan(offsetInOriginalText + spanInLeftText.Start, spanInLeftText.Length);
                if (TryGetSubTextChange(originalText, visibleSpanInOriginalText, rightText, spanInOriginalText, spanInRightText.ToTextSpan(), out var textChange))
                {
                    yield return textChange;
                }
            }
        }

        private static bool TryGetWhitespaceOnlyChanges(string leftText, string rightText, List<TextSpan> spansInLeftText, List<TextSpan> spansInRightText)
            => TryGetWhitespaceGroup(leftText, spansInLeftText) && TryGetWhitespaceGroup(rightText, spansInRightText) && spansInLeftText.Count == spansInRightText.Count;

        private static bool TryGetWhitespaceGroup(string text, List<TextSpan> groups)
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
                    case '\t':
                        if (!TextAt(text, i - 1, ' ', '\t'))
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
                        else if (TextAt(text, i - 1, ' ', '\t'))
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

        private static bool TextAt(string text, int index, char ch1, char ch2 = default)
        {
            if (index < 0 || text.Length <= index)
            {
                return false;
            }

            var actual = text[index];
            if (actual == ch1)
            {
                return true;
            }

            if (ch2 != default && actual == ch2)
            {
                return true;
            }

            return false;
        }

        private static bool TryGetSubTextChange(
            SourceText originalText, TextSpan visibleSpanInOriginalText,
            string rightText, TextSpan spanInOriginalText, TextSpan spanInRightText, out TextChange textChange)
        {
            textChange = default;

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
                        snippetInRightText[firstLineOfRightTextSnippet.Length..]);
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
                        snippetInRightText[..^lastLineOfRightTextSnippet.Length]);
                    return true;
                }

                // footer
                // don't do anything

                return false;
            }

            // if it got hit, then it means there is a missing case
            throw ExceptionUtilities.Unreachable();
        }

        private IHierarchicalDifferenceCollection DiffStrings(string leftTextWithReplacement, string rightTextWithReplacement)
        {
            var diffService = _differenceSelectorService.GetTextDifferencingService(
                _workspace.Services.GetLanguageServices(_project.Language).GetService<IContentTypeLanguageService>().GetDefaultContentType());

            diffService ??= _differenceSelectorService.DefaultTextDifferencingService;
            return diffService.DiffStrings(leftTextWithReplacement, rightTextWithReplacement, s_venusEditOptions.DifferenceOptions);
        }

        private static void GetTextWithReplacements(
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

        private static string GetTextWithReplacementMap(string text, string returnReplacement, string newLineReplacement, List<ValueTuple<int, int>> replacementMap)
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

        private static Span AdjustSpan(Span span, List<ValueTuple<int, int>> replacementMap)
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
            var subjectBuffer = (IProjectionBuffer)this.SubjectBuffer;

            if (DataBuffer is IProjectionBuffer projectionDataBuffer)
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
            using var edit = subjectBuffer.CreateEdit(s_venusEditOptions, reiteratedVersionNumber: null, editTag: null);

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

            edit.ApplyAndLogExceptions();
        }

        private void AdjustIndentation(IProjectionBuffer subjectBuffer, IEnumerable<int> visibleSpanIndex)
        {
            if (!visibleSpanIndex.Any())
            {
                return;
            }

            var document = _workspace.CurrentSolution.GetDocument(this.Id);
            if (!document.SupportsSyntaxTree)
            {
                return;
            }

            var parsedDocument = ParsedDocument.CreateSynchronously(document, CancellationToken.None);
            Debug.Assert(ReferenceEquals(parsedDocument.Text, subjectBuffer.CurrentSnapshot.AsText()));

            var editorOptionsService = _componentModel.GetService<EditorOptionsService>();
            var formattingOptions = subjectBuffer.GetSyntaxFormattingOptions(editorOptionsService, document.Project.Services, explicitFormat: false);

            using var pooledObject = SharedPools.Default<List<TextSpan>>().GetPooledObject();

            var spans = pooledObject.Object;

            spans.AddRange(this.GetEditorVisibleSpans());
            using var edit = subjectBuffer.CreateEdit(s_venusEditOptions, reiteratedVersionNumber: null, editTag: null);

            foreach (var spanIndex in visibleSpanIndex)
            {
                var rule = GetBaseIndentationRule(parsedDocument.Root, parsedDocument.Text, spans, spanIndex);

                var visibleSpan = spans[spanIndex];
                AdjustIndentationForSpan(document, edit, visibleSpan, rule, formattingOptions);
            }

            edit.Apply();
        }

        private void AdjustIndentationForSpan(
            Document document, ITextEdit edit, TextSpan visibleSpan, AbstractFormattingRule baseIndentationRule, SyntaxFormattingOptions options)
        {
            var root = document.GetSyntaxRootSynchronously(CancellationToken.None);

            using var rulePool = SharedPools.Default<List<AbstractFormattingRule>>().GetPooledObject();
            using var spanPool = SharedPools.Default<List<TextSpan>>().GetPooledObject();

            var venusFormattingRules = rulePool.Object;
            var visibleSpans = spanPool.Object;

            venusFormattingRules.Add(baseIndentationRule);
            venusFormattingRules.Add(ContainedDocumentPreserveFormattingRule.Instance);

            var formattingRules = venusFormattingRules.Concat(Formatter.GetDefaultFormattingRules(document));

            var services = document.Project.Solution.Services;
            var formatter = document.GetRequiredLanguageService<ISyntaxFormattingService>();
            var changes = formatter.GetFormattingResult(
                root, new TextSpan[] { CommonFormattingHelpers.GetFormattingSpan(root, visibleSpan) },
                options, formattingRules, CancellationToken.None).GetTextChanges(CancellationToken.None);

            visibleSpans.Add(visibleSpan);
            var newChanges = FilterTextChanges(document.GetTextSynchronously(CancellationToken.None), visibleSpans, changes.ToReadOnlyCollection()).Where(t => visibleSpan.Contains(t.Span));

            foreach (var change in newChanges)
            {
                edit.Replace(change.Span.ToSpan(), change.NewText);
            }
        }

        public BaseIndentationFormattingRule GetBaseIndentationRule(SyntaxNode root, SourceText text, List<TextSpan> spans, int spanIndex)
        {
            if (_hostType == HostType.Razor)
            {
                var currentSpanIndex = spanIndex;
                GetVisibleAndTextSpan(text, spans, currentSpanIndex, out var visibleSpan, out var visibleTextSpan);

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
                        if (blockType is RazorCodeBlockType.Block or RazorCodeBlockType.Helper)
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

        private static void GetVisibleAndTextSpan(SourceText text, List<TextSpan> spans, int spanIndex, out TextSpan visibleSpan, out TextSpan visibleTextSpan)
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
            var editorOptions = _editorOptionsService.Factory.GetOptions(DataBuffer);
            var additionalIndentation = GetAdditionalIndentation(root, text, span, hostIndentationSize: editorOptions.GetIndentSize());

            // Skip over the first line, since it's in "Venus space" anyway.
            var startingLine = text.Lines.GetLineFromPosition(span.Start);
            for (var line = startingLine; line.Start < span.End; line = text.Lines[line.LineNumber + 1])
            {
                Marshal.ThrowExceptionForHR(
                    ContainedLanguageHost.GetLineIndent(
                        line.LineNumber,
                        out var baseIndentationString,
                        out _,
                        out _,
                        out _,
                        out _));

                if (!string.IsNullOrEmpty(baseIndentationString))
                {
                    return baseIndentationString.GetColumnFromLineOffset(baseIndentationString.Length, editorOptions.GetTabSize()) + additionalIndentation;
                }
            }

            return additionalIndentation;
        }

        private static TextSpan GetVisibleTextSpan(SourceText text, TextSpan visibleSpan, bool uptoFirstAndLastLine = false)
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

            return (start <= end) ? TextSpan.FromBounds(start, end + 1) : default;
        }

        private int GetAdditionalIndentation(SyntaxNode root, SourceText text, TextSpan span, int hostIndentationSize)
        {
            if (_hostType == HostType.HTML)
            {
                return hostIndentationSize;
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
                    if (_project.Language == LanguageNames.CSharp)
                    {
                        var textSpan = GetVisibleTextSpan(text, span);
                        var end = textSpan.End - 1;
                        if (end >= 0 && text[end] == '}')
                        {
                            var token = root.FindToken(end);
                            var service = _workspace.Services.GetLanguageServices(_project.Language).GetService<IVenusBraceMatchingService>();
                            if (token.Span.Start == end && service != null)
                            {
                                if (service.TryGetCorrespondingOpenBrace(token, out var openBrace) && !textSpan.Contains(openBrace.Span))
                                {
                                    return 0;
                                }
                            }
                        }
                    }

                    // same as C#, but different text is in the buffer
                    if (_project.Language == LanguageNames.VisualBasic)
                    {
                        var textSpan = GetVisibleTextSpan(text, span);
                        var subjectSnapshot = SubjectBuffer.CurrentSnapshot;
                        var end = textSpan.End - 1;
                        if (end >= 0)
                        {
                            var ch = subjectSnapshot[end];
                            if (CheckCode(subjectSnapshot, textSpan.End, ch, VBRazorBlock, checkAt: false) ||
                                CheckCode(subjectSnapshot, textSpan.End, ch, FunctionsRazor, checkAt: false))
                            {
                                var token = root.FindToken(end, findInsideTrivia: true);
                                var syntaxFact = _workspace.Services.GetLanguageServices(_project.Language).GetService<ISyntaxFactsService>();
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

                    return hostIndentationSize;
                }
            }

            return 0;
        }

        private RazorCodeBlockType GetRazorCodeBlockType(int position)
        {
            Debug.Assert(_hostType == HostType.Razor);

            var subjectBuffer = (IProjectionBuffer)this.SubjectBuffer;
            var subjectSnapshot = subjectBuffer.CurrentSnapshot;
            var surfaceSnapshot = ((IProjectionBuffer)DataBuffer).CurrentSnapshot;

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
            if (_project.Language == LanguageNames.CSharp)
            {
                return CheckCode(surfaceSnapshot, position, ch, CSharpRazorBlock) ||
                       CheckCode(surfaceSnapshot, position, ch, FunctionsRazor, CSharpRazorBlock) ||
                       CheckCode(surfaceSnapshot, position, ch, CodeRazor, CSharpRazorBlock);
            }

            if (_project.Language == LanguageNames.VisualBasic)
            {
                return CheckCode(surfaceSnapshot, position, ch, VBRazorBlock) ||
                       CheckCode(surfaceSnapshot, position, ch, FunctionsRazor);
            }

            return false;
        }

        private static bool CheckCode(ITextSnapshot snapshot, int position, char ch, string tag, bool checkAt = true)
        {
            if (ch != tag[tag.Length - 1] || position < tag.Length)
            {
                return false;
            }

            var start = position - tag.Length;
            var razorTag = snapshot.GetText(start, tag.Length);
            return string.Equals(razorTag, tag, StringComparison.OrdinalIgnoreCase) && (!checkAt || snapshot[start - 1] == RazorExplicit);
        }

        private static bool CheckCode(ITextSnapshot snapshot, int position, string tag)
        {
            var i = position - 1;
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

        private static bool CheckCode(ITextSnapshot snapshot, int position, char ch, string tag1, string tag2)
        {
            if (!CheckCode(snapshot, position, ch, tag2, checkAt: false))
            {
                return false;
            }

            return CheckCode(snapshot, position - tag2.Length, tag1);
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
