// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal static class OptionSetExtensions
    {
        public static OptionSet WithDebuggerCompletionOptions(this OptionSet options)
        {
            return options
                .WithChangedOption(CompletionControllerOptions.FilterOutOfScopeLocals, false)
                .WithChangedOption(CompletionControllerOptions.ShowXmlDocCommentCompletion, false);
        }
    }
}
