// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal static class OptionSetExtensions
    {
        public static OptionSet WithDebuggerCompletionOptions(this OptionSet options)
        {
            return options
                .WithChangedOption(CompletionControllerOptions.AlwaysShowBuilder, true)
                .WithChangedOption(CompletionControllerOptions.FilterOutOfScopeLocals, false)
                .WithChangedOption(CompletionControllerOptions.ShowXmlDocCommentCompletion, false);
        }
    }
}
