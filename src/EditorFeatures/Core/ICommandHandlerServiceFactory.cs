// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface ICommandHandlerServiceFactory
    {
        ICommandHandlerService GetService(ITextView textView);
        ICommandHandlerService GetService(ITextBuffer textBuffer);
        void Initialize(string contentTypeName);
    }
}
