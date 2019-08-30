// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IDebuggerTextView
    {
        bool IsImmediateWindow { get; }

        void HACK_StartCompletionSession(IIntellisenseSession editorSessionOpt);

        uint StartBufferUpdate();
        void EndBufferUpdate(uint cookie);
    }
}
