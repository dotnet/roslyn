// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionsSourceProvider
    {
        private partial class SuggestedActionsSource
        {
            private sealed class State : IDisposable
            {
                private readonly SuggestedActionsSource _source;

                public readonly SuggestedActionsSourceProvider Owner;
                public readonly ITextView TextView;
                public readonly ITextBuffer SubjectBuffer;
                public readonly WorkspaceRegistration Registration;

                public Workspace? Workspace => Registration.Workspace;

                public State(SuggestedActionsSource source, SuggestedActionsSourceProvider owner, ITextView textView, ITextBuffer textBuffer)
                {
                    _source = source;

                    Owner = owner;
                    TextView = textView;
                    SubjectBuffer = textBuffer;
                    Registration = Workspace.GetWorkspaceRegistration(textBuffer.AsTextContainer());
                }

                void IDisposable.Dispose()
                {
                    if (TextView != null)
                        TextView.Closed -= _source.OnTextViewClosed;
                }
            }
        }
    }
}
