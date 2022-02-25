// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator
{
    internal partial class WpfBackgroundWorkIndicatorFactory
    {
        private sealed class BackgroundWorkIndicatorKeyProcessor : KeyProcessor
        {
            private readonly WpfBackgroundWorkIndicatorFactory _factory;

            public BackgroundWorkIndicatorKeyProcessor(WpfBackgroundWorkIndicatorFactory factory)
                => _factory = factory;

            public override void KeyUp(KeyEventArgs args)
            {
                // if the user hits escape and we have any active background indicator, cancel and dismiss it.
                if (args.Key == Key.Escape)
                    _factory._currentContext?.CancelAndDispose();
            }
        }
    }
}
