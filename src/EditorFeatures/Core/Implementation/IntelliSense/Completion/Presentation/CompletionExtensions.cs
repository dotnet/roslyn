// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using VSCompletion = Microsoft.VisualStudio.Language.Intellisense.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    internal static class CompletionExtensions
    {
        public static CompletionItem GetCompletionItem(this VSCompletion completion)
            => ((CustomCommitCompletion)completion).CompletionItem;
    }
}