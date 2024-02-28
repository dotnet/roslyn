// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview;

internal abstract class AbstractChange : ForegroundThreadAffinitizedObject
{
    public ChangeList Children;
    public __PREVIEWCHANGESITEMCHECKSTATE CheckState { get; private set; }
    protected AbstractChange parent;
    protected PreviewEngine engine;

    public AbstractChange(PreviewEngine engine)
        : base(engine.ThreadingContext)
    {
        this.engine = engine;
        if (engine.ShowCheckBoxes)
        {
            CheckState = __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Checked;
        }
        else
        {
            CheckState = __PREVIEWCHANGESITEMCHECKSTATE.PCCS_None;
        }
    }

    internal virtual uint GetDisplayState()
    {
        // Set TDS_SELECTED
        return (uint)CheckState << 12;
    }

    public IVsPreviewChangesList GetChildren()
        => Children;

    internal abstract void GetDisplayData(VSTREEDISPLAYDATA[] pData);

    public void Toggle()
    {
        if (engine.ShowCheckBoxes)
        {
            var newState = __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Checked;
            if (CheckState == __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Checked)
            {
                newState = __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Unchecked;
            }

            foreach (var child in Children.Changes)
            {
                child.SetState(newState);
            }

            CheckState = newState;
            if (this.parent != null)
            {
                parent.Refresh();
            }

            this.UpdatePreview();
        }
    }

    public void SetState(__PREVIEWCHANGESITEMCHECKSTATE newState)
    {
        if (engine.ShowCheckBoxes)
        {
            CheckState = newState;

            foreach (var child in Children.Changes)
            {
                child.SetState(newState);
            }
        }
    }

    public void Refresh()
    {
        if (Children.Changes.Length == 0)
        {
            return;
        }

        if (engine.ShowCheckBoxes)
        {
            var newState = Children.Changes[0].CheckState;
            foreach (var child in Children.Changes)
            {
                if (newState != child.CheckState)
                {
                    newState = __PREVIEWCHANGESITEMCHECKSTATE.PCCS_PartiallyChecked;
                    break;
                }
            }

            CheckState = newState;
        }

        if (this.parent != null)
        {
            parent.Refresh();
        }
    }

    public abstract int GetText(out VSTREETEXTOPTIONS tto, out string ppszText);
    public abstract int GetTipText(out VSTREETOOLTIPTYPE eTipType, out string ppszText);

    public virtual int CanRecurse
    {
        get
        {
            return 1;
        }
    }

    public virtual int IsExpandable
    {
        get
        {
            return 1;
        }
    }

    public abstract int OnRequestSource(object pIUnknownTextView);
    public abstract void UpdatePreview();
}
