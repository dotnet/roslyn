// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
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
            private readonly Workspace workspace;
            private readonly DocumentId documentId;
            internal readonly SourceTextContainer TextContainer;
            private EventHandler<TextChangeEventArgs> weakOnTextChanged;
            private readonly Action<Workspace, DocumentId, SourceText, PreservationMode> onChangedHandler;

            internal TextTracker(
                Workspace workspace,
                DocumentId documentId,
                SourceTextContainer textContainer,
                Action<Workspace, DocumentId, SourceText, PreservationMode> onChangedHandler)
            {
                this.workspace = workspace;
                this.documentId = documentId;
                this.TextContainer = textContainer;
                this.onChangedHandler = onChangedHandler;

                // use weak event so TextContainer cannot accidentally keep workspace alive.
                this.weakOnTextChanged = WeakEventHandler<TextChangeEventArgs>.Create(this, (target, sender, args) => target.OnTextChanged(sender, args));
            }

            public void Connect()
            {
                this.TextContainer.TextChanged += this.weakOnTextChanged;
            }

            public void Disconnect()
            {
                this.TextContainer.TextChanged -= this.weakOnTextChanged;
            }

            private void OnTextChanged(object sender, TextChangeEventArgs e)
            {
                // ok, the version changed.  Report that we've got an edit so that we can analyze
                // this source file and update anything accordingly.
                onChangedHandler(this.workspace, this.documentId, e.NewText, PreservationMode.PreserveIdentity); 
            }
        }
    }
}