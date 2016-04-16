// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EndConstructGeneration
{
    internal interface IEndConstructGenerationService : ILanguageService
    {
        bool TryDo(ITextView textView, ITextBuffer subjectBuffer, char typedChar, CancellationToken cancellationToken);
    }
}
