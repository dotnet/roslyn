// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal interface IHACK_EventHookupDismissalOnBufferChangePreventerService
    {
        void HACK_EnsureQuickInfoSessionNotDismissedPrematurely(ITextView textView);
        void HACK_OnQuickInfoSessionDismissed(ITextView textView);
    }
}
