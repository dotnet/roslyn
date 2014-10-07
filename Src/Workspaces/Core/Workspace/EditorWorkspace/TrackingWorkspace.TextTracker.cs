using System;
using Roslyn.Compilers;
using Roslyn.Services;

namespace Roslyn.Services
{
    public partial class TrackingWorkspace
    {
        private class TextTracker
        {
            private readonly TrackingWorkspace workspace;
            private readonly DocumentId documentId;
            internal readonly ITextContainer TextContainer;
            private bool disconnected;

            internal TextTracker(
                TrackingWorkspace workspace,
                DocumentId documentId,
                ITextContainer textContainer)
            {
                this.workspace = workspace;
                this.documentId = documentId;
                this.TextContainer = textContainer;
            }

            ~TextTracker()
            {
                if (!Environment.HasShutdownStarted)
                {
                    throw new Exception(GetType().Name + " collected without having Disconnect called");
                }
            }

            public void Connect()
            {
                this.TextContainer.TextChanged += OnTextChanged;
            }

            public void Disconnect()
            {
                this.TextContainer.TextChanged -= OnTextChanged;
                disconnected = true;
                GC.SuppressFinalize(this);
            }

            private void OnTextChanged(object sender, TextChangeEventArgs e)
            {
                if (disconnected)
                {
                    return;
                }

                if (e.OldText == e.NewText)
                {
                    // nothing changed.  Note - it's tempting to do this for cases where the editor
                    // says that a change happened, but there are no actual changes. We don't want
                    // to do this, because then when we get passed a snapshot in an editor/VS API,
                    // the Workspace's CurrentSolution won't contain it.  Incremental parsing should
                    // fast anyway, since there are no changes.
                    return;
                }

                // ok, the version changed.  Report that we've got an edit so that we can analyze
                // this source file and update anything accordingly.
                this.workspace.OnDocumentTextUpdated(this.documentId, e.NewText, mode: PreservationMode.PreserveIdentity);
            }
        }
    }
}