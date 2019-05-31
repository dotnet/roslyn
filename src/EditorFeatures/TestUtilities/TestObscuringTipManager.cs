// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    // In 15.6 the editor (QuickInfo in particular) took a dependency on
    // IObscuringTipManager, which is only exported in VS editor layer.
    // This is tracked by the editor bug https://devdiv.visualstudio.com/DevDiv/_workitems?id=544569.
    // Meantime a workaround is to export dummy IObscuringTipManager.
    // Do not delete: this one is still used in Editor and implicitly required for EventHookupCommandHandlerTests in Roslyn.
    [Export(typeof(IObscuringTipManager))]
    internal class TestObscuringTipManager : IObscuringTipManager
    {
        [ImportingConstructor]
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
}
