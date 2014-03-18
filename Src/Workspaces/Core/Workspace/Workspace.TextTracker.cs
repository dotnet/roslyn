// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

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

            internal TextTracker(
                Workspace workspace,
                DocumentId documentId,
                SourceTextContainer textContainer)
            {
                this.workspace = workspace;
                this.documentId = documentId;
                this.TextContainer = textContainer;
            }

#if DEBUG
            ~TextTracker()
            {
                Debug.Assert(Environment.HasShutdownStarted, GetType().Name + " collected without having Disconnect called");
            }
#endif

            public void Connect()
            {
                this.TextContainer.TextChanged += OnTextChanged;
            }

            public void Disconnect()
            {
                this.TextContainer.TextChanged -= OnTextChanged;
#if DEBUG
                GC.SuppressFinalize(this);
#endif
            }

            private void OnTextChanged(object sender, TextChangeEventArgs e)
            {
                // ok, the version changed.  Report that we've got an edit so that we can analyze
                // this source file and update anything accordingly.
                this.workspace.OnDocumentTextChanged(this.documentId, e.NewText, mode: PreservationMode.PreserveIdentity);
            }
        }
    }
}