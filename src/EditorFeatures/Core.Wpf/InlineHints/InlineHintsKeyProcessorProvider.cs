// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// Key processor that allows us to toggle inline hints when a user hits ctrl-alt.
    /// </summary>
    [Export(typeof(IKeyProcessorProvider))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(InlineHintsKeyProcessorProvider))]
    [Order(Before = "default")]
    internal class InlineHintsKeyProcessorProvider : IKeyProcessorProvider
    {
        private static readonly ConditionalWeakTable<IWpfTextView, InlineHintsKeyProcessor> s_viewToProcessor = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineHintsKeyProcessorProvider()
        {
        }

        public KeyProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
        {
            return s_viewToProcessor.GetValue(wpfTextView, v => new InlineHintsKeyProcessor(v));
        }

        private class InlineHintsKeyProcessor : KeyProcessor
        {
            private readonly IWpfTextView _view;

            public InlineHintsKeyProcessor(IWpfTextView view)
            {
                _view = view;
                _view.Closed += OnViewClosed;
                _view.LostAggregateFocus += OnLostFocus;
            }

            private static bool IsCtrlOrAlt(KeyEventArgs args)
                => args.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt;

            private Document? GetDocument()
            {
                var document =
                    _view.BufferGraph.GetTextBuffers(b => true)
                                     .Select(b => b.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges())
                                     .WhereNotNull()
                                     .FirstOrDefault();

                return document;
            }

            private void OnViewClosed(object sender, EventArgs e)
            {
                // Disconnect our callbacks.
                _view.Closed -= OnViewClosed;
                _view.LostAggregateFocus -= OnLostFocus;
            }

            private void OnLostFocus(object sender, EventArgs e)
            {
                // if focus is lost (which can happen for shortcuts that include ctrl-alt...) then go back to normal
                // inline-hint processing.
                ToggleOff(GetDocument());
            }

            public override void KeyDown(KeyEventArgs args)
            {
                base.KeyDown(args);

                var document = GetDocument();

                // if this is either the ctrl or alt key, and only ctrl-alt is down, then toggle on. 
                // otherwise toggle off if anything else is pressed down.
                if (IsCtrlOrAlt(args))
                {
                    if (args.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
                    {
                        ToggleOn(document);
                        return;
                    }
                }

                ToggleOff(document);
            }

            public override void KeyUp(KeyEventArgs args)
            {
                base.KeyUp(args);

                // If we've lifted a key up, then turn off the inline hints.
                ToggleOff(GetDocument());
            }

            private static void ToggleOn(Document? document)
                => Toggle(document, on: true);

            private static void ToggleOff(Document? document)
                => Toggle(document, on: false);

            private static void Toggle(Document? document, bool on)
            {
                if (document == null)
                    return;

                var workspace = document.Project.Solution.Workspace;

                // No need to do anything if we're already in the requested state
                var state = workspace.Options.GetOption(InlineHintsOptions.DisplayAllOverride);
                if (state == on)
                    return;

                // We can only enter the on-state if the user has the ctrl-alt feature enabled.  We can always enter the
                // off state though.
                on = on && workspace.Options.GetOption(InlineHintsOptions.DisplayAllHintsWhilePressingCtrlAlt, document.Project.Language);

                workspace.TryApplyChanges(
                    workspace.CurrentSolution.WithOptions(
                        workspace.CurrentSolution.Options.WithChangedOption(InlineHintsOptions.DisplayAllOverride, on)));
            }
        }
    }
}
