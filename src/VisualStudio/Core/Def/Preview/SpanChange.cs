// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview;

internal class SpanChange : AbstractChange
{
    private readonly DocumentId _id;
    private readonly ITrackingSpan _span;
    private readonly string _text;
    private readonly ITextBuffer _buffer;
    private readonly string _rightText;
    private readonly string _leftText;
    private readonly bool _isDeletion;

    public SpanChange(ITrackingSpan span, ITextBuffer buffer, DocumentId id, string text, string leftText, string rightText, bool isDeletion, AbstractChange parent, PreviewEngine engine)
        : base(engine)
    {
        _span = span;
        _id = id;
        _buffer = buffer;
        _text = text;
        this.parent = parent;
        _rightText = rightText;
        _leftText = leftText;
        _isDeletion = isDeletion;
        this.Children = new ChangeList([]);
    }

    public override int GetText(out VSTREETEXTOPTIONS tto, out string pbstrText)
    {
        pbstrText = _text;
        tto = VSTREETEXTOPTIONS.TTO_DEFAULT;
        return VSConstants.S_OK;
    }

    public override int GetTipText(out VSTREETOOLTIPTYPE eTipType, out string pbstrText)
    {
        eTipType = VSTREETOOLTIPTYPE.TIPTYPE_DEFAULT;
        pbstrText = "";
        return VSConstants.S_OK;
    }

    public override int CanRecurse
    {
        get
        {
            return 0;
        }
    }

    public override int IsExpandable
    {
        get
        {
            return 0;
        }
    }

    public override int OnRequestSource(object pIUnknownTextView)
    {
        if (pIUnknownTextView != null)
        {
            engine.SetTextView(pIUnknownTextView);
            UpdatePreview();
        }

        return VSConstants.S_OK;
    }

    public override void UpdatePreview()
        => engine.UpdatePreview(_id, this);

    internal override void GetDisplayData(VSTREEDISPLAYDATA[] pData)
        => pData[0].Image = pData[0].SelectedImage = (ushort)StandardGlyphGroup.GlyphReference;

    internal string GetApplicableText()
    {
        return CheckState == __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Unchecked
            ? _leftText
            : _rightText;
    }

    internal Span GetSpan()
        => _span.GetSpan(_buffer.CurrentSnapshot).Span;

    internal override uint GetDisplayState()
    {
        // Set TDS_GRAYTEXT if this change is a deletion.
        return base.GetDisplayState() | (_isDeletion ? (uint)1 << 17 : 0);
    }
}
