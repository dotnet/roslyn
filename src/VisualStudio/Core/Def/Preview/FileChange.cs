// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Preview;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview;

internal class FileChange : AbstractChange
{
    private readonly TextDocument _left;
    private readonly TextDocument _right;
    private readonly IComponentModel _componentModel;
    public readonly DocumentId Id;
    private readonly ITextBuffer _buffer;
    private readonly IVsImageService2 _imageService;

    private static readonly StringDifferenceOptions s_differenceOptions = new()
    {
        DifferenceType = StringDifferenceTypes.Line,
    };

    public FileChange(TextDocument left,
        TextDocument right,
        IComponentModel componentModel,
        AbstractChange parent,
        PreviewEngine engine,
        IVsImageService2 imageService) : base(engine)
    {
        Contract.ThrowIfFalse(left != null || right != null);

        this.Id = left != null ? left.Id : right.Id;
        _left = left;
        _right = right;
        _imageService = imageService;

        _componentModel = componentModel;
        var bufferFactory = componentModel.GetService<ITextBufferFactoryService>();
        var bufferCloneService = componentModel.GetService<ITextBufferCloneService>();
        var bufferText = left != null
            ? left.GetTextSynchronously(CancellationToken.None)
            : right.GetTextSynchronously(CancellationToken.None);

        _buffer = bufferCloneService.Clone(bufferText, bufferFactory.InertContentType);

        this.Children = ComputeChildren(left, right, CancellationToken.None);
        this.parent = parent;
    }

    private ChangeList ComputeChildren(TextDocument left, TextDocument right, CancellationToken cancellationToken)
    {
        if (left == null)
        {
            // Added document.
            return GetEntireDocumentAsSpanChange(right);
        }
        else if (right == null)
        {
            // Removed document.
            return GetEntireDocumentAsSpanChange(left);
        }

        var oldText = left.GetTextSynchronously(cancellationToken);
        var newText = right.GetTextSynchronously(cancellationToken);

        var diffSelector = _componentModel.GetService<ITextDifferencingSelectorService>();
        var diffService = diffSelector.GetTextDifferencingService(
            left.Project.Services.GetService<IContentTypeLanguageService>().GetDefaultContentType());

        diffService ??= diffSelector.DefaultTextDifferencingService;

        var diff = ComputeDiffSpans(diffService, left, right, cancellationToken);
        if (diff.Differences.Count == 0)
        {
            // There are no changes.
            return ChangeList.Empty;
        }

        return GetChangeList(diff, right.Id, oldText, newText);
    }

    private ChangeList GetChangeList(IHierarchicalDifferenceCollection diff, DocumentId id, SourceText oldText, SourceText newText)
    {
        var spanChanges = new List<SpanChange>();
        foreach (var difference in diff)
        {
            var leftSpan = diff.LeftDecomposition.GetSpanInOriginal(difference.Left);
            var rightSpan = diff.RightDecomposition.GetSpanInOriginal(difference.Right);

            var leftText = oldText.GetSubText(leftSpan.ToTextSpan()).ToString();
            var rightText = newText.GetSubText(rightSpan.ToTextSpan()).ToString();

            var trackingSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(leftSpan, SpanTrackingMode.EdgeInclusive);

            var isDeletion = difference.DifferenceType == DifferenceType.Remove;
            var displayText = isDeletion ? GetDisplayText(leftText) : GetDisplayText(rightText);

            var spanChange = new SpanChange(trackingSpan, _buffer, id, displayText, leftText, rightText, isDeletion, this, engine);

            spanChanges.Add(spanChange);
        }

        return new ChangeList(spanChanges.ToArray());
    }

    private ChangeList GetEntireDocumentAsSpanChange(TextDocument document)
    {
        // Show the whole document.
        var entireSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(0, _buffer.CurrentSnapshot.Length, SpanTrackingMode.EdgeInclusive);
        var text = document.GetTextSynchronously(CancellationToken.None).ToString();
        var displayText = GetDisplayText(text);
        var entireSpanChild = new SpanChange(entireSpan, _buffer, document.Id, displayText, text, text, isDeletion: false, parent: this, engine: engine);
        return new ChangeList(new[] { entireSpanChild });
    }

