// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// Key processor that allows us to toggle inline hints when a user hits Alt+F1
    /// </summary>
    [Export(typeof(IKeyProcessorProvider))]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(InlineHintsKeyProcessorProvider))]
    internal class InlineHintsKeyProcessorProvider : IKeyProcessorProvider
    {
        private readonly IGlobalOptionService _globalOptionService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineHintsKeyProcessorProvider(IGlobalOptionService globalOptionService)
        {
            _globalOptionService = globalOptionService;
        }

        public KeyProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
            => new InlineHintsKeyProcessor(_globalOptionService, wpfTextView);

        private class InlineHintsKeyProcessor : KeyProcessor
        {
            private readonly IGlobalOptionService _globalOptionService;
            private readonly IWpfTextView _view;

            public InlineHintsKeyProcessor(IGlobalOptionService globalOptionService, IWpfTextView view)
            {
                _globalOptionService = globalOptionService;
                _view = view;
                _view.Closed += OnViewClosed;
                _view.LostAggregateFocus += OnLostFocus;
            }

            private static bool IsAltOrF1(KeyEventArgs args)
                => IsAltOrF1(args.SystemKey) || IsAltOrF1(args.Key);

            private static bool IsAltOrF1(Key key)
                => key is Key.LeftAlt or Key.RightAlt or Key.F1;

            private void OnViewClosed(object sender, EventArgs e)
            {
                // Disconnect our callbacks.
                _view.Closed -= OnViewClosed;
                _view.LostAggregateFocus -= OnLostFocus;

                // Go back to off-mode just so we don't somehow get stuck in on-mode if the option was on when the view closed.
                ToggleOff();
            }

            private void OnLostFocus(object sender, EventArgs e)
            {
                // if focus is lost then go back to normal inline-hint processing.
                ToggleOff();
            }

            public override void KeyDown(KeyEventArgs args)
            {
                base.KeyDown(args);

                // if this is either of the keys, and only our chord is down, then toggle on. 
                // otherwise toggle off if anything else is pressed down.
                if (IsAltOrF1(args))
                {
                    if (args.KeyboardDevice.Modifiers == ModifierKeys.Alt)
                    {
                        ToggleOn();
                        return;
                    }
                }

                ToggleOff();
            }

            public override void KeyUp(KeyEventArgs args)
            {
                base.KeyUp(args);

                // If we've lifted a key up from our chord, then turn off the inline hints.
                if (IsAltOrF1(args))
                    ToggleOff();
            }

            private void ToggleOn()
                => Toggle(on: true);

            private void ToggleOff()
                => Toggle(on: false);

            private void Toggle(bool on)
            {
                // No need to do anything if we're already in the requested state
                var state = _globalOptionService.GetOption(InlineHintsOptions.DisplayAllOverride);
                if (state == on)
                    return;

                // We can only enter the on-state if the user has the chord feature enabled.  We can always enter the
                // off state though.
                on = on && _globalOptionService.GetOption(InlineHintsOptions.DisplayAllHintsWhilePressingAltF1);
                _globalOptionService.RefreshOption(new OptionKey(InlineHintsOptions.DisplayAllOverride), on);
            }
        }
    }
}
