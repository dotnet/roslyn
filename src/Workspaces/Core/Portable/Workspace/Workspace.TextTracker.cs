// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial class Workspace
    {
        /// <summary>
        /// A class that responds to text buffer changes
        /// </summary>
        private class TextTracker
        {
            private readonly Workspace _workspace;
            private readonly DocumentId _documentId;
            internal readonly SourceTextContainer TextContainer;
            private readonly EventHandler<TextChangeEventArgs> _weakOnTextChanged;
            private readonly Action<Workspace, DocumentId, SourceText, PreservationMode> _onChangedHandler;

            /// <summary>
            /// Delay between processing text changes to this document.
            /// A non-zero delay is provided to optimize performance for typing scenarios,
            /// where processing each text change could be very expensive.
            /// For example typing in analyzer config documents.
            /// </summary>
            private readonly TimeSpan _delayBetweenProcessingTextChanges;

            private int _lastTextChangeTickCount;

            internal TextTracker(
                Workspace workspace,
                DocumentId documentId,
                SourceTextContainer textContainer,
                Action<Workspace, DocumentId, SourceText, PreservationMode> onChangedHandler,
                TimeSpan delayBetweenProcessingTextChanges)
            {
                _workspace = workspace;
                _documentId = documentId;
                this.TextContainer = textContainer;
                _onChangedHandler = onChangedHandler;
                _delayBetweenProcessingTextChanges = delayBetweenProcessingTextChanges;

                // use weak event so TextContainer cannot accidentally keep workspace alive.
                _weakOnTextChanged = WeakEventHandler<TextChangeEventArgs>.Create(this, (target, sender, args) => target.OnTextChanged(sender, args));
            }

            public void Connect()
                => this.TextContainer.TextChanged += _weakOnTextChanged;

            public void Disconnect()
                => this.TextContainer.TextChanged -= _weakOnTextChanged;

            private void OnTextChanged(object sender, TextChangeEventArgs e)
            {
                // Check if we require a delay between processing text changes to this document.
                // If we do not require a delay OR the prior text change happened before this required delay,
                // then we process this text change immediately.
                // Otherwise, we kick off a Task.Delay to enforce this delay.
                // After the delay, we check if there was another text change after the current one.
                // If so, we don't process the current text change and wait for the next one to be processed.
                // This ensures that only the last text change is processed, optimizing typing performance.

                if (_delayBetweenProcessingTextChanges == TimeSpan.Zero)
                {
                    OnTextChangedCore(e.NewText);
                    return;
                }

                var lastTextChangeTickCount = _lastTextChangeTickCount;
                var currentTickCount = Environment.TickCount;
                _lastTextChangeTickCount = currentTickCount;

                if (currentTickCount - lastTextChangeTickCount >= _delayBetweenProcessingTextChanges.Ticks)
                {
                    OnTextChangedCore(e.NewText);
                    return;
                }

                Task.Run(async delegate
                {
                    await Task.Delay(_delayBetweenProcessingTextChanges).ConfigureAwait(false);

                    if (_lastTextChangeTickCount != currentTickCount)
                    {
                        // There was another subsequent text change.
                        return;
                    }

                    OnTextChangedCore(e.NewText);
                });
            }

            private void OnTextChangedCore(SourceText newText)
            {
                // ok, the version changed.  Report that we've got an edit so that we can analyze
                // this source file and update anything accordingly.
                _onChangedHandler(_workspace, _documentId, newText, PreservationMode.PreserveIdentity);
            }
        }
    }
}
