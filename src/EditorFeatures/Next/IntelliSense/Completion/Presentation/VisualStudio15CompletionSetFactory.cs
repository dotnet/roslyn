// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    [ExportVersionSpecific(typeof(ICompletionSetFactory), VisualStudioVersion.Dev15)]
    internal sealed class VisualStudio15CompletionSetFactory : ICompletionSetFactory
    {
        public ICompletionSet CreateCompletionSet(
            CompletionPresenterSession completionPresenterSession,
            ITextView textView,
            ITextBuffer subjectBuffer)
        {
            return new VisualStudio15CompletionSet(
                completionPresenterSession, textView, subjectBuffer);
        }
    }
}
