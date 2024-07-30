// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

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

        internal TextTracker(
            Workspace workspace,
            DocumentId documentId,
            SourceTextContainer textContainer,
            Action<Workspace, DocumentId, SourceText, PreservationMode> onChangedHandler)
        {
            _workspace = workspace;
            _documentId = documentId;
            this.TextContainer = textContainer;
            _onChangedHandler = onChangedHandler;

            // use weak event so TextContainer cannot accidentally keep workspace alive.
            _weakOnTextChanged = EventHandlerFactory<TextChangeEventArgs>.CreateWeakHandler(this, (target, sender, args) => target.OnTextChanged(sender, args));
        }

        public void Connect()
            => this.TextContainer.TextChanged += _weakOnTextChanged;

        public void Disconnect()
            => this.TextContainer.TextChanged -= _weakOnTextChanged;

        private void OnTextChanged(object sender, TextChangeEventArgs e)
        {
            // ok, the version changed.  Report that we've got an edit so that we can analyze
            // this source file and update anything accordingly.
            _onChangedHandler(_workspace, _documentId, e.NewText, PreservationMode.PreserveIdentity);
        }
    }
}
