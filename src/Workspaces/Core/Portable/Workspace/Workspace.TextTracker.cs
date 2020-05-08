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
            private readonly TimeSpan _delayOnTextChanged;
            private int _lastTextChangeTickCount;

            internal TextTracker(
                Workspace workspace,
                DocumentId documentId,
                SourceTextContainer textContainer,
                Action<Workspace, DocumentId, SourceText, PreservationMode> onChangedHandler,
                TimeSpan delayOnTextChanged)
            {
                _workspace = workspace;
                _documentId = documentId;
                this.TextContainer = textContainer;
                _onChangedHandler = onChangedHandler;
                _delayOnTextChanged = delayOnTextChanged;

                // use weak event so TextContainer cannot accidentally keep workspace alive.
                _weakOnTextChanged = WeakEventHandler<TextChangeEventArgs>.Create(this, (target, sender, args) => target.OnTextChanged(sender, args));
            }

            public void Connect()
                => this.TextContainer.TextChanged += _weakOnTextChanged;

            public void Disconnect()
                => this.TextContainer.TextChanged -= _weakOnTextChanged;

            private void OnTextChanged(object sender, TextChangeEventArgs e)
            {
                if (_delayOnTextChanged == TimeSpan.Zero)
                {
                    OnTextChangedCore(e.NewText);
                    return;
                }

                var tickCount = Environment.TickCount;
                _lastTextChangeTickCount = tickCount;

                Task.Run(async delegate
                {
                    await Task.Delay(_delayOnTextChanged).ConfigureAwait(false);

                    if (_lastTextChangeTickCount != tickCount)
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
