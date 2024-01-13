// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// Key processor that allows us to toggle inline hints when a user hits Alt+F1
    /// </summary>
    [Export(typeof(IKeyProcessorProvider))]
    [Export(typeof(IInlineHintKeyProcessor))]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(InlineHintsKeyProcessorProvider))]
    internal sealed class InlineHintsKeyProcessorProvider : IKeyProcessorProvider, IInlineHintKeyProcessor
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly IThreadingContext _threadingContext;
        private int _state;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineHintsKeyProcessorProvider(
            IGlobalOptionService globalOptions,
            IThreadingContext threadingContext)
        {
            _globalOptions = globalOptions;
            _threadingContext = threadingContext;
        }

        public bool State
        {
            get
            {
                // Can be read on any thread.
                return Volatile.Read(ref _state) == 1;
            }

            private set
            {
                _threadingContext.ThrowIfNotOnUIThread();
                Volatile.Write(ref _state, value ? 1 : 0);
            }
        }

        public event Action? StateChanged;

        public KeyProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
            => new InlineHintsKeyProcessor(this, wpfTextView);

        private sealed class InlineHintsKeyProcessor : KeyProcessor
        {
            private readonly InlineHintsKeyProcessorProvider _processorProvider;
            private readonly IWpfTextView _view;

            public InlineHintsKeyProcessor(InlineHintsKeyProcessorProvider processorProvider, IWpfTextView view)
            {
                _processorProvider = processorProvider;
                _view = view;
                _view.Closed += OnViewClosed;
                _view.LostAggregateFocus += OnLostFocus;
            }

            private static bool IsAlt(KeyEventArgs args)
                => IsKey(args, Key.LeftAlt) || IsKey(args, Key.RightAlt);

            private static bool IsF1(KeyEventArgs args)
                => IsKey(args, Key.F1);

            private static bool IsKey(KeyEventArgs args, Key key)
                => args.SystemKey == key || args.Key == key;

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

                // If the user is now holding down F1, see if they're also holding down 'alt'.  If so, toggle the inline hints on.
                if (IsF1(args) &&
                    args.KeyboardDevice.Modifiers == ModifierKeys.Alt)
                {
                    ToggleOn();
                }
                else
                {
                    // Otherwise, on any other keypress toggle off.  Note that this will normally be non-expensive as we
                    // will see the option is already off and immediately exit..
                    ToggleOff();
                }
            }

            public override void KeyUp(KeyEventArgs args)
            {
                base.KeyUp(args);

                // If we've lifted a key up from either character of our alt-F1 chord, then turn off the inline hints.
                if (IsAlt(args) || IsF1(args))
                    ToggleOff();
            }

            private void ToggleOn()
                => Toggle(on: true);

            private void ToggleOff()
                => Toggle(on: false);

            private void Toggle(bool on)
            {
                // No need to do anything if we're already in the requested state
                if (_processorProvider.State == on)
                    return;

                // We can only enter the on-state if the user has the chord feature enabled.  We can always enter the
                // off state though.
                on = on && _processorProvider._globalOptions.GetOption(InlineHintsViewOptionsStorage.DisplayAllHintsWhilePressingAltF1);
                if (_processorProvider.State == on)
                    return;

                _processorProvider.State = on;
                _processorProvider.StateChanged?.Invoke();
            }
        }
    }
}