    private static string GetDisplayText(string excerpt)
    {
        if (excerpt.Contains("\r\n"))
        {
            var split = excerpt.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 1)
            {
                return string.Format("{0} ... {1}", split[0].Trim(), split[^1].Trim());
            }
        }

        return excerpt.Trim();
    }

    public override int GetText(out VSTREETEXTOPTIONS tto, out string pbstrText)
    {
        if (_left == null)
        {
            pbstrText = ServicesVSResources.bracket_plus_bracket + _right.Name;
        }
        else if (_right == null)
        {
            pbstrText = ServicesVSResources.bracket_bracket + _left.Name;
        }
        else
        {
            pbstrText = _right.Name;
        }

        tto = VSTREETEXTOPTIONS.TTO_DEFAULT;
        return VSConstants.S_OK;
    }

    public override int GetTipText(out VSTREETOOLTIPTYPE eTipType, out string pbstrText)
    {
        eTipType = VSTREETOOLTIPTYPE.TIPTYPE_DEFAULT;
        pbstrText = null;
        return VSConstants.E_FAIL;
    }

    public override int OnRequestSource(object pIUnknownTextView)
    {
        if (pIUnknownTextView != null && Children.Changes != null && Children.Changes.Length > 0)
        {
            engine.SetTextView(pIUnknownTextView);
            UpdatePreview();
        }

        return VSConstants.S_OK;
    }

    public override void UpdatePreview()
        => engine.UpdatePreview(this.Id, (SpanChange)Children.Changes[0]);

    private SourceText UpdateBufferText()
    {
        foreach (SpanChange child in Children.Changes)
        {
            using var edit = _buffer.CreateEdit();
            edit.Replace(child.GetSpan(), child.GetApplicableText());
            edit.ApplyAndLogExceptions();
        }

        return _buffer.CurrentSnapshot.AsText();
    }

    public TextDocument GetOldDocument()
        => _left;

    public TextDocument GetUpdatedDocument()
    {
        if (_left == null || _right == null)
        {
            // Added or removed document.
            return _right;
        }

        return _right.WithText(UpdateBufferText());
    }

    // Note that either _left or _right *must* be non-null (we are either adding, removing or changing a file).
    public TextDocumentKind ChangedDocumentKind => (_left ?? _right).Kind;

    internal override void GetDisplayData(VSTREEDISPLAYDATA[] pData)
    {
        var document = _right ?? _left;

        // If these are documents from a VS workspace, then attempt to get the right display
        // data from the underlying VSHierarchy and itemids for the document.
        var workspace = document.Project.Solution.Workspace;
        if (workspace is VisualStudioWorkspaceImpl vsWorkspace)
        {
            if (vsWorkspace.TryGetImageListAndIndex(_imageService, document.Id, out pData[0].hImageList, out pData[0].Image))
            {
                pData[0].SelectedImage = pData[0].Image;
                return;
            }
        }

        pData[0].Image = pData[0].SelectedImage
            = document.Project.Language == LanguageNames.CSharp ? (ushort)StandardGlyphGroup.GlyphCSharpFile :
                                                                  (ushort)StandardGlyphGroup.GlyphGroupClass;
    }

    private static IHierarchicalDifferenceCollection ComputeDiffSpans(ITextDifferencingService diffService, TextDocument left, TextDocument right, CancellationToken cancellationToken)
    {
        // TODO: it would be nice to have a syntax based differ for presentation here, 
        //       current way of just using text differ has its own issue, and using syntax differ in compiler that are for incremental parser
        //       has its own drawbacks.

        var oldText = left.GetTextSynchronously(cancellationToken);
        var newText = right.GetTextSynchronously(cancellationToken);

        return diffService.DiffSourceTexts(oldText, newText, s_differenceOptions);
    }
}
