// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.UnitTests;

// In 15.6 the editor (QuickInfo in particular) took a dependency on
// IObscuringTipManager, which is only exported in VS editor layer.
// This is tracked by the editor bug https://devdiv.visualstudio.com/DevDiv/_workitems?id=544569.
// Meantime a workaround is to export dummy IObscuringTipManager.
// Do not delete: this one is still used in Editor and implicitly required for EventHookupCommandHandlerTests in Roslyn.
[Export(typeof(IObscuringTipManager))]
internal sealed class TestObscuringTipManager : IObscuringTipManager
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestObscuringTipManager()
    {
    }

    public void PushTip(ITextView view, IObscuringTip tip)
    {
    }

    public void RemoveTip(ITextView view, IObscuringTip tip)
    {
    }
}
